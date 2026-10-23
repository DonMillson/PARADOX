"""Steam library discovery and launching for PARADOX Companion.

Does not emulate Steam ownership or call private APIs. Merely finds a real,
locally installed Among Us and asks Steam to launch app 945360.
"""
from __future__ import annotations

import os
from pathlib import Path
import re
import subprocess

STEAM_APP_ID = 945360
APP_MANIFEST = "appmanifest_945360.acf"


def valid_among_us_game(path: Path) -> bool:
    return (path / "Among Us.exe").is_file() and (
        path / "GameAssembly.dll").is_file() and (
        path / "Among Us_Data").is_dir()


def parse_steam_vdf_string(document: str, key: str) -> str:
    """Minimal VDF value query; accepts escaped backslashes and LF/CRLF."""
    match = re.search(rf'(?im)^\s*"{re.escape(key)}"\s+"((?:\\.|[^"\\])*)"', document)
    if not match:
        return ""
    return match.group(1).replace("\\\\", "\\")


def _steam_installations() -> list[Path]:
    roots = []
    for name in ("ProgramFiles(x86)", "ProgramFiles"):
        base = os.environ.get(name)
        if base:
            roots.append(Path(base) / "Steam")
    if os.name == "nt":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam") as key:
                root, _ = winreg.QueryValueEx(key, "SteamPath")
                roots.insert(0, Path(root))
        except OSError:
            pass
    for drive in "CDEFGHIJKLMNOPQRSTUVWXYZ":
        roots += [Path(f"{drive}:/Steam"), Path(f"{drive}:/SteamLibrary")]
    return roots


def steam_library_roots() -> tuple[Path, ...]:
    discovered: list[Path] = []
    seen: set[str] = set()
    for root in _steam_installations():
        libraries = [root]
        vdf = root / "steamapps" / "libraryfolders.vdf"
        try:
            raw = vdf.read_text("utf-8-sig", errors="replace")
            libraries.extend(
                Path(p.replace("\\\\", "\\"))
                for p in re.findall(r'(?im)^\s*"path"\s+"([^"]+)"', raw)
            )
        except OSError:
            pass
        for library in libraries:
            key = str(library).casefold().rstrip("\\/")
            if key not in seen:
                seen.add(key)
                discovered.append(library)
    return tuple(discovered)


def discover_owned_installation(libraries: tuple[Path, ...] | None = None) -> Path | None:
    """A Steam app manifest confirms a local installation, not a license claim."""
    for library in libraries if libraries is not None else steam_library_roots():
        manifest = library / "steamapps" / APP_MANIFEST
        try:
            content = manifest.read_text("utf-8-sig", errors="replace")
        except OSError:
            continue
        if parse_steam_vdf_string(content, "appid") != str(STEAM_APP_ID):
            continue
        folder = parse_steam_vdf_string(content, "installdir") or "Among Us"
        if not folder or folder in (".", "..") or "/" in folder or "\\" in folder:
            continue
        game = library / "steamapps" / "common" / folder
        if valid_among_us_game(game):
            return game
    return None


def launch_among_us(game_dir: Path, use_steam: bool = True) -> str:
    if not valid_among_us_game(game_dir):
        raise FileNotFoundError("Brak plików Among Us w wskazanym folderze.")
    if use_steam:
        if os.name != "nt":
            raise OSError("URI Steam można uruchomić tylko na Windows.")
        os.startfile(f"steam://rungameid/{STEAM_APP_ID}")  # type: ignore[attr-defined]
        return "Steam"
    subprocess.Popen([str(game_dir / "Among Us.exe")], cwd=str(game_dir))
    return "Direct"
