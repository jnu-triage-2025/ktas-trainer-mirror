using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Datapack;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
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

      string formatted = $"<{GetDisplayName(sender)}> {rawMessage}";
      ReceiveChatObserversRpc(formatted);
      MarkMessageSent(sender);
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
    
    private void TargetRunProblemSheet(NetworkConnection conn, string problemSetIdentifier)
    {
      var controller = Registry.Registry.Get<ProblemSheetUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey<ProblemSheetUIController>());

      if (controller == null)
      {
        Debug.LogWarning("[ChatService] ProblemSheetUIController is missing on this client.");
        return;
      }

      if (!controller.OpenProblemSet(problemSetIdentifier, 0))
      {
        Debug.LogWarning($"[ChatService] Failed to open problem set '{problemSetIdentifier}'.");
      }
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

      if (!Registry.Registry.Contains(RegistryType.ScenarioGraph, scenarioIdentifier))
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

    public bool TryDispatchProblemSheet(string problemSetIdentifier, IEnumerable<NetworkConnection> targets, out string error)
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
      foreach (var target in targets)
      {
        if (target == null)
          continue;

        anyTarget = true;
        TargetRunProblemSheet(target, problemSetIdentifier);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }

    public bool TryExecuteSystemCommand(string commandLine, out string result)
    {
      return TryExecuteCommandInternal(commandLine, null, out result);
    }

    private bool TryExecuteCommandInternal(string commandLine, NetworkConnection sender, out string result)
    {
      result = string.Empty;

      if (string.IsNullOrWhiteSpace(commandLine))
      {
        result = "Usage: /help";
        SendSystemMessage(sender, result);
        return false;
      }

      string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
      {
        result = "Usage: /help";
        SendSystemMessage(sender, result);
        return false;
      }

      string command = parts[0];
      string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

      if (_commandService.TryExecute(command, args, sender))
      {
        result = $"Executed /{command}.";
        return true;
      }

      result = $"Unknown command: {command}";
      SendSystemMessage(sender, result);
      return false;
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
