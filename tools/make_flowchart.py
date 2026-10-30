# -*- coding: utf-8 -*-
"""
Блок-схема алгоритма работы приложения BicycleDrive по ГОСТ 19.701-90.

Используются стандартные символы схем алгоритмов:
  - терминатор (начало/конец)  — прямоугольник со скруглёнными боковинами;
  - процесс                    — прямоугольник;
  - решение                    — ромб;
  - ввод-вывод данных          — параллелограмм.
Результат: docs/flowchart.pdf и docs/flowchart.png
"""
import os

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch, Polygon, Rectangle

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_PDF = os.path.join(BASE_DIR, "docs", "flowchart.pdf")
OUT_PNG = os.path.join(BASE_DIR, "docs", "flowchart.png")

COL_LINE = "#4B0082"
COL_FILL = "#FFFFFF"
COL_TERM = "#6A5ACD"
FONT = "DejaVu Sans"

FS = 10.5


class Sheet(object):
    def __init__(self, ax):
        self.ax = ax

    # --- символы по ГОСТ 19.701-90 ---

    def terminate(self, x, y, text, w=2.6, h=0.9):
        box = FancyBboxPatch((x - w / 2, y - h / 2), w, h,
                             boxstyle="round,pad=0,rounding_size=0.45",
                             facecolor=COL_TERM, edgecolor=COL_LINE, linewidth=1.6)
        self.ax.add_patch(box)
        self.ax.text(x, y, text, ha="center", va="center", fontsize=FS,
                     family=FONT, color="white", fontweight="bold")
        return (x, y)

    def process(self, x, y, text, w=3.4, h=0.9):
        self.ax.add_patch(Rectangle((x - w / 2, y - h / 2), w, h,
                                    facecolor=COL_FILL, edgecolor=COL_LINE, linewidth=1.5))
        self._text(x, y, text, w)
        return (x, y)

    def decision(self, x, y, text, w=3.4, h=1.3):
        points = [(x, y + h / 2), (x + w / 2, y), (x, y - h / 2), (x - w / 2, y)]
        self.ax.add_patch(Polygon(points, closed=True, facecolor=COL_FILL,
                                  edgecolor=COL_LINE, linewidth=1.5))
        self._text(x, y, text, w - 0.7)
        return (x, y)

    def io(self, x, y, text, w=3.8, h=0.9, skew=0.35):
        points = [(x - w / 2 + skew, y + h / 2), (x + w / 2, y + h / 2),
                  (x + w / 2 - skew, y - h / 2), (x - w / 2, y - h / 2)]
        self.ax.add_patch(Polygon(points, closed=True, facecolor=COL_FILL,
                                  edgecolor=COL_LINE, linewidth=1.5))
        self._text(x, y, text, w - 0.6)
        return (x, y)

    def _text(self, x, y, text, w):
        self.ax.text(x, y, text, ha="center", va="center", fontsize=FS,
                     family=FONT, color="#1A1A2E", linespacing=1.35,
                     wrap=False)

    # --- стрелки ---

    def arrow(self, points, label=None, label_at=None):
        for i in range(len(points) - 1):
            (x1, y1), (x2, y2) = points[i], points[i + 1]
            if i == len(points) - 2:
                self.ax.add_patch(FancyArrowPatch((x1, y1), (x2, y2),
                                                  arrowstyle="-|>", mutation_scale=14,
                                                  color=COL_LINE, linewidth=1.5,
                                                  shrinkA=0, shrinkB=0, zorder=2))
            else:
                self.ax.plot([x1, x2], [y1, y2], color=COL_LINE, linewidth=1.5, zorder=2)
        if label:
            lx, ly = label_at
            self.ax.text(lx, ly, label, fontsize=9.5, family=FONT, fontweight="bold",
                         color=COL_LINE, ha="center", va="center",
                         bbox=dict(boxstyle="round,pad=0.15", facecolor="white",
                                   edgecolor="none"))


def build():
    os.makedirs(os.path.dirname(OUT_PDF), exist_ok=True)
    figure, ax = plt.subplots(figsize=(9.2, 11.6))
    ax.set_xlim(-8.2, 8.2)
    ax.set_ylim(-18.5, 1.2)
    ax.set_aspect("equal")
    ax.axis("off")
    figure.patch.set_facecolor("white")
    sheet = Sheet(ax)

    cx = 0.0
    step = 1.6

    # вертикальные координаты
    y1 = 0.0
    y2 = y1 - step
    y3 = y2 - step
    y4 = y3 - step
    y5 = y4 - step
    y6 = y5 - step
    y7 = y6 - step
    y8 = y7 - step
    y9 = y8 - step
    y10 = y9 - step
    y11 = y10 - step
    y12 = y11 - step

    sheet.terminate(cx, y1, "Начало")
    sheet.io(cx, y2, "Ввод логина и пароля\nили выбор режима «Гость»")
    sheet.decision(cx, y3, "Выбран\nрежим «Гость»?")
    sheet.process(cx, y4, "Поиск пользователя в БД\nпо логину и паролю")
    sheet.decision(cx, y5, "Пользователь\nнайден?")
    sheet.process(cx, y6, "Определение роли\nпользователя")
    sheet.io(cx, y7, "Вывод списка товаров\nиз БД")
    sheet.decision(cx, y8, "Роль — менеджер\nили администратор?")
    sheet.process(cx, y9, "Работа со списком\nтоваров")
    sheet.decision(cx, y10, "Завершить\nработу?")
    sheet.process(cx, y11, "Выход на экран входа,\nзакрытие приложения")
    sheet.terminate(cx, y12, "Конец")

    # основной поток
    sheet.arrow([(cx, y1 - 0.45), (cx, y2 + 0.45)])
    sheet.arrow([(cx, y2 - 0.45), (cx, y3 + 0.65)])
    sheet.arrow([(cx, y3 - 0.65), (cx, y4 + 0.45)], "Нет", (0.55, y3 - 0.95))
    sheet.arrow([(cx, y4 - 0.45), (cx, y5 + 0.65)])
    sheet.arrow([(cx, y5 - 0.65), (cx, y6 + 0.45)], "Да", (0.5, y5 - 0.95))
    sheet.arrow([(cx, y6 - 0.45), (cx, y7 + 0.45)])
    sheet.arrow([(cx, y7 - 0.45), (cx, y8 + 0.65)])
    sheet.arrow([(cx, y8 - 0.65), (cx, y9 + 0.45)], "Нет", (0.5, y8 - 0.95))
    sheet.arrow([(cx, y9 - 0.45), (cx, y10 + 0.65)])
    sheet.arrow([(cx, y10 - 0.65), (cx, y11 + 0.45)], "Да", (0.5, y10 - 0.95))
    sheet.arrow([(cx, y11 - 0.45), (cx, y12 + 0.45)])

    # гость: сразу к списку товаров (обход авторизации)
    guest_x = 6.3
    sheet.arrow([(cx + 1.7, y3), (guest_x, y3)], "Да", (cx + 2.6, y3 + 0.28))
    sheet.arrow([(guest_x, y3), (guest_x, y7), (cx + 1.95, y7)])
    ax.text(guest_x + 0.25, (y3 + y7) / 2 - 0.3, "гость:\nтолько\nпросмотр",
            fontsize=9, family=FONT, color=COL_LINE, va="center")

    # ошибка авторизации и возврат к вводу
    err_x = -4.9
    sheet.arrow([(cx - 1.7, y5), (err_x, y5)], "Нет", (cx - 2.4, y5 + 0.28))
    sheet.io(err_x, y5, "Сообщение\nоб ошибке", w=3.0, h=1.0)
    sheet.arrow([(err_x, y5 + 0.5), (err_x, y2), (cx - 1.9, y2)])

    # права менеджера/администратора
    right_x = 4.4
    sheet.arrow([(cx + 1.7, y8), (right_x, y8)], "Да", (cx + 2.3, y8 + 0.28))
    sheet.process(right_x, y8, "Доступны поиск,\nфильтрация, сортировка", w=3.6)
    sheet.arrow([(right_x, y8 - 0.45), (right_x, y9 - 0.05), (cx + 1.7, y9 - 0.05)])

    # возврат к работе со списком
    loop_x = -4.0
    sheet.arrow([(cx - 1.7, y10), (loop_x, y10)], "Нет", (cx - 2.35, y10 + 0.28))
    sheet.arrow([(loop_x, y10), (loop_x, y9), (cx - 1.7, y9)])

    ax.text(0, 0.85, "Блок-схема алгоритма работы приложения BicycleDrive",
            ha="center", va="center", fontsize=14, fontweight="bold",
            family=FONT, color="#1A1A2E")
    ax.text(0, 0.5, "ГОСТ 19.701-90 · ООО «ВелосипедДрайв»",
            ha="center", va="center", fontsize=10.5, family=FONT, color=COL_LINE)

    figure.savefig(OUT_PDF, format="pdf", bbox_inches="tight")
    figure.savefig(OUT_PNG, format="png", dpi=150, bbox_inches="tight")
    plt.close(figure)
    print("saved:", OUT_PDF)
    print("saved:", OUT_PNG)


if __name__ == "__main__":
    build()
