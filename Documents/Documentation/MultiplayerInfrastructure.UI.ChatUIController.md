# <a id="MultiplayerInfrastructure_UI_ChatUIController"></a> Class ChatUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[RequireComponent(typeof(UIDocument))]
public class ChatUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[ChatUIController](MultiplayerInfrastructure.UI.ChatUIController.md)

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

### <a id="MultiplayerInfrastructure_UI_ChatUIController_IsOpen"></a> IsOpen

```csharp
public bool IsOpen { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_ChatUIController_AppendMessage_System_String_System_Boolean_"></a> AppendMessage\(string, bool\)

```csharp
public void AppendMessage(string message, bool showToastWhenHidden = true)
```

#### Parameters

`message` string

`showToastWhenHidden` bool

### <a id="MultiplayerInfrastructure_UI_ChatUIController_ClearLog"></a> ClearLog\(\)

```csharp
public void ClearLog()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_Close"></a> Close\(\)

```csharp
public void Close()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_HandleCancelKey"></a> HandleCancelKey\(\)

```csharp
public void HandleCancelKey()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_HandleHistoryNextKey"></a> HandleHistoryNextKey\(\)

```csharp
public void HandleHistoryNextKey()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_HandleHistoryPreviousKey"></a> HandleHistoryPreviousKey\(\)

```csharp
public void HandleHistoryPreviousKey()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_HandleSubmitKey"></a> HandleSubmitKey\(\)

```csharp
public void HandleSubmitKey()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_HandleTabKey"></a> HandleTabKey\(\)

Handles a Tab press while the chat input is focused. Returns silently
when there are no candidates so Tab remains harmless in normal chat.

```csharp
public void HandleTabKey()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_Open"></a> Open\(\)

```csharp
public void Open()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OpenWithCommandStart"></a> OpenWithCommandStart\(\)

```csharp
public void OpenWithCommandStart()
```

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OnCancelled"></a> OnCancelled

```csharp
public event Action OnCancelled
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OnSubmitted"></a> OnSubmitted

```csharp
public event Action<string> OnSubmitted
```

#### Event Type

 Action<string\>

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_ChatUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

