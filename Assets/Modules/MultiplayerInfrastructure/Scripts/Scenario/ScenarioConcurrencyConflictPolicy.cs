using System;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 두 개 이상의 시나리오 흐름이 동시에 대화창 계열 UI(Dialogue/Choice/Quiz)를
  /// 점유하려 할 때의 처리 정책.
  /// </summary>
  public enum ScenarioConcurrencyConflictPolicy
  {
    /// <summary>경고만 남기고 그대로 진행한다(undefined behavior 허용). 기본값이자 기존 동작.</summary>
    Warn,

    /// <summary>나중에 대화창을 점유하려 한 흐름을 취소한다(먼저 점유한 흐름 보존).</summary>
    Cancel,

    /// <summary>진행 중인 모든 시나리오를 중단한다(EndScenario).</summary>
    Panic,
  }

  public static class ScenarioConcurrencyConflictPolicyExtensions
  {
    /// <summary>
    /// 커맨드/직렬화 문자열을 정책 값으로 파싱한다(숫자 별칭 0/1/2 허용).
    /// 실패 시 false 를 반환하고 <paramref name="error"/> 에 사유를 담는다.
    /// </summary>
    public static bool TryParse(string raw, out ScenarioConcurrencyConflictPolicy value, out string error)
    {
      value = ScenarioConcurrencyConflictPolicy.Warn;
      error = null;

      if (string.IsNullOrWhiteSpace(raw))
      {
        error = "정책 값이 비어 있습니다. 사용 가능: warn|cancel|panic.";
        return false;
      }

      switch (raw.Trim().ToLowerInvariant())
      {
        case "warn":
        case "0":
          value = ScenarioConcurrencyConflictPolicy.Warn;
          return true;
        case "cancel":
        case "1":
          value = ScenarioConcurrencyConflictPolicy.Cancel;
          return true;
        case "panic":
        case "2":
          value = ScenarioConcurrencyConflictPolicy.Panic;
          return true;
        default:
          error = $"알 수 없는 정책 '{raw}'. 사용 가능: warn|cancel|panic.";
          return false;
      }
    }
  }
}
