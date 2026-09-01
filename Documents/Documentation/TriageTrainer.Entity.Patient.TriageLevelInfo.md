# <a id="TriageTrainer_Entity_Patient_TriageLevelInfo"></a> Class TriageLevelInfo

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

KTAS 트리아지 등급별 표준 색상/명칭 조회 유틸리티.

<p>
트리아지 평가 UI(색상 사각형)와 인게임 환자 위 태그 표기가 동일한 색상/명칭 규칙을 공유하도록,
등급 → (색상, 한국어 명칭, 짧은 라벨)의 매핑을 단일 진실 공급원으로 제공한다.
</p>

```csharp
public static class TriageLevelInfo
```

#### Inheritance

object ← 
[TriageLevelInfo](TriageTrainer.Entity.Patient.TriageLevelInfo.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_TriageLevelInfo_SelectableLevels"></a> SelectableLevels

플레이어가 선택 가능한 KTAS 등급 순서(1→5). Unassessed 는 제외한다.

```csharp
public static readonly TriageLevel[] SelectableLevels
```

#### Field Value

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)\[\]

## Methods

### <a id="TriageTrainer_Entity_Patient_TriageLevelInfo_GetColor_TriageTrainer_Entity_Patient_TriageLevel_"></a> GetColor\(TriageLevel\)

등급의 표준 배경 색상.

```csharp
public static Color GetColor(TriageLevel level)
```

#### Parameters

`level` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

#### Returns

 Color

### <a id="TriageTrainer_Entity_Patient_TriageLevelInfo_GetDisplayName_TriageTrainer_Entity_Patient_TriageLevel_"></a> GetDisplayName\(TriageLevel\)

등급의 한국어 명칭(예: "2단계 긴급").

```csharp
public static string GetDisplayName(TriageLevel level)
```

#### Parameters

`level` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

#### Returns

 string

### <a id="TriageTrainer_Entity_Patient_TriageLevelInfo_GetShortLabel_TriageTrainer_Entity_Patient_TriageLevel_"></a> GetShortLabel\(TriageLevel\)

등급의 짧은 라벨(예: "KTAS 2").

```csharp
public static string GetShortLabel(TriageLevel level)
```

#### Parameters

`level` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

#### Returns

 string

### <a id="TriageTrainer_Entity_Patient_TriageLevelInfo_GetTextColor_TriageTrainer_Entity_Patient_TriageLevel_"></a> GetTextColor\(TriageLevel\)

등급 색상 위에 얹을 텍스트 색상(가독성 고려).

```csharp
public static Color GetTextColor(TriageLevel level)
```

#### Parameters

`level` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

#### Returns

 Color

