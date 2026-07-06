using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioInspectorView : VisualElement
  {
    private ScenarioNodeView targetNode;
    private readonly IMGUIContainer container;
    private readonly ScenarioGraphAuthoringWindow window;

    public ScenarioInspectorView(ScenarioGraphAuthoringWindow window)
    {
      this.window = window;
      style.flexGrow = 1f;
      style.paddingLeft = 4f;
      style.paddingRight = 4f;

      container = new IMGUIContainer(DrawInspector);
      container.style.flexGrow = 1f;
      container.style.overflow = Overflow.Visible;
      Add(container);
    }

    public void RefreshInspector()
    {
        container.MarkDirtyRepaint();
    }

    public void SetTarget(ScenarioNodeView nodeView)
    {
        targetNode = nodeView;
        RefreshInspector();
    }

    private void DrawInspector()
    {
      GUILayout.Label("Scenario Inspector", EditorStyles.boldLabel);
      EditorGUILayout.Space();

      if (targetNode == null)
      {
        EditorGUILayout.HelpBox("노드를 선택하면 속성을 편집할 수 있습니다.", MessageType.Info);
        return;
      }

      var data = targetNode.Data;

      EditorGUI.BeginChangeCheck();
      var newIdentifier = EditorGUILayout.TextField("Identifier", data.Identifier);
      if (EditorGUI.EndChangeCheck())
      {
        if (window.TryRenameNode(targetNode, newIdentifier))
        {
          targetNode.RefreshTitle();
        }
      }

      EditorGUI.BeginChangeCheck();
      var newType = (ScenarioNodeType)EditorGUILayout.EnumPopup("Type", data.NodeType);
      if (EditorGUI.EndChangeCheck())
      {
        var newView = window.ChangeNodeType(targetNode, newType);
        if (newView != null)
        {
          targetNode = newView;
        }
      }

      EditorGUI.BeginChangeCheck();
      DrawTypeSpecificInspector(targetNode.Data);
      if (EditorGUI.EndChangeCheck())
      {
        targetNode.RefreshTitle();
        window.NotifyGraphStructureChanged();
      }
      else
      {
        targetNode.RefreshTitle();
      }
    }

    private void DrawTypeSpecificInspector(IScenarioNode data)
    {
      switch (data.NodeType)
      {
        case ScenarioNodeType.Dialogue:
          DrawDialogueFields((ScenarioDialogueNode)data);
          break;
        case ScenarioNodeType.Choice:
          DrawChoiceFields((ScenarioChoiceNode)data);
          break;
        case ScenarioNodeType.Sound:
          DrawSoundFields((ScenarioSoundNode)data);
          break;
        case ScenarioNodeType.PlayerMove:
          DrawPlayerMoveFields((ScenarioPlayerMoveNode)data);
          break;
        case ScenarioNodeType.NPCMove:
          DrawNPCMoveFields((ScenarioNPCMoveNode)data);
          break;
        case ScenarioNodeType.CameraTarget:
          DrawCameraTargetFields((ScenarioCameraTargetNode)data);
          break;
        case ScenarioNodeType.InvokeEvent:
          DrawInvokeEventFields((ScenarioInvokeEventNode)data);
          break;
        case ScenarioNodeType.ServerInternalSignal:
          DrawServerInternalSignalFields((ScenarioServerInternalSignalNode)data);
          break;
        case ScenarioNodeType.Validator:
          DrawValidatorFields((ScenarioValidatorNode)data);
          break;
        case ScenarioNodeType.Parallel:
          DrawParallelFields((ScenarioParallelNode)data);
          break;
        case ScenarioNodeType.QuestControl:
          DrawQuestControlFields((ScenarioQuestControlNode)data);
          break;
        case ScenarioNodeType.QuestWaypointHighlight:
          DrawQuestWaypointHighlightFields((ScenarioQuestWaypointHighlightNode)data);
          break;
        case ScenarioNodeType.Delay:
          DrawDelayFields((ScenarioDelayNode)data);
          break;
        case ScenarioNodeType.Interaction:
          DrawInteractionFields((ScenarioInteractionNode)data);
          break;
        case ScenarioNodeType.CombineItem:
          DrawCombineItemFields((ScenarioCombineItemNode)data);
          break;
        case ScenarioNodeType.Quiz:
          DrawQuizFields((ScenarioQuizNode)data);
          break;
        case ScenarioNodeType.StateUpdate:
          DrawStateUpdateFields((ScenarioStateUpdateNode)data);
          break;
        case ScenarioNodeType.PlayerTag:
          DrawTagModificationFields((ScenarioPlayerTagNode)data);
          break;
        case ScenarioNodeType.PlayTTS:
          DrawPlayTTSFields((ScenarioPlayTTSNode)data);
          break;
        case ScenarioNodeType.EntityPresetSpawn:
          DrawEntityPresetSpawnFields((ScenarioEntityPresetSpawnNode)data);
          break;
        case ScenarioNodeType.EntityTag:
          DrawEntityTagFields((ScenarioEntityTagNode)data);
          break;
        case ScenarioNodeType.EntityInit:
          DrawEntityInitFields((ScenarioEntityInitNode)data);
          break;
        case ScenarioNodeType.TriageAssessControl:
          DrawTriageAssessControlFields((ScenarioTriageAssessControlNode)data);
          break;
        case ScenarioNodeType.PatientMedicalStatePreset:
          DrawPatientMedicalStatePresetFields((ScenarioPatientMedicalStatePresetNode)data);
          break;
      }
    }

    private void DrawDialogueFields(ScenarioDialogueNode data)
    {
      data.SpeakerName = EditorGUILayout.TextField("Speaker", data.SpeakerName);
      EditorGUILayout.PrefixLabel("Dialogue");
      data.DialogueContent = EditorGUILayout.TextArea(data.DialogueContent, GUILayout.Height(60));
      data.PortraitSpriteIdentifier = EditorGUILayout.TextField("Portrait Sprite", data.PortraitSpriteIdentifier);
      data.PlayTTS = EditorGUILayout.Toggle("Play TTS", data.PlayTTS);
      DrawTTSBakeHint(data.PlayTTS, data.DialogueContent);
    }

    /// <summary>
    /// PlayTTS 플래그가 켜져 있을 때, 대상 텍스트의 bake 가능 여부(변수 포함 여부)를 안내한다.
    /// 변수({...})를 포함하면 bake되지 않고 런타임에 즉석 합성된다.
    /// </summary>
    private static void DrawTTSBakeHint(bool playTTS, string text)
    {
      if (!playTTS) return;

      if (ScenarioTTSBakeScanner.ContainsVariable(text))
      {
        EditorGUILayout.HelpBox(
          "이 텍스트는 변수({...})를 포함하여 사전 합성(bake)되지 않으며 런타임에 즉석 합성됩니다.",
          MessageType.Warning);
      }
      else
      {
        EditorGUILayout.HelpBox(
          "이 텍스트는 'Tools > Text to Speech Service > Bake Scenario Inline Audio' 로 사전 합성할 수 있습니다.",
          MessageType.Info);
      }
    }

    private void DrawChoiceFields(ScenarioChoiceNode data)
    {
      data.SpeakerName = EditorGUILayout.TextField("Speaker", data.SpeakerName);
      EditorGUILayout.PrefixLabel("Dialogue");
      data.DialogueContent = EditorGUILayout.TextArea(data.DialogueContent, GUILayout.Height(60));
      data.PortraitSpriteIdentifier = EditorGUILayout.TextField("Portrait Sprite", data.PortraitSpriteIdentifier);
      data.PlayTTS = EditorGUILayout.Toggle("Play TTS", data.PlayTTS);
      DrawTTSBakeHint(data.PlayTTS, data.DialogueContent);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

      for (int i = 0; i < data.Options.Count; i++)
      {
        var option = data.Options[i];
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        option.DisplayText = EditorGUILayout.TextField("Display Text", option.DisplayText);
        option.DisplayIconIdentifier = EditorGUILayout.TextField("Icon Identifier", option.DisplayIconIdentifier);

        var color = option.DisplayColor;
        var newColor = EditorGUILayout.ColorField("Display Color", color);
        option.DisplayColor = newColor;

        EditorGUILayout.LabelField("Next Node", option.NextNodeIdentifier ?? "(미연결)");
        targetNode.UpdateOptionPortLabel(option);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Remove Option"))
        {
          if (data.Options.Count > 1)
          {
            data.Options.RemoveAt(i);
            targetNode.RefreshPorts();
            break;
          }
          else
          {
            EditorGUILayout.HelpBox("최소 1개의 옵션이 필요합니다.", MessageType.Warning);
          }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
      }
    }

    private void DrawSoundFields(ScenarioSoundNode data)
    {
      data.SoundResourceIdentifier = EditorGUILayout.TextField("Sound Resource", data.SoundResourceIdentifier);
      data.WaitUntilFinished = EditorGUILayout.Toggle("Wait Until Finished", data.WaitUntilFinished);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawPlayerMoveFields(ScenarioPlayerMoveNode data)
    {
      data.DestinationType = (ScenarioMoveDestinationType)EditorGUILayout.EnumPopup("Destination Type", data.DestinationType);
      if (data.DestinationType == ScenarioMoveDestinationType.Position)
      {
        data.DestinationX = EditorGUILayout.FloatField("Destination X", data.DestinationX);
        data.DestinationY = EditorGUILayout.FloatField("Destination Y", data.DestinationY);
        data.DestinationZ = EditorGUILayout.FloatField("Destination Z", data.DestinationZ);
      }
      else
      {
        data.DestinationIdentifier = EditorGUILayout.TextField("Waypoint Identifier", data.DestinationIdentifier);
      }

      data.IgnoreGroundCheck = EditorGUILayout.Toggle("Ignore Ground Check", data.IgnoreGroundCheck);
      data.MoveMode = (ScenarioMoveMode)EditorGUILayout.EnumPopup("Move Mode", data.MoveMode);

      switch (data.MoveMode)
      {
        case ScenarioMoveMode.BySpeed:
          data.MoveSpeed = EditorGUILayout.FloatField("Move Speed", data.MoveSpeed);
          break;
        case ScenarioMoveMode.ByDuration:
          data.MoveDuration = EditorGUILayout.FloatField("Move Duration", data.MoveDuration);
          break;
        default:
          break;
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawNPCMoveFields(ScenarioNPCMoveNode data)
    {
      data.NPCIdentifier = EditorGUILayout.TextField("NPC Identifier", data.NPCIdentifier);
      data.DestinationType = (ScenarioMoveDestinationType)EditorGUILayout.EnumPopup("Destination Type", data.DestinationType);
      if (data.DestinationType == ScenarioMoveDestinationType.Position)
      {
        data.DestinationX = EditorGUILayout.FloatField("Destination X", data.DestinationX);
        data.DestinationY = EditorGUILayout.FloatField("Destination Y", data.DestinationY);
        data.DestinationZ = EditorGUILayout.FloatField("Destination Z", data.DestinationZ);
      }
      else
      {
        data.DestinationIdentifier = EditorGUILayout.TextField("Waypoint Identifier", data.DestinationIdentifier);
      }

      data.IgnoreGroundCheck = EditorGUILayout.Toggle("Ignore Ground Check", data.IgnoreGroundCheck);
      data.MoveMode = (ScenarioMoveMode)EditorGUILayout.EnumPopup("Move Mode", data.MoveMode);

      switch (data.MoveMode)
      {
        case ScenarioMoveMode.BySpeed:
          data.MoveSpeed = EditorGUILayout.FloatField("Move Speed", data.MoveSpeed);
          break;
        case ScenarioMoveMode.ByDuration:
          data.MoveDuration = EditorGUILayout.FloatField("Move Duration", data.MoveDuration);
          break;
        default:
          break;
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawCameraTargetFields(ScenarioCameraTargetNode data)
    {
      data.TargetObjectIdentifier = EditorGUILayout.TextField("Target Object", data.TargetObjectIdentifier);
      data.OffsetX = EditorGUILayout.FloatField("Offset X", data.OffsetX);
      data.OffsetY = EditorGUILayout.FloatField("Offset Y", data.OffsetY);
      data.OffsetZ = EditorGUILayout.FloatField("Offset Z", data.OffsetZ);
      data.BlendTime = EditorGUILayout.FloatField("Blend Time", data.BlendTime);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawInvokeEventFields(ScenarioInvokeEventNode data)
    {
      data.EventIdentifier = EditorGUILayout.TextField("Event Identifier", data.EventIdentifier);
      data.MoveNextBehavior = (ScenarioInvokeEventMoveNextBehavior)EditorGUILayout.EnumPopup("Move Next", data.MoveNextBehavior);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawServerInternalSignalFields(ScenarioServerInternalSignalNode data)
    {
      data.TargetIdentifier = EditorGUILayout.TextField("Target Identifier", data.TargetIdentifier);
      data.SignalIdentifier = EditorGUILayout.TextField("Signal Identifier", data.SignalIdentifier);
      data.Operation = (ScenarioServerInternalSignalOperationType)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.WaitForResolution = EditorGUILayout.Toggle("Wait For Resolution", data.WaitForResolution);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawValidatorFields(ScenarioValidatorNode data)
    {
      if (data.RootConditions == null)
      {
        data.RootConditions = new System.Collections.Generic.List<ScenarioValidatorRootCondition>();
      }

      var editableRootConditions = data.RootConditions as System.Collections.Generic.List<ScenarioValidatorRootCondition>
          ?? data.RootConditions.ToList();
      data.RootConditions = editableRootConditions;

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Root Conditions", EditorStyles.boldLabel);

      for (int i = 0; i < editableRootConditions.Count; i++)
      {
        var rootCondition = editableRootConditions[i] ?? new ScenarioValidatorRootCondition();
        editableRootConditions[i] = rootCondition;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Root Condition {i + 1}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
          editableRootConditions.RemoveAt(i);
          targetNode.RefreshTitle();
          window.NotifyNodeSelected(targetNode);
          EditorGUILayout.EndHorizontal();
          EditorGUILayout.EndVertical();
          break;
        }
        EditorGUILayout.EndHorizontal();

        DrawValidatorRootConditionFields(rootCondition);
        EditorGUILayout.EndVertical();
      }

      if (GUILayout.Button("Add Root Condition"))
      {
        editableRootConditions.Add(new ScenarioValidatorRootCondition
        {
          Condition = ScenarioValidatorCondition.RegistryContains
        });
      }

      if (editableRootConditions.Count == 0)
      {
        EditorGUILayout.HelpBox("Validator 노드에는 최소 1개 이상의 Root Condition이 필요합니다.", MessageType.Warning);
      }

      data.RootConditions = editableRootConditions;

      data.OnFailure = (ScenarioValidatorOnFailure)EditorGUILayout.EnumPopup("On Failure", data.OnFailure);
      data.FailureReportTargets = (ScenarioValidatorFailureReportTarget)EditorGUILayout.EnumFlagsField(
        "Failure Report Targets",
        data.FailureReportTargets);

      if (data.OnFailure == ScenarioValidatorOnFailure.Branching)
      {
        data.FailureNextIdentifier = EditorGUILayout.TextField("Failure Next", data.FailureNextIdentifier);
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawValidatorRootConditionFields(ScenarioValidatorRootCondition rootCondition)
    {
      if (rootCondition == null)
      {
        return;
      }

      rootCondition.Condition = (ScenarioValidatorCondition)EditorGUILayout.EnumPopup("Condition", rootCondition.Condition);

      switch (rootCondition.Condition)
      {
        case ScenarioValidatorCondition.PlayerCountEqual:
        case ScenarioValidatorCondition.PlayerCountNotEqual:
        case ScenarioValidatorCondition.PlayerCountLessThan:
        case ScenarioValidatorCondition.PlayerCountLessThanOrEqual:
        case ScenarioValidatorCondition.PlayerCountGreaterThan:
        case ScenarioValidatorCondition.PlayerCountGreaterThanOrEqual:
          rootCondition.TargetCount = EditorGUILayout.IntField("Target Count", rootCondition.TargetCount);
          break;
        case ScenarioValidatorCondition.RegistryContains:
          DrawValidatorRegistryRules(rootCondition);
          break;
        case ScenarioValidatorCondition.PlayerAssignedTag:
          rootCondition.PlayerTag = EditorGUILayout.TextField("Player Tag", rootCondition.PlayerTag);
          rootCondition.PlayerScope = (ScenarioValidatorPlayerScope)EditorGUILayout.EnumPopup("Player Scope", rootCondition.PlayerScope);
          break;
      }
    }

    private void DrawValidatorRegistryRules(ScenarioValidatorRootCondition rootCondition)
    {
      if (rootCondition.ValidationRules == null)
      {
        rootCondition.ValidationRules = new System.Collections.Generic.List<ScenarioValidatorRule>();
      }

      var rules = rootCondition.ValidationRules as System.Collections.Generic.List<ScenarioValidatorRule>
          ?? rootCondition.ValidationRules.ToList();
      rootCondition.ValidationRules = rules;

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Registry Validation Rules", EditorStyles.boldLabel);

      for (int i = 0; i < rules.Count; i++)
      {
        var rule = rules[i] ?? new ScenarioValidatorRule();
        rules[i] = rule;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Rule {i + 1}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
          rules.RemoveAt(i);
          targetNode.RefreshTitle();
          window.NotifyNodeSelected(targetNode);
          EditorGUILayout.EndHorizontal();
          EditorGUILayout.EndVertical();
          break;
        }
        EditorGUILayout.EndHorizontal();

        rule.Type = (ScenarioValidatorRuleType)EditorGUILayout.EnumPopup("Type", rule.Type);
        rule.Condition = (ScenarioValidatorRuleCondition)EditorGUILayout.EnumPopup("Condition", rule.Condition);
        rule.RegistryType = (RegistryType)EditorGUILayout.EnumPopup("Registry Type", rule.RegistryType);
        rule.RegistryIdentifier = EditorGUILayout.TextField("Registry Identifier", rule.RegistryIdentifier);

        EditorGUILayout.EndVertical();
      }

      if (GUILayout.Button("Add Registry Rule"))
      {
        rules.Add(new ScenarioValidatorRule
        {
          Type = ScenarioValidatorRuleType.Registry,
          Condition = ScenarioValidatorRuleCondition.Contains,
          RegistryType = RegistryType.Waypoint
        });
        targetNode.RefreshTitle();
      }

      if (rules.Count == 0)
      {
        EditorGUILayout.HelpBox("RegistryContains 조건에는 최소 1개 이상의 Rule이 필요합니다.", MessageType.Warning);
      }

      rootCondition.ValidationRules = rules;
    }

    private void DrawParallelFields(ScenarioParallelNode data)
    {
      data.WaitMode = (ScenarioWaitMode)EditorGUILayout.EnumPopup("Wait Mode", data.WaitMode);
      data.AllocationType = (ScenarioParallelAllocationType)EditorGUILayout.EnumPopup("Allocation Type", data.AllocationType);
      data.WhenBranchingPlayerNotMatched = (ScenarioParallelMismatchHandling)EditorGUILayout.EnumPopup("On Mismatch", data.WhenBranchingPlayerNotMatched);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Branches", EditorStyles.boldLabel);

      for (int i = 0; i < data.Branches.Count; i++)
      {
        var branch = data.Branches[i];
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();
        var newIdentifier = EditorGUILayout.TextField("Branch Identifier", branch.Identifier);
        if (EditorGUI.EndChangeCheck())
        {
          branch.Identifier = newIdentifier;
          targetNode.UpdateBranchPortLabel(branch);
        }

        branch.CompletionConditionIdentifier =
            EditorGUILayout.TextField("Completion Condition", branch.CompletionConditionIdentifier);

        var currentTags = branch.RequiredPlayerTags == null || branch.RequiredPlayerTags.Count == 0
          ? string.Empty
          : string.Join(",", branch.RequiredPlayerTags);
        var tagText = EditorGUILayout.TextField("Required Player Tags(csv)", currentTags);
        branch.RequiredPlayerTags = tagText
          .Split(',')
          .Select(each => each.Trim())
          .Where(each => !string.IsNullOrEmpty(each))
          .ToList();

        branch.RequiredPlayerTagsMatchMode = (ScenarioPlayerTagMatchMode)EditorGUILayout.EnumPopup(
          "Player Tag Match Mode",
          branch.RequiredPlayerTagsMatchMode);

        var currentForbiddenTags = branch.ForbiddenPlayerTags == null || branch.ForbiddenPlayerTags.Count == 0
          ? string.Empty
          : string.Join(",", branch.ForbiddenPlayerTags);
        var forbiddenTagText = EditorGUILayout.TextField("Forbidden Player Tags(csv)", currentForbiddenTags);
        branch.ForbiddenPlayerTags = forbiddenTagText
          .Split(',')
          .Select(each => each.Trim())
          .Where(each => !string.IsNullOrEmpty(each))
          .ToList();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Remove Branch"))
        {
          if (data.Branches.Count > 2)
          {
            var modifiableBranches = data.Branches.Cast<ScenarioParallelBranch>().ToList();
            modifiableBranches.RemoveAt(i);
            data.Branches = modifiableBranches;
            targetNode.RefreshPorts();
            break;
          }
          else
          {
            EditorGUILayout.HelpBox("Parallel 노드는 최소 2개 이상의 브랜치가 필요합니다.", MessageType.Warning);
          }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
      }
    }

    private void DrawQuestControlFields(ScenarioQuestControlNode data)
    {
      data.Operation = (ScenarioQuestOperationType)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.FailureStrategy = (ScenarioQuestFailureStrategy)EditorGUILayout.EnumPopup("Failure Strategy", data.FailureStrategy);

      if (data.Quest == null)
      {
        data.Quest = new QuestData();
      }

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Quest", EditorStyles.boldLabel);
      data.Quest.Id = EditorGUILayout.TextField("Id", data.Quest.Id);
      data.Quest.Title = EditorGUILayout.TextField("Title", data.Quest.Title);
      data.Quest.WaypointIdentifier = EditorGUILayout.TextField("Waypoint Identifier", data.Quest.WaypointIdentifier);
      EditorGUILayout.LabelField("Description");
      data.Quest.Description = EditorGUILayout.TextArea(data.Quest.Description, GUILayout.Height(60));
      EditorGUILayout.LabelField("Quest Content");
      data.Quest.QuestContent = EditorGUILayout.TextArea(data.Quest.QuestContent, GUILayout.Height(40));
      data.Quest.IsTracked = EditorGUILayout.Toggle("Track", data.Quest.IsTracked);

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawQuestWaypointHighlightFields(ScenarioQuestWaypointHighlightNode data)
    {
      data.WaypointIdentifier = EditorGUILayout.TextField("Waypoint Identifier", data.WaypointIdentifier);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawDelayFields(ScenarioDelayNode data)
    {
      data.DurationSeconds = EditorGUILayout.FloatField("Duration Seconds", data.DurationSeconds);
      data.WaitUntil = (ScenarioDelayWaitUntil)EditorGUILayout.EnumPopup("Wait Until", data.WaitUntil);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawInteractionFields(ScenarioInteractionNode data)
    {
      data.ActorScope = (ScenarioInteractionActorScope)EditorGUILayout.EnumPopup("Actor Scope", data.ActorScope);
      data.TargetIdentifier = EditorGUILayout.TextField("Target Identifier", data.TargetIdentifier);
      data.RequiredItemIdentifier = EditorGUILayout.TextField("Required Item", data.RequiredItemIdentifier);
      data.InteractionType = (ScenarioInteractionType)EditorGUILayout.EnumPopup("Interaction Type", data.InteractionType);
      data.CompletionConditionIdentifier = EditorGUILayout.TextField("Completion Condition", data.CompletionConditionIdentifier);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawCombineItemFields(ScenarioCombineItemNode data)
    {
      if (data.InputItemIdentifiers == null)
      {
        data.InputItemIdentifiers = new System.Collections.Generic.List<string>();
      }

      var inputItems = data.InputItemIdentifiers.ToList();

      EditorGUILayout.LabelField("Input Items", EditorStyles.boldLabel);
      for (int i = 0; i < inputItems.Count; i++)
      {
        EditorGUILayout.BeginHorizontal();
        inputItems[i] = EditorGUILayout.TextField($"Item {i + 1}", inputItems[i]);
        if (GUILayout.Button("-", GUILayout.Width(22)))
        {
          inputItems.RemoveAt(i);
          i--;
        }
        EditorGUILayout.EndHorizontal();
      }

      if (GUILayout.Button("Add Input Item"))
      {
        inputItems.Add(string.Empty);
      }

      data.InputItemIdentifiers = inputItems;

      data.OutputItemIdentifier = EditorGUILayout.TextField("Output Item", data.OutputItemIdentifier);
      data.AutoCombine = EditorGUILayout.Toggle("Auto Combine", data.AutoCombine);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawQuizFields(ScenarioQuizNode data)
    {
      data.Question = EditorGUILayout.TextField("Question", data.Question);

      if (data.Options == null)
      {
        data.Options = new System.Collections.Generic.List<string>();
      }

      var options = data.Options.ToList();
      EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
      for (int i = 0; i < options.Count; i++)
      {
        EditorGUILayout.BeginHorizontal();
        options[i] = EditorGUILayout.TextField($"Option {i}", options[i]);
        if (GUILayout.Button("-", GUILayout.Width(22)))
        {
          options.RemoveAt(i);
          i--;
        }
        EditorGUILayout.EndHorizontal();
      }

      if (GUILayout.Button("Add Option"))
      {
        options.Add(string.Empty);
      }

      data.Options = options;

      if (data.CorrectIndex < 0)
      {
        data.CorrectIndex = 0;
      }

      if (data.Options.Count > 0 && data.CorrectIndex >= data.Options.Count)
      {
        data.CorrectIndex = data.Options.Count - 1;
      }

      data.CorrectIndex = EditorGUILayout.IntField("Correct Index", data.CorrectIndex);
      data.FeedbackCorrect = EditorGUILayout.TextField("Feedback Correct", data.FeedbackCorrect);
      data.FeedbackIncorrect = EditorGUILayout.TextField("Feedback Incorrect", data.FeedbackIncorrect);
      data.PlayTTS = EditorGUILayout.Toggle("Play TTS", data.PlayTTS);
      DrawTTSBakeHint(data.PlayTTS, data.Question);
      EditorGUILayout.LabelField("On Correct", data.OnCorrectNextIdentifier ?? "(미연결)");
      EditorGUILayout.LabelField("On Incorrect", data.OnIncorrectNextIdentifier ?? "(미연결)");
    }

    private void DrawStateUpdateFields(ScenarioStateUpdateNode data)
    {
      data.TargetEntityIdentifier = EditorGUILayout.TextField("Target Entity", data.TargetEntityIdentifier);
      data.StateKey = EditorGUILayout.TextField("State Key", data.StateKey);
      data.StateValue = EditorGUILayout.TextField("State Value", data.StateValue);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawPlayTTSFields(ScenarioPlayTTSNode data)
    {
      data.TranscriptIdentifier = EditorGUILayout.TextField("Transcript Identifier", data.TranscriptIdentifier);
      data.WaitUntilFinished = EditorGUILayout.Toggle("Wait Until Finished", data.WaitUntilFinished);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Variables (Override)", EditorStyles.boldLabel);

      if (data.Variables == null)
        data.Variables = new System.Collections.Generic.Dictionary<string, string>();

      var keys = new System.Collections.Generic.List<string>(data.Variables.Keys);
      for (int i = 0; i < keys.Count; i++)
      {
        string key = keys[i];
        EditorGUILayout.BeginHorizontal();
        string newKey = EditorGUILayout.TextField(key, GUILayout.Width(120));
        string newVal = EditorGUILayout.TextField(data.Variables[key]);
        if (GUILayout.Button("-", GUILayout.Width(22)))
        {
          data.Variables.Remove(key);
          break;
        }
        EditorGUILayout.EndHorizontal();

        if (!string.Equals(newKey, key, System.StringComparison.Ordinal))
        {
          data.Variables.Remove(key);
          if (!data.Variables.ContainsKey(newKey))
            data.Variables[newKey] = newVal;
        }
        else
        {
          data.Variables[key] = newVal;
        }
      }

      if (GUILayout.Button("Add Variable"))
        data.Variables[$"var{data.Variables.Count}"] = string.Empty;

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawTagModificationFields(ScenarioPlayerTagNode data)
    {
      data.Operation = (ScenarioPlayerTagOperationType)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.Scope = (ScenarioPlayerTagScope)EditorGUILayout.EnumPopup("Scope", data.Scope);

      switch (data.Operation)
      {
        case ScenarioPlayerTagOperationType.Add:
        case ScenarioPlayerTagOperationType.Remove:
          data.Tag = EditorGUILayout.TextField("Tag", data.Tag);
          break;

        case ScenarioPlayerTagOperationType.Change:
          data.FromTag = EditorGUILayout.TextField("From Tag", data.FromTag);
          data.ToTag = EditorGUILayout.TextField("To Tag", data.ToTag);
          break;
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawEntityPresetSpawnFields(ScenarioEntityPresetSpawnNode data)
    {
      data.PresetIdentifier = EditorGUILayout.TextField("Preset Identifier", data.PresetIdentifier);
      data.SpawnedEntityIdentifier = EditorGUILayout.TextField("Spawned Entity Id", data.SpawnedEntityIdentifier);
      data.PositionSourceEntityIdentifier = EditorGUILayout.TextField("Position Source Entity", data.PositionSourceEntityIdentifier);
      data.PositionX = EditorGUILayout.FloatField("Position X", data.PositionX);
      data.PositionY = EditorGUILayout.FloatField("Position Y", data.PositionY);
      data.PositionZ = EditorGUILayout.FloatField("Position Z", data.PositionZ);
      data.ResultStateKey = EditorGUILayout.TextField("Result State Key", data.ResultStateKey);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawEntityTagFields(ScenarioEntityTagNode data)
    {
      data.TargetEntityIdentifier = EditorGUILayout.TextField("Target Entity", data.TargetEntityIdentifier);
      data.TargetEntityStateKey = EditorGUILayout.TextField("Target Entity State Key", data.TargetEntityStateKey);
      data.Operation = (ScenarioPlayerTagOperationType)EditorGUILayout.EnumPopup("Operation", data.Operation);

      switch (data.Operation)
      {
        case ScenarioPlayerTagOperationType.Add:
        case ScenarioPlayerTagOperationType.Remove:
          data.Tag = EditorGUILayout.TextField("Tag", data.Tag);
          break;
        case ScenarioPlayerTagOperationType.Change:
          data.FromTag = EditorGUILayout.TextField("From Tag", data.FromTag);
          data.ToTag = EditorGUILayout.TextField("To Tag", data.ToTag);
          break;
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawEntityInitFields(ScenarioEntityInitNode data)
    {
      EditorGUILayout.LabelField("Target (Preset Spawn)", EditorStyles.boldLabel);
      data.PresetIdentifier = EditorGUILayout.TextField("Preset Identifier", data.PresetIdentifier);
      data.EntityIdentifier = EditorGUILayout.TextField("Entity Identifier", data.EntityIdentifier);
      data.PositionSourceEntityIdentifier = EditorGUILayout.TextField("Position Source Entity", data.PositionSourceEntityIdentifier);
      data.PositionX = EditorGUILayout.FloatField("Position X", data.PositionX);
      data.PositionY = EditorGUILayout.FloatField("Position Y", data.PositionY);
      data.PositionZ = EditorGUILayout.FloatField("Position Z", data.PositionZ);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Target (Existing Entity)", EditorStyles.boldLabel);
      data.TargetEntityIdentifier = EditorGUILayout.TextField("Target Entity Id", data.TargetEntityIdentifier);
      data.TargetEntityStateKey = EditorGUILayout.TextField("Target Entity State Key", data.TargetEntityStateKey);
      data.ResultStateKey = EditorGUILayout.TextField("Result State Key", data.ResultStateKey);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("State Operations", EditorStyles.boldLabel);

      if (data.StateOperations == null)
        data.StateOperations = new System.Collections.Generic.List<ScenarioEntityStateOperation>();

      for (int i = 0; i < data.StateOperations.Count; i++)
      {
        var op = data.StateOperations[i];
        if (op == null) continue;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Op {i + 1}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
          data.StateOperations.RemoveAt(i);
          EditorGUILayout.EndHorizontal();
          EditorGUILayout.EndVertical();
          break;
        }
        EditorGUILayout.EndHorizontal();

        op.Kind = (ScenarioEntityStateOperationKind)EditorGUILayout.EnumPopup("Kind", op.Kind);
        op.Key = EditorGUILayout.TextField("Key", op.Key);
        if (op.Kind == ScenarioEntityStateOperationKind.StateStore)
          op.Value = EditorGUILayout.TextField("Value", op.Value);
        else
          op.DisplayActive = EditorGUILayout.Toggle("Display Active", op.DisplayActive);

        EditorGUILayout.EndVertical();
      }

      if (GUILayout.Button("Add State Operation"))
        data.StateOperations.Add(new ScenarioEntityStateOperation());

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawTriageAssessControlFields(ScenarioTriageAssessControlNode data)
    {
      data.TargetEntityIdentifier = EditorGUILayout.TextField("Target Entity", data.TargetEntityIdentifier);
      data.Assessable = EditorGUILayout.Toggle("Assessable", data.Assessable);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawPatientMedicalStatePresetFields(ScenarioPatientMedicalStatePresetNode data)
    {
      data.TargetEntityIdentifier = EditorGUILayout.TextField("Target Entity", data.TargetEntityIdentifier);
      data.TargetEntityStateKey = EditorGUILayout.TextField("Target Entity State Key", data.TargetEntityStateKey);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("환자 기술자 (PatientDescriptor)", EditorStyles.boldLabel);
      data.Name = NullableTextField("Name", data.Name);
      data.Sex = NullableEnumField<TriageTrainer.Entity.Patient.Sex>("Sex", data.Sex);
      data.Age = NullableIntField("Age", data.Age);
      data.BloodType = NullableEnumField<TriageTrainer.Entity.Patient.BloodType>("Blood Type", data.BloodType);
      data.IntendedTriage = NullableEnumField<TriageTrainer.Entity.Patient.TriageLevel>("Intended Triage", data.IntendedTriage);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("의식 (Consciousness)", EditorStyles.boldLabel);
      data.ConsciousnessGcs = NullableIntField("GCS (3~15)", data.ConsciousnessGcs);
      data.ConsciousnessLocLabel = NullableEnumField<TriageTrainer.Entity.Patient.LOCLabel>("LOC Label", data.ConsciousnessLocLabel);
      data.ConsciousnessPupillaryResponse = NullableEnumField<TriageTrainer.Entity.Patient.PupillaryResponse>("Pupillary Response", data.ConsciousnessPupillaryResponse);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("호흡 (Respiration)", EditorStyles.boldLabel);
      data.RespirationAwRR = NullableIntField("awRR (분당 호흡수)", data.RespirationAwRR);
      data.RespirationTypeValue = NullableEnumField<TriageTrainer.Entity.Patient.RespirationType>("Type", data.RespirationTypeValue);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("맥박 (Pulse)", EditorStyles.boldLabel);
      data.PulseRate = NullableIntField("Rate (분당)", data.PulseRate);
      data.PulseForceType = NullableEnumField<TriageTrainer.Entity.Patient.BloodPulseForceType>("Force Type", data.PulseForceType);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("혈압 (Blood Pressure)", EditorStyles.boldLabel);
      data.BloodPressureSystolic = NullableIntField("Systolic (수축기)", data.BloodPressureSystolic);
      data.BloodPressureDiastolic = NullableIntField("Diastolic (이완기)", data.BloodPressureDiastolic);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("피부 (Skin)", EditorStyles.boldLabel);
      data.SkinColorHue = NullableEnumField<TriageTrainer.Entity.Patient.SkinColorHue>("Color Hue", data.SkinColorHue);
      data.SkinTemperatureType = NullableEnumField<TriageTrainer.Entity.Patient.SkinTemperatureType>("Temperature Type", data.SkinTemperatureType);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("기타", EditorStyles.boldLabel);
      data.IsCardiacArrest = NullableBoolField("Is Cardiac Arrest", data.IsCardiacArrest);

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    /// <summary>
    /// null 허용 string 필드 편집기. 빈 문자열 입력 시 null로 저장한다.
    /// </summary>
    private static string NullableTextField(string label, string current)
    {
      var result = EditorGUILayout.TextField(label, current ?? string.Empty);
      return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// null 허용 int 필드 편집기. 빈 문자열 입력 시 null을 반환한다.
    /// </summary>
    private static int? NullableIntField(string label, int? current)
    {
      var text = EditorGUILayout.TextField(label, current.HasValue ? current.Value.ToString() : string.Empty);
      if (string.IsNullOrWhiteSpace(text)) return null;
      return int.TryParse(text, out var parsed) ? parsed : current;
    }

    /// <summary>
    /// null 허용 bool 필드 편집기. "(not set) / true / false" 3-state 팝업.
    /// </summary>
    private static bool? NullableBoolField(string label, bool? current)
    {
      var options = new[] { "(not set)", "true", "false" };
      int selected = current.HasValue ? (current.Value ? 1 : 2) : 0;
      int newSelected = EditorGUILayout.Popup(label, selected, options);
      return newSelected == 0 ? (bool?)null : newSelected == 1;
    }

    /// <summary>
    /// null 허용 Enum 필드 편집기. "(not set)" 항목을 첫 번째에 추가한다.
    /// </summary>
    private static TEnum? NullableEnumField<TEnum>(string label, TEnum? current) where TEnum : struct, System.Enum
    {
      var names = System.Enum.GetNames(typeof(TEnum));
      var display = new string[names.Length + 1];
      display[0] = "(not set)";
      names.CopyTo(display, 1);

      int selected = current.HasValue ? System.Array.IndexOf(names, current.Value.ToString()) + 1 : 0;
      int newSelected = EditorGUILayout.Popup(label, selected, display);
      if (newSelected == 0) return null;
      return (TEnum)System.Enum.Parse(typeof(TEnum), names[newSelected - 1]);
    }
  }
}
