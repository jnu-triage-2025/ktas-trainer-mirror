using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>
  /// 시나리오 그래프의 모든 노드를 대상으로 텍스트 검색을 수행한다.
  /// 각 노드 타입의 모든 string 필드를 순회하며 대소문자 무시 부분 일치를 검사한다.
  /// </summary>
  public static class ScenarioNodeSearcher
  {
    public sealed class SearchResult
    {
      /// <summary>일치 항목이 속한 노드 식별자.</summary>
      public string NodeIdentifier { get; }
      /// <summary>노드 타입 이름.</summary>
      public string NodeTypeName   { get; }
      /// <summary>일치한 필드 이름.</summary>
      public string FieldName      { get; }
      /// <summary>일치한 필드 원본 값.</summary>
      public string FieldValue     { get; }

      public SearchResult(string nodeIdentifier, string nodeTypeName, string fieldName, string fieldValue)
      {
        NodeIdentifier = nodeIdentifier;
        NodeTypeName   = nodeTypeName;
        FieldName      = fieldName;
        FieldValue     = fieldValue;
      }
    }

    /// <summary>
    /// <paramref name="graph"/>의 모든 노드에서 <paramref name="query"/>와 일치하는 필드를 반환한다.
    /// query 가 비어 있으면 빈 목록을 반환한다.
    /// </summary>
    public static IReadOnlyList<SearchResult> Search(ScenarioGraph graph, string query)
    {
      var results = new List<SearchResult>();
      if (graph == null || string.IsNullOrWhiteSpace(query))
        return results;

      var q = query.Trim();
      foreach (var node in graph.Nodes.Values)
      {
        if (node == null) continue;
        CollectMatches(node, q, results);
      }

      return results;
    }

    // ── 노드별 필드 수집 ──────────────────────────────────────────────────────

    private static void CollectMatches(IScenarioNode node, string q, List<SearchResult> results)
    {
      var id   = node.Identifier ?? string.Empty;
      var type = node.NodeType.ToString();

      void Add(string field, string value)
      {
        if (Contains(value, q))
          results.Add(new SearchResult(id, type, field, value));
      }

      // ── 모든 노드 공통 ──
      Add("identifier",     node.Identifier);
      Add("nextIdentifier", node.NextIdentifier);

      // ── 타입별 ──
      switch (node)
      {
        case ScenarioDialogueNode d:
          Add("speakerName",             d.SpeakerName);
          Add("dialogueContent",         d.DialogueContent);
          Add("portraitSpriteIdentifier",d.PortraitSpriteIdentifier);
          Add("ttsVoiceIdentifier",      d.TtsVoiceIdentifier);
          break;

        case ScenarioDisinteractableDialogueNode d:
          Add("speakerName", d.SpeakerName);
          Add("dialogueContent", d.DialogueContent);
          Add("portraitSpriteIdentifier", d.PortraitSpriteIdentifier);
          break;

        case ScenarioChoiceNode c:
          Add("speakerName",             c.SpeakerName);
          Add("dialogueContent",         c.DialogueContent);
          Add("portraitSpriteIdentifier",c.PortraitSpriteIdentifier);
          Add("ttsVoiceIdentifier",      c.TtsVoiceIdentifier);
          if (c.Options != null)
          {
            for (int i = 0; i < c.Options.Count; i++)
            {
              var opt = c.Options[i];
              if (opt == null) continue;
              Add($"options[{i}].displayText",       opt.DisplayText);
              Add($"options[{i}].displayIconId",     opt.DisplayIconIdentifier);
              Add($"options[{i}].nextNodeIdentifier",opt.NextNodeIdentifier);
            }
          }
          break;

        case ScenarioValidatorNode v:
          Add("failureNextIdentifier", v.FailureNextIdentifier);
          if (v.RootConditions != null)
          {
            for (int i = 0; i < v.RootConditions.Count; i++)
            {
              var rc = v.RootConditions[i];
              if (rc == null) continue;
              Add($"rootConditions[{i}].playerTag", rc.PlayerTag);
              if (rc.ValidationRules != null)
              {
                for (int j = 0; j < rc.ValidationRules.Count; j++)
                {
                  var rule = rc.ValidationRules[j];
                  if (rule == null) continue;
                  Add($"rootConditions[{i}].rules[{j}].registryIdentifier", rule.RegistryIdentifier);
                }
              }
            }
          }
          break;

        case ScenarioParallelNode p:
          if (p.Branches != null)
          {
            for (int i = 0; i < p.Branches.Count; i++)
            {
              var b = p.Branches[i];
              if (b == null) continue;
              Add($"branches[{i}].identifier",                b.Identifier);
              Add($"branches[{i}].completionConditionId",     b.CompletionConditionIdentifier);
              if (b.RequiredPlayerTags != null)
                for (int j = 0; j < b.RequiredPlayerTags.Count; j++)
                  Add($"branches[{i}].requiredPlayerTags[{j}]", b.RequiredPlayerTags[j]);
              if (b.ForbiddenPlayerTags != null)
                for (int j = 0; j < b.ForbiddenPlayerTags.Count; j++)
                  Add($"branches[{i}].forbiddenPlayerTags[{j}]", b.ForbiddenPlayerTags[j]);
            }
          }
          break;

        case ScenarioInvokeEventNode ie:
          Add("eventIdentifier", ie.EventIdentifier);
          break;

        case ScenarioServerInternalSignalNode sig:
          Add("targetIdentifier", sig.TargetIdentifier);
          Add("signalIdentifier", sig.SignalIdentifier);
          break;

        case ScenarioQuestControlNode qc:
          Add("questDefinitionIdentifier", qc.QuestDefinitionIdentifier);
          if (qc.Quest != null)
          {
            Add("quest.id",                qc.Quest.Id);
            Add("quest.title",             qc.Quest.Title);
            Add("quest.description",       qc.Quest.Description);
            Add("quest.questContent",      qc.Quest.QuestContent);
            Add("quest.waypointIdentifier",qc.Quest.WaypointIdentifier);
            if (qc.Quest.PresentationBindings != null)
            {
              for (int i = 0; i < qc.Quest.PresentationBindings.Count; i++)
              {
                var binding = qc.Quest.PresentationBindings[i];
                if (binding == null) continue;
                Add($"quest.presentationBindings[{i}].entityIdentifier", binding.EntityIdentifier);
                Add($"quest.presentationBindings[{i}].interactionIdentifier", binding.InteractionIdentifier);
                Add($"quest.presentationBindings[{i}].completionCriteriaIdentifier", binding.CompletionCriteriaIdentifier);
                Add($"quest.presentationBindings[{i}].iconIdentifier", binding.IconIdentifier);
              }
            }
          }
          break;

        case ScenarioQuestWaypointHighlightNode qw:
          Add("waypointIdentifier", qw.WaypointIdentifier);
          break;

        case ScenarioStateUpdateNode su:
          Add("targetEntityIdentifier", su.TargetEntityIdentifier);
          Add("stateKey",               su.StateKey);
          Add("stateValue",             su.StateValue);
          break;

        case ScenarioPlayerTagNode pt:
          Add("tag",      pt.Tag);
          Add("fromTag",  pt.FromTag);
          Add("toTag",    pt.ToTag);
          Add("swapTagA", pt.SwapTagA);
          Add("swapTagB", pt.SwapTagB);
          break;

        case ScenarioEntityTagNode et:
          Add("targetEntityIdentifier", et.TargetEntityIdentifier);
          Add("targetEntityStateKey",   et.TargetEntityStateKey);
          Add("tag",                    et.Tag);
          Add("fromTag",                et.FromTag);
          Add("toTag",                  et.ToTag);
          break;

        case ScenarioEntityPresetSpawnNode eps:
          Add("presetIdentifier",              eps.PresetIdentifier);
          Add("spawnedEntityIdentifier",       eps.SpawnedEntityIdentifier);
          Add("positionSourceEntityIdentifier",eps.PositionSourceEntityIdentifier);
          Add("resultStateKey",                eps.ResultStateKey);
          break;

        case ScenarioEntityInitNode ei:
          Add("presetIdentifier",              ei.PresetIdentifier);
          Add("positionSourceEntityIdentifier",ei.PositionSourceEntityIdentifier);
          Add("targetEntityIdentifier",        ei.TargetEntityIdentifier);
          Add("targetEntityStateKey",          ei.TargetEntityStateKey);
          Add("entityIdentifier",              ei.EntityIdentifier);
          Add("resultStateKey",                ei.ResultStateKey);
          if (ei.StateOperations != null)
          {
            for (int i = 0; i < ei.StateOperations.Count; i++)
            {
              var op = ei.StateOperations[i];
              if (op == null) continue;
              Add($"stateOperations[{i}].key",   op.Key);
              Add($"stateOperations[{i}].value", op.Value);
            }
          }
          break;

        case ScenarioTriageAssessControlNode tac:
          Add("targetEntityIdentifier", tac.TargetEntityIdentifier);
          break;

        case ScenarioPatientMedicalStatePresetNode pmp:
          Add("targetEntityIdentifier", pmp.TargetEntityIdentifier);
          Add("targetEntityStateKey",   pmp.TargetEntityStateKey);
          Add("name",                   pmp.Name);
          break;

        case ScenarioSoundNode sn:
          Add("soundResourceIdentifier", sn.SoundResourceIdentifier);
          break;

        case ScenarioPlayTTSNode tts:
          Add("transcriptIdentifier", tts.TranscriptIdentifier);
          Add("ttsVoiceIdentifier",   tts.TtsVoiceIdentifier);
          if (tts.Variables != null)
            foreach (var kv in tts.Variables)
            {
              Add($"variables[{kv.Key}].key",   kv.Key);
              Add($"variables[{kv.Key}].value", kv.Value);
            }
          break;

        case ScenarioQuizNode quiz:
          Add("question",                  quiz.Question);
          Add("onCorrectNextIdentifier",   quiz.OnCorrectNextIdentifier);
          Add("onIncorrectNextIdentifier", quiz.OnIncorrectNextIdentifier);
          Add("feedbackCorrect",           quiz.FeedbackCorrect);
          Add("feedbackIncorrect",         quiz.FeedbackIncorrect);
          Add("ttsVoiceIdentifier",        quiz.TtsVoiceIdentifier);
          if (quiz.Options != null)
            for (int i = 0; i < quiz.Options.Count; i++)
              Add($"options[{i}]", quiz.Options[i]);
          break;

        case ScenarioInteractionNode inter:
          Add("targetIdentifier",             inter.TargetIdentifier);
          Add("requiredItemIdentifier",       inter.RequiredItemIdentifier);
          Add("completionConditionIdentifier",inter.CompletionConditionIdentifier);
          break;

        case ScenarioCombineItemNode ci:
          Add("outputItemIdentifier", ci.OutputItemIdentifier);
          if (ci.InputItemIdentifiers != null)
            for (int i = 0; i < ci.InputItemIdentifiers.Count; i++)
              Add($"inputItemIdentifiers[{i}]", ci.InputItemIdentifiers[i]);
          break;

        case ScenarioNPCMoveNode nm:
          Add("npcIdentifier",         nm.NPCIdentifier);
          Add("destinationIdentifier", nm.DestinationIdentifier);
          break;

        case ScenarioNPCControlNode nc:
          Add("npcIdentifier",          nc.NPCIdentifier);
          Add("interactableIdentifier", nc.InteractableIdentifier);
          Add("resultStateKey",          nc.ResultStateKey);
          Add("displayName",             nc.DisplayName);
          Add("destinationIdentifier",   nc.DestinationIdentifier);
          break;

        case ScenarioPlayerMoveNode pm:
          Add("destinationIdentifier", pm.DestinationIdentifier);
          break;

        case ScenarioCameraTargetNode cam:
          Add("targetObjectIdentifier", cam.TargetObjectIdentifier);
          break;

        case ScenarioReturnToOriginNode rto:
          Add("description", rto.Description);
          break;

        case ScenarioBedSnapNode bs:
          Add("bedEntityIdentifier", bs.BedEntityIdentifier);
          Add("bedEntityStateKey",   bs.BedEntityStateKey);
          Add("snapPointIdentifier", bs.SnapPointIdentifier);
          break;

        case ScenarioManualEntrypointNode me:
          Add("entrypointIdentifier",       me.EntrypointIdentifier);
          Add("manualEnterSetupIdentifier", me.ManualEnterSetupIdentifier);
          Add("description",                me.Description);
          break;
      }
    }

    private static bool Contains(string value, string query)
    {
      if (string.IsNullOrEmpty(value)) return false;
      return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
  }
}
