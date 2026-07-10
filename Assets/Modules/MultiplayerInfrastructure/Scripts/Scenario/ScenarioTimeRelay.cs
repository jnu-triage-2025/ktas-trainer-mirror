using System;
using FishNet;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시간 표시(스톱워치/카운트다운)의 서버 권한(authoritative) 동기화 중계기.
  ///
  /// 설계 근거:
  /// - 시간 표시는 모든 클라이언트가 동일한 값을 봐야 한다. 그러나 <see cref="ScenarioController"/> 는
  ///   클라이언트마다 독립 실행되고 동기화된 커서가 없으므로, 서버가 "시작/정지/리셋" 명령을
  ///   모든 클라이언트에 push 해야 한다.
  /// - 타이머 명령의 유일한 출처는 서버에서 실행되는 시나리오 로직(<see cref="ScenarioController"/>)이다.
  ///   따라서 클라이언트→서버 보고 경로(ServerRpc)는 두지 않는다(불필요한 신뢰 경계 축소).
  ///   서버 컨텍스트면 <see cref="ObserversRpc"/> 로 전 클라이언트에 미러링하고,
  ///   네트워크 비활성/중계기 부재면 로컬(<see cref="ScenarioTimeState"/>)에만 적용한다.
  /// - 늦게 접속한 클라이언트도 정확한 진행 지점을 보도록, Start 명령에 서버 tick 을 함께 실어
  ///   수신 피어가 "발행 이후 이미 흐른 시간"을 계산해 보정한다(FishNet 의 동기화된 tick 사용).
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

    private enum TimeCommand
    {
      Start = 0,
      Pause = 1,
      Resume = 2,
      Stop = 3,
      Hide = 4
    }

    /// <summary>tick 정보가 없을 때(오프라인 등) 사용하는 미지정 값.</summary>
    private const uint UnsetTick = 0u;

    /// <summary>타이머를 권위적으로 시작한다(방향/시작값/목표값 지정).</summary>
    public static void StartAuthoritative(ScenarioTimeDirection direction, double startSeconds, double targetSeconds)
    {
      Dispatch(TimeCommand.Start, (int)direction, (float)startSeconds, (float)targetSeconds, CurrentServerTick());
    }

    /// <summary>타이머를 권위적으로 일시정지한다.</summary>
    public static void PauseAuthoritative() => Dispatch(TimeCommand.Pause, 0, 0f, 0f, UnsetTick);

    /// <summary>일시정지된 타이머를 권위적으로 재개한다.</summary>
    public static void ResumeAuthoritative() => Dispatch(TimeCommand.Resume, 0, 0f, 0f, UnsetTick);

    /// <summary>타이머를 권위적으로 정지(값 리셋)한다.</summary>
    public static void StopAuthoritative() => Dispatch(TimeCommand.Stop, 0, 0f, 0f, UnsetTick);

    /// <summary>시간 표시를 권위적으로 숨긴다.</summary>
    public static void HideAuthoritative() => Dispatch(TimeCommand.Hide, 0, 0f, 0f, UnsetTick);

    private static void Dispatch(TimeCommand command, int direction, float startSeconds, float targetSeconds, uint issuedTick)
    {
      // 서버 컨텍스트: 서버 로컬 적용 후 전 클라이언트 미러링.
      if (InstanceFinder.IsServerStarted)
      {
        ApplyLocal(command, direction, startSeconds, targetSeconds, issuedTick);
        if (_instance != null)
        {
          _instance.RpcMirror(command, direction, startSeconds, targetSeconds, issuedTick);
        }
        return;
      }

      // 네트워크 비활성/중계기 부재/클라이언트 단독: 로컬 폴백(단일 플레이어/오프라인).
      // (타이머 명령의 출처는 항상 서버 시나리오 로직이므로 클라이언트→서버 보고 경로는 두지 않는다.)
      ApplyLocal(command, direction, startSeconds, targetSeconds, issuedTick);
    }

    private static void ApplyLocal(TimeCommand command, int direction, float startSeconds, float targetSeconds, uint issuedTick)
    {
      // 네트워크 경계에서 수신한 enum 값은 방어적으로 검증한다(파일 로더의 Enum.IsDefined 규약과 일치).
      if (!Enum.IsDefined(typeof(TimeCommand), command))
      {
        return;
      }

      switch (command)
      {
        case TimeCommand.Start:
        {
          ScenarioTimeDirection dir = Enum.IsDefined(typeof(ScenarioTimeDirection), direction)
              ? (ScenarioTimeDirection)direction
              : ScenarioTimeDirection.Stopwatch;
          ScenarioTimeState.Start(dir, startSeconds, targetSeconds, ElapsedSinceIssued(issuedTick));
          break;
        }
        case TimeCommand.Pause:
          ScenarioTimeState.Pause();
          break;
        case TimeCommand.Resume:
          ScenarioTimeState.Resume();
          break;
        case TimeCommand.Stop:
          ScenarioTimeState.Stop();
          break;
        case TimeCommand.Hide:
          ScenarioTimeState.Hide();
          break;
      }
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcMirror(TimeCommand command, int direction, float startSeconds, float targetSeconds, uint issuedTick)
    {
      // 호스트(서버=클라)에서는 이미 서버 경로에서 적용되었으므로 중복 적용해도 무해하다.
      // BufferLast: 늦게 접속한 클라이언트도 마지막 명령을 받는다. issuedTick 으로 이미 흐른 시간을
      // 보정하므로, 처음부터가 아니라 현재 진행 지점부터 표시된다.
      ApplyLocal(command, direction, startSeconds, targetSeconds, issuedTick);
    }

    /// <summary>Start 발행 시점의 서버 tick. 네트워크 비활성이면 UnsetTick(보정 없음).</summary>
    private static uint CurrentServerTick()
    {
      var tm = InstanceFinder.TimeManager;
      return tm != null ? tm.Tick : UnsetTick;
    }

    /// <summary>
    /// Start 발행(<paramref name="issuedTick"/>) 이후 이 피어 수신까지 흐른 시간(초)을 추정한다.
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
