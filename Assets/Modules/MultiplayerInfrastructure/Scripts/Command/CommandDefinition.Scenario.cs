using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Scenario : IChatCommandModel
  {
    public string CommandEntry => "scenario";
    public string Description => "Execute a scenario. Usage: /scenario execute <player> <scenario_id>";
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;
    private readonly ScenarioCommandRunner _runner;

    public CommandDefinition_Scenario(ChatService chat, ScenarioCommandRunner runner)
    {
      _chat = chat;
      _runner = runner;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (_runner == null)
      {
        _chat.SendSystemMessage(sender, "Scenario command is not configured on the server.");
        return;
      }

      if (args == null || args.Length < 3 || !string.Equals(args[0], "execute", StringComparison.OrdinalIgnoreCase))
      {
        _chat.SendSystemMessage(sender, "Usage: /scenario execute <player> <scenario_id>");
        return;
      }

      string playerSelector = args[1];
      string scenarioId = string.Join(' ', args[2..]).Trim();
      if (string.IsNullOrWhiteSpace(scenarioId))
      {
        _chat.SendSystemMessage(sender, "Scenario identifier is required.");
        return;
      }

      if (!TryResolveTargets(sender, playerSelector, out List<NetworkConnection> targets, out string targetError))
      {
        _chat.SendSystemMessage(sender, targetError);
        return;
      }

      if (!_runner.TryExecuteScenario(scenarioId, targets, out string execError))
      {
        _chat.SendSystemMessage(sender, execError);
        return;
      }

      _chat.SendSystemMessage(sender, $"Scenario '{scenarioId}' dispatched to {targets.Count} player(s).");
    }

    private bool TryResolveTargets(NetworkConnection sender, string raw, out List<NetworkConnection> targets, out string error)
    {
      targets = new List<NetworkConnection>();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(raw))
      {
        error = "Target player selector is required.";
        return false;
      }

      string lowered = raw.ToLowerInvariant();

      if (lowered == "@s")
      {
        if (sender == null)
        {
          error = "Unable to locate the command executor.";
          return false;
        }

        targets.Add(sender);
      }
      else if (lowered == "@a")
      {
        targets.AddRange(GetAllConnections());
      }
      else if (lowered == "@n")
      {
        if (!TryGetNearestPlayer(sender, out NetworkConnection nearest, out error))
          return false;

        if (nearest != null)
          targets.Add(nearest);
      }
      else if (lowered.StartsWith("fish:"))
      {
        string idText = raw.Substring("fish:".Length);
        if (!int.TryParse(idText, out int clientId))
        {
          error = "Invalid FishNet player identifier after 'fish:'.";
          return false;
        }

        var match = FindConnectionByClientId(clientId);
        if (match == null)
        {
          error = $"No player found for fish id '{clientId}'.";
          return false;
        }

        targets.Add(match);
      }
      else if (lowered.StartsWith("id:"))
      {
        error = "Lookup by descriptor id is not implemented yet.";
        return false;
      }
      else if (lowered.StartsWith("name:"))
      {
        error = "Lookup by player name is not implemented yet.";
        return false;
      }
      else
      {
        error = "Unknown target selector. Use fish:, id:, name:, @s, @n, or @a.";
        return false;
      }

      if (targets.Count == 0)
      {
        error = "No players matched the selector.";
        return false;
      }

      targets = targets
        .Where(t => t != null)
        .GroupBy(t => (int)t.ClientId)
        .Select(g => g.First())
        .ToList();

      if (targets.Count == 0)
      {
        error = "No valid players matched the selector.";
        return false;
      }

      return true;
    }

    private bool TryGetNearestPlayer(NetworkConnection sender, out NetworkConnection target, out string error)
    {
      target = null;
      error = string.Empty;

      var players = GetAllPlayerControllers();
      if (players.Count == 0)
      {
        error = "No players are connected.";
        return false;
      }

      var senderController = FindPlayerController(sender, players);
      if (senderController == null)
      {
        error = "Unable to locate the command executor's player on the server.";
        return false;
      }

      var others = players.Where(p => p.Owner != null && p.Owner != sender).ToList();
      var pool = others.Count > 0 ? others : players;

      PlayerController closest = null;
      float bestSqr = float.MaxValue;
      Vector3 origin = senderController.transform.position;

      foreach (var player in pool)
      {
        if (player == null || player.Owner == null)
          continue;

        float sqr = (player.transform.position - origin).sqrMagnitude;
        if (sqr < bestSqr)
        {
          bestSqr = sqr;
          closest = player;
        }
      }

      if (closest == null || closest.Owner == null)
      {
        error = "Unable to resolve the nearest player.";
        return false;
      }

      target = closest.Owner;
      return true;
    }

    private List<NetworkConnection> GetAllConnections()
    {
      var result = new List<NetworkConnection>();
      var clients = InstanceFinder.ServerManager?.Clients;

      if (clients != null)
      {
        foreach (var kvp in clients)
        {
          if (kvp.Value != null)
            result.Add(kvp.Value);
        }
      }

      return result;
    }

    private List<PlayerController> GetAllPlayerControllers()
    {
      var found = UnityEngine.Object.FindObjectsOfType<PlayerController>();
      var result = new List<PlayerController>(found.Length);

      foreach (var player in found)
      {
        if (player != null && player.Owner != null)
          result.Add(player);
      }

      return result;
    }

    private PlayerController FindPlayerController(NetworkConnection connection, List<PlayerController> candidates)
    {
      if (connection == null || candidates == null)
        return null;

      foreach (var player in candidates)
      {
        if (player == null)
          continue;

        if (player.Owner == connection)
          return player;
      }

      return null;
    }

    private NetworkConnection FindConnectionByClientId(int clientId)
    {
      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return null;

      foreach (var kvp in clients)
      {
        var candidate = kvp.Value;
        if (candidate != null && candidate.ClientId == clientId)
          return candidate;
      }

      return null;
    }
  }
}
