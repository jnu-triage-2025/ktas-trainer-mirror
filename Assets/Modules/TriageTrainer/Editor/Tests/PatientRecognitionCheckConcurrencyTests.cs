using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Tests
{
  /// <summary>
  /// 한 환자에게 여러 의식 확인 항목이 동시에 열리는 흐름(P_B_CARE / P_C_CARE)과
  /// 항목별 역할 게이트를 검증한다.
  /// </summary>
  public sealed class PatientRecognitionCheckConcurrencyTests
  {
    private const string RecognitionInteractionIdentifier = "recognition_check";
    private const string TalkSignal = "patient_b_recognition_1";
    private const string PupilSignal = "patient_b_pupil_checked";

    [Test]
    public void ConcurrentRecognitionChecksExposeOneInteractionEach()
    {
      var patientObject = new GameObject("recognition-concurrency-patient");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");

        patient.ActivateRecognitionCheck(TalkSignal, false, "nurse_a", "말 걸기");
        patient.ActivateRecognitionCheck(PupilSignal, false, "nurse_c", "동공반사 확인");

        var recognitionInteracts = GetRecognitionInteracts(patient);
        Assert.That(recognitionInteracts.Select(each => each.DisplayText),
          Is.EquivalentTo(new[] { "말 걸기", "동공반사 확인" }),
          "동시에 열린 확인 항목은 각각 하나의 상호작용으로 노출되어야 한다.");
        Assert.That(
          recognitionInteracts.Cast<IQuestPresentationTarget>()
            .All(target => target.InteractionIdentifier == RecognitionInteractionIdentifier),
          Is.True,
          "퀘스트 표시 바인딩이 그대로 동작하려면 상호작용 식별자는 항목과 무관하게 같아야 한다.");
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void CompletingOneRecognitionCheckLeavesTheOtherActive()
    {
      var patientObject = new GameObject("recognition-completion-patient");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");

        patient.ActivateRecognitionCheck(TalkSignal, false, "nurse_a", "말 걸기");
        patient.ActivateRecognitionCheck(PupilSignal, false, "nurse_c", "동공반사 확인");

        Assert.That(CompleteRecognitionCheck(patient, TalkSignal), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised(TalkSignal), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised(PupilSignal), Is.False,
          "한 항목을 완료해도 다른 항목의 신호가 함께 올라가서는 안 된다.");
        Assert.That(IsRecognitionCheckActive(patient, TalkSignal), Is.False);
        Assert.That(IsRecognitionCheckActive(patient, PupilSignal), Is.True,
          "다른 역할이 진행 중인 항목은 그대로 남아야 한다.");
        Assert.That(GetRecognitionInteracts(patient).Select(each => each.DisplayText),
          Is.EqualTo(new[] { "동공반사 확인" }));

        Assert.That(CompleteRecognitionCheck(patient, TalkSignal), Is.False,
          "이미 닫힌 항목의 완료 요청은 거절되어야 한다.");
      }
      finally
      {
        ScenarioInteractionSignals.Clear(TalkSignal);
        ScenarioInteractionSignals.Clear(PupilSignal);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PupilRecognitionCheckStillRequiresPenlight()
    {
      var patientObject = new GameObject("recognition-penlight-patient");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        patient.ActivateRecognitionCheck(PupilSignal, false, "nurse_c", "동공반사 확인");

        // 펜라이트를 가진 요청자가 없으므로 동공반사 확인은 완료되지 않는다.
        Assert.That(CompleteRecognitionCheck(patient, PupilSignal), Is.False);
        Assert.That(IsRecognitionCheckActive(patient, PupilSignal), Is.True);
      }
      finally
      {
        ScenarioInteractionSignals.Clear(PupilSignal);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void ClearingRecognitionChecksRemovesEveryActiveItem()
    {
      var patientObject = new GameObject("recognition-clear-patient");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        patient.ActivateRecognitionCheck(TalkSignal, false, "nurse_a", "말 걸기");
        patient.ActivateRecognitionCheck(PupilSignal, false, "nurse_c", "동공반사 확인");

        patient.ClearRecognitionChecks();

        Assert.That(IsRecognitionCheckActive(patient, TalkSignal), Is.False);
        Assert.That(IsRecognitionCheckActive(patient, PupilSignal), Is.False);
        Assert.That(GetRecognitionInteracts(patient), Is.Empty,
          "시나리오 종료 정리 뒤에는 확인 항목 상호작용이 남지 않아야 한다.");
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void EmptyRoleTagDoesNotRestrictRoleAndUnknownIdentifierIsRejected()
    {
      var isRoleIdentifier = typeof(PatientController).GetMethod(
        "IsRecognitionRoleIdentifier",
        BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(isRoleIdentifier, Is.Not.Null);

      Assert.That((bool)isRoleIdentifier.Invoke(null, new object[] { null, string.Empty }), Is.True,
        "역할 태그를 비우면 역할을 제한하지 않는다.");
      Assert.That((bool)isRoleIdentifier.Invoke(null, new object[] { "unknown-user", "nurse_c" }), Is.False,
        "태그를 갖지 않은 사용자는 그 항목을 완료할 수 없다.");
    }

    [Test]
    public void RecognitionActivationRoleTagsMatchScenarioBranchRoles()
    {
      var activations = GetRecognitionActivations();
      Assert.That(activations.Select(each => (each.EventIdentifier, each.RequiredRoleTag)), Is.EqualTo(new[]
      {
        ("activate_patient_b_recognition_1", "nurse_a"),
        ("activate_patient_b_recognition_2", "nurse_a"),
        ("activate_patient_b_recognition_3", "nurse_a"),
        ("activate_patient_b_recognition_4", "nurse_a"),
        ("activate_patient_b_strength_check", "nurse_a"),
        ("activate_patient_b_pupil_check", "nurse_c"),
        ("activate_patient_c_recognition_1", "nurse_b"),
        ("activate_patient_c_recognition_2", "nurse_b"),
        ("activate_patient_c_recognition_3", "nurse_b"),
        ("activate_patient_c_recognition_4", "nurse_b"),
        ("activate_patient_c_strength_check", "nurse_b"),
        ("activate_patient_c_pupil_check", "nurse_c"),
      }));

      var graph = ScenarioGraphLoader.LoadFromJson(
        File.ReadAllText(Path.Combine(
          Application.dataPath,
          "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json")),
        validateWithSchema: true);
      var rolesByEvent = CollectBranchRolesByEvent(graph);

      foreach (var activation in activations)
      {
        Assert.That(rolesByEvent.TryGetValue(activation.EventIdentifier, out var branchRoles), Is.True,
          $"{activation.EventIdentifier} 를 실행하는 역할 분기를 그래프에서 찾지 못했다.");
        Assert.That(branchRoles, Is.EqualTo(new[] { activation.RequiredRoleTag }),
          $"{activation.EventIdentifier} 의 역할 태그가 그래프 분기 역할과 어긋난다.");
      }
    }

    /// <summary>
    /// ByRole 병렬 분기마다 그 분기에서 도달하는 InvokeEvent 를 모아 분기 역할과 짝짓는다.
    /// 분기 사이의 흐름은 다음 노드, 선택지, 검증 실패 분기로만 이어지므로 그 세 경로만 따라간다.
    /// </summary>
    private static Dictionary<string, List<string>> CollectBranchRolesByEvent(ScenarioGraph graph)
    {
      var rolesByEvent = new Dictionary<string, List<string>>(System.StringComparer.Ordinal);
      foreach (var node in graph.Nodes.Values)
      {
        if (node is not ScenarioParallelNode parallel || parallel.Branches == null)
          continue;

        foreach (var branch in parallel.Branches)
        {
          if (branch?.RequiredPlayerTags == null || branch.RequiredPlayerTags.Count == 0)
            continue;

          foreach (string eventIdentifier in CollectReachableEvents(graph, branch.Identifier))
          {
            if (!rolesByEvent.TryGetValue(eventIdentifier, out var roles))
              rolesByEvent[eventIdentifier] = roles = new List<string>();
            foreach (string tag in branch.RequiredPlayerTags)
            {
              if (!roles.Contains(tag))
                roles.Add(tag);
            }
          }
        }
      }

      return rolesByEvent;
    }

    private static IEnumerable<string> CollectReachableEvents(ScenarioGraph graph, string startIdentifier)
    {
      var events = new List<string>();
      var visited = new HashSet<string>(System.StringComparer.Ordinal);
      var pending = new Stack<string>();
      pending.Push(startIdentifier);
      while (pending.Count > 0)
      {
        string identifier = pending.Pop();
        if (string.IsNullOrWhiteSpace(identifier)
            || !visited.Add(identifier)
            || !graph.Nodes.TryGetValue(identifier, out var node))
          continue;

        if (node is ScenarioInvokeEventNode invoke && !string.IsNullOrWhiteSpace(invoke.EventIdentifier))
          events.Add(invoke.EventIdentifier);

        // 중첩된 병렬 노드의 분기는 그 분기의 역할을 따르므로 여기서 따라가지 않는다.
        if (node is ScenarioParallelNode)
          continue;

        if (!string.IsNullOrWhiteSpace(node.NextIdentifier))
          pending.Push(node.NextIdentifier);
        if (node is ScenarioValidatorNode validator && !string.IsNullOrWhiteSpace(validator.FailureNextIdentifier))
          pending.Push(validator.FailureNextIdentifier);
        if (node is ScenarioChoiceNode choice && choice.Options != null)
        {
          foreach (var option in choice.Options)
          {
            if (option != null && !string.IsNullOrWhiteSpace(option.NextNodeIdentifier))
              pending.Push(option.NextNodeIdentifier);
          }
        }
      }

      return events;
    }

    private static PatientRecognitionActivation[] GetRecognitionActivations()
    {
      var field = typeof(TriageScenarioEventBootstrap).GetField(
        "PatientBCRecognitionActivations",
        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
      Assert.That(field, Is.Not.Null);
      return (PatientRecognitionActivation[])field.GetValue(null);
    }

    private static List<IInteract> GetRecognitionInteracts(PatientController patient)
    {
      return patient.Interacts
        .Where(interact => interact is IQuestPresentationTarget target
                           && target.InteractionIdentifier == RecognitionInteractionIdentifier)
        .ToList();
    }

    private static bool CompleteRecognitionCheck(PatientController patient, string completionSignal)
    {
      var complete = typeof(PatientController).GetMethod(
        "TryCompleteRecognitionCheckAuthoritative",
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(complete, Is.Not.Null);
      return (bool)complete.Invoke(patient, new object[] { null, false, completionSignal });
    }

    private static bool IsRecognitionCheckActive(PatientController patient, string completionSignal)
    {
      var indexOf = typeof(PatientController).GetMethod(
        "IndexOfRecognitionCheck",
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(indexOf, Is.Not.Null);
      return (int)indexOf.Invoke(patient, new object[] { completionSignal }) >= 0;
    }
  }
}
