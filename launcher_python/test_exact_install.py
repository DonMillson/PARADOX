"""PARADOX supplied installation concept image and real progress updates."""
import unittest
from PIL import ImageChops
from launcher_python.paradox_exact_install import ArtworkRenderer, stage_for_fraction

class ExactInstallArtworkTests(unittest.TestCase):
    def test_reference_artwork_is_bundled_and_readable(self):
        renderer = ArtworkRenderer()
        self.assertEqual(renderer.reference.size, (1536, 864))
        self.assertEqual(renderer.reference.mode, "RGB")

    def test_progress_replaces_demo_sixty_seven_percent(self):
        renderer = ArtworkRenderer()
        before = renderer.render(0, "Gotowy")
        during = renderer.render(.67, "Pobieranie zależności...")
        after = renderer.render(1, "Pliki gotowe")
        roi = (430, 390, 1430, 491)
        self.assertIsNotNone(ImageChops.difference(before.crop(roi), during.crop(roi)).getbbox())
        self.assertIsNotNone(ImageChops.difference(during.crop(roi), after.crop(roi)).getbbox())
        self.assertEqual(before.getpixel((110, 85)), after.getpixel((110, 85)))

    def test_stages(self):
        self.assertEqual(stage_for_fraction(0), 0)
        self.assertEqual(stage_for_fraction(.3), 1)
        self.assertEqual(stage_for_fraction(.8), 2)
        self.assertEqual(stage_for_fraction(1), 3)

if __name__ == "__main__":
    unittest.main()
