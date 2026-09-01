# <a id="TriageTrainer_Entity_Patient_PatientMedicalState"></a> Class PatientMedicalState

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

환자의 의료 상태를 표현합니다.

<p>
<b>시나리오 프리셋 지원:</b> 이 클래스의 모든 필드는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> 를 통해
시나리오 JSON에서 초기값(프리셋)을 설정할 수 있습니다.
</p>

<p>새 필드를 추가할 때 프리셋 지원이 필요하면 다음 네 곳에 동일하게 추가하세요:

<ol><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> — nullable 프로퍼티</li><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNodeDTO" data-throw-if-not-resolved="false"></xref> — nullable JSON 프로퍼티</li><li><code>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</code> — DTO → 도메인 매핑</li><li><code>PatientController.ApplyMedicalStatePreset</code> — 도메인 → PatientController 적용</li></ol>
</p>

```csharp
[Serializable]
public class PatientMedicalState
```

#### Inheritance

object ← 
[PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_MonitorValueUnavailable"></a> MonitorValueUnavailable

모니터 수치 필드(numerics.bpm/pulseRate, nibp.systolic/diastolic 등)에서
"측정 불가 / 무의식 / 호흡 없음"을 나타내는 센티넬 값.
이 값이 설정되면 환자 상태 모니터에는 해당 수치가 <code>-?-</code> 로 표시된다.
프리셋 노드에서 수치 필드에 -1을 지정하면 이 값으로 매핑된다.

```csharp
public const float MonitorValueUnavailable = -1
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_art"></a> art

```csharp
public ARTParameters art
```

#### Field Value

 [ARTParameters](TriageTrainer.Entity.Patient.ARTParameters.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_bloodPressure"></a> bloodPressure

```csharp
[Header("Medical State")]
public BloodPressure bloodPressure
```

#### Field Value

 [BloodPressure](TriageTrainer.Entity.Patient.BloodPressure.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_bodyTemperature"></a> bodyTemperature

```csharp
public BodyTemperature bodyTemperature
```

#### Field Value

 [BodyTemperature](TriageTrainer.Entity.Patient.BodyTemperature.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_consciousness"></a> consciousness

```csharp
public Consciousness consciousness
```

#### Field Value

 [Consciousness](TriageTrainer.Entity.Patient.Consciousness.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_cvp"></a> cvp

```csharp
public CVPParameters cvp
```

#### Field Value

 [CVPParameters](TriageTrainer.Entity.Patient.CVPParameters.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_ecg"></a> ecg

```csharp
[Header("Medical State/Monitor")]
public ECGParameters ecg
```

#### Field Value

 [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_healthProblem"></a> healthProblem

```csharp
public List<HealthProblem> healthProblem
```

#### Field Value

 List<[HealthProblem](TriageTrainer.Entity.Patient.HealthProblem.md)\>

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_isCardiacArrest"></a> isCardiacArrest

```csharp
public bool isCardiacArrest
```

#### Field Value

 bool

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_nibp"></a> nibp

```csharp
public NIBPParameters nibp
```

#### Field Value

 [NIBPParameters](TriageTrainer.Entity.Patient.NIBPParameters.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_numerics"></a> numerics

```csharp
public NumericsParameters numerics
```

#### Field Value

 [NumericsParameters](TriageTrainer.Entity.Patient.NumericsParameters.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_pleth"></a> pleth

```csharp
public PlethParameters pleth
```

#### Field Value

 [PlethParameters](TriageTrainer.Entity.Patient.PlethParameters.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_pulse"></a> pulse

```csharp
public BloodPulse pulse
```

#### Field Value

 [BloodPulse](TriageTrainer.Entity.Patient.BloodPulse.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_requiredDrugs"></a> requiredDrugs

```csharp
public List<RequiredDrug> requiredDrugs
```

#### Field Value

 List<[RequiredDrug](TriageTrainer.Entity.Patient.RequiredDrug.md)\>

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_respiration"></a> respiration

```csharp
public Respiration respiration
```

#### Field Value

 [Respiration](TriageTrainer.Entity.Patient.Respiration.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_skin"></a> skin

```csharp
public Skin skin
```

#### Field Value

 [Skin](TriageTrainer.Entity.Patient.Skin.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_stLeads"></a> stLeads

```csharp
public STLeadValues stLeads
```

#### Field Value

 [STLeadValues](TriageTrainer.Entity.Patient.STLeadValues.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_temperature"></a> temperature

```csharp
public TemperatureParameters temperature
```

#### Field Value

 [TemperatureParameters](TriageTrainer.Entity.Patient.TemperatureParameters.md)

## Methods

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_Clone"></a> Clone\(\)

```csharp
public PatientMedicalState Clone()
```

#### Returns

 [PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)

### <a id="TriageTrainer_Entity_Patient_PatientMedicalState_CopyFrom_TriageTrainer_Entity_Patient_PatientMedicalState_"></a> CopyFrom\(PatientMedicalState\)

```csharp
public void CopyFrom(PatientMedicalState source)
```

#### Parameters

`source` [PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)

