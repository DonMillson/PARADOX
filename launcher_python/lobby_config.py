"""Configure 15/20/30/40/50-player test lobbies without hand-editing BepInEx cfg.

Preserves the user's other preferences and existing sections. Called only after
the mod installer succeeded. Always disabled unless explicitly enabled in GUI.
"""
from __future__ import annotations

import os
from pathlib import Path
import re
import shutil
import tempfile
from datetime import datetime, timezone

ALLOWED_CAPACITIES = (15, 20, 30, 40, 50)
SECTION = "ExperimentalLargeLobby"


def render_lobby_config(existing: str, enabled: bool, max_players: int) -> str:
    if type(enabled) is not bool or max_players not in ALLOWED_CAPACITIES:
        raise ValueError("Invalid 50-player test settings.")
    linebreak = "\r\n" if "\r\n" in existing else "\n"
    content = existing
    sections = list(re.finditer(r"(?m)^\[([^\]\r\n]+)\][ \t]*\r?$", content))
    current = next((i for i, section in enumerate(sections)
                    if section.group(1).strip().casefold() == SECTION.casefold()), -1)
    values = {"Enable": "true" if enabled else "false", "MaxPlayers": str(max_players)}

    if current == -1:
        if content and not content.endswith(("\n", "\r")):
            content += linebreak
        if content:
            content += linebreak
        content += f"[{SECTION}]{linebreak}"
        for key, val in values.items():
            content += f"{key} = {val}{linebreak}"
        return content

    section = sections[current]
    first = section.end()
    end = sections[current + 1].start() if current + 1 < len(sections) else len(content)
    portion = content[first:end]
    for key, val in values.items():
        pattern = re.compile(rf"(?mi)^[ \t]*{re.escape(key)}[ \t]*=[^\r\n]*(?:\r?\n)?")
        found = pattern.search(portion)
        if found:
            suffix = linebreak if found.group(0).endswith(("\r", "\n")) else ""
            portion = portion[:found.start()] + f"{key} = {val}{suffix}" + portion[found.end():]
        else:
            portion += ("" if not portion or portion.endswith(("\r", "\n")) else linebreak)
            portion += f"{key} = {val}{linebreak}"
    return content[:first] + portion + content[end:]


def write_lobby_config(game_dir: Path, enabled: bool, max_players: int) -> Path:
    """Atomic replacement, backup, no symlink traversal and no writes on invalid input."""
    if type(enabled) is not bool or max_players not in ALLOWED_CAPACITIES:
        raise ValueError("Invalid selected capacity.")
    game_dir = Path(game_dir)
    if not (game_dir / "Among Us.exe").is_file():
        raise ValueError("Not an Among Us directory.")
    bep = game_dir / "BepInEx"
    if not bep.is_dir() or bep.is_symlink():
        raise ValueError("BepInEx folder missing or redirected.")
    cfg_dir = bep / "config"
    if cfg_dir.is_symlink():
        raise ValueError("Config directory is a symbolic link.")
    cfg_dir.mkdir(exist_ok=True)
    config = cfg_dir / "gg.paradox.mod.cfg"
    if config.is_symlink():
        raise ValueError("PARADOX settings is a symbolic link.")
    previous = config.read_text("utf-8-sig") if config.exists() else ""
    output = render_lobby_config(previous, enabled, max_players)
    if previous == output:
        return config
    if config.exists():
        backup_root = game_dir / ".paradox-installer"
        if backup_root.is_symlink():
            raise ValueError("Untrusted backup directory.")
        backup_root.mkdir(exist_ok=True)
        backup_name = "paradox-config-" + datetime.now(timezone.utc).strftime(
            "%Y%m%dT%H%M%S%fZ") + ".bak"
        shutil.copy2(config, backup_root / backup_name)
    tmp_name = None
    try:
        with tempfile.NamedTemporaryFile(
            "w", encoding="utf-8", newline="", dir=cfg_dir,
            prefix=".paradox-cfg-", suffix=".tmp", delete=False
        ) as temp:
            tmp_name = temp.name
            temp.write(output)
            temp.flush()
            os.fsync(temp.fileno())
        os.replace(tmp_name, config)
    finally:
        if tmp_name is not None and os.path.exists(tmp_name):
            os.unlink(tmp_name)
    return config
