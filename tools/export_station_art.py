#!/usr/bin/env python3
"""Render all twelve PARADOX STATION art concepts into editable Unity PNG layers.

A successful run tests ART SOURCE / RASTER OUTPUT, not playable Among Us maps.
"""
from __future__ import annotations

import argparse
import hashlib
import io
from pathlib import Path
import subprocess
import sys
import tempfile

import cairosvg
from PIL import Image, ImageDraw

from generate_station_room_art import ROOMS

ROOT = Path(__file__).resolve().parent.parent
LAYER_NAMES = ("01_floor", "02_walls", "03_props", "04_lights")
ALL_ROOMS = [("arrival_bay", "ARRIVAL BAY")] + [(slug, title) for slug, title, *_ in ROOMS]
SIZE = (1920, 1080)


def render_one(slug: str, title: str, source: Path, target: Path) -> Image.Image:
    target.mkdir(parents=True, exist_ok=True)
    composite = Image.new("RGBA", SIZE, (0, 0, 0, 0))

    for layer in LAYER_NAMES:
        file = source / f"{layer}.svg"
        if not file.is_file():
            raise RuntimeError(f"Missing authored artwork: {file}")
        png = target / f"{layer}.png"
        cairosvg.svg2png(
            url=str(file), write_to=str(png),
            output_width=SIZE[0], output_height=SIZE[1],
        )
        with Image.open(png) as image:
            if image.mode != "RGBA" or image.size != SIZE:
                raise RuntimeError(f"Unexpected PNG format: {png}")
            bounds = image.getbbox()
            if bounds is None:
                raise RuntimeError(f"Empty room layer: {png}")
            composite.alpha_composite(image)

    if not composite.getbbox():
        raise RuntimeError(f"Room contains no art: {slug}")

    with (target / f"{slug}_preview.png").open("wb") as output:
        composite.save(output, format="PNG", optimize=True)
    return composite


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output", type=Path,
        default=ROOT / "unity" / "ParadoxStation" / "Assets" / "Art" / "StationRooms",
    )
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)

    assert len(ALL_ROOMS) == 12 and len(set(name for name, _ in ALL_ROOMS)) == 12

    # Each generated SVG is deterministic, original source art. Arrival Bay
    # intentionally uses its separately authored four SVG layers.
    with tempfile.TemporaryDirectory(prefix="paradox-room-src-") as temp:
        generated = Path(temp)
        subprocess.run(
            [sys.executable, str(ROOT / "tools" / "generate_station_room_art.py"),
             "--output", str(generated)],
            check=True,
        )

        sheet = Image.new("RGB", (1920, 996), "#081520")
        draw = ImageDraw.Draw(sheet)
        signatures = set()

        for idx, (slug, title) in enumerate(ALL_ROOMS):
            source = (
                ROOT / "art" / "arrival-bay"
                if slug == "arrival_bay" else generated / slug
            )
            room = render_one(slug, title, source, output / slug)
            thumb = room.convert("RGBA")
            bg = Image.new("RGBA", SIZE, "#07131fff")
            bg.alpha_composite(thumb)
            thumb = bg.convert("RGB").resize((452, 254), Image.Resampling.LANCZOS)

            signature = hashlib.sha256(thumb.tobytes()).hexdigest()
            if signature in signatures:
                raise RuntimeError(f"Duplicate station room artwork: {slug}")
            signatures.add(signature)

            col, row = idx % 4, idx // 4
            x, y = 14 + col * 477, 12 + row * 331
            sheet.paste(thumb, (x, y + 26))
            draw.rectangle((x - 2, y + 24, x + 453, y + 281),
                           outline="#67aab4", width=2)
            draw.text((x + 4, y + 4), f"{idx+1:02d}  {title}", fill="#e5f4f5")
            draw.text((x + 4, y + 287), slug, fill="#8dabad")
            print(f"OK  {slug:<19} four 1920x1080 sprite layers")

        output.joinpath("Station_AllRooms_ContactSheet.png").parent.mkdir(
            parents=True, exist_ok=True)
        sheet.save(output / "Station_AllRooms_ContactSheet.png", optimize=True)
        print("PASS: 12 distinct room previews, 48 nonempty art layers, contact sheet ready.")


if __name__ == "__main__":
    main()
