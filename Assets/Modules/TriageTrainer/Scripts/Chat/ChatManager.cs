using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using TriageTrainer.Command;
using TriageTrainer.UI;
using TriageTrainer.Definitions;
using TriageTrainer.Registry;
using UnityEngine;

namespace TriageTrainer.Chat
{
  [RequireComponent(typeof(ChatUIController))]
  [RequireComponent(typeof(ChatUIController_ChatLogView))]
  [RequireComponent(typeof(ChatCommandService))]
  public class ChatManager : NetworkBehaviour
  {
    [Header("ChatSettings")] [SerializeField, Min(0f)]
    private float _messageCooldownSeconds = DefaultsChatControl.MessageCooldownSeconds;

    private ChatUIController _uiController;
    private ChatUIController_ChatLogView _chatLogView;
    private ChatCommandService _commandService;
    
    private readonly Dictionary<int, float> _lastMessageTimes = new();

    void Awake()
    {
      _uiController = GetComponent<ChatUIController>();
      _chatLogView = GetComponent<ChatUIController_ChatLogView>();
      _commandService = GetComponent<ChatCommandService>();
      _commandService.Initialize(this);

      _uiController.OnSubmitted += HandleLocalSubmission;
      _uiController.OnCancelled += HandleCancel;
    }
    
    private void HandleLocalSubmission(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
      {
        _uiController.UnfocusInput();
        return;
      }

      _uiController.UnfocusInput();

      if (raw.StartsWith("/"))
      {
        ExecuteCommandServerRpc(raw[1..]);
        return;
      }

      SendChatServerRpc(raw);
    }

    private void HandleCancel()
    {
      _uiController.UnfocusInput();
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
      Debug.Log($"[ChatManager] Received chat message: {formattedLine}");
      _chatLogView.AddLine(formattedLine);
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
      _chatLogView.AddLine($"<color=#FFD700>[System]</color> {message}");
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