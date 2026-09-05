using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Quest
{
  public sealed class QuestManagerGroupWaitTests
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

    private static ScenarioGroupGateSnapshot CreateSnapshot(bool localCompleted, bool otherCompleted)
    {
      return new ScenarioGroupGateSnapshot
      {
        GraphIdentifier = "graph",
        ParallelNodeIdentifier = "P001",
        Participants = new List<ScenarioGroupGateParticipantSnapshot>
        {
          new() { ClientId = 1, DisplayName = "플레이어 a", Completed = localCompleted, QuestIds = new List<string> { "quest-a" } },
          new() { ClientId = 2, DisplayName = "플레이어 b", Completed = otherCompleted, QuestIds = new List<string> { "quest-b" } }
        }
      };
    }

    [Test]
    public void SnapshotsCarryGroupWaitStatusForLocalParticipantQuest()
    {
      var gameObject = new GameObject("quest-group-wait-status-test");
      var manager = gameObject.AddComponent<QuestManager>();

      try
      {
        manager.AddOrUpdateQuest(new QuestData { Id = "quest-a", Title = "A", QuestContent = "환자 A 활력 측정", IsTracked = true });
        ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));

        var quests = manager.Quests;
        Assert.That(quests, Has.Count.EqualTo(1), "실제 분기 퀘스트가 남아 있으면 자리 표시 퀘스트를 합성하지 않아야 합니다.");
        var quest = quests[0];
        Assert.That(quest.GroupWait, Is.Not.Null);
        Assert.That(quest.GroupWait.IsWaitingForOthers, Is.True);
        Assert.That(QuestPreviewHudElement.GetCurrentObjective(quest), Is.EqualTo(ExpectedWaitingText));

        var tracked = manager.TrackedQuests;
        Assert.That(tracked, Has.Count.EqualTo(1));
        Assert.That(tracked[0].GroupWait?.WaitingDisplayText, Is.EqualTo(ExpectedWaitingText));
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void ObjectiveStaysOnOwnTaskWhileLocalParticipantIsStillInProgress()
    {
      var gameObject = new GameObject("quest-group-wait-in-progress-test");
      var manager = gameObject.AddComponent<QuestManager>();

      try
      {
        manager.AddOrUpdateQuest(new QuestData { Id = "quest-a", Title = "A", QuestContent = "환자 A 활력 측정", IsTracked = true });
        ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: false, otherCompleted: true));

        var quest = manager.Quests.Single(each => each.Id == "quest-a");
        Assert.That(quest.GroupWait, Is.Not.Null, "참여자 목록은 내 몫이 끝나기 전에도 보여야 합니다.");
        Assert.That(quest.GroupWait.IsWaitingForOthers, Is.False);
        Assert.That(QuestPreviewHudElement.GetCurrentObjective(quest), Is.EqualTo("환자 A 활력 측정"));
        Assert.That(manager.Quests.Any(each => each.IsGroupWaitPlaceholder), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void PlaceholderAppearsWhenBranchQuestsWereRemovedWhileWaiting()
    {
      var gameObject = new GameObject("quest-group-wait-placeholder-test");
      var manager = gameObject.AddComponent<QuestManager>();

      try
      {
        ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));

        var quests = manager.Quests;
        Assert.That(quests, Has.Count.EqualTo(1));
        var placeholder = quests[0];
        Assert.That(placeholder.IsGroupWaitPlaceholder, Is.True);
        Assert.That(placeholder.Id, Is.EqualTo("group-wait::graph/P001"));
        Assert.That(placeholder.IsTracked, Is.True);
        Assert.That(placeholder.IsTrackable, Is.False);
        Assert.That(placeholder.SourceScenarioIdentifier, Is.EqualTo("graph"));
        Assert.That(QuestPreviewHudElement.GetCurrentObjective(placeholder), Is.EqualTo(ExpectedWaitingText));
        Assert.That(manager.TrackedQuests.Select(each => each.Id), Is.EqualTo(new[] { "group-wait::graph/P001" }));

        // 게이트가 열리면 자리 표시 퀘스트도 사라진다.
        ScenarioGroupGateState.Apply(new ScenarioGroupGateSnapshot
        {
          GraphIdentifier = "graph",
          ParallelNodeIdentifier = "P001",
          Active = false
        });
        Assert.That(manager.Quests, Is.Empty);
        Assert.That(manager.TrackedQuests, Is.Empty);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void GroupGateChangeNotifiesListAndTrackedListeners()
    {
      var gameObject = new GameObject("quest-group-wait-notify-test");
      var manager = gameObject.AddComponent<QuestManager>();
      int listChanged = 0;
      int trackedChanged = 0;
      manager.OnQuestListChanged += _ => listChanged++;
      manager.OnTrackedQuestsChanged += _ => trackedChanged++;

      try
      {
        ScenarioGroupGateState.Apply(CreateSnapshot(localCompleted: true, otherCompleted: false));

        Assert.That(listChanged, Is.EqualTo(1));
        Assert.That(trackedChanged, Is.EqualTo(1));
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void ParticipantRowsUseStrikethroughForCompletedAndLeft()
    {
      Assert.That(
        QuestPanelElement.FormatParticipantRow(new QuestGroupWaitParticipant { DisplayName = "플레이어 a", Completed = true }),
        Is.EqualTo("<s>플레이어 a</s> 완료함"));
      Assert.That(
        QuestPanelElement.FormatParticipantRow(new QuestGroupWaitParticipant { DisplayName = "플레이어 b" }),
        Is.EqualTo("플레이어 b"));
      Assert.That(
        QuestPanelElement.FormatParticipantRow(new QuestGroupWaitParticipant { DisplayName = "플레이어 c", Completed = true, IsLocal = true }),
        Is.EqualTo("<s>플레이어 c (나)</s> 완료함"));
      Assert.That(
        QuestPanelElement.FormatParticipantRow(new QuestGroupWaitParticipant { Role = "nurse_d", Completed = true, Left = true }),
        Is.EqualTo("<s>nurse_d</s> 이탈함"));
      Assert.That(
        QuestPanelElement.FormatParticipantRow(new QuestGroupWaitParticipant { ClientId = 7 }),
        Is.EqualTo("플레이어 7"));
    }
  }
}
