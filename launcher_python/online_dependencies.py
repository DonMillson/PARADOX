"""Secure online dependency resolution for PARADOX Among Us launcher.

The mod DLL and its PowerShell installer are embedded; official BepInEx and
a *compatible* Reactor package are resolved/downloaded on each installation.

Do not silently upgrade Reactor to an unrelated package: a newer package can
break the compiler-generated custom RPC types this PARADOX build depends on.
"""
from __future__ import annotations

import base64
import hashlib
import html
from html.parser import HTMLParser
import io
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import ssl
import urllib.parse
import urllib.request
import zipfile
from typing import Callable

MOD_VERSION = "0.7.0-dev.5"
COMPILED_REACTOR = "2.5.1-ci.400"
MANIFEST_URL = (
    "https://raw.githubusercontent.com/DonMillson/PARADOX/"
    "main/launcher_python/compatibility.json"
)
BEPINEX_INDEX = "https://builds.bepinex.dev/projects/bepinex_be"
REACTOR_SERVICE_INDEX = "https://nuget.reactor.gg/v3/index.json"
MAX_HTML = 2_500_000
MAX_ZIP_BYTES = 180 * 1024 * 1024
MAX_EXTRACT_BYTES = 800 * 1024 * 1024
BEP_FILENAME = re.compile(
    r"^BepInEx-Unity\.IL2CPP-win-x64-6\.0\.0-be\.(\d+)\+([a-f0-9]{7,40})\.zip$"
)
REPORT = Callable[[float, str], None]


class DependencyError(RuntimeError):
    """An official server, archive, hash or compatibility check failed."""


class Links(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.hrefs: list[str] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        if tag == "a":
            value = dict(attrs).get("href")
            if value:
                self.hrefs.append(html.unescape(value))


def _validated_https(url: str, host: str) -> str:
    address = urllib.parse.urlsplit(url)
    if address.scheme != "https" or address.hostname != host or address.username or address.password:
        raise DependencyError(f"Niedozwolone źródło pakietu: {url}")
    return url


def _urlopen(url: str, host: str, timeout: int = 20):
    _validated_https(url, host)
    try:
        reply = urllib.request.urlopen(
            urllib.request.Request(url, headers={"User-Agent": "PARADOX-LAUNCHER/0.7"}),
            timeout=timeout,
            context=ssl.create_default_context(),
        )
        _validated_https(reply.geturl(), host)  # Fail closed on redirects to other hosts.
        return reply
    except (OSError, ValueError) as exc:
        raise DependencyError(f"Nie udało się pobrać danych z {host}: {exc}") from exc


def _text(url: str, host: str, limit: int = MAX_HTML) -> str:
    with _urlopen(url, host) as response:
        data = response.read(limit + 1)
        if len(data) > limit:
            raise DependencyError("Dokument z serwera jest zbyt duży.")
        return data.decode("utf-8-sig")


def get_manifest() -> dict:
    # A bundled fallback enables progress on a temporarily unavailable GitHub
    # manifest server; the archives themselves still require live TLS download.
    fallback = Path(__file__).with_name("compatibility.json")
    try:
        details = json.loads(_text(MANIFEST_URL, "raw.githubusercontent.com", 64_000))
    except (DependencyError, ValueError, json.JSONDecodeError):
        details = None

    # A previous/latest GitHub main may advertise a manifest for a *different*
    # PARADOX DLL. Such a manifest is never applied to this frozen EXE.
    # Keep its bundled, build-specific pinned manifest as the safe fallback.
    if details is None or details.get("mod_version") != MOD_VERSION:
        try:
            details = json.loads(fallback.read_text("utf-8"))
        except (OSError, ValueError) as exc:
            raise DependencyError(f"Nie udało się uzyskać zgodnej listy zależności: {exc}") from exc

    if (details.get("schema") != 1 or details.get("mod_version") != MOD_VERSION
            or details.get("game_baseline") != "19.0.0"
            or details.get("reactor_version") != COMPILED_REACTOR):
        raise DependencyError(
            "Serwer opublikował niezgodną wersję Reactor/moda. "
            "Potrzebna jest nowa kompilacja PARADOX, nie automatyczna podmiana."
        )
    if not isinstance(details.get("bepinex_min_build"), int):
        raise DependencyError("Nieprawidłowa lista zgodności BepInEx.")
    _validated_https(details.get("bepinex_fallback_url", ""), "builds.bepinex.dev")
    if not re.fullmatch(r"[0-9a-fA-F]{64}", details.get("bepinex_fallback_sha256", "")):
        raise DependencyError("Nieprawidłowy skrót kontrolny BepInEx.")
    reactor_hash = details.get("reactor_package_sha512", "")
    if reactor_hash and not re.fullmatch(r"[A-Za-z0-9+/]{86}==", reactor_hash):
        raise DependencyError("Nieprawidłowy przypięty hash Reactor.")
    return details


def latest_bepinex_url(manifest: dict) -> tuple[str, int]:
    try:
        page = _text(BEPINEX_INDEX, "builds.bepinex.dev")
        parser = Links()
        parser.feed(page)
        choices: list[tuple[int, str]] = []
        for href in parser.hrefs:
            full = urllib.parse.urljoin(BEPINEX_INDEX + "/", href)
            parsed = urllib.parse.urlsplit(full)
            if parsed.hostname != "builds.bepinex.dev" or parsed.scheme != "https":
                continue
            filename = urllib.parse.unquote(parsed.path.rsplit("/", 1)[-1])
            match = BEP_FILENAME.fullmatch(filename)
            if match:
                choices.append((int(match.group(1)), full))
        candidates = [item for item in choices if item[0] >= manifest["bepinex_min_build"]]
        if candidates:
            return max(candidates, key=lambda item: item[0])[1], max(candidates)[0]
    except (DependencyError, ValueError):
        pass  # Version discovery failed, not permission to install untrusted bytes.
    fallback = manifest["bepinex_fallback_url"]
    match = BEP_FILENAME.fullmatch(urllib.parse.unquote(fallback.rsplit("/", 1)[-1]))
    if not match or int(match.group(1)) < manifest["bepinex_min_build"]:
        raise DependencyError("Brak zweryfikowanego pakietu BepInEx.")
    return fallback, int(match.group(1))


def reactor_package_url(version: str) -> tuple[str, str]:
    if version != COMPILED_REACTOR:
        raise DependencyError("Wersja Reactor nie pasuje do skompilowanej biblioteki moda.")
    service = json.loads(_text(REACTOR_SERVICE_INDEX, "nuget.reactor.gg", 120_000))
    endpoints = [
        item.get("@id", "")
        for item in service.get("resources", [])
        if "PackageBaseAddress/3.0.0" in (
            item.get("@type") if isinstance(item.get("@type"), list)
            else [item.get("@type", "")]
        )
    ]
    if not endpoints:
        raise DependencyError("Serwer Reactor nie opublikował usługi pakietów NuGet.")
    base = _validated_https(endpoints[0].rstrip("/") + "/", "nuget.reactor.gg")
    versions_url = base + "reactor/index.json"
    available = json.loads(_text(versions_url, "nuget.reactor.gg", 200_000)).get("versions", [])
    if version not in available:
        raise DependencyError("Reactor zgodny z PARADOX nie jest dostępny na serwerze.")
    root = base + f"reactor/{version}/reactor.{version}.nupkg"
    return root, root + ".sha512"


def download_file(url: str, host: str, max_bytes: int = MAX_ZIP_BYTES) -> bytes:
    with _urlopen(url, host, timeout=45) as response:
        if int(response.headers.get("Content-Length", "0")) > max_bytes:
            raise DependencyError("Paczka przekracza limit bezpieczeństwa.")
        buffer = io.BytesIO()
        while True:
            chunk = response.read(1024 * 1024)
            if not chunk:
                break
            if buffer.tell() + len(chunk) > max_bytes:
                raise DependencyError("Przekroczono rozmiar pobieranego pliku.")
            buffer.write(chunk)
        return buffer.getvalue()


def _safe_extract(blob: bytes, output: Path) -> None:
    try:
        with zipfile.ZipFile(io.BytesIO(blob)) as archive:
            entries = archive.infolist()
            total = sum(file.file_size for file in entries)
            if len(entries) > 5_000 or total > MAX_EXTRACT_BYTES:
                raise DependencyError("Podejrzany rozmiar archiwum.")
            for info in entries:
                path = PurePosixPath(info.filename.replace("\\", "/"))
                if (not path.parts or path.is_absolute()
                        or ".." in path.parts or ":" in info.filename):
                    raise DependencyError("Archiwum zawiera niedozwoloną ścieżkę.")
                if ((info.external_attr >> 16) & 0o170000) == 0o120000:
                    raise DependencyError("Archiwum zawiera dowiązanie symboliczne.")
                if info.is_dir():
                    continue
                target = output.joinpath(*path.parts)
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(info) as src, target.open("wb") as dst:
                    shutil.copyfileobj(src, dst, 1024 * 1024)
    except (zipfile.BadZipFile, OSError) as exc:
        raise DependencyError(f"Uszkodzone archiwum: {exc}") from exc


def _dll_from_nupkg(blob: bytes) -> bytes:
    try:
        with zipfile.ZipFile(io.BytesIO(blob)) as archive:
            choices = [
                member for member in archive.namelist()
                if re.fullmatch(r"lib/net6\.0/Reactor\.dll", member, re.I)
            ]
            if len(choices) != 1:
                raise DependencyError("Oficjalna paczka NuGet nie zawiera Reactor.dll (net6.0).")
            dll = archive.read(choices[0])
            if len(dll) < 10_000:
                raise DependencyError("Nieprawidłowy Reactor.dll.")
            return dll
    except zipfile.BadZipFile as exc:
        raise DependencyError("Paczka NuGet Reactor jest uszkodzona.") from exc


def stage_live_dependencies(payload: Path, mod_dll: Path, report: REPORT) -> dict:
    """Download real official packages before invoking the transactional installer.

    This MUST be called off the GUI thread and into a temporary folder;
    the game's directory is never modified until all checks succeed.
    """
    manifest = get_manifest()
    report(0.06, "Sprawdzam zgodność i serwery")
    bep_url, build = latest_bepinex_url(manifest)
    report(0.13, f"Pobieram BepInEx be.{build}")
    bep_blob = download_file(bep_url, "builds.bepinex.dev")
    bep_hash = hashlib.sha256(bep_blob).hexdigest()
    if urllib.parse.unquote(bep_url.rsplit("/", 1)[-1]) == urllib.parse.unquote(
        manifest["bepinex_fallback_url"].rsplit("/", 1)[-1]
    ) and bep_hash.lower() != manifest["bepinex_fallback_sha256"].lower():
        raise DependencyError("BepInEx nie przeszedł kontroli SHA-256.")
    _safe_extract(bep_blob, payload)
    if not (payload / "winhttp.dll").is_file() or not (payload / "BepInEx" / "core").is_dir():
        raise DependencyError("Paczka BepInEx nie jest zgodnym loaderem Unity IL2CPP x64.")
    report(0.38, "Pobieram zgodny Reactor")
    reactor_url, checksum_url = reactor_package_url(COMPILED_REACTOR)
    reactor_blob = download_file(reactor_url, "nuget.reactor.gg")
    # BaGetter may not expose the conventional .nupkg.sha512 endpoint
    # (observed HTTP 404 in live Windows CI). Prefer a hash pinned in the
    # PARADOX compatibility manifest, generated from our NuGet restore.
    expected = manifest.get("reactor_package_sha512", "")
    if not expected:
        expected = _text(checksum_url, "nuget.reactor.gg", 1024).strip()
    if not re.fullmatch(r"[A-Za-z0-9+/]{86}==", expected):
        raise DependencyError("Brak poprawnego skrótu SHA-512 Reactor.")
    actual = base64.b64encode(hashlib.sha512(reactor_blob).digest()).decode("ascii")
    if expected != actual:
        raise DependencyError("Reactor nie przeszedł kontroli SHA-512.")
    plugin_dir = payload / "BepInEx" / "plugins"
    plugin_dir.mkdir(parents=True, exist_ok=True)
    (plugin_dir / "Reactor.dll").write_bytes(_dll_from_nupkg(reactor_blob))
    if not mod_dll.is_file():
        raise DependencyError("Brakuje oryginalnego PARADOX.dll wewnątrz instalatora.")
    shutil.copyfile(mod_dll, plugin_dir / "Paradox.dll")
    (payload / "PARADOX-PAYLOAD-MANIFEST.json").write_text(
        json.dumps({
            "mod": MOD_VERSION, "reactor": COMPILED_REACTOR,
            "reactor_package_sha512": actual, "bepinex_build": build,
            "bepinex_sha256": bep_hash,
            "bepinex_url": bep_url,
            "reactor_url": reactor_url,
        }, indent=2), encoding="utf-8"
    )
    report(0.58, "Pliki pobrane i sprawdzone")
    return {"bepinex_build": build, "reactor": COMPILED_REACTOR}


if __name__ == "__main__":
    manifest = get_manifest()
    bep_url, number = latest_bepinex_url(manifest)
    package_url, _ = reactor_package_url(manifest["reactor_version"])
    print("Compatibility manifest:", manifest["mod_version"])
    print("Latest official BepInEx:", number, bep_url)
    print("Compatible official Reactor:", package_url)
