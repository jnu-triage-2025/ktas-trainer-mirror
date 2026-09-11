using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests
{
  public sealed class PlayerCharacterModelAssignmentServiceTests
  {
    [SetUp]
    public void SetUp()
    {
      PlayerCharacterModelAssignmentService.ConfigureCandidates(null);
      PlayerCharacterModelAssignmentService.ClearAll();
    }

    [TearDown]
    public void TearDown()
    {
      PlayerCharacterModelAssignmentService.ConfigureCandidates(null);
      PlayerCharacterModelAssignmentService.ClearAll();
    }

    [Test]
    public void BuiltInCandidatesMatchTheRequestedCharacterSet()
    {
      Assert.That(
        PlayerCharacterModelAssignmentService.BuiltInCandidateIdentifiers,
        Is.EquivalentTo(new[] { "emma", "ethan", "liam", "lisa", "maya", "olivia", "sofia" }));
    }

    [Test]
    public void AssignPicksFromTheCandidatePool()
    {
      string assigned = PlayerCharacterModelAssignmentService.Assign(1);

      Assert.That(PlayerCharacterModelAssignmentService.BuiltInCandidateIdentifiers, Contains.Item(assigned));
    }

    [Test]
    public void AssignIsIdempotentWhileTheClientRemainsConnected()
    {
      string first = PlayerCharacterModelAssignmentService.Assign(7);
      string second = PlayerCharacterModelAssignmentService.Assign(7);

      Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void ConcurrentClientsDoNotShareACharacterWhileCandidatesRemain()
    {
      int candidateCount = PlayerCharacterModelAssignmentService.CandidateIdentifiers.Count;
      var assigned = new List<string>(candidateCount);

      for (int clientId = 0; clientId < candidateCount; clientId++)
        assigned.Add(PlayerCharacterModelAssignmentService.Assign(clientId));

      Assert.That(assigned, Is.Unique);
      Assert.That(assigned, Is.EquivalentTo(PlayerCharacterModelAssignmentService.CandidateIdentifiers));
    }

    [Test]
    public void AssignStillSucceedsWhenMorePlayersJoinThanCandidatesExist()
    {
      int candidateCount = PlayerCharacterModelAssignmentService.CandidateIdentifiers.Count;
      for (int clientId = 0; clientId < candidateCount; clientId++)
        PlayerCharacterModelAssignmentService.Assign(clientId);

      string overflow = PlayerCharacterModelAssignmentService.Assign(candidateCount);

      Assert.That(PlayerCharacterModelAssignmentService.CandidateIdentifiers, Contains.Item(overflow));
    }

    [Test]
    public void ReleaseReturnsTheCharacterToThePoolForTheNextJoiner()
    {
      int candidateCount = PlayerCharacterModelAssignmentService.CandidateIdentifiers.Count;
      for (int clientId = 0; clientId < candidateCount; clientId++)
        PlayerCharacterModelAssignmentService.Assign(clientId);

      string released = PlayerCharacterModelAssignmentService.Assign(0);
      PlayerCharacterModelAssignmentService.Release(0);

      Assert.That(PlayerCharacterModelAssignmentService.Assign(candidateCount), Is.EqualTo(released));
    }

    [Test]
    public void ClearAllDropsAssignmentsFromThePreviousServerSession()
    {
      int candidateCount = PlayerCharacterModelAssignmentService.CandidateIdentifiers.Count;
      for (int clientId = 0; clientId < candidateCount; clientId++)
        PlayerCharacterModelAssignmentService.Assign(clientId);

      PlayerCharacterModelAssignmentService.ClearAll();

      Assert.That(PlayerCharacterModelAssignmentService.TryGetAssignedIdentifier(0, out _), Is.False);

      var reassigned = new List<string>(candidateCount);
      for (int clientId = 0; clientId < candidateCount; clientId++)
        reassigned.Add(PlayerCharacterModelAssignmentService.Assign(clientId));

      Assert.That(reassigned, Is.Unique);
    }

    [Test]
    public void ManualModelChangeFreesThePreviouslyHeldCandidate()
    {
      int candidateCount = PlayerCharacterModelAssignmentService.CandidateIdentifiers.Count;
      for (int clientId = 0; clientId < candidateCount; clientId++)
        PlayerCharacterModelAssignmentService.Assign(clientId);

      string freed = PlayerCharacterModelAssignmentService.Assign(0);
      PlayerCharacterModelAssignmentService.NotifyIdentifierApplied(0, "jeb");

      Assert.That(PlayerCharacterModelAssignmentService.Assign(candidateCount), Is.EqualTo(freed));
    }

    [Test]
    public void ConfigureCandidatesReplacesThePoolAndIgnoresBlankEntries()
    {
      PlayerCharacterModelAssignmentService.ConfigureCandidates(new[] { "maya", " ", "maya", "sofia" });

      Assert.That(PlayerCharacterModelAssignmentService.CandidateIdentifiers, Is.EqualTo(new[] { "maya", "sofia" }));
    }

    [Test]
    public void ConfigureCandidatesFallsBackToTheBuiltInPoolWhenEmpty()
    {
      PlayerCharacterModelAssignmentService.ConfigureCandidates(new string[0]);

      Assert.That(
        PlayerCharacterModelAssignmentService.CandidateIdentifiers,
        Is.EqualTo(PlayerCharacterModelAssignmentService.BuiltInCandidateIdentifiers));
    }
  }
}
