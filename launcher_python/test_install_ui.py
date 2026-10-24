"""Contract tests for PARADOX installation UI rendering math (headless)."""
import unittest
from launcher_python.paradox_install_ui import gradient, progress_state


class InstallUiTests(unittest.TestCase):
    def test_gradient_boundaries(self):
        self.assertEqual(gradient(0), "#873bff")
        self.assertEqual(gradient(1), "#FF3658")
        self.assertEqual(gradient(-1), gradient(0))
        self.assertEqual(gradient(8), gradient(1))

    def test_progress_state_is_actual_progress_not_static(self):
        self.assertIn("OCZEKIWANIE", progress_state(0))
        self.assertIn("POBIERANIE", progress_state(.4))
        self.assertIn("INSTALACJA", progress_state(.8))
        self.assertIn("GOTOWE", progress_state(1))


if __name__ == "__main__":
    unittest.main()
