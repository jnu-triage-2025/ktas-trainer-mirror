using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>태그별 체크리스트 아이템 묶음을 직접 편집하는 ScenarioGraph 탭입니다.</summary>
  internal sealed class ScenarioChecklistItemSetsEditorView : VisualElement
  {
    private readonly Func<ScenarioGraph> getGraph;
    private readonly Action onChanged;
    private readonly VisualElement list;

    public ScenarioChecklistItemSetsEditorView(Func<ScenarioGraph> getGraph, Action onChanged)
    {
      this.getGraph = getGraph;
      this.onChanged = onChanged;
      style.flexGrow = 1f;

      var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
      header.Add(new Label("Checklist Items") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, fontSize = 14, flexGrow = 1f } });
      header.Add(new Button(AddTagSet) { text = "Add Tag Set" });
      Add(header);
      Add(new HelpBox(
        "플레이어 태그마다 체크리스트에 표시할 아이템과 수량을 설정합니다. 한 플레이어가 여러 태그를 가지면 해당 목록을 모두 합쳐 표시합니다.",
        HelpBoxMessageType.Info));
      list = new VisualElement();
      Add(list);
      Refresh();
    }

    public void Refresh()
    {
      list.Clear();
      var graph = getGraph?.Invoke();
      if (graph?.ChecklistItemSetsByPlayerTag == null || graph.ChecklistItemSetsByPlayerTag.Count == 0)
      {
        list.Add(new HelpBox("등록된 체크리스트 항목이 없습니다.", HelpBoxMessageType.None));
        return;
      }

      foreach (var pair in graph.ChecklistItemSetsByPlayerTag.OrderBy(value => value.Key, StringComparer.Ordinal))
        list.Add(BuildTagSet(pair.Key, pair.Value));
    }

    private VisualElement BuildTagSet(string tag, IReadOnlyList<ScenarioChecklistItemRequirement> requirements)
    {
      var foldout = new Foldout { text = tag, value = true };
      foldout.style.marginTop = 8;
      var tagField = new TextField("Player Tag") { value = tag };
      tagField.RegisterValueChangedCallback(evt => RenameTagSet(tag, evt.newValue));
      foldout.Add(tagField);

      var values = requirements?.Where(value => value != null).ToList() ?? new List<ScenarioChecklistItemRequirement>();
      for (var index = 0; index < values.Count; index++)
        foldout.Add(BuildRequirement(tag, values[index]));

      foldout.Add(new Button(() => AddRequirement(tag)) { text = "Add Item" });
      foldout.Add(new Button(() => RemoveTagSet(tag)) { text = "Remove Tag Set" });
      return foldout;
    }

    private VisualElement BuildRequirement(string tag, ScenarioChecklistItemRequirement requirement)
    {
      var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
      var identifier = new TextField { value = requirement.Identifier ?? string.Empty };
      identifier.style.flexGrow = 1f;
      identifier.tooltip = "Item identifier";
      identifier.RegisterValueChangedCallback(evt => { requirement.Identifier = evt.newValue?.Trim(); Changed(); });
      row.Add(identifier);

      var count = new IntegerField { value = Math.Max(1, requirement.Count) };
      count.style.width = 90;
      count.tooltip = "Count";
      count.RegisterValueChangedCallback(evt => { requirement.Count = Math.Max(1, evt.newValue); Changed(); });
      row.Add(count);
      row.Add(new Button(() => RemoveRequirement(tag, requirement)) { text = "−" });
      return row;
    }

    private void AddTagSet()
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;

      var values = Copy(graph.ChecklistItemSetsByPlayerTag);
      var tag = GetUniqueTag(values.Keys);
      values[tag] = new List<ScenarioChecklistItemRequirement>();
      graph.ChecklistItemSetsByPlayerTag = values;
      Changed(true);
    }

    private void RenameTagSet(string previousTag, string proposedTag)
    {
      var graph = getGraph?.Invoke();
      var nextTag = proposedTag?.Trim();
      if (graph == null || string.IsNullOrWhiteSpace(nextTag) || string.Equals(previousTag, nextTag, StringComparison.Ordinal))
        return;

      var values = Copy(graph.ChecklistItemSetsByPlayerTag);
      if (!values.TryGetValue(previousTag, out var requirements) || values.ContainsKey(nextTag))
      {
        Refresh();
        return;
      }

      values.Remove(previousTag);
      values[nextTag] = requirements;
      graph.ChecklistItemSetsByPlayerTag = values;
      Changed(true);
    }

    private void RemoveTagSet(string tag)
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;

      var values = Copy(graph.ChecklistItemSetsByPlayerTag);
      if (!values.Remove(tag))
        return;
      graph.ChecklistItemSetsByPlayerTag = values;
      Changed(true);
    }

    private void AddRequirement(string tag)
    {
      MutateRequirements(tag, values => values.Add(new ScenarioChecklistItemRequirement { Count = 1 }));
    }

    private void RemoveRequirement(string tag, ScenarioChecklistItemRequirement requirement)
    {
      MutateRequirements(tag, values => values.Remove(requirement));
    }

    private void MutateRequirements(string tag, Action<List<ScenarioChecklistItemRequirement>> mutation)
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;

      var values = Copy(graph.ChecklistItemSetsByPlayerTag);
      if (!values.TryGetValue(tag, out var requirements))
        return;
      var mutableRequirements = requirements?.ToList() ?? new List<ScenarioChecklistItemRequirement>();
      mutation(mutableRequirements);
      values[tag] = mutableRequirements;
      graph.ChecklistItemSetsByPlayerTag = values;
      Changed(true);
    }

    private static Dictionary<string, IReadOnlyList<ScenarioChecklistItemRequirement>> Copy(
      IReadOnlyDictionary<string, IReadOnlyList<ScenarioChecklistItemRequirement>> source)
      => source?.ToDictionary(
           pair => pair.Key,
           pair => (IReadOnlyList<ScenarioChecklistItemRequirement>)(pair.Value?.Where(value => value != null).ToList()
             ?? new List<ScenarioChecklistItemRequirement>()),
           StringComparer.Ordinal)
         ?? new Dictionary<string, IReadOnlyList<ScenarioChecklistItemRequirement>>(StringComparer.Ordinal);

    private static string GetUniqueTag(IEnumerable<string> existing)
    {
      var known = new HashSet<string>(existing ?? Array.Empty<string>(), StringComparer.Ordinal);
      for (var number = 1; ; number++)
      {
        var candidate = $"player_tag_{number}";
        if (!known.Contains(candidate))
          return candidate;
      }
    }

    private void Changed(bool refresh = false)
    {
      onChanged?.Invoke();
      if (refresh)
        Refresh();
    }
  }
}
