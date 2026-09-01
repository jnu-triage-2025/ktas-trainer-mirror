# <a id="MultiplayerInfrastructure_UI_KeyConfigUIController"></a> Class KeyConfigUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

키 설정 UI의 UIDocument 컨트롤러입니다.

역할:
  - 좌측 ScrollView에 <xref href="MultiplayerInfrastructure.UI.KeyConfigEntryElement" data-throw-if-not-resolved="false"></xref> 목록을 동적으로 생성합니다.
  - 우측 <xref href="MultiplayerInfrastructure.UI.KeyboardLayoutElement" data-throw-if-not-resolved="false"></xref>에 현재 바인딩을 반영합니다.
  - 키보드 키 클릭 → 좌측 목록 스크롤 및 강조
  - 목록 항목 클릭 → 키보드에서 해당 키 강조

```csharp
[RequireComponent(typeof(UIDocument))]
public class KeyConfigUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[KeyConfigUIController](MultiplayerInfrastructure.UI.KeyConfigUIController.md)

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

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_Hide"></a> Hide\(\)

UI를 숨깁니다.

```csharp
public void Hide()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_ResetToDefaults"></a> ResetToDefaults\(\)

모든 바인딩을 초기(기본) 값으로 되돌리고 저장합니다.

```csharp
public void ResetToDefaults()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_SetBindings_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_UI_KeyBindingEntry__"></a> SetBindings\(IReadOnlyList<KeyBindingEntry\>\)

바인딩 목록을 교체하고 UI를 다시 구성합니다.
런타임에 설정이 변경된 경우 호출합니다.

```csharp
public void SetBindings(IReadOnlyList<KeyBindingEntry> bindings)
```

#### Parameters

`bindings` IReadOnlyList<[KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)\>

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_Show"></a> Show\(\)

UI를 표시합니다.

```csharp
public void Show()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_Toggle"></a> Toggle\(\)

표시/숨김 토글합니다.

```csharp
public void Toggle()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_KeyConfigUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

