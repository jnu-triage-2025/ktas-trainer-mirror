using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Datapack;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Problem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Chat
{
  public class ChatService : NetworkBehaviour
  {
    [Header("ChatSettings")] [SerializeField, Min(0f)]
    private float _messageCooldownSeconds = DefaultsChatControl.MessageCooldownSeconds;

    [Header("References")]
    [SerializeField] private ChatUIController _uiController;
    [SerializeField] private ChatCommandService _commandService;
    [SerializeField] private DatapackRuntimeService _datapackRuntime;
    
    private readonly Dictionary<int, float> _lastMessageTimes = new();
    private readonly Dictionary<int, int> _lastProblemSheetGradeByClientId = new();

    void Awake()
    {
      if (_uiController == null)
        _uiController = GetComponent<ChatUIController>();
      if (_commandService == null)
        _commandService = GetComponent<ChatCommandService>();
      if (_datapackRuntime == null)
        _datapackRuntime = GetComponent<DatapackRuntimeService>();

      if (_datapackRuntime == null)
        _datapackRuntime = gameObject.AddComponent<DatapackRuntimeService>();

      if (_uiController == null || _commandService == null)
      {
        Debug.LogError("ChatService missing required references (UI or CommandService).", this);
        enabled = false;
        return;
      }
      _commandService.Initialize(this);

      _uiController.OnSubmitted += HandleLocalSubmission;
    }
    
    private void HandleLocalSubmission(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return;

      if (raw.StartsWith("/"))
      {
        ExecuteCommandServerRpc(raw[1..]);
        return;
      }

      SendChatServerRpc(raw);
    }

#region Networking

    [ServerRpc(RequireOwnership = false)]
    private void SendChatServerRpc(string rawMessage, NetworkConnection sender = null)
    {
      if (sender == null || string.IsNullOrWhiteSpace(rawMessage))
        return;

      if (!CanSendMessage(sender, out string cooldownMessage))
      {
        SendSystemMessage(sender, cooldownMessage);
        return;
      }

      string displayName = GetDisplayName(sender);
      string senderUuid = string.Empty;
      if (UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor))
        senderUuid = descriptor.Identifier;

      string formatted = $"<{displayName}> {rawMessage}";
      ReceiveChatObserversRpc(formatted);
      MarkMessageSent(sender);

      // 로그 기록 (서버 측)
      GameLogService.WriteChat(
        $"<{displayName}> {rawMessage}",
        string.IsNullOrEmpty(senderUuid) ? displayName : senderUuid);
    }

    [ObserversRpc]
    private void ReceiveChatObserversRpc(string formattedLine)
    {
      Debug.Log($"[ChatService] Received chat message: {formattedLine}");
      _uiController.AppendMessage(formattedLine, showToastWhenHidden: true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExecuteCommandServerRpc(string commandLine, NetworkConnection sender = null)
    {
      if (sender == null)
        return;

      // 커맨드 로그 기록 (서버 측)
      string senderName = GetDisplayName(sender);
      string senderUuid = string.Empty;
      if (UserDescriptorService.TryGetByClientId(sender.ClientId, out var desc))
        senderUuid = desc.Identifier;
      GameLogService.WriteCommand(
        $"/{commandLine} (by {senderName})",
        string.IsNullOrEmpty(senderUuid) ? senderName : senderUuid);

      TryExecuteCommandInternal(commandLine, sender, out _);
    }
    
    [TargetRpc]
    private void TargetReceiveSystemMessage(NetworkConnection conn, string message)
    {
      _uiController.AppendMessage($"<color=#FFD700>[System]</color> {message}", showToastWhenHidden: true);
    }

    [TargetRpc]
    private void TargetRunScenario(NetworkConnection conn, string scenarioIdentifier, int ownerClientId)
    {
      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out string error))
      {
        Debug.LogWarning($"[ChatService] Failed to load scenario '{scenarioIdentifier}': {error}");
        return;
      }

      if (ScenarioController.Instance == null)
      {
        Debug.LogWarning("[ChatService] ScenarioController is missing on this client.");
        return;
      }

      int? owner = ownerClientId >= 0 ? ownerClientId : (int?)null;
      ScenarioController.Instance.StartScenario(graph, null, owner);
    }

    [ObserversRpc(BufferLast = true)]
    private void SyncValidatorBlockLogTargetsObserversRpc(int targets)
    {
      if (ScenarioController.Instance != null)
      {
        ScenarioController.Instance.ValidatorBlockLogTargets = (ScenarioValidatorBlockLogTarget)targets;
      }
    }

    [TargetRpc]
    private void TargetShowTitle(NetworkConnection conn, string title, string subtitle)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ShowTitle(title, subtitle);
    }

    [TargetRpc]
    private void TargetShowSubtitle(NetworkConnection conn, string subtitle)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ShowSubtitle(subtitle);
    }

    [TargetRpc]
    private void TargetShowActionbar(NetworkConnection conn, string actionbar)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ShowActionbar(actionbar);
    }

    [TargetRpc]
    private void TargetClearTitle(NetworkConnection conn)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ClearAll();
    }

    [TargetRpc]
    private void TargetResetTitle(NetworkConnection conn)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ResetTimesAndSubtitle();
    }

    [TargetRpc]
    private void TargetSetTitleTimes(NetworkConnection conn, int fadeInTicks, int stayTicks, int fadeOutTicks)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.SetTimes(fadeInTicks, stayTicks, fadeOutTicks);
    }
    
    [TargetRpc]
    private void TargetRunProblemSheet(NetworkConnection conn, string problemSetIdentifier, int startIndex, bool singleProblemMode)
    {
      var controller = Registry.Registry.Get<ProblemSheetUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey<ProblemSheetUIController>());

      if (controller == null)
      {
        Debug.LogWarning("[ChatService] ProblemSheetUIController is missing on this client.");
        return;
      }

      if (!controller.OpenProblemSet(problemSetIdentifier, startIndex, singleProblemMode))
      {
        Debug.LogWarning($"[ChatService] Failed to open problem set '{problemSetIdentifier}'.");
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportProblemGradeServerRpc(string problemSetIdentifier, int problemIndex, int gradeCode, NetworkConnection sender = null)
    {
      if (sender == null)
        return;

      int normalizedCode = gradeCode == 0 ? 0 : 1;
      _lastProblemSheetGradeByClientId[sender.ClientId] = normalizedCode;

      if (!Registry.Registry.TryGetProblemSet(problemSetIdentifier, out var set, out _))
        return;

      if (set?.Problems == null || problemIndex < 0 || problemIndex >= set.Problems.Count)
        return;

      if (normalizedCode == 0)
        ProblemRewardService.ApplyOnCorrect(set.Problems[problemIndex], sender.ClientId);
    }

#endregion

#region Helpers

    private bool CanSendMessage(NetworkConnection sender, out string message)
    {
      message = string.Empty;

      if (!_lastMessageTimes.TryGetValue(sender.ClientId, out float lastTime))
        return true;

      float elapsed = Time.time - lastTime;
      if (elapsed >= _messageCooldownSeconds)
        return true;

      message = $"You must wait {(_messageCooldownSeconds - elapsed):0.00}s before sending another message.";
      return false;
    }

    private void MarkMessageSent(NetworkConnection sender)
    {
      _lastMessageTimes[sender.ClientId] = Time.time;
    }

    public void SendSystemMessage(NetworkConnection conn, string message)
    {
      if (conn != null)
        TargetReceiveSystemMessage(conn, message);
      else
        Debug.Log($"[System] {message}");
    }

    public bool TryDispatchScenario(string scenarioIdentifier, IEnumerable<NetworkConnection> targets, out string error)
    {
      error = string.Empty;

      if (!IsServer)
      {
        error = "Scenario execution can only be invoked on the server.";
        return false;
      }

      if (!Registry.Registry.PreloadScenarioGraph(scenarioIdentifier, validateWithSchema: true))
      {
        error = $"Scenario '{scenarioIdentifier}' is not registered.";
        return false;
      }

      if (targets == null)
      {
        error = "No target players were matched.";
        return false;
      }

      bool anyTarget = false;
      foreach (var target in targets)
      {
        if (target == null)
          continue;

        anyTarget = true;
        int ownerId = target.ClientId >= 0 ? (int)target.ClientId : -1;
        TargetRunScenario(target, scenarioIdentifier, ownerId);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }

    public bool TrySetValidatorBlockLogTargets(ScenarioValidatorBlockLogTarget targets, out string error)
    {
      error = string.Empty;

      if (!IsServer)
      {
        error = "Validator block logging can only be changed on the server.";
        return false;
      }

      var controller = ScenarioController.Instance;
      if (controller == null)
      {
        error = "ScenarioController instance is not available.";
        return false;
      }

      controller.ValidatorBlockLogTargets = targets;
      SyncValidatorBlockLogTargetsObserversRpc((int)targets);
      return true;
    }

    public bool TryDispatchTitle(
      IEnumerable<NetworkConnection> targets,
      string title,
      string subtitle,
      out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetShowTitle(target, title, subtitle),
        out error);
    }

    public bool TryDispatchSubtitle(IEnumerable<NetworkConnection> targets, string subtitle, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetShowSubtitle(target, subtitle),
        out error);
    }

    public bool TryDispatchActionbar(IEnumerable<NetworkConnection> targets, string actionbar, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetShowActionbar(target, actionbar),
        out error);
    }

    public bool TryDispatchTitleClear(IEnumerable<NetworkConnection> targets, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetClearTitle(target),
        out error);
    }

    public bool TryDispatchTitleReset(IEnumerable<NetworkConnection> targets, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetResetTitle(target),
        out error);
    }

    public bool TryDispatchTitleTimes(IEnumerable<NetworkConnection> targets, int fadeInTicks, int stayTicks, int fadeOutTicks, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetSetTitleTimes(target, fadeInTicks, stayTicks, fadeOutTicks),
        out error);
    }

    public bool TryDispatchProblemSheet(
      string problemSetIdentifier,
      IEnumerable<NetworkConnection> targets,
      int startIndex,
      bool singleProblemMode,
      out string error)
    {
      error = string.Empty;

      if (!IsServer)
      {
        error = "ProblemSheet execution can only be invoked on the server.";
        return false;
      }

      if (!Registry.Registry.PreloadProblemSet(problemSetIdentifier))
      {
        error = $"Problem set '{problemSetIdentifier}' is not registered.";
        return false;
      }

      if (targets == null)
      {
        error = "No target players were matched.";
        return false;
      }

      bool anyTarget = false;
      int safeStartIndex = Mathf.Max(0, startIndex);
      foreach (var target in targets)
      {
        if (target == null)
          continue;

        anyTarget = true;
        _lastProblemSheetGradeByClientId[target.ClientId] = 1;
        TargetRunProblemSheet(target, problemSetIdentifier, safeStartIndex, singleProblemMode);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }

    public void ReportProblemGrade(string problemSetIdentifier, int problemIndex, int gradeCode)
    {
      if (string.IsNullOrWhiteSpace(problemSetIdentifier) || problemIndex < 0)
        return;

      ReportProblemGradeServerRpc(problemSetIdentifier, problemIndex, gradeCode);
    }

    public int GetLastProblemSheetGradeCode(NetworkConnection sender)
    {
      if (sender == null)
        return 1;

      return _lastProblemSheetGradeByClientId.TryGetValue(sender.ClientId, out int code)
        ? (code == 0 ? 0 : 1)
        : 1;
    }

    public bool TryExecuteSystemCommand(string commandLine, out string result)
    {
      // 서버 콘솔 커맨드 로그 기록
      GameLogService.WriteCommand($"/{commandLine} (by server)", "server");
      return TryExecuteCommandInternal(commandLine, null, out result);
    }

    private bool TryExecuteCommandInternal(string commandLine, NetworkConnection sender, out string result)
    {
      if (!TryExecuteCommandLineWithPipeline(commandLine, sender, out var outputValues, out var error, out bool alreadyReported))
      {
        result = string.IsNullOrWhiteSpace(error) ? "Command execution failed." : error;

        // Only surface the error here when the command layer has NOT already
        // shown it to the user. Commands that run with system messages enabled
        // report their own usage/errors, so re-sending would duplicate output.
        if (!alreadyReported)
          SendSystemMessage(sender, result);

        return false;
      }

      result = outputValues.Count > 0
        ? string.Join(", ", outputValues)
        : "Executed command.";
      return true;
    }

    private bool TryExecuteCommandLineWithPipeline(string commandLine, NetworkConnection sender, out List<string> outputValues, out string error, out bool alreadyReported)
    {
      outputValues = new List<string>();
      error = string.Empty;
      alreadyReported = false;

      string trimmed = commandLine?.Trim();
      if (string.IsNullOrWhiteSpace(trimmed))
      {
        error = "Usage: /help";
        return false;
      }

      string[] stages = SplitAndTrim(trimmed, '|', removeEmpty: false);
      if (stages.Length == 0)
      {
        error = "Usage: /help";
        return false;
      }

      bool suppressFirstStageMessages = stages.Length > 1;
      if (!TryExecuteParallelStage(stages[0], sender, suppressFirstStageMessages, out outputValues, out error, out alreadyReported))
        return false;

      for (int i = 1; i < stages.Length; i++)
      {
        if (!TryExecutePipeTargetStage(stages[i], outputValues, sender, out outputValues, out error, out alreadyReported))
          return false;
      }

      return true;
    }

    private bool TryExecuteParallelStage(
      string stage,
      NetworkConnection sender,
      bool suppressSystemMessages,
      out List<string> outputValues,
      out string error,
      out bool alreadyReported)
    {
      outputValues = new List<string>();
      error = string.Empty;
      alreadyReported = false;

      var commands = SplitAndTrim(stage, '&', removeEmpty: true);
      if (commands.Length == 0)
      {
        error = "Invalid command stage.";
        return false;
      }

      for (int i = 0; i < commands.Length; i++)
      {
        if (!TryExecuteSingleCommand(commands[i], sender, suppressSystemMessages, out var values, out error, out alreadyReported))
          return false;

        if (values != null && values.Count > 0)
          outputValues.AddRange(values);
      }

      return true;
    }

    private bool TryExecutePipeTargetStage(
      string stage,
      IReadOnlyList<string> inputValues,
      NetworkConnection sender,
      out List<string> outputValues,
      out string error,
      out bool alreadyReported)
    {
      outputValues = new List<string>();
      error = string.Empty;
      alreadyReported = false;

      if (string.IsNullOrWhiteSpace(stage))
      {
        error = "Invalid pipeline target stage.";
        return false;
      }

      int placeholderCount = CountPlaceholders(stage);
      if (placeholderCount > 0)
      {
        int inputCount = inputValues?.Count ?? 0;
        if (inputCount < placeholderCount)
        {
          error = $"Pipeline requires {placeholderCount} values but received {inputCount}.";
          return false;
        }

        string resolved = stage;
        for (int i = 0; i < placeholderCount; i++)
        {
          resolved = ReplaceFirstPlaceholder(resolved, inputValues[i]);
        }

        stage = resolved;
      }

      if (!TryExecuteParallelStage(stage, sender, suppressSystemMessages: false, out outputValues, out error, out alreadyReported))
        return false;

      return true;
    }

    private static string[] SplitAndTrim(string input, char separator, bool removeEmpty)
    {
      if (string.IsNullOrEmpty(input))
        return Array.Empty<string>();

      var parts = input.Split(separator);
      for (int i = 0; i < parts.Length; i++)
      {
        parts[i] = parts[i].Trim();
      }

      if (!removeEmpty)
        return parts;

      var filtered = new List<string>(parts.Length);
      for (int i = 0; i < parts.Length; i++)
      {
        if (!string.IsNullOrEmpty(parts[i]))
          filtered.Add(parts[i]);
      }

      return filtered.ToArray();
    }

    private bool TryExecuteSingleCommand(
      string commandLine,
      NetworkConnection sender,
      bool suppressSystemMessages,
      out IReadOnlyList<string> pipelineValues,
      out string error,
      out bool alreadyReported)
    {
      pipelineValues = Array.Empty<string>();
      error = string.Empty;
      alreadyReported = false;

      if (string.IsNullOrWhiteSpace(commandLine))
      {
        error = "Usage: /help";
        return false;
      }

      string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
      {
        error = "Usage: /help";
        return false;
      }

      string command = parts[0];
      string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

      bool handled = _commandService.TryExecute(command, args, sender, suppressSystemMessages, out pipelineValues, out error);
      if (!handled)
      {
        error = $"Unknown command: {command}";
        return false;
      }

      if (!string.IsNullOrWhiteSpace(error))
      {
        // The command exists and ran. When system messages are enabled the
        // command already delivered its own error/usage output to the user,
        // so the caller must not print it again (prevents duplicate hints).
        alreadyReported = !suppressSystemMessages;
        return false;
      }

      return true;
    }

    private static int CountPlaceholders(string input)
    {
      if (string.IsNullOrEmpty(input))
        return 0;

      int count = 0;
      for (int i = 0; i < input.Length - 1; i++)
      {
        if (input[i] == '{' && input[i + 1] == '}')
        {
          count++;
          i++;
        }
      }

      return count;
    }

    private static string ReplaceFirstPlaceholder(string input, string value)
    {
      int index = input.IndexOf("{}", StringComparison.Ordinal);
      if (index < 0)
        return input;

      return input.Substring(0, index)
             + (value ?? string.Empty)
             + input.Substring(index + 2);
    }

    public void BroadcastSystemMessage(string message)
    {
      ReceiveChatObserversRpc($"<color=#FFD700>[System]</color> {message}");
    }

    public string GetDisplayName(NetworkConnection conn) => conn?.ClientId.ToString() ?? "Server";

    private TitleUIController GetTitleUIController()
    {
      return Registry.Registry.Get<TitleUIController>(RegistryType.UI, Registry.Registry.TypeKey<TitleUIController>());
    }

    private bool DispatchToTargets(
      IEnumerable<NetworkConnection> targets,
      System.Action<NetworkConnection> dispatch,
      out string error)
    {
      error = string.Empty;

      if (!IsServer)
      {
        error = "Title command can only be invoked on the server.";
        return false;
      }

      if (targets == null)
      {
        error = "No target players were matched.";
        return false;
      }

      bool anyTarget = false;
      foreach (var target in targets)
      {
        if (target == null)
          continue;

        anyTarget = true;
        dispatch?.Invoke(target);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }
#endregion
  }
}
