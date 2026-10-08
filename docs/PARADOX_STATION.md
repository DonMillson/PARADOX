# PARADOX STATION — integration milestones

This file distinguishes blueprint visualization from an actual Among Us map.

## Implemented in 0.5.0-beta.4

- PARADOX's 12-room, 16-corridor, 3-vent, 12-task-ID layout is the single source of truth.
- Press **F6** in the game to open/close a bilingual in-game map blueprint with room names, corridor segments, dashed vent links, designated starting room and planned-task markers.
- The viewer is deliberately a passive overlay: it does **not** display the player's real location, act as a game map, intercept game buttons, or teleport anyone.
- The preview hides for meetings, and cleans up on game end / network disconnect.
- A PowerShell CI check verifies room/task/coordinate uniqueness, link endpoints, one spawn, corridor connectivity and the viewer's references to the same layout manifest.

## NOT YET IMPLEMENTED — required before playable map claims

1. A dedicated Unity **ShipStatus** scene/map registration and multiplayer map selection, with required prefabs, cameras and environment systems.
2. Unique station floor/prop art, traversable corridor geometry, physical collisions, occlusion and door behaviour.
3. Functional vent entrances, vent indices/transport and impostor-only permissions.
4. Task object prefabs and task assignment/completion/network sync for the 12 planned task IDs.
5. Sabotages, emergency button, meetings, spawn/respawn, player camera and official map/minimap hooks.
6. Full multi-client tests on the exact Among Us/Reactor/BepInEx target version.

Keep the experimental blueprint separate from vanilla MapId. Never advertise the 12-room plan or F6 viewer as a fully playable custom level.

## Manual QA

- Run with matching PARADOX DLL on each PC client; join a lobby and press F6.
- Verify 12 room names, green spawn room, 16 cyan corridor links, 3 orange dashed vent links and 12 yellow planned-task squares.
- Switch EN/PL before reopening to confirm localized room names.
- Toggle F6 repeatedly in lobby and game; open a meeting while visible, confirm hidden, then close the meeting.
- Disconnect and start a new lobby; viewer must not persist as a ghost overlay.
- Watch `BepInEx/LogOutput.log` for runtime TMP/font or Unity errors. A green Actions build only proves compilation and the static manifest check.
