using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioGroupGateStateTests
  {
    private const string ExpectedWaitingText = "다른 플레이어가 완료할 때까지 기다리기(1/2)";

    [SetUp]
    public void SetUp()
    {
      ScenarioGroupGateState.ClearAll();
      ScenarioGroupGateState.LocalClientIdProvider = () => 1;
    }

    [TearDown]
    public void TearDown()
    {
      ScenarioGroupGateState.ClearAll();
      ScenarioGroupGateState.ResetLocalClientIdProvider();
    }

    private static ScenarioGroupGateSnapshot CreateSnapshot(bool localCompleted, bool otherCompleted, bool otherLeft = false)
    {
      return new ScenarioGroupGateSnapshot
      {
        GraphIdentifier = "graph",
        ParallelNodeIdentifier = "P001",
        Participants = new List<ScenarioGroupGateParticipantSnapshot>
        {
          new()
          {
            ClientId = 1,
            DisplayName = "플레이어 a",
            Role = "nurse_a",
            Completed = localCompleted,
            QuestIds = new List<string> { "quest-a" }
          },
          new()
          {
            ClientId = 2,
            DisplayName = "플레이어 b",
            Role = "nurse_b",
            Completed = otherCompleted,
            Left = otherLeft,
            QuestIds = new List<string> { "quest-b" }
          }
        }
      };
    }

    [Test]
    public void LocalCompletedWhileOthersPendingReportsWaitingText()
    {
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));

      var status = ScenarioGroupGateState.GetStatusForQuest("quest-a");

      Assert.That(status, Is.Not.Null);
      Assert.That(status.IsWaitingForOthers, Is.True);
      Assert.That(status.CompletedCount, Is.EqualTo(1));
      Assert.That(status.TotalCount, Is.EqualTo(2));
      Assert.That(status.WaitingDisplayText, Is.EqualTo(ExpectedWaitingText));
      Assert.That(status.Participants[0].IsLocal, Is.True);
      Assert.That(status.Participants[0].Completed, Is.True);
      Assert.That(status.Participants[1].IsLocal, Is.False);
      Assert.That(status.Participants[1].Completed, Is.False);
      Assert.That(status.LocalQuestIds, Is.EqualTo(new[] { "quest-a" }));
    }

    [Test]
    public void OtherParticipantsQuestIsNotAttributedToLocalClient()
    {
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));

      Assert.That(ScenarioGroupGateState.GetStatusForQuest("quest-b"), Is.Null);
    }

    [Test]
    public void LocalStillInProgressIsNotWaiting()
    {
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: false, otherCompleted: true));

      var status = ScenarioGroupGateState.GetStatusForQuest("quest-a");

      Assert.That(status, Is.Not.Null);
      Assert.That(status.IsWaitingForOthers, Is.False);
      Assert.That(status.LocalParticipantCompleted, Is.False);
      Assert.That(status.CompletedCount, Is.EqualTo(1));
      Assert.That(ScenarioGroupGateState.GetLocalWaitingStatuses(), Is.Empty);
    }

    [Test]
    public void LeftParticipantCountsTowardCompletion()
    {
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false, otherLeft: true));

      var status = ScenarioGroupGateState.GetStatusForQuest("quest-a");

      Assert.That(status.AllCompleted, Is.True);
      Assert.That(status.IsWaitingForOthers, Is.False);
      Assert.That(status.Participants[1].Left, Is.True);
      Assert.That(status.Participants[1].CountsAsCompleted, Is.True);
    }

    [Test]
    public void InactiveSnapshotRemovesGate()
    {
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));
      ScenarioGroupGateState.Apply(new ScenarioGroupGateSnapshot
      {
        GraphIdentifier = "graph",
        ParallelNodeIdentifier = "P001",
        Active = false
      });

      Assert.That(ScenarioGroupGateState.GetStatusForQuest("quest-a"), Is.Null);
      Assert.That(ScenarioGroupGateState.ActiveGates, Is.Empty);
    }

    [Test]
    public void JsonRoundTripPreservesParticipants()
    {
      string json = ScenarioGroupGateState.Serialize(CreateSnapshot(localCompleted: true, otherCompleted: false));

      Assert.That(ScenarioGroupGateState.TryApplyJson(json), Is.True);
      Assert.That(ScenarioGroupGateState.TryGet("graph", "P001", out var applied), Is.True);
      Assert.That(applied.Participants, Has.Count.EqualTo(2));
      Assert.That(applied.Participants[0].DisplayName, Is.EqualTo("플레이어 a"));
      Assert.That(applied.Participants[0].Role, Is.EqualTo("nurse_a"));
      Assert.That(applied.Participants[0].QuestIds, Is.EqualTo(new[] { "quest-a" }));
      Assert.That(applied.Participants[1].Completed, Is.False);
    }

    [Test]
    public void MalformedJsonIsIgnored()
    {
      UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
      try
      {
        Assert.That(ScenarioGroupGateState.TryApplyJson("{ not json"), Is.False);
        Assert.That(ScenarioGroupGateState.ActiveGates, Is.Empty);
      }
      finally
      {
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
      }
    }

    [Test]
    public void ChangedFiresOnApplyAndClearGraphOnly()
    {
      int fired = 0;
      void Handler() => fired++;
      ScenarioGroupGateState.Changed += Handler;
      try
      {
        ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: false, otherCompleted: false));
        ScenarioGroupGateState.ClearGraph("other-graph");
        ScenarioGroupGateState.ClearGraph("graph");
        ScenarioGroupGateState.ClearGraph("graph");
      }
      finally
      {
        ScenarioGroupGateState.Changed -= Handler;
      }

      Assert.That(fired, Is.EqualTo(2), "적용 1회와 실제로 지운 ClearGraph 1회만 알려야 합니다.");
    }

    [Test]
    public void GetLocalWaitingStatusesListsOnlyGatesWhereLocalIsWaiting()
    {
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));
      var otherGate = CreateSnapshot(localCompleted: false, otherCompleted: true);
      otherGate.ParallelNodeIdentifier = "P002";
      ScenarioGroupGateState.Apply(otherGate);

      var waiting = ScenarioGroupGateState.GetLocalWaitingStatuses();

      Assert.That(waiting, Has.Count.EqualTo(1));
      Assert.That(waiting[0].GateIdentifier, Is.EqualTo("graph/P001"));
      Assert.That(waiting[0].PlaceholderQuestId, Is.EqualTo("group-wait::graph/P001"));
    }

    [Test]
    public void WithoutLocalClientNothingIsWaiting()
    {
      ScenarioGroupGateState.LocalClientIdProvider = () => null;
      ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));

      Assert.That(ScenarioGroupGateState.GetStatusForQuest("quest-a"), Is.Null);
      Assert.That(ScenarioGroupGateState.GetLocalWaitingStatuses(), Is.Empty);
    }
  }
}
