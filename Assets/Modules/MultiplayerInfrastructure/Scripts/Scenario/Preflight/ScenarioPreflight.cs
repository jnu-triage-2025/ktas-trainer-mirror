using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Preflight
{
  /// <summary>
  /// 시나리오 시작 직전에 수행하는 사전 요구사항 검증의 진입점.
  /// 그래프에서 요구 항목을 수집·검사하고, 정책에 따라 경고(콘솔/인게임챗)를 출력한 뒤
  /// 시나리오를 시작해도 되는지 여부를 돌려준다.
  /// </summary>
  public static class ScenarioPreflight
  {
    /// <summary>
    /// 사전 검증을 실행한다.
    /// </summary>
    /// <param name="graph">검사할 시나리오 그래프.</param>
    /// <param name="policy">경고 채널/누락 시 행동 정책.</param>
    /// <param name="inGameChatWarn">
    /// 인게임 채팅 경고 출력 콜백. null 이면 채팅 경고를 건너뛴다.
    /// (ScenarioController 의 AppendSystemChatMessage 를 연결한다.)
    /// </param>
    /// <param name="report">검사 결과 리포트(진단/테스트용).</param>
    /// <returns>시나리오 시작을 진행해도 되면 true, 정책에 따라 중단해야 하면 false.</returns>
    public static bool Run(
      ScenarioGraph graph,
      ScenarioPreflightPolicy policy,
      Action<string> inGameChatWarn,
      out ScenarioPreflightReport report)
    {
      var requirements = ScenarioRequirementsCollector.Collect(graph);
      report = ScenarioRequirementsChecker.Check(requirements);

      // 누락도 정보성도 없으면 조용히 통과(정상 배선된 본 게임 씬).
      if (!report.HasMissing && report.IndeterminateCount == 0)
      {
        return true;
      }

      var scenarioId = graph != null ? graph.Identifier : "(null)";
      var summary = ScenarioRequirementsChecker.BuildSummary(scenarioId, report);

      // 누락이 있으면 경고, 정보성만 있으면 정보 수준으로 출력한다.
      if (policy.WarnToConsole)
      {
        if (report.HasMissing)
        {
          Debug.LogWarning(summary);
        }
        else
        {
          Debug.Log(summary);
        }
      }

      // 인게임 채팅 경고는 누락이 있을 때만(정보성만 있을 때 채팅을 어지럽히지 않음).
      if (policy.WarnToInGameChat && report.HasMissing)
      {
        inGameChatWarn?.Invoke(summary);
      }

      if (report.HasMissing && policy.MissingBehavior == ScenarioPreflightMissingBehavior.AbortStart)
      {
        var abortMessage = $"[ScenarioPreflight] Aborting scenario '{scenarioId}' start due to {report.MissingCount} missing requirement(s).";
        if (policy.WarnToConsole)
        {
          Debug.LogError(abortMessage);
        }
        if (policy.WarnToInGameChat)
        {
          inGameChatWarn?.Invoke(abortMessage);
        }
        return false;
      }

      return true;
    }
  }
}
