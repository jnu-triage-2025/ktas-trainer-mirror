# <a id="TriageTrainer_Entity_Patient_Skin"></a> Class Skin

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

피부 표면에 관한 정보를 표현합니다.

```csharp
public class Skin
```

#### Inheritance

object ← 
[Skin](TriageTrainer.Entity.Patient.Skin.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_Skin_colorHue"></a> colorHue

육안으로 확인 가능한 피부 색조 정보를 표현합니다. SkinColorHue에 의해 정의된 "창백함(SkinColorHue.Pale)" 유형, 혹은 "정상(SkinColorHue.Normal)" 유형 둘 중 하나를 갖습니다.

```csharp
public SkinColorHue colorHue
```

#### Field Value

 [SkinColorHue](TriageTrainer.Entity.Patient.SkinColorHue.md)

### <a id="TriageTrainer_Entity_Patient_Skin_temperatureType"></a> temperatureType

피부 표면의 온도 수준을 표현합니다.

```csharp
public SkinTemperatureType temperatureType
```

#### Field Value

 [SkinTemperatureType](TriageTrainer.Entity.Patient.SkinTemperatureType.md)

#### Remarks

이 값은 단순화되었습니다:<br />
이 값은 체온 데이터와는 다르게 단순화되었습니다. 피부 표면의 온도는 혈액 순환 상황을 확인하기 위해 사용하므로 구체적인 수치로 정의되지 않습니다.
SkinTemperatureType에 의해 정의된 유형 중 하나를 가집니다.

## Properties

### <a id="TriageTrainer_Entity_Patient_Skin_Default"></a> Default

정상 상태일 때의 이 클래스 객체의 값이 미리 정의되어 있습니다.
colorHue: SkinColorHue.Normal <br />
temperatureType: SkinTemperatureType.Normal <br />
이 속성을 호출하면 새 객체를 생성하여 반환합니다.

```csharp
public static Skin Default { get; }
```

#### Property Value

 [Skin](TriageTrainer.Entity.Patient.Skin.md)

## Methods

### <a id="TriageTrainer_Entity_Patient_Skin_Clone"></a> Clone\(\)

```csharp
public object Clone()
```

#### Returns

 object

### <a id="TriageTrainer_Entity_Patient_Skin_Equals_TriageTrainer_Entity_Patient_Skin_"></a> Equals\(Skin?\)

```csharp
public bool Equals(Skin? other)
```

#### Parameters

`other` [Skin](TriageTrainer.Entity.Patient.Skin.md)?

#### Returns

 bool

### <a id="TriageTrainer_Entity_Patient_Skin_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` object

#### Returns

 bool

