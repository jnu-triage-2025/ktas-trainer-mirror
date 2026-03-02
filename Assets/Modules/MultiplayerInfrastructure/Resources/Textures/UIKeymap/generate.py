"""Split the keyboard SVG into base and lettering layers at the file level.

Usage example:
    python generate.py Assets/.../keyboard-layout.html

The script writes <basename>_base.svg and <basename>_lettering.svg next to the source file.
Optionally pass --render-png to render both variants as PNGs using cairosvg.
"""

from __future__ import annotations

import argparse
import copy
import sys
from pathlib import Path
from typing import Iterable, Sequence, Set, Tuple

from lxml import etree


SVG_NS = "http://www.w3.org/2000/svg"
NS_MAP = {"svg": SVG_NS}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Create base and lettering SVG layers.")
    parser.add_argument("input", type=Path, help="Source SVG/HTML exported from keyboard-layout-editor.")
    parser.add_argument("--output-dir", type=Path, help="Directory for generated files.")
    parser.add_argument("--prefix", help="File name prefix for generated files.")
    parser.add_argument("--render-png", action="store_true", help="Also render matching PNGs using cairosvg.")
    return parser.parse_args()


def load_svg(path: Path) -> etree._ElementTree:
    parser = etree.XMLParser(remove_blank_text=True)
    return etree.parse(str(path), parser)


def serialize(tree: etree._ElementTree) -> bytes:
    return etree.tostring(tree, encoding="utf-8", xml_declaration=True, pretty_print=True)


def find_lettering(root: etree._Element) -> Sequence[etree._Element]:
    return root.xpath(".//*[contains(concat(' ', normalize-space(@class), ' '), ' lettering ')]", namespaces=NS_MAP)


def collect_ancestors(elements: Iterable[etree._Element]) -> Set[etree._Element]:
    ancestors = set()
    for element in elements:
        parent = element.getparent()
        while parent is not None:
            ancestors.add(parent)
            parent = parent.getparent()
    return ancestors


def remove_lettering(root: etree._Element) -> None:
    for element in list(find_lettering(root)):
        parent = element.getparent()
        if parent is not None:
            parent.remove(element)


def keep_only_lettering(root: etree._Element) -> None:
    letters = set(find_lettering(root))
    ancestors = collect_ancestors(letters)
    allowed = letters | ancestors | {root}
    for element in list(root.iter()):
        if element in allowed:
            continue
        parent = element.getparent()
        if parent is not None:
            parent.remove(element)


def hide_lettering(root: etree._Element) -> None:
    remove_lettering(root)


def determine_dimensions(root: etree._Element) -> Tuple[float, float]:
    def parse_length(value: str) -> float:
        return float(value.strip().rstrip("px"))

    width = root.get("width")
    height = root.get("height")
    if width and height:
        return parse_length(width), parse_length(height)
    viewbox = root.get("viewBox")
    if viewbox:
        parts = viewbox.strip().split()
        if len(parts) == 4:
            return float(parts[2]), float(parts[3])
    raise RuntimeError("SVG lacks width/height or viewBox information.")


def write_outputs(tree: etree._ElementTree, output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    tree.write(str(output_path), encoding="utf-8", xml_declaration=True, pretty_print=True)


def render_png(svg_bytes: bytes, output_path: Path, width: float, height: float) -> None:
    try:
        import cairosvg
    except ImportError as exc:
        raise RuntimeError("cairosvg is required to render PNGs") from exc
    output_path.parent.mkdir(parents=True, exist_ok=True)
    cairosvg.svg2png(bytestring=svg_bytes, write_to=str(output_path), output_width=width, output_height=height)


def main() -> int:
    args = parse_args()
    source_path = args.input
    if not source_path.exists():
        print(f"missing input: {source_path}", file=sys.stderr)
        return 1

    tree = load_svg(source_path)
    root = tree.getroot()
    width, height = determine_dimensions(root)

    prefix = args.prefix or source_path.stem
    out_dir = args.output_dir or source_path.parent

    base_root = copy.deepcopy(root)
    hide_lettering(base_root)
    base_tree = etree.ElementTree(base_root)
    base_svg = serialize(base_tree)
    base_svg_path = out_dir / f"{prefix}_base.svg"
    write_outputs(base_tree, base_svg_path)

    lettering_root = copy.deepcopy(root)
    keep_only_lettering(lettering_root)
    lettering_tree = etree.ElementTree(lettering_root)
    lettering_svg = serialize(lettering_tree)
    lettering_svg_path = out_dir / f"{prefix}_lettering.svg"
    write_outputs(lettering_tree, lettering_svg_path)

    if args.render_png:
        render_png(base_svg, out_dir / f"{prefix}_base.png", width, height)
        render_png(lettering_svg, out_dir / f"{prefix}_lettering.png", width, height)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())