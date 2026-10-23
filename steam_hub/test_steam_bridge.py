"""No Steam account, Windows registry or network is required for unit tests."""
from pathlib import Path
import tempfile
from unittest import TestCase
from unittest.mock import patch

from steam_hub.steam_bridge import (
    STEAM_APP_ID, parse_steam_vdf_string, discover_owned_installation,
    valid_among_us_game, launch_among_us
)
from steam_hub.paradox_hub import diagnostic_color


class SteamBridgeTests(TestCase):
    def test_app_id(self):
        self.assertEqual(STEAM_APP_ID, 945360)

    def test_parse_manifest(self):
        content = '"AppState"\n{\n"appid"\t"945360"\n"installdir"\t"Among Us"\n}'
        self.assertEqual(parse_steam_vdf_string(content, "appid"), "945360")
        self.assertEqual(parse_steam_vdf_string(content, "installdir"), "Among Us")

    def test_discover_only_real_installed_game(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            manifest = root / "steamapps/appmanifest_945360.acf"
            manifest.parent.mkdir()
            manifest.write_text('"AppState"\n{\n"appid" "945360"\n"installdir" "Among Us"\n}',
                                encoding="utf-8")
            game = root / "steamapps/common/Among Us"
            game.mkdir(parents=True)
            self.assertIsNone(discover_owned_installation((root,)))
            (game / "Among Us.exe").write_bytes(b"game-test")
            (game / "GameAssembly.dll").write_bytes(b"game-test")
            (game / "Among Us_Data").mkdir()
            self.assertEqual(discover_owned_installation((root,)), game)
            manifest.write_text('"appid" "123"\n"installdir" "Among Us"\n')
            self.assertIsNone(discover_owned_installation((root,)))

    def test_manifest_cannot_escape_steam_library(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "steamapps").mkdir()
            (root / "steamapps/appmanifest_945360.acf").write_text(
                '"appid" "945360"\n"installdir" ".."')
            self.assertIsNone(discover_owned_installation((root,)))

    def test_vdf_backslashes(self):
        self.assertEqual(
            parse_steam_vdf_string('"path" "D:\\\\SteamLibrary"', "path"),
            r"D:\SteamLibrary",
        )

    def test_diagnostic_bad_not_green(self):
        for state in ("broken", "failed", "partial"):
            self.assertNotEqual(diagnostic_color(state), "#7AE3DE")
        self.assertNotEqual(diagnostic_color("loaded", False), "#7AE3DE")
        self.assertEqual(diagnostic_color("loaded", True), "#7AE3DE")

    def test_launch_refuses_wrong_game_dir(self):
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaises(FileNotFoundError):
                launch_among_us(Path(temp), use_steam=True)


if __name__ == "__main__":
    import unittest
    unittest.main()
