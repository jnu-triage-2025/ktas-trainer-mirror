using System;

namespace MultiplayerInfrastructure.Scenario.Preflight
{
  using MultiplayerInfrastructure.Scenario.Requirements;
  /// <summary>누락(Missing) 요구가 있을 때 시나리오 시작을 어떻게 처리할지에 대한 정책.</summary>
  public enum ScenarioPreflightMissingBehavior
  {
    /// <summary>경고만 남기고 시나리오를 정상 시작한다(기본, 기존 무중단 철학과 일치).</summary>
    ContinueWithWarning,

    /// <summary>누락이 1건 이상이면 시나리오를 시작하지 않고 중단한다.</summary>
    AbortStart
  }

  /// <summary>
  /// 사전 요구사항 검증의 경고 채널과 누락 시 행동을 담는 정책 값.
  /// 인스펙터에서 설정할 수 있도록 직렬화 가능하다.
  /// 기본값: 콘솔/인게임챗 경고 둘 다 켬, 누락 시 경고 후 계속 진행.
  /// </summary>
  [Serializable]
  public struct ScenarioPreflightPolicy
  {
    [UnityEngine.Tooltip("Unity 콘솔에 경고를 출력한다.")]
    public bool WarnToConsole;

    [UnityEngine.Tooltip("인게임 채팅에 경고를 출력한다.")]
    public bool WarnToInGameChat;

    [UnityEngine.Tooltip("누락이 있을 때의 행동(경고 후 계속 / 시작 중단).")]
    public ScenarioPreflightMissingBehavior MissingBehavior;

    public ScenarioRuntimeValidationMode RuntimeValidationMode
    {
      get => MissingBehavior == ScenarioPreflightMissingBehavior.AbortStart
        ? ScenarioRuntimeValidationMode.AbortScenarioStart
        : ScenarioRuntimeValidationMode.ReportOnly;
    }

    /// <summary>기본 정책: 두 채널 모두 경고 + 누락이어도 계속 진행.</summary>
    public static ScenarioPreflightPolicy Default => new ScenarioPreflightPolicy
    {
      WarnToConsole = true,
      WarnToInGameChat = true,
      MissingBehavior = ScenarioPreflightMissingBehavior.ContinueWithWarning
    };
  }
}
