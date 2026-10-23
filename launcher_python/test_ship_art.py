"""Headless tests for the original spaceship illustration primitives."""
import unittest

from launcher_python.ship_art import draw_bridge, draw_hunt_room, draw_target


class DummyCanvas:
    def __init__(self):
        self.items = []
        self.bg = None

    def configure(self, **kwargs):
        self.bg = kwargs.get("bg", self.bg)

    def delete(self, tag):
        self.items = [item for item in self.items if tag not in item[2]]

    def _add(self, name, args, kw):
        self.items.append((name, args, kw.get("tags", ())))

    def create_oval(self, *a, **kw):
        self._add("oval", a, kw)

    def create_rectangle(self, *a, **kw):
        self._add("rectangle", a, kw)

    def create_polygon(self, *a, **kw):
        self._add("polygon", a, kw)

    def create_text(self, *a, **kw):
        self._add("text", a, kw)

    def create_line(self, *a, **kw):
        self._add("line", a, kw)


class ShipIllustrationTests(unittest.TestCase):
    def test_banner_redraw_does_not_duplicate_items(self):
        canvas = DummyCanvas()
        draw_bridge(canvas, 850, 282)
        before = len(canvas.items)
        self.assertGreater(before, 100)
        self.assertTrue(all("art" in i[2] for i in canvas.items))
        draw_bridge(canvas, 1000, 282)
        self.assertEqual(len(canvas.items), before)

    def test_target_tag_matches_animation_and_click_binding(self):
        canvas = DummyCanvas()
        draw_target(canvas, 120, 160)
        self.assertGreater(len(canvas.items), 4)
        self.assertTrue(all("target" in item[2] for item in canvas.items))

    def test_arena_and_target_have_disjoint_tags(self):
        canvas = DummyCanvas()
        draw_hunt_room(canvas)
        self.assertTrue(any("arena" in item[2] for item in canvas.items))
        n = len(canvas.items)
        draw_target(canvas, 120, 160)
        self.assertGreater(len(canvas.items), n)
        self.assertFalse(any("target" in item[2] for item in canvas.items[:n]))


if __name__ == "__main__":
    unittest.main()
