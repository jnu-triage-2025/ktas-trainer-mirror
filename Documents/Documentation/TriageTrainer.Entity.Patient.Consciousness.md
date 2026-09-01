# <a id="TriageTrainer_Entity_Patient_Consciousness"></a> Class Consciousness

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

환자의 의식 상태를 표현합니다.

```csharp
public class Consciousness
```

#### Inheritance

object ← 
[Consciousness](TriageTrainer.Entity.Patient.Consciousness.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_Consciousness_eyeOpening"></a> eyeOpening

GCS의 E(Eye Opening, 눈뜨기 반응) 세부 항목입니다. EyeOpeningResponse에 의해 정의된 값 중 하나를 가집니다.

```csharp
public EyeOpeningResponse eyeOpening
```

#### Field Value

 [EyeOpeningResponse](TriageTrainer.Entity.Patient.EyeOpeningResponse.md)

### <a id="TriageTrainer_Entity_Patient_Consciousness_gcs"></a> gcs

GCS(Glasgow Coma Scale) 점수입니다.
(NOTE: 이 값은 <xref href="TriageTrainer.Entity.Patient.Consciousness.eyeOpening" data-throw-if-not-resolved="false"></xref>, <xref href="TriageTrainer.Entity.Patient.Consciousness.verbal" data-throw-if-not-resolved="false"></xref>, <xref href="TriageTrainer.Entity.Patient.Consciousness.motor" data-throw-if-not-resolved="false"></xref> 세 필드의 합과
별개로 저장되는 필드입니다. E/V/M 세부 항목을 사정한 경우 <xref href="TriageTrainer.Entity.Patient.Consciousness.GcsFromComponents" data-throw-if-not-resolved="false"></xref> 를 통해
세부 항목 합계를 확인할 수 있으며, 필요 시 이 필드와 일치하도록 별도로 동기화해야 합니다.)

```csharp
public int gcs
```

#### Field Value

 int

### <a id="TriageTrainer_Entity_Patient_Consciousness_locLabel"></a> locLabel

의식수준 5단계(LOC; Level of Consciousness) 값입니다. LOCLabel에 의해 정의된 값 중 하나를 갖습니다.

```csharp
public LOCLabel locLabel
```

#### Field Value

 [LOCLabel](TriageTrainer.Entity.Patient.LOCLabel.md)

### <a id="TriageTrainer_Entity_Patient_Consciousness_motor"></a> motor

GCS의 M(Motor Response, 운동 반응) 세부 항목입니다. MotorResponse에 의해 정의된 값 중 하나를 가집니다.

```csharp
public MotorResponse motor
```

#### Field Value

 [MotorResponse](TriageTrainer.Entity.Patient.MotorResponse.md)

### <a id="TriageTrainer_Entity_Patient_Consciousness_pupillaryResponse"></a> pupillaryResponse

동공 반사 상태입니다. PupillaryResponse에 의해 정의된 값 중 하나를 가집니다.

```csharp
public PupillaryResponse pupillaryResponse
```

#### Field Value

 [PupillaryResponse](TriageTrainer.Entity.Patient.PupillaryResponse.md)

### <a id="TriageTrainer_Entity_Patient_Consciousness_verbal"></a> verbal

GCS의 V(Verbal Response, 언어 반응) 세부 항목입니다. VerbalResponse에 의해 정의된 값 중 하나를 가집니다.

```csharp
public VerbalResponse verbal
```

#### Field Value

 [VerbalResponse](TriageTrainer.Entity.Patient.VerbalResponse.md)

## Properties

### <a id="TriageTrainer_Entity_Patient_Consciousness_Default"></a> Default

정상 상태일 때의 이 클래스 객체의 값이 미리 정의되어 있습니다. <br />
- gcs: 15 <br />
- eyeOpening: EyeOpeningResponse.Spontaneous(4점) <br />
- verbal: VerbalResponse.Oriented(5점) <br />
- motor: MotorResponse.ObeysCommands(6점) <br />
- pupillaryResponse: PupillaryResponse.Normal <br />
- locLabel: LOCLabel.Alert <br />
이 속성을 호출하면 새 객체를 생성하여 반환합니다.

```csharp
public static Consciousness Default { get; }
```

#### Property Value

 [Consciousness](TriageTrainer.Entity.Patient.Consciousness.md)

### <a id="TriageTrainer_Entity_Patient_Consciousness_GcsFromComponents"></a> GcsFromComponents

E(<xref href="TriageTrainer.Entity.Patient.Consciousness.eyeOpening" data-throw-if-not-resolved="false"></xref>) + V(<xref href="TriageTrainer.Entity.Patient.Consciousness.verbal" data-throw-if-not-resolved="false"></xref>) + M(<xref href="TriageTrainer.Entity.Patient.Consciousness.motor" data-throw-if-not-resolved="false"></xref>) 세부 항목 점수의 합입니다.
GCS 세부 사정(E/V/M)을 완료한 경우 이 값이 <xref href="TriageTrainer.Entity.Patient.Consciousness.gcs" data-throw-if-not-resolved="false"></xref> 필드와 일치해야 합니다.

```csharp
public int GcsFromComponents { get; }
```

#### Property Value

 int

### <a id="TriageTrainer_Entity_Patient_Consciousness_GcsLabel"></a> GcsLabel

의식 상태 유형으로, GCS로부터 유도되어 반환됩니다.
(NOTE: 별도의 이상치 검증 로직이 구현되지 않았습니다. gcs 필드가 이상치라면 GCSLabel.NA를 반환합니다.)

```csharp
public GCSLabel GcsLabel { get; }
```

#### Property Value

 [GCSLabel](TriageTrainer.Entity.Patient.GCSLabel.md)

## Methods

### <a id="TriageTrainer_Entity_Patient_Consciousness_Clone"></a> Clone\(\)

```csharp
public object Clone()
```

#### Returns

 object

### <a id="TriageTrainer_Entity_Patient_Consciousness_Equals_TriageTrainer_Entity_Patient_Consciousness_"></a> Equals\(Consciousness?\)

```csharp
public bool Equals(Consciousness? other)
```

#### Parameters

`other` [Consciousness](TriageTrainer.Entity.Patient.Consciousness.md)?

#### Returns

 bool

### <a id="TriageTrainer_Entity_Patient_Consciousness_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` object

#### Returns

 bool

