using System;
using System.Collections.Generic;
using System.Globalization;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 시간 표시(스톱워치/카운트다운)의 주기적 재동기화 밀도를 조정하는 명령어.
  ///
  /// 밀도는 tick / ms / seconds 단위로 지정하며, 기본값은 1초에 1회이다.
  /// 이 설정은 서버 권위이므로 서버(호스트/콘솔)에서만 조정할 수 있다.
  /// (모든 명령 실행은 서버에서 수행되며, PermissionService 권한으로 접근을 제한한다.)
  /// </summary>
  public class CommandDefinition_TimeSync : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "timesync";
    public string Description => "Configure scenario time-display re-sync density (server only).";
    public IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("timesync", "Show the current re-sync density."),
      new UsageLine("timesync tick <count>", "Re-sync every <count> network ticks."),
      new UsageLine("timesync ms <milliseconds>", "Re-sync every <milliseconds> ms."),
      new UsageLine("timesync seconds <seconds>", "Re-sync every <seconds> seconds."),
    };

    // 서버(호스트/콘솔)에서만 조정 가능하게 한다.
    public string PermissionIdentifier => "timesync";

    private readonly ChatService _chat;

    public CommandDefinition_TimeSync(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      // 설정은 서버 권위이다. 명령 실행 자체가 서버에서 이뤄지지만, 방어적으로 한 번 더 확인한다.
      if (!InstanceFinder.IsServerStarted)
      {
        _chat.SendSystemMessage(sender, "timesync can only be configured on the server.");
        return;
      }

      // 인자가 없으면 현재 설정을 보여준다.
      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, $"Time re-sync density: {ScenarioTimeSyncSettings.Describe()}");
        return;
      }

      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
        return;
      }

      if (!TryParseUnit(args[0], out ScenarioTimeUnit unit))
      {
        _chat.SendSystemMessage(sender, $"Unknown unit '{args[0]}'. Use tick, ms, or seconds.");
        return;
      }

      if (!double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
          || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
      {
        _chat.SendSystemMessage(sender, "Value must be a positive number.");
        return;
      }

      if (!ScenarioTimeSyncSettings.Configure(value, unit))
      {
        _chat.SendSystemMessage(sender, "Failed to apply time-sync density (invalid value).");
        return;
      }

      _chat.SendSystemMessage(sender, $"Time re-sync density set to {ScenarioTimeSyncSettings.Describe()}.");
    }

    private static bool TryParseUnit(string raw, out ScenarioTimeUnit unit)
    {
      unit = ScenarioTimeUnit.Seconds;
      if (string.IsNullOrWhiteSpace(raw))
        return false;

      switch (raw.Trim().ToLowerInvariant())
      {
        case "tick":
        case "ticks":
          unit = ScenarioTimeUnit.Tick;
          return true;
        case "ms":
        case "milli":
        case "millis":
        case "milliseconds":
          unit = ScenarioTimeUnit.Milliseconds;
          return true;
        case "s":
        case "sec":
        case "secs":
        case "second":
        case "seconds":
          unit = ScenarioTimeUnit.Seconds;
          return true;
        default:
          return false;
      }
    }
  }
}
