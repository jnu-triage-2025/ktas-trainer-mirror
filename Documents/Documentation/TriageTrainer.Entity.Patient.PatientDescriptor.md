# <a id="TriageTrainer_Entity_Patient_PatientDescriptor"></a> Class PatientDescriptor

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

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

```csharp
public class PatientDescriptor
```

#### Inheritance

object ← 
[PatientDescriptor](TriageTrainer.Entity.Patient.PatientDescriptor.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_age"></a> age

환자의 나이입니다. (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여 이 나이가 자동으로 부여되도록 설계해야합니다.)
프리셋 지원: ScenarioPatientMedicalStatePresetNode.Age

```csharp
public int age
```

#### Field Value

 int

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_assessedTriage"></a> assessedTriage

플레이어가 실제로 평가한 트리아지 등급(현재 상태값)입니다.
아직 평가되지 않았으면 <xref href="TriageTrainer.Entity.Patient.TriageLevel.Unassessed" data-throw-if-not-resolved="false"></xref> 입니다.

```csharp
public TriageLevel assessedTriage
```

#### Field Value

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_bloodType"></a> bloodType

환자의 혈액형입니다. BloodType 열거형에 선언된 값 중 하나를 가집니다.

```csharp
public BloodType bloodType
```

#### Field Value

 [BloodType](TriageTrainer.Entity.Patient.BloodType.md)

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_identifier"></a> identifier

환자 식별에 사용되는 식별자입니다. (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여
이 식별자가 자동으로 부여되도록 설계해야합니다.)

```csharp
public string identifier
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_intendedTriage"></a> intendedTriage

의도된 트리아지 등급(정답)입니다. 시나리오 설계자가 이 환자에 대해 기대하는 KTAS 등급을 지정합니다.
트리아지 평가 시 <xref href="TriageTrainer.Entity.Patient.PatientDescriptor.assessedTriage" data-throw-if-not-resolved="false"></xref> 와 비교해 정답 여부를 판정하는 데 사용됩니다.

```csharp
public TriageLevel intendedTriage
```

#### Field Value

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_name"></a> name

환자의 성명입니다. (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여
이 이름이 자동으로 부여되도록 설계해야합니다.)
프리셋 지원: ScenarioPatientMedicalStatePresetNode.Name

```csharp
public string name
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_Patient_PatientDescriptor_sex"></a> sex

환자의 성별입니다. Sex 열거형에 선언된 두 개 값(Male, Female) 중 하나를 가집니다.
(구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여 이 성별이 자동으로 부여되도록 설계해야합니다.)
프리셋 지원: ScenarioPatientMedicalStatePresetNode.Sex

```csharp
public Sex sex
```

#### Field Value

 [Sex](TriageTrainer.Entity.Patient.Sex.md)

