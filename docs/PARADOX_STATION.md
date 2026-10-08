# PARADOX STATION — map implementation and remaining integration

## 0.6.0-alpha.1: experimental walkable arena inside Among Us

This is an **opt-in, runtime-generated test scene**, not a registered or selectable custom ShipStatus map. It reuses the currently loaded vanilla match to test navigation, collision and shared console objectives. Do not ship it as the finished PARADOX STATION.

- **F6:** original bilingual 12-room blueprint (read-only).
- **F7, host only, during a live match:** build the off-map 2D station and transfer active players to Arrival Bay. Press again to return to their previous positions. All participating clients must use exactly the same PARADOX version.
- **F8:** while standing beside a yellow console, contribute one of three calibration actions for that room. The host checks actor health, range and timing, then sends progress to all clients.
- Includes deterministic room floors, navigable connected corridors, visible boundary walls with BoxCollider2D, 12 localized room labels, and 12 prototype consoles.
- Initial player relocation/return is performed by the host via NetTransform.SnapTo. Runtime mode and task progress are carried by host-validated Reactor RPCs 44–46 (protocol v41). The scene is destroyed at disconnect/endgame, and a meeting causes the host to exit the test scene.
- The scene is built only on demand: regular Among Us matches and map selection are unchanged until the host explicitly presses F7.

## Known alpha limitations

- This test scene is generated off the active vanilla map and **does not replace ShipStatus**; the vanilla map remains loaded.
- The yellow consoles implement synchronized *experimental objectives*, not official Among Us tasks, task-bar or task victories.
- The three vent links remain visual plans (blueprint only): in-game vent entrances, impostor-only vent travel and room doors are not implemented.
- Visuals are intentionally procedural flat-color placeholders; original floor/wall/prop art and occlusion are pending.
- Room geometry relies on runtime static 2D wall colliders. Its collision layer and camera follow need verification on the exact Among Us runtime; build success cannot confirm physics.
- Meeting, death, emergency button, respawn, sabotage and kill range behaviour inside the station **must** be tested on actual host + clients; vanilla systems may assume vanilla coordinates.
- No independent map option exists in lobby. A true selectable custom map requires Unity prefab/asset-bundle integration, ShipStatus/map registration, task, vent, sabotage, spawn and HUD map hooks, plus multiplayer QA.

## Manual test protocol

1. Install matching PARADOX 0.6.0-alpha.1 builds with BepInEx and Reactor. Capture `BepInEx/LogOutput.log`.
2. Host + two clients: start a vanilla match. Host presses **F7** after players can move.
3. Verify all players appear in Arrival Bay and camera follows. Check walking between the 12 connected rooms. Try to walk through every perimeter wall; there should be no holes or invisible barriers.
4. Stand next to a yellow console and press **F8** three times with at least 0.75 s between presses. Check that all clients see identical progress messages. Repeat for all 12 consoles.
5. Host presses F7 again; players should return. Repeat, then trigger a meeting while the scene is active and verify return and no stuck players.
6. Disable the mod on one client and verify host cannot activate with incompatible participants. Test disconnects, lobby transitions and endgame cleanup.
7. If any step fails, record the Among Us game build, PARADOX DLL commit, client logs and a short reproduction.

## Road to a real map

1. Build a dedicated Unity map project with original station prefab art, sorting layers, rooms, collision meshes and door/vent prefabs.
2. Integrate a real ShipStatus-derived custom-map asset and register it in Among Us map selection with client compatibility checks.
3. Replace prototype F8 consoles with 12 actual mini-games and complete task assignment / authoritative task completion / victory integration.
4. Add host-validated ventilation, room doors, saboteur actions and station sabotage scenarios.
5. Add station-specific minimap, admin panel/map camera, dead body / meeting / respawn and impostor systems.
6. Multiplayer stability testing and graphics pass; only then mark PARADOX STATION playable/release-ready.
