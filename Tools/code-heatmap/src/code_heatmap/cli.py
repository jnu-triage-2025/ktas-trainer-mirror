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
from datetime import datetime, timedelta
import fnmatch
import html
import math
import os
import re
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

DEFAULT_EXTENSIONS = ".cs,.csx,.json,.uxml,.uss,.shader,.hlsl,.compute,.asmdef,.asmref"
DEFAULT_EXCLUDES = ".git,Library,Temp,Logs,obj,Build,Builds,Packages"
DEFAULT_IGNORE_CONFIG = Path(__file__).resolve().parents[2] / "code-heatmap.ignore"
PASTELS = ("#A8D8EA", "#F7C5CC", "#C9E4C5", "#F9D29D", "#D6CDEA", "#BFE3D0", "#F2C6DE", "#C9D7F8")
DATE_REF_PATTERN = re.compile(r"^(\d{4})-(\d{2})-(\d{2})(?:-(\d{1,2})(?:-(\d{1,2})(?:-(\d{1,2}))?)?)?$")
COLOUR_PATTERN = re.compile(r"^#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?$")


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


class DateQueryError(ValueError):
    """A date: snapshot spec cannot be resolved to a commit."""


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
        if not key or not COLOUR_PATTERN.fullmatch(colour):
            raise ValueError(f"Invalid colour entry '{item}'. Use key:#RRGGBB.")
        result[key.lower().lstrip(".")] = colour
    return result


def load_ignore_patterns(path: Path) -> list[str]:
    """Read local glob exclusions, falling back to its tracked .template defaults."""
    if not path.is_file():
        template = path.with_name(path.name + ".template")
        if not template.is_file():
            return []
        path = template
    try:
        return [line.strip() for line in path.read_text(encoding="utf-8").splitlines()
                if line.strip() and not line.lstrip().startswith("#")]
    except OSError as error:
        raise OSError(f"unable to read ignore config {path}: {error}") from error


def is_ignored(path: str, patterns: Iterable[str]) -> bool:
    parts = Path(path).parts
    return any(fnmatch.fnmatchcase(path, pattern) or any(fnmatch.fnmatchcase(part, pattern) for part in parts)
               for pattern in patterns)


def local_timezone_name() -> str:
    """Find an IANA zone from macOS/Linux localtime, with UTC as a safe fallback."""
    configured = os.environ.get("TZ")
    if configured:
        return configured.removeprefix(":")
    try:
        resolved = os.path.realpath("/etc/localtime")
        marker = "/zoneinfo/"
        if marker in resolved:
            return resolved.split(marker, 1)[1]
    except OSError:
        pass
    return "UTC"


def timezone_for(value: str) -> ZoneInfo:
    try:
        return ZoneInfo(value)
    except ZoneInfoNotFoundError as error:
        raise DateQueryError(f"unknown IANA timezone '{value}'") from error


def parse_date_range(value: str, timezone: str | ZoneInfo | None = None) -> tuple[int, int]:
    """Turn a date expression in an IANA timezone into a half-open Unix-time range."""
    match = DATE_REF_PATTERN.fullmatch(value)
    if not match:
        raise DateQueryError("date refs must be date:YYYY-MM-DD[-h[-m[-s]]].")
    year, month, day, hour, minute, second = (int(part) if part is not None else None for part in match.groups())
    try:
        zone = timezone_for(timezone) if isinstance(timezone, str) else timezone or timezone_for(local_timezone_name())
        start = datetime(year, month, day, hour or 0, minute or 0, second or 0, tzinfo=zone)
    except ValueError as error:
        raise DateQueryError(f"invalid date ref '{value}': {error}") from error
    delta = timedelta(days=1) if hour is None else timedelta(hours=1) if minute is None else timedelta(minutes=1) if second is None else timedelta(seconds=1)
    return int(start.timestamp()), int((start + delta).timestamp())


def select_date_commit(entries: list[tuple[str, int]], start: int, end: int, multiple: str, none: str) -> str:
    """Select one commit from a date range, or seek immediately forward/backward."""
    matches = sorted((entry for entry in entries if start <= entry[1] < end), key=lambda entry: (entry[1], entry[0]))
    if matches:
        if multiple == "latest":
            return matches[-1][0]
        if multiple == "oldest":
            return matches[0][0]
        return matches[len(matches) // 2][0]
    if none in ("fast-forward", "ff"):
        forward = [entry for entry in entries if entry[1] >= end]
        if forward:
            return min(forward, key=lambda entry: (entry[1], entry[0]))[0]
    else:
        backward = [entry for entry in entries if entry[1] < start]
        if backward:
            return max(backward, key=lambda entry: (entry[1], entry[0]))[0]
    direction = "after" if none in ("fast-forward", "ff") else "before"
    raise DateQueryError(f"no commit exists on or {direction} the requested date range.")


def resolve_date_ref(repo: Path, spec: str, multiple: str, none: str, timezone: str) -> tuple[str, str]:
    """Resolve date:... against all local and remote refs by committer timestamp."""
    value = spec.removeprefix("date:")
    start, end = parse_date_range(value, timezone)
    log = subprocess.run(["git", "log", "--all", "--format=%H%x00%ct"], cwd=repo, check=True,
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout.decode("ascii", "strict")
    entries = [(commit, int(timestamp)) for line in log.splitlines() if line for commit, timestamp in [line.split("\0", 1)]]
    commit = select_date_commit(entries, start, end, multiple, none)
    return commit, f"{spec} → {commit[:12]}"


def module_for(path: str) -> str:
    parts = Path(path).parts
    for index in range(len(parts) - 2):
        if parts[index:index + 2] == ("Assets", "Modules"):
            return parts[index + 2]
    return "(root)"


def measure(content: bytes, metric: str) -> int:
    text = content.decode("utf-8", errors="replace")
    return max(1, len(text) if metric == "characters" else len(text.splitlines()) or 1)


def current_files(root: Path, extensions: set[str], excludes: set[str], ignore_patterns: list[str], metric: str) -> list[SourceFile]:
    files: list[SourceFile] = []
    for directory, subdirs, names in os.walk(root):
        relative_directory = Path(directory).relative_to(root)
        subdirs[:] = [name for name in subdirs if name not in excludes and not is_ignored((relative_directory / name).as_posix(), ignore_patterns)]
        for name in names:
            absolute = Path(directory, name)
            extension = absolute.suffix.lower().lstrip(".")
            if extension not in extensions:
                continue
            relative = absolute.relative_to(root).as_posix()
            if is_ignored(relative, ignore_patterns):
                continue
            try:
                files.append(SourceFile(relative, extension, measure(absolute.read_bytes(), metric), module_for(relative)))
            except OSError as error:
                print(f"warning: skipped {relative}: {error}", file=sys.stderr)
    return sorted(files, key=lambda item: item.path)


def git_files(repo: Path, ref: str, extensions: set[str], excludes: set[str], ignore_patterns: list[str], metric: str) -> list[SourceFile]:
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
        if any(part in excludes for part in Path(path).parts) or is_ignored(path, ignore_patterns) or extension not in extensions:
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
    """Lay out weighted nodes with the Bruls–Huizing–van Wijk squarify heuristic."""
    if not items or width <= 0 or height <= 0:
        return []
    total = sum(item.value for item in items) or len(items)
    weighted = [(item, max(0.0, item.value / total * width * height)) for item in sorted(items, key=lambda item: (-item.value, item.name.lower()))]
    result: list[tuple[Node, float, float, float, float]] = []

    def worst(row: list[tuple[Node, float]], side: float) -> float:
        if not row or side <= 0:
            return float("inf")
        areas = [area for _, area in row]
        area_sum = sum(areas)
        return max((side * side * max(areas)) / (area_sum * area_sum),
                   (area_sum * area_sum) / (side * side * min(areas)))

    def place_row(row: list[tuple[Node, float]], rx: float, ry: float, rw: float, rh: float) -> tuple[float, float, float, float]:
        area_sum = sum(area for _, area in row)
        # Build a row along the *short* side.  This is the essential part of
        # squarify: using the long side here degenerates into thin stripes.
        if rw >= rh:
            row_width = area_sum / rh
            cursor = ry
            for item, area in row:
                item_height = area / row_width
                result.append((item, rx, cursor, row_width, item_height))
                cursor += item_height
            return rx + row_width, ry, max(0.0, rw - row_width), rh
        row_height = area_sum / rw
        cursor = rx
        for item, area in row:
            item_width = area / row_height
            result.append((item, cursor, ry, item_width, row_height))
            cursor += item_width
        return rx, ry + row_height, rw, max(0.0, rh - row_height)

    row: list[tuple[Node, float]] = []
    rx, ry, rw, rh = x, y, width, height
    for item, area in weighted:
        candidate = row + [(item, area)]
        if not row or worst(candidate, min(rw, rh)) <= worst(row, min(rw, rh)):
            row = candidate
            continue
        rx, ry, rw, rh = place_row(row, rx, ry, rw, rh)
        row = [(item, area)]
    if row:
        place_row(row, rx, ry, rw, rh)
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
                colours: dict[str, str]) -> list[str]:
    output: list[str] = []
    # Keep hierarchy readable without consuming an entire small child rectangle
    # with nested headers.  Leaf rectangles retain their full area at small sizes.
    outer_pad, header = 1.0, 15.0
    minimum_group_side, minimum_group_area = 5.0, 64.0
    def colour(source: SourceFile) -> str:
        key = source.extension if colour_by == "type" else source.module.lower()
        return colours.get(key, PASTELS[sum(map(ord, key)) % len(PASTELS)] if key else fallback_colour(key))
    def descendant_count(node: Node) -> int:
        if node.file:
            return 1
        return sum(descendant_count(child) for child in node.children.values())

    def aggregate(node: Node, x: float, y: float, w: float, h: float) -> None:
        """Represent a too-small folder without emitting unusable zero-size leaves."""
        output.append(f'<rect class="aggregate" x="{x:.1f}" y="{y:.1f}" width="{max(0, w):.1f}" height="{max(0, h):.1f}"><title>{html.escape(node.path)} — {node.value:,} across {descendant_count(node):,} files</title></rect>')

    def visit(node: Node, x: float, y: float, w: float, h: float, level: int) -> None:
        if w <= 0 or h <= 0:
            return
        if node.file:
            fill = colour(node.file)
            pad = outer_pad if min(w, h) >= outer_pad * 2 + 1 else 0
            output.append(f'<rect class="file" x="{x + pad:.1f}" y="{y + pad:.1f}" width="{max(0, w - 2 * pad):.1f}" height="{max(0, h - 2 * pad):.1f}" fill="{fill}"><title>{html.escape(node.file.path)} — {node.file.value:,}</title></rect>')
            output.append(svg_text(node.name, x + 5, y + 15, 10 if h > 34 else 8, readable_text(fill), w - 8))
            return
        if min(w, h) < minimum_group_side or w * h < minimum_group_area:
            aggregate(node, x, y, w, h)
            return
        show_header = level and w >= 80 and h >= header * 2 + 8
        if show_header:
            output.append(f'<rect class="folder" x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}"/>')
            output.append(svg_text(f"{node.name} ({node.value:,})", x + 4, y + 12, 10, "#475569", w - 8))
            y += header
            h -= header
        else:
            # A border preserves the folder boundary; the content keeps almost
            # all of the available rectangle if there is no room for a label.
            output.append(f'<rect class="folder" x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}"/>')
            x += outer_pad
            y += outer_pad
            w -= outer_pad * 2
            h -= outer_pad * 2
        if w <= 0 or h <= 0:
            return
        children = sorted(node.children.values(), key=lambda child: (-child.value, child.name.lower()))
        for child, cx, cy, cw, ch in treemap(children, x, y, w, h):
            visit(child, cx, cy, cw, ch, level + 1)
    visit(root, panel_x, panel_y, panel_w, panel_h, 0)
    return output


def main() -> int:
    parser = argparse.ArgumentParser(description="Render a repository source-code heatmap SVG.")
    parser.add_argument("--root", default=".", help="Repository directory for current files (default: current directory).")
    parser.add_argument("--output", default="code-heatmap.svg", help="SVG output path.")
    parser.add_argument("--refs", help="Comma-separated Git refs (hashes, branches, or tags) to render side-by-side.")
    parser.add_argument("--select-date-query-result-is-multiple", choices=("latest", "oldest", "median"), default="latest", help="Commit selected when a date: ref matches multiple commits (default: latest).")
    parser.add_argument("--select-date-query-result-is-none", choices=("fast-forward", "ff", "rewind", "rw"), default="fast-forward", help="Fallback when a date: ref has no match (default: fast-forward).")
    parser.add_argument("--timezone", default=local_timezone_name(), help="IANA timezone for date: refs (default: system local timezone).")
    parser.add_argument("--depth", type=int, default=4, help="Folder nesting depth from root (default: 4).")
    parser.add_argument("--extensions", default=DEFAULT_EXTENSIONS, help="Comma-separated extensions to include.")
    parser.add_argument("--exclude", default=DEFAULT_EXCLUDES, help="Comma-separated directories to exclude.")
    parser.add_argument("--ignore-config", type=Path, default=DEFAULT_IGNORE_CONFIG, help="Optional newline-separated glob exclusions (default: project code-heatmap.ignore).")
    parser.add_argument("--metric", choices=("characters", "lines"), default="characters", help="Box area metric (default: characters).")
    parser.add_argument("--color-by", choices=("type", "module"), default="type", help="Colour grouping (default: type).")
    parser.add_argument("--colors", default="", help="Inline overrides: cs:#A8D8EA,json:#F7C5CC or ModuleName:#A8D8EA.")
    parser.add_argument("--width", type=int, default=1600, help="Base SVG width in pixels (default: 1600).")
    parser.add_argument("--height", type=int, default=1000, help="Base SVG height in pixels (default: 1000).")
    parser.add_argument("--resolution-mode", choices=("proportional", "fixed"), default="proportional", help="Git panel size: reflect each ref's total size, or keep panels equal (default: proportional).")
    args = parser.parse_args()
    if args.depth < 0 or args.width < 200 or args.height < 200:
        parser.error("depth must be non-negative; width and height must be at least 200.")
    try:
        colours = parse_colours(args.colors)
    except ValueError as error:
        parser.error(str(error))
    try:
        timezone_for(args.timezone)
    except DateQueryError as error:
        parser.error(str(error))
    root = Path(args.root).resolve()
    extensions, excludes = csv_set(args.extensions), {item.strip() for item in args.exclude.split(",") if item.strip()}
    try:
        ignore_patterns = load_ignore_patterns(args.ignore_config)
    except OSError as error:
        print(f"error: {error}", file=sys.stderr)
        return 2
    refs = [ref.strip() for ref in args.refs.split(",") if ref.strip()] if args.refs else []
    try:
        resolved_refs = [resolve_date_ref(root, ref, args.select_date_query_result_is_multiple, args.select_date_query_result_is_none, args.timezone) if ref.startswith("date:") else (ref, ref) for ref in refs]
        snapshots = [(label, git_files(root, target, extensions, excludes, ignore_patterns, args.metric)) for target, label in resolved_refs] if refs else [("Working tree", current_files(root, extensions, excludes, ignore_patterns, args.metric))]
    except (OSError, subprocess.CalledProcessError, DateQueryError) as error:
        detail = error.stderr.decode().strip() if isinstance(error, subprocess.CalledProcessError) and error.stderr else str(error)
        print(f"error: unable to read input: {detail}", file=sys.stderr)
        return 2
    trees = [(label, build_tree(files, args.depth)) for label, files in snapshots]
    totals = [tree.value for _, tree in trees]
    max_total = max(totals, default=1)
    columns = min(3, len(trees))
    rows = math.ceil(len(trees) / columns)
    panel_w, panel_h = args.width / columns, (args.height - 58) / rows
    body = ["<style>text{font-family:Inter,Arial,sans-serif;font-weight:600}.folder{fill:#F8FAFC;stroke:#94A3B8;stroke-width:1}.file{stroke:#FFFFFF;stroke-width:0.5}.aggregate{fill:#CBD5E1;stroke:#FFFFFF;stroke-width:0.5}</style>", f'<rect width="100%" height="100%" fill="#FFFFFF"/>', f'<text x="24" y="30" font-size="20" fill="#1E293B">Code heatmap — {html.escape(args.metric)} · depth {args.depth} · colour by {html.escape(args.color_by)}</text>']
    for index, (label, tree) in enumerate(trees):
        column, row = index % columns, index // columns
        x, y = column * panel_w + 12, 50 + row * panel_h + 8
        if args.resolution_mode == "proportional":
            factor = math.sqrt(tree.value / max_total) if max_total else 1
            used_w, used_h = (panel_w - 24) * factor, (panel_h - 24) * factor
        else:
            used_w, used_h = panel_w - 24, panel_h - 24
        body.append(f'<text x="{x:.1f}" y="{y + 13:.1f}" font-size="12" fill="#334155">{html.escape(label)} — {tree.value:,}</text>')
        body.extend(render_tree(tree, x, y + 20, used_w, used_h - 20, args.color_by, colours))
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
