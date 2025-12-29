using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioNodeView : Node
  {
    public IScenarioNode Data { get; }
    public Port InputPort { get; private set; }
    public Port DefaultOutputPort { get; private set; }
    public Vector2 DefaultSize => new Vector2(260f, 160f);
    private Vector2 _editorPosition = new Vector2(0, 0);
    public Vector2 EditorPosition => _editorPosition;

    private readonly ScenarioGraphAuthoringWindow window;
    private readonly ScenarioGraphView graphView;

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

      title = data.Identifier;
      style.left = EditorPosition.x;
      style.top = EditorPosition.y;

      CreateInputPort();
      CreateOutputPorts();
      SetupNodeBody();

      RefreshExpandedState();
      RefreshPorts();
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
        case ScenarioNodeType.Validator:
          DefaultOutputPort = CreateStandardOutput("Next");
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
      mainContainer.Add(label);
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

    public void HandlePortConnection(Port port, ScenarioNodeView target)
    {
      switch (port.userData)
      {
        case "next":
          Data.NextIdentifier = target.Data.Identifier;
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
