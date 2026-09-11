using System.Globalization;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;

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
      new UsageLine("gamerule IgnoreTagAssignFullSatisfactionOnScenarioPlay", "Show whether missing scenario player-tag gates are ignored."),
      new UsageLine("gamerule IgnoreTagAssignFullSatisfactionOnScenarioPlay <true|false>", "Ignore missing scenario player-tag gates (default true)."),
      new UsageLine("gamerule AllowMultipleRoleBranchesForSinglePlayer [true|false]", "Run duplicate role branches sequentially for each assigned player (default true)."),
      new UsageLine("gamerule UseMicInRecognitionCheck [true|false]", "Allow microphone volume for patient recognition checks (default false)."),
      new UsageLine("gamerule DisableInteractionInRecognitionCheck [true|false]", "Disable click interaction for recognition checks; microphone must be enabled."),
      new UsageLine("gamerule ShowRecognitionMicrophoneUnavailableGuidance [true|false]", "Show guidance when the recognition-check microphone is unavailable (default false)."),
      new UsageLine("gamerule DEBUG_INT_CPR_PLAYING_ESCAPE_KEY [true|false]", "Allow the local CPR performer to release animation and position lock with Left Shift (default false)."),
      new UsageLine("gamerule CareZoneMissingEquipmentFallback [wall_suction,oxyflowmeter,defibrillator|none]", "Use nearest equipment when expected CareZone equipment is absent (default defibrillator)."),
    };

    public string PermissionIdentifier => "gamerule";

    private readonly ChatService _chat;

    public CommandDefinition_Gamerule(ChatService chat) => _chat = chat;

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsOffline)
      {
        _chat.SendSystemMessage(sender, "gamerule can only be configured on the server.");
        return;
      }

      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender,
          $"Game rules:\n  runningSpeedMultiplier = {Format(PlayerController.ServerRunningSpeedMultiplier)}\n  IgnoreTagAssignFullSatisfactionOnScenarioPlay = {ScenarioGameRules.IgnoreTagAssignFullSatisfactionOnScenarioPlay}\n  AllowMultipleRoleBranchesForSinglePlayer = {ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer}\n  UseMicInRecognitionCheck = {ScenarioGameRules.UseMicInRecognitionCheck}\n  DisableInteractionInRecognitionCheck = {ScenarioGameRules.DisableInteractionInRecognitionCheck}\n  ShowRecognitionMicrophoneUnavailableGuidance = {ScenarioGameRules.ShowRecognitionMicrophoneUnavailableGuidance}\n  DEBUG_INT_CPR_PLAYING_ESCAPE_KEY = {ScenarioGameRules.DEBUG_INT_CPR_PLAYING_ESCAPE_KEY}\n  CareZoneMissingEquipmentFallback = {ScenarioGameRules.FormatMissingCareZoneEquipmentFallback()}");
        return;
      }

      if (string.Equals(args[0], "IgnoreTagAssignFullSatisfactionOnScenarioPlay", System.StringComparison.OrdinalIgnoreCase))
      {
        if (args.Length == 1)
        {
          _chat.SendSystemMessage(sender,
            $"IgnoreTagAssignFullSatisfactionOnScenarioPlay = {ScenarioGameRules.IgnoreTagAssignFullSatisfactionOnScenarioPlay}");
          return;
        }

        if (args.Length != 2 || !bool.TryParse(args[1], out var enabled))
        {
          _chat.SendSystemMessage(sender, "IgnoreTagAssignFullSatisfactionOnScenarioPlay must be true or false.");
          return;
        }

        ScenarioGameRules.IgnoreTagAssignFullSatisfactionOnScenarioPlay = enabled;
        _chat.SendSystemMessage(sender, $"Set IgnoreTagAssignFullSatisfactionOnScenarioPlay to {enabled}.");
        return;
      }

      if (string.Equals(args[0], "AllowMultipleRoleBranchesForSinglePlayer", System.StringComparison.OrdinalIgnoreCase))
      {
        if (args.Length == 1)
        {
          _chat.SendSystemMessage(sender,
            $"AllowMultipleRoleBranchesForSinglePlayer = {ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer}");
          return;
        }

        if (args.Length != 2 || !bool.TryParse(args[1], out var enabled))
        {
          _chat.SendSystemMessage(sender, "AllowMultipleRoleBranchesForSinglePlayer must be true or false.");
          return;
        }

        ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer = enabled;
        _chat.SendSystemMessage(sender, $"Set AllowMultipleRoleBranchesForSinglePlayer to {enabled}.");
        return;
      }

      if (string.Equals(args[0], "UseMicInRecognitionCheck", System.StringComparison.OrdinalIgnoreCase))
      {
        HandleRecognitionRule(sender, args, "UseMicInRecognitionCheck",
          ScenarioGameRules.UseMicInRecognitionCheck,
          ScenarioGameRules.TrySetUseMicInRecognitionCheck);
        return;
      }

      if (string.Equals(args[0], "DisableInteractionInRecognitionCheck", System.StringComparison.OrdinalIgnoreCase))
      {
        HandleRecognitionRule(sender, args, "DisableInteractionInRecognitionCheck",
          ScenarioGameRules.DisableInteractionInRecognitionCheck,
          ScenarioGameRules.TrySetDisableInteractionInRecognitionCheck);
        return;
      }

      if (string.Equals(args[0], "ShowRecognitionMicrophoneUnavailableGuidance", System.StringComparison.OrdinalIgnoreCase))
      {
        if (args.Length == 1)
        {
          _chat.SendSystemMessage(sender,
            $"ShowRecognitionMicrophoneUnavailableGuidance = {ScenarioGameRules.ShowRecognitionMicrophoneUnavailableGuidance}");
          return;
        }

        if (args.Length != 2 || !bool.TryParse(args[1], out var enabled))
        {
          _chat.SendSystemMessage(sender, "ShowRecognitionMicrophoneUnavailableGuidance must be true or false.");
          return;
        }

        if (!_chat.TrySetRecognitionMicrophoneUnavailableGuidanceServer(enabled))
        {
          _chat.SendSystemMessage(sender,
            "ShowRecognitionMicrophoneUnavailableGuidance could not be synchronized.");
          return;
        }

        _chat.SendSystemMessage(sender, $"Set ShowRecognitionMicrophoneUnavailableGuidance to {enabled}.");
        return;
      }

      if (string.Equals(args[0], "DEBUG_INT_CPR_PLAYING_ESCAPE_KEY", System.StringComparison.OrdinalIgnoreCase))
      {
        if (args.Length == 1)
        {
          _chat.SendSystemMessage(sender,
            $"DEBUG_INT_CPR_PLAYING_ESCAPE_KEY = {ScenarioGameRules.DEBUG_INT_CPR_PLAYING_ESCAPE_KEY}");
          return;
        }

        if (args.Length != 2 || !bool.TryParse(args[1], out var enabled))
        {
          _chat.SendSystemMessage(sender, "DEBUG_INT_CPR_PLAYING_ESCAPE_KEY must be true or false.");
          return;
        }

        if (!_chat.TrySetDebugIntCprPlayingEscapeKeyServer(enabled))
        {
          _chat.SendSystemMessage(sender, "DEBUG_INT_CPR_PLAYING_ESCAPE_KEY could not be synchronized.");
          return;
        }

        _chat.SendSystemMessage(sender, $"Set DEBUG_INT_CPR_PLAYING_ESCAPE_KEY to {enabled}.");
        return;
      }

      if (string.Equals(args[0], "CareZoneMissingEquipmentFallback", System.StringComparison.OrdinalIgnoreCase))
      {
        if (args.Length == 1)
        {
          _chat.SendSystemMessage(sender,
            $"CareZoneMissingEquipmentFallback = {ScenarioGameRules.FormatMissingCareZoneEquipmentFallback()}");
          return;
        }

        string error = null;
        if (args.Length != 2 || !ScenarioGameRules.TrySetMissingCareZoneEquipmentFallback(args[1], out error))
        {
          _chat.SendSystemMessage(sender, error ?? "CareZoneMissingEquipmentFallback accepts wall_suction, oxyflowmeter, defibrillator, or none.");
          return;
        }

        _chat.SendSystemMessage(sender,
          $"Set CareZoneMissingEquipmentFallback to {ScenarioGameRules.FormatMissingCareZoneEquipmentFallback()}.");
        return;
      }

      if (!string.Equals(args[0], "runningSpeedMultiplier", System.StringComparison.OrdinalIgnoreCase))
      {
        _chat.SendSystemMessage(sender, $"Unknown game rule '{args[0]}'. Use /gamerule to list available rules.");
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

    private delegate bool RecognitionRuleSetter(bool value, out string error);

    private void HandleRecognitionRule(
      NetworkConnection sender,
      string[] args,
      string ruleName,
      bool currentValue,
      RecognitionRuleSetter setter)
    {
      if (args.Length == 1)
      {
        _chat.SendSystemMessage(sender, $"{ruleName} = {currentValue}");
        return;
      }

      if (args.Length != 2 || !bool.TryParse(args[1], out var value))
      {
        _chat.SendSystemMessage(sender, $"{ruleName} must be true or false.");
        return;
      }

      if (!setter(value, out var error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"Set {ruleName} to {value}.");
    }
  }
}
