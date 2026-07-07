using TriageTrainer.Entity.Patient;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 환자 엔티티의 의료 상태(PatientDescriptor 및 PatientMedicalState)를 일괄 초기화(프리셋)하는 노드.
  ///
  /// <para>
  /// 지정한 엔티티 식별자(<see cref="TargetEntityIdentifier"/>)에 해당하는 <see cref="TriageTrainer.Entity.PatientController"/>를
  /// 찾아 아래 필드를 덮어쓴다. <b>null 인 항목은 현재 값을 유지</b>하므로,
  /// 설정이 필요한 필드만 기입하면 된다.
  /// </para>
  ///
  /// <para>새 환자 상태 필드를 추가할 때도 이 노드를 통해 프리셋 값을 설정할 수 있다.
  /// 아래 프로퍼티 목록에 필드를 추가하고, DTO(<see cref="ScenarioPatientMedicalStatePresetNodeDTO"/>),
  /// 로더(<c>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</c>),
  /// 컨트롤러(<c>PatientController.ApplyMedicalStatePreset</c>) 세 곳에도 동일하게 추가한다.</para>
  ///
  /// <para>
  /// ── 프리셋 가능 필드 목록 ──
  /// <list type="table">
  /// <listheader><term>필드 그룹</term><term>프로퍼티</term><term>타입</term></listheader>
  /// <item><term>환자 기술자</term>    <term><see cref="Sex"/></term>              <term>Sex?</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="Age"/></term>              <term>int?</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="Name"/></term>             <term>string</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="BloodType"/></term>        <term>BloodType?</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="IntendedTriage"/></term>   <term>TriageLevel?</term></item>
  /// <item><term>의료 상태</term>      <term><see cref="ConsciousnessGcs"/></term> <term>int?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessEyeOpening"/></term><term>EyeOpeningResponse?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessVerbal"/></term><term>VerbalResponse?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessMotor"/></term><term>MotorResponse?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessLocLabel"/></term><term>LOCLabel?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessPupillaryResponse"/></term><term>PupillaryResponse?</term></item>
  /// <item><term>의료 상태/호흡</term> <term><see cref="RespirationAwRR"/></term>  <term>int?</term></item>
  /// <item><term>의료 상태/호흡</term> <term><see cref="RespirationTypeValue"/></term><term>RespirationType?</term></item>
  /// <item><term>의료 상태/맥박</term> <term><see cref="PulseRate"/></term>        <term>int?</term></item>
  /// <item><term>의료 상태/맥박</term> <term><see cref="PulseForceType"/></term>   <term>BloodPulseForceType?</term></item>
  /// <item><term>의료 상태/혈압</term> <term><see cref="BloodPressureSystolic"/></term><term>int?</term></item>
  /// <item><term>의료 상태/혈압</term> <term><see cref="BloodPressureDiastolic"/></term><term>int?</term></item>
  /// <item><term>의료 상태/피부</term> <term><see cref="SkinColorHue"/></term>     <term>SkinColorHue?</term></item>
  /// <item><term>의료 상태/피부</term> <term><see cref="SkinTemperatureType"/></term><term>SkinTemperatureType?</term></item>
  /// <item><term>의료 상태</term>      <term><see cref="IsCardiacArrest"/></term>  <term>bool?</term></item>
  /// </list>
  /// </para>
  /// </summary>
  public sealed class ScenarioPatientMedicalStatePresetNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.PatientMedicalStatePreset;
    public string NextIdentifier { get; set; }

    // ── 대상 엔티티 ──

    /// <summary>
    /// 상태를 초기화할 환자 엔티티 식별자.
    /// 비어 있으면 상태 저장소의 <see cref="TargetEntityStateKey"/> 값을 사용한다.
    /// </summary>
    public string TargetEntityIdentifier { get; set; }

    /// <summary>
    /// 상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키.
    /// <see cref="TargetEntityIdentifier"/> 가 비어 있을 때만 사용된다.
    /// </summary>
    public string TargetEntityStateKey { get; set; }

    // ── 환자 기술자(PatientDescriptor) 프리셋 필드 ──
    // 새 PatientDescriptor 필드 추가 시 아래에 nullable 프로퍼티를 추가하고,
    // DTO / ScenarioGraphLoader.ConvertPatientMedicalStatePreset / PatientController.ApplyMedicalStatePreset
    // 세 곳에도 동일하게 추가한다.

    /// <summary>환자 성명. null이면 현재 값 유지.</summary>
    public string Name { get; set; }

    /// <summary>환자 성별(Sex.Male / Sex.Female). null이면 현재 값 유지.</summary>
    public Sex? Sex { get; set; }

    /// <summary>환자 나이. null이면 현재 값 유지.</summary>
    public int? Age { get; set; }

    /// <summary>환자 혈액형. null이면 현재 값 유지.</summary>
    public BloodType? BloodType { get; set; }

    /// <summary>의도된(정답) 트리아지 등급. null이면 현재 값 유지.</summary>
    public TriageLevel? IntendedTriage { get; set; }

    // ── 의료 상태(PatientMedicalState) 프리셋 필드 ──
    // 새 PatientMedicalState 필드 추가 시 아래에 nullable 프로퍼티를 추가하고,
    // DTO / ScenarioGraphLoader.ConvertPatientMedicalStatePreset / PatientController.ApplyMedicalStatePreset
    // 세 곳에도 동일하게 추가한다.

    // ── 의식(Consciousness) ──

    /// <summary>GCS 점수(3~15). null이면 현재 값 유지.</summary>
    public int? ConsciousnessGcs { get; set; }

    /// <summary>GCS의 E(Eye Opening, 눈뜨기 반응) 세부 항목(1~4점). null이면 현재 값 유지.</summary>
    public EyeOpeningResponse? ConsciousnessEyeOpening { get; set; }

    /// <summary>GCS의 V(Verbal Response, 언어 반응) 세부 항목(1~5점). null이면 현재 값 유지.</summary>
    public VerbalResponse? ConsciousnessVerbal { get; set; }

    /// <summary>GCS의 M(Motor Response, 운동 반응) 세부 항목(1~6점). null이면 현재 값 유지.</summary>
    public MotorResponse? ConsciousnessMotor { get; set; }

    /// <summary>의식수준 5단계(LOC). null이면 현재 값 유지.</summary>
    public LOCLabel? ConsciousnessLocLabel { get; set; }

    /// <summary>동공 반사 상태. null이면 현재 값 유지.</summary>
    public PupillaryResponse? ConsciousnessPupillaryResponse { get; set; }

    // ── 호흡(Respiration) ──

    /// <summary>분당 호흡수(awRR). null이면 현재 값 유지.</summary>
    public int? RespirationAwRR { get; set; }

    /// <summary>호흡 유형. null이면 현재 값 유지.</summary>
    public RespirationType? RespirationTypeValue { get; set; }

    // ── 맥박(BloodPulse) ──

    /// <summary>분당 맥박수. null이면 현재 값 유지.</summary>
    public int? PulseRate { get; set; }

    /// <summary>맥박 세기 유형. null이면 현재 값 유지.</summary>
    public BloodPulseForceType? PulseForceType { get; set; }

    // ── 혈압(BloodPressure) ──

    /// <summary>수축기 혈압(mmHg). null이면 현재 값 유지.</summary>
    public int? BloodPressureSystolic { get; set; }

    /// <summary>이완기 혈압(mmHg). null이면 현재 값 유지.</summary>
    public int? BloodPressureDiastolic { get; set; }

    // ── 피부(Skin) ──

    /// <summary>피부 색조. null이면 현재 값 유지.</summary>
    public SkinColorHue? SkinColorHue { get; set; }

    /// <summary>피부 표면 온도 유형. null이면 현재 값 유지.</summary>
    public SkinTemperatureType? SkinTemperatureType { get; set; }

    // ── 기타 의료 상태 ──

    /// <summary>심정지 여부. null이면 현재 값 유지.</summary>
    public bool? IsCardiacArrest { get; set; }
  }
}
