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
      new UsageLine("scenario exe <target> <scenario>", "Alias for scenario execute."),
      new UsageLine("scenario exec <target> <scenario>", "Alias for scenario execute."),
      new UsageLine("scenario signal <signal> [clear]", "Raise (or clear) a signal."),
      new UsageLine("scenario enter <entrypoint>", "Skip playback to a ManualEntrypoint node."),
      new UsageLine("scenario enter <entrypoint> [clear-state=true|clear-state=false]", "Skip, wiping (default) or keeping prior scenario state."),
      new UsageLine("scenario end", "End the active scenario and clean up its tracked changes."),
      new UsageLine("scenario restart [entrypoint]", "Clean up and restart the active scenario."),
      new UsageLine("scenario conflictpolicy [warn|cancel|panic]", "Get/set concurrent-dialogue conflict policy."),
      new UsageLine("scenario validatorlog", "Show Validator block logging targets."),
      new UsageLine("scenario validatorlog <console|chat|session> <on|off>", "Enable or disable a logging target."),
      new UsageLine("  <target>", "@s, @a, @n, or fish:<id>."),
      new UsageLine("  <scenario>", "Registered scenario identifier."),
    };
    public string PermissionIdentifier => "scenario";

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

      // /scenario enter <entrypoint> [clear-state=true|false]
      // 재생 위치를 시나리오에 선언된 ManualEntrypoint 지점으로 건너뛴다.
      if (args != null
          && args.Length >= 1
          && string.Equals(args[0], "enter", StringComparison.OrdinalIgnoreCase))
      {
        ExecuteEnterCommand(sender, args);
        return;
      }

      if (args != null && args.Length >= 1
          && (string.Equals(args[0], "end", StringComparison.OrdinalIgnoreCase)
              || string.Equals(args[0], "restart", StringComparison.OrdinalIgnoreCase)))
      {
        var controller = ScenarioController.Instance;
        if (controller == null || !controller.HasActiveScenario)
        {
          _chat.SendSystemMessage(sender, "No scenario is currently playing.");
          return;
        }

        if (string.Equals(args[0], "end", StringComparison.OrdinalIgnoreCase))
        {
          bool authoritative = controller.IsAuthoritativeExecutor;
          controller.EndScenario();
          if (!authoritative)
            ScenarioNetworkRelay.BroadcastScenarioEnd();
          _chat.SendSystemMessage(sender, "Scenario ended and cleanup completed.");
          return;
        }

        string entrypoint = args.Length >= 2 ? args[1].Trim() : null;
        bool restartAuthoritative = controller.IsAuthoritativeExecutor;
        if (!controller.RestartScenario(entrypoint))
        {
          _chat.SendSystemMessage(sender, "Scenario restart is unavailable on this peer.");
          return;
        }
        if (!restartAuthoritative)
          ScenarioNetworkRelay.BroadcastScenarioRestart(entrypoint);
        _chat.SendSystemMessage(sender, "Scenario restarted after cleanup.");
        return;
      }

      if (args != null
          && args.Length >= 1
          && string.Equals(args[0], "validatorlog", StringComparison.OrdinalIgnoreCase))
      {
        ExecuteValidatorLogCommand(sender, args);
        return;
      }

      if (args == null || args.Length < 3
          || (!string.Equals(args[0], "execute", StringComparison.OrdinalIgnoreCase)
              && !string.Equals(args[0], "exe", StringComparison.OrdinalIgnoreCase)
              && !string.Equals(args[0], "exec", StringComparison.OrdinalIgnoreCase)))
      {
        _chat.SendSystemMessage(sender, "Usage: /scenario list | /scenario execute|exe|exec <target> <scenario_id> | /scenario enter <entrypoint> [clear-state=true|false] | /scenario end | /scenario restart [entrypoint] | /scenario signal <signal_id> [clear] | /scenario conflictpolicy [warn|cancel|panic] | /scenario validatorlog [<console|chat|session> <on|off>]");
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

    private void ExecuteEnterCommand(NetworkConnection sender, string[] args)
    {
      var controller = ScenarioController.Instance;
      if (controller == null)
      {
        _chat.SendSystemMessage(sender, "ScenarioController instance is not available.");
        return;
      }

      if (args.Length < 2)
      {
        SendManualEntrypointList(sender, controller);
        return;
      }

      string entrypointId = args[1].Trim();
      if (string.IsNullOrWhiteSpace(entrypointId))
      {
        _chat.SendSystemMessage(sender, "Usage: /scenario enter <entrypoint> [clear-state=true|false]");
        return;
      }

      bool clearState = true;
      for (int i = 2; i < args.Length; i++)
      {
        if (!TryParseClearStateOption(args[i], out clearState))
        {
          _chat.SendSystemMessage(sender,
            $"Unknown option '{args[i]}'. Usage: /scenario enter <entrypoint> [clear-state=true|false]");
          return;
        }
      }

      // 서버 권위 실행이면 서버 커서 하나만 옮기면 되고 나머지 피어는 표시로 따라온다.
      // 호환 실행 경로에서는 대상 클라이언트마다 독립 상태기가 돌기 때문에, 서버만 옮기면
      // 원격 플레이어는 스킵 이전 위치에 그대로 남는다. 이때는 모든 피어에 함께 알린다.
      bool authoritative = controller.IsAuthoritativeExecutor;
      bool localEntered = controller.TryEnterManualEntrypoint(entrypointId, clearState, out string enterError);
      bool broadcast = !authoritative && ScenarioNetworkRelay.BroadcastManualEntry(entrypointId, clearState);

      if (!localEntered && !broadcast)
      {
        _chat.SendSystemMessage(sender, enterError);
        return;
      }

      string scope = authoritative
        ? "authoritative"
        : broadcast ? "all peers" : "this peer";
      _chat.SendSystemMessage(sender,
        $"Entered manual entrypoint '{entrypointId}' (clear-state={(clearState ? "true" : "false")}, scope={scope}).");

      // 서버가 그래프를 들고 있지 않으면(전용 서버 등) 별칭을 검증할 방법이 없다.
      // 오타가 조용히 묻히지 않도록 로컬 실패 사유를 함께 알린다.
      if (!localEntered)
        _chat.SendSystemMessage(sender, $"Note: this peer could not verify the entrypoint ({enterError})");
    }

    private void SendManualEntrypointList(NetworkConnection sender, ScenarioController controller)
    {
      if (!controller.HasActiveScenario)
      {
        _chat.SendSystemMessage(sender, "No scenario is currently playing.");
        return;
      }

      var entrypoints = controller.GetManualEntrypointIdentifiers();
      if (entrypoints.Count == 0)
      {
        _chat.SendSystemMessage(sender,
          $"Scenario '{controller.CurrentGraph?.Identifier}' declares no manual entrypoint.");
        return;
      }

      var ordered = entrypoints
        .OrderBy(each => each, StringComparer.OrdinalIgnoreCase)
        .ToList();
      _chat.SendSystemMessage(sender,
        $"Manual entrypoints ({ordered.Count}): {string.Join(", ", ordered)}");
    }

    /// <summary>
    /// <c>clear-state=true</c> 같은 키=값 형태와 <c>true</c> 단독 표기를 함께 받는다.
    /// 값을 생략하면(<c>clear-state</c>) true 로 본다.
    /// </summary>
    private static bool TryParseClearStateOption(string raw, out bool clearState)
    {
      clearState = true;
      if (string.IsNullOrWhiteSpace(raw))
        return false;

      string token = raw.Trim();
      int separator = token.IndexOf('=');
      string key = separator < 0 ? null : token.Substring(0, separator).Trim();
      string value = separator < 0 ? token : token.Substring(separator + 1).Trim();

      if (key != null)
      {
        string normalizedKey = key.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        if (normalizedKey != "clearstate")
          return false;

        if (string.IsNullOrWhiteSpace(value))
          return true;
      }

      switch (value.ToLowerInvariant())
      {
        case "true":
        case "on":
        case "yes":
        case "1":
          clearState = true;
          return true;
        case "false":
        case "off":
        case "no":
        case "0":
          clearState = false;
          return true;
        default:
          return false;
      }
    }

    private void ExecuteValidatorLogCommand(NetworkConnection sender, string[] args)
    {
      var controller = ScenarioController.Instance;
      if (controller == null)
      {
        _chat.SendSystemMessage(sender, "ScenarioController instance is not available.");
        return;
      }

      if (args.Length == 1)
      {
        var targets = controller.ValidatorBlockLogTargets;
        _chat.SendSystemMessage(sender,
          $"Validator block logging: console={FormatFlag(targets, ScenarioValidatorBlockLogTarget.UnityConsole)}, "
          + $"chat={FormatFlag(targets, ScenarioValidatorBlockLogTarget.InGameChat)}, "
          + $"session={FormatFlag(targets, ScenarioValidatorBlockLogTarget.SessionLog)}.");
        return;
      }

      if (args.Length != 3
          || !TryParseValidatorLogTarget(args[1], out var target)
          || !TryParseToggle(args[2], out bool enabled))
      {
        _chat.SendSystemMessage(sender,
          "Usage: /scenario validatorlog <console|chat|session> <on|off>");
        return;
      }

      var updatedTargets = enabled
        ? controller.ValidatorBlockLogTargets | target
        : controller.ValidatorBlockLogTargets & ~target;

      if (!_chat.TrySetValidatorBlockLogTargets(updatedTargets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender,
        $"Validator block logging target '{args[1].ToLowerInvariant()}' set to {(enabled ? "on" : "off")}.");
    }

    private static bool TryParseValidatorLogTarget(string value, out ScenarioValidatorBlockLogTarget target)
    {
      switch (value?.Trim().ToLowerInvariant())
      {
        case "console":
          target = ScenarioValidatorBlockLogTarget.UnityConsole;
          return true;
        case "chat":
          target = ScenarioValidatorBlockLogTarget.InGameChat;
          return true;
        case "session":
          target = ScenarioValidatorBlockLogTarget.SessionLog;
          return true;
        default:
          target = ScenarioValidatorBlockLogTarget.None;
          return false;
      }
    }

    private static bool TryParseToggle(string value, out bool enabled)
    {
      switch (value?.Trim().ToLowerInvariant())
      {
        case "on":
          enabled = true;
          return true;
        case "off":
          enabled = false;
          return true;
        default:
          enabled = false;
          return false;
      }
    }

    private static string FormatFlag(
      ScenarioValidatorBlockLogTarget targets,
      ScenarioValidatorBlockLogTarget target)
    {
      return (targets & target) != 0 ? "on" : "off";
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

      if (raw.StartsWith('@'))
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

  public sealed class CommandDefinition_ScenarioAlias : IChatCommandModel, IChatCommandUsage
  {
    private readonly CommandDefinition_Scenario _scenario;

    public CommandDefinition_ScenarioAlias(CommandDefinition_Scenario scenario)
    {
      _scenario = scenario;
    }

    public string CommandEntry => "scen";
    public string Description => "Alias for /scenario.";
    public string PermissionIdentifier => "scenario";
    public IReadOnlyList<UsageLine> UsageLines => _scenario.UsageLines
      .Select(line => line.Syntax.StartsWith("scenario", StringComparison.OrdinalIgnoreCase)
        ? new UsageLine("scen" + line.Syntax.Substring("scenario".Length), line.Description)
        : line)
      .ToArray();

    public void Execute(NetworkConnection sender, string[] args)
    {
      _scenario.Execute(sender, args);
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
    public string PermissionIdentifier => "problemsheet";

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
