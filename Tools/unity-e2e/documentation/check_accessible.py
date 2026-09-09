#!/usr/bin/env python3
"""Check an XZ point against Cube markers in OverworldSceneMarked (stdlib only)."""
import argparse
import heapq
import json
import math
from pathlib import Path
import re
import sys

DEFAULT_SCENE = Path(__file__).resolve().parents[3] / 'Assets/Scenes/OverworldSceneMarked.unity'


def field(block, name):
    match = re.search(r'^  ' + re.escape(name) + r': (.*)$', block, re.M)
    if not match:
        raise ValueError(f'Missing {name}')
    return match[1]


def reference(block, name):
    return int(re.fullmatch(r'\{fileID: (\d+)\}', field(block, name))[1])


def vector(block, name):
    result = {k: float(v) for k, v in re.findall(r'([xyzw]): ([^,}]+)', field(block, name))}
    if not all(math.isfinite(v) for v in result.values()):
        raise ValueError(f'Non-finite {name}')
    return result


def load_regions(scene=DEFAULT_SCENE):
    """Read plain Unity Cube transforms; reject unsupported geometry, never guess."""
    objects, transforms, meshes = {}, {}, {}
    for match in re.finditer(r'^--- !u!(\d+) &(\d+)([^\n]*)\n(.*?)(?=^--- !u!|\Z)',
                             Path(scene).read_text(encoding='utf-8-sig'), re.M | re.S):
        kind, file_id, suffix, block = match.groups()
        if suffix.strip():
            continue
        if kind == '1':
            objects[int(file_id)] = field(block, 'm_Name')
        elif kind == '4':
            transforms[int(file_id)] = block
        elif kind == '33':
            meshes[reference(block, 'm_GameObject')] = field(block, 'm_Mesh')
    roots = [i for i, b in transforms.items()
             if objects.get(reference(b, 'm_GameObject')) == 'Accessibles']
    if len(roots) != 1:
        raise ValueError(f'Expected one Accessibles root; found {len(roots)}')

    def chain(i):
        result = []
        while i:
            if i in result or i not in transforms:
                raise ValueError(f'Unresolved or cyclic Transform {i}')
            result.append(i)
            i = reference(transforms[i], 'm_Father')
        return result

    regions = []
    for i, block in transforms.items():
        # Only walk descendants here; unrelated prefab hierarchies need not resolve.
        ancestry, current = [], i
        while current and current in transforms and current not in ancestry:
            ancestry.append(current)
            if current == roots[0]:
                break
            current = reference(transforms[current], 'm_Father')
        if i == roots[0] or roots[0] not in ancestry:
            continue
        go = reference(block, 'm_GameObject')
        if go not in meshes:
            continue  # Allow empty grouping objects.
        if meshes[go] != '{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}':
            raise ValueError(f'Transform {i}: expected built-in unit Cube mesh')
        low, high = [-0.5] * 3, [0.5] * 3
        for ancestor in chain(i):
            b = transforms[ancestor]
            q = vector(b, 'm_LocalRotation')
            if any(abs(q[c]) > 1e-8 for c in 'xyz') or abs(abs(q['w']) - 1) > 1e-8:
                raise ValueError(f'Transform {ancestor}: rotated markers are unsupported')
            position, scale = vector(b, 'm_LocalPosition'), vector(b, 'm_LocalScale')
            for axis, c in enumerate('xyz'):
                a, z = low[axis] * scale[c] + position[c], high[axis] * scale[c] + position[c]
                low[axis], high[axis] = min(a, z), max(a, z)
        if low[0] == high[0] or low[2] == high[2]:
            raise ValueError(f'Transform {i}: degenerate XZ region')
        regions.append({'transformId': i, 'name': objects[go],
                        'xMin': low[0], 'xMax': high[0], 'zMin': low[2], 'zMax': high[2]})
    if not regions:
        raise ValueError('No Cube markers found below Accessibles')
    return sorted(regions, key=lambda r: r['transformId'])


def contains(region, x, z):
    return (region['xMin'] - 1e-9 <= x <= region['xMax'] + 1e-9
            and region['zMin'] - 1e-9 <= z <= region['zMax'] + 1e-9)


def find_path(regions, start, end):
    """Dijkstra over overlap centers; each edge lies in a single convex rectangle.

    This finds a connected route, not a globally shortest geometric path.
    Membership here is exact so the point-query tolerance cannot bridge gaps.
    """
    def membership(point):
        x, z = point
        return {i for i, r in enumerate(regions)
                if r['xMin'] <= x <= r['xMax'] and r['zMin'] <= z <= r['zMax']}

    start_members, end_members = membership(start), membership(end)
    if not start_members or not end_members:
        return {'status': 'outside', 'startInside': bool(start_members),
                'endInside': bool(end_members), 'path': []}
    points = [tuple(start), tuple(end)]
    memberships = [start_members, end_members]
    for i, a in enumerate(regions):
        for j in range(i + 1, len(regions)):
            b = regions[j]
            xmin, xmax = max(a['xMin'], b['xMin']), min(a['xMax'], b['xMax'])
            zmin, zmax = max(a['zMin'], b['zMin']), min(a['zMax'], b['zMax'])
            if xmin <= xmax and zmin <= zmax:
                points.append((xmin + (xmax - xmin) / 2, zmin + (zmax - zmin) / 2))
                memberships.append({i, j})
    distances, previous = {0: 0.0}, {}
    queue = [(0.0, 0)]
    while queue:
        distance, current = heapq.heappop(queue)
        if distance != distances[current]:
            continue
        if current == 1:
            indices = [1]
            while indices[-1] != 0:
                indices.append(previous[indices[-1]])
            indices.reverse()
            path, segment_regions = [points[0]], []
            for a, b in zip(indices, indices[1:]):
                if points[b] == path[-1]:
                    continue
                region_index = min(memberships[a] & memberships[b])
                segment_regions.append(regions[region_index]['transformId'])
                path.append(points[b])
            return {'status': 'ok', 'startInside': True, 'endInside': True,
                    'path': path, 'length': distance,
                    'segmentRegionIds': segment_regions}
        for target in range(len(points)):
            if target == current or not memberships[current] & memberships[target]:
                continue
            candidate = distance + math.dist(points[current], points[target])
            if candidate < distances.get(target, math.inf):
                distances[target], previous[target] = candidate, current
                heapq.heappush(queue, (candidate, target))
    return {'status': 'disconnected', 'startInside': True, 'endInside': True, 'path': []}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('x', type=float)
    parser.add_argument('z', type=float)
    parser.add_argument('end_x', type=float, nargs='?')
    parser.add_argument('end_z', type=float, nargs='?')
    parser.add_argument('--scene', type=Path, default=DEFAULT_SCENE)
    args = parser.parse_args()
    if (args.end_x is None) != (args.end_z is None):
        parser.error('provide both end_x and end_z')
    if not all(math.isfinite(v) for v in (args.x, args.z, args.end_x, args.end_z) if v is not None):
        parser.error('x and z must be finite numbers')
    try:
        regions = load_regions(args.scene)
    except (OSError, ValueError, KeyError, TypeError) as error:
        print(json.dumps({'error': str(error)}), file=sys.stderr)
        return 2
    if args.end_x is not None:
        result = find_path(regions, (args.x, args.z), (args.end_x, args.end_z))
        result.update(scene=str(args.scene.resolve()), regionCount=len(regions), coordinateOrder=['x', 'z'])
        print(json.dumps(result, ensure_ascii=False))
        return 0 if result['status'] == 'ok' else 1
    matches = [r for r in regions if contains(r, args.x, args.z)]
    print(json.dumps({'inside': bool(matches), 'x': args.x, 'z': args.z,
                      'scene': str(args.scene.resolve()), 'regionCount': len(regions),
                      'matches': matches}, ensure_ascii=False))
    return 0 if matches else 1


if __name__ == '__main__':
    sys.exit(main())
