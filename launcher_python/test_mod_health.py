"""Offline tests for PARADOX installation versus real plugin-start status."""
import tempfile
from pathlib import Path
from unittest import TestCase

from launcher_python.mod_health import REQUIRED, LOADED_MARKER, inspect_install


class InstallHealthTests(TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.game = Path(self.temp.name)
        for name in REQUIRED:
            f = self.game / name
            f.parent.mkdir(parents=True, exist_ok=True)
            f.write_bytes(b"test")
        (self.game / "doorstop_config.ini").write_text(
            "[General]\nenabled = true\n"
            r"target_assembly = BepInEx\core\BepInEx.Unity.IL2CPP.dll"
            "\n[Il2Cpp]\n", encoding="utf-8")

    def log(self, content):
        file = self.game / "BepInEx/LogOutput.txt"
        file.write_text(content, encoding="utf-8")
        return file

    def test_no_log_does_not_claim_mod_loaded(self):
        result = inspect_install(self.game)
        self.assertEqual(result.state, "pending")

    def test_valid_game_and_runtime_marker(self):
        self.log("BepInEx chainloader\n[Info] " + LOADED_MARKER + "\n")
        result = inspect_install(self.game)
        self.assertEqual(result.state, "loaded")

    def test_bepinex_start_without_mod_is_not_success(self):
        self.log("BepInEx starting...\nChainloader startup complete.")
        result = inspect_install(self.game)
        self.assertEqual(result.state, "failed")

    def test_missing_dependency_is_broken_even_with_old_success_log(self):
        self.log("BepInEx\n" + LOADED_MARKER)
        (self.game / "BepInEx/plugins/Reactor.dll").unlink()
        result = inspect_install(self.game)
        self.assertEqual(result.state, "broken")
        self.assertIn("Reactor.dll", " ".join(result.details))

    def test_disabled_doorstop_detected(self):
        config = self.game / "doorstop_config.ini"
        config.write_text(
            "[General]\nenabled = false\n"
            r"target_assembly = BepInEx\core\BepInEx.Unity.IL2CPP.dll")
        result = inspect_install(self.game)
        self.assertEqual(result.state, "broken")
        self.assertIn("Doorstop", " ".join(result.details))

    def test_wrong_mod_dll_detected(self):
        other = self.game / "original.dll"
        other.write_bytes(b"different")
        result = inspect_install(self.game, expected_mod=other)
        self.assertEqual(result.state, "broken")
        self.assertIn("różni", " ".join(result.details))

    def test_old_log_not_used_as_new_runtime_proof(self):
        file = self.log("BepInEx\n" + LOADED_MARKER)
        result = inspect_install(self.game, installed_after=file.stat().st_mtime + 60)
        self.assertEqual(result.state, "pending")

    def test_patch_failure_revealed(self):
        self.log("BepInEx\nPARADOX v0.7 loading\n"
                 "[Error] PARADOX Harmony patch registration failed: MissingMethod")
        result = inspect_install(self.game)
        self.assertEqual(result.state, "failed")
        self.assertIn("Harmony", " ".join(result.details))

    def test_log_extension_supported(self):
        self.log("BepInEx").rename(self.game / "BepInEx/LogOutput.log")
        self.assertEqual(inspect_install(self.game).state, "failed")
