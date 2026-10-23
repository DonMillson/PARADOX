"""PARADOX AMONG US: original spaceship-inspired illustrations drawn with Tk.

All artwork is deterministic vector geometry, packed with Python sources into
the EXE. No web assets, AI-watermarked images or third-party Among Us sprites.
"""
from __future__ import annotations

import random
import tkinter as tk

INK = "#080C15"
SPACE = "#081020"
HULL = "#243447"
HULL_L = "#415B71"
CREAM = "#FFF7D5"
RED = "#E33E50"
RED_DARK = "#872334"
YELLOW = "#F9D96B"
BLUE = "#91DEF0"
TEAL = "#4AB5BE"
VIOLET = "#A58BD3"


def _stripe(canvas: tk.Canvas, x: float, y: float, height: float) -> None:
    canvas.create_rectangle(x, y, x + 29, y + height, fill=YELLOW, outline=INK, width=2)
    for shift in range(0, int(height), 23):
        canvas.create_polygon(x, y + shift, x + 13, y + shift,
                              x + 29, y + shift + 17, x + 29, y + shift + 23,
                              fill=INK, outline="")


def _crewmate(canvas: tk.Canvas, x: float, y: float,
              scale: float, suit: str, backpack: str, visor: str = BLUE,
              tag: str | None = None) -> None:
    """Draw an original spacesuit avatar from outlined vector primitives."""
    def c(points: tuple[float, ...]) -> tuple[float, ...]:
        return tuple(x + points[i] * scale if i % 2 == 0
                     else y + points[i] * scale for i in range(len(points)))

    tags = (tag,) if tag else ()
    # Soft shadow, pack, legs, suit and visor are separate stable canvas items.
    canvas.create_oval(*c((-36, 48, 49, 65)), fill=INK, outline="", tags=tags)
    canvas.create_oval(*c((-49, -12, -8, 41)), fill=backpack, outline=INK,
                       width=3, tags=tags)
    canvas.create_polygon(*c((-23, 28, -20, 57, -2, 57, 5, 34,
                              19, 34, 25, 57, 44, 57, 40, 13)),
                          fill=suit, outline=INK, width=4, tags=tags, smooth=False)
    canvas.create_oval(*c((-31, -53, 42, 41)), fill=suit, outline=INK,
                       width=4, tags=tags)
    canvas.create_oval(*c((-12, -37, 50, -2)), fill="#4B8DA4", outline=INK,
                       width=4, tags=tags)
    canvas.create_oval(*c((-8, -34, 45, -9)), fill=visor, outline="", tags=tags)
    canvas.create_oval(*c((1, -30, 22, -22)), fill="#E5FAFF", outline="", tags=tags)


def draw_bridge(canvas: tk.Canvas, width: int, height: int = 282) -> None:
    """Paint a dramatic spaceship interior for the launcher hero panel."""
    width = max(620, width)
    canvas.delete("art")
    tag = "art"
    canvas.configure(bg=SPACE)
    canvas.create_rectangle(0, 0, width, height, fill=SPACE, outline="", tags=tag)
    # Distant stars through the central viewport.
    rng = random.Random(627)
    for _ in range(105):
        sx, sy = rng.randrange(25, width-25), rng.randrange(15, height-35)
        radius = rng.choice((1, 1, 1, 2))
        canvas.create_oval(sx-radius, sy-radius, sx+radius, sy+radius,
                           fill=rng.choice(("#59738E", "#91B7D0", "#D5EDF3")),
                           outline="", tags=tag)
    canvas.create_oval(width*.54-135, -142, width*.54+135, 124,
                       fill="#202E60", outline="", tags=tag)
    canvas.create_oval(width*.54-96, -105, width*.54+96, 92,
                       fill="#786AA9", outline="#31466B", width=7, tags=tag)
    # Angular spacecraft bulkheads; floor.
    canvas.create_polygon(0,0, 140,0, 203,height-65, 0,height,
                          fill=HULL, outline=INK, width=5, tags=tag)
    canvas.create_polygon(width,0, width-140,0, width-203,height-65,
                          width,height, fill=HULL, outline=INK, width=5, tags=tag)
    canvas.create_polygon(0,height-65, width*.25,height-106,
                          width*.75,height-106, width,height-65,
                          width,height, 0,height, fill="#152638",
                          outline=INK, width=5, tags=tag)
    for x in (38, width-65):
        _stripe(canvas, x, 26, 151)
    for xx in (12, 166, width-168, width-18):
        for yy in (20, height-35):
            canvas.create_oval(xx-3,yy-3,xx+3, yy+3, fill="#6E8796",
                               outline=INK, tags=tag)
    # Suspicious visor silhouettes and original characters.
    _crewmate(canvas, width*.23, height*.62, .83, RED, RED_DARK)
    _crewmate(canvas, width*.81, height*.69, .63, TEAL, "#216B75")
    # Strong block lettering with offset outline/drop shadow.
    tx = width * .51
    ty = height*.55
    for dx,dy,col in ((5,5,INK),(3,3,INK),(-3,-3,INK)):
        canvas.create_text(tx+dx,ty+dy, text="PARADOX",
                           font=("Arial Black", 46, "bold"),
                           fill=col, tags=tag)
    canvas.create_text(tx,ty, text="PARADOX", font=("Arial Black", 46,"bold"),
                       fill=CREAM, tags=tag)
    canvas.create_text(tx,ty+45, text="AMONG US  /  CREW EDITION",
                       font=("Arial Black", 15,"bold"),
                       fill=YELLOW, tags=tag)
    canvas.create_rectangle(width*.35, height-27, width*.67, height-11,
                            fill=RED, outline=INK, width=2, tags=tag)
    canvas.create_text(width*.51,height-19,text="BY DONMILLSON",
                       font=("Arial Black", 9, "bold"),fill=CREAM,tags=tag)


def draw_hunt_room(canvas: tk.Canvas, width: int = 785, height: int = 327) -> None:
    """Draw the engine-room minigame arena; target drawings are independent."""
    canvas.configure(bg="#142234")
    canvas.delete("arena")
    tag = "arena"
    canvas.create_rectangle(0,0,width,height,fill="#142234",
                            outline="",tags=tag)
    # Tiled floor, pipes, doors and warning-tape boundaries.
    for x in range(0,width,65):
        canvas.create_line(x,0,x,height,fill="#26364B",width=2,tags=tag)
    for y in range(0,height,63):
        canvas.create_line(0,y,width,y,fill="#26364B",width=2,tags=tag)
    canvas.create_rectangle(0, 0, width, 34,fill="#42586A",
                            outline=INK,width=3,tags=tag)
    canvas.create_rectangle(0,height-33,width,height,fill="#42586A",
                            outline=INK,width=3,tags=tag)
    for x in range(20,width,95):
        canvas.create_polygon(x,0,x+20,0,x+56,32,x+35,32,
                              fill=YELLOW,outline="",tags=tag)
        canvas.create_polygon(x,height-33,x+20,height-33,
                              x+56,height,x+35,height,
                              fill=YELLOW,outline="",tags=tag)
    for x in (90,690):
        canvas.create_rectangle(x,65,x+52,100,fill="#080E1A",
                                outline=INK,width=4,tags=tag)
        for n in range(4):
            canvas.create_line(x+8,73+n*7,x+44,73+n*7,
                               fill="#385469",width=3,tags=tag)
    canvas.create_text(width/2,18,text="SABOTAGE  /  IMPOSTOR DETECTED",
                       fill=CREAM, font=("Arial Black",12,"bold"),tags=tag)


def draw_target(canvas: tk.Canvas, x: float, y: float) -> None:
    """Create an outlined moving red impostor (all parts share 'target' tag)."""
    _crewmate(canvas,x,y,.82,RED,RED_DARK,BLUE,tag="target")
