using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <see cref="ScenarioPatientMedicalStatePresetNode"/> 의 JSON 직렬화 DTO.
  ///
  /// <para>
  /// 모든 의료 상태 필드는 nullable로 선언되어 있으므로 JSON에서 생략하면 해당 필드는
  /// 현재 값을 그대로 유지한다. 설정이 필요한 필드만 기입하면 된다.
  /// </para>
  ///
  /// <para>새 PatientDescriptor / PatientMedicalState 필드 추가 시 이 DTO에도 nullable 프로퍼티를 추가하고,
  /// <see cref="ScenarioPatientMedicalStatePresetNode"/>, ScenarioGraphLoader.ConvertPatientMedicalStatePreset,
  /// PatientController.ApplyMedicalStatePreset 세 곳에도 동일하게 추가한다.</para>
  ///
  /// <code>
  /// // JSON 사용 예시 (환자 A 중증 프리셋):
  /// {
  ///   "identifier": "PRESET_PATIENT_A",
  ///   "nodeType": "PatientMedicalStatePreset",
  ///   "targetEntityIdentifier": "patient_a",
  ///   "sex": "Male",
  ///   "age": 35,
  ///   "consciousnessGcs": 8,
  ///   "consciousnessLocLabel": "Stupor",
  ///   "consciousnessPupillaryResponse": "Normal",
  ///   "respirationAwRR": 8,
  ///   "respirationType": "Irregular",
  ///   "pulseRate": 140,
  ///   "pulseForceType": "Weak",
  ///   "bloodPressureSystolic": 70,
  ///   "bloodPressureDiastolic": 40,
  ///   "skinColorHue": "Pale",
  ///   "skinTemperatureType": "Cold",
  ///   "isCardiacArrest": false,
  ///   "nextIdentifier": "D005"
  /// }
  /// </code>
  /// </summary>
  internal sealed class ScenarioPatientMedicalStatePresetNodeDTO : ScenarioNodeDTO
  {
    // ── 대상 엔티티 ──

    /// <summary>상태를 초기화할 환자 엔티티 식별자. 비어 있으면 targetEntityStateKey를 사용한다.</summary>
    [JsonPropertyName("targetEntityIdentifier")]
    public string TargetEntityIdentifier { get; set; }

    /// <summary>상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키.</summary>
    [JsonPropertyName("targetEntityStateKey")]
    public string TargetEntityStateKey { get; set; }

    // ── 환자 기술자(PatientDescriptor) 프리셋 필드 ──
    // 새 PatientDescriptor 필드 추가 시 아래에 nullable 프로퍼티를 추가하고,
    // ScenarioPatientMedicalStatePresetNode / ScenarioGraphLoader.ConvertPatientMedicalStatePreset
    // / PatientController.ApplyMedicalStatePreset 세 곳에도 동일하게 추가한다.

    /// <summary>환자 성명. null이면 현재 값 유지.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>환자 성별 문자열 ("Male" / "Female"). null이면 현재 값 유지.</summary>
    [JsonPropertyName("sex")]
    public string Sex { get; set; }

    /// <summary>환자 나이. null이면 현재 값 유지.</summary>
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>혈액형 문자열 (e.g. "A", "B", "AB", "O"). null이면 현재 값 유지.</summary>
    [JsonPropertyName("bloodType")]
    public string BloodType { get; set; }

    /// <summary>의도된(정답) 트리아지 등급 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("intendedTriage")]
    public string IntendedTriage { get; set; }

    // ── 의료 상태(PatientMedicalState) 프리셋 필드 ──
    // 새 PatientMedicalState 필드 추가 시 아래에 nullable 프로퍼티를 추가하고,
    // ScenarioPatientMedicalStatePresetNode / ScenarioGraphLoader.ConvertPatientMedicalStatePreset
    // / PatientController.ApplyMedicalStatePreset 세 곳에도 동일하게 추가한다.

    // ── 의식(Consciousness) ──

    /// <summary>GCS 점수(3~15). null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessGcs")]
    public int? ConsciousnessGcs { get; set; }

    /// <summary>의식수준 5단계(LOC) 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessLocLabel")]
    public string ConsciousnessLocLabel { get; set; }

    /// <summary>동공 반사 상태 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessPupillaryResponse")]
    public string ConsciousnessPupillaryResponse { get; set; }

    // ── 호흡(Respiration) ──

    /// <summary>분당 호흡수(awRR). null이면 현재 값 유지.</summary>
    [JsonPropertyName("respirationAwRR")]
    public int? RespirationAwRR { get; set; }

    /// <summary>호흡 유형 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("respirationType")]
    public string RespirationTypeValue { get; set; }

    // ── 맥박(BloodPulse) ──

    /// <summary>분당 맥박수. null이면 현재 값 유지.</summary>
    [JsonPropertyName("pulseRate")]
    public int? PulseRate { get; set; }

    /// <summary>맥박 세기 유형 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("pulseForceType")]
    public string PulseForceType { get; set; }

    // ── 혈압(BloodPressure) ──

    /// <summary>수축기 혈압(mmHg). null이면 현재 값 유지.</summary>
    [JsonPropertyName("bloodPressureSystolic")]
    public int? BloodPressureSystolic { get; set; }

    /// <summary>이완기 혈압(mmHg). null이면 현재 값 유지.</summary>
    [JsonPropertyName("bloodPressureDiastolic")]
    public int? BloodPressureDiastolic { get; set; }

    // ── 피부(Skin) ──

    /// <summary>피부 색조 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("skinColorHue")]
    public string SkinColorHue { get; set; }

    /// <summary>피부 표면 온도 유형 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("skinTemperatureType")]
    public string SkinTemperatureType { get; set; }

    // ── 기타 의료 상태 ──

    /// <summary>심정지 여부. null이면 현재 값 유지.</summary>
    [JsonPropertyName("isCardiacArrest")]
    public bool? IsCardiacArrest { get; set; }
  }
}
