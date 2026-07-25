using System.Globalization;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Speed : IChatCommandModel, IChatCommandUsage
  {
    // 기본 걷기 속도(7.5) 대비 충분히 빠르되, CharacterController 관통이 일어나지 않는 수준의 상한.
    private const float MaxSpeed = 100f;

    public string CommandEntry => "speed";
    public string Description => "Change your walking speed.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("speed default", "Reset walking speed to the default value."),
      new UsageLine("speed <number>", $"Set walking speed to the given number (0~{FormatSpeed(MaxSpeed)}, e.g. 7.5)."),
    };

    public string PermissionIdentifier => "speed";
    public bool RequiresAdmin => false;

    private readonly ChatService _manager;

    public CommandDefinition_Speed(ChatService manager)
    {
      _manager = manager;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (sender == null || sender.FirstObject == null || !sender.FirstObject.TryGetComponent(out PlayerController controller))
      {
        _manager.SendSystemMessage(sender, "Unable to locate your target.");
        return;
      }

      if (args.Length == 0)
      {
        _manager.SendSystemMessage(sender,
          $"Current walking speed: {FormatSpeed(controller.WalkingSpeed)} (default: {FormatSpeed(controller.DefaultWalkingSpeed)}). " +
          "Usage: /speed <default|number>");
        return;
      }

      if (string.Equals(args[0], "default", System.StringComparison.OrdinalIgnoreCase))
      {
        float defaultValue = controller.DefaultWalkingSpeed;
        controller.ApplyWalkingSpeedServer(defaultValue);
        _manager.SendSystemMessage(sender, $"Reset walking speed to default ({FormatSpeed(defaultValue)}).");
        return;
      }

      if (!TryParseSpeed(args[0], out float value, out string parseError))
      {
        _manager.SendSystemMessage(sender, parseError);
        return;
      }

      controller.ApplyWalkingSpeedServer(value);
      _manager.SendSystemMessage(sender, $"Set walking speed to {FormatSpeed(value)}.");
    }

    private static bool TryParseSpeed(string raw, out float value, out string error)
    {
      error = string.Empty;
      value = 0f;

      if (string.IsNullOrWhiteSpace(raw))
      {
        error = "Usage: /speed <default|number>";
        return false;
      }

      if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
      {
        error = $"'{raw}' is not a valid number.";
        return false;
      }

      // float.TryParse 는 "NaN"/"Infinity" 문자열도 파싱에 성공하며,
      // NaN은 모든 비교 연산이 false라 아래 범위 검사를 그대로 통과한다.
      // 이 값이 이동 계산에 들어가면 CharacterController.Move 가 NaN/∞ 벡터를
      // 받아 플레이어 이동이 완전히 깨지므로 명시적으로 거부한다.
      if (float.IsNaN(value) || float.IsInfinity(value))
      {
        error = "Speed must be a finite number.";
        return false;
      }

      if (value < 0f || value > MaxSpeed)
      {
        error = $"Speed must be between 0 and {FormatSpeed(MaxSpeed)}.";
        return false;
      }

      return true;
    }

    private static string FormatSpeed(float value)
      => value.ToString("0.##", CultureInfo.InvariantCulture);
  }
}
