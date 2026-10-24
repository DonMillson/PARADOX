"""Original PARADOX spacesuit / spacecraft illustrations built with Tk Canvas.

No external images, proprietary sprites, network calls or generated picture
files. Scales deterministically at common Windows DPI settings.
"""
from __future__ import annotations

import math
import random
import tkinter as tk

INK = "#0B101C"
SPACE = "#080C16"
BULKHEAD = "#1B293A"
STEEL = "#26364A"
EDGE = "#58718C"
WHITE = "#F5F2E9"
MUTED = "#B4BDCE"
RED = "#EC384B"
YELLOW = "#FFD15B"
TURQUOISE = "#7AE3DE"
VIOLET = "#AD91DB"


def hazard_stripe(canvas: tk.Canvas, x: int, y: int, width: int,
                  height: int, pitch: int = 22) -> None:
    canvas.create_rectangle(x, y, x + width, y + height,
                            fill=YELLOW, outline="")
    for sx in range(x - height, x + width + height, pitch):
        canvas.create_polygon(sx, y + height, sx + 12, y + height,
                              sx + height + 12, y, sx + height, y,
                              fill=INK, outline="")
    canvas.create_line(x, y, x + width, y, fill="#FFE49A")
    canvas.create_line(x, y + height, x + width, y + height, fill="#101827")


def crew(canvas: tk.Canvas, x: float, y: float, scale: float = 1.0,
         suit: str = RED, outline: str = INK, label: str = "",
         tag: str = "crew") -> None:
    """Hand-drawn original spacesuit character with chunky shadow and glass visor."""
    def rect(x1, y1, x2, y2, **kw):
        return canvas.create_rectangle(x + x1 * scale, y + y1 * scale,
                                       x + x2 * scale, y + y2 * scale,
                                       tags=(tag,), **kw)
    def oval(x1, y1, x2, y2, **kw):
        return canvas.create_oval(x + x1 * scale, y + y1 * scale,
                                  x + x2 * scale, y + y2 * scale,
                                  tags=(tag,), **kw)
    def poly(points, **kw):
        return canvas.create_polygon(
            *(coord + (x if idx % 2 == 0 else y) for idx, coord in
              enumerate(v * scale for v in points)),
            tags=(tag,), **kw)
    oval(-42, 46, 43, 62, fill="#070A11", outline="")
    rect(-42, -12, -17, 31, fill="#253047", outline=outline, width=3)
    rect(-26, 23, -6, 58, fill=suit, outline=outline, width=3)
    rect(12, 23, 34, 58, fill=suit, outline=outline, width=3)
    oval(-35, -55, 37, 42, fill=suit, outline=outline, width=4)
    # Helmet edge and characteristic rectangular visor with its own visual identity.
    oval(-13, -31, 42, 10, fill="#15334D", outline=outline, width=3)
    oval(-10, -28, 37, 4, fill="#80DCEB", outline="#B8F5FA", width=2)
    oval(-4, -24, 23, -17, fill="#EAFFFF", outline="")
    oval(20, -3, 32, 1, fill="#9AEEF5", outline="")
    if label:
        canvas.create_text(x, y + 79 * scale, text=label, fill=WHITE,
                           font=("Segoe UI", max(9, int(12 * scale)), "bold"),
                           tags=(tag,))


def bulkhead(canvas: tk.Canvas, width: int, height: int, seed: int = 17) -> None:
    """Ship corridor with port holes, rivets, vent grates and task lights."""
    rng = random.Random(seed)
    canvas.create_rectangle(0, 0, width, height, fill=SPACE, outline="")
    canvas.create_rectangle(0, 0, width, 32, fill=BULKHEAD, outline="")
    canvas.create_rectangle(0, height - 30, width, height,
                            fill="#101D2B", outline="")
    canvas.create_line(0, 35, width, 35, fill=EDGE, width=2)
    canvas.create_line(0, height - 32, width, height - 32,
                       fill="#41536C", width=2)
    canvas.create_polygon(0, 35, 42, 35, 150, height - 30, 0,
                          height - 30, fill="#132239", outline="")
    canvas.create_polygon(width, 35, width - 42, 35,
                          width - 150, height - 30, width, height - 30,
                          fill="#132239", outline="")
    canvas.create_line(width // 2, 34, width // 2, height - 31,
                       fill="#223B51", dash=(14, 11), width=2)
    for px in range(20, width + 5, 72):
        for py in (16, height - 15):
            canvas.create_oval(px - 3, py - 3, px + 3, py + 3,
                               fill="#718398", outline="#0C1522")
    for i in range(12):
        sx = rng.randrange(50, max(51, width - 50))
        sy = rng.randrange(42, max(43, height - 48))
        canvas.create_oval(sx, sy, sx + rng.choice((1, 2)), sy + 2,
                           fill=rng.choice(("#344C61", "#3A5F73", "#607889")),
                           outline="")
    for xx in (64, width - 132):
        canvas.create_rectangle(xx, 55, xx + 62, 95,
                                fill="#06121E", outline="#436078", width=2)
        for k in range(4):
            canvas.create_line(xx + 10, 64 + k * 7, xx + 54,
                               64 + k * 7, fill="#2B5368", width=3)


def poster(canvas: tk.Canvas, width: int = 780, height: int = 290) -> None:
    """The title splash resembles a game lobby splash, not an admin dashboard."""
    bulkhead(canvas, width, height, 37)
    hazard_stripe(canvas, 22, 12, width - 44, 13)
    canvas.create_rectangle(25, 47, width - 26, height - 37,
                            fill="#122032", outline="#608499", width=2)
    canvas.create_rectangle(38, 61, width - 39, height - 51,
                            fill="#0B1422", outline="#324B67")
    # Rift emblem, not a copy of the Among Us wordmark.
    cx = 180
    cy = 150
    for rr in (88, 73, 57):
        canvas.create_oval(cx - rr, cy - rr, cx + rr, cy + rr,
                           outline=VIOLET if rr == 73 else "#315970", width=3)
    for n in range(8):
        angle = n * math.pi / 4
        ex, ey = cx + 90 * math.cos(angle), cy + 90 * math.sin(angle)
        canvas.create_line(cx + 77 * math.cos(angle), cy + 77 * math.sin(angle),
                           ex, ey, fill=TURQUOISE, width=3)
    crew(canvas, cx - 4, cy + 5, 1.18, suit=RED)
    canvas.create_text(365, 99, anchor="w", text="PARADOX",
                       font=("Segoe UI Black", 49, "bold"), fill=WHITE)
    canvas.create_text(366, 141, anchor="w", text="AMONG US MOD",
                       font=("Segoe UI", 24, "bold"), fill=YELLOW)
    canvas.create_line(367, 170, width - 73, 170, fill=RED, width=5)
    canvas.create_text(367, 197, anchor="w",
                       text="TRUST NO ONE.  THE REALITY IS BROKEN.",
                       font=("Consolas", 12, "bold"), fill=TURQUOISE)
    canvas.create_text(367, 225, anchor="w", text="BY DONMILLSON  /  PARADOX CREW",
                       font=("Consolas", 11), fill=MUTED)


def header(canvas: tk.Canvas, width: int = 920, height: int = 96) -> None:
    """Space-rift banner: PARADOX magenta anomaly and crewmate HUD."""
    canvas.create_rectangle(0, 0, width, height, fill="#070717", outline="")
    rng = random.Random(9127)
    for _ in range(max(90, width // 6)):
        sx = rng.randrange(0, width)
        sy = rng.randrange(0, height)
        canvas.create_oval(sx, sy, sx+rng.choice((1, 2)), sy+1,
                           fill=rng.choice(("#8B55E3", "#A747F4", "#6F4C9D",
                                            "#F4D1FF", "#EA346E")), outline="")
    # Panoramic space-time tear on the right side of the banner.
    cx, cy = width * .77, height * .46
    radius = height * .78
    for i in range(17):
        rr = radius - i * height * .031
        hue = ("#431471", "#6524BD", "#C021D4", "#FF2A90",
               "#EE3664", "#7D37EA")[i % 6]
        canvas.create_arc(cx-rr*1.65, cy-rr, cx+rr*1.65, cy+rr,
                          start=17+i*7, extent=294-i*2, outline=hue,
                          width=max(1, int(height / 48)))
    canvas.create_oval(cx-radius*.50, cy-radius*.40,
                       cx+radius*.50, cy+radius*.40,
                       fill="#040410", outline="#AB31D8", width=2)
    for n in range(21):
        ax = width*.51 + n*width*.023
        ay = height*.43 + math.sin(n*.92)*height*.25
        canvas.create_polygon(
            ax, ay-4, ax+8, ay-11, ax+12, ay+4, ax-4, ay+8,
            fill=("#170B29", "#271134", "#401745")[n % 3],
            outline="#702B89")
    # Sharp spaceship plating and warning geometry.
    canvas.create_polygon(0, 0, width*.42, 0, width*.40, height*.14,
                          0, height*.14, fill="#0C0B20", outline="#7434AA",
                          width=2)
    canvas.create_polygon(0, height*.78, width*.42, height*.78,
                          width*.46, height, 0, height,
                          fill="#110B23", outline="#7232A5", width=2)
    canvas.create_rectangle(16, 10, width*.46, height*.78,
                            fill="#0D091C", outline="#7D36BB", width=2)
    crew(canvas, 51, height*.49, height/220, suit="#E13268")
    canvas.create_text(100, height*.42, text="PARADOX", anchor="w",
                       fill="#EBB9FF", font=("Segoe UI Black",
                       max(24, round(height*.29)), "bold"))
    canvas.create_text(105, height*.69, text="A M O N G   U S   M O D",
                       anchor="w", fill="#F6D9FE",
                       font=("Consolas", max(10, round(height*.115))))
    canvas.create_rectangle(width-140, 15, width-15, height-19,
                            outline="#BD287C", width=2, fill="#0B081B")
    canvas.create_text(width-130, height*.33, anchor="w",
                       text="CREW", fill="#9D95F7",
                       font=("Consolas", 12, "bold"))
    canvas.create_text(width-130, height*.50, anchor="w",
                       text="SYSTEMS", fill="#9D95F7",
                       font=("Consolas", 12, "bold"))
    canvas.create_text(width-130, height*.67, anchor="w",
                       text="ONLINE", fill="#D54C9B",
                       font=("Consolas", 12, "bold"))
    for a in range(9):
        canvas.create_rectangle(width-157, 17+a*11,
                                width-152, 22+a*11,
                                fill="#FF2775", outline="")
    hazard_stripe(canvas, int(width*.47), height-9, int(width*.21), 9, pitch=23)

def hunt_backdrop(canvas: tk.Canvas, width: int = 785,
                  height: int = 327) -> None:
    bulkhead(canvas, width, height, 42)
    hazard_stripe(canvas, 0, height - 26, width, 13)
    canvas.create_text(25, 55, anchor="w", text="FIND THE IMPOSTOR",
                       font=("Consolas", 17, "bold"), fill=YELLOW)
    canvas.create_text(width - 24, 57, anchor="e", text="SECURITY CAM 01 / LIVE",
                       font=("Consolas", 12, "bold"), fill=TURQUOISE)
    canvas.create_rectangle(12, 48, width - 13, height - 48,
                            outline="#3D5E76", width=2)
