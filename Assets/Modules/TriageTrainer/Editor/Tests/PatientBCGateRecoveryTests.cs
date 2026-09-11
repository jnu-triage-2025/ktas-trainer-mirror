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
    public void DisasterIntroGivesNurseBAndNurseCIndependentEquivalentBranches()
    {
      var path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/disaster_intro.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
      var parallel = (ScenarioParallelNode)graph.Nodes["P001"];

      Assert.That(parallel.Branches, Has.Count.EqualTo(4));
      Assert.That(parallel.Branches.SelectMany(branch => branch.RequiredPlayerTags),
        Is.EqualTo(new[] { "nurse_a", "nurse_b", "nurse_c", "nurse_d" }));
      Assert.That(parallel.Branches.Select(branch => branch.Identifier), Is.Unique,
        "역할별 분기는 서로 다른 시작 노드를 사용해야 참가자 항목이 겹치지 않습니다.");
      Assert.That(parallel.Branches.Select(branch => branch.CompletionConditionIdentifier), Is.Unique,
        "역할별 분기는 서로 다른 완료 조건을 사용해야 네 명의 완료를 각각 기다릴 수 있습니다.");

      var allocation = parallel.Branches
        .Select((branch, index) => (branch, clientId: (int?)index + 1))
        .ToDictionary(pair => pair.branch, pair => pair.clientId);
      var groupGate = ScenarioGroupGateTracker.TryCreate(graph.Identifier, parallel, allocation);
      Assert.That(groupGate, Is.Not.Null);
      var snapshot = groupGate.BuildSnapshot();
      Assert.That(snapshot.Participants, Has.Count.EqualTo(4));
      Assert.That(snapshot.Participants.Select(participant => participant.Role),
        Is.EqualTo(new[] { "nurse_a", "nurse_b", "nurse_c", "nurse_d" }));

      var nurseB = parallel.Branches.Single(branch => branch.RequiredPlayerTags.Contains("nurse_b"));
      var nurseC = parallel.Branches.Single(branch => branch.RequiredPlayerTags.Contains("nurse_c"));
      Assert.That(nurseC.Identifier, Is.Not.EqualTo(nurseB.Identifier));
      Assert.That(nurseC.CompletionConditionIdentifier, Is.Not.EqualTo(nurseB.CompletionConditionIdentifier));

      var nurseBDialogue = (ScenarioDialogueNode)graph.Nodes[nurseB.Identifier];
      var nurseCDialogue = (ScenarioDialogueNode)graph.Nodes[nurseC.Identifier];
      Assert.That(nurseCDialogue.DialogueContent, Is.EqualTo(nurseBDialogue.DialogueContent));
      Assert.That(nurseCDialogue.DialogueContentTTSPassing, Is.EqualTo(nurseBDialogue.DialogueContentTTSPassing));
    }

    [Test]
    public void DisasterIntroPreparesPatientsBetweenArrivalDialogues()
    {
      var path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/disaster_intro.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
      var preparationNodes = new System.Collections.Generic.List<IScenarioNode>();
      var visited = new System.Collections.Generic.HashSet<string>();
      string cursor = graph.Nodes["D003"].NextIdentifier;
      while (!string.IsNullOrWhiteSpace(cursor) && visited.Add(cursor))
      {
        if (cursor == "D003_1")
          break;
        var node = graph.Nodes[cursor];
        preparationNodes.Add(node);
        cursor = node.NextIdentifier;
      }

      Assert.That(cursor, Is.EqualTo("D003_1"));
      foreach (var node in graph.Nodes.Values.Where(node =>
                 node is ScenarioEntityPresetSpawnNode
                 || node is ScenarioPatientMedicalStatePresetNode
                 || node is ScenarioEntityStateSignalBindingNode))
        Assert.That(preparationNodes, Does.Contain(node), node.Identifier);

      Assert.That(preparationNodes.OfType<ScenarioEntityPresetSpawnNode>()
        .Select(node => node.SpawnedEntityIdentifier),
        Is.EquivalentTo(new[] { "patient_a", "patient_dummy_d_a" }));
      foreach (var spawn in preparationNodes.OfType<ScenarioEntityPresetSpawnNode>())
      {
        var preset = preparationNodes.OfType<ScenarioPatientMedicalStatePresetNode>()
          .Single(node => node.TargetEntityIdentifier == spawn.SpawnedEntityIdentifier);
        Assert.That(preparationNodes.IndexOf(spawn), Is.LessThan(preparationNodes.IndexOf(preset)));
      }

      Assert.That(graph.Nodes["D003"].NextIdentifier, Is.EqualTo("DISASTER_INTRO_SPAWN_A"));
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
