#!/usr/bin/env python3
"""Render the original layered Arrival Bay SVG artwork to Unity-ready PNG sprites.

pip install -r tools/requirements-art.txt
python tools/export_arrival_bay.py --output unity/ParadoxStation/Assets/Art/ArrivalBay
"""
from __future__ import annotations

import argparse
from pathlib import Path

import cairosvg
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SOURCES = ROOT / "art" / "arrival-bay"
LAYERS = ("01_floor", "02_walls", "03_props", "04_lights")
SIZE = (1920, 1080)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        type=Path,
        default=ROOT / "unity" / "ParadoxStation" / "Assets" / "Art" / "ArrivalBay",
    )
    args = parser.parse_args()
    target = args.output.resolve()
    target.mkdir(parents=True, exist_ok=True)

    composite = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    for layer in LAYERS:
        source = SOURCES / (layer + ".svg")
        output = target / (layer + ".png")
        cairosvg.svg2png(
            url=str(source),
            write_to=str(output),
            output_width=SIZE[0],
            output_height=SIZE[1],
        )

        with Image.open(output) as image:
            if image.size != SIZE or image.mode != "RGBA":
                raise RuntimeError(f"Bad export: {output}: {image.size}/{image.mode}")
            if image.getbbox() is None:
                raise RuntimeError(f"Empty art layer: {output}")
            composite.alpha_composite(image.convert("RGBA"))

        print(f"Exported {output.relative_to(target.parent)}")

    preview = target / "ArrivalBay_CompositePreview.png"
    composite.save(preview, optimize=True)
    print(f"PREVIEW {preview}")
    print("Arrival Bay art export passed: 1920x1080; 4 opaque/translucent sprite layers.")


if __name__ == "__main__":
    main()
