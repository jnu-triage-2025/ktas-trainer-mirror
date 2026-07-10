using System;
using FishNet;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 다중 시간(스톱워치/카운트다운)의 서버 권한(authoritative) 동기화 중계기.
  ///
  /// 설계 근거:
  /// - 시간 표시는 모든 클라이언트가 동일한 값을 봐야 한다. 그러나 <see cref="ScenarioController"/> 는
  ///   클라이언트마다 독립 실행되고 동기화된 커서가 없으므로, 서버가 각 연산(생성/시작/표시 등)을
  ///   모든 클라이언트에 push 해야 한다.
  /// - 타이머 연산의 유일한 출처는 서버에서 실행되는 시나리오 로직(<see cref="ScenarioController"/>)이다.
  ///   따라서 클라이언트→서버 보고 경로(ServerRpc)는 두지 않는다. 서버 컨텍스트면
  ///   <see cref="ObserversRpc"/> 로 전 클라이언트에 미러링하고, 네트워크 비활성/중계기 부재면
  ///   로컬(<see cref="ScenarioTimeState"/>)에만 적용한다.
  /// - 흐름 시작(Start/Resume) 연산에는 서버 tick 을 함께 실어, 늦게 접속한 클라이언트가
  ///   "발행 이후 이미 흐른 시간"을 계산해 현재 진행 지점부터 표시하도록 보정한다.
  ///
  /// 단일 플레이어(호스트 단독)에서는 서버=클라 이므로 로컬 적용과 동일하게 동작한다.
  /// </summary>
  public sealed class ScenarioTimeRelay : NetworkBehaviour
  {
    private static ScenarioTimeRelay _instance;

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(gameObject);
        return;
      }

      _instance = this;
    }

    private void OnDestroy()
    {
      if (_instance == this)
      {
        _instance = null;
      }
    }

    /// <summary>tick 정보가 없을 때(오프라인 등) 사용하는 미지정 값.</summary>
    private const uint UnsetTick = 0u;

    /// <summary>마지막 재동기화 이후 누적된 tick 수(서버 전용).</summary>
    private uint _ticksSinceLastResync;

    private bool _tickSubscribed;

    // ── 주기적 재동기화 구동 (서버 전용) ───────────────────────────────────

    public override void OnStartServer()
    {
      base.OnStartServer();
      SubscribeTick();
    }

    public override void OnStopServer()
    {
      UnsubscribeTick();
      base.OnStopServer();
    }

    private void SubscribeTick()
    {
      if (_tickSubscribed)
      {
        return;
      }

      var tm = TimeManager;
      if (tm == null)
      {
        return;
      }

      tm.OnTick += TimeManager_OnTick;
      _ticksSinceLastResync = 0u;
      _tickSubscribed = true;
    }

    private void UnsubscribeTick()
    {
      if (!_tickSubscribed)
      {
        return;
      }

      var tm = TimeManager;
      if (tm != null)
      {
        tm.OnTick -= TimeManager_OnTick;
      }

      _tickSubscribed = false;
    }

    /// <summary>
    /// 서버 tick 마다 호출된다. 설정된 밀도(<see cref="ScenarioTimeSyncSettings"/>)에 도달하면
    /// 현재 화면에 표시 중인 타이머의 권위 값을 전 클라이언트에 재전파하여 드리프트를 보정한다.
    /// </summary>
    private void TimeManager_OnTick()
    {
      // 서버 컨텍스트에서만 재전파한다(호스트 포함). 이 콜백은 서버에서만 구독된다.
      _ticksSinceLastResync++;

      uint interval = ScenarioTimeSyncSettings.GetIntervalTicks();
      if (_ticksSinceLastResync < interval)
      {
        return;
      }

      _ticksSinceLastResync = 0u;

      // 표시 중인 타이머가 없으면 재전파할 것이 없다.
      if (!ScenarioTimeState.TryGetShownAuthoritative(
              out string timerId, out ScenarioTimeDirection direction,
              out double displaySeconds, out double targetSeconds, out bool running))
      {
        return;
      }

      // 서버 로컬은 이미 최신이므로 클라이언트에만 미러링한다(호스트 중복 적용도 무해).
      RpcResync(timerId, (int)direction, (float)displaySeconds, (float)targetSeconds, running, CurrentServerTick());
    }

    // ── 공개 API (서버 시나리오 로직에서 호출) ─────────────────────────────

    public static void CreateAuthoritative(string timerId, ScenarioTimeDirection direction, double startSeconds, double targetSeconds)
    {
      Dispatch(ScenarioTimeOperationType.Create, timerId, (int)direction, (float)startSeconds, (float)targetSeconds, false, UnsetTick);
    }

    public static void StartAuthoritative(string timerId)
    {
      Dispatch(ScenarioTimeOperationType.Start, timerId, 0, 0f, 0f, false, CurrentServerTick());
    }

    public static void PauseAuthoritative(string timerId)
    {
      Dispatch(ScenarioTimeOperationType.Pause, timerId, 0, 0f, 0f, false, UnsetTick);
    }

    public static void ResumeAuthoritative(string timerId)
    {
      Dispatch(ScenarioTimeOperationType.Resume, timerId, 0, 0f, 0f, false, CurrentServerTick());
    }

    public static void StopAuthoritative(string timerId)
    {
      Dispatch(ScenarioTimeOperationType.Stop, timerId, 0, 0f, 0f, false, UnsetTick);
    }

    /// <param name="hasNewTarget">카운트다운 목표(총) 시간을 재설정할지 여부.</param>
    public static void SetAuthoritative(string timerId, double displaySeconds, bool hasNewTarget, double newTargetSeconds)
    {
      Dispatch(ScenarioTimeOperationType.Set, timerId, 0, (float)displaySeconds, (float)newTargetSeconds, hasNewTarget, UnsetTick);
    }

    public static void ShowAuthoritative(string timerId)
    {
      Dispatch(ScenarioTimeOperationType.Show, timerId, 0, 0f, 0f, false, UnsetTick);
    }

    public static void HideAuthoritative()
    {
      Dispatch(ScenarioTimeOperationType.Hide, null, 0, 0f, 0f, false, UnsetTick);
    }

    public static void RemoveAuthoritative(string timerId)
    {
      Dispatch(ScenarioTimeOperationType.Remove, timerId, 0, 0f, 0f, false, UnsetTick);
    }

    /// <summary>
    /// 모든 타이머를 삭제하고 표시를 끈다(전 클라이언트). 시나리오 시작/종료 경계 정리용.
    /// 노드 연산 어휘(<see cref="ScenarioTimeOperationType"/>)에는 노출하지 않는 시스템 연산이다.
    /// </summary>
    public static void ClearAllAuthoritative()
    {
      if (InstanceFinder.IsServerStarted)
      {
        ScenarioTimeState.RemoveAll();
        if (_instance != null)
        {
          _instance.RpcClearAll();
        }
        return;
      }

      ScenarioTimeState.RemoveAll();
    }

    // ── 내부 전파/적용 ─────────────────────────────────────────────────────

    private static void Dispatch(
        ScenarioTimeOperationType operation, string timerId, int direction,
        float startSeconds, float targetSeconds, bool flag, uint issuedTick)
    {
      string id = timerId ?? string.Empty;

      // 서버 컨텍스트: 서버 로컬 적용 후 전 클라이언트 미러링.
      if (InstanceFinder.IsServerStarted)
      {
        ApplyLocal(operation, id, direction, startSeconds, targetSeconds, flag, issuedTick);
        if (_instance != null)
        {
          _instance.RpcMirror(operation, id, direction, startSeconds, targetSeconds, flag, issuedTick);
        }
        return;
      }

      // 네트워크 비활성/중계기 부재/클라이언트 단독: 로컬 폴백(단일 플레이어/오프라인).
      ApplyLocal(operation, id, direction, startSeconds, targetSeconds, flag, issuedTick);
    }

    private static void ApplyLocal(
        ScenarioTimeOperationType operation, string timerId, int direction,
        float startSeconds, float targetSeconds, bool flag, uint issuedTick)
    {
      // 네트워크 경계에서 수신한 enum 값은 방어적으로 검증한다(파일 로더의 Enum.IsDefined 규약과 일치).
      if (!Enum.IsDefined(typeof(ScenarioTimeOperationType), operation))
      {
        return;
      }

      switch (operation)
      {
        case ScenarioTimeOperationType.Create:
        {
          ScenarioTimeDirection dir = Enum.IsDefined(typeof(ScenarioTimeDirection), direction)
              ? (ScenarioTimeDirection)direction
              : ScenarioTimeDirection.Stopwatch;
          ScenarioTimeState.Create(timerId, dir, startSeconds, targetSeconds);
          break;
        }
        case ScenarioTimeOperationType.Start:
          ScenarioTimeState.Start(timerId, ElapsedSinceIssued(issuedTick));
          break;
        case ScenarioTimeOperationType.Pause:
          ScenarioTimeState.Pause(timerId);
          break;
        case ScenarioTimeOperationType.Resume:
          ScenarioTimeState.Resume(timerId, ElapsedSinceIssued(issuedTick));
          break;
        case ScenarioTimeOperationType.Stop:
          ScenarioTimeState.Stop(timerId);
          break;
        case ScenarioTimeOperationType.Set:
          ScenarioTimeState.Set(timerId, startSeconds, flag ? targetSeconds : (double?)null);
          break;
        case ScenarioTimeOperationType.Show:
          ScenarioTimeState.Show(timerId);
          break;
        case ScenarioTimeOperationType.Hide:
          ScenarioTimeState.Hide();
          break;
        case ScenarioTimeOperationType.Remove:
          ScenarioTimeState.Remove(timerId);
          break;
      }
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcMirror(
        ScenarioTimeOperationType operation, string timerId, int direction,
        float startSeconds, float targetSeconds, bool flag, uint issuedTick)
    {
      // 호스트(서버=클라)에서는 이미 서버 경로에서 적용되었으므로 중복 적용해도 무해하다.
      // BufferLast: 늦게 접속한 클라이언트도 마지막 연산을 받는다. 흐름 시작 연산은 issuedTick 으로
      // 이미 흐른 시간을 보정하므로, 처음부터가 아니라 현재 진행 지점부터 표시된다.
      ApplyLocal(operation, timerId, direction, startSeconds, targetSeconds, flag, issuedTick);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcClearAll()
    {
      ScenarioTimeState.RemoveAll();
    }

    /// <summary>
    /// 현재 표시 중인 타이머의 권위 스냅샷을 전 클라이언트에 재전파한다(주기적 드리프트 보정).
    /// running 인 경우 issuedTick 으로 발행 이후 흐른 시간을 보정한다.
    /// </summary>
    [ObserversRpc(BufferLast = true)]
    private void RpcResync(string timerId, int direction, float displaySeconds, float targetSeconds, bool running, uint issuedTick)
    {
      ScenarioTimeDirection dir = Enum.IsDefined(typeof(ScenarioTimeDirection), direction)
          ? (ScenarioTimeDirection)direction
          : ScenarioTimeDirection.Stopwatch;

      ScenarioTimeState.ApplyResync(timerId, dir, displaySeconds, targetSeconds, running, ElapsedSinceIssued(issuedTick));
    }

    /// <summary>흐름 시작 연산 발행 시점의 서버 tick. 네트워크 비활성이면 UnsetTick(보정 없음).</summary>
    private static uint CurrentServerTick()
    {
      var tm = InstanceFinder.TimeManager;
      return tm != null ? tm.Tick : UnsetTick;
    }

    /// <summary>
    /// 발행(<paramref name="issuedTick"/>) 이후 이 피어 수신까지 흐른 시간(초)을 추정한다.
    /// tick 정보가 없거나(오프라인) 미래 tick 이면 0 을 반환한다.
    /// </summary>
    private static double ElapsedSinceIssued(uint issuedTick)
    {
      if (issuedTick == UnsetTick)
      {
        return 0d;
      }

      var tm = InstanceFinder.TimeManager;
      if (tm == null)
      {
        return 0d;
      }

      uint now = tm.Tick;
      if (now <= issuedTick)
      {
        return 0d;
      }

      return (now - issuedTick) * tm.TickDelta;
    }
  }
}
