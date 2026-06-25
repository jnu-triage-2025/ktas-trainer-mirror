using FishNet;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 도메인의 서버 권한(authoritative) 신호 중계기.
  ///
  /// 설계 근거(G-8, P1 단계):
  /// - 시나리오 게이팅에 쓰이는 완료 신호는 <see cref="ScenarioInteractionSignals"/> 를 통해
  ///   <c>RegistryType.RuntimeState</c> 레지스트리(정적·비네트워크)에 기록된다.
  /// - 인터랙션은 각 클라이언트 컨텍스트에서 일어나므로, 신호가 클라이언트 로컬에만 남으면
  ///   "한 플레이어의 행동이 다른 플레이어 브랜치의 게이트를 통과시키는" 다인 협력이 성립하지 않는다.
  /// - 이 중계기는 클라이언트가 올린 신호를 <see cref="ServerRpc"/> 로 서버에 보고하여
  ///   서버의 단일 권위 RuntimeState 에 기록되게 한다. (Validator 판정은 서버에서 수행)
  ///
  /// 사용:
  /// - 게임플레이 코드는 그대로 <see cref="ScenarioInteractionSignals.Raise"/> 만 호출하면 된다.
  ///   서버 컨텍스트면 직접 기록, 클라이언트 컨텍스트면 이 중계기를 통해 서버로 보고된다.
  ///
  /// 범위: P1(공유 신호)만 담당한다. 실행 권위 이전/표현 RPC(P2·P3)는 후속.
  /// 단일 플레이어(호스트 단독)에서는 서버=클라 이므로 동작이 기존과 동일하다.
  /// </summary>
  public sealed class ScenarioNetworkRelay : NetworkBehaviour
  {
    private static ScenarioNetworkRelay _instance;

    /// <summary>씬에 배치된 중계기 인스턴스(없으면 null).</summary>
    public static ScenarioNetworkRelay Instance => _instance;

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

    /// <summary>
    /// 신호를 권위적으로 올린다. 서버면 즉시 기록, 클라이언트면 서버로 보고한다.
    /// 중계기가 없거나 네트워크가 비활성이면 로컬에 기록(단일 플레이어/오프라인 폴백).
    /// </summary>
    public static void RaiseAuthoritative(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return;
      }

      // 서버 컨텍스트: 직접 권위 기록.
      if (InstanceFinder.IsServerStarted)
      {
        ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
        return;
      }

      // 클라이언트 컨텍스트: 중계기로 서버 보고.
      if (_instance != null && InstanceFinder.IsClientStarted)
      {
        _instance.CmdRaiseScenarioSignal(normalizedSignalId);
        return;
      }

      // 네트워크 비활성/중계기 부재: 로컬 폴백.
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
    }

    /// <summary>신호를 권위적으로 내린다(사이클 반복 등에서 재설정).</summary>
    public static void ClearAuthoritative(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return;
      }

      if (InstanceFinder.IsServerStarted)
      {
        ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
        return;
      }

      if (_instance != null && InstanceFinder.IsClientStarted)
      {
        _instance.CmdClearScenarioSignal(normalizedSignalId);
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRaiseScenarioSignal(string normalizedSignalId)
    {
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdClearScenarioSignal(string normalizedSignalId)
    {
      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }
  }
}
