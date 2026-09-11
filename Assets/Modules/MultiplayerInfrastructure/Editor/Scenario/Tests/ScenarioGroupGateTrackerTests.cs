using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioGroupGateTrackerTests
  {
    private static ScenarioParallelBranch CreateBranch(string identifier, string role)
    {
      return new ScenarioParallelBranch
      {
        Identifier = identifier,
        RequiredPlayerTags = role == null ? new List<string>() : new List<string> { role }
      };
    }

    private static ScenarioParallelNode CreateNode(ScenarioWaitMode waitMode, params ScenarioParallelBranch[] branches)
    {
      return new ScenarioParallelNode
      {
        Identifier = "P001",
        WaitMode = waitMode,
        AllocationType = ScenarioParallelAllocationType.ByRole,
        Branches = new List<ScenarioParallelBranch>(branches)
      };
    }

    [Test]
    public void TryCreateReturnsNullWhenOnlyOneClientIsAssigned()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 1 };

      Assert.That(ScenarioGroupGateTracker.TryCreate("graph", node, allocation), Is.Null);
    }

    [Test]
    public void TryCreateReturnsNullUnlessWaitModeIsAll()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.Any, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 2 };

      Assert.That(ScenarioGroupGateTracker.TryCreate("graph", node, allocation), Is.Null);
    }

    [Test]
    public void QuestOperationsTrackRemainingQuestsPerParticipant()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 2 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);
      var snapshots = new List<ScenarioGroupGateSnapshot>();
      tracker.Changed = snapshots.Add;

      var participant = tracker.GetParticipant(a);
      participant.RecordQuestOperation(ScenarioQuestOperationType.Add, "quest-1");
      participant.RecordQuestOperation(ScenarioQuestOperationType.Add, "quest-2");
      participant.RecordQuestOperation(ScenarioQuestOperationType.Add, "quest-2");
      participant.RecordQuestOperation(ScenarioQuestOperationType.Remove, "quest-1");
      participant.RecordQuestOperation(ScenarioQuestOperationType.Remove, "quest-missing");

      Assert.That(snapshots, Has.Count.EqualTo(3), "실제로 바뀐 발행/회수만 스냅샷을 내보내야 합니다.");
      var latest = snapshots[^1];
      Assert.That(latest.Active, Is.True);
      Assert.That(latest.GraphIdentifier, Is.EqualTo("graph"));
      Assert.That(latest.ParallelNodeIdentifier, Is.EqualTo("P001"));
      Assert.That(latest.Participants, Has.Count.EqualTo(2));
      Assert.That(latest.Participants[0].ClientId, Is.EqualTo(1));
      Assert.That(latest.Participants[0].QuestIds, Is.EqualTo(new[] { "quest-2" }));
      Assert.That(latest.Participants[1].QuestIds, Is.Empty);
    }

    [Test]
    public void CompletionAggregatesBranchesOfTheSameClient()
    {
      var a = CreateBranch("A", "nurse_a");
      var a2 = CreateBranch("A2", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, a2, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [a2] = 1, [b] = 2 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);

      Assert.That(tracker.BuildSnapshot().Participants, Has.Count.EqualTo(2), "같은 담당자의 분기는 한 참여자로 합쳐야 합니다.");

      tracker.GetParticipant(a).MarkCompleted(left: false);
      Assert.That(tracker.BuildSnapshot().Participants[0].Completed, Is.False);

      tracker.GetParticipant(a2).MarkCompleted(left: false);
      var snapshot = tracker.BuildSnapshot();
      Assert.That(snapshot.Participants[0].Completed, Is.True);
      Assert.That(snapshot.Participants[0].Left, Is.False);
      Assert.That(snapshot.Participants[1].Completed, Is.False);
    }

    [Test]
    public void LeftBranchIsReportedAsLeftAndCompleted()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 2 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);

      tracker.GetParticipant(b).MarkCompleted(left: true);

      var participant = tracker.BuildSnapshot().Participants[1];
      Assert.That(participant.Completed, Is.True);
      Assert.That(participant.Left, Is.True);
    }

    [Test]
    public void MarkClientCompletedMarksEveryBranchOfThatClientOnce()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var b2 = CreateBranch("B2", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, b, b2);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 2, [b2] = 2 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);
      var snapshots = new List<ScenarioGroupGateSnapshot>();
      tracker.Changed = snapshots.Add;

      Assert.That(tracker.MarkClientCompleted(99, left: false), Is.False, "게이트에 없는 클라이언트는 무시해야 합니다.");
      Assert.That(tracker.MarkClientCompleted(2, left: false), Is.True);
      Assert.That(tracker.MarkClientCompleted(2, left: false), Is.False, "같은 완료를 되풀이해도 스냅샷을 다시 내보내지 않아야 합니다.");

      Assert.That(snapshots, Has.Count.EqualTo(1));
      var participants = snapshots[0].Participants;
      Assert.That(participants[0].Completed, Is.False);
      Assert.That(participants[1].ClientId, Is.EqualTo(2));
      Assert.That(participants[1].Completed, Is.True, "원격 참여자의 분기를 모두 완료로 표시해야 합니다.");
      Assert.That(participants[1].Left, Is.False);
    }

    [Test]
    public void MarkClientCompletedUpgradesLeftToCompletedButNotTheReverse()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 2 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);

      Assert.That(tracker.MarkClientCompleted(2, left: true), Is.True);
      Assert.That(tracker.BuildSnapshot().Participants[1].Left, Is.True);

      Assert.That(tracker.MarkClientCompleted(2, left: false), Is.True, "실제 완료 보고는 이탈 표시를 완료로 바꿔야 합니다.");
      Assert.That(tracker.BuildSnapshot().Participants[1].Left, Is.False);

      Assert.That(tracker.MarkClientCompleted(2, left: true), Is.False, "완료한 참여자는 이탈 알림으로 되돌리지 않아야 합니다.");
      Assert.That(tracker.BuildSnapshot().Participants[1].Left, Is.False);
    }

    [Test]
    public void CloseEmitsInactiveSnapshotAndIgnoresLaterChanges()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", "nurse_b");
      var node = CreateNode(ScenarioWaitMode.All, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 2 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);
      var snapshots = new List<ScenarioGroupGateSnapshot>();
      tracker.Changed = snapshots.Add;

      tracker.Close();
      tracker.Close();
      tracker.GetParticipant(a).MarkCompleted(left: false);

      Assert.That(tracker.IsClosed, Is.True);
      Assert.That(snapshots, Has.Count.EqualTo(1));
      Assert.That(snapshots[0].Active, Is.False);
    }

    [Test]
    public void DisplayNameFallsBackToRoleThenClientNumber()
    {
      var a = CreateBranch("A", "nurse_a");
      var b = CreateBranch("B", null);
      var node = CreateNode(ScenarioWaitMode.All, a, b);
      var allocation = new Dictionary<ScenarioParallelBranch, int?> { [a] = 1, [b] = 3 };
      var tracker = ScenarioGroupGateTracker.TryCreate("graph", node, allocation);
      tracker.DisplayNameResolver = clientId => clientId == 1 ? "  간호사 A  " : null;

      var snapshot = tracker.BuildSnapshot();

      Assert.That(snapshot.Participants[0].DisplayName, Is.EqualTo("간호사 A"));
      Assert.That(snapshot.Participants[0].Role, Is.EqualTo("nurse_a"));
      Assert.That(snapshot.Participants[1].DisplayName, Is.EqualTo("플레이어 3"));

      tracker.DisplayNameResolver = null;
      Assert.That(tracker.BuildSnapshot().Participants[0].DisplayName, Is.EqualTo("nurse_a"));
    }
  }
}
