"""PARADOX Windows installer core: discovery, guarded file installation and undo."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import struct
import sys
import time
from typing import Iterator

APP_VERSION = "0.7.0-dev.4"
MANIFEST_DIR = Path("BepInEx") / ".paradox-installer"
MANIFEST_PATH = MANIFEST_DIR / "manifest.json"
NO_GAME_MODIFICATION = "SYMULACJA — nie skopiowano żadnych plików."

try:
    import winreg
except ImportError:
    winreg = None


def valid_game(folder: str | Path, require_x64: bool = True) -> bool:
    game = Path(folder)
    if not (game.joinpath("Among Us.exe").is_file()
            and game.joinpath("GameAssembly.dll").is_file()
            and game.joinpath("Among Us_Data").is_dir()):
        return False
    if not require_x64:
        return True
    try:
        with game.joinpath("Among Us.exe").open("rb") as stream:
            if stream.read(2) != b"MZ":
                return False
            stream.seek(0x3C)
            offset = struct.unpack("<I", stream.read(4))[0]
            stream.seek(offset)
            return stream.read(4) == b"PE\0\0" and struct.unpack(
                "<H", stream.read(2))[0] == 0x8664
    except (OSError, ValueError, struct.error):
        return False


def _registry_values() -> Iterator[Path]:
    if winreg is None:
        return
    for root, key, value in (
        (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360", "InstallLocation"),
        (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Valve\Steam", "SteamPath"),
        (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
    ):
        try:
            with winreg.OpenKey(root, key) as entry:
                path, _ = winreg.QueryValueEx(entry, value)
            if path:
                yield Path(path)
        except OSError:
            pass


def _steam_libraries(steam: Path) -> Iterator[Path]:
    yield steam
    manifest = steam / "steamapps" / "libraryfolders.vdf"
    try:
        text = manifest.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeError):
        return
    for path in re.findall(r'"path"\s*"((?:\\.|[^"\\])*)"', text, re.I):
        # Steam VDF escapes Windows backslashes.
        yield Path(path.replace("\\\\", "\\"))


def find_among_us() -> Path | None:
    roots = list(_registry_values())
    for env_name in ("PROGRAMFILES(X86)", "PROGRAMFILES"):
        if os.environ.get(env_name):
            roots.append(Path(os.environ[env_name]) / "Steam")
    for drive in "CDEFGHIJ":
        roots.extend((Path(f"{drive}:/Steam"),
                      Path(f"{drive}:/SteamLibrary"),
                      Path(f"{drive}:/Games/SteamLibrary")))
    checked: set[str] = set()
    for root in roots:
        candidates = [root]
        candidates.extend(steam / "steamapps" / "common" / "Among Us"
                          for steam in _steam_libraries(root))
        for folder in candidates:
            norm = str(folder).casefold()
            if norm not in checked:
                checked.add(norm)
                if valid_game(folder):
                    return folder
    return None


def bundled_payload() -> Path | None:
    candidates = []
    if hasattr(sys, "_MEIPASS"):
        candidates.append(Path(sys._MEIPASS) / "payload")
    candidates.append(Path(__file__).resolve().parent / "payload")
    for source in candidates:
        if source.is_dir() and list_payload(source):
            return source
    return None


def _allowed(relative: Path) -> bool:
    parts = relative.parts
    if not parts or any(p in ("", ".", "..") for p in parts):
        return False
    if relative.is_absolute() or ":" in str(relative):
        return False
    head = parts[0].lower()
    if head == "bepinex":
        if len(parts) < 3:
            return False
        if parts[1].lower() == "plugins":
            return len(parts) == 3 and parts[2].lower() in ("paradox.dll", "reactor.dll")
        return parts[1].lower() in ("core", "config", "patchers")
    if head in ("dotnet", "doorstop_libs"):
        return len(parts) >= 2
    return len(parts) == 1 and head in (
        "winhttp.dll", "doorstop_config.ini", ".doorstop_version"
    )


def list_payload(source: Path) -> list[tuple[Path, Path]]:
    """Select only loader/mod files; never overwrite game executables or other mods."""
    source = Path(source)
    if not source.is_dir():
        return []
    found: list[tuple[Path, Path]] = []
    for file in source.rglob("*"):
        if not file.is_file() or file.is_symlink():
            continue
        rel = file.relative_to(source)
        if len(rel.parts) == 1 and file.name.lower() in ("paradox.dll", "reactor.dll"):
            rel = Path("BepInEx") / "plugins" / file.name
        if _allowed(rel):
            found.append((file, rel))
    return sorted(found, key=lambda item: str(item[1]).lower())


def _sha256(file: Path) -> str:
    digest = hashlib.sha256()
    with file.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _within(base: Path, path: Path) -> bool:
    try:
        path.relative_to(base)
        return True
    except ValueError:
        return False


def _safe_destination(game: Path, relative: Path) -> Path:
    destination = game / relative
    if not _within(game.resolve(), destination.resolve(strict=False)):
        raise ValueError(f"Niedozwolona ścieżka: {relative}")
    cursor = destination.parent
    while _within(game, cursor):
        if cursor.is_symlink():
            raise ValueError(f"Folder docelowy jest dowiązaniem: {cursor}")
        if cursor == game:
            break
        cursor = cursor.parent
    if destination.is_symlink():
        raise ValueError(f"Plik docelowy jest dowiązaniem: {destination}")
    return destination


def install(game: Path, source: Path) -> Iterator[int]:
    """Yields actual copy progress 0..100; raises on failure and rolls back."""
    game = Path(game).resolve()
    source = Path(source).resolve()
    if not valid_game(game):
        raise ValueError("Nie znaleziono Among Us x64 w wybranym katalogu.")
    if _within(game, source) or _within(source, game):
        raise ValueError("Folder plików moda nie może być folderem gry ani jego rodzicem.")
    files = list_payload(source)
    if not files or not any(rel.name.lower() == "paradox.dll" for _, rel in files):
        raise ValueError("Brak Paradox.dll w wybranym folderze plików moda.")
    state = game / MANIFEST_DIR
    manifest = state / "manifest.json"
    previous = {}
    if manifest.exists():
        saved = json.loads(manifest.read_text(encoding="utf-8"))
        previous = {item["path"]: item for item in saved["files"]}
    original_manifest = manifest.read_bytes() if manifest.exists() else None
    backup_root = state / "backup"
    state.mkdir(parents=True, exist_ok=True)
    # A per-attempt staging area allows rollback without destroying previous backups.
    transaction = state / "transaction"
    if transaction.exists():
        raise RuntimeError("Poprzednia instalacja została przerwana; sprawdź backup.")
    transaction.mkdir()
    changed: list[tuple[Path, Path | None]] = []
    entries: list[dict] = []
    try:
        yield 0
        for index, (item, rel) in enumerate(files, 1):
            dest = _safe_destination(game, rel)
            key = rel.as_posix()
            before = transaction / rel
            if dest.exists():
                before.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(dest, before)
            changed.append((dest, before if dest.exists() else None))
            old = previous.get(key)
            had_original = bool(old["original"]) if old else dest.exists()
            persistent = backup_root / rel
            if old is None and dest.exists():
                persistent.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(dest, persistent)
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(item, dest)
            entries.append({"path": key, "original": had_original,
                            "sha256": _sha256(dest)})
            yield int(index * 100 / len(files))
        temporary = state / "manifest.tmp"
        temporary.write_text(json.dumps({"version": APP_VERSION, "files": entries},
                                        indent=2), encoding="utf-8")
        temporary.replace(manifest)
    except Exception:
        for dest, before in reversed(changed):
            try:
                if before is not None:
                    shutil.copy2(before, dest)
                elif dest.exists():
                    dest.unlink()
            except OSError:
                pass
        if original_manifest is not None:
            manifest.write_bytes(original_manifest)
        elif manifest.exists():
            manifest.unlink()
        raise
    finally:
        shutil.rmtree(transaction, ignore_errors=True)


def uninstall(game: Path) -> tuple[int, int]:
    """Reverts only files unchanged since PARADOX installed them."""
    game = Path(game).resolve()
    state = game / MANIFEST_DIR
    manifest = state / "manifest.json"
    if not manifest.is_file():
        return 0, 0
    saved = json.loads(manifest.read_text(encoding="utf-8"))
    restored = skipped = 0
    remaining: list[dict] = []
    for record in saved["files"]:
        rel = Path(record["path"])
        if not _allowed(rel):
            skipped += 1
            remaining.append(record)
            continue
        try:
            dest = _safe_destination(game, rel)
            if not dest.exists():
                continue
            if _sha256(dest) != record["sha256"]:
                skipped += 1
                remaining.append(record)
                continue
            if record["original"]:
                backup = state / "backup" / rel
                if not backup.is_file():
                    skipped += 1
                    remaining.append(record)
                    continue
                shutil.copy2(backup, dest)
            else:
                dest.unlink()
            restored += 1
        except OSError:
            skipped += 1
            remaining.append(record)
    if remaining:
        saved["files"] = remaining
        manifest.write_text(json.dumps(saved, indent=2), encoding="utf-8")
    else:
        shutil.rmtree(state, ignore_errors=True)
    return restored, skipped


def simulation() -> Iterator[int]:
    """Explicit demo; never claims to have modified the game."""
    for value in range(101):
        time.sleep(0.016)
        yield value
