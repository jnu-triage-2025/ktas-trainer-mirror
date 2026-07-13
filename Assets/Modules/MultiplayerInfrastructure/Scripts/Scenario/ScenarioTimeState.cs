using System;
using System.Collections.Generic;
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
  /// 다중 시간(스톱워치/카운트다운)의 로컬 스냅샷 모델.
  ///
  /// 설계 개요:
  /// - 식별자(timerId)로 여러 타이머를 동시에 보유한다. 다만 화면에 표시되는 타이머는 항상 최대 1개이며,
  ///   <see cref="_shownTimerId"/> 가 그 대상을 가리킨다.
  /// - 생성(<see cref="Create"/>)은 "정지" 상태로 만들고, 흐름은 <see cref="Start"/>/<see cref="Resume"/> 로,
  ///   화면 표시는 <see cref="Show"/>/<see cref="Hide"/> 로 "별도 제어"한다. 카운트다운이 0 에 도달해도
  ///   자동으로 숨기지 않는다(표시 전환은 오직 Show/Hide/Remove 로만 발생).
  ///
  /// 멀티플레이어:
  /// - <see cref="ScenarioController"/> 는 클라이언트마다 독립 실행되므로, 서버가 각 연산을 push 하고
  ///   각 클라이언트가 로컬 기준점(<see cref="TimerInstance._localAnchorRealtime"/>, realtime)에서 tick 한다.
  ///   서버 동기화는 <see cref="ScenarioTimeRelay"/> 가, 표시는
  ///   <see cref="MultiplayerInfrastructure.UI.TimeDisplayUIController"/> HUD 가 담당한다.
  /// </summary>
  public static class ScenarioTimeState
  {
    /// <summary>표시 대상/값 상태가 바뀔 때(생성/시작/정지/표시/숨김/삭제 등) 발생한다. HUD 즉시 갱신용.</summary>
    public static event Action Changed;

    /// <summary>개별 타이머의 로컬 스냅샷.</summary>
    private sealed class TimerInstance
    {
      public ScenarioTimeDirection Direction;

      /// <summary>카운트다운의 목표(총) 시간(초). 스톱워치에서는 0.</summary>
      public double TargetSeconds;

      /// <summary>정지/일시정지 시점까지 확정된 누적 경과(초).</summary>
      public double BaseElapsedSeconds;

      /// <summary>running 으로 전환된 로컬 시각(초, realtime). 정지 상태에서는 의미 없음.</summary>
      public double LocalAnchorRealtime;

      /// <summary>현재 흐르고 있는가(정지/일시정지면 false).</summary>
      public bool Running;
    }

    private static readonly Dictionary<string, TimerInstance> Timers =
        new Dictionary<string, TimerInstance>(StringComparer.Ordinal);

    /// <summary>현재 화면에 표시 중인 타이머 id(없으면 null). 표시는 항상 최대 1개.</summary>
    private static string _shownTimerId;

    /// <summary>현재 화면에 표시할 타이머가 있는가.</summary>
    public static bool IsVisible => _shownTimerId != null && Timers.ContainsKey(_shownTimerId);

    /// <summary>현재 표시 중인 타이머 id(없으면 null).</summary>
    public static string ShownTimerId => IsVisible ? _shownTimerId : null;

    /// <summary>
    /// 정적 상태를 초기화한다. 플레이 세션 시작 시(도메인 리로드 비활성 환경 포함)와
    /// 시나리오 시작/종료 시 호출하여 이전 세션/시나리오의 잔여 타이머가 새어 나오지 않게 한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
      Timers.Clear();
      _shownTimerId = null;
      Changed?.Invoke();
    }

    /// <summary>
    /// 타이머를 생성(또는 재설정)한다. "정지" 상태로 만들며, 화면에 표시하지 않는다.
    /// 이미 같은 id 가 있으면 방향/목표/시작값을 새로 덮어쓴다.
    /// </summary>
    /// <param name="timerId">타이머 식별자.</param>
    /// <param name="direction">정방향(스톱워치) 또는 역방향(카운트다운).</param>
    /// <param name="startSeconds">
    /// 시작 시점의 "표시 값"(초). 스톱워치는 표시할 경과(보통 0), 카운트다운은 표시할 남은값(보통 목표와 동일).
    /// </param>
    /// <param name="targetSeconds">카운트다운의 목표(총) 시간(초). 스톱워치에서는 무시된다.</param>
    public static void Create(string timerId, ScenarioTimeDirection direction, double startSeconds, double targetSeconds)
    {
      if (string.IsNullOrWhiteSpace(timerId))
      {
        return;
      }

      var timer = new TimerInstance
      {
        Direction = direction,
        TargetSeconds = Math.Max(0d, targetSeconds),
        Running = false,
        LocalAnchorRealtime = Now()
      };
      timer.BaseElapsedSeconds = ToElapsed(timer, startSeconds);

      Timers[timerId] = timer;
      Changed?.Invoke();
    }

    /// <summary>
    /// 타이머의 흐름을 시작(또는 재시작)한다. 없으면 아무 것도 하지 않는다.
    /// 표시 상태는 건드리지 않는다(표시는 <see cref="Show"/> 로 별도 제어).
    /// </summary>
    /// <param name="alreadyRunningSeconds">
    /// 이 명령이 서버에서 발행된 뒤 이 피어가 수신하기까지 이미 흐른 시간(초). 늦게 수신한 피어의 보정용.
    /// 서버/호스트/즉시 수신 피어는 0. 음수는 0 으로 취급한다.
    /// </param>
    public static void Start(string timerId, double alreadyRunningSeconds = 0d)
    {
      if (!TryGet(timerId, out var timer))
      {
        return;
      }

      // 시작 시 경과를 0 으로 되돌리지 않는다(Create 에서 설정한 시작값을 유지). 다만 발행 이후
      // 이미 흐른 시간을 경과에 더해 늦게 수신한 피어의 표시 지점을 맞춘다.
      timer.BaseElapsedSeconds += Math.Max(0d, alreadyRunningSeconds);
      timer.LocalAnchorRealtime = Now();
      timer.Running = true;
      Changed?.Invoke();
    }

    /// <summary>타이머 흐름을 일시정지한다(표시/값 유지). 현재까지의 경과를 확정한다.</summary>
    public static void Pause(string timerId)
    {
      if (!TryGet(timerId, out var timer) || !timer.Running)
      {
        return;
      }

      timer.BaseElapsedSeconds = RawElapsed(timer);
      timer.Running = false;
      Changed?.Invoke();
    }

    /// <summary>일시정지된 타이머 흐름을 재개한다.</summary>
    public static void Resume(string timerId, double alreadyRunningSeconds = 0d)
    {
      if (!TryGet(timerId, out var timer) || timer.Running)
      {
        return;
      }

      timer.BaseElapsedSeconds += Math.Max(0d, alreadyRunningSeconds);
      timer.LocalAnchorRealtime = Now();
      timer.Running = true;
      Changed?.Invoke();
    }

    /// <summary>
    /// 타이머 흐름을 정지하고 값을 시작값(경과 0)으로 되돌린다(표시/생성 상태 유지).
    /// 스톱워치는 00:00:00, 카운트다운은 목표 시간(전량)으로 표시된다.
    /// </summary>
    public static void Stop(string timerId)
    {
      if (!TryGet(timerId, out var timer))
      {
        return;
      }

      timer.Running = false;
      timer.BaseElapsedSeconds = 0d;
      Changed?.Invoke();
    }

    /// <summary>
    /// 타이머의 현재 표시값을 절대값으로 설정한다(스톱워치=경과, 카운트다운=남은값). 흐름 상태는 유지한다.
    /// 카운트다운의 목표(총) 시간도 함께 조정할 수 있다.
    /// </summary>
    /// <param name="displaySeconds">설정할 현재 표시값(초). 음수는 0 으로 취급.</param>
    /// <param name="newTargetSeconds">
    /// 카운트다운 목표(총) 시간(초)을 재설정한다. null 이면 기존 목표 유지. 스톱워치에서는 무시된다.
    /// </param>
    public static void Set(string timerId, double displaySeconds, double? newTargetSeconds)
    {
      if (!TryGet(timerId, out var timer))
      {
        return;
      }

      if (newTargetSeconds.HasValue && timer.Direction == ScenarioTimeDirection.Countdown)
      {
        timer.TargetSeconds = Math.Max(0d, newTargetSeconds.Value);
      }

      // 흐르는 중이면 기준점을 지금으로 다시 잡아 표시값을 정확히 고정한다.
      timer.BaseElapsedSeconds = ToElapsed(timer, displaySeconds);
      timer.LocalAnchorRealtime = Now();
      Changed?.Invoke();
    }

    /// <summary>지정한 타이머를 화면에 표시한다(표시는 항상 최대 1개, 기존 표시는 교체). 없으면 무시.</summary>
    public static void Show(string timerId)
    {
      if (!Timers.ContainsKey(timerId))
      {
        return;
      }

      _shownTimerId = timerId;
      Changed?.Invoke();
    }

    /// <summary>화면 표시를 끈다. 타이머 상태/흐름은 유지한다(삭제 아님).</summary>
    public static void Hide()
    {
      if (_shownTimerId == null)
      {
        return;
      }

      _shownTimerId = null;
      Changed?.Invoke();
    }

    /// <summary>
    /// 주기적 재동기화 스냅샷을 적용한다. 지정한 타이머의 값(방향/목표/경과)만 스냅샷으로 덮어쓴다(없으면 생성).
    /// 흐르는 중이면 발행 이후 이미 흐른 시간(<paramref name="alreadyRunningSeconds"/>)을 반영해
    /// 늦게 수신한 피어도 정확히 맞춘다.
    ///
    /// 표시 여부(<see cref="_shownTimerId"/>)는 이 메서드에서 절대 변경하지 않는다. 표시 전환은
    /// 스펙상 오직 <see cref="Show"/>/<see cref="Hide"/>/<see cref="Remove"/> 연산으로만 발생해야 한다.
    /// (과거 구현은 resync 가 표시를 강제로 켜서, 생성만 한 타이머나 이전 세션의 버퍼된 resync 로 인해
    ///  Show 없이 화면에 표시되는 문제가 있었다.)
    /// </summary>
    public static void ApplyResync(
        string timerId,
        ScenarioTimeDirection direction,
        double displaySeconds,
        double targetSeconds,
        bool running,
        double alreadyRunningSeconds)
    {
      if (string.IsNullOrWhiteSpace(timerId))
      {
        return;
      }

      var timer = new TimerInstance
      {
        Direction = direction,
        TargetSeconds = Math.Max(0d, targetSeconds),
        Running = running,
        LocalAnchorRealtime = Now()
      };

      double elapsed = ToElapsed(timer, displaySeconds);
      if (running)
      {
        elapsed += Math.Max(0d, alreadyRunningSeconds);
      }
      timer.BaseElapsedSeconds = elapsed;

      // 값만 갱신한다. 표시 대상(_shownTimerId)은 건드리지 않는다.
      Timers[timerId] = timer;
      Changed?.Invoke();
    }

    /// <summary>모든 타이머를 삭제하고 표시를 끈다. 시나리오 시작/종료 등 경계 정리에 사용.</summary>
    public static void RemoveAll()
    {
      if (Timers.Count == 0 && _shownTimerId == null)
      {
        return;
      }

      Timers.Clear();
      _shownTimerId = null;
      Changed?.Invoke();
    }

    /// <summary>타이머를 삭제한다. 표시 중이던 타이머면 표시도 꺼진다.</summary>
    public static void Remove(string timerId)
    {
      if (!Timers.Remove(timerId))
      {
        return;
      }

      if (_shownTimerId == timerId)
      {
        _shownTimerId = null;
      }

      Changed?.Invoke();
    }

    /// <summary>현재 표시 중인 타이머의 흐름 방향. 표시 중이 아니면 스톱워치(기본).</summary>
    public static ScenarioTimeDirection ShownDirection()
    {
      return TryGet(_shownTimerId, out var timer) ? timer.Direction : ScenarioTimeDirection.Stopwatch;
    }

    /// <summary>
    /// 현재 표시 중인 타이머의 표시값을 정수 초로 반환한다(음수/NaN 은 0). 표시 중이 아니면 0.
    /// 매 프레임 변화 감지에 사용하여 값이 바뀐 초에만 재포맷하도록 한다(프레임당 문자열 할당 방지).
    /// </summary>
    public static long ShownWholeSeconds()
    {
      if (!TryGet(_shownTimerId, out var timer))
      {
        return 0L;
      }

      double value = DisplaySeconds(timer);
      if (value < 0d || double.IsNaN(value))
      {
        return 0L;
      }

      return (long)Math.Floor(value);
    }

    /// <summary>
    /// 현재 표시 중인 타이머의 권위 상태를 반환한다(주기적 재동기화용).
    /// 표시 중인 타이머가 없으면 false.
    /// </summary>
    /// <param name="timerId">표시 중인 타이머 id.</param>
    /// <param name="direction">흐름 방향.</param>
    /// <param name="displaySeconds">현재 표시값(스톱워치=경과, 카운트다운=남은값). 실수 초.</param>
    /// <param name="targetSeconds">카운트다운 목표(총) 시간. 스톱워치면 0.</param>
    /// <param name="running">현재 흐르고 있는가.</param>
    public static bool TryGetShownAuthoritative(
        out string timerId,
        out ScenarioTimeDirection direction,
        out double displaySeconds,
        out double targetSeconds,
        out bool running)
    {
      if (!TryGet(_shownTimerId, out var timer))
      {
        timerId = null;
        direction = ScenarioTimeDirection.Stopwatch;
        displaySeconds = 0d;
        targetSeconds = 0d;
        running = false;
        return false;
      }

      timerId = _shownTimerId;
      direction = timer.Direction;
      displaySeconds = Math.Max(0d, DisplaySeconds(timer));
      targetSeconds = timer.TargetSeconds;
      running = timer.Running;
      return true;
    }

    /// <summary>현재 표시 중인 카운트다운이 0 에 도달했는가(표시 중 아님/스톱워치면 false).</summary>
    public static bool ShownCountdownFinished()
    {
      if (!TryGet(_shownTimerId, out var timer))
      {
        return false;
      }

      return timer.Direction == ScenarioTimeDirection.Countdown && DisplaySeconds(timer) <= 0d;
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

    // ── 내부 헬퍼 ──────────────────────────────────────────────────────────

    private static bool TryGet(string timerId, out TimerInstance timer)
    {
      if (!string.IsNullOrWhiteSpace(timerId))
      {
        return Timers.TryGetValue(timerId, out timer);
      }

      timer = null;
      return false;
    }

    /// <summary>표시값(스톱워치=경과, 카운트다운=남은값) → 내부 경과(elapsed) 로 환산.</summary>
    private static double ToElapsed(TimerInstance timer, double displaySeconds)
    {
      if (timer.Direction == ScenarioTimeDirection.Countdown)
      {
        // 남은값 = target - 경과  →  경과 = target - 남은값.
        return timer.TargetSeconds - Math.Clamp(displaySeconds, 0d, timer.TargetSeconds);
      }

      return Math.Max(0d, displaySeconds);
    }

    /// <summary>현재 표시값(스톱워치=경과, 카운트다운=남은값)을 계산한다.</summary>
    private static double DisplaySeconds(TimerInstance timer)
    {
      double elapsed = RawElapsed(timer);

      if (timer.Direction == ScenarioTimeDirection.Countdown)
      {
        return Math.Clamp(timer.TargetSeconds - elapsed, 0d, timer.TargetSeconds);
      }

      return elapsed;
    }

    private static double RawElapsed(TimerInstance timer)
    {
      if (!timer.Running)
      {
        return timer.BaseElapsedSeconds;
      }

      return timer.BaseElapsedSeconds + (Now() - timer.LocalAnchorRealtime);
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

    private static double Now()
    {
      return Time.realtimeSinceStartupAsDouble;
    }
  }
}
