# <a id="MultiplayerInfrastructure_UI_QuestUIController"></a> Class QuestUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[RequireComponent(typeof(UIDocument))]
public class QuestUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[QuestUIController](MultiplayerInfrastructure.UI.QuestUIController.md)

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

## Properties

### <a id="MultiplayerInfrastructure_UI_QuestUIController_IsOpen"></a> IsOpen

```csharp
public bool IsOpen { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_QuestUIController_Close"></a> Close\(\)

```csharp
public void Close()
```

### <a id="MultiplayerInfrastructure_UI_QuestUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_QuestUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_QuestUIController_Open"></a> Open\(\)

```csharp
public void Open()
```

### <a id="MultiplayerInfrastructure_UI_QuestUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_QuestUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

