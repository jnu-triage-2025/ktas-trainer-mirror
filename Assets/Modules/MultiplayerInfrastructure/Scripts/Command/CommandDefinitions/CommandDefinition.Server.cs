using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Server;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Command
{
  public sealed class CommandDefinition_Server : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "server";
    public string Description => "Control the FishNet server and connected players.";
    public string PermissionIdentifier => "server";
    public IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("stop", "Stop the server."),
      new UsageLine("kick <player>", "Disconnect a connected player."),
      new UsageLine("ban <player>", "Ban a player by display name and disconnect them."),
      new UsageLine("unban <player>", "Remove a display name from the ban list."),
      new UsageLine("banlist", "Return the current ban list."),
      new UsageLine("  <player>", "@selector (@a, @p, @r, @s), <clientId>, id:<uuid>, name:<displayName>, or a display name."),
    };

    private readonly ChatService _chat;

    public CommandDefinition_Server(ChatService chat) => _chat = chat;

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /server <stop|kick|ban|unban|banlist>");
        return;
      }

      switch (args[0].ToLowerInvariant())
      {
        case "stop":
          Stop(sender);
          break;
        case "kick":
          Kick(sender, args, ban: false);
          break;
        case "ban":
          Kick(sender, args, ban: true);
          break;
        case "unban":
          Unban(sender, args);
          break;
        case "banlist":
          BanList(sender);
          break;
        default:
          _chat.SendSystemMessage(sender, $"Unknown server subcommand '{args[0]}'.");
          break;
      }
    }

    private void Stop(NetworkConnection sender)
    {
      if (!InstanceFinder.IsServerStarted)
      {
        _chat.SendSystemMessage(sender, "The server is not running.");
        return;
      }

      _chat.SendSystemMessage(sender, "Stopping server.");
      InstanceFinder.ServerManager.StopConnection(true);
    }

    private void Kick(NetworkConnection sender, string[] args, bool ban)
    {
      if (!TryResolveTargets(sender, args, out var targets))
      {
        _chat.SendSystemMessage(sender, $"Usage: /server {(ban ? "ban" : "kick")} <player>");
        return;
      }

      foreach ((NetworkConnection connection, UserDescriptor descriptor) in targets)
      {
        if (ban)
        {
          ServerBanService.Ban(descriptor.DisplayName);
          _chat.SendSystemMessage(sender, $"Banned '{descriptor.DisplayName}'.");
        }
        else
        {
          _chat.SendSystemMessage(sender, $"Kicked '{descriptor.DisplayName}'.");
        }

        connection.Disconnect(true);
      }
    }

    private void Unban(NetworkConnection sender, string[] args)
    {
      string name = GetTargetText(args);
      if (string.IsNullOrWhiteSpace(name))
      {
        _chat.SendSystemMessage(sender, "Usage: /server unban <player>");
        return;
      }

      _chat.SendSystemMessage(sender, ServerBanService.Unban(name)
        ? $"Unbanned '{name}'."
        : $"'{name}' is not in the ban list.");
    }

    private void BanList(NetworkConnection sender)
    {
      var names = ServerBanService.GetBanList();
      _chat.SendSystemMessage(sender, names.Count == 0
        ? "Ban list is empty."
        : "Ban list:\n" + string.Join("\n", names.Select(name => $"- {name}")));
    }

    /// <summary>
    /// 대상 토큰을 접속 중인 연결로 해석한다. 선택자(@a 등), clientId, id:/name:, 표시 이름을 허용한다.
    /// </summary>
    private static bool TryResolveTargets(
      NetworkConnection sender,
      string[] args,
      out List<(NetworkConnection Connection, UserDescriptor Descriptor)> targets)
    {
      targets = new List<(NetworkConnection, UserDescriptor)>();
      string target = GetTargetText(args);
      if (string.IsNullOrWhiteSpace(target) || InstanceFinder.ServerManager == null)
        return false;

      if (int.TryParse(target, out int clientId))
      {
        if (!InstanceFinder.ServerManager.Clients.TryGetValue(clientId, out NetworkConnection connection)
            || !UserDescriptorService.TryGetByClientId(clientId, out UserDescriptor descriptor))
          return false;

        targets.Add((connection, descriptor));
        return true;
      }

      if (!PlayerTargetResolver.TryResolve(sender, target, out var descriptors, out _))
        return false;

      foreach (UserDescriptor descriptor in descriptors)
      {
        if (!UserDescriptorService.TryGetClientId(descriptor.Identifier, out int resolvedClientId)
            || !InstanceFinder.ServerManager.Clients.TryGetValue(resolvedClientId, out NetworkConnection connection))
          continue;

        targets.Add((connection, descriptor));
      }

      return targets.Count > 0;
    }

    private static string GetTargetText(string[] args)
      => args == null || args.Length < 2 ? string.Empty : string.Join(' ', args.Skip(1)).Trim();
  }

  public sealed class CommandDefinition_ServerAlias : IChatCommandModel
  {
    private readonly CommandDefinition_Server _server;
    private readonly string _alias;
    private readonly string _subcommand;

    public CommandDefinition_ServerAlias(CommandDefinition_Server server, string alias, string subcommand)
    {
      _server = server;
      _alias = alias;
      _subcommand = subcommand;
    }

    public string CommandEntry => _alias;
    public string Description => $"Alias for /server {_subcommand}.";
    public string PermissionIdentifier => "server";

    public void Execute(NetworkConnection sender, string[] args)
    {
      var forwarded = new List<string> { _subcommand };
      if (args != null)
        forwarded.AddRange(args);
      _server.Execute(sender, forwarded.ToArray());
    }
  }
}
