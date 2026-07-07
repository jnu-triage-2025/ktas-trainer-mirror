using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity.Patient;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>
  /// 시나리오 그래프의 모든 노드에 대해 정적 진단(warn/error)을 실행한다.
  /// 런타임 레지스트리를 필요로 하지 않는 순수 구조 검사만 수행한다.
  /// </summary>
  public static class ScenarioGraphDiagnostics
  {
    public enum Severity { Info, Warning, Error }

    public sealed class DiagnosticItem
    {
      public Severity Severity { get; }
      public string NodeIdentifier { get; }
      public string Message { get; }

      public DiagnosticItem(Severity severity, string nodeIdentifier, string message)
      {
        Severity = severity;
        NodeIdentifier = nodeIdentifier;
        Message = message;
      }
    }

    /// <summary>
    /// 그래프 전체를 검사하여 DiagnosticItem 목록을 반환한다.
    /// </summary>
    public static IReadOnlyList<DiagnosticItem> Run(ScenarioGraph graph)
    {
      var items = new List<DiagnosticItem>();
      if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0)
        return items;

      var nodeIds = new HashSet<string>(graph.Nodes.Keys);

      foreach (var node in graph.Nodes.Values)
      {
        if (node == null) continue;
        CheckNode(node, nodeIds, graph, items);
      }

      // 그래프 수준 검사
      CheckGraphLevel(graph, nodeIds, items);

      return items;
    }

    // ── 노드 수준 검사 ────────────────────────────────────────────────────────

    private static void CheckNode(
        IScenarioNode node,
        HashSet<string> nodeIds,
        ScenarioGraph graph,
        List<DiagnosticItem> items)
    {
      var id = node.Identifier;

      // 공통: identifier 누락
      if (string.IsNullOrWhiteSpace(id))
      {
        items.Add(new DiagnosticItem(Severity.Error, "(unknown)", "Identifier가 비어 있습니다."));
        return;
      }

      // 공통: NextIdentifier 참조 무결성
      if (!string.IsNullOrEmpty(node.NextIdentifier) && !nodeIds.Contains(node.NextIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Error, id,
          $"nextIdentifier '{node.NextIdentifier}' 가 존재하지 않는 노드를 참조합니다."));
      }

      // 타입별 검사
      switch (node)
      {
        case ScenarioDialogueNode dialogue:
          CheckDialogue(dialogue, items);
          break;
        case ScenarioChoiceNode choice:
          CheckChoice(choice, nodeIds, items);
          break;
        case ScenarioValidatorNode validator:
          CheckValidator(validator, nodeIds, items);
          break;
        case ScenarioParallelNode parallel:
          CheckParallel(parallel, nodeIds, items);
          break;
        case ScenarioInvokeEventNode invoke:
          CheckInvokeEvent(invoke, items);
          break;
        case ScenarioStateUpdateNode stateUpdate:
          CheckStateUpdate(stateUpdate, items);
          break;
        case ScenarioEntityPresetSpawnNode spawn:
          CheckEntityPresetSpawn(spawn, items);
          break;
        case ScenarioEntityInitNode entityInit:
          CheckEntityInit(entityInit, items);
          break;
        case ScenarioEntityTagNode entityTag:
          CheckEntityTag(entityTag, items);
          break;
        case ScenarioTriageAssessControlNode triageAssess:
          CheckTriageAssessControl(triageAssess, items);
          break;
        case ScenarioPatientMedicalStatePresetNode patientPreset:
          CheckPatientMedicalStatePreset(patientPreset, items);
          break;
        case ScenarioQuestControlNode questControl:
          CheckQuestControl(questControl, items);
          break;
        case ScenarioQuizNode quiz:
          CheckQuiz(quiz, nodeIds, items);
          break;
        case ScenarioDelayNode delay:
          CheckDelay(delay, items);
          break;
        case ScenarioSoundNode sound:
          CheckSound(sound, items);
          break;
      }
    }

    private static void CheckDialogue(ScenarioDialogueNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.DialogueContent))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "dialogueContent가 비어 있습니다."));
      if (string.IsNullOrWhiteSpace(node.SpeakerName))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "speakerName이 비어 있습니다."));
      if (string.IsNullOrWhiteSpace(node.NextIdentifier))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "nextIdentifier가 없습니다 (시나리오 종료 노드일 경우 무시)."));
    }

    private static void CheckChoice(ScenarioChoiceNode node, HashSet<string> nodeIds, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.DialogueContent))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "dialogueContent가 비어 있습니다."));
      if (node.Options == null || node.Options.Count == 0)
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "Choice 노드에 옵션이 없습니다."));
        return;
      }
      for (int i = 0; i < node.Options.Count; i++)
      {
        var opt = node.Options[i];
        if (string.IsNullOrWhiteSpace(opt?.DisplayText))
          items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, $"옵션 {i + 1}: displayText가 비어 있습니다."));
        if (string.IsNullOrEmpty(opt?.NextNodeIdentifier))
          items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, $"옵션 {i + 1}: nextNodeIdentifier가 없습니다."));
        else if (!nodeIds.Contains(opt.NextNodeIdentifier))
          items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"옵션 {i + 1}: nextNodeIdentifier '{opt.NextNodeIdentifier}' 가 존재하지 않습니다."));
      }
    }

    private static void CheckValidator(ScenarioValidatorNode node, HashSet<string> nodeIds, List<DiagnosticItem> items)
    {
      if (node.RootConditions == null || node.RootConditions.Count == 0)
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "Validator 노드에 RootCondition이 없습니다."));
        return;
      }
      foreach (var rc in node.RootConditions)
      {
        if (rc == null) continue;
        if (rc.Condition == ScenarioValidatorCondition.RegistryContains
            && (rc.ValidationRules == null || rc.ValidationRules.Count == 0))
        {
          items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "RegistryContains 조건에 ValidationRule이 없습니다."));
        }
      }
      if (node.OnFailure == ScenarioValidatorOnFailure.Branching
          && string.IsNullOrEmpty(node.FailureNextIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "OnFailure=Branching 이지만 failureNextIdentifier가 비어 있습니다."));
      }
      if (!string.IsNullOrEmpty(node.FailureNextIdentifier) && !nodeIds.Contains(node.FailureNextIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"failureNextIdentifier '{node.FailureNextIdentifier}' 가 존재하지 않습니다."));
      }
    }

    private static void CheckParallel(ScenarioParallelNode node, HashSet<string> nodeIds, List<DiagnosticItem> items)
    {
      if (node.Branches == null || node.Branches.Count < 2)
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "Parallel 노드에 브랜치가 2개 미만입니다."));
        return;
      }
      var seen = new HashSet<string>();
      foreach (var branch in node.Branches)
      {
        if (branch == null) continue;
        if (string.IsNullOrEmpty(branch.Identifier))
        {
          items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "브랜치 identifier가 비어 있습니다."));
          continue;
        }
        if (!seen.Add(branch.Identifier))
          items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, $"브랜치 identifier '{branch.Identifier}' 가 중복됩니다."));
        if (!nodeIds.Contains(branch.Identifier))
          items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"브랜치 '{branch.Identifier}' 가 노드로 존재하지 않습니다."));
        if (branch.RequiredPlayerTags == null || branch.RequiredPlayerTags.Count == 0)
          items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, $"브랜치 '{branch.Identifier}': requiredPlayerTags가 비어 있습니다."));
      }
    }

    private static void CheckInvokeEvent(ScenarioInvokeEventNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.EventIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "eventIdentifier가 비어 있습니다."));
    }

    private static void CheckStateUpdate(ScenarioStateUpdateNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "targetEntityIdentifier가 비어 있습니다."));
      if (string.IsNullOrWhiteSpace(node.StateKey))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "stateKey가 비어 있습니다."));
    }

    private static void CheckEntityPresetSpawn(ScenarioEntityPresetSpawnNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.PresetIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "presetIdentifier가 비어 있습니다."));
      if (string.IsNullOrWhiteSpace(node.SpawnedEntityIdentifier))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "spawnedEntityIdentifier가 비어 있습니다. 자동 GUID가 부여됩니다."));
    }

    private static void CheckEntityInit(ScenarioEntityInitNode node, List<DiagnosticItem> items)
    {
      bool hasTarget = !string.IsNullOrWhiteSpace(node.TargetEntityIdentifier)
                       || !string.IsNullOrWhiteSpace(node.TargetEntityStateKey)
                       || !string.IsNullOrWhiteSpace(node.PresetIdentifier);
      if (!hasTarget)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          "targetEntityIdentifier, targetEntityStateKey, presetIdentifier 중 하나 이상을 지정해야 합니다."));
    }

    private static void CheckEntityTag(ScenarioEntityTagNode node, List<DiagnosticItem> items)
    {
      bool hasTarget = !string.IsNullOrWhiteSpace(node.TargetEntityIdentifier)
                       || !string.IsNullOrWhiteSpace(node.TargetEntityStateKey);
      if (!hasTarget)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          "targetEntityIdentifier 또는 targetEntityStateKey 를 지정해야 합니다."));
      if (string.IsNullOrWhiteSpace(node.Tag) && node.Operation != ScenarioPlayerTagOperationType.Change)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "tag가 비어 있습니다."));
    }

    private static void CheckTriageAssessControl(ScenarioTriageAssessControlNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "targetEntityIdentifier가 비어 있습니다."));
    }

    private static void CheckPatientMedicalStatePreset(ScenarioPatientMedicalStatePresetNode node, List<DiagnosticItem> items)
    {
      var id = node.Identifier;

      // ── 대상 지정 여부 ──
      bool hasTarget = !string.IsNullOrWhiteSpace(node.TargetEntityIdentifier)
                       || !string.IsNullOrWhiteSpace(node.TargetEntityStateKey);
      if (!hasTarget)
        items.Add(new DiagnosticItem(Severity.Error, id,
          "targetEntityIdentifier 또는 targetEntityStateKey 를 지정해야 합니다."));

      // ── 프리셋 필드 존재 여부 ──
      bool hasAnyField =
        node.Sex.HasValue || node.Age.HasValue || node.Name != null || node.BloodType.HasValue
        || node.IntendedTriage.HasValue || node.ConsciousnessGcs.HasValue
        || node.ConsciousnessEyeOpening.HasValue || node.ConsciousnessVerbal.HasValue || node.ConsciousnessMotor.HasValue
        || node.ConsciousnessLocLabel.HasValue || node.ConsciousnessPupillaryResponse.HasValue
        || node.RespirationAwRR.HasValue || node.RespirationTypeValue.HasValue
        || node.PulseRate.HasValue || node.PulseForceType.HasValue
        || node.BloodPressureSystolic.HasValue || node.BloodPressureDiastolic.HasValue
        || node.SkinColorHue.HasValue || node.SkinTemperatureType.HasValue
        || node.IsCardiacArrest.HasValue;

      if (!hasAnyField)
        items.Add(new DiagnosticItem(Severity.Warning, id,
          "설정된 프리셋 필드가 없습니다. 노드가 아무 효과도 없습니다."));

      // ── 전이(Transition) 검사 ──
      // 점차 변화(Gradual)인데 소요 시간이 0 이하이면 즉시 적용과 동일하게 동작하므로 경고한다.
      if (node.TransitionMode == PatientMedicalStateTransitionMode.Gradual
          && node.TransitionDurationSeconds <= 0f)
        items.Add(new DiagnosticItem(Severity.Warning, id,
          "transitionMode=Gradual 이지만 transitionDurationSeconds 가 0 이하입니다. 즉시 적용(Immediate)과 동일하게 동작합니다."));

      // ── 열거형 필드 검사 ────────────────────────────────────────────────────
      // null(not set) → Warning, 정의되지 않은 값 → Error.
      // 새 열거형 필드 추가 시 아래에 동일 패턴으로 추가한다.

      CheckEnumField<Sex>(node.Sex,                                        "sex",                          id, items);
      CheckEnumField<BloodType>(node.BloodType,                            "bloodType",                    id, items);
      CheckEnumField<EyeOpeningResponse>(node.ConsciousnessEyeOpening,     "consciousnessEyeOpening",       id, items);
      CheckEnumField<VerbalResponse>(node.ConsciousnessVerbal,             "consciousnessVerbal",           id, items);
      CheckEnumField<MotorResponse>(node.ConsciousnessMotor,               "consciousnessMotor",            id, items);
      CheckEnumField<LOCLabel>(node.ConsciousnessLocLabel,                 "consciousnessLocLabel",         id, items);
      CheckEnumField<PupillaryResponse>(node.ConsciousnessPupillaryResponse, "consciousnessPupillaryResponse", id, items);
      CheckEnumField<RespirationType>(node.RespirationTypeValue,           "respirationType",              id, items);
      CheckEnumField<BloodPulseForceType>(node.PulseForceType,             "pulseForceType",               id, items);
      CheckEnumField<SkinColorHue>(node.SkinColorHue,                      "skinColorHue",                 id, items);
      CheckEnumField<SkinTemperatureType>(node.SkinTemperatureType,        "skinTemperatureType",          id, items);

      // TriageLevel: not set / Unassessed(="미평가" sentinel) / 미정의 값을 각각 구분
      if (!node.IntendedTriage.HasValue)
        items.Add(new DiagnosticItem(Severity.Warning, id,
          "intendedTriage가 설정되지 않았습니다(not set). 의도된 정답 등급(Level1~Level5)을 지정하세요."));
      else if (!Enum.IsDefined(typeof(TriageLevel), node.IntendedTriage.Value))
        items.Add(new DiagnosticItem(Severity.Error, id,
          $"intendedTriage={node.IntendedTriage.Value} 는 TriageLevel에 정의되지 않은 값입니다."));
      else if (node.IntendedTriage.Value == TriageLevel.Unassessed)
        items.Add(new DiagnosticItem(Severity.Warning, id,
          "intendedTriage=Unassessed 는 '미평가' 상태를 의미합니다. 의도된 정답 등급을 지정하려면 Level1~Level5 중 하나를 사용하세요."));

      // ── 수치 범위 검사 ──────────────────────────────────────────────────────

      if (node.ConsciousnessGcs.HasValue)
      {
        int gcs = node.ConsciousnessGcs.Value;
        if (gcs < 3 || gcs > 15)
          items.Add(new DiagnosticItem(Severity.Error, id,
            $"consciousnessGcs={gcs} 는 유효 범위(3~15)를 벗어납니다."));
      }

      // GCS와 LOC 레이블 일관성 검사
      // Consciousness.cs 기준: GCS ≤8 → Severe(Stupor/SemiComa/Coma),
      //                        GCS ≤12 → Moderate(Drowsy/Stupor),
      //                        GCS ≤15 → Mild(Alert/Drowsy)
      if (node.ConsciousnessGcs.HasValue && node.ConsciousnessLocLabel.HasValue)
      {
        int gcs       = node.ConsciousnessGcs.Value;
        var locLabel  = node.ConsciousnessLocLabel.Value;
        bool mismatch = false;
        string expected = string.Empty;

        if (gcs <= 8 && (locLabel == LOCLabel.Alert || locLabel == LOCLabel.Drowsy))
        {
          mismatch = true;
          expected = "Stupor / SemiComa / Coma";
        }
        else if (gcs >= 13 && (locLabel == LOCLabel.Stupor || locLabel == LOCLabel.SemiComa || locLabel == LOCLabel.Coma))
        {
          mismatch = true;
          expected = "Alert / Drowsy";
        }

        if (mismatch)
          items.Add(new DiagnosticItem(Severity.Warning, id,
            $"GCS={gcs} 와 consciousnessLocLabel={locLabel} 이 일치하지 않습니다. GCS {gcs} 에서 기대되는 LOC: {expected}"));
      }

      // GCS와 E/V/M 세부 항목(합계) 일관성 검사
      // E(1~4) + V(1~5) + M(1~6) 의 합이 consciousnessGcs와 일치해야 한다.
      if (node.ConsciousnessGcs.HasValue
          && node.ConsciousnessEyeOpening.HasValue
          && node.ConsciousnessVerbal.HasValue
          && node.ConsciousnessMotor.HasValue)
      {
        int gcs = node.ConsciousnessGcs.Value;
        int evm = (int)node.ConsciousnessEyeOpening.Value + (int)node.ConsciousnessVerbal.Value + (int)node.ConsciousnessMotor.Value;
        if (gcs != evm)
          items.Add(new DiagnosticItem(Severity.Warning, id,
            $"consciousnessGcs={gcs} 가 E+V+M 세부 항목의 합({evm})과 일치하지 않습니다. " +
            $"(E={node.ConsciousnessEyeOpening.Value}, V={node.ConsciousnessVerbal.Value}, M={node.ConsciousnessMotor.Value})"));
      }

      if (node.BloodPressureSystolic.HasValue && node.BloodPressureDiastolic.HasValue
          && node.BloodPressureSystolic.Value <= node.BloodPressureDiastolic.Value)
        items.Add(new DiagnosticItem(Severity.Warning, id,
          $"수축기 혈압({node.BloodPressureSystolic}) ≤ 이완기 혈압({node.BloodPressureDiastolic}) 입니다."));

      if (node.RespirationAwRR.HasValue && node.RespirationAwRR.Value < 0)
        items.Add(new DiagnosticItem(Severity.Error, id,
          $"respirationAwRR={node.RespirationAwRR.Value} 는 음수입니다."));

      if (node.PulseRate.HasValue && node.PulseRate.Value < 0)
        items.Add(new DiagnosticItem(Severity.Error, id,
          $"pulseRate={node.PulseRate.Value} 는 음수입니다."));

      if (node.Age.HasValue && node.Age.Value < 0)
        items.Add(new DiagnosticItem(Severity.Error, id,
          $"age={node.Age.Value} 는 음수입니다."));
    }

    /// <summary>
    /// nullable 열거형 필드를 검사한다.
    /// <list type="bullet">
    /// <item>null(not set) → Warning: 필드가 설정되지 않음</item>
    /// <item>HasValue 이지만 <c>Enum.IsDefined</c> 실패 → Error: 정의되지 않은 값
    ///   (JSON int 캐스팅 오류 등으로 범위 밖 값이 들어왔을 때)</item>
    /// </list>
    /// 새 열거형 필드 추가 시 <see cref="CheckPatientMedicalStatePreset"/> 에서
    /// 이 메서드를 한 줄로 호출하면 된다.
    /// </summary>
    private static void CheckEnumField<TEnum>(
        TEnum? value, string fieldName, string nodeId, List<DiagnosticItem> items)
        where TEnum : struct, Enum
    {
      if (!value.HasValue)
      {
        items.Add(new DiagnosticItem(Severity.Warning, nodeId,
          $"{fieldName}이(가) 설정되지 않았습니다(not set)."));
        return;
      }
      if (!Enum.IsDefined(typeof(TEnum), value.Value))
        items.Add(new DiagnosticItem(Severity.Error, nodeId,
          $"{fieldName}={value.Value} 는 {typeof(TEnum).Name}에 정의되지 않은 값입니다."));
    }

    private static void CheckQuestControl(ScenarioQuestControlNode node, List<DiagnosticItem> items)
    {
      bool hasRef = node.Quest != null && !string.IsNullOrWhiteSpace(node.Quest.Id)
                    || !string.IsNullOrWhiteSpace(node.QuestDefinitionIdentifier);
      if (!hasRef)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          "quest.id 또는 questDefinitionIdentifier 를 지정해야 합니다."));
    }

    private static void CheckQuiz(ScenarioQuizNode node, HashSet<string> nodeIds, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.Question))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "question이 비어 있습니다."));
      if (node.Options == null || node.Options.Count == 0)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "Quiz 노드에 옵션이 없습니다."));
      else if (node.CorrectIndex < 0 || node.CorrectIndex >= node.Options.Count)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          $"correctIndex={node.CorrectIndex} 가 옵션 범위를 벗어납니다 (0~{node.Options.Count - 1})."));

      if (!string.IsNullOrEmpty(node.OnCorrectNextIdentifier) && !nodeIds.Contains(node.OnCorrectNextIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          $"onCorrectNextIdentifier '{node.OnCorrectNextIdentifier}' 가 존재하지 않습니다."));
      if (!string.IsNullOrEmpty(node.OnIncorrectNextIdentifier) && !nodeIds.Contains(node.OnIncorrectNextIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          $"onIncorrectNextIdentifier '{node.OnIncorrectNextIdentifier}' 가 존재하지 않습니다."));
    }

    private static void CheckDelay(ScenarioDelayNode node, List<DiagnosticItem> items)
    {
      if (node.DurationSeconds < 0f)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"durationSeconds={node.DurationSeconds} 는 음수입니다."));
    }

    private static void CheckSound(ScenarioSoundNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.SoundResourceIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "soundResourceIdentifier가 비어 있습니다."));
    }

    // ── 그래프 수준 검사 ──────────────────────────────────────────────────────

    private static void CheckGraphLevel(ScenarioGraph graph, HashSet<string> nodeIds, List<DiagnosticItem> items)
    {
      const string graphLevel = "(graph)";

      // 진입 노드 감지: 다른 노드로부터 참조되지 않는 노드
      var referenced = new HashSet<string>();
      foreach (var node in graph.Nodes.Values)
      {
        if (node == null) continue;
        if (!string.IsNullOrEmpty(node.NextIdentifier)) referenced.Add(node.NextIdentifier);
        if (node is ScenarioChoiceNode c)
          foreach (var o in c.Options) if (!string.IsNullOrEmpty(o?.NextNodeIdentifier)) referenced.Add(o.NextNodeIdentifier);
        if (node is ScenarioParallelNode p)
          foreach (var b in p.Branches) if (!string.IsNullOrEmpty(b?.Identifier)) referenced.Add(b.Identifier);
        if (node is ScenarioQuizNode q)
        {
          if (!string.IsNullOrEmpty(q.OnCorrectNextIdentifier)) referenced.Add(q.OnCorrectNextIdentifier);
          if (!string.IsNullOrEmpty(q.OnIncorrectNextIdentifier)) referenced.Add(q.OnIncorrectNextIdentifier);
        }
      }

      var entryNodes = nodeIds.Except(referenced).ToList();
      if (entryNodes.Count == 0)
        items.Add(new DiagnosticItem(Severity.Warning, graphLevel, "진입 노드(in-degree=0)가 없습니다. 순환 그래프일 수 있습니다."));
      else if (entryNodes.Count > 1)
        items.Add(new DiagnosticItem(Severity.Info, graphLevel,
          $"진입 노드가 {entryNodes.Count}개입니다: {string.Join(", ", entryNodes.OrderBy(s => s))}"));

      // 종료 노드: nextIdentifier가 없고 Choice/Parallel/Quiz가 아닌 노드
      var terminalCount = graph.Nodes.Values
        .Where(n => n != null
                    && string.IsNullOrEmpty(n.NextIdentifier)
                    && n is not ScenarioChoiceNode
                    && n is not ScenarioParallelNode
                    && n is not ScenarioQuizNode)
        .Count();
      if (terminalCount == 0)
        items.Add(new DiagnosticItem(Severity.Warning, graphLevel, "종료 노드(nextIdentifier=null)가 없습니다."));
    }
  }
}
