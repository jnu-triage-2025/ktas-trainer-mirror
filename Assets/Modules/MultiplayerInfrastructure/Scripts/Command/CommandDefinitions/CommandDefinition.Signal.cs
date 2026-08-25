using System;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>시나리오 신호의 JSON 파라미터 값을 조회·발신·초기화하는 운영 명령.</summary>
  public sealed class CommandDefinition_Signal : IChatCommandModel, IChatCommandUsage
  {
    private const int MaxListEntries = 20;
    private const int MaxListResponseCharacters = 6000;
    public string CommandEntry => "signal";
    public string Description => "Inspect and manage scenario signal JSON parameters.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("signal raise <identifier> [json]", "Raise a signal with an optional JSON parameter."),
      new UsageLine("signal get <identifier>", "Show the latest value across all players."),
      new UsageLine("signal player <player> <identifier>", "Show one player's latest value."),
      new UsageLine("signal list [identifier]", "List stored values."),
      new UsageLine("signal flush", "Clear all stored signal parameter values."),
    };
    public string PermissionIdentifier => "scenario";

    private readonly ChatService _chat;

    public CommandDefinition_Signal(ChatService chat) => _chat = chat;

    public void Execute(NetworkConnection sender, string[] args)
    {
      string subcommand = args != null && args.Length > 0 ? args[0].Trim().ToLowerInvariant() : string.Empty;
      switch (subcommand)
      {
        case "raise":
          Raise(sender, args);
          break;
        case "get":
          Get(sender, args);
          break;
        case "player":
          GetPlayer(sender, args);
          break;
        case "list":
          List(sender, args);
          break;
        case "flush":
          Flush(sender);
          break;
        default:
          Send(sender, "Usage: /signal raise <identifier> [json] | /signal get <identifier> | /signal player <player> <identifier> | /signal list [identifier] | /signal flush");
          break;
      }
    }

    private void Raise(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
      {
        Send(sender, "Usage: /signal raise <identifier> [json]");
        return;
      }

      string identifier = args[1].Trim();
      string parameterJson = args.Length > 2 ? string.Join(' ', args[2..]).Trim() : null;
      if (!ScenarioSignalParameterStore.TryValidateJson(parameterJson, out _))
      {
        Send(sender, $"시그널 ({ScenarioInteractionSignals.Normalize(identifier)})의 매개변수 ({parameterJson})는 올바른 JSON 형식이 아닙니다.");
        return;
      }

      string playerIdentifier = ScenarioSignalParameterStore.ServerPlayerIdentifier;
      string playerDisplayName = playerIdentifier;
      if (sender != null && UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor))
      {
        playerIdentifier = descriptor.Identifier;
        playerDisplayName = descriptor.DisplayName;
      }
      bool parameterStored = ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(ScenarioInteractionSignals.Normalize(identifier), parameterJson,
        playerIdentifier, playerDisplayName, sender);
      if (parameterStored)
        Send(sender, $"Scenario signal '{ScenarioInteractionSignals.Normalize(identifier)}' raised with parameter {parameterJson ?? "(none)"}.");
      else if (ScenarioInteractionSignals.IsRaised(identifier))
        Send(sender, $"Scenario signal '{ScenarioInteractionSignals.Normalize(identifier)}' was raised, but its parameter value was not stored.");
      else
        Send(sender, $"Scenario signal '{ScenarioInteractionSignals.Normalize(identifier)}' was rejected.");
    }

    private void Get(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
      {
        Send(sender, "Usage: /signal get <identifier>");
        return;
      }
      if (!ScenarioSignalParameterStore.TryGetLatest(args[1], out var value))
      {
        LogQuery(sender, "get", ScenarioInteractionSignals.Normalize(args[1]), "not-found");
        Send(sender, $"No stored parameter value for '{ScenarioInteractionSignals.Normalize(args[1])}'.");
        return;
      }
      LogQuery(sender, "get", value.SignalIdentifier, Format(value));
      Send(sender, Format(value));
    }

    private void GetPlayer(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length != 3)
      {
        Send(sender, "Usage: /signal player <player> <identifier>");
        return;
      }
      if (!TryResolvePlayer(args[1], out var playerIdentifier, out string error))
      {
        Send(sender, error);
        return;
      }
      if (!ScenarioSignalParameterStore.TryGetForPlayer(args[2], playerIdentifier, out var value))
      {
        LogQuery(sender, "player", ScenarioInteractionSignals.Normalize(args[2]), $"player={playerIdentifier}, not-found");
        Send(sender, $"No stored parameter value for player '{args[1]}' and signal '{ScenarioInteractionSignals.Normalize(args[2])}'.");
        return;
      }
      LogQuery(sender, "player", value.SignalIdentifier, Format(value));
      Send(sender, Format(value));
    }

    private void List(NetworkConnection sender, string[] args)
    {
      if (args != null && args.Length > 2)
      {
        Send(sender, "Usage: /signal list [identifier]");
        return;
      }
      string identifier = args != null && args.Length == 2 ? args[1] : null;
      var allValues = ScenarioSignalParameterStore.GetAll(identifier).ToArray();
      var values = allValues.Take(MaxListEntries).ToArray();
      if (values.Length == 0)
      {
        LogQuery(sender, "list", identifier == null ? "*" : ScenarioInteractionSignals.Normalize(identifier), "count=0");
        Send(sender, "No stored scenario signal parameter values.");
        return;
      }
      var lines = new System.Collections.Generic.List<string>();
      int responseLength = 0;
      bool truncated = allValues.Length > values.Length;
      foreach (ScenarioSignalParameter value in values)
      {
        string line = Format(value);
        int addedLength = line.Length + (lines.Count > 0 ? 1 : 0);
        if (responseLength + addedLength > MaxListResponseCharacters)
        {
          truncated = true;
          break;
        }
        lines.Add(line);
        responseLength += addedLength;
      }
      LogQuery(sender, "list", identifier == null ? "*" : ScenarioInteractionSignals.Normalize(identifier),
        $"available={allValues.Length}, returned={lines.Count}, truncated={truncated}");
      string suffix = truncated ? "\n… output truncated; narrow by signal or player." : string.Empty;
      Send(sender, string.Join('\n', lines) + suffix);
    }

    private void Flush(NetworkConnection sender)
    {
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      Send(sender, "Scenario signal parameter values flushed.");
    }

    private static bool TryResolvePlayer(string selector, out string playerIdentifier, out string error)
    {
      playerIdentifier = null;
      error = string.Empty;
      if (UserDescriptorService.TryGetByIdentifier(selector, out var byIdentifier)
          || UserDescriptorService.TryGetByDisplayName(selector, out byIdentifier))
      {
        playerIdentifier = byIdentifier.Identifier;
        return true;
      }
      error = $"Player '{selector}' was not found.";
      return false;
    }

    private static string Format(ScenarioSignalParameter value)
      => $"signal={value.SignalIdentifier}, player={value.PlayerDisplayName} ({value.PlayerIdentifier}), parameter={value.DisplayParameter}, sequence={value.Sequence}";

    private static void LogQuery(NetworkConnection sender, string operation, string identifier, string result)
    {
      string player = ScenarioSignalParameterStore.ServerPlayerIdentifier;
      if (sender != null && UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor))
        player = $"{descriptor.DisplayName} ({descriptor.Identifier})";
      GameLogService.WriteSignal(
        $"Signal parameter query: operation={operation}, requester={ScenarioSignalParameterStore.FormatForLog(player)}, "
        + $"identifier={ScenarioSignalParameterStore.FormatForLog(identifier)}, result={ScenarioSignalParameterStore.FormatForLog(result)}",
        ScenarioSignalParameterStore.FormatForLog(identifier));
    }

    private void Send(NetworkConnection sender, string message) => _chat?.SendSystemMessage(sender, message);
  }
}
