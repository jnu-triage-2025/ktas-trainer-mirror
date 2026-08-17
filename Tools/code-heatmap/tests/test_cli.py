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

    def test_depth_flattens_deeper_folders(self):
        source = MODULE.SourceFile("Assets/Modules/Test/Scripts/A.cs", "cs", 12, "Test")
        tree = MODULE.build_tree([source], 3)
        leaf = tree.children["Assets"].children["Modules"].children["Test"].children["Scripts/A.cs"]
        self.assertEqual(leaf.value, 12)

    def test_current_files_filters_and_excludes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "a.cs").write_text("x\ny")
            (root / "skip").mkdir()
            (root / "skip" / "b.json").write_text("{}")
            files = MODULE.current_files(root, {"cs", "json"}, {"skip"}, "lines")
            self.assertEqual([(item.path, item.value) for item in files], [("a.cs", 2)])


if __name__ == "__main__":
    unittest.main()
