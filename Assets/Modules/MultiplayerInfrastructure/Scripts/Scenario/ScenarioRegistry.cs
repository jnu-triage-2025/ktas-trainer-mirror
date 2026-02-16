using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
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
    private readonly HashSet<string> _registeredIds = new(StringComparer.OrdinalIgnoreCase);

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

    void OnDestroy()
    {
      UnregisterFromGlobalRegistry();
    }

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

      if (!TryGetEntry(identifier, out ScenarioRegistryEntry entry, out error))
        return false;

      string key = entry.Identifier?.Trim();
      if (!Registry.Registry.PreloadScenarioGraph(key, validateWithSchema: true))
      {
        error = $"Scenario '{identifier}' failed to load from registered text resource.";
        return false;
      }

      graph = Registry.Registry.Get<ScenarioGraph>(RegistryType.ScenarioGraph, key);
      if (graph == null)
      {
        error = $"Scenario '{identifier}' failed to load from registered text resource.";
        return false;
      }

      return true;
    }

    public bool PreloadScenario(string identifier, out string error)
    {
      error = string.Empty;

      if (!TryGetEntry(identifier, out ScenarioRegistryEntry entry, out error))
        return false;

      string key = entry.Identifier?.Trim();
      if (Registry.Registry.PreloadScenarioGraph(key, validateWithSchema: true))
        return true;

      error = $"Scenario '{identifier}' preload failed.";
      return false;
    }

    public void PreloadAllScenarios()
    {
      foreach (var key in _registeredIds)
      {
        Registry.Registry.PreloadScenarioGraph(key, validateWithSchema: true);
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
      UnregisterFromGlobalRegistry();
      _byId.Clear();

      foreach (var scenario in _scenarios)
      {
        if (scenario == null)
          continue;

        string key = ResolveScenarioIdentifier(scenario);
        if (string.IsNullOrWhiteSpace(key))
          continue;

        scenario.Identifier = key;
        _byId[key] = scenario;
        if (scenario.ScenarioJson != null)
        {
          Registry.Registry.Register(RegistryType.ScenarioGraph, key, scenario.ScenarioJson);
          _registeredIds.Add(key);
        }
      }
    }

    private string ResolveScenarioIdentifier(ScenarioRegistryEntry entry)
    {
      if (entry.ScenarioJson == null || string.IsNullOrWhiteSpace(entry.ScenarioJson.text))
      {
        string fallbackIdentifier = entry.Identifier?.Trim();
        return fallbackIdentifier ?? string.Empty;
      }

      try
      {
        var graph = ScenarioGraphLoader.LoadFromJson(entry.ScenarioJson.text, validateWithSchema: false);
        if (!string.IsNullOrWhiteSpace(graph.Identifier))
          return graph.Identifier.Trim();
      }
      catch
      {
      }

      string explicitIdentifier = entry.Identifier?.Trim();
      if (!string.IsNullOrWhiteSpace(explicitIdentifier))
        return explicitIdentifier;

      return string.Empty;
    }

    private void UnregisterFromGlobalRegistry()
    {
      if (_registeredIds.Count == 0)
        return;

      foreach (var id in _registeredIds)
      {
        Registry.Registry.Unregister(RegistryType.ScenarioGraph, id);
      }

      _registeredIds.Clear();
    }
  }
}
