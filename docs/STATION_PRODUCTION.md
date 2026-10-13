# PARADOX STATION — production map integration

**Production target:** a selectable Among Us custom map comparable in scope to established
custom maps. No F6/F7/F8/F9 prototype, no remote map teleport, and no public release
until the native map is integrated and tested.

## What has been implemented in this production branch (0.7.0-dev.1)

- One authoritative 12-room/16-corridor/3-vent **logical map definition**
  in `src/Paradox/Maps/ParadoxStation.cs`.
- `tools/build_station_production_layout.py` builds a deterministic physical
  floor plan from that existing manifest using A* routes that avoid crossing
  unrelated rooms. All twelve centers must be reachable through actual
  connected corridor tiles. The script merges exposed perimeter tile edges into
  boundary segments and emits `StationProductionLayout.json`.
- `unity/ParadoxStation/Assets/Editor/ParadoxStationProductionSceneBuilder.cs`
  imports the twelve original four-layer room artworks, lays out the rooms
  using real station coordinates, creates visual corridor segments, adds the
  generated perimeter **BoxCollider2D** walls, and creates task/spawn/vent
  attachment-point objects. Unity's editor can save the new scene and build
  an asset bundle for Windows.
- New `Verify PARADOX Production Map Geometry` GitHub Actions workflow
  generates/checks the layout and uploads the JSON for Unity.
- Old off-map F7 prototype entry, F6 blueprint and TESTUJ MAPĘ button are
  disabled in normal builds. This is intentional: an incomplete map should
  not be mistaken for a selectable production map.

## Build the Unity scene

1. Install the Unity Editor that matches the Among Us target. The earlier
   SubmergedUnity open-source project pins `2022.3.62f2`, but that is **a
   reference for Submerged**, not proof that the latest Among Us uses it.
   Determine the exact asset serialisation compatibility before distribution.
2. From the repository root:
   ```
   python -m pip install -r tools/requirements-art.txt
   python tools/export_station_art.py
   python tools/build_station_production_layout.py
   ```
3. Open `unity/ParadoxStation` in the matching Unity Editor. In the menu,
   select **PARADOX > Production > Build Connected Station Scene**.
4. Inspect the connected scene at
   `Assets/Generated/StationProduction/ParadoxStation_ProductionLayout.unity`.
   You can also select **Build Windows Scene AssetBundle** to create a
   standalone Unity scene bundle **for integration testing only**.
5. This Unity-generated scene is not yet an Among Us `ShipStatus` prefab and
   **cannot be launched as a native map** until the game-specific adapter is
   implemented. Do not package it as a finished map.

## Release blockers (all required before announcing a playable map)

- [ ] Select and verify the precise Among Us PC build, Unity asset pipeline,
      32/64-bit IL2CPP runtime and compatible Reactor/BepInEx dependencies.
- [ ] Implement and runtime-check a custom **ShipStatus** scene/prefab adapter,
      load bundle safely, and register a non-conflicting custom map identifier.
- [ ] Add PARADOX STATION to the genuine create-room/map-choice UI and
      synchronize selection/handshakes (not just an F7 button).
- [ ] Integrate native player spawn, camera, kill/report/range checks,
      meeting/emergency/exile and winning flows.
- [ ] Attach and validate 12 task minigames with actual task assignments,
      UI, completion RPC and vanilla victory counters.
- [ ] Implement functional vents and doors with host-trusted authorization.
- [ ] Add native sabotage system and emergency/repair logic.
- [ ] Complete final occlusion, masking, sorting and environmental art polish.
- [ ] Test collisions/vent routes/physics in Unity Editor and **at least
      two real synchronized Among Us clients**.
- [ ] Build reproducible installer and report runtime results; then decide
      release readiness (do not infer readiness from a green .NET compile).

## Related references

Submerged demonstrates a separate `SubmergedUnity` project, a resource bundle
and `MapSelectButtonPatches`, unlike a mere image-overlay. Respect that project's
licence and do not copy its art/assets or proprietary behaviour into PARADOX.
Reference code: https://github.com/SubmergedAmongUs/Submerged

This document intentionally distinguishes authored geometry and a true runnable
Among Us map. The latter has **not been implemented** as of this milestone.
