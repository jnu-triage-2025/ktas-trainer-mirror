using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Problem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Scenario : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "scenario";
    public string Description => "Run and control scenarios.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("scenario list", "List available scenarios."),
      new UsageLine("scenario execute <target> <scenario>", "Start a scenario for targets."),
      new UsageLine("scenario signal <signal> [clear]", "Raise (or clear) a signal."),
      new UsageLine("scenario conflictpolicy [warn|cancel|panic]", "Get/set concurrent-dialogue conflict policy."),
      new UsageLine("  <target>", "@s, @a, @n, or fish:<id>."),
      new UsageLine("  <scenario>", "Registered scenario identifier."),
    };
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;

    public CommandDefinition_Scenario(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args != null
          && args.Length >= 1
          && string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
      {
        SendScenarioList(sender);
        return;
      }

      // 디버그 훅(G-4): 인터랙션 완료 신호를 수동으로 올리거나 내린다.
      // 게임플레이의 실제 ScenarioInteractionSignals.Raise 배선 전에 게이트(Validator/Parallel) 통합 테스트에 사용한다.
      // 사용: /scenario signal <signal_id> [clear]
      if (args != null
          && args.Length >= 2
          && string.Equals(args[0], "signal", StringComparison.OrdinalIgnoreCase))
      {
        string signalId = args[1].Trim();
        if (string.IsNullOrWhiteSpace(signalId))
        {
          _chat.SendSystemMessage(sender, "Signal identifier is required. Usage: /scenario signal <signal_id> [clear]");
          return;
        }

        bool clear = args.Length >= 3 && string.Equals(args[2], "clear", StringComparison.OrdinalIgnoreCase);
        if (clear)
        {
          ScenarioInteractionSignals.Clear(signalId);
          _chat.SendSystemMessage(sender, $"Scenario signal '{ScenarioInteractionSignals.Normalize(signalId)}' cleared.");
        }
        else
        {
          ScenarioInteractionSignals.Raise(signalId);
          _chat.SendSystemMessage(sender, $"Scenario signal '{ScenarioInteractionSignals.Normalize(signalId)}' raised.");
        }
        return;
      }

      // /scenario conflictpolicy [warn|cancel|panic]
      // 두 개 이상의 시나리오 흐름이 동시에 대화창(Dialogue/Choice/Quiz)을 점유하려 할 때의 정책을
      // 조회/변경한다. 인자 없으면 현재 값을 보고한다.
      if (args != null
          && args.Length >= 1
          && string.Equals(args[0], "conflictpolicy", StringComparison.OrdinalIgnoreCase))
      {
        var controller = ScenarioController.Instance;
        if (controller == null)
        {
          _chat.SendSystemMessage(sender, "ScenarioController instance is not available.");
          return;
        }

        if (args.Length < 2)
        {
          _chat.SendSystemMessage(sender,
            $"Current scenario concurrency conflict policy: {controller.ConcurrencyConflictPolicy}. "
            + "Change with: /scenario conflictpolicy <warn|cancel|panic>");
          return;
        }

        if (!ScenarioConcurrencyConflictPolicyExtensions.TryParse(args[1], out var policy, out string policyError))
        {
          _chat.SendSystemMessage(sender, policyError);
          return;
        }

        controller.ConcurrencyConflictPolicy = policy;
        _chat.SendSystemMessage(sender, $"Scenario concurrency conflict policy set to '{policy}'.");
        return;
      }

      if (args == null || args.Length < 3 || !string.Equals(args[0], "execute", StringComparison.OrdinalIgnoreCase))
      {
        _chat.SendSystemMessage(sender, "Usage: /scenario list | /scenario execute <target> <scenario_id> | /scenario signal <signal_id> [clear] | /scenario conflictpolicy [warn|cancel|panic]");
        return;
      }

      string targetSelector = args[1];
      string scenarioId = string.Join(' ', args[2..]).Trim();
      if (string.IsNullOrWhiteSpace(scenarioId))
      {
        _chat.SendSystemMessage(sender, "Scenario identifier is required.");
        return;
      }

      if (!TryResolveTargets(sender, targetSelector, out List<NetworkConnection> targets, out string targetError))
      {
        _chat.SendSystemMessage(sender, targetError);
        return;
      }

      if (!_chat.TryDispatchScenario(scenarioId, targets, out string execError))
      {
        _chat.SendSystemMessage(sender, execError);
        return;
      }

      _chat.SendSystemMessage(sender, $"Scenario '{scenarioId}' dispatched to {targets.Count} target(s).");
    }

    private void SendScenarioList(NetworkConnection sender)
    {
      Registry.Registry.PreloadScenarioGraphsFromResources(validateWithSchema: true);

      var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var pair in Registry.Registry.GetAll<object>(RegistryType.ScenarioGraph))
      {
        if (!string.IsNullOrWhiteSpace(pair.Key))
          discovered.Add(pair.Key.Trim());

        if (pair.Value is ScenarioGraph graph && !string.IsNullOrWhiteSpace(graph.Identifier))
          discovered.Add(graph.Identifier.Trim());
      }

      if (discovered.Count == 0)
      {
        _chat.SendSystemMessage(sender, "No scenarios are available.");
        return;
      }

      var ordered = discovered
        .Where(each => !string.IsNullOrWhiteSpace(each))
        .OrderBy(each => each, StringComparer.OrdinalIgnoreCase)
        .ToList();

      _chat.SendSystemMessage(sender, $"Available scenarios ({ordered.Count}): {string.Join(", ", ordered)}");
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

  public class CommandDefinition_ProblemSheet : IChatCommandModel, IChatCommandPipelineCommand, IChatCommandUsage
  {
    public string CommandEntry => "problemsheet";
    public string Description => "Dispatch problem sheets to players.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("problemsheet list", "List available problem sheets."),
      new UsageLine("problemsheet <target> <problem> [index]", "Open a problem sheet for targets."),
      new UsageLine("  <target>", "@s, @a, @n, or fish:<id>."),
      new UsageLine("  <problem>", "Registered problem set identifier."),
      new UsageLine("  [index]", "1-based problem number; opens only that one."),
    };
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;

    public CommandDefinition_ProblemSheet(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      TryExecute(sender, args, suppressSystemMessages: false, out _, out _);
    }

    public bool TryExecute(
      NetworkConnection sender,
      string[] args,
      bool suppressSystemMessages,
      out IReadOnlyList<string> pipelineValues,
      out string error)
    {
      pipelineValues = Array.Empty<string>();
      error = string.Empty;

      if (_chat == null)
      {
        error = "Chat service is unavailable.";
        return false;
      }

      if (args != null && args.Length >= 1 && string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
      {
        SendProblemSheetList(sender, suppressSystemMessages);
        pipelineValues = new[] { "0" };
        return true;
      }

      if (args == null || args.Length < 2)
      {
        error = "Usage: /problemsheet list | /problemsheet <target> <problem-identifier> [problem-index]";
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      string playerSelector = args[0];
      int startIndex = 0;
      bool singleProblemMode = false;

      int problemIdArgEnd = args.Length;
      if (args.Length >= 3 && int.TryParse(args[^1], out int parsedIndex))
      {
        startIndex = Mathf.Max(0, parsedIndex - 1);
        singleProblemMode = true;
        problemIdArgEnd = args.Length - 1;
      }

      string problemId = string.Join(' ', args[1..problemIdArgEnd]).Trim();
      if (string.IsNullOrWhiteSpace(problemId))
      {
        error = "Problem identifier is required.";
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      if (!TryResolveTargets(sender, playerSelector, out List<NetworkConnection> targets, out string targetError))
      {
        error = targetError;
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      if (!_chat.TryDispatchProblemSheet(problemId, targets, startIndex, singleProblemMode, out string dispatchError))
      {
        error = dispatchError;
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      string modeText = singleProblemMode ? $"single problem #{startIndex + 1}" : "full set mode";
      if (!suppressSystemMessages)
        _chat.SendSystemMessage(sender, $"ProblemSheet '{problemId}' dispatched to {targets.Count} player(s) ({modeText}).");

      int resultCode = _chat.GetLastProblemSheetGradeCode(sender);
      pipelineValues = new[] { resultCode == 0 ? "0" : "1" };
      return true;
    }

    private void SendProblemSheetList(NetworkConnection sender, bool suppressSystemMessages)
    {
      var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var pair in Registry.Registry.GetAll<object>(RegistryType.ProblemSet))
      {
        if (!string.IsNullOrWhiteSpace(pair.Key))
          discovered.Add(pair.Key.Trim());

        if (pair.Value is ProblemSetDefinition set && !string.IsNullOrWhiteSpace(set.Identifier))
          discovered.Add(set.Identifier.Trim());
      }

      var resources = Resources.LoadAll<TextAsset>("Problems");
      foreach (var textAsset in resources)
      {
        if (textAsset == null)
          continue;

        if (string.Equals(textAsset.name, "problem-pack.manifest", StringComparison.OrdinalIgnoreCase))
          continue;

        if (!Registry.Registry.PreloadProblemSet(textAsset.name))
          continue;

        discovered.Add(textAsset.name);

        if (Registry.Registry.TryGetProblemSet(textAsset.name, out var set, out _) && !string.IsNullOrWhiteSpace(set?.Identifier))
          discovered.Add(set.Identifier.Trim());
      }

      if (discovered.Count == 0)
      {
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, "No problem sheets are available.");
        return;
      }

      var ordered = discovered
        .Where(each => !string.IsNullOrWhiteSpace(each))
        .OrderBy(each => each, StringComparer.OrdinalIgnoreCase)
        .ToList();

      if (!suppressSystemMessages)
        _chat.SendSystemMessage(sender, $"Available problem sheets ({ordered.Count}): {string.Join(", ", ordered)}");
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
          error = "Invalid FishNet player identifier after 'fish:'";
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
      else
      {
        error = "Unknown target selector. Use fish:, @s, @n, or @a.";
        return false;
      }

      if (targets.Count == 0)
      {
        error = "No players matched the selector.";
        return false;
      }

      targets = targets.Where(t => t != null).GroupBy(t => (int)t.ClientId).Select(g => g.First()).ToList();
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
