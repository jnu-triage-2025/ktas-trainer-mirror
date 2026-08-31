using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

        Assert.That(WaypointSet.TryGet("route", out var resolved), Is.True);
        Assert.That(resolved, Is.SameAs(waypointSet));
        Assert.That(resolved.Waypoints, Has.Count.EqualTo(2));
        Assert.That(resolved.Waypoints[0].Identifier, Is.EqualTo("route:0"));
        Assert.That(resolved.Waypoints[1].Identifier, Is.EqualTo("route:1"));
      }
      finally
      {
        Object.DestroyImmediate(setObject);
      }
    }

    [Test]
    public void DuplicateSetIsPromotedWhenPreviousSetIsDisabled()
    {
      var firstObject = new GameObject("First Set");
      var secondObject = new GameObject("Second Set");
      try
      {
        var first = firstObject.AddComponent<WaypointSet>();
        first.ConfigureIdentifier("overlap-route");
        var second = secondObject.AddComponent<WaypointSet>();
        LogAssert.Expect(LogType.Error, "[WaypointSet] Duplicate identifier 'overlap-route'. Registration was rejected.");
        second.ConfigureIdentifier("overlap-route");

        Assert.That(WaypointSet.TryGet("overlap-route", out var initial), Is.True);
        Assert.That(initial, Is.SameAs(first));

        first.ConfigureIdentifier("retired-route");

        Assert.That(WaypointSet.TryGet("overlap-route", out var promoted), Is.True);
        Assert.That(promoted, Is.SameAs(second));
      }
      finally
      {
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(secondObject);
      }
    }

    [Test]
    public void DuplicateAnchorIsPromotedWhenPreviousAnchorIsDisabled()
    {
      var firstObject = new GameObject("First Anchor");
      var secondObject = new GameObject("Second Anchor");
      try
      {
        var first = firstObject.AddComponent<WaypointAnchor>();
        first.ConfigureIdentifier("overlap-anchor");
        var second = secondObject.AddComponent<WaypointAnchor>();
        LogAssert.Expect(LogType.Error, "[WaypointAnchor] Duplicate identifier 'overlap-anchor'. Registration was rejected.");
        second.ConfigureIdentifier("overlap-anchor");

        Assert.That(WaypointAnchor.TryGet("overlap-anchor", out var initial), Is.True);
        Assert.That(initial, Is.SameAs(first));
        first.ConfigureIdentifier("retired-anchor");
        Assert.That(WaypointAnchor.TryGet("overlap-anchor", out var promoted), Is.True);
        Assert.That(promoted, Is.SameAs(second));
      }
      finally
      {
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(secondObject);
      }
    }
  }
}
