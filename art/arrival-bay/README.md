# Arrival Bay — original layered art source

This is a **first art-quality milestone**, not a finished custom Among Us level.
Artwork is original vector work for PARADOX STATION, not ripped from The Skeld or Submerged.

## Layer stack (1920 × 1080)

1. `01_floor.svg` — layered deck plates, docking glyph, scuffs, rivets and painted route lines
2. `02_walls.svg` — articulated station bulkheads, lights and airlock door detailing
3. `03_props.svg` — cargo crates, security console, manifest terminal and modular hardware
4. `04_lights.svg` — soft teal/amber illumination and localized emissive highlights

Unlike the old colored-tile prototype, these are **authored details with their own
stylistic hierarchy**. They still need gameplay polish, a real prefabricated door,
walk/vent/task components, and testing in the actual Among Us runtime.

To build the layered PNG sprites:

```sh
python -m pip install -r tools/requirements-art.txt
python tools/export_arrival_bay.py
```

The PNG output defaults to `unity/ParadoxStation/Assets/Art/ArrivalBay/`.

Open a Unity 2D project using the `unity/ParadoxStation/Assets` folder,
then select **PARADOX → Build Arrival Bay Art Preview**. The editor builds
`Assets/Generated/ArrivalBay/ArrivalBay_Art.prefab` and
`ArrivalBay_ArtPreview.unity`, including the first collision envelope.

**Important:** This standalone preview is not wired to `ShipStatus`, Among Us
task prefabs or map choice. The old 12-room station remains a development
prototype until real custom-map integration and multiplayer QA are completed.
