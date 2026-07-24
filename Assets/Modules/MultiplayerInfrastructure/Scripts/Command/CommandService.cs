using System;
using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Permission;
using MultiplayerInfrastructure.Session;
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
      RegisterCommand(new CommandDefinition_Item(_chatManager));
      RegisterCommand(new CommandDefinition_Give(_chatManager));
      RegisterCommand(new CommandDefinition_Clean(_chatManager));
      RegisterCommand(new CommandDefinition_Tag(_chatManager));
      RegisterCommand(new CommandDefinition_Scoreboard(_chatManager));
      RegisterCommand(new CommandDefinition_Scenario(_chatManager));
      RegisterCommand(new CommandDefinition_ProblemSheet(_chatManager));
      RegisterCommand(new CommandDefinition_Character(_chatManager));
      RegisterCommand(new CommandDefinition_Title(_chatManager));
      RegisterCommand(new CommandDefinition_EntityPreset(_chatManager));
      RegisterCommand(new CommandDefinition_TimeSync(_chatManager));
      RegisterCommand(new CommandDefinition_Tp(_chatManager));
      RegisterCommand(new CommandDefinition_Permission(_chatManager));
      RegisterCommand(new CommandDefinition_Log(_chatManager));
      RegisterCommand(new CommandDefinition_ConnGate(_chatManager));
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

      // ── 권한 검사 ────────────────────────────────────────────────────────────
      // sender == null (서버 콘솔) 또는 IsHost 이면 항상 허용.
      // 그 외 클라이언트는 PermissionService 를 통해 role 기반 검사를 수행한다.
      // Legacy RequiresAdmin flag 는 PermissionService 로드에 실패한 경우의 fallback으로 사용한다.
      if (sender != null && !sender.IsHost)
      {
        PermissionService.EnsureLoaded();
        string userIdentifier = ResolveUserIdentifier(sender);
        string permId = command.PermissionIdentifier;

        bool denied;
        if (!string.IsNullOrWhiteSpace(permId))
        {
          denied = !PermissionService.HasPermission(userIdentifier, permId);
        }
        else
        {
          // PermissionIdentifier 없는 커맨드는 RequiresAdmin fallback 사용
          denied = command.RequiresAdmin;
        }

        if (denied)
        {
          if (!suppressSystemMessages)
            _chatManager.SendSystemMessage(sender, "Permission denied.");
          error = "Permission denied.";
          return true;
        }
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

    /// <summary>
    /// NetworkConnection 으로부터 UserDescriptor 의 Identifier(UUID) 를 조회한다.
    /// 조회에 실패하면 빈 문자열을 반환 (PermissionService 는 빈 identifier 를 default role 로 처리한다).
    /// </summary>
    private static string ResolveUserIdentifier(NetworkConnection sender)
    {
      if (sender == null)
        return string.Empty;

      if (UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor))
        return descriptor.Identifier;

      return string.Empty;
    }
  }
}
