# PARADOX STATION — Unity artwork workspace

The original twelve-room art pipeline consists of **Arrival Bay** (manually
authored SVG sources) and eleven distinct generated room concepts. Source SVG
content is procedural but each room has its own gameplay prop language
(reactor/pods/telescope/bunks/radar/medical etc.), not a generic uniform cell.

1. Open a Unity **2D** project with \`unity/ParadoxStation/Assets\` as the \`Assets\`
   content. Choose the final game's compatible Unity editor version before
   attempting to build any Among Us-compatible asset bundle.
2. Run \`python -m pip install -r tools/requirements-art.txt\` at repo root.
3. Run \`python tools/export_station_art.py\`, which populates
   \`Assets/Art/StationRooms/<room_id>/\` with 48 PNG art layers, 12 room
   preview composites and a sheet showing all rooms.
4. In Unity use **PARADOX → Build All 12 Room Art Previews** to create
   twelve editor prefabs and the \`Station_12RoomGallery.unity\` scene.
   This scene exists for reviewing visuals and coarse collision bounds only.

Original standalone Arrival Bay tool remains available from its older menu.

### Status
- [x] Concept art sources for twelve distinct rooms
- [x] PNG export and static output checks in GitHub Actions
- [x] Unity editor script to author twelve independent art prefabs
- [ ] Verify visual quality, scale and collisions inside the actual Unity editor
- [ ] Native among-us compatible ShipStatus asset / map selection integration
- [ ] Actual tasks, vents, sabotage, visibility systems and multiplayer QA

**Unity art prefabs alone do not make a playable custom Among Us level.**
