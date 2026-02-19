using System;
using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Datapack
{
  public class DatapackRuntimeService : MonoBehaviour
  {
    [SerializeField] private ChatService _chatService;
    [SerializeField] private List<TextAsset> _bootstrapDatapacks = new();

    private readonly Dictionary<string, LoadedDatapack> _loaded = new(StringComparer.Ordinal);

    private sealed class LoadedDatapack
    {
      public string PackId;
      public readonly List<Coroutine> RunningCoroutines = new();
      public readonly List<InjectedEventHandler> InjectedHandlers = new();
    }

    private sealed class InjectedEventHandler
    {
      public string EventIdentifier;
      public ScenarioEventIdentifierRegistry.ScenarioEventHandler RegisteredHandler;
      public ScenarioEventIdentifierRegistry.ScenarioEventHandler PreviousHandler;
    }

    private void Awake()
    {
      if (_chatService == null)
        _chatService = GetComponent<ChatService>();
      if (_chatService == null)
        _chatService = FindFirstObjectByType<ChatService>();
    }

    private void Start()
    {
      foreach (var textAsset in _bootstrapDatapacks)
      {
        if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
          continue;

        if (!RegisterDatapackFromJson(textAsset.text, textAsset.name, out var error))
          Debug.LogWarning($"[DatapackRuntimeService] Failed to load bootstrap datapack '{textAsset.name}': {error}");
      }
    }

    private void OnDestroy()
    {
      var keys = new List<string>(_loaded.Keys);
      foreach (var key in keys)
        UnregisterDatapack(key);
    }

    public bool RegisterDatapackFromJson(string json, string sourceName, out string error)
    {
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(json))
      {
        error = "Datapack json is empty.";
        return false;
      }

      DatapackDefinition definition;
      try
      {
        definition = JsonUtility.FromJson<DatapackDefinition>(json);
      }
      catch (Exception ex)
      {
        error = $"Invalid datapack json: {ex.Message}";
        return false;
      }

      if (definition == null)
      {
        error = "Datapack json did not produce a valid object.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(definition.packId))
      {
        error = "Datapack field 'packId' is required.";
        return false;
      }

      if (_loaded.ContainsKey(definition.packId))
      {
        error = $"Datapack '{definition.packId}' is already registered.";
        return false;
      }

      var loaded = new LoadedDatapack
      {
        PackId = definition.packId
      };

      if (definition.periodicCommands != null)
      {
        for (int i = 0; i < definition.periodicCommands.Length; i++)
        {
          var entry = definition.periodicCommands[i];
          if (entry == null || string.IsNullOrWhiteSpace(entry.command))
            continue;

          float interval = Mathf.Max(0.05f, entry.intervalSeconds);
          var routine = StartCoroutine(PeriodicCommandRoutine(definition.packId, entry.command, interval, entry.runImmediately));
          loaded.RunningCoroutines.Add(routine);
        }
      }

      if (definition.eventHandlers != null)
      {
        for (int i = 0; i < definition.eventHandlers.Length; i++)
        {
          var entry = definition.eventHandlers[i];
          if (entry == null || string.IsNullOrWhiteSpace(entry.eventIdentifier) || string.IsNullOrWhiteSpace(entry.command))
            continue;

          ScenarioEventIdentifierRegistry.TryGetHandler(entry.eventIdentifier, out var previousHandler);
          ScenarioEventIdentifierRegistry.ScenarioEventHandler injected = () => ExecuteInjectedEventCommandRoutine(definition.packId, entry.eventIdentifier, entry.command);

          ScenarioEventIdentifierRegistry.Register(entry.eventIdentifier, injected);
          loaded.InjectedHandlers.Add(new InjectedEventHandler
          {
            EventIdentifier = entry.eventIdentifier,
            RegisteredHandler = injected,
            PreviousHandler = previousHandler
          });
        }
      }

      _loaded[definition.packId] = loaded;
      Debug.Log($"[DatapackRuntimeService] Registered datapack '{definition.packId}' from '{sourceName}'.");
      return true;
    }

    public bool UnregisterDatapack(string packId)
    {
      if (string.IsNullOrWhiteSpace(packId))
        return false;

      if (!_loaded.TryGetValue(packId, out var loaded) || loaded == null)
        return false;

      for (int i = 0; i < loaded.RunningCoroutines.Count; i++)
      {
        var routine = loaded.RunningCoroutines[i];
        if (routine != null)
          StopCoroutine(routine);
      }

      for (int i = 0; i < loaded.InjectedHandlers.Count; i++)
      {
        var injected = loaded.InjectedHandlers[i];
        if (injected == null || string.IsNullOrWhiteSpace(injected.EventIdentifier))
          continue;

        ScenarioEventIdentifierRegistry.Unregister(injected.EventIdentifier);
        if (injected.PreviousHandler != null)
          ScenarioEventIdentifierRegistry.Register(injected.EventIdentifier, injected.PreviousHandler);
      }

      _loaded.Remove(packId);
      Debug.Log($"[DatapackRuntimeService] Unregistered datapack '{packId}'.");
      return true;
    }

    public IReadOnlyCollection<string> GetLoadedDatapackIds()
    {
      return _loaded.Keys;
    }

    private IEnumerator PeriodicCommandRoutine(string packId, string rawCommand, float intervalSeconds, bool runImmediately)
    {
      string command = NormalizeCommand(rawCommand);

      if (runImmediately)
        ExecuteSystemCommand(packId, command, $"periodic:{intervalSeconds:0.###}s");

      while (_loaded.ContainsKey(packId))
      {
        yield return new WaitForSeconds(intervalSeconds);

        if (!_loaded.ContainsKey(packId))
          yield break;

        ExecuteSystemCommand(packId, command, $"periodic:{intervalSeconds:0.###}s");
      }
    }

    private IEnumerator ExecuteInjectedEventCommandRoutine(string packId, string eventIdentifier, string rawCommand)
    {
      string command = NormalizeCommand(rawCommand);
      ExecuteSystemCommand(packId, command, $"event:{eventIdentifier}");
      yield break;
    }

    private void ExecuteSystemCommand(string packId, string command, string origin)
    {
      if (string.IsNullOrWhiteSpace(command))
        return;

      if (_chatService == null)
      {
        Debug.LogWarning($"[DatapackRuntimeService] ChatService is missing. Command skipped: {command}");
        return;
      }

      _chatService.TryExecuteSystemCommand(command, out string result);
      if (!string.IsNullOrWhiteSpace(result))
        Debug.Log($"[Datapack:{packId}] {origin} -> {result}");
    }

    private static string NormalizeCommand(string rawCommand)
    {
      if (string.IsNullOrWhiteSpace(rawCommand))
        return string.Empty;

      string trimmed = rawCommand.Trim();
      if (trimmed.StartsWith("/"))
        trimmed = trimmed.Substring(1);

      return trimmed;
    }
  }
}
