using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Definitions;
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
    
    private readonly Dictionary<int, float> _lastMessageTimes = new();

    void Awake()
    {
      if (_uiController == null)
        _uiController = GetComponent<ChatUIController>();
      if (_commandService == null)
        _commandService = GetComponent<ChatCommandService>();

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

    [ObserversRpc(BufferLast = true)]
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

      if (string.IsNullOrWhiteSpace(commandLine))
      {
        SendSystemMessage(sender, "Usage: /help");
        return;
      }

      string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
      {
        SendSystemMessage(sender, "Usage: /help");
        return;
      }

      string command = parts[0];
      string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

      if (_commandService.TryExecute(command, args, sender))
        return;

      SendSystemMessage(sender, $"Unknown command: {command}");
    }
    
    [TargetRpc]
    private void TargetReceiveSystemMessage(NetworkConnection conn, string message)
    {
      _uiController.AppendMessage($"<color=#FFD700>[System]</color> {message}", showToastWhenHidden: true);
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
    }

    public void BroadcastSystemMessage(string message)
    {
      ReceiveChatObserversRpc($"<color=#FFD700>[System]</color> {message}");
    }

    public string GetDisplayName(NetworkConnection conn) => conn?.ClientId.ToString() ?? "Server";
#endregion
  }
}