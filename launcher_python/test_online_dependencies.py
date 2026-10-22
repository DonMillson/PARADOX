"""All downloader tests are fully mocked: no real internet or game files."""
import base64
from io import BytesIO
import hashlib
import json
import tempfile
from pathlib import Path
from unittest import TestCase
from unittest.mock import patch
import zipfile

from launcher_python import online_dependencies as deps


def make_zip(files: dict[str, bytes]) -> bytes:
    out = BytesIO()
    with zipfile.ZipFile(out, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for name, content in files.items():
            zf.writestr(name, content)
    return out.getvalue()


def sample_manifest():
    return {
        "schema": 1, "mod_version": deps.MOD_VERSION,
        "game_baseline": "19.0.0",
        "reactor_version": deps.COMPILED_REACTOR,
        "bepinex_min_build": 790,
        "bepinex_fallback_url":
            "https://builds.bepinex.dev/projects/bepinex_be/790/"
            "BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.790%2B4e98359.zip",
        "bepinex_fallback_sha256": "a" * 64
    }


class DependencyTests(TestCase):
    def test_bep_latest_x64_il2cpp_only(self):
        html = """
        <a href="/projects/bepinex_be/791/BepInEx-Unity.Mono-win-x64-6.0.0-be.791%2Babcdeff.zip"></a>
        <a href="/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.791%2Babcdeff.zip"></a>
        <a href="/projects/bepinex_be/790/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.790%2B4e98359.zip"></a>
        <a href="/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Babcdeff.zip"></a>
        """
        with patch.object(deps, "_text", return_value=html):
            url, build = deps.latest_bepinex_url(sample_manifest())
        # The newest web listing is not a tested runtime: use pinned BE 790.
        self.assertEqual(build, 790)
        self.assertIn("IL2CPP-win-x64", url)

    def test_fallback_when_page_unreachable(self):
        with patch.object(deps, "_text", side_effect=deps.DependencyError("no connection")):
            url, build = deps.latest_bepinex_url(sample_manifest())
        self.assertEqual(build, 790)
        self.assertEqual(url, sample_manifest()["bepinex_fallback_url"])

    def test_no_insecure_url(self):
        with self.assertRaises(deps.DependencyError):
            deps._validated_https("http://builds.bepinex.dev/b.zip", "builds.bepinex.dev")
        with self.assertRaises(deps.DependencyError):
            deps._validated_https("https://evil.example/b.zip", "builds.bepinex.dev")

    def test_manifest_blocks_wrong_reactor(self):
        wrong = sample_manifest()
        wrong["reactor_version"] = "99.0"
        with patch.object(deps, "_text", return_value=json.dumps(wrong)):
            with self.assertRaises(deps.DependencyError):
                deps.get_manifest()

    def test_zip_does_not_extract_game_exe_path_traversal(self):
        with tempfile.TemporaryDirectory() as path:
            blob = make_zip({"../Among Us.exe": b"malicious"})
            with self.assertRaises(deps.DependencyError):
                deps._safe_extract(blob, Path(path))
            self.assertFalse((Path(path).parent / "Among Us.exe").exists())

    def test_official_nuget_package_index(self):
        def fake_text(url, host, limit=0):
            if url.endswith("/v3/index.json"):
                return json.dumps({"resources": [
                    {"@type": "PackageBaseAddress/3.0.0",
                     "@id": "https://nuget.reactor.gg/v3-flatcontainer/"}]})
            return json.dumps({"versions": ["2.5.1-ci.384", "2.5.1-ci.400"]})
        with patch.object(deps, "_text", side_effect=fake_text):
            url, checksum = deps.reactor_package_url(deps.COMPILED_REACTOR)
        self.assertEqual(url, "https://nuget.reactor.gg/v3-flatcontainer/"
                         "reactor/2.5.1-ci.400/reactor.2.5.1-ci.400.nupkg")
        self.assertEqual(checksum, url + ".sha512")

    def test_integration_download_stage_without_network(self):
        be_zip = make_zip({
            "winhttp.dll": b"x64 mock native loader",
            "doorstop_config.ini": b"[General]\nenabled = true\ntarget_assembly = BepInEx\\core\\BepInEx.Unity.IL2CPP.dll\n",
            "BepInEx/core/BepInEx.Unity.IL2CPP.dll": b"framework",
            "dotnet/coreclr.dll": b"runtime"})
        reactor_zip = make_zip({"lib/net6.0/Reactor.dll": b"MZ" + b"x" * 11000})
        manifest = sample_manifest()
        bep_url = manifest["bepinex_fallback_url"]
        manifest["bepinex_fallback_sha256"] = hashlib.sha256(be_zip).hexdigest()
        reactor_url = "https://nuget.reactor.gg/v3-flatcontainer/reactor/"
        reactor_url += "2.5.1-ci.400/reactor.2.5.1-ci.400.nupkg"
        sha = base64.b64encode(hashlib.sha512(reactor_zip).digest()).decode()
        events = []
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            mod = root / "Paradox.dll"
            mod.write_bytes(b"MZ" + b"paradox unit test")

            def fake_download(url, host, max_bytes=0):
                return be_zip if host == "builds.bepinex.dev" else reactor_zip

            with (patch.object(deps, "get_manifest", return_value=manifest),
                  patch.object(deps, "latest_bepinex_url", return_value=(bep_url, 790)),
                  patch.object(deps, "reactor_package_url",
                               return_value=(reactor_url, reactor_url + ".sha512")),
                  patch.object(deps, "download_file", side_effect=fake_download),
                  patch.object(deps, "_text", return_value=sha)):
                result = deps.stage_live_dependencies(
                    root / "payload", mod,
                    lambda percent, message: events.append((percent, message))
                )
            self.assertEqual(result["bepinex_build"], 790)
            self.assertTrue((root / "payload/BepInEx/plugins/Paradox.dll").is_file())
            self.assertTrue((root / "payload/BepInEx/plugins/Reactor.dll").is_file())
            self.assertTrue((root / "payload/dotnet/runtime.dll").is_file())
            self.assertGreater(events[-1][0], .5)

    def test_previous_release_manifest_uses_build_matched_fallback(self):
        old = sample_manifest()
        old["mod_version"] = "0.7.0-dev.4"
        with patch.object(deps, "_text", return_value=json.dumps(old)):
            found = deps.get_manifest()
        self.assertEqual(found["mod_version"], deps.MOD_VERSION)
        self.assertEqual(found["reactor_version"], deps.COMPILED_REACTOR)

    def test_download_bad_reactor_checksum_rejected_before_install(self):
        reactor_zip = make_zip({"lib/net6.0/Reactor.dll": b"x" * 11_000})
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "mod.dll").write_bytes(b"x")
            be_zip = make_zip({
                "winhttp.dll": b"x",
                "doorstop_config.ini": b"[General]\nenabled = true\ntarget_assembly = BepInEx\\core\\BepInEx.Unity.IL2CPP.dll\n",
                "BepInEx/core/BepInEx.Unity.IL2CPP.dll": b"y",
                "dotnet/coreclr.dll": b"runtime"
            })
            manifest = sample_manifest()
            manifest["bepinex_fallback_sha256"] = hashlib.sha256(be_zip).hexdigest()
            with (patch.object(deps, "get_manifest", return_value=manifest),
                  patch.object(deps, "latest_bepinex_url",
                               return_value=(manifest["bepinex_fallback_url"], 790)),
                  patch.object(deps, "reactor_package_url",
                               return_value=("https://nuget.reactor.gg/r.nupkg",
                                             "https://nuget.reactor.gg/r.nupkg.sha512")),
                  patch.object(deps, "download_file",
                               side_effect=[be_zip, reactor_zip]),
                  patch.object(deps, "_text", return_value="INVALID")):
                with self.assertRaisesRegex(deps.DependencyError, "SHA-512"):
                    deps.stage_live_dependencies(root / "payload", root / "mod.dll",
                                                 lambda p, m: None)
