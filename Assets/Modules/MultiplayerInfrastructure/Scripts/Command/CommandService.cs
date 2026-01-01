using System;
using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public class ChatCommandService : MonoBehaviour
  {
    private readonly Dictionary<string, IChatCommandModel> _commands = new();
    private ChatService _chatManager;
    private ScenarioCommandRunner _scenarioRunner;
    public ScenarioCommandRunner ScenarioRunner
    {
      get => _scenarioRunner;
      set => _scenarioRunner = value;
    }

    public void Initialize(ChatService manager, ScenarioCommandRunner scenarioRunner)
    {
      _chatManager = manager;
      _scenarioRunner = scenarioRunner;

      RegisterCommand(new CommandDefinition_Help(_chatManager, this));
      RegisterCommand(new CammandDefinition_Gamemode(_chatManager));
      if (_scenarioRunner != null)
        RegisterCommand(new CommandDefinition_Scenario(_chatManager, _scenarioRunner));
      else
        Debug.LogWarning("[ChatCommandService] ScenarioCommandRunner is missing; /scenario command not registered.");
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
