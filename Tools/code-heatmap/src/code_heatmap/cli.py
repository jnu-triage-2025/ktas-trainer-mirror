#!/usr/bin/env python3
"""Render a source-code treemap heatmap as a standalone SVG.

Run from the repository root, for example:
  python3 Tools/code-heatmap.py --output /tmp/code-heatmap.svg
  python3 Tools/code-heatmap.py --refs main,HEAD~10 --color-by module

The program only uses Python's standard library and Git when --refs is used.
Exit status is non-zero when input cannot be read or an SVG cannot be written.
"""

from __future__ import annotations

import argparse
import colorsys
import html
import math
import os
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable

DEFAULT_EXTENSIONS = ".cs,.csx,.json,.uxml,.uss,.shader,.hlsl,.compute,.asmdef,.asmref"
DEFAULT_EXCLUDES = ".git,Library,Temp,Logs,obj,Build,Builds,Packages"
PASTELS = ("#A8D8EA", "#F7C5CC", "#C9E4C5", "#F9D29D", "#D6CDEA", "#BFE3D0", "#F2C6DE", "#C9D7F8")


@dataclass
class SourceFile:
    path: str
    extension: str
    value: int
    module: str


@dataclass
class Node:
    name: str
    path: str
    value: int = 0
    file: SourceFile | None = None
    children: dict[str, "Node"] = field(default_factory=dict)


def csv_set(value: str) -> set[str]:
    return {item.strip().lower().lstrip(".") for item in value.split(",") if item.strip()}


def parse_colours(value: str) -> dict[str, str]:
    result: dict[str, str] = {}
    if not value:
        return result
    for item in value.split(","):
        if ":" not in item:
            raise ValueError(f"Invalid colour entry '{item}'. Use key:#RRGGBB.")
        key, colour = (part.strip() for part in item.split(":", 1))
        if not key or not colour.startswith("#") or len(colour) not in (4, 7):
            raise ValueError(f"Invalid colour entry '{item}'. Use key:#RRGGBB.")
        result[key.lower().lstrip(".")] = colour
    return result


def module_for(path: str) -> str:
    parts = Path(path).parts
    for index in range(len(parts) - 2):
        if parts[index:index + 2] == ("Assets", "Modules"):
            return parts[index + 2]
    return "(root)"


def measure(content: bytes, metric: str) -> int:
    text = content.decode("utf-8", errors="replace")
    return max(1, len(text) if metric == "characters" else len(text.splitlines()) or 1)


def current_files(root: Path, extensions: set[str], excludes: set[str], metric: str) -> list[SourceFile]:
    files: list[SourceFile] = []
    for directory, subdirs, names in os.walk(root):
        subdirs[:] = [name for name in subdirs if name not in excludes]
        for name in names:
            absolute = Path(directory, name)
            extension = absolute.suffix.lower().lstrip(".")
            if extension not in extensions:
                continue
            relative = absolute.relative_to(root).as_posix()
            try:
                files.append(SourceFile(relative, extension, measure(absolute.read_bytes(), metric), module_for(relative)))
            except OSError as error:
                print(f"warning: skipped {relative}: {error}", file=sys.stderr)
    return sorted(files, key=lambda item: item.path)


def git_files(repo: Path, ref: str, extensions: set[str], excludes: set[str], metric: str) -> list[SourceFile]:
    # Select paths first, then use cat-file's batch protocol.  This avoids both
    # one Git process per source file and archiving this Unity project's large
    # binary assets just to inspect text sources.
    listing = subprocess.run(["git", "ls-tree", "-r", "-z", ref], cwd=repo, check=True,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout
    selected: list[tuple[str, str, str]] = []
    for raw in filter(None, listing.split(b"\0")):
        metadata, raw_path = raw.split(b"\t", 1)
        mode, kind, object_id = metadata.decode("ascii").split()
        if kind != "blob":
            continue
        path = raw_path.decode("utf-8", "surrogateescape")
        extension = Path(path).suffix.lower().lstrip(".")
        if any(part in excludes for part in Path(path).parts) or extension not in extensions:
            continue
        selected.append((object_id, path, extension))
    if not selected:
        return []
    request = "".join(f"{object_id}\n" for object_id, _, _ in selected).encode("ascii")
    batch = subprocess.run(["git", "cat-file", "--batch"], cwd=repo, input=request, check=True,
                           stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout
    files: list[SourceFile] = []
    offset = 0
    for _, path, extension in selected:
        line_end = batch.index(b"\n", offset)
        header = batch[offset:line_end].decode("ascii")
        offset = line_end + 1
        fields = header.split()
        if len(fields) != 3 or fields[1] != "blob":
            print(f"warning: skipped {ref}:{path}: {header}", file=sys.stderr)
            continue
        size = int(fields[2])
        content = batch[offset:offset + size]
        offset += size + 1  # content is followed by a protocol newline
        files.append(SourceFile(path, extension, measure(content, metric), module_for(path)))
    return sorted(files, key=lambda item: item.path)


def build_tree(files: Iterable[SourceFile], depth: int) -> Node:
    root = Node("Repository", "")
    for source in files:
        parts = source.path.split("/")
        folders, filename = parts[:-1], parts[-1]
        node = root
        for index, folder in enumerate(folders):
            if index >= depth:
                filename = "/".join(parts[index:])
                break
            node = node.children.setdefault(folder, Node(folder, f"{node.path}/{folder}".strip("/")))
        leaf = node.children.setdefault(filename, Node(filename, f"{node.path}/{filename}".strip("/")))
        leaf.file = source
        leaf.value += source.value
    def total(node: Node) -> int:
        if node.children:
            node.value = sum(total(child) for child in node.children.values())
        return node.value
    total(root)
    return root


def treemap(items: list[Node], x: float, y: float, width: float, height: float) -> list[tuple[Node, float, float, float, float]]:
    """A compact slice-and-dice layout; deterministic layouts aid Git comparisons."""
    if not items or width <= 0 or height <= 0:
        return []
    total = sum(item.value for item in items) or len(items)
    horizontal = width >= height
    cursor = x if horizontal else y
    result = []
    for index, item in enumerate(items):
        fraction = item.value / total if total else 1 / len(items)
        remaining = (x + width - cursor) if horizontal else (y + height - cursor)
        size = remaining if index == len(items) - 1 else (width if horizontal else height) * fraction
        rect = (cursor, y, size, height) if horizontal else (x, cursor, width, size)
        result.append((item, *rect))
        cursor += size
    return result


def readable_text(hex_colour: str) -> str:
    colour = hex_colour.lstrip("#")
    if len(colour) == 3:
        colour = "".join(char * 2 for char in colour)
    red, green, blue = (int(colour[index:index + 2], 16) for index in (0, 2, 4))
    return "#1F2937" if red * 0.299 + green * 0.587 + blue * 0.114 > 155 else "#FFFFFF"


def fallback_colour(key: str) -> str:
    seed = sum((index + 1) * ord(char) for index, char in enumerate(key))
    hue = (seed % 360) / 360
    red, green, blue = colorsys.hls_to_rgb(hue, 0.78, 0.52)
    return "#{:02X}{:02X}{:02X}".format(round(red * 255), round(green * 255), round(blue * 255))


def svg_text(value: str, x: float, y: float, size: int, colour: str, max_width: float) -> str:
    if max_width < size * 3 or size < 7:
        return ""
    limit = max(3, int(max_width / (size * 0.58)))
    label = value if len(value) <= limit else value[:limit - 1] + "…"
    return f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" fill="{colour}">{html.escape(label)}</text>'


def render_tree(root: Node, panel_x: float, panel_y: float, panel_w: float, panel_h: float, colour_by: str,
                colours: dict[str, str], fixed_layout_total: int | None) -> list[str]:
    output: list[str] = []
    pad, header = 2.0, 15.0
    scale = (fixed_layout_total / root.value) if fixed_layout_total and root.value else 1.0
    def colour(source: SourceFile) -> str:
        key = source.extension if colour_by == "type" else source.module.lower()
        return colours.get(key, PASTELS[sum(map(ord, key)) % len(PASTELS)] if key else fallback_colour(key))
    def visit(node: Node, x: float, y: float, w: float, h: float, level: int) -> None:
        if node.file:
            fill = colour(node.file)
            output.append(f'<rect class="file" x="{x + pad:.1f}" y="{y + pad:.1f}" width="{max(0, w - 2 * pad):.1f}" height="{max(0, h - 2 * pad):.1f}" fill="{fill}"><title>{html.escape(node.file.path)} — {node.file.value:,}</title></rect>')
            output.append(svg_text(node.name, x + 5, y + 15, 10 if h > 34 else 8, readable_text(fill), w - 8))
            return
        if level:
            output.append(f'<rect class="folder" x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}"/>')
            output.append(svg_text(f"{node.name} ({node.value:,})", x + 4, y + 12, 10, "#475569", w - 8))
            y += header
            h -= header
        children = sorted(node.children.values(), key=lambda child: (-child.value, child.name.lower()))
        adjusted = [Node(child.name, child.path, max(1, round(child.value * scale)), child.file, child.children) for child in children]
        for child, cx, cy, cw, ch in treemap(adjusted, x, y, w, h):
            original = next(item for item in children if item.path == child.path)
            visit(original, cx, cy, cw, ch, level + 1)
    visit(root, panel_x, panel_y, panel_w, panel_h, 0)
    return output


def main() -> int:
    parser = argparse.ArgumentParser(description="Render a repository source-code heatmap SVG.")
    parser.add_argument("--root", default=".", help="Repository directory for current files (default: current directory).")
    parser.add_argument("--output", default="code-heatmap.svg", help="SVG output path.")
    parser.add_argument("--refs", help="Comma-separated Git refs (hashes, branches, or tags) to render side-by-side.")
    parser.add_argument("--depth", type=int, default=5, help="Folder nesting depth from root (default: 5).")
    parser.add_argument("--extensions", default=DEFAULT_EXTENSIONS, help="Comma-separated extensions to include.")
    parser.add_argument("--exclude", default=DEFAULT_EXCLUDES, help="Comma-separated directories to exclude.")
    parser.add_argument("--metric", choices=("characters", "lines"), default="characters", help="Box area metric (default: characters).")
    parser.add_argument("--color-by", choices=("type", "module"), default="type", help="Colour grouping (default: type).")
    parser.add_argument("--colors", default="", help="Inline overrides: cs:#A8D8EA,json:#F7C5CC or ModuleName:#A8D8EA.")
    parser.add_argument("--width", type=int, default=1600, help="Base SVG width in pixels (default: 1600).")
    parser.add_argument("--height", type=int, default=1000, help="Base SVG height in pixels (default: 1000).")
    parser.add_argument("--resolution-mode", choices=("proportional", "fixed"), default="proportional", help="Git panel size: reflect each ref's total size, or keep panels equal (default: proportional).")
    parser.add_argument("--fixed-layout", action="store_true", help="Keep each comparison panel's internal total area equal; compare proportions only.")
    args = parser.parse_args()
    if args.depth < 0 or args.width < 200 or args.height < 200:
        parser.error("depth must be non-negative; width and height must be at least 200.")
    try:
        colours = parse_colours(args.colors)
    except ValueError as error:
        parser.error(str(error))
    root = Path(args.root).resolve()
    extensions, excludes = csv_set(args.extensions), {item.strip() for item in args.exclude.split(",") if item.strip()}
    refs = [ref.strip() for ref in args.refs.split(",") if ref.strip()] if args.refs else []
    try:
        snapshots = [(ref, git_files(root, ref, extensions, excludes, args.metric)) for ref in refs] if refs else [("Working tree", current_files(root, extensions, excludes, args.metric))]
    except (OSError, subprocess.CalledProcessError) as error:
        detail = error.stderr.decode().strip() if isinstance(error, subprocess.CalledProcessError) and error.stderr else str(error)
        print(f"error: unable to read input: {detail}", file=sys.stderr)
        return 2
    trees = [(label, build_tree(files, args.depth)) for label, files in snapshots]
    totals = [tree.value for _, tree in trees]
    max_total = max(totals, default=1)
    columns = min(3, len(trees))
    rows = math.ceil(len(trees) / columns)
    panel_w, panel_h = args.width / columns, (args.height - 58) / rows
    body = ["<style>text{font-family:Inter,Arial,sans-serif;font-weight:600}.folder{fill:#F8FAFC;stroke:#94A3B8;stroke-width:1}.file{stroke:#FFFFFF;stroke-width:1.5}</style>", f'<rect width="100%" height="100%" fill="#FFFFFF"/>', f'<text x="24" y="30" font-size="20" fill="#1E293B">Code heatmap — {html.escape(args.metric)} · depth {args.depth} · colour by {html.escape(args.color_by)}</text>']
    layout_total = max_total if args.fixed_layout else None
    for index, (label, tree) in enumerate(trees):
        column, row = index % columns, index // columns
        x, y = column * panel_w + 12, 50 + row * panel_h + 8
        if args.resolution_mode == "proportional" and not args.fixed_layout:
            factor = math.sqrt(tree.value / max_total) if max_total else 1
            used_w, used_h = (panel_w - 24) * factor, (panel_h - 24) * factor
        else:
            used_w, used_h = panel_w - 24, panel_h - 24
        body.append(f'<text x="{x:.1f}" y="{y + 13:.1f}" font-size="12" fill="#334155">{html.escape(label)} — {tree.value:,}</text>')
        body.extend(render_tree(tree, x, y + 20, used_w, used_h - 20, args.color_by, colours, layout_total))
    legend_keys = sorted({(file.extension if args.color_by == "type" else file.module) for _, files in snapshots for file in files}, key=str.lower)
    for index, key in enumerate(legend_keys[:20]):
        value = key.lower().lstrip(".")
        fill = colours.get(value, PASTELS[sum(map(ord, value)) % len(PASTELS)])
        x = 24 + (index % 8) * 190
        y = args.height - 12 - (index // 8) * 20
        body.append(f'<rect x="{x}" y="{y - 10}" width="11" height="11" fill="{fill}"/><text x="{x + 16}" y="{y}" font-size="10" fill="#475569">{html.escape(key)}</text>')
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{args.width}" height="{args.height}" viewBox="0 0 {args.width} {args.height}">' + "".join(body) + "</svg>"
    try:
        Path(args.output).write_text(svg, encoding="utf-8")
    except OSError as error:
        print(f"error: unable to write SVG: {error}", file=sys.stderr)
        return 3
    print(f"Wrote {args.output}: {len(snapshots)} snapshot(s), {sum(len(files) for _, files in snapshots)} file(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
