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
        titleContainer.style.backgroundColor = highlighted ? ExecutionTitleColor : new Color(0.18f, 0.18f, 0.18f, 1f);
      }

      if (mainContainer != null)
      {
        mainContainer.style.backgroundColor = highlighted ? ExecutionFillColor : new Color(0.16f, 0.16f, 0.16f, 1f);
      }
    }

    public override void OnSelected()
    {
      base.OnSelected();
      window.NotifyNodeSelected(this);
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
        case ScenarioNodeType.Sound:
        case ScenarioNodeType.PlayerMove:
        case ScenarioNodeType.NPCMove:
        case ScenarioNodeType.CameraTarget:
        case ScenarioNodeType.InvokeEvent:
        case ScenarioNodeType.ServerInternalSignal:
        case ScenarioNodeType.Validator:
        case ScenarioNodeType.QuestControl:
        case ScenarioNodeType.QuestWaypointHighlight:
        case ScenarioNodeType.Delay:
        case ScenarioNodeType.Interaction:
        case ScenarioNodeType.CombineItem:
        case ScenarioNodeType.StateUpdate:
        case ScenarioNodeType.PlayTTS:
        case ScenarioNodeType.PlayerTag:
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
      AddNextIdentifierField(data);
    }

    private void BuildChoiceInlineEditor(ScenarioChoiceNode data)
    {
      AddTextField("Speaker", value => data.SpeakerName = value, data.SpeakerName);
      AddTextAreaField("Dialogue", value => data.DialogueContent = value, data.DialogueContent);
      AddTextField("Option 1", value =>
      {
        EnsureChoiceOptions(data);
        data.Options[0].DisplayText = value;
        UpdateOptionPortLabel(data.Options[0]);
      }, GetChoiceOptionText(data, 0));
      AddTextField("Option 2", value =>
      {
        EnsureChoiceOptions(data);
        data.Options[1].DisplayText = value;
        UpdateOptionPortLabel(data.Options[1]);
      }, GetChoiceOptionText(data, 1));
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

    private static void EnsureChoiceOptions(ScenarioChoiceNode data)
    {
      data.Options ??= new List<ScenarioChoiceOption>();
      while (data.Options.Count < 2)
        data.Options.Add(new ScenarioChoiceOption());
    }

    private static string GetChoiceOptionText(ScenarioChoiceNode data, int index)
    {
      EnsureChoiceOptions(data);
      return data.Options[index]?.DisplayText ?? string.Empty;
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
          return $"{index + 1}. RegistryContains [{compactRules}]";
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
          branch.Identifier = null;
          break;
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
