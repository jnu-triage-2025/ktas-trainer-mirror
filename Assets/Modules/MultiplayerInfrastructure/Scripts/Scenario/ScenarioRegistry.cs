using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  [Serializable]
  public class ScenarioRegistryEntry
  {
    public string Identifier;
    public TextAsset ScenarioJson;
  }

  /// <summary>
  /// Maps scenario identifiers to TextAssets assigned in the editor and caches parsed graphs.
  /// </summary>
  public class ScenarioRegistry : MonoBehaviour
  {
    [SerializeField] private List<ScenarioRegistryEntry> _scenarios = new();

    private readonly Dictionary<string, ScenarioRegistryEntry> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ScenarioGraph> _graphCache = new(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
      RebuildLookup();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
      RebuildLookup();
    }
#endif

    public bool TryGetScenarioJson(string identifier, out string json, out string error)
    {
      json = null;

      if (!TryGetEntry(identifier, out ScenarioRegistryEntry entry, out error))
        return false;

      if (entry.ScenarioJson == null || string.IsNullOrWhiteSpace(entry.ScenarioJson.text))
      {
        error = $"Scenario '{identifier}' has no json asset assigned.";
        return false;
      }

      json = entry.ScenarioJson.text;
      return true;
    }

    public bool TryGetScenarioGraph(string identifier, out ScenarioGraph graph, out string error)
    {
      graph = null;
      error = string.Empty;

      string key = identifier?.Trim() ?? string.Empty;
      if (_graphCache.TryGetValue(key, out graph))
        return true;

      if (!TryGetScenarioJson(identifier, out string json, out error))
        return false;

      try
      {
        graph = ScenarioGraphLoader.LoadFromJson(json);
        _graphCache[key] = graph;
        return true;
      }
      catch (Exception ex)
      {
        error = $"Scenario '{identifier}' failed to load: {ex.Message}";
        return false;
      }
    }

    private bool TryGetEntry(string identifier, out ScenarioRegistryEntry entry, out string error)
    {
      entry = null;
      error = string.Empty;

      string key = identifier?.Trim();
      if (string.IsNullOrWhiteSpace(key))
      {
        error = "Scenario identifier is required.";
        return false;
      }

      if (_byId.TryGetValue(key, out entry))
        return true;

      error = $"Scenario '{identifier}' is not registered.";
      return false;
    }

    private void RebuildLookup()
    {
      _byId.Clear();

      foreach (var scenario in _scenarios)
      {
        if (scenario == null)
          continue;

        string key = scenario.Identifier?.Trim();
        if (string.IsNullOrWhiteSpace(key))
          continue;

        _byId[key] = scenario;
      }

      _graphCache.Clear();
    }
  }
}
