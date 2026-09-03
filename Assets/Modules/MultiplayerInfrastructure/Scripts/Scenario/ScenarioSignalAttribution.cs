using System;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 신호를 "누가 올렸는가"까지 포함해 판정하는 헬퍼.
  ///
  /// <para><see cref="ScenarioInteractionSignals"/>가 기록하는 RuntimeState 항목은 신호별로 하나뿐이므로,
  /// 그 항목만 검사하면 어느 참여자가 올린 신호인지 구분할 수 없다. 여러 참여자가 같은 퀘스트를
  /// 들고 있거나 같은 월드 오브젝트를 조작할 수 있는 멀티플레이 세션에서는, 한 참여자의 행동이
  /// 다른 참여자의 목표까지 완료 처리하는 교차 완료가 발생한다. 이 타입은
  /// <see cref="ScenarioSignalParameterStore"/>가 이미 보관하고 있는 (신호, 발신 플레이어) 귀속 기록을
  /// 사용해 그 판정을 참여자 단위로 좁힌다.</para>
  ///
  /// <para><b>진행을 막지 않는다는 원칙:</b> 귀속 기록을 확인할 수 없는 상황에서는 항상 전역 신호
  /// 판정으로 물러선다. 귀속 정보가 없다는 이유로 게이트나 목표가 영구히 미완료로 남으면 세션 전체가
  /// 멈추기 때문이다. 구체적으로는 소유자 식별자를 알 수 없을 때, 저장소 한도 초과나 미러 유실로 해당
  /// 신호의 귀속 기록이 아예 없을 때, 서버·명령 같은 시스템 주체가 올린 신호일 때 전역 판정을 따른다.</para>
  /// </summary>
  public static class ScenarioSignalAttribution
  {
    /// <summary>정규화된 신호가 전역 RuntimeState 레지스트리에 올라와 있는지 확인한다.</summary>
    public static bool IsRaised(string signalId)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signalId);
      return !string.IsNullOrWhiteSpace(normalized)
        && Registry.Registry.Contains(RegistryType.RuntimeState, normalized);
    }

    /// <summary>
    /// 지정한 플레이어가 올린 신호인지 확인한다.
    /// 귀속을 확인할 수 없는 경우에는 <see cref="IsRaised"/>의 결과를 그대로 사용한다.
    /// </summary>
    /// <param name="signalId">신호 식별자입니다. 접두사가 없으면 정규화됩니다.</param>
    /// <param name="playerIdentifier">판정 기준이 되는 플레이어의 UserDescriptor 식별자입니다.</param>
    public static bool IsRaisedBy(string signalId, string playerIdentifier)
    {
      if (!IsRaised(signalId))
      {
        // 신호 자체가 올라오지 않았다면 귀속을 따질 필요가 없다.
        return false;
      }

      if (string.IsNullOrWhiteSpace(playerIdentifier))
      {
        // 판정 기준이 되는 플레이어를 특정할 수 없으므로 전역 판정을 따른다.
        return true;
      }

      if (!ScenarioSignalParameterStore.HasAnyRecord(signalId))
      {
        // 신호는 올라왔지만 귀속 기록이 남지 않은 경우다(저장소 한도 초과, 미러 유실 등).
        return true;
      }

      if (ScenarioSignalParameterStore.TryGetForPlayer(signalId, playerIdentifier, out _))
      {
        return true;
      }

      // 서버·명령처럼 특정 참여자에게 귀속되지 않는 주체가 올린 신호는 모든 참여자에게 적용한다.
      return ScenarioSignalParameterStore.TryGetForPlayer(
        signalId, ScenarioSignalParameterStore.ServerPlayerIdentifier, out _);
    }

    /// <summary>
    /// 신호 판정 범위에 따라 <see cref="IsRaised"/>와 <see cref="IsRaisedBy"/> 중 하나를 적용한다.
    /// </summary>
    public static bool IsSatisfied(string signalId, ScenarioSignalScope scope, string playerIdentifier)
      => scope == ScenarioSignalScope.Owner
        ? IsRaisedBy(signalId, playerIdentifier)
        : IsRaised(signalId);
  }

  /// <summary>신호 판정을 참여자 단위로 좁힐지 결정하는 범위입니다.</summary>
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum ScenarioSignalScope
  {
    /// <summary>누가 올린 신호인지 구분하지 않습니다. 팀 공동 목표의 기본값입니다.</summary>
    Any,

    /// <summary>판정 대상 참여자가 직접 올린 신호만 인정합니다.</summary>
    Owner
  }
}
