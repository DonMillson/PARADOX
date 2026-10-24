"""PARADOX ship-console installation progress widget.

The canvas draws a dynamic progress gauge; no fake percentage and no remote
assets. Designed for the Windows CustomTkinter launcher and Steam Hub.
Only manipulate it on Tk's UI thread, using existing worker event queues.
"""
from __future__ import annotations

import math
import tkinter as tk
import customtkinter as ctk

SPACE = "#080A17"
BORDER = "#6746B5"
WHITE = "#F5EEFF"
FAINT = "#A9A1C8"
VIOLET = "#873BFF"
MAGENTA = "#E331E9"
CRIMSON = "#FF3658"
GOLD = "#F9BE37"


def gradient(fraction: float) -> str:
    """Purple -> electric magenta -> red; deterministic for all DPIs."""
    fraction = max(0.0, min(1.0, fraction))
    stops = ((135, 59, 255), (227, 49, 233), (255, 54, 88))
    pos = min(1, int(fraction * 2))
    t = fraction * 2 - pos
    if fraction == 1:
        return "#FF3658"
    a, b = stops[pos:pos + 2]
    return "#{:02x}{:02x}{:02x}".format(
        *(round(x * (1 - t) + y * t) for x, y in zip(a, b)))


def progress_state(amount: float) -> str:
    if amount >= 1:
        return "SYSTEMY GOTOWE  //  SPRAWDŹ URUCHOMIENIE GRY"
    if amount >= .97:
        return "KOŃCOWA WERYFIKACJA PLIKÓW"
    if amount >= .62:
        return "INSTALACJA MODUŁÓW"
    if amount >= .02:
        return "POBIERANIE I WERYFIKACJA ZALEŻNOŚCI"
    return "OCZEKIWANIE NA ROZPOCZĘCIE MISJI"


class ParadoxInstallProgress(ctk.CTkFrame):
    """Live progress screen visually inspired by the PARADOX concept art."""

    def __init__(self, master, **kwargs):
        super().__init__(
            master, fg_color=SPACE, corner_radius=8,
            border_width=2, border_color="#6140A2", **kwargs)
        self.amount = 0.0
        self.message = "GOTOWY"
        title = ctk.CTkLabel(
            self, text="INSTALOWANIE SYSTEMÓW  PARADOX",
            text_color=WHITE,
            font=ctk.CTkFont(family="Segoe UI", size=21, weight="bold"))
        title.pack(pady=(12, 0), padx=12)
        self.bar = tk.Canvas(
            self, bg=SPACE, height=88, highlightthickness=0, bd=0)
        self.bar.pack(fill="x", padx=12, pady=(0, 1))
        self.bar.bind("<Configure>", lambda _event: self._paint())
        self.details = ctk.CTkFrame(
            self, fg_color="#0D1126", corner_radius=4,
            border_width=1, border_color="#42345D")
        self.details.pack(fill="x", padx=15, pady=(0, 12))
        self.status_label = ctk.CTkLabel(
            self.details, text="GOTOWY  /  0%", text_color="#F9EAFE",
            font=ctk.CTkFont(family="Segoe UI", size=16, weight="bold"),
            anchor="w")
        self.status_label.pack(fill="x", padx=16, pady=(7, 0))
        self.phase_label = ctk.CTkLabel(
            self.details, text=progress_state(0), text_color=FAINT,
            font=ctk.CTkFont(family="Consolas", size=11), anchor="w")
        self.phase_label.pack(fill="x", padx=16, pady=(0, 8))
        self._paint()

    def set(self, amount: float, message: str | None = None) -> None:
        # Existing installer callbacks run on the Tk main thread.
        self.amount = max(0.0, min(1.0, float(amount)))
        if message is not None:
            self.message = message
        self.status_label.configure(
            text=f"{self.message}  /  {round(self.amount * 100)}%",
            text_color=WHITE)
        self.phase_label.configure(text=progress_state(self.amount))
        self._paint()

    def _paint(self) -> None:
        canvas = self.bar
        if not canvas.winfo_exists():
            return
        canvas.delete("all")
        width = max(290, canvas.winfo_width())
        height = 88
        x0, x1 = 20, max(170, width - 19)
        y0, y1 = 17, 68
        # Recessed spacecraft steel edges and corner trims.
        canvas.create_polygon(
            x0, 9, x1-18, 9, x1, 27, x1, 74, x0+16, 74,
            x0, 58, fill="#1C1836", outline="#7453AF", width=2)
        canvas.create_rectangle(
            x0+7, y0, x1-7, y1, fill="#080619",
            outline="#A546EF", width=2)
        for inset, tint in ((1, "#4B1D86"), (4, "#7228C8")):
            canvas.create_line(x0+9, y0+inset, x1-9, y0+inset,
                               fill=tint, width=2)
        end = x1 - 7
        start = x0 + 10
        available = max(1, end - start - 114)
        filled = int(available * self.amount)
        if filled:
            # Canvas vertical strips produce a neon gradient without extra deps.
            strips = max(1, min(filled, 180))
            for idx in range(strips):
                a = start + filled * idx / strips
                b = start + filled * (idx + 1) / strips + 1
                color = gradient(idx / max(1, strips - 1))
                canvas.create_rectangle(
                    a, y0+8, b, y1-8, fill=color, outline="")
            # Thin light and end-cap flare provide a sense of movement.
            canvas.create_line(start, y0+11, start+filled, y0+11,
                               fill="#F6A8FD", width=2)
            canvas.create_line(
                start+filled, y0+9, start+filled, y1-9,
                fill="#FFB8E8", width=3)
            for i in range(6):
                shimmer_x = start + filled * (i + 1) / 7
                canvas.create_line(
                    shimmer_x-8, y1-12, shimmer_x+11, y0+12,
                    fill="#F7AAFF", width=1, stipple="gray50")
        for i in range(5):
            px = x0 + 22 + i * 12
            canvas.create_polygon(
                px, 11, px+7, 11, px+2, 15, px-5, 15,
                fill=GOLD if i % 2 == 0 else "#282334", outline="")
        canvas.create_text(
            x1-19, (y0+y1)//2, anchor="e", text=f"{round(self.amount * 100)}%",
            font=("Segoe UI", 22, "bold"), fill=WHITE)
        canvas.create_line(x0+10, y1+7, x1-20, y1+7,
                           fill="#7E39D5", width=2)
