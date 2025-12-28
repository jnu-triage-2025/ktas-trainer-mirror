using MultiplayerInfrastructure.Scenario;
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

      DrawTypeSpecificInspector(targetNode.Data);
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
        case ScenarioNodeType.Parallel:
          DrawParallelFields((ScenarioParallelNode)data);
          break;
      }
    }

    private void DrawDialogueFields(ScenarioDialogueNode data)
    {
      data.SpeakerName = EditorGUILayout.TextField("Speaker", data.SpeakerName);
      EditorGUILayout.PrefixLabel("Dialogue");
      data.DialogueContent = EditorGUILayout.TextArea(data.DialogueContent, GUILayout.Height(60));
      data.PortraitSpriteIdentifier = EditorGUILayout.TextField("Portrait Sprite", data.PortraitSpriteIdentifier);
    }

    private void DrawChoiceFields(ScenarioChoiceNode data)
    {
      data.SpeakerName = EditorGUILayout.TextField("Speaker", data.SpeakerName);
      EditorGUILayout.PrefixLabel("Dialogue");
      data.DialogueContent = EditorGUILayout.TextArea(data.DialogueContent, GUILayout.Height(60));
      data.PortraitSpriteIdentifier = EditorGUILayout.TextField("Portrait Sprite", data.PortraitSpriteIdentifier);

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

    private void DrawParallelFields(ScenarioParallelNode data)
    {
      data.WaitMode = (ScenarioWaitMode)EditorGUILayout.EnumPopup("Wait Mode", data.WaitMode);

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
  }
}
