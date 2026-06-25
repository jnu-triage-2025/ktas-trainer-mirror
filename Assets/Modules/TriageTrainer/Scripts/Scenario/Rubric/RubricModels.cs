using System;
using System.Collections.Generic;

namespace TriageTrainer.Scenario.Rubric
{
  /// <summary>
  /// 평가 루브릭의 ABCDE 영역. 원본 루브릭의 영역 구분에 대응한다.
  /// </summary>
  public enum RubricArea
  {
    /// <summary>분류/초기 대응 등 ABCDE 이전 단계.</summary>
    Triage,
    Airway,
    Breathing,
    Circulation,
    Disability,
    Exposure,
    /// <summary>위 영역에 속하지 않는 일반/기타 항목.</summary>
    General
  }

  /// <summary>
  /// 한 루브릭 항목의 수행 판정 상태.
  /// </summary>
  public enum RubricStatus
  {
    /// <summary>아직 판정되지 않음(세션 진행 중 기본값).</summary>
    Pending,
    /// <summary>수행함.</summary>
    Performed,
    /// <summary>미수행(게이트 타임아웃 강제진행/실패분기 또는 관찰자 표기).</summary>
    NotPerformed,
    /// <summary>해당 없음.</summary>
    NotApplicable
  }

  /// <summary>
  /// 루브릭 항목 1개의 정의. 시나리오 게이트(Validator 노드/신호)나 사정 퀴즈(Choice)와 연결된다.
  /// 자동 판정이 불가능한 항목(예: 의사 전달 pass_*, 신체 사정)은 매핑 필드를 비워 두고
  /// 관찰자 수동 체크 대상으로 둔다.
  /// </summary>
  [Serializable]
  public sealed class RubricItemDefinition
  {
    /// <summary>항목 고유 ID(예: "rubric.circulation.stop_bleeding").</summary>
    public string Id { get; set; }

    /// <summary>ABCDE 영역.</summary>
    public RubricArea Area { get; set; } = RubricArea.General;

    /// <summary>표시 제목(예: "지혈(거즈 압박)").</summary>
    public string Title { get; set; }

    /// <summary>
    /// 자동 수행 판정용 시나리오 노드 식별자(Validator 게이트의 Identifier). 비어 있으면
    /// 노드 기반 자동 판정을 하지 않는다(수동 또는 신호 기반).
    /// </summary>
    public string GateNodeIdentifier { get; set; }

    /// <summary>
    /// 자동 수행 판정용 인터랙션 신호 조건명(접두사 sig. 제외, 예: "apply_gauze"). 비어 있으면
    /// 신호 기반 자동 판정을 하지 않는다.
    /// </summary>
    public string AutoSignal { get; set; }

    /// <summary>플레이어별로 개별 기록할 항목인지(true) 팀 단위 1건인지(false).</summary>
    public bool PerPlayer { get; set; }
  }

  /// <summary>
  /// 한 항목에 대한 한 대상(플레이어 또는 팀)의 기록 결과.
  /// </summary>
  [Serializable]
  public sealed class RubricResult
  {
    public string SessionId { get; set; }

    /// <summary>대상 플레이어 식별자. 팀 단위(PerPlayer=false) 항목이면 null.</summary>
    public string PlayerId { get; set; }

    public string ItemId { get; set; }
    public RubricStatus Status { get; set; } = RubricStatus.Pending;

    /// <summary>사정 퀴즈 등에서의 오답/재응시 누적 횟수.</summary>
    public int Retries { get; set; }

    /// <summary>마지막 갱신 시각(UTC ISO-8601).</summary>
    public string UpdatedAtUtc { get; set; }

    /// <summary>판정 근거 메모(예: "gate timeout: ForceAdvance", "signal raised", "manual").</summary>
    public string Note { get; set; }
  }

  /// <summary>
  /// 루브릭 정의 묶음(데이터팩 JSON 역직렬화 대상).
  /// </summary>
  [Serializable]
  public sealed class RubricDefinitionSet
  {
    public string Identifier { get; set; }
    public List<RubricItemDefinition> Items { get; set; } = new List<RubricItemDefinition>();
  }
}
