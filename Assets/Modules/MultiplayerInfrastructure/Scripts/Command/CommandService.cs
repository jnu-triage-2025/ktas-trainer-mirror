using System;
using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public class ChatCommandService : MonoBehaviour
  {
    private readonly Dictionary<string, IChatCommandModel> _commands = new();
    private ChatService _chatManager;

    public void Initialize(ChatService manager)
    {
      _chatManager = manager;

      RegisterCommand(new CommandDefinition_Help(_chatManager, this));
      RegisterCommand(new CommandDefinition_Gamemode(_chatManager));
      RegisterCommand(new CommandDefinition_Give(_chatManager));
      RegisterCommand(new CommandDefinition_Clean(_chatManager));
      RegisterCommand(new CommandDefinition_Tag(_chatManager));
      RegisterCommand(new CommandDefinition_Scoreboard(_chatManager));
      RegisterCommand(new CommandDefinition_Scenario(_chatManager));
      RegisterCommand(new CommandDefinition_ProblemSheet(_chatManager));
      RegisterCommand(new CommandDefinition_Character(_chatManager));
      RegisterCommand(new CommandDefinition_Title(_chatManager));
      RegisterCommand(new CommandDefinition_EntityPreset(_chatManager));
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
      return TryExecute(commandName, args, sender, suppressSystemMessages: false, out _, out _);
    }

    public bool TryExecute(
      string commandName,
      string[] args,
      NetworkConnection sender,
      bool suppressSystemMessages,
      out IReadOnlyList<string> pipelineValues,
      out string error)
    {
      pipelineValues = System.Array.Empty<string>();
      error = string.Empty;

      string key = commandName?.ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(key))
      {
        error = "Command name is required.";
        return false;
      }

      if (!_commands.TryGetValue(key, out IChatCommandModel command))
      {
        error = $"Unknown command: {commandName}";
        return false;
      }

      if (command.RequiresAdmin/* && !_chatManager.IsAdmin(sender)*/)
      {
        if (!suppressSystemMessages)
          _chatManager.SendSystemMessage(sender, "Permission denied.");
        error = "Permission denied.";
        return true;
      }

      if (command is IChatCommandPipelineCommand pipelineCommand)
      {
        if (!pipelineCommand.TryExecute(sender, args, suppressSystemMessages, out pipelineValues, out error))
          return true;

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
