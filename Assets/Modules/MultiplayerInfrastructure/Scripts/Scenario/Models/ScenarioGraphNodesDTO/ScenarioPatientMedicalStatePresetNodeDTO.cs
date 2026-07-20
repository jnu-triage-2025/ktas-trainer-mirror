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
  /// <para>
  /// 수치 필드에 -1을 지정하면 "무의식 / 호흡 없음 / 측정 불가"를 의미하며, 모니터에 <c>-?-</c>로 표시된다.
  /// (null은 "현재 값 유지", -1은 "측정 불가"로 서로 다르다.)
  /// transitionMode를 "Gradual"로 지정하면 transitionDurationSeconds 동안 수치가 점차 변화한다.
  /// </para>
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
  ///   "consciousnessEyeOpening": "ToPressure",
  ///   "consciousnessVerbal": "Sounds",
  ///   "consciousnessMotor": "Withdrawal",
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
  ///   "bodyTemperatureCelsius": 35.9,
  ///   "spo2": 82,
  ///   "isCardiacArrest": false,
  ///   "nextIdentifier": "D005"
  /// }
  ///
  /// // JSON 사용 예시 (심정지 전이 - 3초에 걸쳐 측정 불가로 변화):
  /// {
  ///   "identifier": "PRESET_PEA",
  ///   "nodeType": "PatientMedicalStatePreset",
  ///   "targetEntityIdentifier": "patient_a",
  ///   "transitionMode": "Gradual",
  ///   "transitionDurationSeconds": 3.0,
  ///   "consciousnessGcs": -1,
  ///   "respirationAwRR": -1,
  ///   "pulseRate": -1,
  ///   "bloodPressureSystolic": -1,
  ///   "bloodPressureDiastolic": -1,
  ///   "skinColorHue": "Pale",
  ///   "skinTemperatureType": "Cold",
  ///   "isCardiacArrest": true,
  ///   "nextIdentifier": "D006"
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

    // ── 전이(Transition) ──

    /// <summary>프리셋 값 적용 방식 문자열 ("Immediate" / "Gradual"). null이면 즉시(Immediate).</summary>
    [JsonPropertyName("transitionMode")]
    public string TransitionMode { get; set; }

    /// <summary>점차 변화(Gradual) 시 소요 시간(초). Immediate에서는 무시된다.</summary>
    [JsonPropertyName("transitionDurationSeconds")]
    public float? TransitionDurationSeconds { get; set; }

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

    /// <summary>GCS 점수(3~15). null이면 현재 값 유지, -1이면 무의식(측정 불가).</summary>
    [JsonPropertyName("consciousnessGcs")]
    public int? ConsciousnessGcs { get; set; }

    /// <summary>GCS의 E(Eye Opening, 눈뜨기 반응) 세부 항목 문자열(1~4점). null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessEyeOpening")]
    public string ConsciousnessEyeOpening { get; set; }

    /// <summary>GCS의 V(Verbal Response, 언어 반응) 세부 항목 문자열(1~5점). null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessVerbal")]
    public string ConsciousnessVerbal { get; set; }

    /// <summary>GCS의 M(Motor Response, 운동 반응) 세부 항목 문자열(1~6점). null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessMotor")]
    public string ConsciousnessMotor { get; set; }

    /// <summary>의식수준 5단계(LOC) 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessLocLabel")]
    public string ConsciousnessLocLabel { get; set; }

    /// <summary>동공 반사 상태 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("consciousnessPupillaryResponse")]
    public string ConsciousnessPupillaryResponse { get; set; }

    // ── 호흡(Respiration) ──

    /// <summary>분당 호흡수(awRR). null이면 현재 값 유지, -1이면 호흡 없음(측정 불가).</summary>
    [JsonPropertyName("respirationAwRR")]
    public int? RespirationAwRR { get; set; }

    /// <summary>호흡 유형 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("respirationType")]
    public string RespirationTypeValue { get; set; }

    // ── 맥박(BloodPulse) ──

    /// <summary>분당 맥박수. null이면 현재 값 유지, -1이면 맥박 없음(측정 불가).</summary>
    [JsonPropertyName("pulseRate")]
    public int? PulseRate { get; set; }

    /// <summary>맥박 세기 유형 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("pulseForceType")]
    public string PulseForceType { get; set; }

    // ── 혈압(BloodPressure) ──

    /// <summary>수축기 혈압(mmHg). null이면 현재 값 유지, -1이면 측정 불가.</summary>
    [JsonPropertyName("bloodPressureSystolic")]
    public int? BloodPressureSystolic { get; set; }

    /// <summary>이완기 혈압(mmHg). null이면 현재 값 유지, -1이면 측정 불가.</summary>
    [JsonPropertyName("bloodPressureDiastolic")]
    public int? BloodPressureDiastolic { get; set; }

    // ── 피부(Skin) ──

    /// <summary>피부 색조 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("skinColorHue")]
    public string SkinColorHue { get; set; }

    /// <summary>피부 표면 온도 유형 문자열. null이면 현재 값 유지.</summary>
    [JsonPropertyName("skinTemperatureType")]
    public string SkinTemperatureType { get; set; }

    // ── 체온(BodyTemperature) ──

    /// <summary>심부 체온(°C). null이면 현재 값 유지, -1이면 측정 불가. 모니터 체온(T1)에도 반영된다.</summary>
    [JsonPropertyName("bodyTemperatureCelsius")]
    public float? BodyTemperatureCelsius { get; set; }

    // ── 산소포화도(SpO2) ──

    /// <summary>산소포화도(SpO2, %). null이면 현재 값 유지, -1이면 측정 불가. 모니터 numerics/pleth SpO2에 반영된다.</summary>
    [JsonPropertyName("spo2")]
    public int? Spo2 { get; set; }

    // ── 기타 의료 상태 ──

    /// <summary>심정지 여부. null이면 현재 값 유지.</summary>
    [JsonPropertyName("isCardiacArrest")]
    public bool? IsCardiacArrest { get; set; }
  }
}
