# PARADOX installation UI — spaceship / anomaly style

The installation screen in the online Ship Launcher and Steam Companion Hub
now uses a reusable \`ParadoxInstallProgress\` visual component in
\`launcher_python/paradox_install_ui.py\`.

- Draws a real live neon purple-to-red progress meter and percentage.
- Existing worker queues drive all transitions; no fake 67% progress.
- Displays dynamic download, install and verification phases.
- Updates are applied on the Tk UI thread, as before.
- Resize-safe for Windows DPI settings and independent of downloaded artwork.
- The sci-fi rift banner in \`paradox_visuals.header\` is shared by both apps.
- Scrolling pages keep install controls, diagnostics and errors accessible.

The visual style follows the concept mockup. It is an implemented Tk/CustomTkinter
interpretation, not an exact pixel-for-pixel reproduction of a flat preview PNG.

Builds:
\`python -m unittest discover -s launcher_python -p "test_*.py"\`;
GitHub Windows workflows \`python-launcher.yml\` and \`steam-hub.yml\` run on
pushes to the default branch.
