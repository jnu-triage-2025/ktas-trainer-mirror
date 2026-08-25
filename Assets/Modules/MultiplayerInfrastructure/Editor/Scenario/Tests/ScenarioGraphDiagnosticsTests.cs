using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Editor;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioGraphDiagnosticsTests
  {
    [Test]
    public void ManualEnterSetupChainEndingWithoutReturnToOriginNodeIsReported()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "catch-up",
        NextIdentifier = "after"
      });
      graph.Add(new ScenarioStateUpdateNode
      {
        Identifier = "catch-up",
        TargetEntityIdentifier = "patient_b",
        StateKey = "phase",
        StateValue = "ct"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "after", DialogueContent = "after" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Warning
          && item.NodeIdentifier == "catch-up"
          && item.Message.Contains("ReturnToOrigin 노드를 두어")),
        Is.True);
    }

    [Test]
    public void ManualEnterSetupChainEndingWithReturnToOriginNodeIsAccepted()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "catch-up",
        NextIdentifier = "after"
      });
      graph.Add(new ScenarioStateUpdateNode
      {
        Identifier = "catch-up",
        TargetEntityIdentifier = "patient_b",
        StateKey = "phase",
        StateValue = "ct",
        NextIdentifier = "return-to-origin"
      });
      graph.Add(new ScenarioReturnToOriginNode { Identifier = "return-to-origin" });
      graph.Add(new ScenarioDialogueNode { Identifier = "after", DialogueContent = "after" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item => item.Message.Contains("ReturnToOrigin 노드를 두어")),
        Is.False);
    }

    [Test]
    public void ReturnToOriginReachableFromMainFlowIsReported()
    {
      var graph = new ScenarioGraph { DefaultEntrypoint = "start" };
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "start",
        DialogueContent = "start",
        NextIdentifier = "tail"
      });
      graph.Add(new ScenarioReturnToOriginNode { Identifier = "tail" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Warning
          && item.NodeIdentifier == "tail"
          && item.Message.Contains("메인 흐름에서 도달 가능")),
        Is.True);
    }

    [Test]
    public void ReturnToOriginInsideAManualEnterSetupChainIsNotReportedAsMainFlow()
    {
      var graph = new ScenarioGraph { DefaultEntrypoint = "start" };
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "start",
        DialogueContent = "start",
        NextIdentifier = "checkpoint"
      });
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "catch-up",
        NextIdentifier = "after"
      });
      graph.Add(new ScenarioStateUpdateNode
      {
        Identifier = "catch-up",
        TargetEntityIdentifier = "patient_b",
        StateKey = "phase",
        StateValue = "ct",
        NextIdentifier = "return-to-origin"
      });
      graph.Add(new ScenarioReturnToOriginNode { Identifier = "return-to-origin" });
      graph.Add(new ScenarioDialogueNode { Identifier = "after", DialogueContent = "after" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item => item.Message.Contains("메인 흐름에서 도달 가능")),
        Is.False);
    }

    [Test]
    public void ManualEnterSetupChainEndingInAChoiceIsNotReportedAsImplicitTermination()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "ask",
        NextIdentifier = "after"
      });
      graph.Add(new ScenarioChoiceNode
      {
        Identifier = "ask",
        DialogueContent = "고르세요",
        Options = new List<ScenarioChoiceOption>
        {
          new ScenarioChoiceOption { DisplayText = "A", NextNodeIdentifier = "after" }
        }
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "after", DialogueContent = "after" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item => item.Message.Contains("ReturnToOrigin 노드를 두어")),
        Is.False,
        "Choice 는 NextIdentifier 로 흐르지 않으므로 암묵적 종료로 오판하면 안 된다.");
    }

    [Test]
    public void ReturnToOriginNodeCarryingANextLinkIsReported()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioReturnToOriginNode { Identifier = "tail", NextIdentifier = "after" });
      graph.Add(new ScenarioDialogueNode { Identifier = "after", DialogueContent = "after" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Warning
          && item.NodeIdentifier == "tail"
          && item.Message.Contains("실행에 쓰이지 않습니다")),
        Is.True);
    }

    [Test]
    public void ManualEntrypointReportsMissingSetupNode()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "missing-setup"
      });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Error
          && item.NodeIdentifier == "checkpoint"
          && item.Message.Contains("manualEnterSetupIdentifier 'missing-setup'")),
        Is.True);
    }

    [Test]
    public void ManualEntrypointWithoutSetupChainWarnsAboutEntityHandleWipe()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode { Identifier = "checkpoint" });
      graph.Add(new ScenarioEntityPresetSpawnNode
      {
        Identifier = "spawn_patient",
        PresetIdentifier = "patient_b_preset",
        ResultStateKey = "patient_b.entity"
      });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Info
          && item.NodeIdentifier == "checkpoint"
          && item.Message.Contains("clear-state=true")),
        Is.True);
    }

    [Test]
    public void ManualEntrypointWithSetupChainDoesNotWarnAboutEntityHandleWipe()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "respawn_patient"
      });
      graph.Add(new ScenarioEntityPresetSpawnNode
      {
        Identifier = "respawn_patient",
        PresetIdentifier = "patient_b_preset",
        ResultStateKey = "patient_b.entity",
        NextIdentifier = "checkpoint"
      });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Any(item => item.Message.Contains("clear-state=true")),
        Is.False);
    }

    [Test]
    public void DuplicateManualEntrypointAliasesAreReported()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioManualEntrypointNode { Identifier = "first", EntrypointIdentifier = "ct-arrival" });
      graph.Add(new ScenarioManualEntrypointNode { Identifier = "second", EntrypointIdentifier = "CT-Arrival" });

      Assert.That(
        ScenarioGraphDiagnostics.Run(graph).Count(item =>
          item.Severity == ScenarioGraphDiagnostics.Severity.Error
          && item.Message.Contains("ManualEntrypoint 별칭")),
        Is.EqualTo(2));
    }

    [Test]
    public void ManualEnterSetupChainStartIsNotCountedAsAGraphEntryNode()
    {
      var graph = new ScenarioGraph();
      graph.Add(new ScenarioDialogueNode
      {
        Identifier = "start",
        DialogueContent = "start",
        NextIdentifier = "checkpoint"
      });
      graph.Add(new ScenarioManualEntrypointNode
      {
        Identifier = "checkpoint",
        ManualEnterSetupIdentifier = "catch-up"
      });
      graph.Add(new ScenarioStateUpdateNode
      {
        Identifier = "catch-up",
        StateKey = "phase",
        StateValue = "ct",
        NextIdentifier = "checkpoint"
      });

      var entryInfo = ScenarioGraphDiagnostics.Run(graph)
        .SingleOrDefault(item => item.Message.StartsWith("진입 노드가 "));

      Assert.That(entryInfo, Is.Null, "준비 체인 시작 노드는 별도 진입 노드로 보고되면 안 된다.");
    }

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
