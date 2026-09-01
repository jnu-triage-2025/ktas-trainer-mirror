# <a id="TriageTrainer_UI_TriageAssessmentUIController"></a> Class TriageAssessmentUIController

Namespace: [TriageTrainer.UI](TriageTrainer.UI.md)  
Assembly: Assembly\-CSharp.dll  

트리아지 평가 전체화면 오버레이 UI 컨트롤러.

<p>
플레이어가 환자의 트리아지 인터랙션을 수행하면 <xref href="TriageTrainer.UI.TriageAssessmentUIController.Open(TriageTrainer.Entity.Patient.TriageLevel%2cSystem.Action%7bTriageTrainer.Entity.Patient.TriageLevel%7d)" data-throw-if-not-resolved="false"></xref> 으로 열리며, 가로로 늘어진 KTAS 색상
사각형(<xref href="TriageTrainer.UI.TriageAssessmentPanelElement" data-throw-if-not-resolved="false"></xref>)을 표시한다. 사각형을 클릭하면 선택 콜백으로 등급을
통지하고 패널을 닫는다.
</p>

<p>
<xref href="MultiplayerInfrastructure.UI.ProblemSheetUIController" data-throw-if-not-resolved="false"></xref> 와 동일한 <xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>
관례를 따른다.
</p>

```csharp
[RequireComponent(typeof(UIDocument))]
public sealed class TriageAssessmentUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[TriageAssessmentUIController](TriageTrainer.UI.TriageAssessmentUIController.md)

#### Implements

[IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

## Properties

### <a id="TriageTrainer_UI_TriageAssessmentUIController_ActiveInstance"></a> ActiveInstance

활성 인스턴스(단일 로컬 클라이언트에 하나). 환자 컨트롤러가 손쉽게 접근하기 위한 헬퍼.

```csharp
public static TriageAssessmentUIController ActiveInstance { get; }
```

#### Property Value

 [TriageAssessmentUIController](TriageTrainer.UI.TriageAssessmentUIController.md)

### <a id="TriageTrainer_UI_TriageAssessmentUIController_IsOpen"></a> IsOpen

```csharp
public bool IsOpen { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_UI_TriageAssessmentUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="TriageTrainer_UI_TriageAssessmentUIController_Close"></a> Close\(\)

```csharp
public void Close()
```

### <a id="TriageTrainer_UI_TriageAssessmentUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="TriageTrainer_UI_TriageAssessmentUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="TriageTrainer_UI_TriageAssessmentUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="TriageTrainer_UI_TriageAssessmentUIController_Open_TriageTrainer_Entity_Patient_TriageLevel_System_Action_TriageTrainer_Entity_Patient_TriageLevel__"></a> Open\(TriageLevel, Action<TriageLevel\>\)

트리아지 평가 패널을 연다.

```csharp
public void Open(TriageLevel current, Action<TriageLevel> onSelected)
```

#### Parameters

`current` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

현재(기존) 평가 등급. 패널에서 해당 등급을 선택 상태로 강조 표시한다.

`onSelected` Action<[TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)\>

등급 선택 시 호출되는 콜백.

### <a id="TriageTrainer_UI_TriageAssessmentUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="TriageTrainer_UI_TriageAssessmentUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

