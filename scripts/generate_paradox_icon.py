"""Render PARADOX's original multi-resolution Windows executable icon."""
from __future__ import annotations

import argparse
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

SIZE = 1024

def paint() -> Image.Image:
    s = SIZE
    base = Image.new("RGBA", (s, s), (6, 7, 20, 255))
    px = base.load()
    for y in range(s):
        for x in range(s):
            r = math.hypot(x - 500, y - 510) / 720
            glow = max(0, 1 - abs(math.hypot(x - 510, y - 510) - 325) / 270)
            px[x, y] = (int(7 + 28 * glow), int(6 + 4 * glow),
                        int(21 + 42 * (1 - r) + 25 * glow), 255)

    aura = Image.new("RGBA", base.size, (0, 0, 0, 0))
    a = ImageDraw.Draw(aura)
    for i in range(34):
        inset = 115 + i * 5
        c = (95 + (i * 9) % 120, 16, 210 + (i * 5) % 45, max(4, 35 - i))
        a.arc((inset, inset, s-inset, s-inset), 200-i*8, 515-i*8, fill=c, width=14)
    base = Image.alpha_composite(base, aura.filter(ImageFilter.GaussianBlur(29)))
    base = Image.alpha_composite(base, aura)
    d = ImageDraw.Draw(base, "RGBA")
    for width, c in [(40, (151, 33, 233, 15)),
                     (20, (163, 42, 240, 50)),
                     (8, (219, 93, 255, 250))]:
        d.rounded_rectangle((60, 60, 964, 964), radius=178, outline=c, width=width)
    d.rounded_rectangle((83, 83, 941, 941), radius=160,
                        outline=(75, 20, 139, 150), width=7)
    for i in range(15):
        angle = 2 * math.pi * i / 15 + 0.3
        x1 = 510 + 372 * math.cos(angle)
        y1 = 510 + 372 * math.sin(angle)
        x2 = 510 + (416 + (i % 4) * 7) * math.cos(angle + .035)
        y2 = 510 + (416 + (i % 4) * 7) * math.sin(angle + .035)
        d.line((x1, y1, x2, y2),
               fill=(240, 39, 123, 175) if i % 3 == 0 else (165, 63, 254, 155),
               width=8)
    d.line([(350, 777), (350, 271), (581, 271), (662, 289),
            (722, 362), (722, 473), (665, 558), (551, 585), (350, 585)],
           fill=(240, 27, 99, 95), width=119, joint="curve")
    d.line([(340, 784), (340, 258), (566, 258), (657, 277),
            (727, 357), (734, 466), (663, 557), (533, 584), (340, 584)],
           fill=(107, 22, 170, 255), width=108, joint="curve")
    d.line([(344, 773), (344, 264), (567, 264), (658, 287),
            (717, 361), (720, 455), (656, 547), (528, 566), (344, 566)],
           fill=(20, 13, 39, 255), width=82, joint="curve")
    d.polygon([(296, 236), (388, 236), (388, 700), (302, 815)],
              fill=(31, 16, 51, 255))
    d.line((318, 788, 318, 262), fill=(210, 70, 250, 255), width=13)
    d.line([(348, 250), (584, 250), (672, 292)],
           fill=(238, 84, 253, 245), width=13, joint="curve")
    d.arc((536, 280, 765, 560), 290, 80, fill=(241, 42, 105, 245), width=16)
    d.line((369, 588, 559, 588), fill=(221, 75, 245, 230), width=15)
    d.polygon([(488, 341), (450, 482), (515, 472),
               (397, 716), (454, 542), (399, 540)], fill=(244, 37, 96, 240))
    d.line([(489, 340), (450, 480), (512, 473), (397, 717)],
           fill=(255, 137, 176, 230), width=6)
    d.rounded_rectangle((539, 373, 651, 437), radius=30,
                        fill=(22, 11, 57, 255), outline=(196, 90, 253, 255), width=8)
    d.rounded_rectangle((555, 384, 636, 415), radius=16, fill=(161, 99, 254, 245))
    d.line((562, 390, 605, 389), fill=(245, 212, 255, 220), width=8)
    for x, y, r in [(171, 210, 6), (828, 184, 7), (205, 811, 6),
                    (828, 740, 8), (746, 830, 5), (188, 493, 4)]:
        d.ellipse((x-r, y-r, x+r, y+r), fill=(193, 96, 255, 205))
    return base.convert("RGB")

def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--output", default="dist/branding/paradox.ico")
    args = p.parse_args()
    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    paint().save(out, format="ICO", sizes=[
        (16, 16), (24, 24), (32, 32), (48, 48),
        (64, 64), (128, 128), (256, 256)])
    print(f"PARADOX icon: {out} ({out.stat().st_size} bytes)")

if __name__ == "__main__":
    main()
