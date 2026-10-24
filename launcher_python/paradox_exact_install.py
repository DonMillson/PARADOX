"""PARADOX installer artwork / working progress overlay (Tk + Pillow).

Uses the *actual supplied 1536x864 reference artwork* instead of a
procedural imitation. It does not install files on its own: connect to the
existing GitHub PARADOX installation worker via the shown adapter.
"""
from __future__ import annotations

from pathlib import Path
import sys
import tkinter as tk
from PIL import Image, ImageDraw, ImageFont, ImageTk, ImageFilter

ROOT = Path(getattr(sys, "_MEIPASS", Path(__file__).resolve().parents[1]))
REFERENCE = ROOT / "launcher_python" / "assets" / "paradox_installer_reference.webp"
ART_SIZE = (1536, 864)


def _font(size: int, bold=True):
    for candidate in (
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ):
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
    return ImageFont.load_default()


def _mix(a: int, b: int, t: float) -> int:
    return round(a * (1 - t) + b * t)


def _gradient(t: float) -> tuple[int, int, int]:
    stops = ((109, 35, 243), (236, 32, 223), (255, 43, 99))
    index = 0 if t < .5 else 1
    f = t * 2 - index
    return tuple(_mix(a, b, f) for a, b in zip(stops[index], stops[index + 1]))


def stage_for_fraction(value: float) -> int:
    if value >= 1.0:
        return 3
    if value >= .62:
        return 2
    if value >= .02:
        return 1
    return 0


class ArtworkRenderer:
    def __init__(self, reference: str | Path = REFERENCE):
        self.reference = Image.open(reference).convert("RGB")
        if self.reference.size != ART_SIZE:
            self.reference = self.reference.resize(ART_SIZE, Image.Resampling.LANCZOS)
        self.big_font = _font(50)
        self.small_font = _font(27)
        self.subtitle_font = _font(20, bold=False)

    def render(self, fraction: float, status: str, detail: str = "Weryfikacja plików i przygotowanie modułów") -> Image.Image:
        amount = max(0.0, min(1.0, float(fraction)))
        canvas = self.reference.copy()
        draw = ImageDraw.Draw(canvas, "RGBA")
        # Preserve all ship artwork; repaint only the 67% demo text, live bar,
        # and static download labels to eliminate false displayed progress.
        draw.rounded_rectangle((435, 403, 1150, 479), radius=22, fill=(17, 4, 39, 250))
        # Inset progress track, neon flare and reference's original metal frame.
        draw.rounded_rectangle((441, 410, 1142, 471), radius=17,
                               fill=(15, 5, 36, 255), outline=(129, 35, 183, 255), width=2)
        start, end = 450, 1130
        width = int((end - start) * amount)
        if width > 1:
            overlay = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
            gd = ImageDraw.Draw(overlay, "RGBA")
            for x in range(start, start + width):
                color = _gradient((x - start) / max(1, end - start - 1))
                gd.line((x, 417, x, 464), fill=(*color, 245), width=1)
                if x % 19 == 0:
                    gd.line((x+3, 464, x+18, 418), fill=(255, 212, 250, 64), width=2)
            glow = overlay.filter(ImageFilter.GaussianBlur(12))
            canvas = Image.alpha_composite(canvas.convert("RGBA"), glow)
            canvas = Image.alpha_composite(canvas, overlay)
            draw = ImageDraw.Draw(canvas, "RGBA")
            draw.line((start+width, 415, start+width, 467), fill=(255, 201, 252, 240), width=3)
        # Cover mockup's unchanging "67%" and draw the REAL percentage.
        draw.rounded_rectangle((1250, 392, 1420, 483), radius=8,
                               fill=(15, 5, 35, 245))
        draw.text((1397, 437), f"{round(amount * 100)}%",
                  font=self.big_font, fill=(250, 234, 255, 255), anchor="rm",
                  stroke_width=1, stroke_fill=(102, 30, 142, 255))
        # Replace mockup text with actual queue-driven stage and status.
        draw.rectangle((499, 544, 1087, 624), fill=(10, 8, 29, 245))
        clean_status = status.strip() or "Oczekiwanie na instalację..."
        clean_detail = detail.strip()
        if len(clean_status) > 36:
            clean_status = clean_status[:33] + "..."
        if len(clean_detail) > 54:
            clean_detail = clean_detail[:51] + "..."
        draw.text((510, 552), clean_status, font=self.small_font,
                  fill=(246, 237, 255), anchor="lt")
        draw.text((510, 593), clean_detail, font=self.subtitle_font,
                  fill=(175, 145, 224), anchor="lt")
        # Visually indicate the currently active stage in the left-hand nav.
        idx = stage_for_fraction(amount)
        positions = (305, 375, 453, 530)
        center_y = positions[idx]
        draw.ellipse((48, center_y-24, 109, center_y+24),
                     outline=(238, 88, 253, 255), width=3)
        return canvas.convert("RGB")


class ParadoxExactInstall(tk.Toplevel):
    """Window showing supplied artwork, while progress comes from installer events.

    set_progress must be invoked on the Tk/UI thread. The existing PARADOX
    worker queue already schedules such updates on the main thread.
    """
    def __init__(self, master, *, reference=REFERENCE, width=1280,
                 on_close=None, **kwargs):
        super().__init__(master, **kwargs)
        self.renderer = ArtworkRenderer(reference)
        self.on_close = on_close
        self._fraction = 0.0
        self._status = "Oczekiwanie na instalację..."
        self._detail = "Gotowy do aktywacji systemów PARADOX"
        self._photo = None
        self._drag = None
        self.overrideredirect(True)
        width = max(900, int(width))
        height = round(width * ART_SIZE[1] / ART_SIZE[0])
        self.geometry(f"{width}x{height}")
        self.configure(bg="#0A071A")
        self.canvas = tk.Canvas(self, borderwidth=0, highlightthickness=0,
                                background="#0A071A")
        self.canvas.pack(fill="both", expand=True)
        self.canvas.bind("<Configure>", lambda _ev: self._draw())
        self.canvas.bind("<Button-1>", self._clicked)
        self.canvas.bind("<B1-Motion>", self._dragged)
        self.bind("<Escape>", lambda _ev: self.close())
        self._draw()

    def _coords(self, event):
        w = max(self.canvas.winfo_width(), 1)
        h = max(self.canvas.winfo_height(), 1)
        return event.x * ART_SIZE[0] / w, event.y * ART_SIZE[1] / h

    def _clicked(self, event):
        x, y = self._coords(event)
        if 1468 <= x <= 1529 and y <= 67:
            self.close()
        elif 1390 <= x <= 1467 and y <= 67:
            self.iconify()
        elif y <= 80:
            self._drag = (event.x_root - self.winfo_x(), event.y_root - self.winfo_y())

    def _dragged(self, event):
        if self._drag is not None:
            self.geometry(f"+{event.x_root-self._drag[0]}+{event.y_root-self._drag[1]}")

    def set_progress(self, amount: float, status: str, detail: str = "Weryfikacja plików i przygotowanie modułów"):
        self._fraction, self._status, self._detail = amount, status, detail
        if self.winfo_exists():
            self._draw()

    def _draw(self):
        if not self.winfo_exists():
            return
        result = self.renderer.render(self._fraction, self._status, self._detail)
        w = max(self.canvas.winfo_width(), 900)
        h = max(self.canvas.winfo_height(), 506)
        if (w, h) != result.size:
            result = result.resize((w, h), Image.Resampling.LANCZOS)
        self._photo = ImageTk.PhotoImage(result)
        self.canvas.delete("all")
        self.canvas.create_image(0, 0, anchor="nw", image=self._photo)

    def close(self):
        if callable(self.on_close):
            self.on_close()
        self.destroy()