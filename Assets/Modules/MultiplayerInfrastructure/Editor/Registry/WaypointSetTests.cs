using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Waypoints
{
  public sealed class WaypointSetTests
  {
    [Test]
    public void WaypointSetReturnsConfiguredWaypointsInListOrder()
    {
      var setObject = new GameObject("Waypoint Set");
      var first = new GameObject("First");
      var second = new GameObject("Second");
      try
      {
        first.transform.SetParent(setObject.transform, false);
        second.transform.SetParent(setObject.transform, false);
        var firstAnchor = first.AddComponent<WaypointAnchor>();
        firstAnchor.ConfigureIdentifier("route:0");
        var secondAnchor = second.AddComponent<WaypointAnchor>();
        secondAnchor.ConfigureIdentifier("route:1");
        var waypointSet = setObject.AddComponent<WaypointSet>();
        waypointSet.ConfigureIdentifier("route");
        waypointSet.ConfigureWaypoints(new[] { firstAnchor, secondAnchor });

        second.transform.SetSiblingIndex(0);

        Assert.That(WaypointSet.TryGet("route", out var resolved), Is.SameAs(waypointSet));
        Assert.That(resolved.Waypoints, Has.Count.EqualTo(2));
        Assert.That(resolved.Waypoints[0].Identifier, Is.EqualTo("route:0"));
        Assert.That(resolved.Waypoints[1].Identifier, Is.EqualTo("route:1"));
      }
      finally
      {
        Object.DestroyImmediate(setObject);
      }
    }
  }
}
