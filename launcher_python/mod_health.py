"""Read-only installation and startup diagnostics for PARADOX.

Copying files is not proof that an IL2CPP mod actually loaded. Keep installation,
BepInEx bootstrap and PARADOX runtime health as separate states.
"""
from __future__ import annotations

from dataclasses import dataclass
import hashlib
from pathlib import Path
import re


REQUIRED = (
    "winhttp.dll",
    "doorstop_config.ini",
    "BepInEx/core/BepInEx.Unity.IL2CPP.dll",
    "BepInEx/plugins/Reactor.dll",
    "BepInEx/plugins/Paradox.dll",
    "dotnet/coreclr.dll",
)
LOG_NAMES = ("BepInEx/LogOutput.log", "BepInEx/LogOutput.txt")
LOADED_MARKER = "PARADOX loaded. Reactor RPC transport registered"


@dataclass(frozen=True)
class InstallHealth:
    state: str  # broken, pending, loaded, failed
    summary: str
    details: tuple[str, ...]
    log: Path | None = None


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _doorstop_errors(path: Path) -> list[str]:
    try:
        content = path.read_text(encoding="utf-8-sig", errors="replace")
    except OSError as exc:
        return [f"Nie można odczytać doorstop_config.ini: {exc}"]

    # Ignore comments and verify only the active [General] section.
    active = ""
    values: dict[str, str] = {}
    for raw in content.splitlines():
        line = raw.strip()
        if not line or line.startswith(("#", ";")):
            continue
        if line.startswith("[") and line.endswith("]"):
            active = line[1:-1].casefold().strip()
            continue
        if "=" in line and active == "general":
            key, value = line.split("=", 1)
            values[key.strip().casefold()] = value.split("#", 1)[0].split(";", 1)[0].strip()

    errors = []
    if values.get("enabled", "").casefold() != "true":
        errors.append("Doorstop jest wyłączony (enabled != true).")
    assembly = values.get("target_assembly", "").replace("/", "\\").casefold()
    if assembly != r"bepinex\core\bepinex.unity.il2cpp.dll":
        errors.append("Doorstop wskazuje nieprawidłową bibliotekę BepInEx IL2CPP.")
    return errors


def inspect_install(game: Path, expected_mod: Path | None = None,
                    installed_after: float | None = None) -> InstallHealth:
    """Check actual disk files and the latest BepInEx startup log.

    A fresh load is accepted only if its log was written after the specified
    installation attempt. The marker verifies plugin startup, not multiplayer.
    """
    game = Path(game)
    missing = [name for name in REQUIRED if not (game / name).is_file()]
    issues = [f"Brakuje pliku: {name}" for name in missing]
    if (game / "doorstop_config.ini").is_file():
        issues.extend(_doorstop_errors(game / "doorstop_config.ini"))

    dll = game / "BepInEx/plugins/Paradox.dll"
    if expected_mod is not None and dll.is_file() and expected_mod.is_file():
        try:
            if _sha256(dll) != _sha256(expected_mod):
                issues.append("Paradox.dll różni się od wersji w tym launcherze.")
        except OSError as exc:
            issues.append(f"Nie można zweryfikować Paradox.dll: {exc}")

    if issues:
        return InstallHealth("broken", "Instalacja jest niekompletna lub nieprawidłowa.",
                             tuple(issues + ["Uruchom instalację naprawczą PARADOX."]))

    found: list[Path] = []
    for name in LOG_NAMES:
        path = game / name
        if path.is_file():
            found.append(path)
    if not found:
        return InstallHealth("pending", "Pliki są na miejscu; nie wykryto startu BepInEx.",
                             ("Uruchom Among Us przez launcher, zamknij grę i kliknij SPRAWDŹ MODY.",
                              "Brak logu oznacza, że BepInEx jeszcze nie wystartował lub nie został wstrzyknięty."))
    try:
        log = max(found, key=lambda p: p.stat().st_mtime)
        timestamp = log.stat().st_mtime
        if installed_after is not None and timestamp < installed_after - 2:
            return InstallHealth("pending", "Log pochodzi sprzed tej instalacji.",
                                 ("Uruchom Among Us ponownie po zainstalowaniu PARADOX.",), log)
        with log.open("rb") as handle:
            handle.seek(max(0, log.stat().st_size - 1024 * 1024))
            content = handle.read().decode("utf-8-sig", errors="replace")
    except OSError as exc:
        return InstallHealth("failed", "Nie można odczytać logu BepInEx.",
                             (str(exc),), None)

    if LOADED_MARKER.casefold() in content.casefold():
        return InstallHealth("loaded", "PARADOX został załadowany przez BepInEx.",
                             ("Potwierdzony start pluginu. Działanie ról i multiplayer wymaga osobnych testów.",), log)

    if (re.search(r"PARADOX Harmony patch registration failed", content, re.I)
            or re.search(r"(?:error|exception|failed).*?(?:PARADOX|Reactor)", content, re.I)):
        lines = [line.strip()[:350] for line in content.splitlines()
                 if re.search(r"(?:error|exception|failed).*?(?:PARADOX|Reactor)", line, re.I)]
        return InstallHealth("failed", "BepInEx zgłosił błąd moda lub Reactora.",
                             tuple(lines[-4:] or ["Sprawdź szczegóły w LogOutput."]), log)

    if "PARADOX v" in content:
        return InstallHealth("failed", "PARADOX rozpoczął start, lecz nie potwierdził załadowania.",
                             ("Możliwy błąd Harmony, zgodności gry albo zależności.",), log)
    if "BepInEx" in content:
        return InstallHealth("failed", "BepInEx wystartował, ale PARADOX nie załadował się.",
                             ("Sprawdź błędy ładowania pluginów i Reactor w LogOutput.",), log)
    return InstallHealth("failed", "Log istnieje, ale brak śladów uruchomienia BepInEx.",
                         ("Sprawdź antywirusa, Doorstop i zgodność wersji gry.",), log)


def diagnosis_text(result: InstallHealth) -> str:
    lines = [result.summary, "", *result.details]
    if result.log is not None:
        lines += ["", f"Log: {result.log}"]
    return "\n".join(lines)
