# PARADOX STATION — Unity map artwork workspace

Initial Unity Editor pipeline for **Arrival Bay**, the first room in the eventual
12-room PARADOX STATION. This is an authoring workspace, not another Among Us DLL
and not yet an independent custom `ShipStatus` scene.

1. Create/open a Unity **2D** project, keeping `Assets/Editor/ArrivalBayBuilder.cs`.
   Pin the eventual game's compatible Unity editor version before asset-bundle integration;
   do not assume any random editor release will load on the current Among Us runtime.
2. From the PARADOX repo root, run:
   `python -m pip install -r tools/requirements-art.txt`
   then `python tools/export_arrival_bay.py`.
3. Unity imports 4 transparent 1920x1080 sprites. Open the editor menu
   **PARADOX > Build Arrival Bay Art Preview**.
4. Inspect room proportions, bulkhead art, console placement and collider envelope;
   ensure first room quality before adding further rooms or official game systems.

The four source SVGs are in `art/arrival-bay/`. The art-render GitHub workflow
publishes generated PNGs and a composite preview as a CI artifact, so this
folder intentionally does not need generated PNGs committed to Git.

**Still missing:** a real custom-map asset bundle, `ShipStatus` registration,
Among Us lobby map choice, properly authored functional tasks/sabotages/vents,
room transitions and real multi-client testing. Do not call the custom map playable.
