# <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController"></a> Class GraphicsSettingsUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

그래픽 설정 UI의 UIDocument 컨트롤러입니다.

역할:
  - <xref href="MultiplayerInfrastructure.UI.TextureQualityOptionElement" data-throw-if-not-resolved="false"></xref> 타일 4개를 동적으로 생성합니다.
  - 타일 클릭 시 선택 상태를 갱신하고 "적용 및 저장" 버튼 활성화를 조절합니다.
  - "적용 및 저장" 클릭 시 <xref href="MultiplayerInfrastructure.Performance.TexturePerformanceService" data-throw-if-not-resolved="false"></xref>를 통해 설정을 저장합니다.
  - <xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref>를 구현하여 <xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>으로 열고 닫습니다.

```csharp
[RequireComponent(typeof(UIDocument))]
public class GraphicsSettingsUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[GraphicsSettingsUIController](MultiplayerInfrastructure.UI.GraphicsSettingsUIController.md)

#### Implements

[IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Methods

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_Hide"></a> Hide\(\)

UI를 숨깁니다.

```csharp
public void Hide()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_Show"></a> Show\(\)

UI를 표시합니다.

```csharp
public void Show()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_Toggle"></a> Toggle\(\)

표시/숨김을 토글합니다.

```csharp
public void Toggle()
```

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_GraphicsSettingsUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

