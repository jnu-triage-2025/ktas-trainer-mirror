using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;

using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>ScenarioGraph 최상위 actingNpcs 컬렉션을 편집하는 UI Toolkit 뷰.</summary>
  internal sealed class ScenarioActingNpcEditorView : VisualElement
  {
    private readonly Func<ScenarioGraph> getGraph;
    private readonly Action onChanged;
    private readonly VisualElement list;

    public ScenarioActingNpcEditorView(Func<ScenarioGraph> getGraph, Action onChanged)
    {
      this.getGraph = getGraph;
      this.onChanged = onChanged;
      style.flexGrow = 1f;

      var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
      header.Add(new Label("Acting NPCs")
      {
        style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, flexGrow = 1f }
      });
      header.Add(new Button(AddActingNpc) { text = "Add Acting NPC" });
      Add(header);
      Add(new HelpBox(
        "시나리오가 소유하는 NPC 인스턴스와 상호작용을 편집합니다. Preset ID는 EntityPreset Registry의 identifier와 일치해야 합니다.",
        HelpBoxMessageType.Info));

      list = new VisualElement();
      Add(list);
      Refresh();
    }

    public void Refresh()
    {
      list.Clear();
      var graph = getGraph?.Invoke();
      var actingNpcs = graph?.ActingNpcs;
      if (actingNpcs == null || actingNpcs.Count == 0)
      {
        list.Add(new Label("등록된 Acting NPC가 없습니다.") { style = { marginTop = 8 } });
        return;
      }

      for (var index = 0; index < actingNpcs.Count; index++)
      {
        var actingNpc = actingNpcs[index];
        if (actingNpc != null)
          list.Add(BuildActingNpc(index, actingNpc));
      }
    }

    private VisualElement BuildActingNpc(int index, ScenarioActingNpcDefinition value)
    {
      var foldout = new Foldout
      {
        text = string.IsNullOrWhiteSpace(value.Identifier) ? $"Acting NPC {index + 1}" : value.Identifier,
        value = true
      };
      foldout.style.marginTop = 8;
      foldout.style.paddingLeft = 6;
      foldout.style.paddingRight = 6;
      foldout.style.paddingBottom = 6;
      foldout.style.borderBottomWidth = 1;
      foldout.style.borderTopWidth = 1;
      foldout.style.borderLeftWidth = 1;
      foldout.style.borderRightWidth = 1;

      foldout.Add(Text("Identifier", value.Identifier, next =>
      {
        value.Identifier = Normalize(next);
        foldout.text = string.IsNullOrWhiteSpace(value.Identifier) ? $"Acting NPC {index + 1}" : value.Identifier;
      }));
      foldout.Add(Enum("Type", value.ActingNpcType, next => value.ActingNpcType = next));
      foldout.Add(Text("Preset ID", value.PresetIdentifier, next => value.PresetIdentifier = Normalize(next)));
      foldout.Add(Text("Display Name", value.DisplayName, next => value.DisplayName = next));
      foldout.Add(Vector3Field("Position",
        new Vector3(value.PositionX, value.PositionY, value.PositionZ),
        next => { value.PositionX = next.x; value.PositionY = next.y; value.PositionZ = next.z; }));
      foldout.Add(Vector3Field("Rotation",
        new Vector3(value.RotationX, value.RotationY, value.RotationZ),
        next => { value.RotationX = next.x; value.RotationY = next.y; value.RotationZ = next.z; }));
      foldout.Add(Toggle("Spawn On Start", value.SpawnOnStart, next => value.SpawnOnStart = next));
      foldout.Add(Toggle("Despawn On End", value.DespawnOnScenarioEnd,
        next => value.DespawnOnScenarioEnd = next));

      var interactions = new Foldout { text = "Interactions", value = true };
      var interactionValues = value.Interactions?.Where(each => each != null).ToList()
                              ?? new List<ScenarioActingNpcInteractionDefinition>();
      for (var interactionIndex = 0; interactionIndex < interactionValues.Count; interactionIndex++)
        interactions.Add(BuildInteraction(value, interactionIndex, interactionValues[interactionIndex]));
      interactions.Add(new Button(() =>
      {
        var updated = value.Interactions?.Where(each => each != null).ToList()
                      ?? new List<ScenarioActingNpcInteractionDefinition>();
        updated.Add(new ScenarioActingNpcInteractionDefinition
        {
          Identifier = "interaction",
          InteractionType = ScenarioActingNpcInteractionType.Signal,
          DisplayText = "상호작용",
          Enabled = true,
          ConsumeOnce = true
        });
        value.Interactions = updated;
        Changed(true);
      }) { text = "Add Interaction" });
      foldout.Add(interactions);

      foldout.Add(new Button(() => RemoveActingNpc(value)) { text = "Remove Acting NPC" });
      return foldout;
    }

    private VisualElement BuildInteraction(
      ScenarioActingNpcDefinition owner,
      int index,
      ScenarioActingNpcInteractionDefinition value)
    {
      var foldout = new Foldout
      {
        text = string.IsNullOrWhiteSpace(value.Identifier) ? $"Interaction {index + 1}" : value.Identifier,
        value = false
      };
      foldout.style.marginLeft = 10;
      foldout.Add(Text("Identifier", value.Identifier, next =>
      {
        value.Identifier = Normalize(next);
        foldout.text = string.IsNullOrWhiteSpace(value.Identifier) ? $"Interaction {index + 1}" : value.Identifier;
      }));
      foldout.Add(Enum("Type", value.InteractionType, next =>
      {
        value.InteractionType = next;
        Changed(true);
      }));
      foldout.Add(Text("Display Text", value.DisplayText, next => value.DisplayText = next));
      foldout.Add(Text("Icon ID", value.IconIdentifier, next => value.IconIdentifier = Normalize(next)));

      if (value.InteractionType == ScenarioActingNpcInteractionType.StartScenario)
      {
        foldout.Add(Text("Scenario ID", value.ScenarioIdentifier,
          next => value.ScenarioIdentifier = Normalize(next)));
        foldout.Add(Text("Start Node ID", value.ScenarioStartNodeIdentifier,
          next => value.ScenarioStartNodeIdentifier = Normalize(next)));
      }
      else
      {
        foldout.Add(Text("Completion Signal", value.CompletionSignalIdentifier,
          next => value.CompletionSignalIdentifier = Normalize(next)));
      }

      if (value.InteractionType == ScenarioActingNpcInteractionType.ItemSubmission)
      {
        foldout.Add(Text("Title", value.Title, next => value.Title = next));
        foldout.Add(Text("Submit Button", value.SubmitButtonText, next => value.SubmitButtonText = next));
        foldout.Add(Toggle("Consume Once", value.ConsumeOnce, next => value.ConsumeOnce = next));

        var requiredItems = new Foldout { text = "Required Items", value = true };
        var items = value.RequiredItems?.Where(each => each != null).ToList()
                    ?? new List<ScenarioActingNpcItemRequirement>();
        foreach (var item in items)
        {
          var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
          var itemId = Text("Item ID", item.ItemIdentifier, next => item.ItemIdentifier = Normalize(next));
          itemId.style.flexGrow = 1f;
          row.Add(itemId);
          var count = new IntegerField("Count") { value = item.Count };
          count.style.width = 140;
          count.RegisterValueChangedCallback(evt => { item.Count = Math.Max(1, evt.newValue); Changed(); });
          row.Add(count);
          row.Add(new Button(() =>
          {
            value.RequiredItems = value.RequiredItems.Where(each => !ReferenceEquals(each, item)).ToList();
            Changed(true);
          }) { text = "−" });
          requiredItems.Add(row);
        }
        requiredItems.Add(new Button(() =>
        {
          var updated = value.RequiredItems?.Where(each => each != null).ToList()
                        ?? new List<ScenarioActingNpcItemRequirement>();
          updated.Add(new ScenarioActingNpcItemRequirement { Count = 1 });
          value.RequiredItems = updated;
          Changed(true);
        }) { text = "Add Required Item" });
        foldout.Add(requiredItems);
      }

      foldout.Add(Toggle("Enabled", value.Enabled, next => value.Enabled = next));
      foldout.Add(new Button(() =>
      {
        owner.Interactions = owner.Interactions.Where(each => !ReferenceEquals(each, value)).ToList();
        Changed(true);
      }) { text = "Remove Interaction" });
      return foldout;
    }

    private void AddActingNpc()
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;
      var updated = graph.ActingNpcs?.Where(each => each != null).ToList()
                    ?? new List<ScenarioActingNpcDefinition>();
      updated.Add(new ScenarioActingNpcDefinition
      {
        Identifier = "npc",
        ActingNpcType = ScenarioActingNpcType.Npc,
        SpawnOnStart = true,
        DespawnOnScenarioEnd = true
      });
      graph.ActingNpcs = updated;
      Changed(true);
    }

    private void RemoveActingNpc(ScenarioActingNpcDefinition value)
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;
      graph.ActingNpcs = graph.ActingNpcs.Where(each => !ReferenceEquals(each, value)).ToList();
      Changed(true);
    }

    private TextField Text(string label, string value, Action<string> setter)
    {
      var field = new TextField(label) { value = value ?? string.Empty };
      field.RegisterValueChangedCallback(evt => { setter(evt.newValue); Changed(); });
      return field;
    }

    private Toggle Toggle(string label, bool value, Action<bool> setter)
    {
      var field = new Toggle(label) { value = value };
      field.RegisterValueChangedCallback(evt => { setter(evt.newValue); Changed(); });
      return field;
    }

    private EnumField Enum<T>(string label, T value, Action<T> setter) where T : System.Enum
    {
      var field = new EnumField(label, value);
      field.RegisterValueChangedCallback(evt => { setter((T)evt.newValue); Changed(); });
      return field;
    }

    private Vector3Field Vector3Field(string label, Vector3 value, Action<Vector3> setter)
    {
      var field = new UnityEngine.UIElements.Vector3Field(label) { value = value };
      field.RegisterValueChangedCallback(evt => { setter(evt.newValue); Changed(); });
      return field;
    }

    private void Changed(bool refresh = false)
    {
      onChanged?.Invoke();
      if (refresh)
        Refresh();
    }

    private static string Normalize(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
