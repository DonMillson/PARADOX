# PARADOX / Installer artwork — 1:1 illustrated shell

The illustrated installation overlay uses the **approved 1536×864 reference image** supplied by the project owner, stored in compressed WebP form at:
`launcher_python/assets/paradox_installer_reference.webp`.

Both Windows executables (Ship Launcher and Steam Companion Hub) package the actual image. Once installation is confirmed, the overlay opens automatically. The pre-existing Python workers and queue callbacks update:
- the live percentage, which replaces the mockup's static “67%”;
- the status and secondary verification text;
- the active stage in the left rail.

Closing the overlay does **not** cancel an already-running install; the underlying program continues it and reports errors.

The main launcher / hub still use their existing controls before installation (game directory, consent, diagnostics). The illustrated overlay covers the live installation phase only. **The offline Inno Setup wizard is a separate executable and has not been redesigned to precisely match this image.**

The static art is not generated at runtime and doesn't use any external URL. It is bundled into the EXE via PyInstaller --add-data. Requires Pillow.
