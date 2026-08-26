using System.Linq;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
        case ScenarioNodeType.DisinteractableDialogue:
          DrawDisinteractableDialogueFields((ScenarioDisinteractableDialogueNode)data);
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
        case ScenarioNodeType.NPCControl:
          DrawNPCControlFields((ScenarioNPCControlNode)data);
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
        case ScenarioNodeType.QuestMark:
          DrawQuestMarkFields((ScenarioQuestMarkNode)data);
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
        case ScenarioNodeType.TimeControl:
          DrawTimeControlFields((ScenarioTimeControlNode)data);
          break;
        case ScenarioNodeType.SignalListener:
          DrawSignalListenerFields((ScenarioSignalListenerNode)data);
          break;
        case ScenarioNodeType.EntityStateSignalBinding:
          DrawEntityStateSignalBindingFields((ScenarioEntityStateSignalBindingNode)data);
          break;
        case ScenarioNodeType.SignalCounter:
          DrawSignalCounterFields((ScenarioSignalCounterNode)data);
          break;
        case ScenarioNodeType.ChatPrint:
          DrawChatPrintFields((ScenarioChatPrintNode)data);
          break;
        case ScenarioNodeType.ExecuteCommand:
          DrawExecuteCommandFields((ScenarioExecuteCommandNode)data);
          break;
        case ScenarioNodeType.ItemSubmissionConfig:
          DrawItemSubmissionConfigFields((ScenarioItemSubmissionConfigNode)data);
          break;
        case ScenarioNodeType.NpcInteractControl:
          DrawNpcInteractControlFields((ScenarioNpcInteractControlNode)data);
          break;
        case ScenarioNodeType.ManualEntrypoint:
          DrawManualEntrypointFields((ScenarioManualEntrypointNode)data);
          break;
        case ScenarioNodeType.BedSnap:
          DrawBedSnapFields((ScenarioBedSnapNode)data);
          break;
        case ScenarioNodeType.ReturnToOrigin:
          DrawReturnToOriginFields((ScenarioReturnToOriginNode)data);
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
      data.TtsVoiceProfile = DrawTtsVoiceProfile(data.TtsVoiceProfile);
      DrawTTSBakeHint(data.PlayTTS, data.DialogueContent);
    }

    private void DrawDisinteractableDialogueFields(ScenarioDisinteractableDialogueNode data)
    {
      data.SpeakerName = EditorGUILayout.TextField("Speaker", data.SpeakerName);
      EditorGUILayout.PrefixLabel("Dialogue");
      data.DialogueContent = EditorGUILayout.TextArea(data.DialogueContent, GUILayout.Height(60));
      data.PortraitSpriteIdentifier = EditorGUILayout.TextField("Portrait Sprite", data.PortraitSpriteIdentifier);
      data.FadeInDuration = DrawTimeValue("Fade In", data.FadeInDuration);
      data.DisplayDuration = DrawTimeValue("Display", data.DisplayDuration);
      data.FadeOutDuration = DrawTimeValue("Fade Out", data.FadeOutDuration);
      data.PlayTTS = EditorGUILayout.Toggle("Play TTS", data.PlayTTS);
      data.TtsVoiceProfile = DrawTtsVoiceProfile(data.TtsVoiceProfile);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private static ScenarioTimeValue DrawTimeValue(string label, ScenarioTimeValue value)
    {
      EditorGUILayout.BeginHorizontal();
      double amount = System.Math.Max(0d, EditorGUILayout.DoubleField(label, value.Value));
      var unit = (ScenarioTimeUnit)EditorGUILayout.EnumPopup(value.Unit, GUILayout.Width(110));
      EditorGUILayout.EndHorizontal();
      return new ScenarioTimeValue(amount, unit);
    }

    /// <summary>노드의 TTS 프로필을 하나의 구조체 그룹으로 편집한다.</summary>
    private static ScenarioTTSVoiceProfile DrawTtsVoiceProfile(ScenarioTTSVoiceProfile value)
    {
      value ??= new ScenarioTTSVoiceProfile { Preset = TTSVoiceStyle.None };
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("TTS Voice Profile", EditorStyles.boldLabel);
      var preset = (TTSVoiceStyle)EditorGUILayout.EnumPopup("Preset", value.Preset ?? TTSVoiceStyle.None);
      value.Preset = preset;

      bool usePreset = preset != TTSVoiceStyle.None;
      using (new EditorGUI.DisabledScope(usePreset))
      {
        value.VoiceIdentifier = EditorGUILayout.TextField("Custom Identifier", value.VoiceIdentifier);
        value.VoiceStyleName = EditorGUILayout.TextField("Custom Style Name", value.VoiceStyleName);
        value.Language = EditorGUILayout.TextField("Custom Language", value.Language);
        value.Speed = EditorGUILayout.FloatField("Custom Speed", value.Speed);
        value.TotalStep = EditorGUILayout.IntField("Custom Total Step", value.TotalStep);
      }
      if (usePreset)
        EditorGUILayout.HelpBox("프리셋 사용 중에는 커스텀 필드를 수정할 수 없습니다.", MessageType.None);
      EditorGUILayout.EndVertical();
      return value;
    }

    /// <summary>
    /// PlayTTS 플래그가 켜져 있을 때, 대상 텍스트의 bake 가능 여부(변수 포함 여부)를 안내한다.
    /// 변수({...})를 포함하면 bake되지 않고 런타임에 즉석 합성된다.
    /// </summary>
    private static void DrawTTSBakeHint(bool playTTS, string text)
    {
      if (!playTTS)
        return;

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
      data.TtsVoiceProfile = DrawTtsVoiceProfile(data.TtsVoiceProfile);
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

    private void DrawNPCControlFields(ScenarioNPCControlNode data)
    {
      data.Mode = (ScenarioNPCControlMode)EditorGUILayout.EnumPopup("Mode", data.Mode);
      data.NPCIdentifier = EditorGUILayout.TextField("NPC Identifier", data.NPCIdentifier);

      if (data.Mode == ScenarioNPCControlMode.Update)
      {
        data.InteractOperation = (ScenarioNPCInteractCrudOperation)EditorGUILayout.EnumPopup(
          "Interact CRUD", data.InteractOperation);
        if (data.InteractOperation != ScenarioNPCInteractCrudOperation.None)
          data.InteractableIdentifier = EditorGUILayout.TextField("Interactable Identifier", data.InteractableIdentifier);
        if (data.InteractOperation == ScenarioNPCInteractCrudOperation.Update)
          data.InteractEnabled = EditorGUILayout.Toggle("Interact Enabled", data.InteractEnabled ?? true);
        if (data.InteractOperation == ScenarioNPCInteractCrudOperation.Read)
          data.ResultStateKey = EditorGUILayout.TextField("Result State Key", data.ResultStateKey);
        data.DisplayName = NullableTextField("Display Name", data.DisplayName);
        data.ShowOverheadName = NullableBoolField("Show Overhead Name", data.ShowOverheadName);
      }
      else
      {
        data.DestinationType = (ScenarioMoveDestinationType)EditorGUILayout.EnumPopup(
          "Destination Type", data.DestinationType);
        if (data.DestinationType == ScenarioMoveDestinationType.Position)
        {
          data.DestinationX = EditorGUILayout.FloatField("Destination X", data.DestinationX);
          data.DestinationY = EditorGUILayout.FloatField("Destination Y", data.DestinationY);
          data.DestinationZ = EditorGUILayout.FloatField("Destination Z", data.DestinationZ);
        }
        else
        {
          data.DestinationIdentifier = EditorGUILayout.TextField(
            "Waypoint Identifier", data.DestinationIdentifier);
        }

        data.IgnoreGroundCheck = EditorGUILayout.Toggle("Ignore Ground Check", data.IgnoreGroundCheck);
        data.MoveMode = (ScenarioMoveMode)EditorGUILayout.EnumPopup("Move Mode", data.MoveMode);
        if (data.MoveMode == ScenarioMoveMode.BySpeed)
          data.MoveSpeed = EditorGUILayout.FloatField("Move Speed", data.MoveSpeed);
        else if (data.MoveMode == ScenarioMoveMode.ByDuration)
          data.MoveDuration = EditorGUILayout.FloatField("Move Duration", data.MoveDuration);
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
      rootCondition.MatchMode = (ScenarioValidatorMatchMode)EditorGUILayout.EnumPopup(
        new GUIContent("Match Mode", "All=모든 규칙 충족(AND), Any=하나 이상 충족(OR)"),
        rootCondition.MatchMode);

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
      data.SkipCompletionDisplayDelay = EditorGUILayout.Toggle(
        "Skip Completion Display Delay", data.SkipCompletionDisplayDelay);
      bool persistProgress = data.PersistProgressOnSessionEnd ?? false;
      persistProgress = EditorGUILayout.Toggle("Persist Progress On Session End", persistProgress);
      data.PersistProgressOnSessionEnd = persistProgress;
      data.QuestDefinitionIdentifier = EditorGUILayout.TextField(
        "Quest Definition",
        data.QuestDefinitionIdentifier ?? string.Empty);

      if (data.Quest == null)
        DrawReferencedQuestDefinition(data.QuestDefinitionIdentifier);

      var useInlineQuestData = EditorGUILayout.Toggle("Inline Quest Data", data.Quest != null);
      if (!useInlineQuestData)
      {
        // 외부 quest definition만 사용하는 노드는 quest:null 상태를 유지해야 한다.
        // 인스펙터에서 노드를 선택했다는 이유만으로 빈 QuestData를 만들면 저장 JSON이
        // 불필요하게 변경되고, 불완전한 inline quest가 스키마 검증을 실패시킨다.
        data.Quest = null;
      }
      else
      {
        data.Quest ??= new QuestData();
      }

      if (data.Quest != null)
      {
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
        data.Quest.PersistProgressOnSessionEnd = EditorGUILayout.Toggle(
          "Persist Progress On Session End", data.Quest.PersistProgressOnSessionEnd);

        DrawQuestWaypointReachedCriteria(data.Quest.Tasks, "Tasks", true);
        DrawQuestWaypointReachedCriteria(data.Quest.CompletionCriteria, "Completion Criteria", true);
        DrawQuestPresentationBindings(data.Quest.PresentationBindings ??= new System.Collections.Generic.List<QuestPresentationBinding>());
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private static void DrawQuestPresentationBindings(System.Collections.Generic.List<QuestPresentationBinding> bindings)
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Presentation Bindings", EditorStyles.boldLabel);
      int removeIndex = -1;
      for (int i = 0; i < bindings.Count; i++)
      {
        var binding = bindings[i] ??= new QuestPresentationBinding();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        binding.Activation = (QuestPresentationActivation)EditorGUILayout.EnumPopup("Activation", binding.Activation);
        binding.CompletionCriteriaIdentifier = EditorGUILayout.TextField("Completion Criteria", binding.CompletionCriteriaIdentifier ?? string.Empty);
        binding.TargetType = (QuestPresentationTargetType)EditorGUILayout.EnumPopup("Target Type", binding.TargetType);
        binding.EntityIdentifier = EditorGUILayout.TextField("Entity Identifier", binding.EntityIdentifier ?? string.Empty);
        binding.InteractionIdentifier = EditorGUILayout.TextField("Interaction Identifier", binding.InteractionIdentifier ?? string.Empty);
        binding.IconIdentifier = EditorGUILayout.TextField("Icon Identifier", binding.IconIdentifier ?? string.Empty);
        binding.IconMode = (QuestPresentationIconMode)EditorGUILayout.EnumPopup("Icon Mode", binding.IconMode);
        binding.Priority = EditorGUILayout.IntField("Priority", binding.Priority);
        binding.ShowWhenUntracked = EditorGUILayout.Toggle("Show When Untracked", binding.ShowWhenUntracked);
        if (GUILayout.Button("Remove Binding"))
          removeIndex = i;
        EditorGUILayout.EndVertical();
      }

      if (removeIndex >= 0)
        bindings.RemoveAt(removeIndex);
      if (GUILayout.Button("Add Presentation Binding"))
        bindings.Add(new QuestPresentationBinding());
    }

    private void DrawReferencedQuestDefinition(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        EditorGUILayout.HelpBox("Quest Definition 또는 Inline Quest Data를 지정해야 합니다.", MessageType.Warning);
        return;
      }

      if (!QuestDefinitionRegistry.TryGetGlobal(identifier, out var definition) || definition == null)
      {
        EditorGUILayout.HelpBox(
          $"Quest Definition '{identifier}'를 Resources/Quest에서 찾을 수 없습니다. " +
          "Scenario의 questDefinitionIncludes와 definition identifier를 확인하세요.",
          MessageType.Warning);
        return;
      }

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Referenced Quest Definition (Read Only)", EditorStyles.boldLabel);
      if (GUILayout.Button("Open Quest Definition in Quests"))
        window?.OpenQuestView(definition.Identifier);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Identifier", definition.Identifier);
      EditorGUILayout.LabelField("Title", string.IsNullOrWhiteSpace(definition.Title) ? "(없음)" : definition.Title);
      EditorGUILayout.LabelField("Description", string.IsNullOrWhiteSpace(definition.Description) ? "(없음)" : definition.Description);
      DrawQuestContentLink(definition);
      EditorGUILayout.LabelField("Scope", definition.Scope.ToString());
      EditorGUILayout.LabelField("Tracked By Default", definition.IsTrackedByDefault.ToString());
      EditorGUILayout.LabelField("Trackable", definition.IsTrackable.ToString());
      EditorGUILayout.LabelField("Auto Complete", definition.IsAutoComplete.ToString());
      EditorGUILayout.LabelField("Ordinal", definition.IsOrdinal.ToString());
      EditorGUILayout.LabelField("Persist Progress", definition.PersistProgressOnSessionEnd.ToString());
      EditorGUILayout.LabelField("Waypoint", string.IsNullOrWhiteSpace(definition.WaypointIdentifier) ? "(없음)" : definition.WaypointIdentifier);
      DrawQuestCriteriaPreview("Tasks", definition.Tasks);
      DrawQuestCriteriaPreview("Completion Criteria", definition.CompletionCriteria);
      EditorGUILayout.EndVertical();
    }

    private void DrawQuestContentLink(QuestDefinition definition)
    {
      var content = string.IsNullOrWhiteSpace(definition.QuestContent) ? "(없음)" : definition.QuestContent;
      var rect = EditorGUILayout.GetControlRect();
      EditorGUI.LabelField(rect, "Quest Content", content);
      if (string.IsNullOrWhiteSpace(definition.QuestContent))
        return;

      var currentEvent = Event.current;
      if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.clickCount == 2 && rect.Contains(currentEvent.mousePosition))
      {
        window?.OpenQuestView(definition.Identifier);
        currentEvent.Use();
      }
    }

    private static void DrawQuestCriteriaPreview(string label, System.Collections.Generic.IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
      if (criteria == null || criteria.Count == 0)
      {
        EditorGUILayout.LabelField("(정의된 항목 없음)");
        return;
      }

      for (int index = 0; index < criteria.Count; index++)
        DrawQuestCriterionPreview(criteria[index], index + 1, 0);
    }

    private static void DrawQuestCriterionPreview(QuestCompletionCriteria criterion, int index, int depth)
    {
      if (criterion == null)
        return;

      string indent = new string(' ', depth * 2);
      string target = criterion.Type switch
      {
        QuestCompletionCriteriaType.WaypointReached => $"waypoint={criterion.WaypointIdentifier}, distance={criterion.ReachDistance:0.##}m",
        QuestCompletionCriteriaType.InteractionSignalReceived => $"signal={criterion.SignalId}",
        QuestCompletionCriteriaType.InventoryContains => $"item={criterion.ItemId}",
        _ => string.Empty
      };
      string display = string.IsNullOrWhiteSpace(criterion.DisplayTextContent) ? "" : $", text={criterion.DisplayTextContent}";
      EditorGUILayout.LabelField($"{indent}{index}. {criterion.Type} (count={criterion.Count}{(string.IsNullOrWhiteSpace(target) ? string.Empty : $", {target}")}{display})");

      if (criterion.Conditions == null)
        return;

      for (int childIndex = 0; childIndex < criterion.Conditions.Count; childIndex++)
        DrawQuestCriterionPreview(criterion.Conditions[childIndex], childIndex + 1, depth + 1);
    }

    private static void DrawQuestWaypointReachedCriteria(
      System.Collections.Generic.List<QuestCompletionCriteria> criteria,
      string label,
      bool allowAdd)
    {
      if (criteria == null)
        return;

      bool hasWaypointReached = false;
      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion == null)
          continue;

        if (criterion.Type == QuestCompletionCriteriaType.WaypointReached)
        {
          if (!hasWaypointReached)
          {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{label} / WaypointReached", EditorStyles.boldLabel);
            hasWaypointReached = true;
          }

          criterion.Identifier = EditorGUILayout.TextField(
            $"Criterion Identifier [{i}]",
            criterion.Identifier ?? string.Empty);
          criterion.WaypointIdentifier = EditorGUILayout.TextField(
            $"Waypoint [{i}]",
            criterion.WaypointIdentifier ?? string.Empty);
          criterion.ReachDistance = Mathf.Max(
            0.01f,
            EditorGUILayout.FloatField($"Detection Range [{i}] (m)", criterion.ReachDistance));

          if (GUILayout.Button($"Remove WaypointReached [{i}]"))
          {
            criteria.RemoveAt(i);
            i--;
            continue;
          }
        }

        DrawQuestWaypointReachedCriteria(criterion.Conditions, $"{label} [{i}]", false);
      }

      if (allowAdd && GUILayout.Button($"Add WaypointReached to {label}"))
      {
        criteria.Add(new QuestCompletionCriteria
        {
          Type = QuestCompletionCriteriaType.WaypointReached
        });
      }
    }

    private void DrawQuestWaypointHighlightFields(ScenarioQuestWaypointHighlightNode data)
    {
      data.WaypointIdentifier = EditorGUILayout.TextField("Waypoint Identifier", data.WaypointIdentifier);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawQuestMarkFields(ScenarioQuestMarkNode data)
    {
      data.Operation = (ScenarioQuestMarkOperationType)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.TargetType = (QuestPresentationTargetType)EditorGUILayout.EnumPopup("Target Type", data.TargetType);
      data.EntityIdentifier = EditorGUILayout.TextField("Entity Identifier", data.EntityIdentifier);
      if (data.TargetType == QuestPresentationTargetType.Interaction)
        data.InteractionIdentifier = EditorGUILayout.TextField("Interaction Identifier", data.InteractionIdentifier);
      data.IconIdentifier = EditorGUILayout.TextField("Icon Identifier", data.IconIdentifier);
      EditorGUILayout.HelpBox("Icon Identifier 를 비우면 대상 종류별 기본 퀘스트 마크 아이콘을 사용한다.", MessageType.None);
      data.Priority = EditorGUILayout.IntField("Priority", data.Priority);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawDelayFields(ScenarioDelayNode data)
    {
      var duration = data.Duration;
      duration.Value = EditorGUILayout.DoubleField("Duration Value", duration.Value);
      duration.Unit = (ScenarioTimeUnit)EditorGUILayout.EnumPopup("Duration Unit", duration.Unit);
      data.Duration = duration;
      data.WaitUntil = (ScenarioDelayWaitUntil)EditorGUILayout.EnumPopup("Wait Until", data.WaitUntil);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawTimeControlFields(ScenarioTimeControlNode data)
    {
      data.Operation = (ScenarioTimeOperationType)EditorGUILayout.EnumPopup("Operation", data.Operation);

      // Hide 를 제외한 모든 연산은 대상 타이머 식별자를 사용한다.
      if (data.Operation != ScenarioTimeOperationType.Hide)
      {
        data.TimerId = EditorGUILayout.TextField("Timer Id", data.TimerId);
      }

      switch (data.Operation)
      {
        case ScenarioTimeOperationType.Create:
          data.Direction = (ScenarioTimeDirection)EditorGUILayout.EnumPopup("Direction", data.Direction);
          if (data.Direction == ScenarioTimeDirection.Countdown)
          {
            data.DurationSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("Duration Seconds (target)", data.DurationSeconds));
            EditorGUILayout.HelpBox(
              "카운트다운: 목표 시간에서 0으로 감소합니다. Start Seconds(표시 남은값)가 0이면 Duration 을 시작값으로 사용합니다.",
              MessageType.None);
          }
          data.StartSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("Start Seconds (display)", data.StartSeconds));
          EditorGUILayout.HelpBox("Create 는 정지 상태로 생성하며 화면에 표시하지 않습니다. Start 로 흐름, Show 로 표시하세요.", MessageType.None);
          break;

        case ScenarioTimeOperationType.Set:
          data.StartSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("Display Seconds", data.StartSeconds));
          data.DurationSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("New Target Seconds (0=keep)", data.DurationSeconds));
          EditorGUILayout.HelpBox("Set 은 현재 표시값을 절대값으로 설정합니다. New Target 이 양수이면 카운트다운 목표(총)도 재설정합니다.", MessageType.None);
          break;

        case ScenarioTimeOperationType.Hide:
          EditorGUILayout.HelpBox("Hide 는 화면 표시만 끕니다(타이머 상태/흐름 유지).", MessageType.None);
          break;
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawSignalListenerFields(ScenarioSignalListenerNode data)
    {
      data.ListenerIdentifier = EditorGUILayout.TextField("Listener Identifier", data.ListenerIdentifier);
      data.Operation = (ScenarioSignalListenerOperation)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.SourceSignalIdentifier = EditorGUILayout.TextField("Source Signal", data.SourceSignalIdentifier);
      data.OutputSignalIdentifier = EditorGUILayout.TextField("Output Signal", data.OutputSignalIdentifier);

      var requiredSignals = data.RequiredSignalIdentifiers == null || data.RequiredSignalIdentifiers.Count == 0
        ? string.Empty
        : string.Join(",", data.RequiredSignalIdentifiers);
      var requiredText = EditorGUILayout.TextField("Required Signals(csv)", requiredSignals);
      data.RequiredSignalIdentifiers = requiredText
        .Split(',')
        .Select(each => each.Trim())
        .Where(each => !string.IsNullOrEmpty(each))
        .ToList();

      data.ConsumeOnce = EditorGUILayout.Toggle("Consume Once", data.ConsumeOnce);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawEntityStateSignalBindingFields(ScenarioEntityStateSignalBindingNode data)
    {
      data.BindingIdentifier = EditorGUILayout.TextField("Binding Identifier", data.BindingIdentifier);
      data.Operation = (ScenarioEntityStateSignalBindingOperation)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.TargetEntityIdentifier = EditorGUILayout.TextField("Target Entity", data.TargetEntityIdentifier);
      data.TargetEntityStateKey = EditorGUILayout.TextField("Target Entity State Key", data.TargetEntityStateKey);
      data.EventName = EditorGUILayout.TextField("Event Name", data.EventName);
      data.EventKey = EditorGUILayout.TextField("Event Key", data.EventKey);
      data.OutputSignalIdentifier = EditorGUILayout.TextField("Output Signal", data.OutputSignalIdentifier);
      data.ConsumeOnce = EditorGUILayout.Toggle("Consume Once", data.ConsumeOnce);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawSignalCounterFields(ScenarioSignalCounterNode data)
    {
      data.CounterIdentifier = EditorGUILayout.TextField("Counter Identifier", data.CounterIdentifier);
      data.Operation = (ScenarioSignalCounterOperation)EditorGUILayout.EnumPopup("Operation", data.Operation);
      data.SourceSignalPrefix = EditorGUILayout.TextField("Source Signal Prefix", data.SourceSignalPrefix);
      data.Threshold = EditorGUILayout.IntField("Threshold", data.Threshold);
      data.OutputSignalIdentifier = EditorGUILayout.TextField("Output Signal", data.OutputSignalIdentifier);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawReturnToOriginFields(ScenarioReturnToOriginNode data)
    {
      EditorGUILayout.PrefixLabel("Description");
      data.Description = EditorGUILayout.TextArea(data.Description, GUILayout.Height(40));
      EditorGUILayout.HelpBox(
        "곁가지(ManualEntrypoint 준비 체인 · 병렬 브랜치)를 여기서 끝내고 원래 흐름으로 돌아갑니다. "
        + "준비 체인이면 진입 지점으로 돌아가 그 노드의 Next로 이어지고, 병렬 브랜치면 그 브랜치만 완료됩니다. "
        + "메인 흐름에서 만나면 아무 일도 하지 않고 지나갑니다. 이 노드는 Next를 쓰지 않습니다.",
        MessageType.Info);
    }

    private void DrawBedSnapFields(ScenarioBedSnapNode data)
    {
      data.BedEntityIdentifier = EditorGUILayout.TextField("Bed Entity", data.BedEntityIdentifier);
      data.BedEntityStateKey = EditorGUILayout.TextField("Bed State Key", data.BedEntityStateKey);
      data.SnapPointIdentifier = EditorGUILayout.TextField("Snap Point", data.SnapPointIdentifier);
      data.Teleport = EditorGUILayout.Toggle("Teleport", data.Teleport);
      data.IgnoreFailure = EditorGUILayout.Toggle("Ignore Failure", data.IgnoreFailure);
      EditorGUILayout.HelpBox(
        "이동식 환자 침대를 포지셔닝 포인트에 붙입니다. 환자가 결합된 침대면 환자도 함께 옮겨집니다. "
        + "Teleport가 켜져 있으면 거리와 무관하게 포인트로 옮긴 뒤 붙이고, 꺼져 있으면 스냅 범위 안에 있을 때만 붙습니다. "
        + "Bed Entity와 Bed State Key 중 하나는 채워야 합니다.",
        MessageType.Info);
    }

    private void DrawManualEntrypointFields(ScenarioManualEntrypointNode data)
    {
      data.EntrypointIdentifier = EditorGUILayout.TextField("Entrypoint Id", data.EntrypointIdentifier);
      EditorGUILayout.LabelField(" ", $"명령 별칭: {data.ResolvedEntrypointIdentifier}");
      data.ManualEnterSetupIdentifier = EditorGUILayout.TextField("Manual Enter Setup", data.ManualEnterSetupIdentifier);
      EditorGUILayout.PrefixLabel("Description");
      data.Description = EditorGUILayout.TextArea(data.Description, GUILayout.Height(40));
      EditorGUILayout.HelpBox(
        "평소에는 그냥 지나가는 표식입니다. /scenario enter <entrypoint> 로 이 지점까지 건너뛸 수 있고, "
        + "그때만 Manual Enter Setup 체인을 먼저 실행한 뒤 Next 로 이어집니다.",
        MessageType.Info);
    }

    private void DrawChatPrintFields(ScenarioChatPrintNode data)
    {
      EditorGUILayout.PrefixLabel("Message");
      data.Message = EditorGUILayout.TextArea(data.Message ?? string.Empty, GUILayout.Height(40));
      data.Targets = (ScenarioChatPrintTarget)EditorGUILayout.EnumFlagsField("Targets", data.Targets);
      data.Broadcast = EditorGUILayout.Toggle("Broadcast", data.Broadcast);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawExecuteCommandFields(ScenarioExecuteCommandNode data)
    {
      data.CommandLine = EditorGUILayout.TextField("Command Line", data.CommandLine);
      EditorGUILayout.HelpBox(
        "시나리오 전용 지급: give-if-missing <item> [count] [target]. 대상별 인벤토리에 아이템이 없을 때만 지급합니다. 예: give-if-missing checklist_paper @a",
        MessageType.Info);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawItemSubmissionConfigFields(ScenarioItemSubmissionConfigNode data)
    {
      EditorGUILayout.LabelField("Target (Preset Spawn)", EditorStyles.boldLabel);
      data.PresetIdentifier = EditorGUILayout.TextField("Preset Identifier", data.PresetIdentifier);
      data.SpawnedEntityIdentifier = EditorGUILayout.TextField("Spawned Entity Id", data.SpawnedEntityIdentifier);
      data.PositionSourceEntityIdentifier = EditorGUILayout.TextField("Position Source Entity", data.PositionSourceEntityIdentifier);
      data.PositionX = EditorGUILayout.FloatField("Position X", data.PositionX);
      data.PositionY = EditorGUILayout.FloatField("Position Y", data.PositionY);
      data.PositionZ = EditorGUILayout.FloatField("Position Z", data.PositionZ);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Target (Existing Entity)", EditorStyles.boldLabel);
      data.TargetIdentifier = EditorGUILayout.TextField("Target Identifier", data.TargetIdentifier);
      data.TargetStateKey = EditorGUILayout.TextField("Target State Key", data.TargetStateKey);

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Required Items", EditorStyles.boldLabel);

      if (data.RequiredItems == null)
      {
        data.RequiredItems = new System.Collections.Generic.List<ScenarioItemRequirement>();
      }

      for (int i = 0; i < data.RequiredItems.Count; i++)
      {
        var req = data.RequiredItems[i] ?? new ScenarioItemRequirement();
        data.RequiredItems[i] = req;

        EditorGUILayout.BeginHorizontal();
        req.ItemIdentifier = EditorGUILayout.TextField($"Item {i + 1}", req.ItemIdentifier);
        req.Count = Mathf.Max(1, EditorGUILayout.IntField(req.Count, GUILayout.Width(48)));
        if (GUILayout.Button("-", GUILayout.Width(22)))
        {
          data.RequiredItems.RemoveAt(i);
          EditorGUILayout.EndHorizontal();
          break;
        }
        EditorGUILayout.EndHorizontal();
      }

      if (GUILayout.Button("Add Required Item"))
      {
        data.RequiredItems.Add(new ScenarioItemRequirement { Count = 1 });
      }

      data.CompletionSignalIdentifier = EditorGUILayout.TextField("Completion Signal", data.CompletionSignalIdentifier);
      data.Enabled = EditorGUILayout.Toggle("Enabled", data.Enabled);
      data.ResultStateKey = EditorGUILayout.TextField("Result State Key", data.ResultStateKey);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawNpcInteractControlFields(ScenarioNpcInteractControlNode data)
    {
      data.NpcIdentifier = EditorGUILayout.TextField("NPC Identifier", data.NpcIdentifier);
      data.InteractableIdentifier = EditorGUILayout.TextField("Interactable Identifier", data.InteractableIdentifier);
      data.Operation = (ScenarioNpcInteractControlOperation)EditorGUILayout.EnumPopup("Operation", data.Operation);
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
      data.TtsVoiceProfile = DrawTtsVoiceProfile(data.TtsVoiceProfile);
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
      data.TtsVoiceProfile = DrawTtsVoiceProfile(data.TtsVoiceProfile);

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
      data.RotationX = EditorGUILayout.FloatField("Rotation X", data.RotationX);
      data.RotationY = EditorGUILayout.FloatField("Rotation Y", data.RotationY);
      data.RotationZ = EditorGUILayout.FloatField("Rotation Z", data.RotationZ);
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
        if (op == null)
          continue;

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
      EditorGUILayout.LabelField("전이 (Transition)", EditorStyles.boldLabel);
      data.TransitionMode = (PatientMedicalStateTransitionMode)EditorGUILayout.EnumPopup(
        "Transition Mode", data.TransitionMode);
      // 점차 변화(Gradual) 선택 시에만 소요 시간 필드를 표시한다.
      if (data.TransitionMode == PatientMedicalStateTransitionMode.Gradual)
      {
        data.TransitionDurationSeconds = Mathf.Max(0f,
          EditorGUILayout.FloatField("소요 시간(초)", data.TransitionDurationSeconds));
      }

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
      data.ConsciousnessEyeOpening = NullableEnumField<TriageTrainer.Entity.Patient.EyeOpeningResponse>("E: Eye Opening (1~4)", data.ConsciousnessEyeOpening);
      data.ConsciousnessVerbal = NullableEnumField<TriageTrainer.Entity.Patient.VerbalResponse>("V: Verbal Response (1~5)", data.ConsciousnessVerbal);
      data.ConsciousnessMotor = NullableEnumField<TriageTrainer.Entity.Patient.MotorResponse>("M: Motor Response (1~6)", data.ConsciousnessMotor);
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
      if (string.IsNullOrWhiteSpace(text))
        return null;
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
      if (newSelected == 0)
        return null;
      return (TEnum)System.Enum.Parse(typeof(TEnum), names[newSelected - 1]);
    }
  }
}
