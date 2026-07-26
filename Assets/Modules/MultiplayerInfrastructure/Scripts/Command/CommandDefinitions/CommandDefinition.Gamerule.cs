using System.Globalization;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>서버 공통 게임 규칙을 조회하거나 변경한다.</summary>
  public class CommandDefinition_Gamerule : IChatCommandModel, IChatCommandUsage
  {
    private const float MinRunningSpeedMultiplier = 0f;
    private const float MaxRunningSpeedMultiplier = 10f;

    public string CommandEntry => "gamerule";
    public string Description => "View or change server game rules.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("gamerule", "Show available game rules."),
      new UsageLine("gamerule runningSpeedMultiplier", "Show the current running speed multiplier."),
      new UsageLine("gamerule runningSpeedMultiplier <number>", "Set running speed multiplier (0~10; default 1.5)."),
    };

    public string PermissionIdentifier => "gamerule";
    public bool RequiresAdmin => true;

    private readonly ChatService _chat;

    public CommandDefinition_Gamerule(ChatService chat) => _chat = chat;

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (!InstanceFinder.IsServerStarted)
      {
        _chat.SendSystemMessage(sender, "gamerule can only be configured on the server.");
        return;
      }

      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender,
          $"Game rules:\n  runningSpeedMultiplier = {Format(PlayerController.ServerRunningSpeedMultiplier)}");
        return;
      }

      if (!string.Equals(args[0], "runningSpeedMultiplier", System.StringComparison.OrdinalIgnoreCase))
      {
        _chat.SendSystemMessage(sender, $"Unknown game rule '{args[0]}'. Use runningSpeedMultiplier.");
        return;
      }

      if (args.Length == 1)
      {
        _chat.SendSystemMessage(sender,
          $"runningSpeedMultiplier = {Format(PlayerController.ServerRunningSpeedMultiplier)}");
        return;
      }

      if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
          || float.IsNaN(value) || float.IsInfinity(value)
          || value < MinRunningSpeedMultiplier || value > MaxRunningSpeedMultiplier)
      {
        _chat.SendSystemMessage(sender,
          $"runningSpeedMultiplier must be a finite number between {Format(MinRunningSpeedMultiplier)} and {Format(MaxRunningSpeedMultiplier)}.");
        return;
      }

      PlayerController.ApplyRunningSpeedMultiplierServer(value);
      _chat.SendSystemMessage(sender, $"Set runningSpeedMultiplier to {Format(value)}.");
    }

    private static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
  }
}
