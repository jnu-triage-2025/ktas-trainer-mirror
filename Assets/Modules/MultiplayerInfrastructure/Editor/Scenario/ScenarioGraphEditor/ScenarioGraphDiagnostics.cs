using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Utils;
using UnityEngine;

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
        if (node == null)
          continue;
        CheckNode(node, nodeIds, graph, items);
      }

      // 그래프 수준 검사
      CheckGraphLevel(graph, nodeIds, items);
      CheckReturnToOriginReachableFromMainFlow(graph, items);
      CheckGeneratedStaticLayouts(items);

      return items;
    }

    private static void CheckGeneratedStaticLayouts(List<DiagnosticItem> items)
    {
      var roots = UnityEngine.Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach (var root in roots)
      {
        for (int i = 0; i < root.transform.childCount; i++)
        {
          var name = root.transform.GetChild(i).name;
          if (name.StartsWith("static_entities:", StringComparison.Ordinal))
            items.Add(new DiagnosticItem(Severity.Info, "(static-layout)", $"StaticEntityLayout present: {name}"));
        }
      }
      if (roots.Length == 0)
        items.Add(new DiagnosticItem(Severity.Warning, "(static-layout)", "GeneratedByOverworldGameObjectInitializerEditor가 씬에 없습니다."));
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

      // ReturnToOrigin 은 곁가지 종료 표식이라 다음 노드를 갖지 않는다. 값이 남아 있으면
      // 흐름을 오해하게 만든다(실행은 이 값을 보지 않는다).
      if (node is ScenarioReturnToOriginNode && !string.IsNullOrEmpty(node.NextIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Warning, id,
          $"ReturnToOrigin 노드에 nextIdentifier '{node.NextIdentifier}' 가 남아 있습니다. "
          + "곁가지는 이 노드에서 끝나므로 이 값은 실행에 쓰이지 않습니다. 비워 주세요."));
      }

      // 타입별 검사
      switch (node)
      {
        case ScenarioDialogueNode dialogue:
          CheckDialogue(dialogue, items);
          break;
        case ScenarioDisinteractableDialogueNode dialogue:
          CheckDisinteractableDialogue(dialogue, items);
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
        case ScenarioTimeControlNode timeControl:
          CheckTimeControl(timeControl, items);
          break;
        case ScenarioManualEntrypointNode manualEntrypoint:
          CheckManualEntrypoint(manualEntrypoint, nodeIds, graph, items);
          break;
        case ScenarioBedSnapNode bedSnap:
          CheckBedSnap(bedSnap, items);
          break;
        case ScenarioInteractionVisibilityNode interactionVisibility:
          CheckInteractionVisibility(interactionVisibility, graph, items);
          break;
      }
    }

    private static void CheckInteractionVisibility(ScenarioInteractionVisibilityNode node, ScenarioGraph graph, List<DiagnosticItem> items)
    {
      if (node.Targets == null || node.Targets.Count == 0)
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "targets 가 비어 있습니다."));
        return;
      }
      if (node.PlayerScope == ScenarioInteractionVisibilityPlayerScope.ByTag
          && (node.PlayerTags == null || node.PlayerTags.Count == 0))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "playerScope 가 ByTag 이면 playerTags 가 필요합니다."));

      for (int i = 0; i < node.Targets.Count; i++)
      {
        var target = node.Targets[i];
        if (target?.Entity == null || target.Entity.IsEmpty)
        {
          items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"targets[{i}] 의 entity(id 또는 tag)가 비어 있습니다."));
          continue;
        }
        if (string.IsNullOrWhiteSpace(target.InteractionIdentifier))
        {
          items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"targets[{i}] 의 interaction 이 비어 있습니다."));
          continue;
        }
        if (!IsInteractionDeclaredInGraph(graph, target.Entity, target.InteractionIdentifier))
          items.Add(new DiagnosticItem(Severity.Warning, node.Identifier,
            $"'{target.Entity}/{target.InteractionIdentifier}' 이(가) 이 시나리오의 interactions 구역에 없습니다. " +
            "코드 리터럴 정의라면 무시해도 되지만, 식별자 오타가 아닌지 확인하세요."));
      }
    }

    private static bool IsInteractionDeclaredInGraph(ScenarioGraph graph, ScenarioEntityReference entity, string interactionIdentifier)
    {
      if (graph?.Interactions == null)
        return false;
      foreach (var definition in graph.Interactions)
      {
        if (definition == null || definition.Entity == null)
          continue;
        if (!string.Equals(definition.InteractionIdentifier, interactionIdentifier?.Trim(), StringComparison.Ordinal))
          continue;
        bool sameEntity = entity.IsTagReference
          ? string.Equals(definition.Entity.Tag, entity.Tag, StringComparison.Ordinal)
          : string.Equals(definition.Entity.Identifier, entity.Identifier, StringComparison.Ordinal)
            || definition.Entity.IsTagReference; // 태그 참조 정의는 어떤 엔티티에도 붙을 수 있다.
        if (sameEntity)
          return true;
      }
      return false;
    }

    private static void AddInteractionDefinitionWarnings(ScenarioGraph graph, List<DiagnosticItem> items, string graphLevel)
    {
      if (graph?.Interactions == null)
        return;
      foreach (var definition in graph.Interactions)
      {
        if (definition == null)
          continue;
        string address = $"{definition.Entity}/{definition.InteractionIdentifier}";
        if (definition.KindSpecified && definition.Kind == global::MultiplayerInfrastructure.InteractableEntity.InteractionKind.Custom
            && string.IsNullOrWhiteSpace(definition.HandlerKey))
          items.Add(new DiagnosticItem(Severity.Info, graphLevel,
            $"interactions '{address}': Custom 종류는 같은 주소의 코드 리터럴 정의(핸들러)가 있어야 노출됩니다."));
        if (!definition.HasVisibilityConditions && !definition.InitialVisible
            && !HasVisibilityTrigger(graph, definition))
          items.Add(new DiagnosticItem(Severity.Warning, graphLevel,
            $"interactions '{address}': 조건도 없고 initial 도 false 인데 InteractionVisibility 노드가 이 주소를 켜지 않습니다. 영영 숨겨질 수 있습니다."));
      }

      foreach (var node in graph.Nodes.Values)
      {
        if (node is not ScenarioQuestMarkNode questMark
            || questMark.TargetType != QuestPresentationTargetType.Interaction
            || string.IsNullOrWhiteSpace(questMark.EntityIdentifier)
            || string.IsNullOrWhiteSpace(questMark.InteractionIdentifier))
          continue;
        if (!IsInteractionDeclaredInGraph(graph, ScenarioEntityReference.ForIdentifier(questMark.EntityIdentifier), questMark.InteractionIdentifier))
          items.Add(new DiagnosticItem(Severity.Info, node.Identifier,
            $"QuestMark 대상 '{questMark.EntityIdentifier}/{questMark.InteractionIdentifier}' 이(가) interactions 구역에 없습니다(코드 리터럴 정의일 수 있음)."));
      }
    }

    private static bool HasVisibilityTrigger(ScenarioGraph graph, global::MultiplayerInfrastructure.InteractableEntity.InteractionDefinition definition)
    {
      foreach (var node in graph.Nodes.Values)
      {
        if (node is not ScenarioInteractionVisibilityNode visibility || visibility.Targets == null)
          continue;
        if (visibility.Operation == ScenarioInteractionVisibilityOperation.Hide)
          continue;
        foreach (var target in visibility.Targets)
        {
          if (target?.Entity == null
              || !string.Equals(target.InteractionIdentifier, definition.InteractionIdentifier, StringComparison.Ordinal))
            continue;
          if (target.Entity.IsTagReference || definition.Entity.IsTagReference
              || string.Equals(target.Entity.Identifier, definition.Entity.Identifier, StringComparison.Ordinal))
            return true;
        }
      }
      return false;
    }

    private static void CheckBedSnap(ScenarioBedSnapNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.SnapPointIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          "snapPointIdentifier가 비어 있습니다."));
      }

      if (string.IsNullOrWhiteSpace(node.BedEntityIdentifier)
          && string.IsNullOrWhiteSpace(node.BedEntityStateKey))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          "bedEntityIdentifier와 bedEntityStateKey가 모두 비어 있습니다. 둘 중 하나는 지정해야 합니다."));
      }

      // 씬에 없는 포인트는 런타임에 조용히 실패한다(ignoreFailure 기본값이 true).
      if (!string.IsNullOrWhiteSpace(node.SnapPointIdentifier)
          && !SceneHasPositioningPoint(node.SnapPointIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier,
          $"현재 열린 씬에서 '{node.SnapPointIdentifier}' 포지셔닝 포인트를 찾지 못했습니다. "
          + "다른 씬에 있거나 이름이 다를 수 있으니 확인하세요."));
      }
    }

    /// <summary>
    /// ManualEntrypoint 준비 체인을 따라가며 종료 방식을 확인한다.
    /// ReturnToOrigin 노드 / 진입 지점 회귀 / 진입 지점의 next 도달은 명시적 종료로 본다.
    /// next 가 비어서 끝나는 암묵적 종료만 경고한다.
    /// </summary>
    private static void CheckManualEnterSetupChainTermination(
        ScenarioManualEntrypointNode entrypoint,
        ScenarioGraph graph,
        List<DiagnosticItem> items)
    {
      var visited = new HashSet<string>(StringComparer.Ordinal);
      string cursorId = entrypoint.ManualEnterSetupIdentifier;

      while (!string.IsNullOrEmpty(cursorId) && visited.Add(cursorId))
      {
        if (!graph.Nodes.TryGetValue(cursorId, out var cursor) || cursor == null)
          return; // 참조 무결성은 공통 검사가 이미 보고한다.

        if (cursor is ScenarioReturnToOriginNode)
          return; // 명시적 종료 표식에 닿았다.

        // Choice/Quiz/Parallel 은 흐름이 NextIdentifier 로 결정되지 않는다(옵션·정오답·브랜치).
        // 여기서부터는 정적으로 끝을 판정할 수 없으므로 경고 없이 추적을 멈춘다.
        if (cursor is ScenarioChoiceNode or ScenarioQuizNode or ScenarioParallelNode)
          return;

        string nextId = cursor.NextIdentifier;

        if (string.IsNullOrEmpty(nextId))
        {
          items.Add(new DiagnosticItem(Severity.Warning, cursor.Identifier,
            $"ManualEntrypoint '{entrypoint.Identifier}' 의 준비 체인이 nextIdentifier가 비어서 끝납니다. "
            + "체인 끝에 ReturnToOrigin 노드를 두어 종료 의도를 남기세요. 지금 상태로는 나중에 여기에 "
            + "다음 노드를 연결하는 순간 준비 체인이 원래 흐름으로 흘러가 버립니다."));
          return;
        }

        // 진입 지점 자신이나 그 다음 노드에 닿는 것도 명시적 회귀로 본다.
        if (string.Equals(nextId, entrypoint.Identifier, StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(entrypoint.NextIdentifier)
                && string.Equals(nextId, entrypoint.NextIdentifier, StringComparison.Ordinal)))
          return;

        cursorId = nextId;
      }
    }

    /// <summary>
    /// ReturnToOrigin 은 곁가지 전용 표식이다. 메인 흐름에서 닿으면 다음 노드가 없어
    /// 시나리오가 그대로 끝나므로, defaultEntrypoint 에서 도달 가능한지 확인해 경고한다.
    /// 병렬 브랜치와 ManualEntrypoint 준비 체인은 곁가지이므로 추적에서 제외한다.
    /// </summary>
    private static void CheckReturnToOriginReachableFromMainFlow(
        ScenarioGraph graph,
        List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(graph.DefaultEntrypoint)
          || !graph.Nodes.ContainsKey(graph.DefaultEntrypoint))
        return;

      var visited = new HashSet<string>(StringComparer.Ordinal);
      var pending = new Queue<string>();
      pending.Enqueue(graph.DefaultEntrypoint);

      while (pending.Count > 0)
      {
        string currentId = pending.Dequeue();
        if (!visited.Add(currentId)
            || !graph.Nodes.TryGetValue(currentId, out var current)
            || current == null)
          continue;

        if (current is ScenarioReturnToOriginNode)
        {
          items.Add(new DiagnosticItem(Severity.Warning, current.Identifier,
            "ReturnToOrigin이 메인 흐름에서 도달 가능합니다. 이 노드는 곁가지 전용 종료 표식이라 "
            + "다음 노드가 없고, 메인 흐름이 여기 닿으면 시나리오가 그대로 끝납니다. 배선을 확인하세요."));
          continue;
        }

        void Follow(string id)
        {
          if (!string.IsNullOrEmpty(id))
            pending.Enqueue(id);
        }

        Follow(current.NextIdentifier);

        if (current is ScenarioChoiceNode choice && choice.Options != null)
        {
          foreach (var option in choice.Options)
            Follow(option?.NextNodeIdentifier);
        }

        if (current is ScenarioQuizNode quiz)
        {
          Follow(quiz.OnCorrectNextIdentifier);
          Follow(quiz.OnIncorrectNextIdentifier);
        }

        if (current is ScenarioValidatorNode validator)
          Follow(validator.FailureNextIdentifier);

        // Parallel 의 branches 와 ManualEntrypoint 의 준비 체인은 곁가지라 따라가지 않는다.
      }
    }

    private static bool SceneHasPositioningPoint(string identifier)
    {
      var points = UnityEngine.Object.FindObjectsByType<TriageTrainer.Entity.MovingPatientBedPositioningPoint>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < points.Length; i++)
      {
        if (points[i] != null
            && string.Equals(points[i].Identifier, identifier.Trim(), StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    private static void CheckManualEntrypoint(
        ScenarioManualEntrypointNode node,
        HashSet<string> nodeIds,
        ScenarioGraph graph,
        List<DiagnosticItem> items)
    {
      if (!string.IsNullOrEmpty(node.ManualEnterSetupIdentifier)
          && !nodeIds.Contains(node.ManualEnterSetupIdentifier))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          $"manualEnterSetupIdentifier '{node.ManualEnterSetupIdentifier}' 가 존재하지 않는 노드를 참조합니다."));
      }

      // 준비 체인이 자기 자신에서 시작하면 진입할 때마다 같은 노드를 두 번 밟는다.
      if (string.Equals(node.ManualEnterSetupIdentifier, node.Identifier, StringComparison.Ordinal))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          "manualEnterSetupIdentifier가 자기 자신을 가리킵니다."));
      }

      // 준비 체인의 첫 노드가 Next 와 같으면 진입 직후 그 노드를 두 번 밟는다.
      if (!string.IsNullOrEmpty(node.ManualEnterSetupIdentifier)
          && string.Equals(node.ManualEnterSetupIdentifier, node.NextIdentifier, StringComparison.Ordinal))
      {
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier,
          $"manualEnterSetupIdentifier가 nextIdentifier와 같습니다('{node.NextIdentifier}'). 수동 진입 시 이 노드를 두 번 실행합니다."));
      }

      // 준비 체인이 어떻게 끝나는지 확인한다. ReturnToOrigin 없이 "next 가 비어서" 끝나면
      // 나중에 누군가 그 노드에 다음 노드를 이으면 곁가지가 원래 흐름으로 새어 나간다.
      if (!string.IsNullOrEmpty(node.ManualEnterSetupIdentifier)
          && nodeIds.Contains(node.ManualEnterSetupIdentifier))
      {
        CheckManualEnterSetupChainTermination(node, graph, items);
      }

      // clear-state=true 는 상태 저장소를 통째로 비운다. 그 저장소가 엔티티 해석 표를 겸하므로,
      // 표를 채우는 노드가 있는 그래프에서 준비 체인이 없으면 진입 후 대상 조회가 전부 실패한다.
      if (string.IsNullOrEmpty(node.ManualEnterSetupIdentifier)
          && graph.Nodes.Values.Any(each => each is ScenarioEntityPresetSpawnNode
                                            or ScenarioEntityInitNode))
      {
        items.Add(new DiagnosticItem(Severity.Info, node.Identifier,
          "manualEnterSetupIdentifier가 없습니다. 이 그래프는 resultStateKey로 엔티티를 등록하는데, "
          + "clear-state=true로 진입하면 그 해석 표가 비워집니다. 준비 체인에서 다시 채워 주세요."));
      }

      // 명령이 별칭 하나로 지점을 특정해야 하므로 중복은 허용하지 않는다.
      string alias = node.ResolvedEntrypointIdentifier;
      if (string.IsNullOrWhiteSpace(alias))
        return;

      int duplicateCount = graph.Nodes.Values
        .OfType<ScenarioManualEntrypointNode>()
        .Count(each => string.Equals(each.ResolvedEntrypointIdentifier, alias, StringComparison.OrdinalIgnoreCase));
      if (duplicateCount > 1)
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          $"ManualEntrypoint 별칭 '{alias}' 가 {duplicateCount}개 노드에서 중복됩니다."));
      }
    }

    private static void CheckTimeControl(ScenarioTimeControlNode node, List<DiagnosticItem> items)
    {
      if (node.DurationSeconds < 0f)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"durationSeconds={node.DurationSeconds} 는 음수입니다."));
      if (node.StartSeconds < 0f)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"startSeconds={node.StartSeconds} 는 음수입니다."));

      // Hide 를 제외한 모든 연산은 대상 타이머 식별자가 필요하다.
      if (node.Operation != ScenarioTimeOperationType.Hide && string.IsNullOrWhiteSpace(node.TimerId))
      {
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier,
          $"{node.Operation} 연산에는 timerId 가 필요합니다."));
      }

      // 카운트다운 생성인데 목표/시작이 모두 0이면 즉시 0으로 표시된다.
      if (node.Operation == ScenarioTimeOperationType.Create
          && node.Direction == ScenarioTimeDirection.Countdown
          && node.DurationSeconds <= 0f
          && node.StartSeconds <= 0f)
      {
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier,
          "카운트다운 Create 인데 durationSeconds/startSeconds 가 모두 0입니다. 즉시 0으로 표시됩니다."));
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

    private static void CheckDisinteractableDialogue(ScenarioDisinteractableDialogueNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.DialogueContent))
        items.Add(new DiagnosticItem(Severity.Warning, node.Identifier, "dialogueContent가 비어 있습니다."));
      if (node.FadeInDuration.Value < 0d || node.DisplayDuration.Value < 0d || node.FadeOutDuration.Value < 0d)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "대화 표시 시간은 음수일 수 없습니다."));
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
        if (rc == null)
          continue;
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
        if (branch == null)
          continue;
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

      CheckEnumField<Sex>(node.Sex, "sex", id, items);
      CheckEnumField<BloodType>(node.BloodType, "bloodType", id, items);
      CheckEnumField<EyeOpeningResponse>(node.ConsciousnessEyeOpening, "consciousnessEyeOpening", id, items);
      CheckEnumField<VerbalResponse>(node.ConsciousnessVerbal, "consciousnessVerbal", id, items);
      CheckEnumField<MotorResponse>(node.ConsciousnessMotor, "consciousnessMotor", id, items);
      CheckEnumField<LOCLabel>(node.ConsciousnessLocLabel, "consciousnessLocLabel", id, items);
      CheckEnumField<PupillaryResponse>(node.ConsciousnessPupillaryResponse, "consciousnessPupillaryResponse", id, items);
      CheckEnumField<RespirationType>(node.RespirationTypeValue, "respirationType", id, items);
      CheckEnumField<BloodPulseForceType>(node.PulseForceType, "pulseForceType", id, items);
      CheckEnumField<SkinColorHue>(node.SkinColorHue, "skinColorHue", id, items);
      CheckEnumField<SkinTemperatureType>(node.SkinTemperatureType, "skinTemperatureType", id, items);

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
        int gcs = node.ConsciousnessGcs.Value;
        var locLabel = node.ConsciousnessLocLabel.Value;
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

      if (node.Quest != null)
        CheckQuestPresentation(node.Quest, node.Identifier, items);
    }

    private static void CheckQuestPresentation(QuestData quest, string nodeIdentifier, List<DiagnosticItem> items)
    {
      var criterionIds = new HashSet<string>(StringComparer.Ordinal);
      CollectCriterionIdentifiers(quest.Tasks, criterionIds, nodeIdentifier, items);
      CollectCriterionIdentifiers(quest.CompletionCriteria, criterionIds, nodeIdentifier, items);
      if (quest.PresentationBindings == null)
        return;

      var bindingKeys = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < quest.PresentationBindings.Count; i++)
      {
        var binding = quest.PresentationBindings[i];
        if (binding == null)
          continue;

        if (string.IsNullOrWhiteSpace(binding.EntityIdentifier) || string.IsNullOrWhiteSpace(binding.IconIdentifier))
          items.Add(new DiagnosticItem(Severity.Error, nodeIdentifier,
            $"presentationBindings[{i}]의 entityIdentifier 또는 iconIdentifier가 비어 있습니다."));
        if (binding.TargetType == QuestPresentationTargetType.Interaction
            && string.IsNullOrWhiteSpace(binding.InteractionIdentifier))
          items.Add(new DiagnosticItem(Severity.Error, nodeIdentifier,
            $"presentationBindings[{i}]의 interactionIdentifier가 비어 있습니다."));
        if (binding.Activation == QuestPresentationActivation.CompletionCriteria
            && !criterionIds.Contains(binding.CompletionCriteriaIdentifier ?? string.Empty))
          items.Add(new DiagnosticItem(Severity.Error, nodeIdentifier,
            $"presentationBindings[{i}]가 알 수 없는 완료 조건 '{binding.CompletionCriteriaIdentifier}'을 참조합니다."));

        string key = $"{binding.TargetType}|{binding.EntityIdentifier}|{binding.InteractionIdentifier}|{binding.Priority}";
        if (!bindingKeys.Add(key))
          items.Add(new DiagnosticItem(Severity.Warning, nodeIdentifier,
            $"presentationBindings[{i}]의 대상과 priority가 다른 바인딩과 중복됩니다."));
      }
    }

    private static void CollectCriterionIdentifiers(
      IReadOnlyList<QuestCompletionCriteria> criteria,
      HashSet<string> identifiers,
      string nodeIdentifier,
      List<DiagnosticItem> items)
    {
      if (criteria == null)
        return;

      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion == null)
          continue;

        if (!string.IsNullOrWhiteSpace(criterion.Identifier) && !identifiers.Add(criterion.Identifier))
          items.Add(new DiagnosticItem(Severity.Error, nodeIdentifier,
            $"완료 조건 identifier '{criterion.Identifier}'가 중복됩니다."));
        CollectCriterionIdentifiers(criterion.Conditions, identifiers, nodeIdentifier, items);
      }
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
      if (node.Duration.Value < 0d)
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, $"duration.value={node.Duration.Value} 는 음수입니다."));
    }

    private static void CheckSound(ScenarioSoundNode node, List<DiagnosticItem> items)
    {
      if (string.IsNullOrWhiteSpace(node.SoundResourceIdentifier))
        items.Add(new DiagnosticItem(Severity.Error, node.Identifier, "soundResourceIdentifier가 비어 있습니다."));
    }

    // ── 그래프 수준 검사 ──────────────────────────────────────────────────────

    /// <summary>
    /// Validator 게이트가 기다리는 RuntimeState 신호 가운데, 그래프 데이터만으로는 누가 올리는지
    /// 확인할 수 없는 것을 알린다.
    ///
    /// <para>
    /// 원격 클라이언트가 올리는 신호는 그래프의 clientSignalIdentifiers 나 clientSignalPrefixes 에
    /// 선언되어 있어야 서버가 받아들인다. 호스트가 올리는 같은 신호는 이 검사를 거치지 않으므로,
    /// 선언을 빠뜨리면 호스트로 확인할 때는 정상으로 보이고 나머지 참가자만 막힌다. 그 신호를
    /// 기다리는 게이트는 열리지 않고, 결국 세션 전체가 멈춘다. 선언 누락을 미리 찾기 위한 검사다.
    /// </para>
    ///
    /// <para>
    /// 다만 이 검사는 거짓 양성을 낸다. 게임플레이 C# 코드가 서버 권위로 직접 올리는 신호는
    /// 그래프 데이터에 흔적이 남지 않기 때문이다. 그래서 심각도를 Info 로 두고, 문구도
    /// "발신자가 없다"가 아니라 "그래프만으로는 확인할 수 없다"로 적는다.
    /// </para>
    /// </summary>
    private static void AddUnattributedGateSignalWarnings(ScenarioGraph graph, List<DiagnosticItem> items)
    {
      // 그래프 엔진이 서버에서 스스로 계산해 내보내는 출력은 선언이 필요 없다.
      var serverProduced = new HashSet<string>(StringComparer.Ordinal);
      foreach (var node in graph.Nodes.Values)
      {
        switch (node)
        {
          case ScenarioSignalCounterNode counter:
            AddNormalizedSignal(serverProduced, counter.OutputSignalIdentifier);
            break;
          case ScenarioSignalListenerNode listener:
            AddNormalizedSignal(serverProduced, listener.OutputSignalIdentifier);
            break;
          case ScenarioEntityStateSignalBindingNode binding:
            AddNormalizedSignal(serverProduced, binding.OutputSignalIdentifier);
            break;
        }
      }

      var declared = new HashSet<string>(StringComparer.Ordinal);
      foreach (var signal in graph.ClientSignalIdentifiers ?? Array.Empty<string>())
        AddNormalizedSignal(declared, signal);

      var prefixes = new List<string>();
      foreach (var prefix in graph.ClientSignalPrefixes ?? Array.Empty<string>())
      {
        var normalized = ScenarioInteractionSignals.Normalize(prefix);
        if (!string.IsNullOrWhiteSpace(normalized))
          prefixes.Add(normalized);
      }

      // 같은 신호를 여러 게이트가 기다리는 일이 흔하므로 신호마다 한 번만 보고한다.
      var reported = new HashSet<string>(StringComparer.Ordinal);
      foreach (var node in graph.Nodes.Values)
      {
        if (node is not ScenarioValidatorNode validator || validator.RootConditions == null)
          continue;

        foreach (var rootCondition in validator.RootConditions)
        {
          foreach (var rule in rootCondition?.ValidationRules ?? (IReadOnlyList<ScenarioValidatorRule>)Array.Empty<ScenarioValidatorRule>())
          {
            if (rule == null
                || rule.Type != ScenarioValidatorRuleType.Registry
                || rule.RegistryType != RegistryType.RuntimeState
                || string.IsNullOrWhiteSpace(rule.RegistryIdentifier))
            {
              continue;
            }

            var signal = ScenarioInteractionSignals.Normalize(rule.RegistryIdentifier);
            if (serverProduced.Contains(signal)
                || declared.Contains(signal)
                || prefixes.Any(prefix => signal.StartsWith(prefix, StringComparison.Ordinal))
                || !reported.Add(signal))
            {
              continue;
            }

            items.Add(new DiagnosticItem(
              Severity.Info,
              validator.Identifier,
              $"게이트가 기다리는 신호 '{signal}' 를 누가 올리는지 그래프 데이터만으로는 확인할 수 없습니다. "
              + "게임플레이 코드가 서버에서 올리는 신호라면 정상입니다. "
              + "클라이언트가 올리는 신호라면 clientSignalIdentifiers 또는 clientSignalPrefixes 에 선언해야 하며, "
              + "선언하지 않으면 호스트에서만 통과하고 나머지 참가자는 이 게이트를 넘지 못합니다."));
          }
        }
      }
    }

    private static void AddNormalizedSignal(HashSet<string> target, string signal)
    {
      var normalized = ScenarioInteractionSignals.Normalize(signal);
      if (!string.IsNullOrWhiteSpace(normalized))
        target.Add(normalized);
    }

    private static void CheckGraphLevel(ScenarioGraph graph, HashSet<string> nodeIds, List<DiagnosticItem> items)
    {
      const string graphLevel = "(graph)";

      AddSessionStartActingNpcInfos(graph, items, graphLevel);
      AddUndefinedWaypointWarnings(graph, items);
      AddQuestMarkWarnings(graph, items);
      AddInteractionDefinitionWarnings(graph, items, graphLevel);
      AddUnattributedGateSignalWarnings(graph, items);

      // 진입 노드 감지: 다른 노드로부터 참조되지 않는 노드
      var referenced = new HashSet<string>();
      foreach (var node in graph.Nodes.Values)
      {
        if (node == null)
          continue;
        if (!string.IsNullOrEmpty(node.NextIdentifier))
          referenced.Add(node.NextIdentifier);
        if (node is ScenarioValidatorNode v
            && (v.OnFailure == ScenarioValidatorOnFailure.Branching
                || (v.WaitForCondition
                    && v.OnWaitTimeout == ScenarioValidatorWaitTimeoutBehavior.FailBranch))
            && !string.IsNullOrEmpty(v.FailureNextIdentifier))
        {
          referenced.Add(v.FailureNextIdentifier);
        }
        if (node is ScenarioChoiceNode c)
          foreach (var o in c.Options)
            if (!string.IsNullOrEmpty(o?.NextNodeIdentifier))
              referenced.Add(o.NextNodeIdentifier);
        if (node is ScenarioParallelNode p)
          foreach (var b in p.Branches)
            if (!string.IsNullOrEmpty(b?.Identifier))
              referenced.Add(b.Identifier);
        if (node is ScenarioQuizNode q)
        {
          if (!string.IsNullOrEmpty(q.OnCorrectNextIdentifier))
            referenced.Add(q.OnCorrectNextIdentifier);
          if (!string.IsNullOrEmpty(q.OnIncorrectNextIdentifier))
            referenced.Add(q.OnIncorrectNextIdentifier);
        }
        if (node is ScenarioManualEntrypointNode me && !string.IsNullOrEmpty(me.ManualEnterSetupIdentifier))
          referenced.Add(me.ManualEnterSetupIdentifier);
      }

      var entryNodes = nodeIds.Except(referenced).ToList();
      if (entryNodes.Count == 0)
        items.Add(new DiagnosticItem(Severity.Warning, graphLevel, "진입 노드(in-degree=0)가 없습니다. 순환 그래프일 수 있습니다."));
      else if (entryNodes.Count > 1)
        items.Add(new DiagnosticItem(Severity.Info, graphLevel,
          $"진입 노드가 {entryNodes.Count}개입니다: {string.Join(", ", entryNodes.OrderBy(s => s))}"));

      // defaultEntrypoint(DefaultInit) 참조 무결성: 존재하지 않는 노드를 가리키면 시작 실패로 이어진다.
      if (!string.IsNullOrWhiteSpace(graph.DefaultEntrypoint) && !nodeIds.Contains(graph.DefaultEntrypoint))
        items.Add(new DiagnosticItem(Severity.Error, graphLevel,
          $"defaultEntrypoint '{graph.DefaultEntrypoint}' 가 존재하지 않는 노드를 참조합니다."));

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

    private static void AddUndefinedWaypointWarnings(
        ScenarioGraph graph,
        List<DiagnosticItem> items)
    {
      var definedWaypointIds = new HashSet<string>(
        graph.Waypoints?
          .Where(waypoint => waypoint != null && !string.IsNullOrWhiteSpace(waypoint.Identifier))
          .Select(waypoint => waypoint.Identifier)
        ?? Enumerable.Empty<string>(),
        StringComparer.Ordinal);

      foreach (var node in graph.Nodes.Values)
      {
        if (node == null)
          continue;

        string waypointId = node switch
        {
          ScenarioPlayerMoveNode playerMove
            when playerMove.DestinationType == ScenarioMoveDestinationType.Waypoint
            => playerMove.DestinationIdentifier,
          ScenarioNPCMoveNode npcMove
            when npcMove.DestinationType == ScenarioMoveDestinationType.Waypoint
            => npcMove.DestinationIdentifier,
          ScenarioNPCControlNode npcControl
            when npcControl.Mode == ScenarioNPCControlMode.Control
                 && npcControl.DestinationType == ScenarioMoveDestinationType.Waypoint
            => npcControl.DestinationIdentifier,
          ScenarioQuestWaypointHighlightNode highlight => highlight.WaypointIdentifier,
          _ => null
        };

        if (string.IsNullOrWhiteSpace(waypointId) || definedWaypointIds.Contains(waypointId))
          continue;

        items.Add(new DiagnosticItem(
          Severity.Warning,
          node.Identifier,
          $"이 시나리오 파일에서는 {waypointId} waypoint가 정의되지 않았습니다. " +
          $"게임을 실행하기 전, 게임 시스템에 다른 방법으로 {waypointId}를 등록했는지 확인하세요."));
      }
    }

    private static void AddQuestMarkWarnings(
        ScenarioGraph graph,
        List<DiagnosticItem> items)
    {
      var actingNpcIds = new HashSet<string>(
        graph.ActingNpcs?
          .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Identifier))
          .Select(value => value.Identifier)
        ?? Enumerable.Empty<string>(),
        StringComparer.Ordinal);

      foreach (var node in graph.Nodes.Values)
      {
        if (node is not ScenarioQuestMarkNode questMark)
          continue;

        if (string.IsNullOrWhiteSpace(questMark.EntityIdentifier))
        {
          items.Add(new DiagnosticItem(
            Severity.Error,
            node.Identifier,
            "QuestMark 노드에 entityIdentifier가 없습니다. 마크를 붙일 대상을 지정하세요."));
          continue;
        }

        if (questMark.TargetType == QuestPresentationTargetType.Interaction
            && string.IsNullOrWhiteSpace(questMark.InteractionIdentifier))
        {
          items.Add(new DiagnosticItem(
            Severity.Error,
            node.Identifier,
            "Interaction 대상 QuestMark 노드에는 interactionIdentifier가 필요합니다."));
          continue;
        }

        if (questMark.TargetType == QuestPresentationTargetType.Npc
            && actingNpcIds.Count > 0
            && !actingNpcIds.Contains(questMark.EntityIdentifier))
        {
          items.Add(new DiagnosticItem(
            Severity.Warning,
            node.Identifier,
            $"이 시나리오 파일의 actingNpcs에 {questMark.EntityIdentifier}가 없습니다. " +
            "씬에 배치된 NPC라면 무시해도 되지만, 식별자 오타가 아닌지 확인하세요."));
        }
      }
    }

    private static void AddSessionStartActingNpcInfos(
        ScenarioGraph graph,
        List<DiagnosticItem> items,
        string graphLevel)
    {
      if (graph.ActingNpcs == null)
        return;

      foreach (var actingNpc in graph.ActingNpcs
                   .Where(value => value != null
                                   && value.SpawnOnStart
                                   && !string.IsNullOrWhiteSpace(value.Identifier))
                   .GroupBy(value => value.Identifier, StringComparer.Ordinal)
                   .Select(group => group.First())
                   .OrderBy(value => value.Identifier, StringComparer.Ordinal))
      {
        items.Add(new DiagnosticItem(
          Severity.Info,
          graphLevel,
          $"{actingNpc.Identifier}는 세션이 시작되면 EntityPreset으로부터 로드됩니다. " +
          "게임을 실행하기 전, 게임 시스템에서 사용중인 EntityPresetRegistryRequirementsSO에 이 NPC가 등록되어있는지 확인하세요."));
      }
    }
  }
}
