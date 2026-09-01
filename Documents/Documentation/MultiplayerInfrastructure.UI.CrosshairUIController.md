# <a id="MultiplayerInfrastructure_UI_CrosshairUIController"></a> Class CrosshairUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

CrosshairUIController는 화면 중앙 크로스헤어 UI를 표시합니다.

레이캐스트는 PlayerController.Raycast에서 수행되며,
이 컨트롤러는 UI 요소기만 담당합니다.

레이캐스트 결과는 PlayerController의 PlayerController.RaycastHasHit,
PlayerController.RaycastHit, PlayerController.RaycastHitObject에서 접근합니다.

```csharp
[RequireComponent(typeof(UIDocument))]
public class CrosshairUIController : UIControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[CrosshairUIController](MultiplayerInfrastructure.UI.CrosshairUIController.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Methods

### <a id="MultiplayerInfrastructure_UI_CrosshairUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_CrosshairUIController_SetCrosshairVisible_System_Boolean_"></a> SetCrosshairVisible\(bool\)

크로스헤어 UI의 시각성을 제어합니다.

```csharp
public void SetCrosshairVisible(bool visible)
```

#### Parameters

`visible` bool

