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
    state: str  # broken, pending, loaded, partial, failed
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

    # UI exceptions occur *after* the core marker. Checking the marker first
    # used to misreport "working mod" even when the PARADOX settings tab failed.
    critical_pattern = re.compile(
        r"PARADOX PARTIAL LOAD|PARADOX PATCH FAILED|PARADOX UI ERROR|"
        r"PARADOX Harmony patch registration failed|"
        r"Failed to build PARADOX lobby settings|"
        r"PARADOX could not create lobby role card|"
        r"(?:error|exception|failed).*?(?:PARADOX|Reactor)",
        re.I,
    )
    errors = [line.strip()[:600] for line in content.splitlines()
              if critical_pattern.search(line)]
    if errors:
        return InstallHealth(
            "partial" if LOADED_MARKER.casefold() in content.casefold()
                         or "PARADOX PARTIAL LOAD" in content else "failed",
            "PARADOX ma błędy ładowania lub interfejsu — wymaga naprawy.",
            tuple(errors[-7:] + [
                "Otwórz własny pokój (host) i sprawdź zakładkę PARADOX w opcjach pokoju.",
                "Wyślij fragment LogOutput.log z widocznym błędem.",
            ]),
            log,
        )

    if LOADED_MARKER.casefold() in content.casefold():
        # Runtime loading, lobby UI creation and actual access to the custom
        # host tab are distinct milestones. Never imply that a successful core
        # startup is evidence that clicking the tab worked.
        ui_card = "PARADOX UI READY: lobby role card" in content or \
                  "PARADOX compact lobby role card built" in content
        ui_tab = "PARADOX SETTINGS TAB READY" in content
        ui_opened = "PARADOX SETTINGS OPENED" in content
        return InstallHealth(
            "loaded", "PARADOX core uruchomiony; sprawdź panel gry.",
            (
                "Plugin (BepInEx): TAK",
                "Karta ról lobby: " + ("WYKRYTO" if ui_card else "BRAK POTWIERDZENIA"),
                "Zakładka hosta: " + ("WYKRYTO" if ui_tab else "BRAK POTWIERDZENIA"),
                "Otwarcie PARADOX: " + ("WYKRYTO" if ui_opened else "BRAK POTWIERDZENIA"),
                "Ustawienia są w WŁASNYM POKOJU (host) → opcje gry → PARADOX.",
                "Awaryjnie: otwórz opcje pokoju i naciśnij F10.",
                "Brak potwierdzenia nie oznacza automatycznie błędu: mogłeś nie wejść do własnego lobby.",
                "Działanie 43 ról i multiplayer nadal wymaga osobnych testów.",
            ),
            log,
        )

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


def build_support_report(game: Path, expected_mod: Path | None = None,
                         installed_after: float | None = None) -> str:
    """User-copied, short diagnostic report; no automatic upload.

    Don't include the full game log because it can contain room codes, names,
    network addresses, access tokens or other user information.
    """
    result = inspect_install(game, expected_mod=expected_mod,
                             installed_after=installed_after)
    # Only explicit diagnostic categories and known relative filenames.
    # Never copy arbitrary log lines: they may include private room codes,
    # player names, IP addresses, Steam identifiers or authentication tokens.
    safe_missing = [
        "Brakuje pliku: " + name for name in REQUIRED
        if not (Path(game) / name).is_file()
    ]
    error_markers = (
        "PARADOX PATCH FAILED", "PARADOX PARTIAL LOAD",
        "PARADOX UI ERROR", "Failed to build PARADOX lobby settings",
        "PARADOX Harmony patch registration failed",
    )
    combined = "\n".join(result.details)
    detected = [marker for marker in error_markers if marker.casefold() in combined.casefold()]
    lines = [
        "PARADOX / RAPORT DIAGNOSTYCZNY",
        "Status: " + result.state,
        result.summary,
        *safe_missing,
        *(("Wykryty typ błędu: " + marker) for marker in detected),
        "",
        "Potwierdzenie uruchomienia gry i patchy wymaga BepInEx LogOutput.",
        "Ustawienia PARADOX: HOST → własny pokój → opcje gry.",
        "Otwórz log osobno i usuń prywatne dane przed jego udostępnieniem.",
    ]
    # The report never copies arbitrary lines from game logs automatically.
    return "\n".join(lines)
