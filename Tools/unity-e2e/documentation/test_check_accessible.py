"""Run: python3 -m unittest discover -s Tools/unity-e2e/documentation -p 'test_*.py'."""
import unittest
from check_accessible import find_path, load_regions


def rectangle(i, xmin, xmax, zmin, zmax):
    return dict(transformId=i, xMin=xmin, xMax=xmax, zMin=zmin, zMax=zmax)


class PathTests(unittest.TestCase):
    def assert_contained(self, regions, result):
        self.assertEqual(result['status'], 'ok')
        self.assertEqual(len(result['segmentRegionIds']), len(result['path']) - 1)
        by_id = {r['transformId']: r for r in regions}
        for a, b, identifier in zip(result['path'], result['path'][1:], result['segmentRegionIds']):
            r = by_id[identifier]
            # Both endpoints in a convex rectangle prove full segment containment.
            for x, z in (a, b):
                self.assertTrue(r['xMin'] <= x <= r['xMax'])
                self.assertTrue(r['zMin'] <= z <= r['zMax'])

    def test_l_shaped_detour(self):
        regions = [rectangle(1, 0, 4, 0, 1), rectangle(2, 3, 4, 0, 4)]
        result = find_path(regions, (0, .5), (3.5, 4))
        self.assert_contained(regions, result)
        self.assertGreater(len(result['path']), 2)
        self.assertEqual(result['path'][0], (0, .5))
        self.assertEqual(result['path'][-1], (3.5, 4))

    def test_tiny_gap_is_not_connected(self):
        regions = [rectangle(1, 0, 1, 0, 1), rectangle(2, 1 + 1e-10, 2, 0, 1)]
        self.assertEqual(find_path(regions, (.5, .5), (1.5, .5))['status'], 'disconnected')

    def test_touching_corner(self):
        regions = [rectangle(1, 0, 1, 0, 1), rectangle(2, 1, 2, 1, 2)]
        self.assert_contained(regions, find_path(regions, (0, 0), (2, 2)))

    def test_same_point_and_outside(self):
        regions = [rectangle(1, 0, 1, 0, 1)]
        self.assertEqual(find_path(regions, (0, 0), (0, 0))['path'], [(0, 0)])
        self.assertEqual(find_path(regions, (0, 0), (2, 2))['status'], 'outside')

    def test_scene_route(self):
        regions = load_regions()
        self.assert_contained(regions, find_path(regions, (-68, -20.95), (-76.86, -20.91)))
        self.assertEqual(find_path(regions, (-72.73, -4.7), (-72.73, -4.5))['status'], 'disconnected')


if __name__ == '__main__':
    unittest.main()
