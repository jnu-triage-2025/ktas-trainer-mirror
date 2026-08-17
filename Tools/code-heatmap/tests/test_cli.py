#!/usr/bin/env python3
"""Small dependency-free regression tests for code-heatmap.py."""
import tempfile
import unittest
from pathlib import Path

from code_heatmap import cli as MODULE


class CodeHeatmapTests(unittest.TestCase):
    def test_default_extensions_and_measurement(self):
        self.assertIn("cs", MODULE.csv_set(MODULE.DEFAULT_EXTENSIONS))
        self.assertEqual(MODULE.measure(b"one\ntwo\n", "lines"), 2)
        self.assertEqual(MODULE.measure("한글".encode(), "characters"), 2)

    def test_depth_rolls_up_all_descendants_into_the_limit_folder(self):
        files = [
            MODULE.SourceFile("Assets/Modules/Test/Scripts/A.cs", "cs", 12, "Test"),
            MODULE.SourceFile("Assets/Modules/Test/Scripts/Nested/B.cs", "cs", 8, "Test"),
        ]
        tree = MODULE.build_tree(files, 4)
        scripts = tree.children["Assets"].children["Modules"].children["Test"].children["Scripts"]
        self.assertTrue(scripts.rollup)
        self.assertEqual((scripts.value, scripts.file_count, scripts.children), (20, 2, {}))
        self.assertEqual(scripts.type_counts, {"cs": 2})

    def test_current_files_filters_and_excludes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "a.cs").write_text("x\ny")
            (root / "skip").mkdir()
            (root / "skip" / "b.json").write_text("{}")
            files = MODULE.current_files(root, {"cs", "json"}, {"skip"}, [], "lines")
            self.assertEqual([(item.path, item.value) for item in files], [("a.cs", 2)])

    def test_ignore_pattern_matches_hidden_files_and_directories(self):
        self.assertTrue(MODULE.is_ignored(".editorconfig", [".*"]))
        self.assertTrue(MODULE.is_ignored("Assets/.cache/Generated.cs", [".*"]))
        self.assertFalse(MODULE.is_ignored("Assets/Modules/Test.cs", [".*"]))
        self.assertTrue(MODULE.is_ignored("Assets/Modules/FishNet/Runtime/Fish.cs", ["Assets/Modules/FishNet/"]))

    def test_missing_config_uses_template(self):
        with tempfile.TemporaryDirectory() as directory:
            config = Path(directory) / "code-heatmap.ignore"
            config.with_name("code-heatmap.ignore.template").write_text(".*\n")
            self.assertEqual(MODULE.load_ignore_patterns(config), [".*"])

    def test_squarified_layout_uses_multiple_rows(self):
        nodes = [MODULE.Node(str(index), str(index), value) for index, value in enumerate((60, 40, 30, 20, 10))]
        rectangles = MODULE.treemap(nodes, 0, 0, 100, 100)
        self.assertAlmostEqual(sum(width * height for _, _, _, width, height in rectangles), 10000)
        self.assertGreater(len({round(y, 3) for _, _, y, _, _ in rectangles}), 1)

    def test_small_leaves_do_not_become_zero_size_rectangles(self):
        files = [MODULE.SourceFile(f"Assets/{index}.cs", "cs", 1, "(root)") for index in range(20)]
        rendered = "".join(MODULE.render_tree(MODULE.build_tree(files, 5), 0, 0, 100, 100, "type", {}))
        sizes = [(float(width), float(height)) for width, height in __import__("re").findall(r'class="file" x="[^"]+" y="[^"]+" width="([0-9.]+)" height="([0-9.]+)"', rendered)]
        self.assertEqual(len(sizes), 20)
        self.assertTrue(all(width > 0 and height > 0 for width, height in sizes))

    def test_rollup_uses_its_most_common_file_type_colour(self):
        files = [
            MODULE.SourceFile("Assets/Foo/A.cs", "cs", 1, "(root)"),
            MODULE.SourceFile("Assets/Foo/B.cs", "cs", 1, "(root)"),
            MODULE.SourceFile("Assets/Foo/C.json", "json", 1, "(root)"),
        ]
        rendered = "".join(MODULE.render_tree(MODULE.build_tree(files, 2), 0, 0, 100, 100, "type", {}))
        self.assertIn('class="aggregate"', rendered)
        self.assertIn('fill="#F2C6DE"', rendered)  # Pastel selected for "cs".

    def test_date_range_accepts_every_supported_precision(self):
        for value, seconds in (("2026-08-17", 86400), ("2026-08-17-9", 3600), ("2026-08-17-09", 3600), ("2026-08-17-09-3", 60), ("2026-08-17-09-03", 60), ("2026-08-17-09-03-4", 1), ("2026-08-17-09-03-04", 1)):
            start, end = MODULE.parse_date_range(value)
            self.assertEqual(end - start, seconds)

    def test_date_selection_policies(self):
        entries = [("early", 100), ("middle", 150), ("late", 199), ("next", 300)]
        self.assertEqual(MODULE.select_date_commit(entries, 100, 200, "latest", "fast-forward"), "late")
        self.assertEqual(MODULE.select_date_commit(entries, 100, 200, "oldest", "fast-forward"), "early")
        self.assertEqual(MODULE.select_date_commit(entries, 100, 200, "median", "fast-forward"), "middle")
        self.assertEqual(MODULE.select_date_commit(entries, 201, 250, "latest", "ff"), "next")
        self.assertEqual(MODULE.select_date_commit(entries, 201, 250, "latest", "rw"), "late")

    def test_size_baseline_scales_panels_by_the_selected_snapshot(self):
        self.assertEqual(MODULE.proportional_panel_scales([100, 400], 100), [1.0, 2.0])
        self.assertEqual(MODULE.proportional_panel_scales([100, 400], 400), [0.5, 1.0])

    def test_date_range_uses_timezone_dst_rules(self):
        start, end = MODULE.parse_date_range("2026-11-01", "America/New_York")
        self.assertEqual(end - start, 25 * 60 * 60)

    def test_invalid_hex_colour_is_rejected_before_rendering(self):
        with self.assertRaises(ValueError):
            MODULE.parse_colours("cs:#GGG")


if __name__ == "__main__":
    unittest.main()
