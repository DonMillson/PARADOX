# PARADOX STATION — 12 room art collection

**Art milestone, NOT a playable map.** Arrival Bay is separately authored in
\`art/arrival-bay/\`. The other eleven are individually illustrated in
\`tools/generate_station_room_art.py\`, one unique set of devices/structures per
room. Each exports to four separately editable transparent 1920 × 1080
sprite layers: floor, structural walls, machinery/props, illumination.

| Room | Distinguishing art |
|---|---|
| Arrival Bay | Cargo manifest, airlock, landing emblem |
| Cargo Hold | Container racks, crates, freight scanner |
| Crew Quarters | Sleeping bunks, central lounge table |
| Security | Ten monitor surveillance wall, mainframe |
| Communications | Dish/radar uplink, control screens |
| Observation Dome | Starfield viewport and telescope |
| Temporal Lab | Chronal coil array and oscillator |
| Medical Bay | Four medical beds and triage scanner |
| Containment | Four secured isolation pods |
| Rift Reactor | Energy torus, containment conduits |
| Power Core | Fusion capacitors and energy grid |
| Void Chamber | Anomaly well with gravity control |

To build all twelve room sprites and a contact sheet:

\`\`\`sh
python -m pip install -r tools/requirements-art.txt
python tools/export_station_art.py
\`\`\`

This writes assets under:
\`unity/ParadoxStation/Assets/Art/StationRooms/<room_id>/\`.

In Unity, run **PARADOX → Build All 12 Room Art Previews**. This builds
twelve distinct editor prefabs and one review-gallery scene.

CI workflow **Build PARADOX 12 Room Art** produces a ZIP containing
48 PNG sprite layers, twelve room composites, plus a contact sheet.
Export validation checks resolution, nonempty layers and no duplicate
room composites.

These are **graphics prototypes** requiring concept-art review and manual polish.
Actual Among Us gameplay requires a compatible Unity asset bundle, new
ShipStatus/map registration, correctly aligned room interiors/corridors,
authored colliders/doors, tasks/vents/sabotage and multiplayer verification.
