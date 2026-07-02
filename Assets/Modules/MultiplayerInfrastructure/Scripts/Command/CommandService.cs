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

      // 관리자 전용 커맨드: 호스트(서버 로컬 클라이언트) 또는 서버 콘솔(sender == null)만 허용한다.
      // (기존에는 IsAdmin 검사가 주석 처리되어 RequiresAdmin 커맨드가 모두에게 거부되는 버그가 있었다.)
      if (command.RequiresAdmin && sender != null && !sender.IsHost)
      {
        if (!suppressSystemMessages)
          _chatManager.SendSystemMessage(sender, "Permission denied.");
        error = "Permission denied.";
        return true;
      }

      // Intercept help flags (-h / --help / /? / ?) for every command so that
      // detailed usage is shown without executing the command itself.
      if (ChatCommandHelp.IsHelpFlag(args))
      {
        if (!suppressSystemMessages)
          _chatManager.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(command));

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
