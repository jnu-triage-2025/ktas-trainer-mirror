using System;
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

    /// <summary>인터랙션 완료 신호를 올린다(RuntimeState 레지스트리에 등록).</summary>
    public static void Raise(string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return;
      }

      Registry.Registry.Register(RegistryType.RuntimeState, Normalize(signalId), true);
    }

    /// <summary>신호를 내린다(RuntimeState 레지스트리에서 제거). 사이클 반복 등에서 재설정 시 사용.</summary>
    public static void Clear(string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return;
      }

      Registry.Registry.Unregister(RegistryType.RuntimeState, Normalize(signalId));
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
  }
}
