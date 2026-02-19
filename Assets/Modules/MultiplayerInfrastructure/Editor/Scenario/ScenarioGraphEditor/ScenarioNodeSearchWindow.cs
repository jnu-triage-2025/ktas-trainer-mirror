
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioNodeSearchWindow : ScriptableObject, ISearchWindowProvider
  {
    private ScenarioGraphAuthoringWindow window;
    private ScenarioGraphView graphView;

    public void Initialize(ScenarioGraphAuthoringWindow window, ScenarioGraphView graphView)
    {
      this.window = window;
      this.graphView = graphView;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
      return new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Scenario Nodes"), 0),
            new SearchTreeEntry(new GUIContent("Dialogue")) { level = 1, userData = ScenarioNodeType.Dialogue },
            new SearchTreeEntry(new GUIContent("Choice")) { level = 1, userData = ScenarioNodeType.Choice },
            new SearchTreeEntry(new GUIContent("Sound")) { level = 1, userData = ScenarioNodeType.Sound },
            new SearchTreeEntry(new GUIContent("Player Move")) { level = 1, userData = ScenarioNodeType.PlayerMove },
            new SearchTreeEntry(new GUIContent("NPC Move")) { level = 1, userData = ScenarioNodeType.NPCMove },
            new SearchTreeEntry(new GUIContent("Camera Target")) { level = 1, userData = ScenarioNodeType.CameraTarget },
            new SearchTreeEntry(new GUIContent("Invoke Event")) { level = 1, userData = ScenarioNodeType.InvokeEvent },
            new SearchTreeEntry(new GUIContent("Validator")) { level = 1, userData = ScenarioNodeType.Validator },
            new SearchTreeEntry(new GUIContent("Parallel")) { level = 1, userData = ScenarioNodeType.Parallel },
            new SearchTreeEntry(new GUIContent("Quest Control")) { level = 1, userData = ScenarioNodeType.QuestControl },
            new SearchTreeEntry(new GUIContent("Quest Waypoint Highlight")) { level = 1, userData = ScenarioNodeType.QuestWaypointHighlight },
            new SearchTreeEntry(new GUIContent("Notification")) { level = 1, userData = ScenarioNodeType.Notification },
            new SearchTreeEntry(new GUIContent("Delay")) { level = 1, userData = ScenarioNodeType.Delay },
            new SearchTreeEntry(new GUIContent("Interaction")) { level = 1, userData = ScenarioNodeType.Interaction },
            new SearchTreeEntry(new GUIContent("Combine Item")) { level = 1, userData = ScenarioNodeType.CombineItem },
            new SearchTreeEntry(new GUIContent("Quiz")) { level = 1, userData = ScenarioNodeType.Quiz },
            new SearchTreeEntry(new GUIContent("State Update")) { level = 1, userData = ScenarioNodeType.StateUpdate },
            new SearchTreeEntry(new GUIContent("Role Assignment")) { level = 1, userData = ScenarioNodeType.RoleAssignment }
        };
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
      if (!(entry.userData is ScenarioNodeType type)) return false;
      window.CreateNode(type, context.screenMousePosition);
      return true;
    }
  }
}
