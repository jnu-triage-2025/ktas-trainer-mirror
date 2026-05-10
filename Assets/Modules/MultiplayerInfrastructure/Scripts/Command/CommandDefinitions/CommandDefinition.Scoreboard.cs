using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Variable;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Scoreboard : IChatCommandModel
  {
    public string CommandEntry => "scoreboard";
    public string Description =>
      "Session-wide per-player variable storage.\n" +
      "  /scoreboard objectives add <objective> <criteria(dummy|trigger)>\n" +
      "  /scoreboard objectives list\n" +
      "  /scoreboard objectives remove <objective>\n" +
      "  /scoreboard players get <target> <objective>\n" +
      "  /scoreboard players set <target> <objective> <value>\n" +
      "  /scoreboard players add <target> <objective> <value>\n" +
      "  /scoreboard players remove <target> <objective> <value>\n" +
      "  /scoreboard players list [target]\n" +
      "  /scoreboard players reset <target> [objective]\n" +
      "  /scoreboard players operation <target> <targetObjective> <op> <source> <sourceObjective>";
    public bool RequiresAdmin => false;

    private static readonly HashSet<string> SupportedCriteria = new(StringComparer.OrdinalIgnoreCase)
    {
      "dummy",
      "trigger"
    };

    private readonly ChatService _chat;

    public CommandDefinition_Scoreboard(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, Description);
        return;
      }

      string category = args[0].ToLowerInvariant();
      string[] rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

      switch (category)
      {
        case "objectives":
          HandleObjectives(sender, rest);
          return;
        case "players":
          HandlePlayers(sender, rest);
          return;
        default:
          _chat.SendSystemMessage(sender, $"Unknown category '{args[0]}'. Use objectives|players.");
          return;
      }
    }

    private void HandleObjectives(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /scoreboard objectives <add|list|remove> ...");
        return;
      }

      string action = args[0].ToLowerInvariant();
      switch (action)
      {
        case "add":
          if (args.Length < 3)
          {
            _chat.SendSystemMessage(sender, "Usage: /scoreboard objectives add <objective> <criteria>");
            return;
          }

          string objective = args[1];
          string criteria = args[2];
          if (!SupportedCriteria.Contains(criteria))
          {
            _chat.SendSystemMessage(sender, "Supported criteria: dummy, trigger");
            return;
          }

          if (!SessionVariableService.AddObjective(objective, criteria, out string addError))
          {
            _chat.SendSystemMessage(sender, addError);
            return;
          }

          _chat.SendSystemMessage(sender, $"Objective '{objective}' added with criteria '{criteria}'.");
          return;

        case "list":
          IReadOnlyCollection<SessionVariableService.ObjectiveDefinition> all = SessionVariableService.GetObjectives();
          if (all.Count == 0)
          {
            _chat.SendSystemMessage(sender, "No objectives registered.");
            return;
          }

          string joined = string.Join(
            "\n",
            all.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => $"- {x.Name} ({x.Criteria})"));
          _chat.SendSystemMessage(sender, $"Objectives:\n{joined}");
          return;

        case "remove":
          if (args.Length < 2)
          {
            _chat.SendSystemMessage(sender, "Usage: /scoreboard objectives remove <objective>");
            return;
          }

          if (!SessionVariableService.RemoveObjective(args[1], out string removeError))
          {
            _chat.SendSystemMessage(sender, removeError);
            return;
          }

          _chat.SendSystemMessage(sender, $"Objective '{args[1]}' removed.");
          return;

        default:
          _chat.SendSystemMessage(sender, $"Unknown objectives action '{args[0]}'.");
          return;
      }
    }

    private void HandlePlayers(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /scoreboard players <get|set|add|remove|list|reset|operation> ...");
        return;
      }

      string action = args[0].ToLowerInvariant();
      switch (action)
      {
        case "get":
          HandlePlayersGet(sender, args[1..]);
          return;
        case "set":
          HandlePlayersSet(sender, args[1..]);
          return;
        case "add":
          HandlePlayersAdd(sender, args[1..]);
          return;
        case "remove":
          HandlePlayersRemove(sender, args[1..]);
          return;
        case "list":
          HandlePlayersList(sender, args.Length > 1 ? args[1..] : Array.Empty<string>());
          return;
        case "reset":
          HandlePlayersReset(sender, args[1..]);
          return;
        case "operation":
          HandlePlayersOperation(sender, args[1..]);
          return;
        default:
          _chat.SendSystemMessage(sender, $"Unknown players action '{args[0]}'.");
          return;
      }
    }

    private void HandlePlayersGet(NetworkConnection sender, string[] args)
    {
      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /scoreboard players get <target> <objective>");
        return;
      }

      if (!TryResolveSession(sender, args[0], out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      string objective = args[1];
      if (!SessionVariableService.ContainsObjective(objective))
      {
        _chat.SendSystemMessage(sender, $"Objective '{objective}' does not exist.");
        return;
      }

      if (!SessionVariableService.TryGetScore(target.Identifier, objective, out int value))
      {
        _chat.SendSystemMessage(sender, $"{target.DisplayName} has no score in '{objective}'.");
        return;
      }

      _chat.SendSystemMessage(sender, $"{target.DisplayName} {objective} = {value}");
    }

    private void HandlePlayersSet(NetworkConnection sender, string[] args)
    {
      if (!TryParseTargetObjectiveValue(sender, args, out var target, out string objective, out int value))
        return;

      if (!SessionVariableService.SetScore(target.Identifier, objective, value, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"{target.DisplayName} {objective} = {value}");
    }

    private void HandlePlayersAdd(NetworkConnection sender, string[] args)
    {
      if (!TryParseTargetObjectiveValue(sender, args, out var target, out string objective, out int value))
        return;

      if (!SessionVariableService.AddScore(target.Identifier, objective, value, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      SessionVariableService.TryGetScore(target.Identifier, objective, out int newValue);
      _chat.SendSystemMessage(sender, $"{target.DisplayName} {objective} = {newValue}");
    }

    private void HandlePlayersRemove(NetworkConnection sender, string[] args)
    {
      if (!TryParseTargetObjectiveValue(sender, args, out var target, out string objective, out int value))
        return;

      if (!SessionVariableService.RemoveScore(target.Identifier, objective, value, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      SessionVariableService.TryGetScore(target.Identifier, objective, out int newValue);
      _chat.SendSystemMessage(sender, $"{target.DisplayName} {objective} = {newValue}");
    }

    private void HandlePlayersList(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        var allUsers = UserDescriptorService.GetAll();
        if (allUsers.Count == 0)
        {
          _chat.SendSystemMessage(sender, "No tracked players.");
          return;
        }

        string users = string.Join("\n", allUsers.Values.OrderBy(x => x.DisplayName).Select(x => $"- {x.DisplayName}"));
        _chat.SendSystemMessage(sender, $"Tracked players:\n{users}");
        return;
      }

      if (!TryResolveSession(sender, args[0], out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      IReadOnlyDictionary<string, int> scores = SessionVariableService.GetScoresForUser(target.Identifier);
      if (scores.Count == 0)
      {
        _chat.SendSystemMessage(sender, $"{target.DisplayName} has no scores.");
        return;
      }

      string joined = string.Join("\n", scores.OrderBy(x => x.Key).Select(x => $"- {x.Key}: {x.Value}"));
      _chat.SendSystemMessage(sender, $"Scores for {target.DisplayName}:\n{joined}");
    }

    private void HandlePlayersReset(NetworkConnection sender, string[] args)
    {
      if (args.Length < 1)
      {
        _chat.SendSystemMessage(sender, "Usage: /scoreboard players reset <target> [objective]");
        return;
      }

      if (!TryResolveSession(sender, args[0], out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      if (args.Length >= 2)
      {
        if (!SessionVariableService.ResetScore(target.Identifier, args[1], out error))
        {
          _chat.SendSystemMessage(sender, error);
          return;
        }

        _chat.SendSystemMessage(sender, $"Reset '{args[1]}' score for {target.DisplayName}.");
        return;
      }

      if (!SessionVariableService.ResetAllScores(target.Identifier, out error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"Reset all scores for {target.DisplayName}.");
    }

    private void HandlePlayersOperation(NetworkConnection sender, string[] args)
    {
      if (args.Length < 5)
      {
        _chat.SendSystemMessage(
          sender,
          "Usage: /scoreboard players operation <target> <targetObjective> <op> <source> <sourceObjective>");
        return;
      }

      if (!TryResolveSession(sender, args[0], out var target, out string targetError))
      {
        _chat.SendSystemMessage(sender, targetError);
        return;
      }

      if (!TryResolveSession(sender, args[3], out var source, out string sourceError))
      {
        _chat.SendSystemMessage(sender, sourceError);
        return;
      }

      string targetObjective = args[1];
      string operation = args[2];
      string sourceObjective = args[4];

      if (!SessionVariableService.ApplyOperation(
            target.Identifier,
            targetObjective,
            operation,
            source.Identifier,
            sourceObjective,
            out string operationError))
      {
        _chat.SendSystemMessage(sender, operationError);
        return;
      }

      SessionVariableService.TryGetScore(target.Identifier, targetObjective, out int newValue);
      _chat.SendSystemMessage(sender, $"{target.DisplayName} {targetObjective} = {newValue}");
    }

    private bool TryParseTargetObjectiveValue(
      NetworkConnection sender,
      string[] args,
      out UserDescriptor target,
      out string objective,
      out int value)
    {
      target = null;
      objective = string.Empty;
      value = 0;

      if (args.Length < 3)
      {
        _chat.SendSystemMessage(sender, "Usage: <target> <objective> <value>");
        return false;
      }

      if (!TryResolveSession(sender, args[0], out target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return false;
      }

      objective = args[1];
      if (!int.TryParse(args[2], out value))
      {
        _chat.SendSystemMessage(sender, $"Invalid integer value '{args[2]}'.");
        return false;
      }

      return true;
    }

    private bool TryResolveSession(
      NetworkConnection sender,
      string selector,
      out UserDescriptor session,
      out string error)
    {
      session = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(selector))
      {
        error = "Player selector is required.";
        return false;
      }

      if (selector.Equals("@self", StringComparison.OrdinalIgnoreCase)
          || selector.Equals("@s", StringComparison.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = "Unable to locate command sender.";
          return false;
        }

        if (!UserDescriptorService.TryGetByClientId(sender.ClientId, out session))
        {
          error = "Sender session not found.";
          return false;
        }

        return true;
      }

      if (!UserDescriptorService.TryGetByDisplayName(selector, out session))
      {
        error = $"Player '{selector}' not found.";
        return false;
      }

      return true;
    }
  }
}
