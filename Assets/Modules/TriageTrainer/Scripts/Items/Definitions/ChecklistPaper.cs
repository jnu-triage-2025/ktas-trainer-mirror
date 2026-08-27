using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;

namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  [IntendedMissingItemSpriteAttribute]
  public sealed class ChecklistPaper : Paper
  {
    private const string CompletedTextPrefix = "<s><color=#2E7D32>";
    private const string CompletedTextSuffix = "</color></s>";

    public new const string Identifier = "checklist_paper";
    public new const string DisplayName = "종이";
    public new const string Description = "";

    [NonSerialized] private ScenarioController _observedScenarioController;
    private HashSet<string> _completedIdentifiers = new(StringComparer.Ordinal);

    public override Item Clone()
    {
      var clone = (ChecklistPaper)base.Clone();
      clone._completedIdentifiers = new HashSet<string>(_completedIdentifiers, StringComparer.Ordinal);
      clone._observedScenarioController = null;
      return clone;
    }

    /// <summary>
    /// 의료 아이템의 획득 훅에서 호출됩니다. 인벤토리에 보관 중인 체크리스트만 갱신하므로,
    /// 월드 아이템이나 다른 플레이어의 체크리스트에는 영향을 주지 않습니다.
    /// </summary>
    public static void NotifyItemAcquired(PlayerController player, string itemIdentifier)
    {
      if (player?.InventorySlots == null || string.IsNullOrWhiteSpace(itemIdentifier))
        return;

      foreach (var slot in player.InventorySlots)
      {
        if (slot?.ItemInstance is not ChecklistPaper checklist)
          continue;

        checklist.BindScenarioLifecycle(player);
        checklist.MarkAcquired(itemIdentifier);
      }
    }

    public override string GetCurrentSerializedDerivedAttributes()
      => _completedIdentifiers == null || _completedIdentifiers.Count == 0
        ? string.Empty
        : JsonSerializer.Serialize(new SerializedState
        {
          CompletedIdentifiers = _completedIdentifiers.OrderBy(value => value, StringComparer.Ordinal).ToArray()
        });

    public override void SetCurrentSerializedDerivedAttributes(string serialized)
    {
      _completedIdentifiers ??= new HashSet<string>(StringComparer.Ordinal);
      _completedIdentifiers.Clear();
      if (!string.IsNullOrWhiteSpace(serialized))
      {
        try
        {
          var state = JsonSerializer.Deserialize<SerializedState>(serialized);
          if (state?.CompletedIdentifiers != null)
          {
            foreach (var identifier in state.CompletedIdentifiers)
            {
              if (!string.IsNullOrWhiteSpace(identifier))
                _completedIdentifiers.Add(identifier.Trim());
            }
          }
        }
        catch (JsonException)
        {
          // 잘못된 이전 상태는 빈 체크리스트 상태로 안전하게 복구합니다.
        }
      }

      MarkDerivedAttributesModified();
      RefreshDescription();
    }

    private void BindScenarioLifecycle(PlayerController player)
    {
      if (player != null && !string.IsNullOrWhiteSpace(player.UserIdentifier))
        _ownerPlayerIdentifier = player.UserIdentifier;
      var controller = ScenarioController.Instance;
      if (ReferenceEquals(controller, _observedScenarioController))
      {
        if (controller == null)
        {
          ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
          ScenarioController.InstanceAvailable += HandleScenarioControllerAvailable;
        }
        RefreshDescription();
        return;
      }

      if (_observedScenarioController != null)
      {
        _observedScenarioController.OnScenarioStarted -= HandleScenarioStarted;
        _observedScenarioController.OnScenarioEnded -= HandleScenarioEnded;
        _observedScenarioController.OnNodeChanged -= HandleScenarioNodeChanged;
      }

      _observedScenarioController = controller;
      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
      if (_observedScenarioController != null)
      {
        _observedScenarioController.OnScenarioStarted += HandleScenarioStarted;
        _observedScenarioController.OnScenarioEnded += HandleScenarioEnded;
        _observedScenarioController.OnNodeChanged += HandleScenarioNodeChanged;
      }
      else
      {
        ScenarioController.InstanceAvailable += HandleScenarioControllerAvailable;
      }

      RefreshDescription();
    }

    private void HandleScenarioControllerAvailable(ScenarioController controller)
    {
      if (controller == null)
        return;

      BindScenarioLifecycle(null);
    }

    private void HandleScenarioStarted()
    {
      _completedIdentifiers ??= new HashSet<string>(StringComparer.Ordinal);
      _completedIdentifiers.Clear();
      MarkDerivedAttributesModified();
      RefreshDescription();
    }

    private void HandleScenarioEnded()
    {
      _completedIdentifiers ??= new HashSet<string>(StringComparer.Ordinal);
      _completedIdentifiers.Clear();
      CurrentDescription = string.Empty;
      MarkDerivedAttributesModified();
    }

    private void HandleScenarioNodeChanged(IScenarioNode _)
      => RefreshDescription();

    private void MarkAcquired(string itemIdentifier)
    {
      _completedIdentifiers ??= new HashSet<string>(StringComparer.Ordinal);
      var controller = _observedScenarioController ?? ScenarioController.Instance;
      if (controller == null || !controller.HasActiveScenario || !IsListedItem(controller.CurrentGraph, itemIdentifier))
        return;

      if (_completedIdentifiers.Add(itemIdentifier.Trim()))
        MarkDerivedAttributesModified();

      RefreshDescription();
    }

    private void RefreshDescription()
    {
      var controller = _observedScenarioController ?? ScenarioController.Instance;
      if (controller == null || !controller.HasActiveScenario || controller.CurrentGraph == null)
      {
        CurrentDescription = string.Empty;
        return;
      }

      var requirements = GetRequirementsForPlayer(controller.CurrentGraph, ResolveCurrentPlayerIdentifier());
      CurrentDescription = string.Join("\n", requirements.Select(FormatRequirement));
    }

    private bool IsListedItem(ScenarioGraph graph, string itemIdentifier)
      => GetRequirementsForPlayer(graph, ResolveCurrentPlayerIdentifier())
        .Any(requirement => string.Equals(requirement.Identifier, itemIdentifier?.Trim(), StringComparison.Ordinal));

    private static IReadOnlyList<ScenarioChecklistItemRequirement> GetRequirementsForPlayer(
      ScenarioGraph graph,
      string playerIdentifier)
    {
      if (graph?.ChecklistItemSetsByPlayerTag == null || string.IsNullOrWhiteSpace(playerIdentifier))
        return Array.Empty<ScenarioChecklistItemRequirement>();

      var combined = new Dictionary<string, int>(StringComparer.Ordinal);
      foreach (var pair in graph.ChecklistItemSetsByPlayerTag)
      {
        if (string.IsNullOrWhiteSpace(pair.Key) || !PlayerTagService.HasTag(playerIdentifier, pair.Key))
          continue;

        foreach (var requirement in pair.Value ?? Array.Empty<ScenarioChecklistItemRequirement>())
        {
          string identifier = requirement?.Identifier?.Trim();
          if (string.IsNullOrWhiteSpace(identifier))
            continue;

          combined.TryGetValue(identifier, out int previousCount);
          combined[identifier] = previousCount + Math.Max(1, requirement.Count);
        }
      }

      return combined
        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => new ScenarioChecklistItemRequirement { Identifier = pair.Key, Count = pair.Value })
        .ToArray();
    }

    private string FormatRequirement(ScenarioChecklistItemRequirement requirement)
    {
      string displayName = ResolveDisplayName(requirement.Identifier);
      string row = $"{displayName} × {requirement.Count}";
      return _completedIdentifiers != null && _completedIdentifiers.Contains(requirement.Identifier)
        ? CompletedTextPrefix + row + CompletedTextSuffix
        : row;
    }

    private static string ResolveDisplayName(string identifier)
    {
      var item = Registry.CreateItemInstance(identifier);
      return string.IsNullOrWhiteSpace(item?.CurrentDisplayName) ? identifier : item.CurrentDisplayName;
    }

    private string ResolveCurrentPlayerIdentifier()
    {
      return _ownerPlayerIdentifier;
    }

    [NonSerialized] private string _ownerPlayerIdentifier;

    private sealed class SerializedState
    {
      public string[] CompletedIdentifiers { get; set; }
    }
  }
}
