"""Pure startup checks — GUI is NOT opened in CI."""
import tempfile
from pathlib import Path
import unittest

from launcher_python.paradox_launcher import (
    new_challenge, pe_machine, valid_game, source_payload, install_backend
)


class StartupTests(unittest.TestCase):
    def test_math_has_all_four_operations(self):
        seen = set()
        for _ in range(1200):
            text, result = new_challenge()
            operator = text.split()[1]
            seen.add(operator)
            left = int(text.split()[0])
            right = int(text.split()[2])
            self.assertTrue(1 <= left <= 50)
            self.assertTrue(1 <= right <= 50)
            self.assertEqual(result, {
                "+": lambda: left + right, "-": lambda: left - right,
                "×": lambda: left * right, "^": lambda: left ** right
            }[operator]())
        self.assertEqual(seen, {"+", "-", "×", "^"})

    def test_game_folder_and_x64_guard(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)
            self.assertFalse(valid_game(path))
            (path / "Among Us_Data").mkdir()
            (path / "GameAssembly.dll").write_bytes(b"fake-data")
            pe = bytearray(512)
            pe[:2] = b"MZ"
            pe[0x3C:0x40] = (0x80).to_bytes(4, "little")
            pe[0x80:0x84] = b"PE\0\0"
            pe[0x84:0x86] = (0x8664).to_bytes(2, "little")
            (path / "Among Us.exe").write_bytes(pe)
            self.assertTrue(valid_game(path))
            self.assertEqual(pe_machine(path / "Among Us.exe"), 0x8664)
            pe[0x84:0x86] = (0x14C).to_bytes(2, "little")
            (path / "Among Us.exe").write_bytes(pe)
            self.assertEqual(pe_machine(path / "Among Us.exe"), 0x14C)

    def test_bundle_paths_are_separate(self):
        self.assertEqual(source_payload().name, "payload")
        self.assertEqual(install_backend().name, "Install-Paradox.ps1")


if __name__ == "__main__":
    unittest.main()
