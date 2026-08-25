using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 환자의 의료 상태를 표현합니다.
  ///
  /// <para>
  /// <b>시나리오 프리셋 지원:</b> 이 클래스의 모든 필드는
  /// <see cref="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode"/> 를 통해
  /// 시나리오 JSON에서 초기값(프리셋)을 설정할 수 있습니다.
  /// </para>
  ///
  /// <para>새 필드를 추가할 때 프리셋 지원이 필요하면 다음 네 곳에 동일하게 추가하세요:
  /// <list type="number">
  /// <item><see cref="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode"/> — nullable 프로퍼티</item>
  /// <item><see cref="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNodeDTO"/> — nullable JSON 프로퍼티</item>
  /// <item><c>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</c> — DTO → 도메인 매핑</item>
  /// <item><c>PatientController.ApplyMedicalStatePreset</c> — 도메인 → PatientController 적용</item>
  /// </list>
  /// </para>
  /// </summary>
  [Serializable]
  public class PatientMedicalState
  {
    /// <summary>
    /// 모니터 수치 필드(numerics.bpm/pulseRate, nibp.systolic/diastolic 등)에서
    /// "측정 불가 / 무의식 / 호흡 없음"을 나타내는 센티넬 값.
    /// 이 값이 설정되면 환자 상태 모니터에는 해당 수치가 <c>-?-</c> 로 표시된다.
    /// 프리셋 노드에서 수치 필드에 -1을 지정하면 이 값으로 매핑된다.
    /// </summary>
    public const float MonitorValueUnavailable = -1f;

    [Header("Medical State")]
    // 프리셋 지원: ScenarioPatientMedicalStatePresetNode.BloodPressureSystolic / BloodPressureDiastolic
    public BloodPressure bloodPressure;
    // 프리셋 지원: ScenarioPatientMedicalStatePresetNode.PulseRate / PulseForceType
    public BloodPulse pulse;
    // 프리셋 지원: ScenarioPatientMedicalStatePresetNode.SkinColorHue / SkinTemperatureType
    public Skin skin = Skin.Default;
    public BodyTemperature bodyTemperature;
    public List<HealthProblem> healthProblem = new();
    // 프리셋 지원: ScenarioPatientMedicalStatePresetNode.ConsciousnessGcs / ConsciousnessLocLabel / ConsciousnessPupillaryResponse
    public Consciousness consciousness = Consciousness.Default;
    // 프리셋 지원: ScenarioPatientMedicalStatePresetNode.RespirationAwRR / RespirationTypeValue
    public Respiration respiration;
    public List<RequiredDrug> requiredDrugs = new();
    // 프리셋 지원: ScenarioPatientMedicalStatePresetNode.IsCardiacArrest
    public bool isCardiacArrest;

    [Header("Medical State/Monitor")]
    public ECGParameters ecg = ECGParameters.Normal;
    public ARTParameters art = ARTParameters.Default;
    public CVPParameters cvp = CVPParameters.Default;
    public PlethParameters pleth = PlethParameters.Default;
    public NumericsParameters numerics = NumericsParameters.Default;
    public NIBPParameters nibp = NIBPParameters.Default;
    public TemperatureParameters temperature = TemperatureParameters.Default;
    public STLeadValues stLeads = STLeadValues.Default;

    public void CopyFrom(PatientMedicalState source)
    {
      if (source == null)
        return;

      bloodPressure = source.bloodPressure;
      pulse = source.pulse;
      skin = source.skin;
      bodyTemperature = source.bodyTemperature;
      healthProblem = source.healthProblem != null ? new List<HealthProblem>(source.healthProblem) : new List<HealthProblem>();
      consciousness = source.consciousness;
      respiration = source.respiration;
      requiredDrugs = source.requiredDrugs != null ? new List<RequiredDrug>(source.requiredDrugs) : new List<RequiredDrug>();
      isCardiacArrest = source.isCardiacArrest;

      ecg = source.ecg;
      art = source.art;
      cvp = source.cvp;
      pleth = source.pleth;
      numerics = source.numerics;
      nibp = source.nibp;
      temperature = source.temperature;
      stLeads = source.stLeads;
    }

    public PatientMedicalState Clone()
    {
      var clone = new PatientMedicalState();
      clone.CopyFrom(this);
      return clone;
    }
  }
}
