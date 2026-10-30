# -*- coding: utf-8 -*-
"""
Подготовка данных заказчика (Приложение 2) к импорту в БД BicycleDrive.

Читает xlsx-файлы, нормализует значения (пробелы, даты, состав заказа)
и формирует скрипт db/data.sql с INSERT-запросами.
"""
import os
import re
import datetime
from decimal import Decimal

import openpyxl

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMPORT_DIR = os.path.join(BASE_DIR, "Приложение_2_Данные_для_импорта", "import")
DATA_SQL = os.path.join(BASE_DIR, "db", "data.sql")

# Заведомо некорректная дата из исходного файла (30 февраля не существует)
BAD_DATE_REPLACEMENT = datetime.date(2023, 2, 28)


def sql_str(value):
    """Литерал nvarchar для T-SQL."""
    if value is None:
        return "NULL"
    return "N'" + str(value).replace("'", "''") + "'"


def sql_num(value, digits=2):
    if value is None:
        return "NULL"
    return f"{Decimal(str(value)).quantize(Decimal('1.' + '0' * digits))}"


def clean_text(value):
    """Убирает лишние пробелы и переводы строк."""
    if value is None:
        return None
    text = str(value).replace("\n", " ").replace("\r", " ")
    text = re.sub(r"\s+", " ", text).strip()
    return text or None


def load_rows(filename):
    path = os.path.join(IMPORT_DIR, filename)
    workbook = openpyxl.load_workbook(path, data_only=True)
    sheet = workbook.worksheets[0]
    rows = []
    for row in sheet.iter_rows(values_only=True):
        if all(cell is None or str(cell).strip() == "" for cell in row):
            continue
        rows.append(row)
    return rows


def parse_date(value, issues):
    """Дата из ячейки: datetime, 'дд.мм.гггг' или текст."""
    if isinstance(value, datetime.datetime):
        return value.date()
    if isinstance(value, datetime.date):
        return value
    text = str(value).strip()
    for fmt in ("%d.%m.%Y", "%Y-%m-%d", "%d.%m.%y"):
        try:
            return datetime.datetime.strptime(text, fmt).date()
        except ValueError:
            continue
    issues.append(f"некорректная дата {text!r} заменена на {BAD_DATE_REPLACEMENT}")
    return BAD_DATE_REPLACEMENT


def build():
    issues = []

    # ---------------- Товары ----------------
    product_rows = load_rows("Tovar.xlsx")[1:]
    products = []
    categories, manufacturers, suppliers, units = [], [], [], []
    for row in product_rows:
        article = clean_text(row[0])
        name = clean_text(row[1])
        unit = clean_text(row[2])
        price = row[3]
        supplier = clean_text(row[4])
        manufacturer = clean_text(row[5])
        category = clean_text(row[6])
        discount = row[7] or 0
        quantity = row[8] or 0
        description = clean_text(row[9])
        photo = clean_text(row[10])

        if photo is None:
            issues.append(f"товар {article}: нет фото, используется заглушка picture.png")
            photo = "picture.png"
        else:
            photo = photo.lower()

        for collection, value in (
            (categories, category),
            (manufacturers, manufacturer),
            (suppliers, supplier),
            (units, unit),
        ):
            if value not in collection:
                collection.append(value)

        products.append({
            "article": article,
            "name": name,
            "category": category,
            "manufacturer": manufacturer,
            "supplier": supplier,
            "unit": unit,
            "price": price,
            "quantity": quantity,
            "discount": discount,
            "description": description,
            "image": photo,
        })

    # ---------------- Пользователи ----------------
    user_rows = load_rows("user_import.xlsx")[1:]
    users = []
    roles = []
    for row in user_rows:
        role = clean_text(row[0])
        if role not in roles:
            roles.append(role)
        users.append({
            "role": role,
            "full_name": clean_text(row[1]),
            "login": clean_text(row[2]),
            "password": clean_text(row[3]),
        })

    # ---------------- Пункты выдачи ----------------
    point_rows = load_rows("Пункты выдачи_import.xlsx")
    points = []
    for row in point_rows:
        address = clean_text(row[0])
        if address and address not in points:
            points.append(address)

    # ---------------- Заказы ----------------
    order_rows = load_rows("Заказ_import.xlsx")[1:]
    orders = []
    statuses = []
    for row in order_rows:
        number = int(row[0])
        composition = clean_text(row[1])
        order_date = parse_date(row[2], issues)
        delivery_date = parse_date(row[3], issues) if row[3] else None
        point_index = int(row[4])
        customer = clean_text(row[5])
        code = int(row[6])
        status = clean_text(row[7])
        if status not in statuses:
            statuses.append(status)

        items = []
        parts = [part.strip() for part in composition.split(",") if part.strip()]
        if len(parts) % 2 != 0:
            issues.append(f"заказ {number}: состав {composition!r} не разобран")
        for index in range(0, len(parts) - 1, 2):
            items.append({"article": parts[index], "quantity": int(parts[index + 1])})

        orders.append({
            "number": number,
            "items": items,
            "order_date": order_date,
            "delivery_date": delivery_date,
            "point_index": point_index,
            "customer": customer,
            "code": code,
            "status": status,
        })

    # ---------------- Генерация SQL ----------------
    lines = [
        "/* =====================================================================",
        "   ООО «ВелосипедДрайв» — данные, подготовленные из файлов заказчика",
        "   (Приложение 2). Скрипт сформирован скриптом tools/import_data.py.",
        "   ===================================================================== */",
        "USE [BicycleDrive];",
        "GO",
        "SET NOCOUNT ON;",
        "GO",
    ]

    def insert_block(table, columns, rows, values_fn):
        if not rows:
            return
        lines.append(f"\n-- {table}")
        lines.append(f"INSERT INTO dbo.{table} ({', '.join(columns)}) VALUES")
        body = ",\n".join("    (" + ", ".join(values_fn(row)) + ")" for row in rows)
        lines.append(body + ";")
        lines.append("GO")

    insert_block("Roles", ["Name"], roles, lambda r: [sql_str(r)])
    insert_block("Units", ["Name"], units, lambda r: [sql_str(r)])
    insert_block("Categories", ["Name"], categories, lambda r: [sql_str(r)])
    insert_block("Manufacturers", ["Name"], manufacturers, lambda r: [sql_str(r)])
    insert_block("Suppliers", ["Name"], suppliers, lambda r: [sql_str(r)])
    insert_block("Statuses", ["Name"], statuses, lambda r: [sql_str(r)])
    insert_block("PickupPoints", ["Address"], points, lambda r: [sql_str(r)])

    role_id = {name: index + 1 for index, name in enumerate(roles)}
    insert_block("Users", ["RoleId", "FullName", "Login", "Password"], users,
                 lambda u: [str(role_id[u["role"]]), sql_str(u["full_name"]),
                            sql_str(u["login"]), sql_str(u["password"])])

    insert_block("Products",
                 ["Article", "Name", "Description", "CategoryId", "ManufacturerId",
                  "SupplierId", "UnitId", "Price", "Quantity", "Discount", "ImagePath"],
                 products,
                 lambda p: [
                     sql_str(p["article"]), sql_str(p["name"]), sql_str(p["description"]),
                     str(categories.index(p["category"]) + 1),
                     str(manufacturers.index(p["manufacturer"]) + 1),
                     str(suppliers.index(p["supplier"]) + 1),
                     str(units.index(p["unit"]) + 1),
                     sql_num(p["price"]), str(int(p["quantity"])),
                     sql_num(p["discount"]), sql_str(p["image"]),
                 ])

    user_id = {u["full_name"]: index + 1 for index, u in enumerate(users)}
    status_id = {name: index + 1 for index, name in enumerate(statuses)}

    order_values = []
    item_values = []
    for index, order in enumerate(orders, start=1):
        missing = order["customer"] not in user_id
        if missing:
            issues.append(f"заказ {order['number']}: клиент {order['customer']!r} не найден")
        order_values.append([
            str(order["number"]), str(user_id.get(order["customer"], 1)),
            str(order["point_index"]), str(status_id[order["status"]]),
            "'" + order["order_date"].isoformat() + "'",
            ("'" + order["delivery_date"].isoformat() + "'") if order["delivery_date"] else "NULL",
            str(order["code"]),
        ])
        for item in order["items"]:
            articles = [p["article"] for p in products]
            if item["article"] not in articles:
                issues.append(f"заказ {order['number']}: артикул {item['article']!r} не найден")
                continue
            item_values.append([
                str(index), str(articles.index(item["article"]) + 1), str(item["quantity"]),
            ])

    insert_block("Orders",
                 ["Number", "UserId", "PickupPointId", "StatusId", "OrderDate",
                  "DeliveryDate", "Code"],
                 order_values, lambda values: values)

    insert_block("OrderItems", ["OrderId", "ProductId", "Quantity"],
                 item_values, lambda values: values)

    lines.append("\nPRINT N'Импорт завершён.';")
    lines.append("GO")

    with open(DATA_SQL, "w", encoding="utf-8-sig", newline="\r\n") as stream:
        stream.write("\n".join(lines))

    print("Roles:", len(roles), roles)
    print("Units:", units)
    print("Categories:", len(categories))
    print("Manufacturers:", len(manufacturers), manufacturers)
    print("Suppliers:", len(suppliers), suppliers)
    print("Statuses:", statuses)
    print("PickupPoints:", len(points))
    print("Users:", len(users))
    print("Products:", len(products))
    print("Orders:", len(orders), "OrderItems:", len(item_values))
    print("--- замечания по данным ---")
    for issue in issues:
        print(" *", issue)


if __name__ == "__main__":
    build()
