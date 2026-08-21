using System;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 도메인 인터랙션 "완료 신호"를 다루는 얇은 헬퍼.
  ///
  /// 설계 메모(TODO-SPEC-2):
  /// - 엔진의 Validator 는 RegistryContains 조건 + RegistryType.RuntimeState 로
  ///   "특정 식별자가 등록되어 있는가"를 이미 검사할 수 있다. 따라서 별도의 신규
  ///   ScenarioValidatorCondition/RuleType 없이도 인터랙션 게이팅이 가능하다.
  /// - 이 클래스는 그 위에 의도를 드러내는 얇은 래퍼만 제공한다(신규 저장소 없음).
  ///   게임플레이 코드가 인터랙션 완료 시 Raise(signalId) 를 호출하고,
  ///   시나리오 JSON 은 Validator(RegistryContains, RuntimeState, signalId) 로 검사한다.
  ///
  /// 변환 규칙(요약): JSON 의 `todo.validate.&lt;cond&gt;` (InvokeEvent 스텁) 은
  ///   Validator 노드(condition=RegistryContains, rule.registryType=RuntimeState,
  ///   rule.registryIdentifier=`sig.&lt;cond&gt;`) 로 치환한다.
  /// </summary>
  public static class ScenarioInteractionSignals
  {
    /// <summary>RuntimeState 레지스트리에서 신호 식별자에 적용할 접두사.</summary>
    public const string Prefix = "sig.";

    /// <summary>
    /// 신호가 로컬 레지스트리에 기록될 때 발생한다(정규화된 식별자 전달).
    /// 루브릭 기록 등 신호 관찰자가 게이트 타임아웃 이후의 수행도 추적할 수 있게 한다.
    /// </summary>
    public static event Action<string> OnSignalRegistered;

    /// <summary>
    /// 신호가 로컬 레지스트리에서 내려갈 때 발생한다(정규화된 식별자 전달).
    /// 신호의 누적 상태를 따로 들고 있는 관찰자(시그널 카운터 등)가 내려간 신호를 반영할 수 있게 한다.
    /// </summary>
    public static event Action<string> OnSignalCleared;

    /// <summary>신호 식별자를 정규화한다(접두사 보장).</summary>
    public static string Normalize(string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return signalId;
      }

      signalId = signalId.Trim();
      return signalId.StartsWith(Prefix, StringComparison.Ordinal) ? signalId : Prefix + signalId;
    }

    /// <summary>
    /// 인터랙션 완료 신호를 올린다.
    /// 서버 권한(authoritative) 경로로 라우팅한다: 서버면 직접 RuntimeState 에 기록하고,
    /// 클라이언트면 <see cref="ScenarioNetworkRelay"/> 를 통해 서버로 보고한다(G-8 P1).
    /// 네트워크가 비활성이거나 중계기가 없으면 로컬에 기록(단일 플레이어/오프라인).
    /// </summary>
    public static void Raise(string signalId)
      => Raise(signalId, null);

    /// <summary>
    /// JSON 문자열 파라미터와 함께 인터랙션 완료 신호를 올린다.
    /// 네트워크 세션에서는 서버가 JSON 문법을 검증하고 발신 플레이어별 마지막 값을 권위적으로 기록한다.
    /// </summary>
    public static void Raise(string signalId, string parameterJson)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return;
      }

      ScenarioNetworkRelay.RaiseAuthoritative(Normalize(signalId), parameterJson);
    }

    /// <summary>신호를 내린다. 서버 권한 경로로 라우팅한다(사이클 반복 등에서 재설정 시 사용).</summary>
    public static void Clear(string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return;
      }

      ScenarioNetworkRelay.ClearAuthoritative(Normalize(signalId));
    }

    /// <summary>
    /// 서버 내부 신호를 수신 대기 등록한다.
    /// targetId 는 `@m`(서버) 또는 `@s`(self) 같은 서버 내부 목적지 식별자일 수 있다.
    /// </summary>
    public static bool RegisterInternal(string targetId, string signalId, Action onResolved)
      => ScenarioServerInternalSignalRegistry.Register(targetId, signalId, onResolved);

    /// <summary>
    /// 서버 내부 신호를 resolve 한다.
    /// targetId 는 `@m`(서버) 또는 `@s`(self) 같은 서버 내부 목적지 식별자일 수 있다.
    /// </summary>
    public static bool ResolveInternal(string targetId, string signalId)
      => ScenarioServerInternalSignalRegistry.Resolve(targetId, signalId);

    /// <summary>
    /// 특정 대상/신호의 내부 신호 상태를 제거한다.
    /// </summary>
    public static void ClearInternal(string targetId, string signalId)
      => ScenarioServerInternalSignalRegistry.Clear(targetId, signalId);

    /// <summary>
    /// 내부 신호 레지스트리 전체를 제거한다.
    /// 시나리오 시작/종료 시점에 호출하여 이전 세션 상태가 섞이지 않도록 한다.
    /// </summary>
    public static void ClearAllInternalSignals()
      => ScenarioServerInternalSignalRegistry.ClearAll();

    /// <summary>
    /// 정규화된 신호를 로컬 RuntimeState 레지스트리에 직접 등록한다.
    /// 권한 라우팅을 거치지 않으므로, 서버 컨텍스트 또는 중계기 내부에서만 호출해야 한다.
    /// </summary>
    internal static void RegisterLocal(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return;
      }

      // 상태 전이 여부를 먼저 판정한다. 서버 권위 기록 후 미러 ObserversRpc 가 호스트 로컬에서도
      // 실행되어 동일 신호가 연달아 두 번 기록될 수 있는데(서버=클라), 이미 올라간 신호에 대해
      // 로그/이벤트를 재발생시키면 조건부 리스너의 중복 Raise 나 로그 중복이 발생한다.
      // Registry.Register 자체는 멱등하므로 항상 호출해 각 피어 레지스트리 동기화는 유지하되,
      // GameLogService/OnSignalRegistered 는 최초 전이에서만 발생시킨다.
      bool wasAlreadyRaised = Registry.Registry.Contains(RegistryType.RuntimeState, normalizedSignalId);
      Registry.Registry.Register(RegistryType.RuntimeState, normalizedSignalId, true);
      if (wasAlreadyRaised)
      {
        return;
      }

      GameLogService.WriteSignal($"Signal raised: {normalizedSignalId}", normalizedSignalId);
      OnSignalRegistered?.Invoke(normalizedSignalId);
    }

    /// <summary>정규화된 신호를 로컬 RuntimeState 레지스트리에서 직접 제거한다(권한 라우팅 미경유).</summary>
    internal static void UnregisterLocal(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return;
      }

      Registry.Registry.Unregister(RegistryType.RuntimeState, normalizedSignalId);
      GameLogService.WriteSignal($"Signal cleared: {normalizedSignalId}", normalizedSignalId);

      // 이 신호를 내보내던 1회성 엔티티 상태 바인딩은 이미 소비되어 등록이 해제돼 있다.
      // 신호를 내린 뒤에도 발신자가 남아 있어야 게이트가 다시 열릴 수 있으므로 재무장한다.
      ScenarioEntityStateSignalBindings.RearmConsumedBindingsForSignal(normalizedSignalId);
      OnSignalCleared?.Invoke(normalizedSignalId);
    }

    /// <summary>
    /// 현재 실행에 속한 일회성 gameplay signal을 모두 제거한다.
    /// RuntimeState는 sticky 저장소이므로 시나리오 재실행 전에 반드시 호출해야 한다.
    /// </summary>
    public static void ClearAllRaisedSignals()
    {
      var all = Registry.Registry.GetAll<bool>(RegistryType.RuntimeState);
      if (all == null) return;
      foreach (var pair in all)
      {
        if (pair.Value && pair.Key != null && pair.Key.StartsWith(Prefix, StringComparison.Ordinal))
          UnregisterLocal(pair.Key);
      }
    }

    /// <summary>신호가 올라가 있는지 조회한다(Validator 의 RegistryContains 와 동일 기준).</summary>
    public static bool IsRaised(string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return false;
      }

      return Registry.Registry.Contains(RegistryType.RuntimeState, Normalize(signalId));
    }

    /// <summary>
    /// 정규화된 접두사(<paramref name="normalizedPrefix"/>)로 시작하며 현재 올라가 있는(raised)
    /// 신호 식별자들을 반환한다. 시그널 카운터의 초기 카운트 산정에 사용한다.
    /// </summary>
    internal static System.Collections.Generic.IEnumerable<string> GetRaisedSignalsWithPrefix(string normalizedPrefix)
    {
      if (string.IsNullOrWhiteSpace(normalizedPrefix))
      {
        yield break;
      }

      var all = Registry.Registry.GetAll<bool>(RegistryType.RuntimeState);
      if (all == null)
      {
        yield break;
      }

      foreach (var pair in all)
      {
        if (pair.Value && pair.Key != null && pair.Key.StartsWith(normalizedPrefix, StringComparison.Ordinal))
        {
          yield return pair.Key;
        }
      }
    }
  }
}
