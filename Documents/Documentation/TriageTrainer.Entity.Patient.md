# <a id="TriageTrainer_Entity_Patient"></a> Namespace TriageTrainer.Entity.Patient

### Classes

 [BloodPressure](TriageTrainer.Entity.Patient.BloodPressure.md)

혈압 정보를 정의합니다.

 [BloodPulse](TriageTrainer.Entity.Patient.BloodPulse.md)

맥박을 표현합니다.

 [BodyTemperature](TriageTrainer.Entity.Patient.BodyTemperature.md)

신체의 체온을 표현합니다.

 [Consciousness](TriageTrainer.Entity.Patient.Consciousness.md)

환자의 의식 상태를 표현합니다.

 [HealthProblem](TriageTrainer.Entity.Patient.HealthProblem.md)

 [PatientDescriptor](TriageTrainer.Entity.Patient.PatientDescriptor.md)

환자 상태 기술자
이 클래스는 환자의 생체 상태를 정의합니다. 환자 엔티티 생성에 필요한 데이터, 환자 모니터에 제공하는 데이터,
환자 소생에 필요한 조치에 관한 데이터 일체를 포함합니다.
이후 구현 상황에 따라서 사후 평가 과정에서도 사용할 수 있도록 확장할 수 있습니다.

<p>
<b>시나리오 프리셋 지원:</b> 이 클래스의 필드는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> 를 통해
시나리오 JSON에서 초기값(프리셋)을 설정할 수 있습니다.
새 필드를 추가할 때 프리셋 지원이 필요하면 다음 네 곳에 동일하게 추가하세요:
ScenarioPatientMedicalStatePresetNode / ScenarioPatientMedicalStatePresetNodeDTO /
ScenarioGraphLoader.ConvertPatientMedicalStatePreset / PatientController.ApplyMedicalStatePreset
</p>

 [PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)

환자의 의료 상태를 표현합니다.

<p>
<b>시나리오 프리셋 지원:</b> 이 클래스의 모든 필드는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> 를 통해
시나리오 JSON에서 초기값(프리셋)을 설정할 수 있습니다.
</p>

<p>새 필드를 추가할 때 프리셋 지원이 필요하면 다음 네 곳에 동일하게 추가하세요:

<ol><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> — nullable 프로퍼티</li><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNodeDTO" data-throw-if-not-resolved="false"></xref> — nullable JSON 프로퍼티</li><li><code>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</code> — DTO → 도메인 매핑</li><li><code>PatientController.ApplyMedicalStatePreset</code> — 도메인 → PatientController 적용</li></ol>
</p>

 [RequiredDrug](TriageTrainer.Entity.Patient.RequiredDrug.md)

환자에게 요구되는 약물의 정보입니다. 정맥로를 통해 주입되어야 하는 약의 유형과 양을 표현하도록 의도되었습니다.

 [Respiration](TriageTrainer.Entity.Patient.Respiration.md)

환자의 호흡 상태를 정의합니다.

 [Skin](TriageTrainer.Entity.Patient.Skin.md)

피부 표면에 관한 정보를 표현합니다.

 [TriageLevelInfo](TriageTrainer.Entity.Patient.TriageLevelInfo.md)

KTAS 트리아지 등급별 표준 색상/명칭 조회 유틸리티.

<p>
트리아지 평가 UI(색상 사각형)와 인게임 환자 위 태그 표기가 동일한 색상/명칭 규칙을 공유하도록,
등급 → (색상, 한국어 명칭, 짧은 라벨)의 매핑을 단일 진실 공급원으로 제공한다.
</p>

### Structs

 [ARTParameters](TriageTrainer.Entity.Patient.ARTParameters.md)

 [CVPParameters](TriageTrainer.Entity.Patient.CVPParameters.md)

 [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

 [NIBPParameters](TriageTrainer.Entity.Patient.NIBPParameters.md)

 [NumericsParameters](TriageTrainer.Entity.Patient.NumericsParameters.md)

 [PlethParameters](TriageTrainer.Entity.Patient.PlethParameters.md)

 [STLeadValues](TriageTrainer.Entity.Patient.STLeadValues.md)

 [TemperatureParameters](TriageTrainer.Entity.Patient.TemperatureParameters.md)

### Enums

 [BloodPulseForceType](TriageTrainer.Entity.Patient.BloodPulseForceType.md)

맥박의 세기를 표현합니다.

 [BloodPulseSpeedType](TriageTrainer.Entity.Patient.BloodPulseSpeedType.md)

맥박의 속도 유형을 정의합니다.

 [BloodType](TriageTrainer.Entity.Patient.BloodType.md)

혈액형을 표현합니다.

 [BodyTemperatureType](TriageTrainer.Entity.Patient.BodyTemperatureType.md)

신체 온도의 유형을 표현합니다.

 [Drug](TriageTrainer.Entity.Patient.Drug.md)

구현체에서 표현할 약물들을 식별하기 위한 식별자입니다.

 [ECGRhythmType](TriageTrainer.Entity.Patient.ECGRhythmType.md)

 [EyeOpeningResponse](TriageTrainer.Entity.Patient.EyeOpeningResponse.md)

GCS(Glasgow Coma Scale)의 E(Eye Opening, 눈뜨기 반응) 세부 항목을 표현합니다.
값은 해당 GCS 점수(1~4점)와 동일하게 매핑되어 있습니다.

 [GCSLabel](TriageTrainer.Entity.Patient.GCSLabel.md)

Glasgow Coma Scale (GCS) 점수 분포 별 유형을 표현합니다.

 [HealthProblemType](TriageTrainer.Entity.Patient.HealthProblemType.md)

 [LOCLabel](TriageTrainer.Entity.Patient.LOCLabel.md)

의식수준 5단계(LOC; Level of Consciousness)를 표현합니다.

 [MotorResponse](TriageTrainer.Entity.Patient.MotorResponse.md)

GCS(Glasgow Coma Scale)의 M(Motor Response, 운동 반응) 세부 항목을 표현합니다.
값은 해당 GCS 점수(1~6점)와 동일하게 매핑되어 있습니다.

 [PupillaryResponse](TriageTrainer.Entity.Patient.PupillaryResponse.md)

동공 반사 결과를 표현합니다.

 [RespirationType](TriageTrainer.Entity.Patient.RespirationType.md)

환자의 호흡 상태를 정의합니다.

 [Sex](TriageTrainer.Entity.Patient.Sex.md)

생물학적 성별을 표현합니다.

 [SkinColorHue](TriageTrainer.Entity.Patient.SkinColorHue.md)

피부 표면의 색조를 표현합니다.

 [SkinTemperatureType](TriageTrainer.Entity.Patient.SkinTemperatureType.md)

피부 표면의 온도를 단순화하여 표현합니다.

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

KTAS(Korean Triage and Acuity Scale, 한국형 응급환자 분류도구) 5단계 트리아지 등급을 표현합니다.

<p>
트리아지 분류에서 플레이어가 환자에게 부여하는 응급도 등급이며, 각 등급은 표준 색상과 명칭을 가집니다.
색상/명칭 조회는 <xref href="TriageTrainer.Entity.Patient.TriageLevelInfo" data-throw-if-not-resolved="false"></xref> 를 사용합니다.
</p>

<p>
<xref href="TriageTrainer.Entity.Patient.TriageLevel.Unassessed" data-throw-if-not-resolved="false"></xref> 는 아직 트리아지 분류가 수행되지 않은 상태를 나타내며, 실제 KTAS 등급이 아닙니다.
</p>

 [VerbalResponse](TriageTrainer.Entity.Patient.VerbalResponse.md)

GCS(Glasgow Coma Scale)의 V(Verbal Response, 언어 반응) 세부 항목을 표현합니다.
값은 해당 GCS 점수(1~5점)와 동일하게 매핑되어 있습니다.

