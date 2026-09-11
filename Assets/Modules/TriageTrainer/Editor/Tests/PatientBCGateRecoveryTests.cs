using System.IO;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientBCGateRecoveryTests
  {
    [Test]
    public void DisasterIntroPreparesPatientsBeforeRoleSpecificExecution()
    {
      var path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/disaster_intro.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
      var commonNodes = new System.Collections.Generic.List<IScenarioNode>();
      var visited = new System.Collections.Generic.HashSet<string>();
      string cursor = graph.DefaultEntrypoint;
      while (!string.IsNullOrWhiteSpace(cursor) && visited.Add(cursor))
      {
        var node = graph.Nodes[cursor];
        if (node is ScenarioChoiceNode || node is ScenarioParallelNode)
          break;
        commonNodes.Add(node);
        cursor = node.NextIdentifier;
      }

      Assert.That(cursor, Is.EqualTo("C_role_select"));
      // A remote nurse_a cannot execute server-only setup. Every host role must reach it
      // before role selection, including the medical presets and triage signal bindings.
      foreach (var node in graph.Nodes.Values.Where(node =>
                 node is ScenarioEntityPresetSpawnNode
                 || node is ScenarioPatientMedicalStatePresetNode
                 || node is ScenarioEntityStateSignalBindingNode))
        Assert.That(commonNodes, Does.Contain(node), node.Identifier);

      Assert.That(commonNodes.OfType<ScenarioEntityPresetSpawnNode>()
        .Select(node => node.SpawnedEntityIdentifier),
        Is.EquivalentTo(new[] { "patient_a", "patient_dummy_d_a" }));
      foreach (var spawn in commonNodes.OfType<ScenarioEntityPresetSpawnNode>())
      {
        var preset = commonNodes.OfType<ScenarioPatientMedicalStatePresetNode>()
          .Single(node => node.TargetEntityIdentifier == spawn.SpawnedEntityIdentifier);
        Assert.That(commonNodes.IndexOf(spawn), Is.LessThan(commonNodes.IndexOf(preset)));
      }

      Assert.That(graph.Nodes["D003"].NextIdentifier, Is.EqualTo("E001"));
      Assert.That(graph.Nodes["E001"].NextIdentifier, Is.EqualTo("D003_1"));
    }

    [TestCase("patient_b_c_ct")]
    [TestCase("patient_a_critical")]
    [TestCase("disaster_intro")]
    [TestCase("tutorial")]
    public void CollaborativeWaitGatesForceAdvanceAfterTheirTimeout(string scenario)
    {
      var path = Path.Combine(Application.dataPath,
        $"Modules/TriageTrainer/Resources/Scenario/{scenario}.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
      var gates = graph.Nodes.Values
        .OfType<ScenarioValidatorNode>()
        .Where(node => node.WaitForCondition)
        .ToArray();

      Assert.That(gates, Is.Not.Empty);
      Assert.That(gates.All(node => node.WaitTimeoutSeconds is > 0f), Is.True,
        "협업 게이트에는 무기한 대기를 방지하는 양수 타임아웃이 필요합니다.");
      // ForceAdvance 는 nextIdentifier 로, FailBranch 는 실제로 존재하는 failureNextIdentifier 로 진행해야 한다.
      // (patient_b_c_ct 의 분류 제출 게이트는 미제출을 정답으로 보지 않기 위해 FailBranch 를 쓴다.)
      Assert.That(gates.All(node =>
          node.OnWaitTimeout == ScenarioValidatorWaitTimeoutBehavior.ForceAdvance
          || (node.OnWaitTimeout == ScenarioValidatorWaitTimeoutBehavior.FailBranch
              && !string.IsNullOrWhiteSpace(node.FailureNextIdentifier)
              && graph.Nodes.ContainsKey(node.FailureNextIdentifier))), Is.True,
        "신호 누락·권한 거부·담당자 이탈 이후에도 다른 참여자의 진행을 막지 않아야 합니다.");
    }

    /// <summary>
    /// 선택지에 응답이 없으면 런타임은 nextIdentifier, 없으면 첫 선택지의 경로로 복구한다. 그 복구 경로가
    /// 오답 재시도 대사를 거쳐 같은 선택지로 되돌아오면, 응답하지 않는 담당자는 영원히 맴돌고 나머지 인원은
    /// 다음 합류점에서 그 담당자를 기다린다. 모든 선택지의 복구 경로는 앞으로 나아가야 한다.
    /// </summary>
    [TestCase("patient_b_c_ct")]
    [TestCase("patient_a_critical")]
    [TestCase("disaster_intro")]
    [TestCase("tutorial")]
    public void UnansweredChoiceRecoveryNeverLoopsBackToTheSameChoice(string scenario)
    {
      var path = Path.Combine(Application.dataPath,
        $"Modules/TriageTrainer/Resources/Scenario/{scenario}.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
      var looping = new System.Collections.Generic.List<string>();
      foreach (var choice in graph.Nodes.Values.OfType<ScenarioChoiceNode>())
      {
        string recovery = !string.IsNullOrWhiteSpace(choice.NextIdentifier)
          ? choice.NextIdentifier
          : choice.Options?.FirstOrDefault(option => !string.IsNullOrWhiteSpace(option.NextNodeIdentifier))?.NextNodeIdentifier;
        var visited = new System.Collections.Generic.HashSet<string>();
        string cursor = recovery;
        while (!string.IsNullOrWhiteSpace(cursor) && visited.Add(cursor) && graph.Nodes.TryGetValue(cursor, out var node))
        {
          if (node is ScenarioChoiceNode || node is ScenarioQuizNode || node is ScenarioParallelNode)
            break;
          cursor = node.NextIdentifier;
        }
        if (string.Equals(cursor, choice.Identifier, System.StringComparison.Ordinal))
          looping.Add(choice.Identifier);
      }

      Assert.That(looping, Is.Empty,
        "응답 없는 선택지의 복구 경로가 같은 선택지로 되돌아옵니다. 해당 Choice 에 nextIdentifier 를 지정하세요.");
    }
  }
}
