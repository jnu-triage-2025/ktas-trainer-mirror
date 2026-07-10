using System;
using FishNet;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시간 동기화 밀도(주기)의 단위.
  /// </summary>
  public enum ScenarioTimeSyncUnit
  {
    /// <summary>네트워크 tick 단위(FishNet <c>TimeManager.Tick</c> 기준).</summary>
    Tick,

    /// <summary>밀리초 단위.</summary>
    Milliseconds,

    /// <summary>초 단위.</summary>
    Seconds
  }

  /// <summary>
  /// 시간 표시(스톱워치/카운트다운)의 주기적 재동기화 밀도를 담는 서버 측 설정.
  ///
  /// 설계:
  /// - 타이머 값은 연산 시점(Create/Start/Show 등)에만 전파되고 그 후엔 각 클라이언트가 로컬로 tick 하므로
  ///   시간이 지날수록 드리프트가 누적될 수 있다. 서버는 이 설정에 따라 "현재 표시 중인 타이머"의
  ///   권위 값을 주기적으로 재전파하여 모든 클라이언트를 다시 맞춘다(<see cref="ScenarioTimeRelay"/>).
  /// - 밀도는 tick / milliseconds / seconds 세 단위로 지정할 수 있으며, 기본값은 1초에 1회이다.
  /// - 이 설정은 서버 권위이며, 조정은 오직 명령어(<c>timesync</c>)로만 수행한다(런타임 UI/인스펙터 노출 없음).
  ///
  /// 내부적으로는 원본 값+단위를 보존(명령 에코/조회용)하되, 실제 재전파 간격 판정은
  /// <see cref="GetIntervalTicks"/> 가 반환하는 tick 수로 수행한다.
  /// </summary>
  public static class ScenarioTimeSyncSettings
  {
    /// <summary>설정이 바뀔 때 발생한다(재전파 카운터 재설정 등에 사용).</summary>
    public static event Action Changed;

    /// <summary>tick 단위 폴백 간격. TimeManager 가 없을 때(오프라인) 사용하는 근사 tickRate(30) 기준 1초.</summary>
    private const uint FallbackTicksPerSecond = 30u;

    private static double _value = 1d;
    private static ScenarioTimeSyncUnit _unit = ScenarioTimeSyncUnit.Seconds;

    /// <summary>현재 설정된 원본 값(단위는 <see cref="Unit"/>).</summary>
    public static double Value => _value;

    /// <summary>현재 설정된 단위.</summary>
    public static ScenarioTimeSyncUnit Unit => _unit;

    /// <summary>
    /// 재동기화 밀도를 설정한다. 값은 양수여야 한다.
    /// </summary>
    /// <returns>유효하면 true, 아니면 false(설정 미변경).</returns>
    public static bool Configure(double value, ScenarioTimeSyncUnit unit)
    {
      if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
      {
        return false;
      }

      _value = value;
      _unit = unit;
      Changed?.Invoke();
      return true;
    }

    /// <summary>도메인 리로드 비활성 환경에서도 기본값(1초)으로 초기화되도록 한다.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetToDefault()
    {
      _value = 1d;
      _unit = ScenarioTimeSyncUnit.Seconds;
      Changed?.Invoke();
    }

    /// <summary>
    /// 현재 설정을 tick 간격으로 환산한다(최소 1). 재전파 판정에 사용.
    /// TimeManager 가 없으면 근사 tickRate 로 폴백한다.
    /// </summary>
    public static uint GetIntervalTicks()
    {
      var tm = InstanceFinder.TimeManager;

      switch (_unit)
      {
        case ScenarioTimeSyncUnit.Tick:
          return (uint)Math.Max(1d, Math.Round(_value));

        case ScenarioTimeSyncUnit.Milliseconds:
        {
          double seconds = _value / 1000d;
          if (tm != null)
          {
            return Math.Max(1u, tm.TimeToTicks(seconds));
          }
          return (uint)Math.Max(1d, Math.Round(seconds * FallbackTicksPerSecond));
        }

        case ScenarioTimeSyncUnit.Seconds:
        default:
        {
          if (tm != null)
          {
            return Math.Max(1u, tm.TimeToTicks(_value));
          }
          return (uint)Math.Max(1d, Math.Round(_value * FallbackTicksPerSecond));
        }
      }
    }

    /// <summary>현재 설정을 사람이 읽을 수 있는 문자열로 반환한다(명령 에코용).</summary>
    public static string Describe()
    {
      string unitText = _unit switch
      {
        ScenarioTimeSyncUnit.Tick => "tick(s)",
        ScenarioTimeSyncUnit.Milliseconds => "ms",
        ScenarioTimeSyncUnit.Seconds => "second(s)",
        _ => _unit.ToString()
      };

      return $"{_value:0.###} {unitText} (≈{GetIntervalTicks()} tick interval)";
    }
  }
}
