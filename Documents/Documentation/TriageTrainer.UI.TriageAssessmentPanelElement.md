# <a id="TriageTrainer_UI_TriageAssessmentPanelElement"></a> Class TriageAssessmentPanelElement

Namespace: [TriageTrainer.UI](TriageTrainer.UI.md)  
Assembly: Assembly\-CSharp.dll  

트리아지 평가 패널: 가로로 늘어진 KTAS 색상 사각형들을 표시한다.

<p>
각 사각형은 (1) 해당 트리아지 등급 색으로 칠해지고 (2) 색 명칭과 이름(예: "KTAS 2", "2단계 긴급")이
표기된다. 사각형을 클릭하면 <xref href="TriageTrainer.UI.TriageAssessmentPanelElement.LevelSelected" data-throw-if-not-resolved="false"></xref> 이벤트로 선택된 등급을 통지한다.
</p>

```csharp
public sealed class TriageAssessmentPanelElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[TriageAssessmentPanelElement](TriageTrainer.UI.TriageAssessmentPanelElement.md)

## Constructors

### <a id="TriageTrainer_UI_TriageAssessmentPanelElement__ctor"></a> TriageAssessmentPanelElement\(\)

```csharp
public TriageAssessmentPanelElement()
```

## Methods

### <a id="TriageTrainer_UI_TriageAssessmentPanelElement_SetCurrentSelection_TriageTrainer_Entity_Patient_TriageLevel_"></a> SetCurrentSelection\(TriageLevel\)

현재(기존) 평가 등급을 선택 상태로 강조 표시한다. Unassessed 면 강조 없음.

```csharp
public void SetCurrentSelection(TriageLevel current)
```

#### Parameters

`current` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

### <a id="TriageTrainer_UI_TriageAssessmentPanelElement_CancelRequested"></a> CancelRequested

취소(닫기) 요청 시.

```csharp
public event Action CancelRequested
```

#### Event Type

 Action

### <a id="TriageTrainer_UI_TriageAssessmentPanelElement_LevelSelected"></a> LevelSelected

사각형 클릭으로 등급이 선택되었을 때.

```csharp
public event Action<TriageLevel> LevelSelected
```

#### Event Type

 Action<[TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)\>

