using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시간 표시 UI(스톱워치/타이머)가 흘러가는 방향.
  /// </summary>
  public enum ScenarioTimeDirection
  {
    /// <summary>정방향(스톱워치): 경과 시간이 0 에서 증가한다.</summary>
    Stopwatch,

    /// <summary>역방향(타이머/카운트다운): 남은 시간이 목표 시간에서 0 으로 감소한다.</summary>
    Countdown
  }

  /// <summary>
  /// 시간 표시(스톱워치/카운트다운)의 로컬 스냅샷 모델.
  ///
  /// 설계 근거:
  /// - <see cref="ScenarioController"/> 는 클라이언트마다 독립 실행되고 동기화된 커서가 없다.
  ///   따라서 "모든 클라이언트가 같은 시각을 본다"는 요구를 만족하려면 서버가 시작 기준점을
  ///   push 하고, 각 클라이언트가 그 기준점으로부터 로컬에서 tick 해야 한다.
  ///   (TitleUIController 가 서버로부터 tick 값을 받아 로컬에서 페이드 시간을 계산하는 방식과 동일한 계열.)
  /// - 이 정적 저장소는 그 "로컬 기준점"을 담는다. 서버 동기화는 <see cref="ScenarioTimeRelay"/> 가 담당하고,
  ///   실제 표시는 <see cref="MultiplayerInfrastructure.UI.TimeDisplayUIController"/> HUD 가 매 프레임 조회한다.
  ///
  /// 기준점은 각 피어의 <see cref="Time.realtimeSinceStartupAsDouble"/> 로 환산된
  /// <see cref="_localAnchorRealtime"/> 로 저장된다. 서버/클라 절대 클럭이 다르더라도
  /// 각 피어는 "명령을 수신한 순간"을 기준점으로 삼아 동일한 경과 시간을 계산한다.
  /// (RPC 전파 지연만큼의 오차가 있으나 초 단위 표시에는 무해하다.)
  /// </summary>
  public static class ScenarioTimeState
  {
    /// <summary>표시 상태가 바뀔 때(시작/정지/리셋/숨김) 발생한다. HUD 즉시 갱신용.</summary>
    public static event Action Changed;

    private static bool _visible;
    private static bool _running;
    private static ScenarioTimeDirection _direction = ScenarioTimeDirection.Stopwatch;

    /// <summary>카운트다운의 목표 시간(초). 스톱워치에서는 0.</summary>
    private static double _targetSeconds;

    /// <summary>기준점에서의 시작 값(초). 일시정지 후 재개 시 누적 경과를 담는다.</summary>
    private static double _baseElapsedSeconds;

    /// <summary>running 으로 전환된 로컬 시각(초, realtime). 정지 상태에서는 의미 없음.</summary>
    private static double _localAnchorRealtime;

    /// <summary>현재 표시되어야 하는가.</summary>
    public static bool IsVisible => _visible;

    /// <summary>현재 흐름 방향.</summary>
    public static ScenarioTimeDirection Direction => _direction;

    /// <summary>
    /// 정적 상태를 초기화한다. 플레이 세션 시작 시(도메인 리로드 비활성 환경 포함)와
    /// 시나리오 시작/종료 시 호출하여 이전 세션/시나리오의 잔여 타이머가 새어 나오지 않게 한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
      _visible = false;
      _running = false;
      _direction = ScenarioTimeDirection.Stopwatch;
      _targetSeconds = 0d;
      _baseElapsedSeconds = 0d;
      _localAnchorRealtime = 0d;
      Changed?.Invoke();
    }

    /// <summary>
    /// 타이머를 시작(또는 재시작)한다.
    /// </summary>
    /// <param name="direction">정방향(스톱워치) 또는 역방향(카운트다운).</param>
    /// <param name="startSeconds">
    /// 시작 시점의 "표시 값"(초). 스톱워치는 표시할 경과 값(보통 0),
    /// 카운트다운은 표시할 남은 값(보통 목표 시간과 동일). 내부적으로는 경과(elapsed)로 환산된다.
    /// </param>
    /// <param name="targetSeconds">
    /// 카운트다운의 목표(총) 시간(초). 스톱워치에서는 무시된다.
    /// </param>
    /// <param name="alreadyRunningSeconds">
    /// 이 명령이 서버에서 발행된 뒤 이 피어가 수신하기까지 이미 흐른 시간(초).
    /// 뒤늦게 접속한 클라이언트가 처음부터가 아니라 "현재 진행된 지점"부터 표시하도록 보정한다.
    /// 서버/호스트나 즉시 수신한 클라이언트는 0 을 준다. 음수는 0 으로 취급한다.
    /// </param>
    public static void Start(
        ScenarioTimeDirection direction,
        double startSeconds,
        double targetSeconds,
        double alreadyRunningSeconds = 0d)
    {
      _direction = direction;
      _targetSeconds = Math.Max(0d, targetSeconds);

      // startSeconds 는 "표시 값" 이므로 내부 경과(elapsed) 로 환산한다.
      // - 스톱워치: 표시 경과 = 경과. (음수 방지)
      // - 카운트다운: 표시 남은값 = target - 경과  →  경과 = target - 표시 남은값.
      double initialElapsed = direction == ScenarioTimeDirection.Countdown
          ? _targetSeconds - Math.Clamp(startSeconds, 0d, _targetSeconds)
          : Math.Max(0d, startSeconds);

      // 발행 이후 이미 흐른 시간을 경과에 더해, 늦게 수신한 피어도 동일한 표시 지점으로 맞춘다.
      _baseElapsedSeconds = initialElapsed + Math.Max(0d, alreadyRunningSeconds);
      _localAnchorRealtime = Now();
      _running = true;
      _visible = true;
      Changed?.Invoke();
    }

    /// <summary>흐름을 일시정지한다(표시는 유지). 현재까지의 경과를 확정한다.</summary>
    public static void Pause()
    {
      if (!_running)
      {
        return;
      }

      _baseElapsedSeconds = RawElapsedSeconds();
      _running = false;
      Changed?.Invoke();
    }

    /// <summary>일시정지된 흐름을 재개한다.</summary>
    public static void Resume()
    {
      if (_running || !_visible)
      {
        return;
      }

      _localAnchorRealtime = Now();
      _running = true;
      Changed?.Invoke();
    }

    /// <summary>
    /// 흐름을 정지하고 값을 시작값으로 되돌린다(표시는 유지).
    /// 경과(elapsed)를 0 으로 되돌리므로, 스톱워치는 00:00:00, 카운트다운은 목표 시간(전량)으로 표시된다.
    /// </summary>
    public static void Stop()
    {
      _running = false;
      _baseElapsedSeconds = 0d;
      Changed?.Invoke();
    }

    /// <summary>표시를 숨기고 상태를 초기화한다.</summary>
    public static void Hide()
    {
      _visible = false;
      _running = false;
      _baseElapsedSeconds = 0d;
      _targetSeconds = 0d;
      Changed?.Invoke();
    }

    /// <summary>
    /// 현재 표시되어야 할 값(초)을 반환한다. 스톱워치는 경과 시간, 카운트다운은 남은 시간.
    /// 카운트다운은 0 미만으로 내려가지 않는다.
    /// </summary>
    public static double CurrentDisplaySeconds()
    {
      double elapsed = RawElapsedSeconds();

      if (_direction == ScenarioTimeDirection.Countdown)
      {
        return Math.Clamp(_targetSeconds - elapsed, 0d, _targetSeconds);
      }

      return elapsed;
    }

    /// <summary>카운트다운이 0 에 도달했는가(스톱워치면 항상 false).</summary>
    public static bool HasCountdownFinished()
    {
      return _direction == ScenarioTimeDirection.Countdown && CurrentDisplaySeconds() <= 0d;
    }

    /// <summary>
    /// 현재 표시 값을 정수 초로 반환한다(음수/NaN 은 0). 매 프레임 변화 감지에 사용하여
    /// 값이 바뀐 초에만 문자열을 재포맷하도록 한다(프레임당 문자열 할당 방지).
    /// </summary>
    public static long CurrentDisplayWholeSeconds()
    {
      double value = CurrentDisplaySeconds();
      if (value < 0d || double.IsNaN(value))
      {
        return 0L;
      }

      return (long)Math.Floor(value);
    }

    /// <summary>정수 초 값을 hh:mm:ss 로 변환한다. 두 자리 룩업으로 박싱/보간 할당을 피한다.</summary>
    public static string FormatHhMmSs(long wholeSeconds)
    {
      if (wholeSeconds < 0L)
      {
        wholeSeconds = 0L;
      }

      long hours = wholeSeconds / 3600;
      int minutes = (int)((wholeSeconds % 3600) / 60);
      int seconds = (int)(wholeSeconds % 60);

      // 시(hour)는 2자리를 넘을 수 있으므로 별도 포맷, 분/초는 00~59 룩업.
      string hh = hours < 100 ? TwoDigits[(int)hours] : hours.ToString("00");
      return string.Concat(hh, ":", TwoDigits[minutes], ":", TwoDigits[seconds]);
    }

    /// <summary>편의 오버로드. 실수 초를 정수 초로 내림하여 포맷한다.</summary>
    public static string FormatHhMmSs(double totalSeconds)
    {
      if (totalSeconds < 0d || double.IsNaN(totalSeconds))
      {
        totalSeconds = 0d;
      }

      return FormatHhMmSs((long)Math.Floor(totalSeconds));
    }

    /// <summary>00~99 두 자리 문자열 룩업 테이블(프레임당 문자열 할당 방지).</summary>
    private static readonly string[] TwoDigits = BuildTwoDigits();

    private static string[] BuildTwoDigits()
    {
      var table = new string[100];
      for (int i = 0; i < 100; i++)
      {
        table[i] = i.ToString("00");
      }

      return table;
    }

    private static double RawElapsedSeconds()
    {
      if (!_running)
      {
        return _baseElapsedSeconds;
      }

      return _baseElapsedSeconds + (Now() - _localAnchorRealtime);
    }

    private static double Now()
    {
      return Time.realtimeSinceStartupAsDouble;
    }
  }
}
