using System;
using System.Collections.Generic;
using FishNet.Connection;
using TriageTrainer.Chat;
using UnityEngine;

namespace TriageTrainer.Command
{
  public class ChatCommandService : MonoBehaviour
  {
    private readonly Dictionary<string, IChatCommandModel> _commands = new();
    private ChatManager _chatManager;

    public void Initialize(ChatManager manager)
    {
      _chatManager = manager;

      RegisterCommand(new CommandDefinition_Help(_chatManager, this));
      RegisterCommand(new CammandDefinition_Gamemode(_chatManager));
      // RegisterCommand(new CommandDefinition_Kick(_chatManager));
    }

    private void RegisterCommand(IChatCommandModel command)
    {
      if (command == null)
        return;

      string key = command.CommandEntry?.ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(key))
        return;

      _commands[key] = command;
    }

    public bool TryExecute(string commandName, string[] args, NetworkConnection sender)
    {
      string key = commandName?.ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(key))
        return false;

      if (!_commands.TryGetValue(key, out IChatCommandModel command))
        return false;

      if (command.RequiresAdmin/* && !_chatManager.IsAdmin(sender)*/)
      {
        _chatManager.SendSystemMessage(sender, "Permission denied.");
        return true;
      }

      command.Execute(sender, args);
      return true;
    }

    public IEnumerable<IChatCommandModel> GetCommands()
    {
      return _commands.Values;
    }
  }
}
