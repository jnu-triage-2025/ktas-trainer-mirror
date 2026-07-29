using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Title : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "title";
    public string Description => "Display screen titles and actionbar text.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("title <targets> clear", "Hide all title text."),
      new UsageLine("title <targets> reset", "Reset times and subtitle."),
      new UsageLine("title <targets> title <text>", "Show a title."),
      new UsageLine("title <targets> subtitle <text>", "Show a subtitle."),
      new UsageLine("title <targets> actionbar <text>", "Show actionbar text."),
      new UsageLine("title <targets> times <fadeIn> <stay> <fadeOut>", "Set timings, in ticks."),
      new UsageLine("  <targets>", "@s, @a, @n, fish:<id>, or an @selector."),
    };

    public string PermissionIdentifier => "title";

    private readonly ChatService _chat;

    public CommandDefinition_Title(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length < 2)
      {
        _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
        return;
      }

      string targetSelector = args[0];
      string sub = args[1].ToLowerInvariant();

      if (!TryResolveTargets(sender, targetSelector, out List<NetworkConnection> targets, out string targetError))
      {
        _chat.SendSystemMessage(sender, targetError);
        return;
      }

      switch (sub)
      {
        case "clear":
          if (args.Length != 2)
          {
            _chat.SendSystemMessage(sender, "Usage: /title <targets> clear");
            return;
          }

          Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitleClear(t, out err), "Title cleared");
          return;

        case "reset":
          if (args.Length != 2)
          {
            _chat.SendSystemMessage(sender, "Usage: /title <targets> reset");
            return;
          }

          Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitleReset(t, out err), "Title reset");
          return;

        case "times":
          if (args.Length != 5)
          {
            _chat.SendSystemMessage(sender, "Usage: /title <targets> times <fadeIn> <stay> <fadeOut>");
            return;
          }

          if (!TryParseTicks(args[2], args[3], args[4], out int fadeIn, out int stay, out int fadeOut))
          {
            _chat.SendSystemMessage(sender, "Times must be non-negative integers in ticks.");
            return;
          }

          Dispatch(
            sender,
            targets,
            (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitleTimes(t, fadeIn, stay, fadeOut, out err),
            $"Title times set to {fadeIn}/{stay}/{fadeOut} ticks");
          return;

        case "title":
        case "subtitle":
        case "actionbar":
          if (args.Length < 3)
          {
            _chat.SendSystemMessage(sender, $"Usage: /title <targets> {sub} <text>");
            return;
          }

          string text = string.Join(' ', args[2..]).Trim();
          if (string.IsNullOrWhiteSpace(text))
          {
            _chat.SendSystemMessage(sender, "Text cannot be empty.");
            return;
          }

          if (sub == "title")
            Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitle(t, text, null, out err), "Title displayed");
          else if (sub == "subtitle")
            Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchSubtitle(t, text, out err), "Subtitle updated");
          else
            Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchActionbar(t, text, out err), "Actionbar displayed");
          return;
      }

      _chat.SendSystemMessage(sender, $"Unknown subcommand '{args[1]}'. Use /title for help.");
    }

    private delegate bool DispatchCall(IEnumerable<NetworkConnection> targets, out string error);

    private void Dispatch(
      NetworkConnection sender,
      List<NetworkConnection> targets,
      DispatchCall action,
      string successMessage)
    {
      if (action == null)
        return;

      if (!action(targets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"{successMessage} ({targets.Count} target(s)).");
    }

    private bool TryParseTicks(string fadeInRaw, string stayRaw, string fadeOutRaw, out int fadeIn, out int stay, out int fadeOut)
    {
      fadeIn = 0;
      stay = 0;
      fadeOut = 0;

      if (!int.TryParse(fadeInRaw, out fadeIn))
        return false;
      if (!int.TryParse(stayRaw, out stay))
        return false;
      if (!int.TryParse(fadeOutRaw, out fadeOut))
        return false;

      if (fadeIn < 0 || stay < 0 || fadeOut < 0)
        return false;

      return true;
    }

    private bool TryResolveTargets(NetworkConnection sender, string raw, out List<NetworkConnection> targets, out string error)
    {
      targets = new List<NetworkConnection>();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(raw))
      {
        error = "Target selector is required.";
        return false;
      }

      if (raw.StartsWith("@", StringComparison.Ordinal))
      {
        return TargetSelectorResolver.TryResolveTargets(sender, raw, out targets, out error);
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
          error = "Invalid FishNet target identifier after 'fish:'.";
          return false;
        }

        var match = FindConnectionByClientId(clientId);
        if (match == null)
        {
          error = $"No target found for fish id '{clientId}'.";
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
        error = "Lookup by target name is not implemented yet.";
        return false;
      }
      else
      {
        error = "Unknown target selector. Use fish:, id:, name:, @s, @n, or @a.";
        return false;
      }

      if (targets.Count == 0)
      {
        error = "No targets matched the selector.";
        return false;
      }

      targets = targets
        .Where(t => t != null)
        .GroupBy(t => (int)t.ClientId)
        .Select(g => g.First())
        .ToList();

      if (targets.Count == 0)
      {
        error = "No valid targets matched the selector.";
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
        error = "No targets are connected.";
        return false;
      }

      var senderController = FindPlayerController(sender, players);
      if (senderController == null)
      {
        error = "Unable to locate the command executor's target on the server.";
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
        error = "Unable to resolve the nearest target.";
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
      var found = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
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
