using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Datapack
{
  public class DatapackRuntimeService : MonoBehaviour
  {
    [SerializeField] private ChatService _chatService;
    [SerializeField] private List<TextAsset> _bootstrapDatapacks = new();

    private readonly Dictionary<string, LoadedDatapack> _loaded = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<InjectedEventHandler>> _eventStacks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ScenarioEventIdentifierRegistry.ScenarioEventHandler> _eventBaseHandlers = new(StringComparer.Ordinal);
    private readonly List<PendingGameRule> _pendingGameRules = new();
    private NetworkManager _networkManager;

    private sealed class PendingGameRule
    {
      public string PackId;
      public string Name;
      public string Value;
    }

    private sealed class LoadedDatapack
    {
      public string PackId;
      public DatapackDefinition Definition;
      public readonly List<string> RegisteredAliases = new();
      public readonly List<Coroutine> RunningCoroutines = new();
      public readonly List<InjectedEventHandler> InjectedHandlers = new();
    }

    private sealed class InjectedEventHandler
    {
      public string EventIdentifier;
      public ScenarioEventIdentifierRegistry.ScenarioEventHandler RegisteredHandler;
    }

    private void Awake()
    {
      _networkManager = FindAnyObjectByType<NetworkManager>();
      if (_chatService == null)
        _chatService = GetComponent<ChatService>();
      if (_chatService == null)
        _chatService = FindFirstObjectByType<ChatService>();
    }

    private void OnEnable()
    {
      if (_networkManager == null)
        _networkManager = FindAnyObjectByType<NetworkManager>();
      if (_networkManager != null)
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
    }

    private void OnDisable()
    {
      if (_networkManager != null)
        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    }

    private void Start()
    {
      PrepareRuntimeDatapackFolder();

      // The host's selection is the authoritative session configuration.
      // On a standalone development scene, an empty selection means all valid packs.
      var selected = Registry.Registry.Get<List<string>>(RegistryType.RuntimeState, RegistryGlobalKeys.SelectedDatapackIds)
        ?? new List<string>(SessionConfigurationService.DatapackIds);
      if (selected != null && selected.Count > 0)
      {
        var files = ScanDatapacks();
        for (int i = selected.Count - 1; i >= 0; i--)
        {
          var file = files.FirstOrDefault(x => x.IsValid && x.PackId == selected[i]);
          if (file != null)
            RegisterDatapackFromJson(file.Json, file.FileName, out _);
        }
      }

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
        PackId = definition.packId,
        Definition = definition
      };

      // Register low-priority definitions first. The last registration wins for aliases.
      if (definition.commandAliases != null)
        foreach (var alias in definition.commandAliases)
          if (alias != null && !string.IsNullOrWhiteSpace(alias.name) && !string.IsNullOrWhiteSpace(alias.target))
          {
            _chatService?.CommandService?.RegisterAlias(alias.name, alias.target, definition.packId);
            loaded.RegisteredAliases.Add(alias.name);
          }

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
        var registeredEventIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < definition.eventHandlers.Length; i++)
        {
          var entry = definition.eventHandlers[i];
          if (entry == null || string.IsNullOrWhiteSpace(entry.eventIdentifier) || string.IsNullOrWhiteSpace(entry.command))
            continue;
          if (!registeredEventIdentifiers.Add(entry.eventIdentifier))
          {
            Debug.LogWarning($"[DatapackRuntimeService] Duplicate event handler '{entry.eventIdentifier}' in datapack '{definition.packId}'. Later entry ignored.");
            continue;
          }

          ScenarioEventIdentifierRegistry.ScenarioEventHandler baseHandler;
          if (!_eventStacks.ContainsKey(entry.eventIdentifier))
            ScenarioEventIdentifierRegistry.TryGetHandler(entry.eventIdentifier, out baseHandler);
          else
            baseHandler = _eventBaseHandlers[entry.eventIdentifier];
          if (!_eventStacks.ContainsKey(entry.eventIdentifier))
            _eventBaseHandlers[entry.eventIdentifier] = baseHandler;

          ScenarioEventIdentifierRegistry.ScenarioEventHandler injected = () => ExecuteInjectedEventCommandRoutine(definition.packId, entry.eventIdentifier, entry.command);

          ScenarioEventIdentifierRegistry.Register(entry.eventIdentifier, injected);
          var registration = new InjectedEventHandler
          {
            EventIdentifier = entry.eventIdentifier,
            RegisteredHandler = injected
          };
          loaded.InjectedHandlers.Add(registration);
          if (!_eventStacks.TryGetValue(entry.eventIdentifier, out var stack))
            _eventStacks[entry.eventIdentifier] = stack = new List<InjectedEventHandler>();
          stack.Add(registration);
        }
      }

      _loaded[definition.packId] = loaded;
      if (definition.gameRules != null)
        foreach (var rule in definition.gameRules)
          if (rule != null && !string.IsNullOrWhiteSpace(rule.name) && rule.value != null)
          {
            var pending = new PendingGameRule { PackId = definition.packId, Name = rule.name, Value = rule.value };
            if (InstanceFinder.IsServerStarted || InstanceFinder.IsOffline)
              ExecuteGameRule(pending);
            else
              _pendingGameRules.Add(pending);
          }
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

      for (int i = 0; i < loaded.RegisteredAliases.Count; i++)
        _chatService?.CommandService?.UnregisterAlias(loaded.RegisteredAliases[i], loaded.PackId);

      for (int i = 0; i < loaded.InjectedHandlers.Count; i++)
      {
        var injected = loaded.InjectedHandlers[i];
        if (injected == null || string.IsNullOrWhiteSpace(injected.EventIdentifier))
          continue;

        if (_eventStacks.TryGetValue(injected.EventIdentifier, out var stack))
        {
          stack.Remove(injected);
          if (stack.Count == 0)
            _eventStacks.Remove(injected.EventIdentifier);
        }

        if (!ScenarioEventIdentifierRegistry.Unregister(injected.EventIdentifier, injected.RegisteredHandler))
          continue;
        var replacement = stack != null && stack.Count > 0
          ? stack[stack.Count - 1].RegisteredHandler
          : (_eventBaseHandlers.TryGetValue(injected.EventIdentifier, out var baseHandler) ? baseHandler : null);
        if (replacement != null)
          ScenarioEventIdentifierRegistry.Register(injected.EventIdentifier, replacement);
        if (stack == null || stack.Count == 0)
          _eventBaseHandlers.Remove(injected.EventIdentifier);
      }

      _pendingGameRules.RemoveAll(x => string.Equals(x.PackId, packId, StringComparison.Ordinal));
      _loaded.Remove(packId);
      RestoreDebugCprEscapeRuleAfterUnload(loaded);
      Debug.Log($"[DatapackRuntimeService] Unregistered datapack '{packId}'.");
      return true;
    }

    private void RestoreDebugCprEscapeRuleAfterUnload(LoadedDatapack unloaded)
    {
      const string ruleName = "DEBUG_INT_CPR_PLAYING_ESCAPE_KEY";
      if (unloaded?.Definition?.gameRules == null
          || !unloaded.Definition.gameRules.Any(x => x != null
            && string.Equals(x.name, ruleName, StringComparison.OrdinalIgnoreCase)))
        return;

      string value = "false";
      foreach (var remaining in _loaded.Values)
      {
        var replacement = remaining?.Definition?.gameRules?.LastOrDefault(x => x != null
          && string.Equals(x.name, ruleName, StringComparison.OrdinalIgnoreCase));
        if (replacement?.value != null)
          value = replacement.value;
      }

      if (!bool.TryParse(value, out bool enabled))
        enabled = false;

      if (_chatService == null || !_chatService.TrySetDebugIntCprPlayingEscapeKeyServer(enabled))
        ScenarioGameRules.DEBUG_INT_CPR_PLAYING_ESCAPE_KEY = enabled;
    }

    public IReadOnlyCollection<string> GetLoadedDatapackIds()
    {
      return _loaded.Keys;
    }

    public static List<DatapackFileInfo> ScanDatapacks()
    {
      var result = new List<DatapackFileInfo>();
      string root = GameLogService.DatapackRootPath;
      if (!Directory.Exists(root))
        return result;

      var packIds = new HashSet<string>(StringComparer.Ordinal);
      string[] paths;
      try
      {
        paths = Directory.GetFiles(root, "*.datapack.json", SearchOption.AllDirectories)
          .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
          .ToArray();
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DatapackRuntimeService] Failed to scan '{root}': {ex.Message}");
        return result;
      }

      foreach (var path in paths)
      {
        string json = string.Empty;
        string error = string.Empty;
        try
        { json = File.ReadAllText(path); }
        catch (Exception ex) { error = ex.Message; }
        DatapackDefinition definition = null;
        if (string.IsNullOrWhiteSpace(error))
        {
          try
          { definition = JsonUtility.FromJson<DatapackDefinition>(json); }
          catch (Exception ex) { error = ex.Message; }
          if (definition == null || string.IsNullOrWhiteSpace(definition.packId))
            error = "packId is required.";
          else if (!packIds.Add(definition.packId))
            error = $"Duplicate packId '{definition.packId}'.";
        }
        result.Add(new DatapackFileInfo(Path.GetFileName(path), path, json, definition, error));
      }
      result.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));
      return result;
    }

    public static void EnsureRuntimeDatapackFolder()
    {
      string root = GameLogService.DatapackRootPath;
      try
      { Directory.CreateDirectory(root); }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DatapackRuntimeService] Failed to create datapack folder '{root}': {ex.Message}");
        return;
      }
      string source = Path.Combine(Application.streamingAssetsPath, "DataPacks");
      if (!Directory.Exists(source))
        return;
      string[] sourcePaths;
      try
      { sourcePaths = Directory.GetFiles(source, "*.datapack.json", SearchOption.AllDirectories); }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DatapackRuntimeService] Failed to read built-in datapacks from '{source}': {ex.Message}");
        return;
      }
      foreach (string sourcePath in sourcePaths)
      {
        try
        {
          string relative = sourcePath.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
          string destination = Path.Combine(root, relative);
          Directory.CreateDirectory(Path.GetDirectoryName(destination));
          File.Copy(sourcePath, destination, true);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[DatapackRuntimeService] Failed to copy built-in datapack '{sourcePath}': {ex.Message}");
        }
      }
    }

    private static void PrepareRuntimeDatapackFolder() => EnsureRuntimeDatapackFolder();

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
      if (args.ConnectionState != LocalConnectionState.Started)
        return;
      for (int i = 0; i < _pendingGameRules.Count; i++)
      {
        var pending = _pendingGameRules[i];
        if (_loaded.ContainsKey(pending.PackId))
          ExecuteGameRule(pending);
      }
      _pendingGameRules.Clear();
    }

    private void ExecuteGameRule(PendingGameRule rule)
    {
      ExecuteSystemCommand(rule.PackId, $"gamerule {rule.Name} {rule.Value}", "session-start");
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

      bool succeeded = _chatService.TryExecuteSystemCommand(command, out string result);
      if (!succeeded)
      {
        Debug.LogWarning(
          $"[Datapack:{packId}] {origin} 명령 실행에 실패했습니다: {command} -> "
          + $"{(string.IsNullOrWhiteSpace(result) ? "(사유 없음)" : result)}");
        return;
      }

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

  public sealed class DatapackFileInfo
  {
    public string FileName { get; }
    public string Path { get; }
    public string Json { get; }
    public DatapackDefinition Definition { get; }
    public string Error { get; }
    public bool IsValid => Definition != null && string.IsNullOrWhiteSpace(Error);
    public string PackId => Definition?.packId ?? string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(Definition?.displayName) ? FileName : Definition.displayName;

    public DatapackFileInfo(string fileName, string path, string json, DatapackDefinition definition, string error)
    { FileName = fileName; Path = path; Json = json; Definition = definition; Error = error ?? string.Empty; }
  }
}
