using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioParallelRoleAllocatorTests
  {
    [Test]
    public void ConstrainedBranchIsAllocatedBeforeBroadBranch()
    {
      var broad = new ScenarioParallelBranch { Identifier = "broad" };
      var constrained = new ScenarioParallelBranch { Identifier = "constrained" };
      var branches = new[] { broad, constrained };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [broad] = new[] { 1, 2 },
        [constrained] = new[] { 1 }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var complete = ScenarioParallelRoleAllocator.TryAllocateDistinct(branches, candidates, allocation);

      Assert.That(complete, Is.True);
      Assert.That(allocation[constrained], Is.EqualTo(1));
      Assert.That(allocation[broad], Is.EqualTo(2));
    }

    [Test]
    public void ReturnsIncompleteAndRecordsNullForUnmatchedBranch()
    {
      var first = new ScenarioParallelBranch { Identifier = "first" };
      var second = new ScenarioParallelBranch { Identifier = "second" };
      var branches = new[] { first, second };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [first] = new[] { 7 },
        [second] = new[] { 7 }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var complete = ScenarioParallelRoleAllocator.TryAllocateDistinct(branches, candidates, allocation);

      Assert.That(complete, Is.False);
      Assert.That(allocation[first], Is.EqualTo(7));
      Assert.That(allocation[second], Is.Null);
    }

    [Test]
    public void ReassignsEarlierBranchWhenThatIsRequiredForCompleteMatching()
    {
      var first = new ScenarioParallelBranch { Identifier = "first" };
      var second = new ScenarioParallelBranch { Identifier = "second" };
      var third = new ScenarioParallelBranch { Identifier = "third" };
      var branches = new[] { first, second, third };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [first] = new[] { 1, 2 },
        [second] = new[] { 1, 3 },
        [third] = new[] { 1, 3 }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var complete = ScenarioParallelRoleAllocator.TryAllocateDistinct(branches, candidates, allocation);

      Assert.That(complete, Is.True);
      Assert.That(allocation[first], Is.EqualTo(2));
      Assert.That(candidates[second], Does.Contain(allocation[second].Value));
      Assert.That(candidates[third], Does.Contain(allocation[third].Value));
      Assert.That(allocation.Values, Is.Unique);
    }

    [Test]
    public void AllocatesEveryEligibleBranchToSinglePlayer()
    {
      var first = new ScenarioParallelBranch { Identifier = "first" };
      var second = new ScenarioParallelBranch { Identifier = "second" };
      var branches = new[] { first, second };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [first] = new[] { 7 },
        [second] = new[] { 7 }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var complete = ScenarioParallelRoleAllocator.TryAllocateAllToPlayer(
        branches, candidates, 7, allocation);

      Assert.That(complete, Is.True);
      Assert.That(allocation[first], Is.EqualTo(7));
      Assert.That(allocation[second], Is.EqualTo(7));
    }

    [Test]
    public void DoesNotAllocateSinglePlayerWhenAnyBranchIsIneligible()
    {
      var first = new ScenarioParallelBranch { Identifier = "first" };
      var second = new ScenarioParallelBranch { Identifier = "second" };
      var branches = new[] { first, second };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [first] = new[] { 7 },
        [second] = new[] { 8 }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>
      {
        [first] = 7,
        [second] = null
      };

      var complete = ScenarioParallelRoleAllocator.TryAllocateAllToPlayer(
        branches, candidates, 7, allocation);

      Assert.That(complete, Is.False);
      Assert.That(allocation[first], Is.EqualTo(7));
      Assert.That(allocation[second], Is.Null);
    }

    [Test]
    public void KeepsDistinctAssignmentsAndDuplicatesOnlyUnmatchedRoles()
    {
      var airway = new ScenarioParallelBranch { Identifier = "airway" };
      var circulation = new ScenarioParallelBranch { Identifier = "circulation" };
      var support = new ScenarioParallelBranch { Identifier = "support" };
      var branches = new[] { airway, circulation, support };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [airway] = new[] { 1 },
        [circulation] = new[] { 1 },
        [support] = new[] { 2 }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var complete = ScenarioParallelRoleAllocator.TryAllocateAllowingDuplicates(
        branches, candidates, allocation);

      Assert.That(complete, Is.True);
      Assert.That(allocation[airway], Is.EqualTo(1));
      Assert.That(allocation[circulation], Is.EqualTo(1));
      Assert.That(allocation[support], Is.EqualTo(2));
    }

    [Test]
    public void DoesNotAllocateWhenARequiredRoleHasNoEligiblePlayer()
    {
      var first = new ScenarioParallelBranch { Identifier = "first" };
      var second = new ScenarioParallelBranch { Identifier = "second" };
      var branches = new[] { first, second };
      var candidates = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>
      {
        [first] = new[] { 1 },
        [second] = new int[0]
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var complete = ScenarioParallelRoleAllocator.TryAllocateAllowingDuplicates(
        branches, candidates, allocation);

      Assert.That(complete, Is.False);
      Assert.That(allocation[first], Is.EqualTo(1));
      Assert.That(allocation[second], Is.Null);
    }
  }
}
