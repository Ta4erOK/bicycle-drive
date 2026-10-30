# -*- coding: utf-8 -*-
"""
Блок-схема алгоритма работы приложения BicycleDrive по ГОСТ 19.701-90.

Символы:
  - терминатор (начало/конец)  — прямоугольник со скруглёнными боковинами;
  - процесс                    — прямоугольник;
  - решение                    — ромб;
  - ввод-вывод данных          — параллелограмм.

Текст в каждом символе подбирается автоматически: слова переносятся по
измеренной ширине, высота фигуры увеличивается так, чтобы надпись целиком
оставалась внутри контура.

Результат: docs/flowchart.pdf и docs/flowchart.png
"""
import os

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.font_manager import FontProperties
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch, Polygon, Rectangle
from matplotlib.textpath import TextPath

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_PDF = os.path.join(BASE_DIR, "docs", "flowchart.pdf")
OUT_PNG = os.path.join(BASE_DIR, "docs", "flowchart.png")

COL_LINE = "#4B0082"
COL_FILL = "#FFFFFF"
COL_TERM = "#6A5ACD"
COL_TEXT = "#1A1A2E"
FONT = "DejaVu Sans"

FS = 10.5
LINE_SPACING = 1.35
SCALE = 0.55          # дюймов на одну единицу координат
PT_PER_UNIT = SCALE * 72.0   # точек на единицу координат
GAP = 0.55            # вертикальный зазор между символами


def text_width(text, size=FS):
    """Ширина строки в единицах координат."""
    if not text:
        return 0.0
    path = TextPath((0, 0), text, prop=FontProperties(family=FONT, size=size))
    return path.get_extents().width / PT_PER_UNIT


def wrap(text, max_width, size=FS):
    """Перенос по словам с учётом уже расставленных переводов строк."""
    result = []
    for hard_line in text.split("\n"):
        current = ""
        for word in hard_line.split(" "):
            candidate = word if not current else current + " " + word
            if current and text_width(candidate, size) > max_width:
                result.append(current)
                current = word
            else:
                current = candidate
        result.append(current)
    return "\n".join(result)


def max_line_width(text, size=FS):
    return max(text_width(line, size) for line in text.split("\n"))


LH = FS * LINE_SPACING / PT_PER_UNIT   # высота строки в единицах


class Sheet(object):
    def __init__(self, ax, report):
        self.ax = ax
        self.report = report
        self.boxes = {}
        self.artists = {}
        self.kinds = {}

    # ---------- измерение ----------

    def fit_process(self, text, w, pad=0.55, min_h=0.85):
        wrapped = wrap(text, w - pad)
        lines = wrapped.split("\n")
        h = max(min_h, len(lines) * LH + 0.5)
        self.report.append((wrapped.split("\n")[0][:28], max_line_width(wrapped), w - pad))
        return wrapped, h

    def fit_decision(self, text, w, min_h=1.5):
        """Ромб: ширина текста ограничена сужением контура к вершинам."""
        wrapped = wrap(text, w * 0.5)
        h = min_h
        for _ in range(12):
            lines = wrapped.split("\n")
            block_h = len(lines) * LH
            widest = max_line_width(wrapped)
            # высота, при которой блок текста с такой шириной вписан в ромб
            need = block_h / max(0.25, 1.0 - (widest + 0.30) / w)
            h_new = max(min_h, need)
            avail = max(0.8, w * (1.0 - block_h / h_new) - 0.30)
            wrapped_new = wrap(text, avail)
            if wrapped_new == wrapped and abs(h_new - h) < 0.02:
                h = h_new
                break
            wrapped, h = wrapped_new, h_new
        self.report.append((wrapped.split("\n")[0][:28], max_line_width(wrapped), w))
        return wrapped, h

    def fit_io(self, text, w, pad=1.0, min_h=0.85):
        wrapped = wrap(text, w - pad)
        lines = wrapped.split("\n")
        h = max(min_h, len(lines) * LH + 0.5)
        self.report.append((wrapped.split("\n")[0][:28], max_line_width(wrapped), w - pad))
        return wrapped, h

    # ---------- отрисовка ----------

    def _draw_text(self, x, y, text):
        self.ax.text(x, y, text, ha="center", va="center", fontsize=FS,
                     family=FONT, color=COL_TEXT, linespacing=LINE_SPACING,
                     zorder=3)

    def terminate(self, key, x, y, text, w, h):
        self.ax.add_patch(FancyBboxPatch((x - w / 2, y - h / 2), w, h,
                                         boxstyle="round,pad=0,rounding_size=0.45",
                                         facecolor=COL_TERM, edgecolor=COL_LINE,
                                         linewidth=1.6, zorder=2))
        art = self.ax.text(x, y, text, ha="center", va="center", fontsize=FS,
                           family=FONT, color="white", fontweight="bold", zorder=3)
        self._remember(key, x, y, w, h, art, "term")

    def process(self, key, x, y, text, w, h):
        self.ax.add_patch(Rectangle((x - w / 2, y - h / 2), w, h,
                                    facecolor=COL_FILL, edgecolor=COL_LINE,
                                    linewidth=1.5, zorder=2))
        art = self._draw_text(x, y, text)
        self._remember(key, x, y, w, h, art, "proc")

    def decision(self, key, x, y, text, w, h):
        points = [(x, y + h / 2), (x + w / 2, y), (x, y - h / 2), (x - w / 2, y)]
        self.ax.add_patch(Polygon(points, closed=True, facecolor=COL_FILL,
                                  edgecolor=COL_LINE, linewidth=1.5, zorder=2))
        art = self._draw_text(x, y, text)
        self._remember(key, x, y, w, h, art, "dec")

    def io(self, key, x, y, text, w, h, skew=0.38):
        points = [(x - w / 2 + skew, y + h / 2), (x + w / 2, y + h / 2),
                  (x + w / 2 - skew, y - h / 2), (x - w / 2, y - h / 2)]
        self.ax.add_patch(Polygon(points, closed=True, facecolor=COL_FILL,
                                  edgecolor=COL_LINE, linewidth=1.5, zorder=2))
        art = self._draw_text(x, y, text)
        self._remember(key, x, y, w, h, art, "io")

    def _remember(self, key, x, y, w, h, artist=None, kind=None):
        self.boxes[key] = dict(x=x, y=y, w=w, h=h,
                               top=y + h / 2, bottom=y - h / 2,
                               left=x - w / 2, right=x + w / 2)
        if artist is not None:
            self.artists[key] = artist
            self.kinds[key] = kind

    def verify(self, figure, tol=0.02):
        """Проверка: bbox каждой надписи лежит внутри контура своего символа."""
        figure.canvas.draw()
        renderer = figure.canvas.get_renderer()
        inverse = self.ax.transData.inverted()
        problems = []
        for key, artist in self.artists.items():
            extent = artist.get_window_extent(renderer)
            (x0, y0) = inverse.transform((extent.x0, extent.y0))
            (x1, y1) = inverse.transform((extent.x1, extent.y1))
            bx = self.boxes[key]
            kind = self.kinds[key]
            if kind == "dec":
                w2, h2 = bx["w"] / 2.0, bx["h"] / 2.0
                for px, py in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
                    dx = abs(px - bx["x"]) / w2
                    dy = abs(py - bx["y"]) / h2
                    if dx + dy > 1.0 + tol:
                        problems.append((key, round(dx + dy, 3)))
            else:
                pad = 0.38 if kind == "io" else 0.0
                if (x0 < bx["left"] + pad - tol or x1 > bx["right"] - pad + tol or
                        y0 < bx["bottom"] - tol or y1 > bx["top"] + tol):
                    problems.append((key, "размеры"))
        for key, info in problems:
            print("  ПРОБЛЕМА:", key, info)
        print("символов проверено:", len(self.artists), "| проблем:", len(problems))
        return len(problems)

    # ---------- стрелки ----------

    def arrow(self, points, label=None, label_at=None):
        for i in range(len(points) - 1):
            (x1, y1), (x2, y2) = points[i], points[i + 1]
            if i == len(points) - 2:
                self.ax.add_patch(FancyArrowPatch((x1, y1), (x2, y2),
                                                  arrowstyle="-|>", mutation_scale=14,
                                                  color=COL_LINE, linewidth=1.5,
                                                  shrinkA=0, shrinkB=0, zorder=4))
            else:
                self.ax.plot([x1, x2], [y1, y2], color=COL_LINE, linewidth=1.5, zorder=4)
        if label:
            lx, ly = label_at
            self.ax.text(lx, ly, label, fontsize=9.5, family=FONT, fontweight="bold",
                         color=COL_LINE, ha="center", va="center", zorder=5,
                         bbox=dict(boxstyle="round,pad=0.15", facecolor="white",
                                   edgecolor="none"))


# описание символов: (тип, ключ, текст, ширина)
SHAPES = [
    ("term", "start", "Начало", 3.0),
    ("io", "input", "Ввод логина и пароля или выбор режима «Гость»", 5.0),
    ("dec", "is_guest", "Выбран режим «Гость»?", 5.6),
    ("proc", "auth", "Поиск пользователя в БД по логину и паролю", 5.0),
    ("dec", "found", "Пользователь найден?", 4.8),
    ("proc", "role_def", "Определение роли пользователя", 5.0),
    ("io", "show_list", "Вывод списка товаров из БД", 5.0),
    ("dec", "is_manager", "Роль — менеджер или администратор?", 5.8),
    ("proc", "work", "Работа со списком товаров", 5.0),
    ("dec", "finish", "Завершить работу?", 4.6),
    ("proc", "exit", "Выход на экран входа, закрытие приложения", 5.0),
    ("term", "end", "Конец", 3.0),
]


def build():
    os.makedirs(os.path.dirname(OUT_PDF), exist_ok=True)

    probe = Sheet(None, [])
    prepared = []
    total = 0.0
    for kind, key, text, w in SHAPES:
        if kind == "term":
            wrapped, h = text, 0.85
        elif kind == "dec":
            wrapped, h = probe.fit_decision(text, w)
        elif kind == "io":
            wrapped, h = probe.fit_io(text, w)
        else:
            wrapped, h = probe.fit_process(text, w)
        prepared.append((kind, key, wrapped, w, h))
        total += h
    total += GAP * (len(SHAPES) - 1)

    title_h = 1.9
    x_left, x_right = -7.6, 8.0
    span_x = x_right - x_left
    span_y = total + title_h + 0.3

    figure, ax = plt.subplots(figsize=(span_x * SCALE, span_y * SCALE))
    ax.set_position([0, 0, 1, 1])
    ax.set_xlim(x_left, x_right)
    ax.set_ylim(-total - 0.15, title_h + 0.15)
    ax.set_aspect("equal")
    ax.axis("off")
    figure.patch.set_facecolor("white")

    report = []
    sheet = Sheet(ax, report)

    # --- вертикальная раскладка ---
    y = 0.0
    for kind, key, wrapped, w, h in prepared:
        y -= h / 2
        if kind == "term":
            sheet.terminate(key, 0.0, y, wrapped, w, h)
        elif kind == "dec":
            sheet.decision(key, 0.0, y, wrapped, w, h)
        elif kind == "io":
            sheet.io(key, 0.0, y, wrapped, w, h)
        else:
            sheet.process(key, 0.0, y, wrapped, w, h)
        y -= h / 2 + GAP

    b = sheet.boxes

    # --- основной поток ---
    order = ["start", "input", "is_guest", "auth", "found", "role_def",
             "show_list", "is_manager", "work", "finish", "exit", "end"]
    labels = {("is_guest", "auth"): ("Нет", 0.75),
              ("found", "role_def"): ("Да", 0.6),
              ("is_manager", "work"): ("Нет", 0.6),
              ("finish", "exit"): ("Да", 0.6)}
    for upper, lower in zip(order, order[1:]):
        label, dx = labels.get((upper, lower), (None, 0))
        sheet.arrow([(0.0, b[upper]["bottom"]), (0.0, b[lower]["top"])],
                    label, (dx, (b[upper]["bottom"] + b[lower]["top"]) / 2) if label else None)

    # --- гость: сразу к списку товаров ---
    guest_x = 6.6
    y3 = b["is_guest"]["y"]
    sheet.arrow([(b["is_guest"]["right"], y3), (guest_x, y3)],
                "Да", (b["is_guest"]["right"] + 0.75, y3 + 0.32))
    sheet.arrow([(guest_x, y3), (guest_x, b["show_list"]["y"]), (b["show_list"]["right"], b["show_list"]["y"])])
    ax.text(guest_x + 0.3, (y3 + b["show_list"]["y"]) / 2 + 0.55,
            "гость:\nтолько\nпросмотр", fontsize=9, family=FONT, color=COL_LINE,
            va="center", ha="left")

    # --- ошибка авторизации и возврат к вводу ---
    err_w = 3.2
    err_x = b["found"]["left"] - 2.3
    err_text = wrap("Сообщение об ошибке", err_w - 1.0)
    err_h = max(0.95, err_text.count("\n") * LH + 0.5)
    sheet.io("error", err_x, b["found"]["y"], err_text, err_w, err_h)
    y5 = b["found"]["y"]
    sheet.arrow([(b["found"]["left"], y5), (err_x + err_w / 2, y5)],
                "Нет", ((b["found"]["left"] + err_x + err_w / 2) / 2, y5 + 0.32))
    sheet.arrow([(err_x, b["error"]["top"]), (err_x, b["input"]["y"]),
                 (b["input"]["left"], b["input"]["y"])])

    # --- права менеджера/администратора ---
    right_w = 4.2
    right_x = b["is_manager"]["right"] + 2.35
    right_text = wrap("Доступны поиск, фильтрация, сортировка", right_w - 0.55)
    right_h = max(0.95, right_text.count("\n") * LH + 0.5)
    sheet.process("manage", right_x, b["is_manager"]["y"], right_text, right_w, right_h)
    y8 = b["is_manager"]["y"]
    sheet.arrow([(b["is_manager"]["right"], y8), (right_x - right_w / 2, y8)],
                "Да", ((b["is_manager"]["right"] + right_x - right_w / 2) / 2, y8 + 0.32))
    sheet.arrow([(right_x, b["manage"]["bottom"]), (right_x, b["work"]["y"]),
                 (b["work"]["right"], b["work"]["y"])])

    # --- возврат к работе со списком ---
    loop_x = b["finish"]["left"] - 2.2
    y10 = b["finish"]["y"]
    sheet.arrow([(b["finish"]["left"], y10), (loop_x, y10)],
                "Нет", ((b["finish"]["left"] + loop_x) / 2, y10 + 0.32))
    sheet.arrow([(loop_x, y10), (loop_x, b["work"]["y"]), (b["work"]["left"], b["work"]["y"])])

    # --- заголовки ---
    ax.text(0.0, title_h - 0.42, "Блок-схема алгоритма работы приложения BicycleDrive",
            ha="center", va="center", fontsize=14, fontweight="bold",
            family=FONT, color=COL_TEXT)
    ax.text(0.0, title_h - 0.95, "ГОСТ 19.701-90 · ООО «ВелосипедДрайв»",
            ha="center", va="center", fontsize=10.5, family=FONT, color=COL_LINE)

    sheet.verify(figure)
    figure.savefig(OUT_PDF, format="pdf")
    figure.savefig(OUT_PNG, format="png", dpi=150)
    plt.close(figure)

    print("saved:", OUT_PDF)
    print("saved:", OUT_PNG)

    # --- проверка: текст каждой надписи вписан в контур символа ---
    bad = 0
    for kind, key, wrapped, w, h in prepared:
        width = max_line_width(wrapped)
        lines = wrapped.count("\n") + 1
        block_h = lines * LH
        if kind == "dec":
            avail = w * (1.0 - block_h / h) - 0.20
        elif kind == "io":
            avail = w - 0.80
        elif kind == "term":
            avail = w - 0.55
        else:
            avail = w - 0.45
        ok = width <= avail and block_h <= h - 0.15
        if not ok:
            bad += 1
        print(f"  {'OK ' if ok else 'ПЕРЕПОЛНЕНИЕ'} {key:<12} "
              f"текст={width:5.2f} доступно={avail:5.2f} "
              f"строк={lines} блок={block_h:4.2f} высота={h:4.2f}")
    print("переполнений:", bad)


if __name__ == "__main__":
    build()
