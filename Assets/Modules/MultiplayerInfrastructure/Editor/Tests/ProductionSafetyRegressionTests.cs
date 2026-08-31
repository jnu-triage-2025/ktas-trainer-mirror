using System.Collections;
using System.Reflection;
using System.Threading;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Variable;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests
{
  public sealed class ProductionSafetyRegressionTests
  {
    [TestCase("  Alice  ", "Alice")]
    [TestCase("한글 이름", "한글 이름")]
    public void DisplayNameNormalizationTrimsValidNames(string input, string expected)
    {
      Assert.That(UserDescriptorService.TryNormalizeDisplayName(input, out string normalized, out _), Is.True);
      Assert.That(normalized, Is.EqualTo(expected));
    }

    [Test]
    public void DisplayNameNormalizationRejectsControlCharactersAndOversizeNames()
    {
      Assert.That(UserDescriptorService.TryNormalizeDisplayName("Alice\nAdmin", out _, out _), Is.False);
      Assert.That(UserDescriptorService.TryNormalizeDisplayName(
        new string('a', UserDescriptorService.MaxDisplayNameLength + 1), out _, out _), Is.False);
    }

    [Test]
    public void DisplayNameLookupUsesStableIdentifierTieBreakForLegacyDuplicates()
    {
      const string laterIdentifier = "zzzzzzzz-0000-0000-0000-000000000000";
      const string earlierIdentifier = "aaaaaaaa-0000-0000-0000-000000000000";
      UserDescriptorService.Register(7, new UserDescriptor(laterIdentifier, "duplicate"));
      UserDescriptorService.Register(8, new UserDescriptor(earlierIdentifier, "duplicate"));
      try
      {
        Assert.That(UserDescriptorService.TryGetByDisplayName("duplicate", out var result), Is.True);
        Assert.That(result.Identifier, Is.EqualTo(earlierIdentifier));
      }
      finally
      {
        UserDescriptorService.Unregister(laterIdentifier);
        UserDescriptorService.Unregister(earlierIdentifier);
      }
    }

    [Test]
    public void ClearSessionStateRemovesObjectivesAndScores()
    {
      var objectives = (IDictionary)typeof(SessionVariableService)
        .GetField("_objectives", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
      var scores = (IDictionary)typeof(SessionVariableService)
        .GetField("_scoresByObjective", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
      objectives.Add("old", new SessionVariableService.ObjectiveDefinition("old", "dummy"));
      scores.Add("old", new System.Collections.Generic.Dictionary<string, int> { ["player"] = 5 });

      SessionVariableService.ClearSessionState();

      Assert.That(objectives.Count, Is.Zero);
      Assert.That(scores.Count, Is.Zero);
    }

    [Test]
    public void LanDiscoveryStopToleratesAlreadyDisposedCancellationSource()
    {
      var gameObject = new GameObject("LAN Discovery Test");
      try
      {
        var service = gameObject.AddComponent<LanDiscoveryService>();
        var cancellation = new CancellationTokenSource();
        cancellation.Dispose();
        typeof(LanDiscoveryService)
          .GetField("_listenCts", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(service, cancellation);

        Assert.DoesNotThrow(service.StopDiscovery);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }
}
