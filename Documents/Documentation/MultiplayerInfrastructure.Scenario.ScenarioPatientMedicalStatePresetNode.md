# <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode"></a> Class ScenarioPatientMedicalStatePresetNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

환자 엔티티의 의료 상태(PatientDescriptor 및 PatientMedicalState)를 일괄 초기화(프리셋)하는 노드.

<p>
지정한 엔티티 식별자(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref>)에 해당하는 <xref href="TriageTrainer.Entity.PatientController" data-throw-if-not-resolved="false"></xref>를
찾아 아래 필드를 덮어쓴다. <b>null 인 항목은 현재 값을 유지</b>하므로,
설정이 필요한 필드만 기입하면 된다.
</p>

<p>새 환자 상태 필드를 추가할 때도 이 노드를 통해 프리셋 값을 설정할 수 있다.
아래 프로퍼티 목록에 필드를 추가하고, DTO(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNodeDTO" data-throw-if-not-resolved="false"></xref>),
로더(<code>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</code>),
컨트롤러(<code>PatientController.ApplyMedicalStatePreset</code>) 세 곳에도 동일하게 추가한다.</p>

<p>
── 프리셋 가능 필드 목록 ──

<table><thead><tr><th class="term">필드 그룹</th><th class="term">프로퍼티</th><th class="term">타입</th></tr></thead><tbody><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Sex" data-throw-if-not-resolved="false"></xref></td><td class="term">Sex?</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Age" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Name" data-throw-if-not-resolved="false"></xref></td><td class="term">string</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodType" data-throw-if-not-resolved="false"></xref></td><td class="term">BloodType?</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.IntendedTriage" data-throw-if-not-resolved="false"></xref></td><td class="term">TriageLevel?</td></tr><tr><td class="term">의료 상태</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessGcs" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessEyeOpening" data-throw-if-not-resolved="false"></xref></td><td class="term">EyeOpeningResponse?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessVerbal" data-throw-if-not-resolved="false"></xref></td><td class="term">VerbalResponse?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessMotor" data-throw-if-not-resolved="false"></xref></td><td class="term">MotorResponse?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessLocLabel" data-throw-if-not-resolved="false"></xref></td><td class="term">LOCLabel?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessPupillaryResponse" data-throw-if-not-resolved="false"></xref></td><td class="term">PupillaryResponse?</td></tr><tr><td class="term">의료 상태/호흡</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.RespirationAwRR" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/호흡</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.RespirationTypeValue" data-throw-if-not-resolved="false"></xref></td><td class="term">RespirationType?</td></tr><tr><td class="term">의료 상태/맥박</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.PulseRate" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/맥박</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.PulseForceType" data-throw-if-not-resolved="false"></xref></td><td class="term">BloodPulseForceType?</td></tr><tr><td class="term">의료 상태/혈압</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureSystolic" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/혈압</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureDiastolic" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/피부</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.SkinColorHue" data-throw-if-not-resolved="false"></xref></td><td class="term">SkinColorHue?</td></tr><tr><td class="term">의료 상태/피부</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.SkinTemperatureType" data-throw-if-not-resolved="false"></xref></td><td class="term">SkinTemperatureType?</td></tr><tr><td class="term">의료 상태/체온</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BodyTemperatureCelsius" data-throw-if-not-resolved="false"></xref></td><td class="term">float?</td></tr><tr><td class="term">의료 상태/모니터</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Spo2" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.IsCardiacArrest" data-throw-if-not-resolved="false"></xref></td><td class="term">bool?</td></tr></tbody></table>
</p>

<p>
── 측정 불가/무의식/없음 표현 ──
수치 필드(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessGcs" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.RespirationAwRR" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.PulseRate" data-throw-if-not-resolved="false"></xref>,
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureSystolic" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureDiastolic" data-throw-if-not-resolved="false"></xref>)에 <b>-1</b>을 지정하면
"무의식 / 호흡 없음 / 측정 불가" 등 <b>값이 존재하지 않는 상태</b>를 의미한다.
이 경우 환자 상태 모니터에는 해당 수치가 <code>-?-</code> 로 표시된다.
(null은 "현재 값 유지", -1은 "측정 불가"로 서로 다른 의미임에 유의한다.)
</p>

<p>
── 전이(Transition) 방식 ──
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TransitionMode" data-throw-if-not-resolved="false"></xref> 로 프리셋 값이 적용되는 방식을 지정한다.
<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Immediate" data-throw-if-not-resolved="false"></xref> 는 즉시 적용,
<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Gradual" data-throw-if-not-resolved="false"></xref> 은 <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TransitionDurationSeconds" data-throw-if-not-resolved="false"></xref> 동안
수치 값을 현재 값에서 대상 값으로 점차 보간한다.
</p>

```csharp
public sealed class ScenarioPatientMedicalStatePresetNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioPatientMedicalStatePresetNode](MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_Age"></a> Age

환자 나이. null이면 현재 값 유지.

```csharp
public int? Age { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_BloodPressureDiastolic"></a> BloodPressureDiastolic

이완기 혈압(mmHg). null이면 현재 값 유지, -1이면 측정 불가.

```csharp
public int? BloodPressureDiastolic { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_BloodPressureSystolic"></a> BloodPressureSystolic

수축기 혈압(mmHg). null이면 현재 값 유지, -1이면 측정 불가.

```csharp
public int? BloodPressureSystolic { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_BloodType"></a> BloodType

환자 혈액형. null이면 현재 값 유지.

```csharp
public BloodType? BloodType { get; set; }
```

#### Property Value

 [BloodType](TriageTrainer.Entity.Patient.BloodType.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_BodyTemperatureCelsius"></a> BodyTemperatureCelsius

심부 체온(°C). null이면 현재 값 유지, -1이면 측정 불가.
적용 시 <code>PatientMedicalState.bodyTemperature.celsius</code> 와 모니터 체온(T1)에 반영된다.

```csharp
public float? BodyTemperatureCelsius { get; set; }
```

#### Property Value

 float?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_ConsciousnessEyeOpening"></a> ConsciousnessEyeOpening

GCS의 E(Eye Opening, 눈뜨기 반응) 세부 항목(1~4점). null이면 현재 값 유지.

```csharp
public EyeOpeningResponse? ConsciousnessEyeOpening { get; set; }
```

#### Property Value

 [EyeOpeningResponse](TriageTrainer.Entity.Patient.EyeOpeningResponse.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_ConsciousnessGcs"></a> ConsciousnessGcs

GCS 점수(3~15). null이면 현재 값 유지, -1이면 무의식(측정 불가).

```csharp
public int? ConsciousnessGcs { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_ConsciousnessLocLabel"></a> ConsciousnessLocLabel

의식수준 5단계(LOC). null이면 현재 값 유지.

```csharp
public LOCLabel? ConsciousnessLocLabel { get; set; }
```

#### Property Value

 [LOCLabel](TriageTrainer.Entity.Patient.LOCLabel.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_ConsciousnessMotor"></a> ConsciousnessMotor

GCS의 M(Motor Response, 운동 반응) 세부 항목(1~6점). null이면 현재 값 유지.

```csharp
public MotorResponse? ConsciousnessMotor { get; set; }
```

#### Property Value

 [MotorResponse](TriageTrainer.Entity.Patient.MotorResponse.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_ConsciousnessPupillaryResponse"></a> ConsciousnessPupillaryResponse

동공 반사 상태. null이면 현재 값 유지.

```csharp
public PupillaryResponse? ConsciousnessPupillaryResponse { get; set; }
```

#### Property Value

 [PupillaryResponse](TriageTrainer.Entity.Patient.PupillaryResponse.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_ConsciousnessVerbal"></a> ConsciousnessVerbal

GCS의 V(Verbal Response, 언어 반응) 세부 항목(1~5점). null이면 현재 값 유지.

```csharp
public VerbalResponse? ConsciousnessVerbal { get; set; }
```

#### Property Value

 [VerbalResponse](TriageTrainer.Entity.Patient.VerbalResponse.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_IntendedTriage"></a> IntendedTriage

의도된(정답) 트리아지 등급. null이면 현재 값 유지.

```csharp
public TriageLevel? IntendedTriage { get; set; }
```

#### Property Value

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_IsCardiacArrest"></a> IsCardiacArrest

심정지 여부. null이면 현재 값 유지.

```csharp
public bool? IsCardiacArrest { get; set; }
```

#### Property Value

 bool?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_Name"></a> Name

환자 성명. null이면 현재 값 유지.

```csharp
public string Name { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_PulseForceType"></a> PulseForceType

맥박 세기 유형. null이면 현재 값 유지.

```csharp
public BloodPulseForceType? PulseForceType { get; set; }
```

#### Property Value

 [BloodPulseForceType](TriageTrainer.Entity.Patient.BloodPulseForceType.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_PulseRate"></a> PulseRate

분당 맥박수. null이면 현재 값 유지, -1이면 맥박 없음(측정 불가).

```csharp
public int? PulseRate { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_RespirationAwRR"></a> RespirationAwRR

분당 호흡수(awRR). null이면 현재 값 유지, -1이면 호흡 없음(측정 불가).

```csharp
public int? RespirationAwRR { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_RespirationTypeValue"></a> RespirationTypeValue

호흡 유형. null이면 현재 값 유지.

```csharp
public RespirationType? RespirationTypeValue { get; set; }
```

#### Property Value

 [RespirationType](TriageTrainer.Entity.Patient.RespirationType.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_Sex"></a> Sex

환자 성별(Sex.Male / Sex.Female). null이면 현재 값 유지.

```csharp
public Sex? Sex { get; set; }
```

#### Property Value

 [Sex](TriageTrainer.Entity.Patient.Sex.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_SkinColorHue"></a> SkinColorHue

피부 색조. null이면 현재 값 유지.

```csharp
public SkinColorHue? SkinColorHue { get; set; }
```

#### Property Value

 [SkinColorHue](TriageTrainer.Entity.Patient.SkinColorHue.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_SkinTemperatureType"></a> SkinTemperatureType

피부 표면 온도 유형. null이면 현재 값 유지.

```csharp
public SkinTemperatureType? SkinTemperatureType { get; set; }
```

#### Property Value

 [SkinTemperatureType](TriageTrainer.Entity.Patient.SkinTemperatureType.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_Spo2"></a> Spo2

산소포화도(SpO2, %). null이면 현재 값 유지, -1이면 측정 불가.
적용 시 모니터 numerics/pleth 의 SpO2 에 반영된다.

```csharp
public int? Spo2 { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_TargetEntityIdentifier"></a> TargetEntityIdentifier

상태를 초기화할 환자 엔티티 식별자.
비어 있으면 상태 저장소의 <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TargetEntityStateKey" data-throw-if-not-resolved="false"></xref> 값을 사용한다.

```csharp
public string TargetEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_TargetEntityStateKey"></a> TargetEntityStateKey

상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref> 가 비어 있을 때만 사용된다.

```csharp
public string TargetEntityStateKey { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_TransitionDurationSeconds"></a> TransitionDurationSeconds

점차 변화(<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Gradual" data-throw-if-not-resolved="false"></xref>) 시 소요 시간(초).
<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Immediate" data-throw-if-not-resolved="false"></xref> 에서는 무시된다.
0 이하이면 즉시 적용과 동일하게 동작한다.

```csharp
public float TransitionDurationSeconds { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_TransitionMode"></a> TransitionMode

프리셋 값 적용 방식. 기본값은 <xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Immediate" data-throw-if-not-resolved="false"></xref>(즉시).
<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Gradual" data-throw-if-not-resolved="false"></xref> 로 지정하면
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TransitionDurationSeconds" data-throw-if-not-resolved="false"></xref> 동안 수치 값을 점차 변화시킨다.

```csharp
public PatientMedicalStateTransitionMode TransitionMode { get; set; }
```

#### Property Value

 [PatientMedicalStateTransitionMode](MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.md)

