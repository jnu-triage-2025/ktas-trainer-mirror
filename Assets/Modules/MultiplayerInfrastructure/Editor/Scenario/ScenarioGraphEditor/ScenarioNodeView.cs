using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Quest;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioNodeView : Node
  {
    private const float NodeMinWidth = 260f;
    private const float NodeMaxWidth = 340f;
    private const int VisitPreviewLimit = 3;
    private static readonly Color ExecutionBorderColor = new Color(0.29f, 0.82f, 0.47f, 1f);
    private static readonly Color ExecutionFillColor = new Color(0.12f, 0.24f, 0.16f, 0.95f);
    private static readonly Color ExecutionTitleColor = new Color(0.16f, 0.34f, 0.22f, 1f);
    private static readonly Color DefaultTitleColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color DefaultFillColor = new Color(0.16f, 0.16f, 0.16f, 1f);

    // DefaultInit(기본 진입) 노드 표시용 색상. 런타임 실행 강조(초록)와 구별되도록 금색 계열을 사용한다.
    private const string DefaultInitBadgeText = "★ Default Init";
    private static readonly Color DefaultInitTitleColor = new Color(0.55f, 0.42f, 0.10f, 1f);
    private static readonly Color DefaultInitBadgeColor = new Color(0.72f, 0.55f, 0.10f, 1f);

    public IScenarioNode Data { get; }
    public Port InputPort { get; private set; }
    public Port DefaultOutputPort { get; private set; }
    public Vector2 DefaultSize => new Vector2(260f, 160f);
    private Vector2 _editorPosition = new Vector2(0, 0);
    public Vector2 EditorPosition => _editorPosition;

    private readonly ScenarioGraphAuthoringWindow window;
    private readonly ScenarioGraphView graphView;
    private Label _summaryLabel;
    private Foldout _visitHistoryFoldout;
    private Label _visitHistoryLabel;
    private VisualElement _inlineEditorContainer;
    private Label _defaultInitBadge;
    private bool _defaultInitMarked;
    private bool _executionHighlighted;
    private readonly List<int> _runtimeVisitOrders = new List<int>();

    private readonly Dictionary<ScenarioChoiceOption, Port> choicePorts = new Dictionary<ScenarioChoiceOption, Port>();
    private readonly Dictionary<ScenarioParallelBranch, Port> branchPorts = new Dictionary<ScenarioParallelBranch, Port>();

    /// <summary>
    /// 병렬 노드의 브랜치들을 수정하기 위한 가변 리스트
    /// 병렬 노드 자체는 IReadOnlyList 로 외부에 노출됨
    /// </summary>
    private List<ScenarioParallelBranch> mutableBranches => (Data as ScenarioParallelNode)?.Branches as List<ScenarioParallelBranch>;

    public ScenarioNodeView(ScenarioGraphAuthoringWindow window, ScenarioGraphView graphView, IScenarioNode data)
    {
      this.window = window;
      this.graphView = graphView;
      Data = data;

      style.minWidth = NodeMinWidth;
      style.maxWidth = NodeMaxWidth;
      style.width = NodeMaxWidth;

      // 타이틀바의 접기/펼치기 화살표 버튼은 사용하지 않으므로 컨테이너째 제거한다.
      titleButtonContainer?.RemoveFromHierarchy();

      RefreshTitle();
      style.left = EditorPosition.x;
      style.top = EditorPosition.y;

      CreateInputPort();
      CreateOutputPorts();
      SetupNodeBody();

      RefreshExpandedState();
      RefreshPorts();
    }

    public void SetExecutionHighlighted(bool highlighted)
    {
      _executionHighlighted = highlighted;

      style.borderLeftWidth = highlighted ? 4f : 1f;
      style.borderRightWidth = highlighted ? 4f : 1f;
      style.borderTopWidth = highlighted ? 4f : 1f;
      style.borderBottomWidth = highlighted ? 4f : 1f;

      var borderColor = highlighted ? ExecutionBorderColor : new Color(0.25f, 0.25f, 0.25f, 1f);
      style.borderLeftColor = borderColor;
      style.borderRightColor = borderColor;
      style.borderTopColor = borderColor;
      style.borderBottomColor = borderColor;

      if (titleContainer != null)
      {
        titleContainer.style.backgroundColor = highlighted ? ExecutionTitleColor : ResolveBaseTitleColor();
      }

      if (mainContainer != null)
      {
        mainContainer.style.backgroundColor = highlighted ? ExecutionFillColor : DefaultFillColor;
      }
    }

    /// <summary>
    /// 이 노드가 시나리오의 기본 진입 노드(DefaultEntrypoint, 일명 DefaultInit)인지 표시한다.
    /// 타이틀바에 금색 배지를 추가하고 타이틀바 색을 칠해 다른 노드와 구분한다.
    /// 런타임 실행 강조(초록 테두리)와 독립적으로 동작하며 동시에 표시될 수 있다.
    /// </summary>
    public void SetDefaultInitMarked(bool marked)
    {
      if (_defaultInitMarked == marked)
        return;

      _defaultInitMarked = marked;

      if (titleContainer != null)
      {
        if (marked && _defaultInitBadge == null)
        {
          _defaultInitBadge = BuildDefaultInitBadge();
          titleContainer.Add(_defaultInitBadge);
        }
        else if (!marked && _defaultInitBadge != null)
        {
          titleContainer.Remove(_defaultInitBadge);
          _defaultInitBadge = null;
        }

        if (!_executionHighlighted)
        {
          titleContainer.style.backgroundColor = ResolveBaseTitleColor();
        }
      }
    }

    private Color ResolveBaseTitleColor()
    {
      return _defaultInitMarked ? DefaultInitTitleColor : DefaultTitleColor;
    }

    private static Label BuildDefaultInitBadge()
    {
      var badge = new Label(DefaultInitBadgeText);
      badge.style.fontSize = 9f;
      badge.style.color = Color.white;
      badge.style.backgroundColor = DefaultInitBadgeColor;
      badge.style.unityFontStyleAndWeight = FontStyle.Bold;
      badge.style.paddingLeft = 4f;
      badge.style.paddingRight = 4f;
      badge.style.paddingTop = 1f;
      badge.style.paddingBottom = 1f;
      badge.style.marginLeft = 3f;
      badge.style.marginRight = 3f;
      badge.style.alignSelf = Align.Center;
      badge.style.borderTopLeftRadius = 3f;
      badge.style.borderTopRightRadius = 3f;
      badge.style.borderBottomLeftRadius = 3f;
      badge.style.borderBottomRightRadius = 3f;
      badge.tooltip = "startNodeIdentifier 없이 시나리오를 시작할 때 사용되는 기본 진입 노드(defaultEntrypoint)입니다.";
      return badge;
    }

    public override void OnSelected()
    {
      base.OnSelected();
      window.NotifyNodeSelected(this);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
      base.BuildContextualMenu(evt);

      var isDefaultInit = window.GraphData != null
        && !string.IsNullOrEmpty(window.GraphData.DefaultEntrypoint)
        && window.GraphData.DefaultEntrypoint == Data?.Identifier;

      evt.menu.AppendAction("Set as Default Init", _ => window.SetDefaultEntrypoint(Data?.Identifier));
      evt.menu.AppendAction("Clear Default Init", _ => window.SetDefaultEntrypoint(null),
        isDefaultInit ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
    }

    public override void SetPosition(Rect newPos)
    {
      base.SetPosition(newPos);
      _editorPosition = newPos.position;
    }

    public void RefreshTitle()
    {
      title = Data.Identifier;
      RefreshSummaryLabel();
      RefreshVisitHistory();
    }

    public void SetRuntimeVisitOrders(IReadOnlyList<int> visitOrders)
    {
      _runtimeVisitOrders.Clear();
      if (visitOrders != null)
      {
        _runtimeVisitOrders.AddRange(visitOrders);
      }

      RefreshSummaryLabel();
      RefreshVisitHistory();
    }

    public new void RefreshPorts()
    {
      base.RefreshPorts();
      RefreshExpandedState();
      RefreshPortsInternal();
    }

    private void RefreshPortsInternal()
    {
      RefreshPort(InputPort);
      if (DefaultOutputPort != null)
      {
        RefreshPort(DefaultOutputPort);
      }

      foreach (var port in choicePorts.Values)
      {
        RefreshPort(port);
      }

      foreach (var port in branchPorts.Values)
      {
        RefreshPort(port);
      }
    }

    private static void RefreshPort(Port port)
    {
      if (port == null) return;
      port.portColor = Color.white;
    }

    private void CreateInputPort()
    {
      InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
      InputPort.portName = "In";
      inputContainer.Add(InputPort);
    }

    private void CreateOutputPorts()
    {
      choicePorts.Clear();
      branchPorts.Clear();

      switch (Data.NodeType)
      {
        case ScenarioNodeType.Dialogue:
        case ScenarioNodeType.DisinteractableDialogue:
        case ScenarioNodeType.Sound:
        case ScenarioNodeType.PlayerMove:
        case ScenarioNodeType.NPCMove:
        case ScenarioNodeType.NPCControl:
        case ScenarioNodeType.CameraTarget:
        case ScenarioNodeType.InvokeEvent:
        case ScenarioNodeType.ServerInternalSignal:
        case ScenarioNodeType.SignalListener:
        case ScenarioNodeType.EntityStateSignalBinding:
        case ScenarioNodeType.SignalCounter:
        case ScenarioNodeType.Validator:
        case ScenarioNodeType.QuestControl:
        case ScenarioNodeType.QuestWaypointHighlight:
        case ScenarioNodeType.Delay:
        case ScenarioNodeType.Interaction:
        case ScenarioNodeType.CombineItem:
        case ScenarioNodeType.StateUpdate:
        case ScenarioNodeType.PlayTTS:
        case ScenarioNodeType.PlayerTag:
        case ScenarioNodeType.EntityPresetSpawn:
        case ScenarioNodeType.EntityTag:
        case ScenarioNodeType.EntityInit:
        case ScenarioNodeType.TriageAssessControl:
        case ScenarioNodeType.PatientMedicalStatePreset:
        case ScenarioNodeType.ItemSubmissionConfig:
        case ScenarioNodeType.NpcInteractControl:
        case ScenarioNodeType.ChatPrint:
        case ScenarioNodeType.ExecuteCommand:
        case ScenarioNodeType.TimeControl:
          DefaultOutputPort = CreateStandardOutput("Next");
          break;

        case ScenarioNodeType.Quiz:
          AddQuizOutput("Correct", "quiz.correct");
          AddQuizOutput("Incorrect", "quiz.incorrect");
          break;

        case ScenarioNodeType.Choice:
          var choiceData = Data as ScenarioChoiceNode;
          if (choiceData == null)
            break;

          choiceData.Options ??= new List<ScenarioChoiceOption>();
          if (choiceData.Options.Count == 0)
          {
            choiceData.Options.Add(new ScenarioChoiceOption());
          }

          // displayText 는 스키마 필수(null 불허) 필드다. 자동 생성된 옵션에 기본 텍스트를
          // 미리 채워 노드 생성 직후 저장해도 검증에 실패하지 않게 한다.
          for (int i = 0; i < choiceData.Options.Count; i++)
          {
            var existingOption = choiceData.Options[i];
            if (existingOption != null && string.IsNullOrEmpty(existingOption.DisplayText))
            {
              existingOption.DisplayText = $"옵션 {i + 1}";
            }
          }

          foreach (var option in choiceData.Options)
          {
            AddChoiceOptionPort(option);
          }

          extensionContainer.Add(new Button(() =>
          {
            var newOption = new ScenarioChoiceOption
            {
              DisplayText = $"옵션 {choiceData.Options.Count + 1}"
            };
            choiceData.Options.Add(newOption);
            AddChoiceOptionPort(newOption);
          })
          { text = "Add Option" });
          break;

        case ScenarioNodeType.Parallel:
          var parallelData = Data as ScenarioParallelNode;
          if (parallelData == null)
            break;

          if (parallelData.Branches == null)
          {
            parallelData.Branches = new List<ScenarioParallelBranch>();
          }

          var branchList = parallelData.Branches as List<ScenarioParallelBranch> ?? new List<ScenarioParallelBranch>(parallelData.Branches);
          parallelData.Branches = branchList;

          if (branchList.Count < 2)
          {
            while (branchList.Count < 2)
            {
              branchList.Add(new ScenarioParallelBranch
              {
                Identifier = $"branch_{branchList.Count + 1}"
              });
            }
          }

          foreach (var branch in branchList)
          {
            AddParallelBranchPort(branch);
          }

          extensionContainer.Add(new Button(() =>
          {
            var branch = new ScenarioParallelBranch
            {
              Identifier = $"branch_{branchList.Count + 1}"
            };
            branchList.Add(branch);
            AddParallelBranchPort(branch);
          })
          { text = "Add Branch" });
          break;
      }
    }

    private void SetupNodeBody()
    {
      var label = new Label(Data.NodeType.ToString()) { style = { unityFontStyleAndWeight = FontStyle.Italic } };
      label.style.whiteSpace = WhiteSpace.Normal;
      mainContainer.Add(label);

      _summaryLabel = new Label();
      _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
      _summaryLabel.style.fontSize = 10;
      _summaryLabel.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
      mainContainer.Add(_summaryLabel);

      _visitHistoryFoldout = new Foldout
      {
        text = "Visit History",
        value = false
      };
      _visitHistoryFoldout.style.marginTop = 4;
      _visitHistoryFoldout.style.display = DisplayStyle.None;
      _visitHistoryLabel = new Label();
      _visitHistoryLabel.style.whiteSpace = WhiteSpace.Normal;
      _visitHistoryLabel.style.fontSize = 10;
      _visitHistoryLabel.style.color = new Color(0.78f, 0.88f, 0.80f, 1f);
      _visitHistoryFoldout.Add(_visitHistoryLabel);
      mainContainer.Add(_visitHistoryFoldout);

      _inlineEditorContainer = new VisualElement();
      _inlineEditorContainer.style.marginTop = 6;
      _inlineEditorContainer.style.flexDirection = FlexDirection.Column;
      mainContainer.Add(_inlineEditorContainer);

      BuildInlineEditor();
      RefreshSummaryLabel();
    }

    private void BuildInlineEditor()
    {
      if (_inlineEditorContainer == null)
        return;

      _inlineEditorContainer.Clear();

      AddIdentifierField();

      switch (Data.NodeType)
      {
        case ScenarioNodeType.Dialogue:
          BuildDialogueInlineEditor((ScenarioDialogueNode)Data);
          break;
        case ScenarioNodeType.DisinteractableDialogue:
          BuildDisinteractableDialogueInlineEditor((ScenarioDisinteractableDialogueNode)Data);
          break;
        case ScenarioNodeType.PlayTTS:
          BuildPlayTTSInlineEditor((ScenarioPlayTTSNode)Data);
          break;
        case ScenarioNodeType.Choice:
          BuildChoiceInlineEditor((ScenarioChoiceNode)Data);
          break;
        case ScenarioNodeType.InvokeEvent:
          BuildInvokeEventInlineEditor((ScenarioInvokeEventNode)Data);
          break;
        case ScenarioNodeType.ServerInternalSignal:
          BuildServerInternalSignalInlineEditor((ScenarioServerInternalSignalNode)Data);
          break;
        case ScenarioNodeType.Validator:
          BuildValidatorInlineEditor((ScenarioValidatorNode)Data);
          break;
        case ScenarioNodeType.QuestControl:
          BuildQuestControlInlineEditor((ScenarioQuestControlNode)Data);
          break;
        case ScenarioNodeType.PlayerTag:
          BuildPlayerTagInlineEditor((ScenarioPlayerTagNode)Data);
          break;
        case ScenarioNodeType.EntityPresetSpawn:
          BuildEntityPresetSpawnInlineEditor((ScenarioEntityPresetSpawnNode)Data);
          break;
        case ScenarioNodeType.EntityTag:
          BuildEntityTagInlineEditor((ScenarioEntityTagNode)Data);
          break;
        case ScenarioNodeType.EntityInit:
          BuildEntityInitInlineEditor((ScenarioEntityInitNode)Data);
          break;
        case ScenarioNodeType.TriageAssessControl:
          BuildTriageAssessControlInlineEditor((ScenarioTriageAssessControlNode)Data);
          break;
        case ScenarioNodeType.PatientMedicalStatePreset:
          BuildPatientMedicalStatePresetInlineEditor((ScenarioPatientMedicalStatePresetNode)Data);
          break;
        case ScenarioNodeType.ItemSubmissionConfig:
          BuildItemSubmissionConfigInlineEditor((ScenarioItemSubmissionConfigNode)Data);
          break;
        case ScenarioNodeType.NpcInteractControl:
          BuildNpcInteractControlInlineEditor((ScenarioNpcInteractControlNode)Data);
          break;
        case ScenarioNodeType.NPCControl:
          BuildNPCControlInlineEditor((ScenarioNPCControlNode)Data);
          break;
        case ScenarioNodeType.TimeControl:
          BuildTimeControlInlineEditor((ScenarioTimeControlNode)Data);
          break;
        case ScenarioNodeType.Parallel:
          break;
        default:
          AddNextIdentifierField(Data);
          break;
      }
    }

    private void AddIdentifierField()
    {
      var idField = new TextField("Id")
      {
        value = Data.Identifier ?? string.Empty
      };
      ConfigureTextField(idField, false);
      idField.RegisterValueChangedCallback(evt =>
      {
        if (window.TryRenameNode(this, evt.newValue))
          RefreshTitle();
        else
          idField.SetValueWithoutNotify(Data.Identifier ?? string.Empty);
      });
      _inlineEditorContainer.Add(idField);
    }

    private void BuildDialogueInlineEditor(ScenarioDialogueNode data)
    {
      AddTextField("Speaker", value => data.SpeakerName = value, data.SpeakerName);
      AddTextAreaField("Dialogue", value => data.DialogueContent = value, data.DialogueContent);
      AddToggleField("Interaction Required", value => data.InteractionRequired = value, data.InteractionRequired);
      AddOptionalFloatField("Auto Advance (sec)", value => data.AutoAdvanceSeconds = value, data.AutoAdvanceSeconds);
      AddToggleField("Play TTS", value => data.PlayTTS = value, data.PlayTTS);
      AddVoiceProfileField(data, value => data.TtsVoiceProfile = value, value => data.TtsVoiceIdentifier = value);
      AddNextIdentifierField(data);
    }

    private void BuildPlayTTSInlineEditor(ScenarioPlayTTSNode data)
    {
      AddTextField("Transcript", value => data.TranscriptIdentifier = value, data.TranscriptIdentifier);
      AddToggleField("Wait Until Finished", value => data.WaitUntilFinished = value, data.WaitUntilFinished);
      AddVoiceProfileField(data, value => data.TtsVoiceProfile = value, value => data.TtsVoiceIdentifier = value);
      AddNextIdentifierField(data);
    }

    private void AddVoiceProfileField(
      object owner,
      System.Action<ScenarioTTSVoiceProfile> setProfile,
      System.Action<string> setIdentifier)
    {
      var current = owner is ScenarioDialogueNode dialogue ? dialogue.TtsVoiceProfile
        : owner is ScenarioPlayTTSNode playTts ? playTts.TtsVoiceProfile : null;
      var preset = new EnumField("TTS Voice Preset", current?.Preset ?? TTSVoiceStyle.F1);
      preset.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is TTSVoiceStyle style)
        {
          var profile = new ScenarioTTSVoiceProfile { Preset = style };
          setProfile(profile);
          setIdentifier(profile.ToServiceProfile().VoiceIdentifier);
        }
      });
      _inlineEditorContainer.Add(preset);
      AddTextField("Custom Voice JSON ID", value =>
      {
        if (string.IsNullOrWhiteSpace(value)) return;
        setProfile(new ScenarioTTSVoiceProfile { VoiceIdentifier = value });
        setIdentifier(value);
      }, current?.VoiceIdentifier);
    }

    private void BuildDisinteractableDialogueInlineEditor(ScenarioDisinteractableDialogueNode data)
    {
      AddTextField("Speaker", value => data.SpeakerName = value, data.SpeakerName);
      AddTextAreaField("Dialogue", value => data.DialogueContent = value, data.DialogueContent);
      AddTimeValueField("Fade In", data.FadeInDuration, value => data.FadeInDuration = value);
      AddTimeValueField("Display", data.DisplayDuration, value => data.DisplayDuration = value);
      AddTimeValueField("Fade Out", data.FadeOutDuration, value => data.FadeOutDuration = value);
      AddToggleField("Play TTS", value => data.PlayTTS = value, data.PlayTTS);
      AddTextField("TTS Voice", value => data.TtsVoiceIdentifier = value, data.TtsVoiceIdentifier);
      AddNextIdentifierField(data);
    }

    private void BuildChoiceInlineEditor(ScenarioChoiceNode data)
    {
      data.Options ??= new List<ScenarioChoiceOption>();
      AddTextField("Speaker", value => data.SpeakerName = value, data.SpeakerName);
      AddTextAreaField("Dialogue", value => data.DialogueContent = value, data.DialogueContent);
      AddTextField("Option 1", value =>
      {
        var option = EnsureChoiceOptionEditable(data, 0);
        option.DisplayText = value;
        UpdateOptionPortLabel(option);
      }, GetChoiceOptionText(data, 0, "옵션 1"));
      AddTextField("Option 2", value =>
      {
        if (data.Options.Count <= 1 && string.IsNullOrEmpty(value))
          return;

        var option = EnsureChoiceOptionEditable(data, 1);
        option.DisplayText = value;
        UpdateOptionPortLabel(option);
      }, GetChoiceOptionText(data, 1, string.Empty));
      AddNextIdentifierField(data);
    }

    private void BuildInvokeEventInlineEditor(ScenarioInvokeEventNode data)
    {
      AddTextField("Event", value => data.EventIdentifier = value, data.EventIdentifier);
      var enumField = new EnumField("Move Next", data.MoveNextBehavior);
      enumField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioInvokeEventMoveNextBehavior value)
          data.MoveNextBehavior = value;
      });
      _inlineEditorContainer.Add(enumField);
      AddNextIdentifierField(data);
    }

    private void BuildServerInternalSignalInlineEditor(ScenarioServerInternalSignalNode data)
    {
      AddTextField("Target", value => data.TargetIdentifier = value, data.TargetIdentifier);
      AddTextField("Signal", value => data.SignalIdentifier = value, data.SignalIdentifier);

      var enumField = new EnumField("Operation", data.Operation);
      enumField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioServerInternalSignalOperationType value)
          data.Operation = value;
      });
      _inlineEditorContainer.Add(enumField);

      AddToggleField("Wait For Resolution", value => data.WaitForResolution = value, data.WaitForResolution);
      AddNextIdentifierField(data);
    }

    private void BuildValidatorInlineEditor(ScenarioValidatorNode data)
    {
      AddToggleField("Wait For Condition", value => data.WaitForCondition = value, data.WaitForCondition);
      AddOptionalFloatField("Wait Timeout (sec)", value => data.WaitTimeoutSeconds = value, data.WaitTimeoutSeconds);
      AddTextField("Failure Next", value => data.FailureNextIdentifier = value, data.FailureNextIdentifier);
      AddNextIdentifierField(data);
    }

    private void BuildQuestControlInlineEditor(ScenarioQuestControlNode data)
    {
      var opField = new EnumField("Operation", data.Operation);
      opField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioQuestOperationType value)
          data.Operation = value;
      });
      _inlineEditorContainer.Add(opField);

      AddTextField("Quest Definition", value => data.QuestDefinitionIdentifier = value, data.QuestDefinitionIdentifier);
      AddToggleField("Persist Progress On Session End", value => data.PersistProgressOnSessionEnd = value,
        data.PersistProgressOnSessionEnd ?? false);
      AddTextField("Quest Id", value =>
      {
        data.Quest ??= new QuestData();
        data.Quest.Id = value;
      }, data.Quest?.Id);
      AddNextIdentifierField(data);
    }

    private void BuildPlayerTagInlineEditor(ScenarioPlayerTagNode data)
    {
      var opField = new EnumField("Operation", data.Operation);
      opField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioPlayerTagOperationType value)
          data.Operation = value;
      });
      _inlineEditorContainer.Add(opField);

      AddTextField("Tag", value => data.Tag = value, data.Tag);
      AddNextIdentifierField(data);
    }

    private void BuildEntityPresetSpawnInlineEditor(ScenarioEntityPresetSpawnNode data)
    {
      AddTextField("Preset Id", value => data.PresetIdentifier = value, data.PresetIdentifier);
      AddTextField("Spawned Entity Id", value => data.SpawnedEntityIdentifier = value, data.SpawnedEntityIdentifier);
      AddNextIdentifierField(data);
    }

    private void BuildEntityTagInlineEditor(ScenarioEntityTagNode data)
    {
      AddTextField("Target Entity", value => data.TargetEntityIdentifier = value, data.TargetEntityIdentifier);
      var opField = new EnumField("Operation", data.Operation);
      opField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioPlayerTagOperationType value)
          data.Operation = value;
      });
      _inlineEditorContainer.Add(opField);
      AddTextField("Tag", value => data.Tag = value, data.Tag);
      AddNextIdentifierField(data);
    }

    private void BuildEntityInitInlineEditor(ScenarioEntityInitNode data)
    {
      AddTextField("Target Entity", value => data.TargetEntityIdentifier = value, data.TargetEntityIdentifier);
      AddTextField("Preset Id", value => data.PresetIdentifier = value, data.PresetIdentifier);
      AddTextField("Entity Id", value => data.EntityIdentifier = value, data.EntityIdentifier);
      AddNextIdentifierField(data);
    }

    private void BuildTriageAssessControlInlineEditor(ScenarioTriageAssessControlNode data)
    {
      AddTextField("Target Entity", value => data.TargetEntityIdentifier = value, data.TargetEntityIdentifier);
      AddToggleField("Assessable", value => data.Assessable = value, data.Assessable);
      AddNextIdentifierField(data);
    }

    private void BuildPatientMedicalStatePresetInlineEditor(ScenarioPatientMedicalStatePresetNode data)
    {
      AddTextField("Target Entity", value => data.TargetEntityIdentifier = value, data.TargetEntityIdentifier);

      // 전이 방식: Immediate(즉시) / Gradual(점차 변화). Gradual 선택 시 소요 시간 필드를 표시한다.
      var transitionField = new EnumField("Transition", data.TransitionMode);
      transitionField.RegisterValueChangedCallback(evt =>
      {
        data.TransitionMode = (PatientMedicalStateTransitionMode)evt.newValue;
        BuildInlineEditor();
      });
      _inlineEditorContainer.Add(transitionField);

      if (data.TransitionMode == PatientMedicalStateTransitionMode.Gradual)
      {
        var durationField = new FloatField("소요 시간(초)") { value = data.TransitionDurationSeconds };
        durationField.RegisterValueChangedCallback(evt =>
          data.TransitionDurationSeconds = Mathf.Max(0f, evt.newValue));
        _inlineEditorContainer.Add(durationField);
      }

      // 주요 식별 필드만 인라인으로 표시하고, 상세 수치는 Inspector 패널에서 편집한다
      var sexLabel = data.Sex.HasValue ? data.Sex.Value.ToString() : "(not set)";
      var ageLabel = data.Age.HasValue ? data.Age.Value.ToString() : "(not set)";
      _inlineEditorContainer.Add(new Label($"Sex: {sexLabel}  Age: {ageLabel}") { style = { fontSize = 10, color = new Color(0.85f, 0.85f, 0.85f) } });
      var gcsLabel = data.ConsciousnessGcs.HasValue ? $"GCS {data.ConsciousnessGcs}" : string.Empty;
      var evmLabel = (data.ConsciousnessEyeOpening.HasValue || data.ConsciousnessVerbal.HasValue || data.ConsciousnessMotor.HasValue)
        ? $"E{(int?)data.ConsciousnessEyeOpening}/V{(int?)data.ConsciousnessVerbal}/M{(int?)data.ConsciousnessMotor}" : string.Empty;
      var bpLabel = (data.BloodPressureSystolic.HasValue || data.BloodPressureDiastolic.HasValue)
        ? $"BP {data.BloodPressureSystolic}/{data.BloodPressureDiastolic}" : string.Empty;
      var summary = string.Join("  ", new[] { gcsLabel, evmLabel, bpLabel }.Where(s => !string.IsNullOrEmpty(s)));
      if (!string.IsNullOrEmpty(summary))
        _inlineEditorContainer.Add(new Label(summary) { style = { fontSize = 10, color = new Color(0.85f, 0.85f, 0.85f) } });
      AddNextIdentifierField(data);
    }

    private void BuildItemSubmissionConfigInlineEditor(ScenarioItemSubmissionConfigNode data)
    {
      AddTextField("Preset Id (spawn)", value => data.PresetIdentifier = value, data.PresetIdentifier);
      AddTextField("Target Id (existing)", value => data.TargetIdentifier = value, data.TargetIdentifier);
      AddTextField("Completion Signal", value => data.CompletionSignalIdentifier = value, data.CompletionSignalIdentifier);
      AddToggleField("Enabled", value => data.Enabled = value, data.Enabled);

      data.RequiredItems ??= new List<ScenarioItemRequirement>();

      _inlineEditorContainer.Add(new Label("Required Items (id x count)")
      {
        style = { fontSize = 10, color = new Color(0.85f, 0.85f, 0.85f), marginTop = 4 }
      });

      var itemsContainer = new VisualElement();
      _inlineEditorContainer.Add(itemsContainer);

      void RebuildItemsList()
      {
        itemsContainer.Clear();
        for (int i = 0; i < data.RequiredItems.Count; i++)
        {
          int index = i;
          var req = data.RequiredItems[index] ??= new ScenarioItemRequirement();

          var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

          var idField = new TextField { value = req.ItemIdentifier ?? string.Empty, style = { flexGrow = 1 } };
          idField.RegisterValueChangedCallback(evt => data.RequiredItems[index].ItemIdentifier = evt.newValue);
          row.Add(idField);

          var countField = new IntegerField { value = req.Count <= 0 ? 1 : req.Count, style = { width = 48 } };
          countField.RegisterValueChangedCallback(evt => data.RequiredItems[index].Count = evt.newValue <= 0 ? 1 : evt.newValue);
          row.Add(countField);

          var removeButton = new Button(() =>
          {
            data.RequiredItems.RemoveAt(index);
            RebuildItemsList();
          })
          { text = "-", style = { width = 22 } };
          row.Add(removeButton);

          itemsContainer.Add(row);
        }
      }

      RebuildItemsList();

      _inlineEditorContainer.Add(new Button(() =>
      {
        data.RequiredItems.Add(new ScenarioItemRequirement { Count = 1 });
        RebuildItemsList();
      })
      { text = "Add Required Item" });

      AddNextIdentifierField(data);
    }

    private void BuildNpcInteractControlInlineEditor(ScenarioNpcInteractControlNode data)
    {
      AddTextField("NPC Id", value => data.NpcIdentifier = value, data.NpcIdentifier);
      AddTextField("Interactable Id", value => data.InteractableIdentifier = value, data.InteractableIdentifier);

      var opField = new EnumField("Operation", data.Operation);
      opField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioNpcInteractControlOperation value)
          data.Operation = value;
      });
      _inlineEditorContainer.Add(opField);

      AddNextIdentifierField(data);
    }

    private void BuildNPCControlInlineEditor(ScenarioNPCControlNode data)
    {
      var modeField = new EnumField("Mode", data.Mode);
      modeField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioNPCControlMode value && value != data.Mode)
        {
          data.Mode = value;
          BuildInlineEditor();
        }
      });
      _inlineEditorContainer.Add(modeField);
      AddTextField("NPC Id", value => data.NPCIdentifier = value, data.NPCIdentifier);

      if (data.Mode == ScenarioNPCControlMode.Update)
      {
        var crudField = new EnumField("Interact CRUD", data.InteractOperation);
        crudField.RegisterValueChangedCallback(evt =>
        {
          if (evt.newValue is ScenarioNPCInteractCrudOperation value && value != data.InteractOperation)
          {
            data.InteractOperation = value;
            BuildInlineEditor();
          }
        });
        _inlineEditorContainer.Add(crudField);
        if (data.InteractOperation != ScenarioNPCInteractCrudOperation.None)
          AddTextField("Interactable Id", value => data.InteractableIdentifier = value, data.InteractableIdentifier);
        if (data.InteractOperation == ScenarioNPCInteractCrudOperation.Update)
          AddToggleField("Interact Enabled", value => data.InteractEnabled = value, data.InteractEnabled ?? true);
        if (data.InteractOperation == ScenarioNPCInteractCrudOperation.Read)
          AddTextField("Result State Key", value => data.ResultStateKey = value, data.ResultStateKey);
        AddTextField("Display Name", value => data.DisplayName = value, data.DisplayName);
        AddToggleField("Show Overhead Name", value => data.ShowOverheadName = value, data.ShowOverheadName ?? false);
      }
      else
      {
        var destinationField = new EnumField("Destination", data.DestinationType);
        destinationField.RegisterValueChangedCallback(evt =>
        {
          if (evt.newValue is ScenarioMoveDestinationType value && value != data.DestinationType)
          {
            data.DestinationType = value;
            BuildInlineEditor();
          }
        });
        _inlineEditorContainer.Add(destinationField);
        if (data.DestinationType == ScenarioMoveDestinationType.Waypoint)
        {
          AddTextField("Waypoint Id", value => data.DestinationIdentifier = value, data.DestinationIdentifier);
        }
        else
        {
          AddOptionalFloatField("Destination X", value => data.DestinationX = value ?? 0f, data.DestinationX);
          AddOptionalFloatField("Destination Y", value => data.DestinationY = value ?? 0f, data.DestinationY);
          AddOptionalFloatField("Destination Z", value => data.DestinationZ = value ?? 0f, data.DestinationZ);
        }
        AddToggleField("Ignore Ground", value => data.IgnoreGroundCheck = value, data.IgnoreGroundCheck);
        var moveModeField = new EnumField("Move Mode", data.MoveMode);
        moveModeField.RegisterValueChangedCallback(evt =>
        {
          if (evt.newValue is ScenarioMoveMode value && value != data.MoveMode)
          {
            data.MoveMode = value;
            BuildInlineEditor();
          }
        });
        _inlineEditorContainer.Add(moveModeField);
        if (data.MoveMode == ScenarioMoveMode.BySpeed)
          AddOptionalFloatField("Move Speed", value => data.MoveSpeed = value ?? 0f, data.MoveSpeed);
        else if (data.MoveMode == ScenarioMoveMode.ByDuration)
          AddOptionalFloatField("Move Duration", value => data.MoveDuration = value ?? 0f, data.MoveDuration);
      }

      AddNextIdentifierField(data);
    }

    private void BuildTimeControlInlineEditor(ScenarioTimeControlNode data)
    {
      var operationField = new EnumField("Operation", data.Operation);
      operationField.RegisterValueChangedCallback(evt =>
      {
        if (evt.newValue is ScenarioTimeOperationType value && value != data.Operation)
        {
          data.Operation = value;
          // 연산이 바뀌면 관련 파라미터만 다시 그린다.
          BuildInlineEditor();
        }
      });
      _inlineEditorContainer.Add(operationField);

      // Hide 를 제외한 모든 연산은 대상 타이머 식별자를 사용한다.
      if (data.Operation != ScenarioTimeOperationType.Hide)
      {
        AddTextField("Timer Id", value => data.TimerId = value, data.TimerId);
      }

      switch (data.Operation)
      {
        case ScenarioTimeOperationType.Create:
          var directionField = new EnumField("Direction", data.Direction);
          directionField.RegisterValueChangedCallback(evt =>
          {
            if (evt.newValue is ScenarioTimeDirection value)
              data.Direction = value;
          });
          _inlineEditorContainer.Add(directionField);
          AddPlainFloatField("Duration (sec, countdown)", value => data.DurationSeconds = Mathf.Max(0f, value), data.DurationSeconds);
          AddPlainFloatField("Start (sec, display)", value => data.StartSeconds = Mathf.Max(0f, value), data.StartSeconds);
          break;

        case ScenarioTimeOperationType.Set:
          AddPlainFloatField("Display (sec)", value => data.StartSeconds = Mathf.Max(0f, value), data.StartSeconds);
          AddPlainFloatField("New Target (sec, 0=keep)", value => data.DurationSeconds = Mathf.Max(0f, value), data.DurationSeconds);
          break;
      }

      AddNextIdentifierField(data);
    }

    private void AddPlainFloatField(string label, System.Action<float> setter, float current)
    {
      var field = new FloatField(label) { value = current };
      field.RegisterValueChangedCallback(evt => setter(evt.newValue));
      _inlineEditorContainer.Add(field);
    }

    private void AddNextIdentifierField(IScenarioNode data)
    {
      AddTextField("Next", value => data.NextIdentifier = value, data.NextIdentifier);
    }

    private void AddTextField(string label, System.Action<string> setter, string current)
    {
      var field = new TextField(label) { value = current ?? string.Empty };
      ConfigureTextField(field, false);
      field.RegisterValueChangedCallback(evt => setter(evt.newValue));
      _inlineEditorContainer.Add(field);
    }

    private void AddTextAreaField(string label, System.Action<string> setter, string current)
    {
      var field = new TextField(label)
      {
        multiline = true,
        value = current ?? string.Empty
      };
      ConfigureTextField(field, true);
      field.style.minHeight = 54;
      field.RegisterValueChangedCallback(evt => setter(evt.newValue));
      _inlineEditorContainer.Add(field);
    }

    private void AddToggleField(string label, System.Action<bool> setter, bool current)
    {
      var field = new Toggle(label) { value = current };
      field.RegisterValueChangedCallback(evt => setter(evt.newValue));
      _inlineEditorContainer.Add(field);
    }

    private void AddOptionalFloatField(string label, System.Action<float?> setter, float? current)
    {
      var field = new TextField(label)
      {
        value = current.HasValue ? current.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty
      };
      field.RegisterValueChangedCallback(evt =>
      {
        if (string.IsNullOrWhiteSpace(evt.newValue))
        {
          setter(null);
          return;
        }

        if (float.TryParse(evt.newValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
          setter(parsed);
      });
      _inlineEditorContainer.Add(field);
    }

    private void AddTimeValueField(string label, ScenarioTimeValue current, System.Action<ScenarioTimeValue> setter)
    {
      var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
      var valueField = new DoubleField(label) { value = current.Value };
      valueField.style.flexGrow = 1f;
      var unitField = new EnumField(current.Unit);
      valueField.RegisterValueChangedCallback(evt => setter(new ScenarioTimeValue(System.Math.Max(0d, evt.newValue), (ScenarioTimeUnit)unitField.value)));
      unitField.RegisterValueChangedCallback(evt => setter(new ScenarioTimeValue(System.Math.Max(0d, valueField.value), (ScenarioTimeUnit)evt.newValue)));
      row.Add(valueField);
      row.Add(unitField);
      _inlineEditorContainer.Add(row);
    }

    private ScenarioChoiceOption EnsureChoiceOptionEditable(ScenarioChoiceNode data, int index)
    {
      data.Options ??= new List<ScenarioChoiceOption>();
      while (data.Options.Count <= index)
      {
        var option = new ScenarioChoiceOption { DisplayText = $"옵션 {data.Options.Count + 1}" };
        data.Options.Add(option);
        AddChoiceOptionPort(option);
      }

      var editable = data.Options[index] ??= new ScenarioChoiceOption { DisplayText = $"옵션 {index + 1}" };
      if (choicePorts.TryGetValue(editable, out _) == false)
        AddChoiceOptionPort(editable);
      return editable;
    }

    private static string GetChoiceOptionText(ScenarioChoiceNode data, int index, string fallback)
    {
      if (data?.Options == null || index < 0 || index >= data.Options.Count || data.Options[index] == null)
        return fallback;
      return data.Options[index].DisplayText ?? string.Empty;
    }

    private void RefreshSummaryLabel()
    {
      if (_summaryLabel == null)
      {
        return;
      }

      _summaryLabel.text = BuildNodeSummary(Data, _runtimeVisitOrders);
    }

    private void RefreshVisitHistory()
    {
      if (_visitHistoryFoldout == null || _visitHistoryLabel == null)
      {
        return;
      }

      if (_runtimeVisitOrders.Count == 0)
      {
        _visitHistoryFoldout.style.display = DisplayStyle.None;
        _visitHistoryLabel.text = string.Empty;
        return;
      }

      _visitHistoryFoldout.style.display = DisplayStyle.Flex;
      _visitHistoryFoldout.text = _runtimeVisitOrders.Count > VisitPreviewLimit
        ? $"Visit History ({_runtimeVisitOrders.Count})"
        : "Visit History";
      _visitHistoryFoldout.value = _runtimeVisitOrders.Count <= VisitPreviewLimit;
      _visitHistoryLabel.text = string.Join(", ", _runtimeVisitOrders.Select(FormatVisitOrder));
    }

    private static string BuildNodeSummary(IScenarioNode node, IReadOnlyList<int> visitOrders)
    {
      var summaryParts = new List<string>();

      if (node is not ScenarioValidatorNode validator)
      {
        if (node is ScenarioPlayerTagNode playerTag)
        {
          summaryParts.Add($"Tag: {playerTag.Tag ?? string.Empty}\nNext: {playerTag.NextIdentifier ?? "(미연결)"}");
        }
        else if (node is ScenarioServerInternalSignalNode internalSignal)
        {
          summaryParts.Add($"Target: {internalSignal.TargetIdentifier ?? "@m"}\nSignal: {internalSignal.SignalIdentifier ?? string.Empty}\nOperation: {internalSignal.Operation}\nNext: {internalSignal.NextIdentifier ?? "(미연결)"}");
        }

        AddVisitSummary(summaryParts, visitOrders);
        return string.Join("\n", summaryParts.Where(each => !string.IsNullOrWhiteSpace(each)));
      }

      var rootConditions = validator.RootConditions?
          .Where(each => each != null)
          .ToList();

      if (rootConditions == null || rootConditions.Count == 0)
      {
        summaryParts.Add("Root conditions: (empty)");
        AddVisitSummary(summaryParts, visitOrders);
        return string.Join("\n", summaryParts.Where(each => !string.IsNullOrWhiteSpace(each)));
      }

      summaryParts.AddRange(rootConditions.Select((rootCondition, index) =>
          BuildRootConditionSummary(index, rootCondition)));
      AddVisitSummary(summaryParts, visitOrders);
      return string.Join("\n", summaryParts.Where(each => !string.IsNullOrWhiteSpace(each)));
    }

    private static void AddVisitSummary(List<string> summaryParts, IReadOnlyList<int> visitOrders)
    {
      var visitSummary = BuildVisitSummary(visitOrders);
      if (!string.IsNullOrWhiteSpace(visitSummary))
      {
        summaryParts.Add($"Visits: {visitSummary}");
      }
    }

    private static string BuildVisitSummary(IReadOnlyList<int> visitOrders)
    {
      if (visitOrders == null || visitOrders.Count == 0)
      {
        return string.Empty;
      }

      var preview = visitOrders
          .Take(VisitPreviewLimit)
          .Select(FormatVisitOrder);

      var summary = string.Join(", ", preview);
      if (visitOrders.Count > VisitPreviewLimit)
      {
        summary += ", ...";
      }

      return summary;
    }

    private static string FormatVisitOrder(int visitOrder)
    {
      return $"#{visitOrder}";
    }

    private void ConfigureTextField(TextField field, bool multiline)
    {
      field.style.flexGrow = 1f;
      field.style.minWidth = 0;
      field.style.maxWidth = Length.Percent(100);
      field.labelElement.style.whiteSpace = WhiteSpace.Normal;
      field.labelElement.style.minWidth = 0;

      if (multiline)
      {
        field.style.whiteSpace = WhiteSpace.Normal;
      }
    }

    private static string BuildRootConditionSummary(int index, ScenarioValidatorRootCondition rootCondition)
    {
      if (rootCondition == null)
      {
        return $"{index + 1}. (null)";
      }

      switch (rootCondition.Condition)
      {
        case ScenarioValidatorCondition.RegistryContains:
        {
          var rules = rootCondition.ValidationRules?
              .Where(each => each != null)
              .ToList();

          if (rules == null || rules.Count == 0)
          {
            return $"{index + 1}. RegistryContains (rules: empty)";
          }

          var compactRules = string.Join(", ", rules.Select(each =>
              $"{each.Type}/{each.Condition}/{each.RegistryType}:{each.RegistryIdentifier}"));
          return $"{index + 1}. RegistryContains [{rootCondition.MatchMode}: {compactRules}]";
        }
        case ScenarioValidatorCondition.PlayerAssignedTag:
          return $"{index + 1}. PlayerAssignedTag {rootCondition.PlayerScope}/{rootCondition.PlayerTag}";
        case ScenarioValidatorCondition.PlayerCountEqual:
        case ScenarioValidatorCondition.PlayerCountNotEqual:
        case ScenarioValidatorCondition.PlayerCountLessThan:
        case ScenarioValidatorCondition.PlayerCountLessThanOrEqual:
        case ScenarioValidatorCondition.PlayerCountGreaterThan:
        case ScenarioValidatorCondition.PlayerCountGreaterThanOrEqual:
          return $"{index + 1}. {rootCondition.Condition} {rootCondition.TargetCount}";
        default:
          return $"{index + 1}. {rootCondition.Condition}";
      }
    }

    private Port CreateStandardOutput(string name)
    {
      var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
      port.userData = "next";
      port.portName = name;
      outputContainer.Add(port);
      return port;
    }

    private void AddChoiceOptionPort(ScenarioChoiceOption option)
    {
      var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
      port.portName = option.DisplayText;
      port.userData = option;
      outputContainer.Add(port);
      choicePorts[option] = port;
    }

    private void AddParallelBranchPort(ScenarioParallelBranch branch)
    {
      var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
      port.portName = branch.Identifier ?? "Branch";
      port.userData = branch;
      outputContainer.Add(port);
      branchPorts[branch] = port;
    }

    private void AddQuizOutput(string name, string marker)
    {
      var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
      port.portName = name;
      port.userData = marker;
      outputContainer.Add(port);
    }

    public void HandlePortConnection(Port port, ScenarioNodeView target)
    {
      switch (port.userData)
      {
        case "next":
          Data.NextIdentifier = target.Data.Identifier;
          break;
        case "quiz.correct":
          if (Data is ScenarioQuizNode quizCorrect)
          {
            quizCorrect.OnCorrectNextIdentifier = target.Data.Identifier;
          }
          break;
        case "quiz.incorrect":
          if (Data is ScenarioQuizNode quizIncorrect)
          {
            quizIncorrect.OnIncorrectNextIdentifier = target.Data.Identifier;
          }
          break;
        case ScenarioChoiceOption option:
          option.NextNodeIdentifier = target.Data.Identifier;
          break;
        case ScenarioParallelBranch branch:
          branch.Identifier = target.Data.Identifier;
          break;
      }
    }

    public void HandlePortDisconnection(Port port)
    {
      switch (port.userData)
      {
        case "next":
          Data.NextIdentifier = null;
          break;
        case "quiz.correct":
          if (Data is ScenarioQuizNode quizCorrect)
          {
            quizCorrect.OnCorrectNextIdentifier = null;
          }
          break;
        case "quiz.incorrect":
          if (Data is ScenarioQuizNode quizIncorrect)
          {
            quizIncorrect.OnIncorrectNextIdentifier = null;
          }
          break;
        case ScenarioChoiceOption option:
          option.NextNodeIdentifier = null;
          break;
        case ScenarioParallelBranch branch:
          // 브랜치 identifier 는 스키마 필수(null 불허) 필드라서 null 로 비우면
          // 저장 검증이 실패한다. 대신 placeholder 로 교체해 언제든 저장 가능하고
          // 다시 연결할 수 있는 상태를 유지한다(재연결 시 HandlePortConnection 에서
          // 실제 식별자가 다시 설정된다).
          branch.Identifier = GetDisconnectedBranchPlaceholder(branch);
          UpdateBranchPortLabel(branch);
          break;
      }
    }

    public string GetOutputRouteSlot(Port port)
    {
      switch (port?.userData)
      {
        case "next": return "next";
        case "quiz.correct": return "quiz.correct";
        case "quiz.incorrect": return "quiz.incorrect";
        case ScenarioChoiceOption option:
          return $"choice.{((ScenarioChoiceNode)Data).Options.ToList().IndexOf(option)}";
        case ScenarioParallelBranch branch:
          return $"parallel.{((ScenarioParallelNode)Data).Branches.ToList().IndexOf(branch)}";
        default:
          return "output";
      }
    }

    public Port GetPortForOption(ScenarioChoiceOption option)
    {
      choicePorts.TryGetValue(option, out var port);
      return port;
    }

    public Port GetPortForBranch(ScenarioParallelBranch branch)
    {
      branchPorts.TryGetValue(branch, out var port);
      return port;
    }

    /// <summary>
    /// 연결이 해제된 병렬 브랜치에 채울 placeholder 식별자를 만든다.
    /// 스키마는 브랜치 identifier 를 필수(null 불허)로 요구하므로 null 대신
    /// placeholder 로 저장 가능성을 지키고, 포트는 유지되어 재연결할 수 있다.
    /// CreateOutputPorts 의 자동 추가 브랜치와 같은 branch_N 명명 규칙을 쓴다.
    /// </summary>
    private string GetDisconnectedBranchPlaceholder(ScenarioParallelBranch branch)
    {
      var index = mutableBranches?.IndexOf(branch) ?? -1;
      return index >= 0 ? $"branch_{index + 1}" : "branch_disconnected";
    }

    public void UpdateOptionPortLabel(ScenarioChoiceOption option)
    {
      var port = GetPortForOption(option);
      if (port != null)
      {
        port.portName = option.DisplayText;
      }
    }

    public void UpdateBranchPortLabel(ScenarioParallelBranch branch)
    {
      var port = GetPortForBranch(branch);
      if (port != null)
      {
        port.portName = branch.Identifier;
      }
    }
  }
}
