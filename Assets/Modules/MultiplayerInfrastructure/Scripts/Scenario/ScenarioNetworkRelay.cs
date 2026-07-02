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

      // 서버 컨텍스트: 직접 권위 기록 후 전체 클라이언트에 미러링.
      // (Validator 판정은 각 피어 로컬에서 폴링되므로, 서버 기록만으로는
      //  다른 클라이언트의 게이트가 통과되지 않는다.)
      if (InstanceFinder.IsServerStarted)
      {
        ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
        if (_instance != null)
        {
          _instance.RpcMirrorRaiseScenarioSignal(normalizedSignalId);
        }
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
        if (_instance != null)
        {
          _instance.RpcMirrorClearScenarioSignal(normalizedSignalId);
        }
        return;
      }

      if (_instance != null && InstanceFinder.IsClientStarted)
      {
        _instance.CmdClearScenarioSignal(normalizedSignalId);
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }

    /// <summary>신호 식별자 최대 길이(자원 고갈 방지용 방어선).</summary>
    private const int MaxSignalIdentifierLength = 256;

    /// <summary>
    /// 클라이언트가 보고한 신호 식별자의 서버측 검증.
    /// 정규화 접두사(sig.)와 길이 상한을 강제하여, 임의 문자열 주입으로 인한
    /// 레지스트리 오염/무한 증식(1→N 증폭)을 차단한다.
    /// </summary>
    private static bool IsValidClientSignal(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return false;
      }

      if (normalizedSignalId.Length > MaxSignalIdentifierLength)
      {
        return false;
      }

      return normalizedSignalId.StartsWith(ScenarioInteractionSignals.Prefix, System.StringComparison.Ordinal);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRaiseScenarioSignal(string normalizedSignalId)
    {
      if (!IsValidClientSignal(normalizedSignalId))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected invalid client signal raise: '{normalizedSignalId}'");
        return;
      }

      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
      // 서버 기록 후 모든 클라이언트(호스트 포함)에 미러링하여
      // 각 피어 로컬의 Validator 폴링이 통과되도록 한다.
      RpcMirrorRaiseScenarioSignal(normalizedSignalId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdClearScenarioSignal(string normalizedSignalId)
    {
      if (!IsValidClientSignal(normalizedSignalId))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected invalid client signal clear: '{normalizedSignalId}'");
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
      RpcMirrorClearScenarioSignal(normalizedSignalId);
    }

    /// <summary>
    /// 서버의 권위 신호 기록을 모든 클라이언트 로컬 레지스트리로 미러링한다.
    /// </summary>
    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorRaiseScenarioSignal(string normalizedSignalId)
    {
      // 호스트(서버=클라)에서는 이미 서버 경로에서 기록되었으므로 중복 기록해도 무해(idempotent)하다.
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorClearScenarioSignal(string normalizedSignalId)
    {
      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }
  }
}
