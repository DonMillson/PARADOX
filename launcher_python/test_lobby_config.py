"""No GUI, network or Windows requirements — settings file tests."""
from pathlib import Path
import tempfile
import unittest

from launcher_python.lobby_config import (
    render_lobby_config, write_lobby_config
)


class LobbyConfigTests(unittest.TestCase):
    def test_new_config(self):
        text = render_lobby_config("", True, 50)
        self.assertIn("[ExperimentalLargeLobby]", text)
        self.assertIn("Enable = true", text)
        self.assertIn("MaxPlayers = 50", text)

    def test_preserves_existing_sections(self):
        before = "[General]\nLanguage = Polish\n\n[ExperimentalLargeLobby]\nEnable = false\nMaxPlayers = 20\n[Roles]\nSheriff.Enabled = true\n"
        text = render_lobby_config(before, True, 40)
        self.assertIn("Language = Polish", text)
        self.assertIn("Sheriff.Enabled = true", text)
        self.assertEqual(text.count("Enable = true"), 1)
        self.assertEqual(text.count("MaxPlayers = 40"), 1)
        self.assertNotIn("MaxPlayers = 20", text)

    def test_insert_missing_values_and_idempotency(self):
        before = "[ExperimentalLargeLobby]\n# existing section\n[Other]\nX = y\n"
        once = render_lobby_config(before, True, 30)
        self.assertIn("[Other]\nX = y", once)
        self.assertEqual(once, render_lobby_config(once, True, 30))

    def test_rejects_unsafe_capacity(self):
        for limit in (0, 16, 35, 51, 500):
            with self.assertRaises(ValueError):
                render_lobby_config("", True, limit)

    def test_actual_write_backup_and_preservation(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "Among Us.exe").write_bytes(b"MZ")
            (root / "BepInEx/config").mkdir(parents=True)
            original = root / "BepInEx/config/gg.paradox.mod.cfg"
            original.write_text("[General]\nLanguage = Polish\n", "utf-8")
            path = write_lobby_config(root, True, 50)
            self.assertEqual(original, path)
            self.assertIn("Language = Polish", path.read_text("utf-8"))
            self.assertIn("Enable = true", path.read_text("utf-8"))
            self.assertEqual(len(list((root / ".paradox-installer").glob("*.bak"))), 1)
            write_lobby_config(root, False, 15)
            self.assertIn("Enable = false", path.read_text("utf-8"))

    def test_symlink_not_written(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "Among Us.exe").write_bytes(b"MZ")
            (root / "BepInEx").mkdir()
            external = root / "outside"
            external.mkdir()
            try:
                (root / "BepInEx/config").symlink_to(external, target_is_directory=True)
            except (OSError, NotImplementedError):
                self.skipTest("symlink creation disabled on this host")
            with self.assertRaises(ValueError):
                write_lobby_config(root, True, 50)
            self.assertEqual(list(external.iterdir()), [])


if __name__ == "__main__":
    unittest.main()
