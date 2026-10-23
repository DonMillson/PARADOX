"""Headless rendering tests for the original PARADOX spaceship theme."""
import unittest
from launcher_python import paradox_visuals as art


class RecordingCanvas:
    def __init__(self):
        self.calls = []

    def __getattr__(self, name):
        if name.startswith("create_"):
            def capture(*args, **kwargs):
                self.calls.append((name, args, kwargs))
                return len(self.calls)
            return capture
        raise AttributeError(name)


class VisualTests(unittest.TestCase):
    def test_header_contains_crew_and_game_branding(self):
        canvas = RecordingCanvas()
        art.header(canvas)
        labels = [str(args) for fn, args, kw in canvas.calls if fn == "create_text"]
        self.assertTrue(any("PARADOX" in t for t in labels))
        self.assertTrue(any("AMONG US" in t for t in labels))
        self.assertGreater(len(canvas.calls), 20)

    def test_space_ship_poster_draws_crewmate(self):
        canvas = RecordingCanvas()
        art.poster(canvas)
        texts = [str(args) for fn, args, kw in canvas.calls if fn == "create_text"]
        self.assertTrue(any("PARADOX" in text for text in texts))
        self.assertGreater(len(canvas.calls), 90)

    def test_security_camera_backdrop_renders(self):
        canvas = RecordingCanvas()
        art.hunt_backdrop(canvas)
        texts = [str(args) for fn, args, kw in canvas.calls if fn == "create_text"]
        self.assertTrue(any("FIND THE IMPOSTOR" in text for text in texts))
        self.assertGreater(len(canvas.calls), 50)

    def test_hazard_tape_has_dark_diagonal_bands(self):
        canvas = RecordingCanvas()
        art.hazard_stripe(canvas, 0, 0, 250, 15)
        polygons = [x for x in canvas.calls if x[0] == "create_polygon"]
        self.assertGreater(len(polygons), 8)


if __name__ == "__main__":
    unittest.main()
