using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>ScenarioGraph 최상위 waypoints 컬렉션을 편집하는 UI Toolkit 뷰.</summary>
  internal sealed class ScenarioWaypointEditorView : VisualElement
  {
    private readonly Func<ScenarioGraph> getGraph;
    private readonly Action onChanged;
    private readonly VisualElement list;

    public ScenarioWaypointEditorView(Func<ScenarioGraph> getGraph, Action onChanged)
    {
      this.getGraph = getGraph;
      this.onChanged = onChanged;
      style.flexGrow = 1f;

      var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
      header.Add(new Label("Waypoints") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, flexGrow = 1f } });
      header.Add(new Button(AddWaypoint) { text = "Add Waypoint" });
      Add(header);
      Add(new HelpBox(
        "시나리오 시작 전에 WaypointAnchor로 생성·등록됩니다. 이동 및 퀘스트 waypoint ID와 동일한 Identifier를 사용하세요.",
        HelpBoxMessageType.Info));
      list = new VisualElement();
      Add(list);
      Refresh();
    }

    public void Refresh()
    {
      list.Clear();
      var waypoints = getGraph?.Invoke()?.Waypoints;
      if (waypoints == null || waypoints.Count == 0)
      {
        list.Add(new Label("등록된 Waypoint가 없습니다.") { style = { marginTop = 8 } });
        return;
      }

      for (var index = 0; index < waypoints.Count; index++)
        if (waypoints[index] != null)
          list.Add(BuildWaypoint(index, waypoints[index]));
    }

    private VisualElement BuildWaypoint(int index, ScenarioWaypointDefinition value)
    {
      var foldout = new Foldout
      {
        text = string.IsNullOrWhiteSpace(value.Identifier) ? $"Waypoint {index + 1}" : value.Identifier,
        value = true
      };
      foldout.Add(Text("Identifier", value.Identifier, next =>
      {
        value.Identifier = Normalize(next);
        foldout.text = string.IsNullOrWhiteSpace(value.Identifier) ? $"Waypoint {index + 1}" : value.Identifier;
      }));
      foldout.Add(Vector3Field("Position", new Vector3(value.PositionX, value.PositionY, value.PositionZ), next =>
      {
        value.PositionX = next.x;
        value.PositionY = next.y;
        value.PositionZ = next.z;
      }));
      foldout.Add(Vector3Field("Rotation", new Vector3(value.RotationX, value.RotationY, value.RotationZ), next =>
      {
        value.RotationX = next.x;
        value.RotationY = next.y;
        value.RotationZ = next.z;
      }));
      var despawn = new Toggle("Despawn On End") { value = value.DespawnOnScenarioEnd };
      despawn.RegisterValueChangedCallback(evt => { value.DespawnOnScenarioEnd = evt.newValue; Changed(); });
      foldout.Add(despawn);
      foldout.Add(new Button(() => RemoveWaypoint(value)) { text = "Remove Waypoint" });
      return foldout;
    }

    private void AddWaypoint()
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;
      var updated = graph.Waypoints?.Where(value => value != null).ToList() ?? new List<ScenarioWaypointDefinition>();
      updated.Add(new ScenarioWaypointDefinition { Identifier = "waypoint", DespawnOnScenarioEnd = true });
      graph.Waypoints = updated;
      Changed(true);
    }

    private void RemoveWaypoint(ScenarioWaypointDefinition value)
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;
      graph.Waypoints = graph.Waypoints.Where(each => !ReferenceEquals(each, value)).ToList();
      Changed(true);
    }

    private TextField Text(string label, string value, Action<string> setter)
    {
      var field = new TextField(label) { value = value ?? string.Empty };
      field.RegisterValueChangedCallback(evt => { setter(evt.newValue); Changed(); });
      return field;
    }

    private Vector3Field Vector3Field(string label, Vector3 value, Action<Vector3> setter)
    {
      var field = new Vector3Field(label) { value = value };
      field.RegisterValueChangedCallback(evt => { setter(evt.newValue); Changed(); });
      return field;
    }

    private void Changed(bool refresh = false)
    {
      onChanged?.Invoke();
      if (refresh)
        Refresh();
    }

    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
