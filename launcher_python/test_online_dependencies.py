"""No-network unit checks for the live dependency manager (real download in CI)."""
import base64
import hashlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

from launcher_python import online_dependencies as online


class OnlineDependencyTests(unittest.TestCase):
    def setUp(self):
        self.manifest = json.loads(
            (Path(__file__).resolve().parent / "dependencies.json").read_text("utf-8"))

    def test_local_manifest_is_valid(self):
        self.assertEqual(online.validate_manifest(self.manifest), self.manifest)

    def test_reject_changed_reactor_even_if_newer(self):
        altered = json.loads(json.dumps(self.manifest))
        altered["reactor"]["version"] = "9.0.0-ci.999"
        with self.assertRaises(online.DependencyError):
            online.validate_manifest(altered)

    def test_reject_wrong_game_version(self):
        altered = json.loads(json.dumps(self.manifest))
        altered["among_us_major"] = 20
        with self.assertRaises(online.DependencyError):
            online.validate_manifest(altered)

    def test_reject_injected_download_host(self):
        altered = json.loads(json.dumps(self.manifest))
        altered["bepinex"]["url"] = "https://evil.example.net/not-a-bepinex.zip"
        with self.assertRaises(online.DependencyError):
            online.validate_manifest(altered)

    def test_fallback_offline_manifest_when_online_lookup_fails(self):
        with patch.object(online, "_fetch_bytes", side_effect=online.DependencyError("offline")):
            result = online.choose_manifest()
        self.assertEqual(result, self.manifest)

    def test_reject_insecure_https_redirect(self):
        from email.message import Message
        redirect = online.StrictRedirect()
        with self.assertRaises(online.DependencyError):
            redirect.redirect_request(None, None, 302, "Moved", Message(),
                                      "http://bad.example/evil")

    def test_archive_zip_slip_is_rejected(self):
        output = io.BytesIO()
        with zipfile.ZipFile(output, "w") as z:
            z.writestr("../escape.dll", b"test")
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaises(online.DependencyError):
                online._extract_safe(output.getvalue(), Path(temp))

    def test_nuget_package_url_discovered_from_service_index(self):
        service = {"resources": [
            {"@id": "https://nuget.reactor.gg/custom-flatcontainer/",
             "@type": "PackageBaseAddress/3.0.0"}
        ]}
        with patch.object(online, "_fetch_bytes",
                          return_value=json.dumps(service).encode("utf-8")):
            pkg = online.reactor_download_urls(
                "https://nuget.reactor.gg/v3/index.json")
        self.assertEqual(pkg, "https://nuget.reactor.gg/custom-flatcontainer/reactor/"
                              "2.5.1-ci.400/reactor.2.5.1-ci.400.nupkg")

    def test_untrusted_nuget_index_endpoint_rejected(self):
        service = {"resources": [
            {"@id": "https://evil.example.com/v3-flatcontainer/",
             "@type": "PackageBaseAddress/3.0.0"}
        ]}
        with patch.object(online, "_fetch_bytes",
                          return_value=json.dumps(service).encode("utf-8")):
            with self.assertRaises(online.DependencyError):
                online.reactor_download_urls("https://nuget.reactor.gg/v3/index.json")

    def test_reactor_zip_selects_expected_net6(self):
        output = io.BytesIO()
        with zipfile.ZipFile(output, "w") as z:
            z.writestr("lib/net6.0/Reactor.dll", b"MZ" + b"\0" * 50)
            z.writestr("readme.txt", "demo")
        self.assertEqual(online._reactor_assembly(output.getvalue())[:2], b"MZ")

    def test_reactor_package_hash_is_verified_before_cache(self):
        output = io.BytesIO()
        with zipfile.ZipFile(output, "w") as z:
            z.writestr("lib/net6.0/Reactor.dll", b"MZ" + b"\0" * 50)
        content = output.getvalue()
        sha512 = hashlib.sha512(content).digest()
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(online, "cache_root", return_value=Path(folder)):
                with patch.object(online, "_fetch_bytes", return_value=content):
                    downloaded = online._get_verified_reactor("https://nuget.reactor.gg/example",
                                                              hashlib.sha256(b"MZ"+b"\0"*50).hexdigest(), None)
                self.assertEqual(downloaded, content)
                self.assertTrue(next(Path(folder).iterdir()).exists())
                with patch.object(online, "_fetch_bytes", side_effect=RuntimeError("no network")):
                    self.assertEqual(online._get_verified_reactor("https://nuget.reactor.gg/example",
                                                                  hashlib.sha256(b"MZ"+b"\0"*50).hexdigest(), None), content)

    def test_bepinex_fingerprint_failure_fails_closed(self):
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(online, "cache_root", return_value=Path(folder)):
                with patch.object(online, "_fetch_bytes", return_value=b"unauthorized"):
                    with self.assertRaises(online.DependencyError):
                        online._get_verified(self.manifest["bepinex"]["url"], "BepInEx",
                                             self.manifest["bepinex"]["sha256"], None, .1, .3)


if __name__ == "__main__":
    unittest.main()
