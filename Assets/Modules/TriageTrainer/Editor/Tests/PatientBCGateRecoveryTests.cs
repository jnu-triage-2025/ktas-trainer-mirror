using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Quest;
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

    [Test]
    public void DisasterIntroTriageInteractionsShowPatientBriefingsAndAdvanceFromSubmissions()
    {
      var path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/disaster_intro.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      var expectations = new[]
      {
        (Patient: "patient_a", DistinctText: "흉부 관통상", Enable: "DISASTER_INTRO_TRIAGE_ENABLE_A",
          Wait: "DISASTER_INTRO_TRIAGE_WAIT_A", Evaluate: "DISASTER_INTRO_TRIAGE_EVALUATE_A", CorrectNext: "N001_3"),
        (Patient: "patient_dummy_d_a", DistinctText: "하지 통증", Enable: "DISASTER_INTRO_TRIAGE_ENABLE_DUMMY_D_A",
          Wait: "DISASTER_INTRO_TRIAGE_WAIT_DUMMY_D_A", Evaluate: "DISASTER_INTRO_TRIAGE_EVALUATE_DUMMY_D_A", CorrectNext: "N001_4"),
      };

      foreach (var expectation in expectations)
      {
        var definition = graph.Interactions.Single(item =>
          item.Entity?.Identifier == expectation.Patient
          && item.InteractionIdentifier == "triage_assess");
        Assert.That(definition.GetExtra("triageBriefing"), Does.Contain(expectation.DistinctText),
          $"{expectation.Patient}의 분류 인터랙션에서 환자 정보를 표시해야 합니다.");
        Assert.That(definition.VisibilityConditions.Single().Tag, Is.EqualTo("nurse_a"));

        var enable = (ScenarioTriageAssessControlNode)graph.Nodes[expectation.Enable];
        var wait = (ScenarioValidatorNode)graph.Nodes[expectation.Wait];
        var evaluate = (ScenarioValidatorNode)graph.Nodes[expectation.Evaluate];
        Assert.That(enable.TargetEntityIdentifier, Is.EqualTo(expectation.Patient));
        Assert.That(enable.NextIdentifier, Is.EqualTo(expectation.Wait));
        Assert.That(wait.NextIdentifier, Is.EqualTo(expectation.Evaluate));
        Assert.That(evaluate.NextIdentifier, Is.EqualTo(expectation.CorrectNext),
          $"{expectation.Patient}의 올바른 분류 결과가 후속 처리로 진행되어야 합니다.");
      }

      Assert.That(graph.Nodes["N001_2"].NextIdentifier, Is.EqualTo("DISASTER_INTRO_TRIAGE_ENABLE_A"));
      Assert.That(graph.Nodes["N001_3"].NextIdentifier, Is.EqualTo("DISASTER_INTRO_TRIAGE_ENABLE_DUMMY_D_A"));
    }

    [Test]
    public void DisasterIntroKeepsNurseATriageQuestUntilCriticalPatientSelectionCompletes()
    {
      var scenarioPath = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/disaster_intro.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(scenarioPath), validateWithSchema: true);
      var questPath = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Quest/disaster_intro.quests.quest.json");
      var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      jsonOptions.Converters.Add(new JsonStringEnumConverter());
      var definitions = JsonSerializer.Deserialize<QuestDefinitionRegistryPayload>(
        File.ReadAllText(questPath), jsonOptions);
      var triageQuest = definitions?.Definitions?.SingleOrDefault(definition =>
        definition.Identifier == "triage_patients");
      var selectionTask = triageQuest?.Tasks?.SingleOrDefault(task =>
        task.Identifier == "select-critical-patient");

      Assert.That(triageQuest, Is.Not.Null);
      Assert.That(triageQuest.IsOrdinal, Is.True);
      Assert.That(triageQuest.Tasks.Select(task => task.Identifier), Is.EqualTo(new[]
      {
        "triage-patient-a",
        "triage-patient-dummy-d-a",
        "select-critical-patient"
      }));
      Assert.That(selectionTask, Is.Not.Null);
      Assert.That(selectionTask.Type, Is.EqualTo(QuestCompletionCriteriaType.InteractionSignalReceived));
      Assert.That(selectionTask.SignalId, Is.EqualTo("move_patient_a"));
      Assert.That(selectionTask.SignalScope, Is.EqualTo(ScenarioSignalScope.Owner));
      Assert.That(graph.Nodes["N001_4"].NextIdentifier, Is.EqualTo("N001_5"),
        "두 번째 분류 직후에는 퀘스트를 제거하지 않고 긴급 환자 선택 목표를 안내해야 합니다.");
      Assert.That(graph.Nodes["V004"].NextIdentifier, Is.EqualTo("Q_TRIAGE_A_REMOVE"),
        "긴급 환자 선택 신호를 확인한 뒤에만 중증도 분류 퀘스트를 제거해야 합니다.");
      Assert.That(graph.Nodes["Q_TRIAGE_A_REMOVE"].NextIdentifier, Is.EqualTo("CC_A_Triage"),
        "퀘스트를 제거한 직후 nurse_a 분기를 완료하여 다른 참여자 대기 상태로 전환해야 합니다.");
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
