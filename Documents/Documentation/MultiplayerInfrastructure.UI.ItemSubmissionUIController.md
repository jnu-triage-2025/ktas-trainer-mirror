# <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController"></a> Class ItemSubmissionUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

아이템 제출 패널의 MonoBehaviour 컨트롤러.

흐름:
 1) <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable" data-throw-if-not-resolved="false"></xref> 가 <xref href="MultiplayerInfrastructure.UI.ItemSubmissionUIController.Open(MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable%2cMultiplayerInfrastructure.Player.PlayerController)" data-throw-if-not-resolved="false"></xref> 을 호출 → 요구 아이템으로 패널을 구성하고 오버레이로 push.
 2) 매 프레임 플레이어 인벤토리 보유량으로 요구 칸 표시/제출 버튼 활성 상태를 갱신.
 3) 제출 버튼 클릭 → 요구 아이템을 소모(제거)하고, Interactable 에 완료를 통지 → 서버 세션 전역 신호가 올라간다.

인벤토리는 소유자(owner) 클라이언트 로컬 권한이므로, 소모/검증은 소유자 클라이언트에서 수행하고
완료 신호만 서버 권한 경로(<xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref>)로 라우팅한다.

```csharp
[RequireComponent(typeof(UIDocument))]
public class ItemSubmissionUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[ItemSubmissionUIController](MultiplayerInfrastructure.UI.ItemSubmissionUIController.md)

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

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_IsOpened"></a> IsOpened

```csharp
public bool IsOpened { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_Close"></a> Close\(\)

```csharp
public void Close()
```

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_Open_MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_MultiplayerInfrastructure_Player_PlayerController_"></a> Open\(ItemSubmissionInteractable, PlayerController\)

제출 패널을 특정 Interactable/플레이어 기준으로 연다.

```csharp
public void Open(ItemSubmissionInteractable interactable, PlayerController player)
```

#### Parameters

`interactable` [ItemSubmissionInteractable](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.md)

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

