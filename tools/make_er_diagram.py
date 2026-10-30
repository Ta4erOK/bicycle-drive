# -*- coding: utf-8 -*-
"""
Построение ER-диаграммы базы данных BicycleDrive.

Диаграмма рисуется средствами matplotlib и сохраняется в PDF:
таблицы, атрибуты, первичные (PK) и внешние (FK) ключи, связи 1:N.
"""
import os

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, Rectangle

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_PDF = os.path.join(BASE_DIR, "docs", "ER_diagram.pdf")
OUT_PNG = os.path.join(BASE_DIR, "docs", "ER_diagram.png")

COL_HEADER = "#6A5ACD"
COL_BORDER = "#4B0082"
COL_BG = "#FFFFFF"
FONT = "DejaVu Sans"

HEADER_H = 0.34
ROW_H = 0.28

# Описание таблиц: имя -> список атрибутов (имя, ключ)
TABLES = {
    "Categories": [("Id", "PK"), ("Name", "")],
    "Manufacturers": [("Id", "PK"), ("Name", "")],
    "Suppliers": [("Id", "PK"), ("Name", "")],
    "Units": [("Id", "PK"), ("Name", "")],
    "Roles": [("Id", "PK"), ("Name", "")],
    "Statuses": [("Id", "PK"), ("Name", "")],
    "PickupPoints": [("Id", "PK"), ("Address", "")],
    "Users": [("Id", "PK"), ("RoleId", "FK"), ("FullName", ""), ("Login", ""), ("Password", "")],
    "Products": [("Id", "PK"), ("Article", ""), ("Name", ""), ("Description", ""),
                 ("CategoryId", "FK"), ("ManufacturerId", "FK"), ("SupplierId", "FK"),
                 ("UnitId", "FK"), ("Price", ""), ("Quantity", ""), ("Discount", ""),
                 ("ImagePath", "")],
    "Orders": [("Id", "PK"), ("Number", ""), ("UserId", "FK"), ("PickupPointId", "FK"),
               ("StatusId", "FK"), ("OrderDate", ""), ("DeliveryDate", ""), ("Code", "")],
    "OrderItems": [("Id", "PK"), ("OrderId", "FK"), ("ProductId", "FK"), ("Quantity", "")],
}

# Положение таблиц: x и верхний край y
POS = {
    "Categories": (0.40, 9.70),
    "Manufacturers": (0.40, 8.60),
    "Suppliers": (0.40, 7.50),
    "Units": (0.40, 6.40),
    "Roles": (0.40, 5.30),
    "Users": (5.40, 3.90),
    "Products": (5.40, 9.70),
    "Orders": (11.40, 9.70),
    "OrderItems": (11.40, 6.60),
    "Statuses": (11.40, 4.40),
    "PickupPoints": (11.40, 3.30),
}
WIDTH = {"Products": 4.20, "Users": 4.20, "Orders": 4.20, "OrderItems": 4.20}
DEFAULT_WIDTH = 3.60


def row_center(table, attribute):
    """Вертикальный центр строки атрибута."""
    x, y_top = POS[table]
    index = [name for name, _ in TABLES[table]].index(attribute)
    return y_top - HEADER_H - ROW_H * index - ROW_H / 2


def draw_table(ax, name):
    x, y_top = POS[name]
    width = WIDTH.get(name, DEFAULT_WIDTH)
    attributes = TABLES[name]
    height = HEADER_H + ROW_H * len(attributes)

    # заголовок
    ax.add_patch(Rectangle((x, y_top - HEADER_H), width, HEADER_H,
                           facecolor=COL_HEADER, edgecolor=COL_BORDER, linewidth=1.4))
    ax.text(x + width / 2, y_top - HEADER_H / 2, name, ha="center", va="center",
            color="white", fontsize=11, fontweight="bold", family=FONT)

    # корпус
    ax.add_patch(Rectangle((x, y_top - height), width, height - HEADER_H,
                           facecolor=COL_BG, edgecolor=COL_BORDER, linewidth=1.4))

    for index, (attribute, key) in enumerate(attributes):
        center = y_top - HEADER_H - ROW_H * index - ROW_H / 2
        if index > 0:
            ax.plot([x, x + width], [center + ROW_H / 2, center + ROW_H / 2],
                    color="#D8D8E8", linewidth=0.6, zorder=1)
        label = attribute
        if key:
            label = f"{attribute}  ({key})"
        ax.text(x + 0.14, center, label, ha="left", va="center", fontsize=9,
                family=FONT, fontweight="bold" if key else "normal",
                color="#1A1A2E")
    return width, height


SEGMENTS = []


def _segment(p1, p2):
    """Регистрирует отрезок для последующей проверки пересечений."""
    SEGMENTS.append((p1, p2))


def table_rect(name):
    x, y_top = POS[name]
    width = WIDTH.get(name, DEFAULT_WIDTH)
    height = HEADER_H + ROW_H * len(TABLES[name])
    return (x, y_top - height, x + width, y_top)


def _seg_hits_rect(p1, p2, rect, tol=0.06):
    """Проверяет, проходит ли отрезок через внутренность прямоугольника."""
    x1, y1, x2, y2 = rect
    x1, y1 = x1 + tol, y1 + tol
    x2, y2 = x2 - tol, y2 - tol
    if x1 >= x2 or y1 >= y2:
        return False
    # вертикальный отрезок
    if abs(p1[0] - p2[0]) < 1e-6:
        return x1 < p1[0] < x2 and min(p1[1], p2[1]) < y2 and max(p1[1], p2[1]) > y1
    # горизонтальный отрезок
    if abs(p1[1] - p2[1]) < 1e-6:
        return y1 < p1[1] < y2 and min(p1[0], p2[0]) < x2 and max(p1[0], p2[0]) > x1
    return False


def verify():
    """Проверка: линии связей не должны проходить сквозь таблицы."""
    errors = []
    for p1, p2 in SEGMENTS:
        for name in TABLES:
            if _seg_hits_rect(p1, p2, table_rect(name)):
                errors.append("линия %s -> %s пересекает %s" % (
                    _fmt(p1), _fmt(p2), name))
    return errors


def _fmt(point):
    return "(%.2f, %.2f)" % point


def draw_link(ax, source, source_attr, target, target_attr, mid_x=None):
    """Связь 1:N от PK (source) к FK (target)."""
    sx, sy_top = POS[source]
    tx, ty_top = POS[target]
    s_width = WIDTH.get(source, DEFAULT_WIDTH)
    y1 = row_center(source, source_attr)
    y2 = row_center(target, target_attr)

    # определяем стороны соединения
    if abs(tx - sx) < 0.6:
        # таблицы друг над другом — вертикальная связь
        top_table, bottom_table = (source, target) if sy_top > ty_top else (target, source)
        x_line = sx + WIDTH.get(source, DEFAULT_WIDTH) / 2
        y_start = sy_top - (HEADER_H + ROW_H * len(TABLES[source])) if top_table == source else ty_top
        y_end = ty_top if top_table == source else sy_top - (HEADER_H + ROW_H * len(TABLES[target]))
        ax.annotate("", xy=(x_line, y_end), xytext=(x_line, y_start),
                    arrowprops=dict(arrowstyle="-|>", color=COL_BORDER, linewidth=1.5))
        _segment((x_line, y_start), (x_line, y_end))
        label_y = (y_start + y_end) / 2
        ax.text(x_line - 0.15, label_y, "1", fontsize=9, fontweight="bold",
                ha="right", va="center", color=COL_BORDER, family=FONT)
        ax.text(x_line + 0.15, label_y, "N", fontsize=9, fontweight="bold",
                ha="left", va="center", color=COL_BORDER, family=FONT)
        return

    if tx > sx:
        start = (sx + s_width, y1)
        end = (tx, y2)
    else:
        start = (sx, y1)
        end = (tx + WIDTH.get(target, DEFAULT_WIDTH), y2)

    if mid_x is None:
        mid_x = (start[0] + end[0]) / 2
    ax.plot([start[0], mid_x], [start[1], start[1]], color=COL_BORDER, linewidth=1.5, zorder=2)
    ax.plot([mid_x, mid_x], [start[1], end[1]], color=COL_BORDER, linewidth=1.5, zorder=2)
    _segment((start[0], start[1]), (mid_x, start[1]))
    _segment((mid_x, start[1]), (mid_x, end[1]))
    _segment((mid_x, end[1]), end)
    ax.add_patch(FancyArrowPatch((mid_x, end[1]), end, arrowstyle="-|>", mutation_scale=12,
                                 color=COL_BORDER, linewidth=1.5, zorder=3))
    ax.text(start[0] + 0.08, start[1] + 0.12, "1", fontsize=9, fontweight="bold",
            color=COL_BORDER, family=FONT)
    ax.text(end[0] - 0.22, end[1] + 0.12, "N", fontsize=9, fontweight="bold",
            color=COL_BORDER, family=FONT)


def draw_bottom_link(ax, source, source_attr, target, target_attr, route_x):
    """Связь 1:N с обходом справа: от правого края источника к правому краю приёмника."""
    sx, _ = POS[source]
    s_width = WIDTH.get(source, DEFAULT_WIDTH)
    y1 = row_center(source, source_attr)
    y2 = row_center(target, target_attr)
    start = (sx + s_width, y1)
    end = (POS[target][0] + WIDTH.get(target, DEFAULT_WIDTH), y2)
    ax.plot([start[0], route_x], [y1, y1], color=COL_BORDER, linewidth=1.5, zorder=2)
    ax.plot([route_x, route_x], [y1, y2], color=COL_BORDER, linewidth=1.5, zorder=2)
    ax.add_patch(FancyArrowPatch((route_x, y2), end, arrowstyle="-|>", mutation_scale=12,
                                 color=COL_BORDER, linewidth=1.5, zorder=3))
    ax.text(start[0] + 0.08, y1 + 0.12, "1", fontsize=9, fontweight="bold",
            color=COL_BORDER, family=FONT)
    ax.text(end[0] + 0.08, y2 + 0.12, "N", fontsize=9, fontweight="bold",
            color=COL_BORDER, family=FONT)


def build():
    os.makedirs(os.path.dirname(OUT_PDF), exist_ok=True)
    figure, ax = plt.subplots(figsize=(16.5, 11.0))
    ax.set_xlim(0, 16.3)
    ax.set_ylim(2.0, 10.9)
    ax.axis("off")
    figure.patch.set_facecolor("white")

    for name in TABLES:
        draw_table(ax, name)

    # связи 1:N
    draw_link(ax, "Categories", "Id", "Products", "CategoryId")
    draw_link(ax, "Manufacturers", "Id", "Products", "ManufacturerId")
    draw_link(ax, "Suppliers", "Id", "Products", "SupplierId")
    draw_link(ax, "Units", "Id", "Products", "UnitId")
    draw_link(ax, "Roles", "Id", "Users", "RoleId")
    draw_link(ax, "Users", "Id", "Orders", "UserId", mid_x=10.35)
    draw_link(ax, "Products", "Id", "OrderItems", "ProductId", mid_x=10.90)
    draw_link(ax, "Orders", "Id", "OrderItems", "OrderId")
    draw_bottom_link(ax, "Statuses", "Id", "Orders", "StatusId", route_x=15.45)
    draw_bottom_link(ax, "PickupPoints", "Id", "Orders", "PickupPointId", route_x=15.90)

    ax.text(8.1, 10.62, "ER-диаграмма базы данных BicycleDrive",
            ha="center", va="center", fontsize=16, fontweight="bold",
            family=FONT, color="#1A1A2E")
    ax.text(8.1, 10.30, "ООО «ВелосипедДрайв» · 3 нормальная форма · связи 1:N",
            ha="center", va="center", fontsize=11, family=FONT, color="#4B0082")

    figure.savefig(OUT_PDF, format="pdf", bbox_inches="tight")
    figure.savefig(OUT_PNG, format="png", dpi=140, bbox_inches="tight")
    plt.close(figure)
    problems = verify()
    if problems:
        print("ВНИМАНИЕ, пересечения линий с таблицами:")
        for problem in problems:
            print("  -", problem)
    else:
        print("проверка: пересечений нет")
    print("saved:", OUT_PDF)
    print("saved:", OUT_PNG)


if __name__ == "__main__":
    build()
