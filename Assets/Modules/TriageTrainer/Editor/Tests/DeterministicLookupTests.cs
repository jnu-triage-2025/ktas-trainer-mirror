using System;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TriageTrainer.Tests
{
  public sealed class DeterministicLookupTests
  {
    [Test]
    public void DuplicateWaypointAnchorDoesNotReplaceOrUnregisterOriginal()
    {
      string identifier = $"test:waypoint:{Guid.NewGuid():N}";
      var firstObject = new GameObject("first-waypoint");
      var duplicateObject = new GameObject("duplicate-waypoint");
      try
      {
        var first = firstObject.AddComponent<WaypointAnchor>();
        first.ConfigureIdentifier(identifier);
        var duplicate = duplicateObject.AddComponent<WaypointAnchor>();
        LogAssert.Expect(LogType.Error, $"[WaypointAnchor] Duplicate identifier '{identifier}'. Registration was rejected.");
        duplicate.ConfigureIdentifier(identifier);

        Assert.That(WaypointAnchor.TryGet(identifier, out var registered), Is.True);
        Assert.That(registered, Is.SameAs(first));

        duplicateObject.SetActive(false);
        Assert.That(WaypointAnchor.TryGet(identifier, out registered), Is.True);
        Assert.That(registered, Is.SameAs(first));
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(duplicateObject);
        UnityEngine.Object.DestroyImmediate(firstObject);
      }
    }

    [Test]
    public void DuplicateWaypointSetDoesNotReplaceOriginal()
    {
      string identifier = $"test:waypoint-set:{Guid.NewGuid():N}";
      var firstObject = new GameObject("first-waypoint-set");
      var duplicateObject = new GameObject("duplicate-waypoint-set");
      try
      {
        var first = firstObject.AddComponent<WaypointSet>();
        first.ConfigureIdentifier(identifier);
        var duplicate = duplicateObject.AddComponent<WaypointSet>();
        LogAssert.Expect(LogType.Error, $"[WaypointSet] Duplicate identifier '{identifier}'. Registration was rejected.");
        duplicate.ConfigureIdentifier(identifier);

        Assert.That(WaypointSet.TryGet(identifier, out var registered), Is.True);
        Assert.That(registered, Is.SameAs(first));
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(duplicateObject);
        UnityEngine.Object.DestroyImmediate(firstObject);
      }
    }
  }
}
