using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Quest;
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
        case ScenarioNodeType.InvokeEvent:
          DrawInvokeEventFields((ScenarioInvokeEventNode)data);
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
        case ScenarioNodeType.Notification:
          DrawNotificationFields((ScenarioNotificationNode)data);
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
        case ScenarioNodeType.RoleAssignment:
          DrawRoleAssignmentFields((ScenarioRoleAssignmentNode)data);
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

    private void DrawInvokeEventFields(ScenarioInvokeEventNode data)
    {
      data.EventIdentifier = EditorGUILayout.TextField("Event Identifier", data.EventIdentifier);
      data.MoveNextBehavior = (ScenarioInvokeEventMoveNextBehavior)EditorGUILayout.EnumPopup("Move Next", data.MoveNextBehavior);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }

    private void DrawValidatorFields(ScenarioValidatorNode data)
    {
      data.Condition = (ScenarioValidatorCondition)EditorGUILayout.EnumPopup("Condition", data.Condition);
      data.TargetCount = EditorGUILayout.IntField("Target Count", data.TargetCount);
      data.OnFailure = (ScenarioValidatorOnFailure)EditorGUILayout.EnumPopup("On Failure", data.OnFailure);

      if (data.OnFailure == ScenarioValidatorOnFailure.Branching)
      {
        data.FailureNextIdentifier = EditorGUILayout.TextField("Failure Next", data.FailureNextIdentifier);
      }

      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
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

        var currentRoles = branch.RequiredRoleIdentifiers == null || branch.RequiredRoleIdentifiers.Count == 0
          ? string.Empty
          : string.Join(",", branch.RequiredRoleIdentifiers);
        var roleText = EditorGUILayout.TextField("Required Roles(csv)", currentRoles);
        branch.RequiredRoleIdentifiers = roleText
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

    private void DrawNotificationFields(ScenarioNotificationNode data)
    {
      data.Message = EditorGUILayout.TextField("Message", data.Message);
      data.DisplayMode = (ScenarioNotificationDisplayMode)EditorGUILayout.EnumPopup("Display Mode", data.DisplayMode);
      var duration = data.Duration ?? 0f;
      data.Duration = EditorGUILayout.Toggle("Use Duration", data.Duration.HasValue)
          ? EditorGUILayout.FloatField("Duration", duration)
          : null;
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

    private void DrawRoleAssignmentFields(ScenarioRoleAssignmentNode data)
    {
      if (data.RoleOptions == null)
      {
        data.RoleOptions = new System.Collections.Generic.List<string>();
      }

      var options = data.RoleOptions.ToList();
      EditorGUILayout.LabelField("Role Options", EditorStyles.boldLabel);
      for (int i = 0; i < options.Count; i++)
      {
        EditorGUILayout.BeginHorizontal();
        options[i] = EditorGUILayout.TextField($"Role {i + 1}", options[i]);
        if (GUILayout.Button("-", GUILayout.Width(22)))
        {
          options.RemoveAt(i);
          i--;
        }
        EditorGUILayout.EndHorizontal();
      }

      if (GUILayout.Button("Add Role"))
      {
        options.Add(string.Empty);
      }

      data.RoleOptions = options;
      data.AssignmentMode = (ScenarioRoleAssignmentMode)EditorGUILayout.EnumPopup("Assignment Mode", data.AssignmentMode);
      EditorGUILayout.LabelField("Next Node", data.NextIdentifier ?? "(미연결)");
    }
  }
}
