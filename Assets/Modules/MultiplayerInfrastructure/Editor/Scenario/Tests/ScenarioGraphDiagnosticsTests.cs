using System.Linq;
using MultiplayerInfrastructure.Editor;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioGraphDiagnosticsTests
  {
    [Test]
    public void SessionStartActingNpcAddsEntityPresetRegistrationInfo()
    {
      var graph = new ScenarioGraph
      {
        ActingNpcs = new[]
        {
          new ScenarioActingNpcDefinition
          {
            Identifier = "npc_doctor",
            PresetIdentifier = "npc_doctor_preset",
            SpawnOnStart = true
          }
        }
      };
      graph.Add(new ScenarioDialogueNode { Identifier = "start", DialogueContent = "start" });

      var info = ScenarioGraphDiagnostics.Run(graph).Single(item =>
        item.Severity == ScenarioGraphDiagnostics.Severity.Info
        && item.Message.Contains("npc_doctor는 세션이 시작되면 EntityPreset으로부터 로드됩니다."));

      Assert.That(info.NodeIdentifier, Is.EqualTo("(graph)"));
      Assert.That(info.Message, Does.Contain("EntityPresetRegistryRequirementsSO"));
    }

    [Test]
    public void NonSessionStartActingNpcDoesNotAddEntityPresetRegistrationInfo()
    {
      var graph = new ScenarioGraph
      {
        ActingNpcs = new[]
        {
          new ScenarioActingNpcDefinition
          {
            Identifier = "npc_later",
            PresetIdentifier = "npc_later_preset",
            SpawnOnStart = false
          }
        }
      };
      graph.Add(new ScenarioDialogueNode { Identifier = "start", DialogueContent = "start" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item => item.Message.Contains("EntityPresetRegistryRequirementsSO")),
        Is.False);
    }

    [Test]
    public void WaypointUsingNodesWarnWhenWaypointIsNotDefinedAtScenarioRoot()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "player_move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "player-waypoint"
      });
      graph.Add(new ScenarioNPCMoveNode
      {
        Identifier = "npc_move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "npc-waypoint"
      });
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "npc_control",
        Mode = ScenarioNPCControlMode.Control,
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "control-waypoint"
      });
      graph.Add(new ScenarioQuestWaypointHighlightNode
      {
        Identifier = "highlight",
        WaypointIdentifier = "highlight-waypoint"
      });

      var warnings = ScenarioGraphDiagnostics.Run(graph)
        .Where(item => item.Severity == ScenarioGraphDiagnostics.Severity.Warning
                       && item.Message.StartsWith("이 시나리오 파일에서는 "))
        .ToList();

      Assert.That(warnings.Select(item => item.NodeIdentifier), Is.EquivalentTo(new[]
      {
        "player_move",
        "npc_move",
        "npc_control",
        "highlight"
      }));
      Assert.That(warnings.Single(item => item.NodeIdentifier == "player_move").Message, Is.EqualTo(
        "이 시나리오 파일에서는 player-waypoint waypoint가 정의되지 않았습니다. " +
        "게임을 실행하기 전, 게임 시스템에 다른 방법으로 player-waypoint를 등록했는지 확인하세요."));
    }

    [Test]
    public void WaypointUsingNodeDoesNotWarnWhenWaypointIsDefinedAtScenarioRoot()
    {
      var graph = new ScenarioGraph
      {
        Waypoints = new[]
        {
          new ScenarioWaypointDefinition { Identifier = "defined-waypoint" }
        }
      };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "defined-waypoint"
      });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Message.StartsWith("이 시나리오 파일에서는 defined-waypoint waypoint가 정의되지 않았습니다.")),
        Is.False);
    }

    [Test]
    public void ValidatorFailureBranchIsNotReportedAsAnAdditionalEntryNode()
    {
      var graph = new ScenarioGraph { DefaultEntrypoint = "start" };
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "start",
        NextIdentifier = "end",
        OnFailure = ScenarioValidatorOnFailure.Branching,
        FailureNextIdentifier = "failure"
      });
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "failure",
        NextIdentifier = "end",
        DialogueContent = "failed"
      });
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "end",
        DialogueContent = "done"
      });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Info
          && item.Message.Contains("진입 노드가")),
        Is.False);
    }

    [Test]
    public void ValidatorTimeoutFailureBranchIsNotReportedAsAnAdditionalEntryNode()
    {
      var graph = new ScenarioGraph { DefaultEntrypoint = "start" };
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "start",
        NextIdentifier = "end",
        WaitForCondition = true,
        OnWaitTimeout = ScenarioValidatorWaitTimeoutBehavior.FailBranch,
        FailureNextIdentifier = "timeout"
      });
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "timeout",
        NextIdentifier = "end",
        DialogueContent = "timed out"
      });
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "end",
        DialogueContent = "done"
      });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Info
          && item.Message.Contains("진입 노드가")),
        Is.False);
    }
  }
}
