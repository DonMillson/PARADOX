"""Secure-ish live dependency resolution for PARADOX's Windows installer.

The official source is downloaded at install time. The compatibility manifest
on PARADOX/main can be updated independently of old launchers, but unapproved
game-major / Reactor ABI changes are rejected. No unverified "latest" DLL.
"""
from __future__ import annotations

import base64
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import tempfile
from typing import Callable
import urllib.error
import urllib.parse
import urllib.request
import zipfile

GAME_MAJOR = 19
COMPILED_REACTOR = "2.5.1-ci.400"
MOD_VERSION = "0.7.0-dev.4"
MAX_DOWNLOAD = 120 * 1024 * 1024
MAX_EXTRACT = 450 * 1024 * 1024
UPSTREAM_MANIFEST = ("https://raw.githubusercontent.com/DonMillson/"
                     "PARADOX/main/launcher_python/dependencies.json")
Progress = Callable[[str, float], None]


class DependencyError(RuntimeError):
    pass


def bundled_root() -> Path:
    return Path(getattr(__import__("sys"), "_MEIPASS", Path(__file__).resolve().parent))


def bundled_manifest() -> Path:
    return bundled_root() / "launcher_python" / "dependencies.json"


def bundled_mod() -> Path:
    return bundled_root() / "mod" / "Paradox.dll"


def cache_root() -> Path:
    root = os.environ.get("LOCALAPPDATA")
    return (Path(root) if root else Path(tempfile.gettempdir())) / "PARADOX" / "cache"


def validate_manifest(doc: object) -> dict:
    if not isinstance(doc, dict):
        raise DependencyError("Nieprawidłowy format manifestu.")
    if doc.get("schema") != 1 or doc.get("among_us_major") != GAME_MAJOR:
        raise DependencyError("Manifest nie obsługuje tej wersji Among Us.")
    if doc.get("paradox") != MOD_VERSION or doc.get("compiled_reactor") != COMPILED_REACTOR:
        raise DependencyError("Dla tej wersji Reactora wymagany jest nowy build PARADOX.")
    bep = doc.get("bepinex")
    reactor = doc.get("reactor")
    if not isinstance(bep, dict) or not isinstance(reactor, dict):
        raise DependencyError("Brak danych zależności.")
    version = bep.get("version", "")
    if not isinstance(version, str) or not re.fullmatch(r"6\.0\.0-be\.\d+\+[0-9a-f]{7,40}", version):
        raise DependencyError("Nieprawidłowa wersja BepInEx.")
    build = version.split("-be.")[1].split("+")[0]
    expected_bep_url = (f"https://builds.bepinex.dev/projects/bepinex_be/{build}/"
                        f"BepInEx-Unity.IL2CPP-win-x64-{urllib.parse.quote(version, safe='')}.zip")
    if bep.get("url") != expected_bep_url:
        raise DependencyError("Adres BepInEx nie jest z oficjalnego źródła IL2CPP x64.")
    sha = bep.get("sha256", "")
    if not isinstance(sha, str) or not re.fullmatch(r"[0-9a-fA-F]{64}", sha):
        raise DependencyError("Manifest nie zawiera SHA256 BepInEx.")
    if reactor.get("version") != COMPILED_REACTOR:
        raise DependencyError("Reactor nie pasuje do kompilacji PARADOX.")
    base = "https://nuget.reactor.gg/v3-flatcontainer/reactor/"
    nupkg = f"reactor.{COMPILED_REACTOR}.nupkg"
    if reactor.get("url") != base + COMPILED_REACTOR + "/" + nupkg:
        raise DependencyError("Nieznany adres paczki Reactor.")
    if reactor.get("sha512_url") != base + COMPILED_REACTOR + "/" + nupkg + ".sha512":
        raise DependencyError("Nieznany adres sumy kontrolnej Reactor.")
    return doc


class StrictRedirect(urllib.request.HTTPRedirectHandler):
    """Redirects may not downgrade TLS or move off approved vendor/CDN hosts."""
    ALLOWED = {
        "builds.bepinex.dev", "nuget.reactor.gg",
        "github.com", "objects.githubusercontent.com",
        "release-assets.githubusercontent.com",
    }

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        u = urllib.parse.urlsplit(newurl)
        if u.scheme != "https" or (u.hostname or "").lower() not in self.ALLOWED:
            raise DependencyError("Pobieranie przekierowało na nieznany serwer.")
        return super().redirect_request(req, fp, code, msg, headers, newurl)


OPENER = urllib.request.build_opener(StrictRedirect())


def _fetch_bytes(url: str, max_bytes: int) -> bytes:
    if urllib.parse.urlsplit(url).scheme != "https":
        raise DependencyError("Wymagany jest bezpieczny adres HTTPS.")
    request = urllib.request.Request(url, headers={"User-Agent": "PARADOX-Installer/1.0"})
    try:
        with OPENER.open(request, timeout=35) as response:
            final = urllib.parse.urlsplit(response.geturl())
            allowed = StrictRedirect.ALLOWED | {"raw.githubusercontent.com"}
            if final.scheme != "https" or (final.hostname or "").lower() not in allowed:
                raise DependencyError("Nieoczekiwana lokalizacja pliku.")
            output = bytearray()
            while True:
                chunk = response.read(min(65536, max_bytes + 1 - len(output)))
                if not chunk:
                    break
                output.extend(chunk)
                if len(output) > max_bytes:
                    raise DependencyError("Pobrany plik przekracza bezpieczny limit.")
            return bytes(output)
    except (OSError, urllib.error.URLError) as exc:
        raise DependencyError(f"Błąd pobierania z {url}: {exc}") from exc


def choose_manifest(progress: Progress | None = None) -> dict:
    local = validate_manifest(json.loads(bundled_manifest().read_text("utf-8")))
    try:
        remote = validate_manifest(json.loads(
            _fetch_bytes(UPSTREAM_MANIFEST, 32768).decode("utf-8")))
        if progress:
            progress("Sprawdzono aktualną zgodną konfigurację PARADOX", 0.04)
        return remote
    except (DependencyError, ValueError, OSError, UnicodeError) as exc:
        if progress:
            progress(f"Używam zapisanej kompatybilnej wersji: {exc}", 0.04)
        return local


def _sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _get_verified(url: str, name: str, sha256: str,
                  progress: Progress | None, p1: float, p2: float) -> bytes:
    cache = cache_root()
    cache.mkdir(parents=True, exist_ok=True)
    cached = cache / f"{name}-{sha256[:16]}.zip"
    if cached.is_file():
        data = cached.read_bytes()
        if _sha256(data).lower() == sha256.lower():
            if progress:
                progress(f"Z cache: {name}", p2)
            return data
        cached.unlink()
    if progress:
        progress(f"Pobieram {name} z oficjalnego serwera", p1)
    data = _fetch_bytes(url, MAX_DOWNLOAD)
    if _sha256(data).lower() != sha256.lower():
        raise DependencyError(f"SHA256 nie zgadza się dla {name}. Instalacja przerwana.")
    temp = cached.with_suffix(".tmp")
    temp.write_bytes(data)
    temp.replace(cached)
    if progress:
        progress(f"Zweryfikowano {name} (SHA256)", p2)
    return data


def _extract_safe(archive: bytes, dest: Path) -> None:
    with zipfile.ZipFile(__import__("io").BytesIO(archive)) as z:
        files = z.infolist()
        if sum(item.file_size for item in files) > MAX_EXTRACT:
            raise DependencyError("Archiwum po rozpakowaniu jest zbyt duże.")
        for item in files:
            name = item.filename.replace("\\", "/")
            path = PurePosixPath(name)
            if (path.is_absolute() or ".." in path.parts or
                    ":" in name or name.startswith("/") or
                    (item.external_attr >> 16) & 0o170000 == 0o120000):
                raise DependencyError("Wykryto niebezpieczną ścieżkę w ZIP.")
            if not name or item.is_dir():
                continue
            target = dest.joinpath(*path.parts)
            target.parent.mkdir(parents=True, exist_ok=True)
            with z.open(item) as src, target.open("wb") as dst:
                shutil.copyfileobj(src, dst, length=1024 * 1024)


def _reactor_assembly(nupkg: bytes) -> bytes:
    with zipfile.ZipFile(__import__("io").BytesIO(nupkg)) as z:
        candidates = [name for name in z.namelist()
                      if re.fullmatch(r"lib/[^/]+/Reactor\.dll", name, flags=re.I)]
        if not candidates:
            raise DependencyError("Oficjalny pakiet NuGet nie zawiera Reactor.dll.")
        # The CI400 package was compiled for net6.0; reject unknown layout.
        selected = next((x for x in candidates if x.lower() == "lib/net6.0/reactor.dll"), None)
        if selected is None:
            if len(candidates) != 1:
                raise DependencyError("Reactor NuGet ma niejednoznaczne biblioteki.")
            selected = candidates[0]
        if z.getinfo(selected).file_size > 15 * 1024 * 1024:
            raise DependencyError("Niespodziewany rozmiar Reactor.dll.")
        dll = z.read(selected)
        if not dll.startswith(b"MZ"):
            raise DependencyError("Reactor.dll nie jest poprawną biblioteką PE.")
        return dll


def assemble_payload(destination: Path, progress: Progress | None = None) -> dict:
    manifest = choose_manifest(progress)
    mod = bundled_mod()
    if not mod.is_file() or not mod.read_bytes().startswith(b"MZ"):
        raise DependencyError("Brakuje dołączonej do programu Paradox.dll.")

    bep = manifest["bepinex"]
    reactor = manifest["reactor"]
    bep_zip = _get_verified(bep["url"], "BepInEx", bep["sha256"], progress, .06, .33)

    # NuGet's official SHA-512 sidecar is downloaded over HTTPS and must match
    # the package bytes. The package version cannot change without a mod rebuild.
    sha_b64 = _fetch_bytes(reactor["sha512_url"], 4096).decode("ascii").strip()
    try:
        reactor_sha = base64.b64decode(sha_b64, validate=True)
    except ValueError as exc:
        raise DependencyError("Nieprawidłowa suma SHA512 od dostawcy Reactora.") from exc
    if len(reactor_sha) != 64:
        raise DependencyError("Nieprawidłowy rozmiar sumy SHA512 Reactora.")
    # Cache the official package under its upstream SHA512-derived content ID.
    nupkg = _get_verified_reactor(reactor["url"], reactor_sha, progress)
    _extract_safe(bep_zip, destination)
    required = [destination / "winhttp.dll", destination / "BepInEx" / "core"]
    if not all(p.exists() for p in required):
        raise DependencyError("Oficjalne BepInEx nie zawiera wymaganych plików IL2CPP.")
    plugin_folder = destination / "BepInEx" / "plugins"
    plugin_folder.mkdir(parents=True, exist_ok=True)
    (plugin_folder / "Reactor.dll").write_bytes(_reactor_assembly(nupkg))
    shutil.copy2(mod, plugin_folder / "Paradox.dll")
    if progress:
        progress("Weryfikacja plików gotowa; rozpoczynam instalację", .65)
    return manifest


def _get_verified_reactor(url: str, sha512: bytes, progress: Progress | None) -> bytes:
    filename = "Reactor-" + sha512.hex()[:20] + ".nupkg"
    cached = cache_root() / filename
    cached.parent.mkdir(parents=True, exist_ok=True)
    if cached.exists():
        content = cached.read_bytes()
        if hashlib.sha512(content).digest() == sha512:
            if progress:
                progress("Reactor z cache (SHA512)", .58)
            return content
        cached.unlink()
    if progress:
        progress("Pobieram Reactor NuGet", .38)
    content = _fetch_bytes(url, MAX_DOWNLOAD)
    if hashlib.sha512(content).digest() != sha512:
        raise DependencyError("Reactor NuGet: niezgodna suma SHA512!")
    tmp = cached.with_suffix(".tmp")
    tmp.write_bytes(content)
    tmp.replace(cached)
    if progress:
        progress("Reactor zweryfikowany SHA512", .58)
    return content
