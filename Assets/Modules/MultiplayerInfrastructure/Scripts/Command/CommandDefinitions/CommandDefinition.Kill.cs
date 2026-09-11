using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public sealed class CommandDefinition_Kill : IChatCommandModel, IChatCommandUsage
  {
    private const string FallbackSpawnPointIdentifier = "spawnpoint-commons";

    public string CommandEntry => "kill";
    public string Description => "Return players to spawn or remove an entity from the world.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("kill <player>", "Return each matching player to the default spawn point."),
      new UsageLine("kill <entity_identifier>", "Remove the registered non-player entity from the world."),
      new UsageLine("  <player>", PlayerTargetResolver.ShortSyntaxHint + "."),
    };
    public string PermissionIdentifier => "kill";

    private readonly ChatService _chat;

    public CommandDefinition_Kill(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
      {
        _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
        return;
      }

      string targetToken = args[0].Trim();
      if (PlayerTargetResolver.TryResolveControllers(sender, targetToken, out var players, out string playerError))
      {
        KillPlayers(sender, targetToken, players);
        return;
      }

      string entityIdentifier = NormalizeEntityIdentifier(targetToken);
      if (!Registry.Registry.TryGetEntity(entityIdentifier, out var entity) || entity?.GameObject == null)
      {
        _chat.SendSystemMessage(sender,
          $"Target '{targetToken}' was not found as a spawned player or registered entity. {playerError}");
        return;
      }

      if (entity.EntityType == EntityType.Player)
      {
        _chat.SendSystemMessage(sender,
          $"Player entity '{entityIdentifier}' is not currently associated with a spawned player object.");
        return;
      }

      GameObject target = entity.GameObject;
      DespawnEntity(target);
      _chat.SendSystemNotification(sender, $"Removed entity '{entityIdentifier}' from the world.");
    }

    private void KillPlayers(
      NetworkConnection sender,
      string targetToken,
      System.Collections.Generic.IReadOnlyList<PlayerController> players)
    {
      if (!TryGetDefaultSpawnPoint(out Transform spawnPoint, out string spawnPointIdentifier))
      {
        _chat.SendSystemMessage(sender,
          $"Default spawn point '{spawnPointIdentifier}' is not available; no players were moved.");
        return;
      }

      for (int i = 0; i < players.Count; i++)
        players[i].TeleportToServer(spawnPoint.position);

      if (players.Count == 1)
      {
        string playerName = PlayerTargetResolver.DescribeTarget(targetToken, players[0]);
        _chat.SendSystemNotification(sender, $"Killed {playerName}; returned to spawn point '{spawnPointIdentifier}'.");
        return;
      }

      _chat.SendSystemNotification(sender,
        $"Killed {players.Count} players; returned them to spawn point '{spawnPointIdentifier}'.");
    }

    private static bool TryGetDefaultSpawnPoint(out Transform spawnPoint, out string identifier)
    {
      identifier = Registry.Registry.Get<string>(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.DefaultCommonSpawnPoint);

      if (string.IsNullOrWhiteSpace(identifier))
        identifier = FallbackSpawnPointIdentifier;

      return PlayerSpawnPointRegistry.TryGet(identifier, out spawnPoint);
    }

    private static string NormalizeEntityIdentifier(string token)
    {
      const string entityPrefix = "entity:";
      return token.StartsWith(entityPrefix, StringComparison.OrdinalIgnoreCase)
        ? token.Substring(entityPrefix.Length).Trim()
        : token;
    }

    private static void DespawnEntity(GameObject target)
    {
      NetworkObject networkObject = target.GetComponent<NetworkObject>();
      if (InstanceFinder.IsServerStarted && networkObject != null && networkObject.IsSpawned)
      {
        InstanceFinder.ServerManager.Despawn(networkObject);
        return;
      }

      UnityEngine.Object.Destroy(target);
    }
  }
}
