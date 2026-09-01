# <a id="MultiplayerInfrastructure_UI_InventoryUIController"></a> Class InventoryUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

MonoBehaviour controller that owns the view, handles overlay lifecycle, and is accessed via Registry.Registry.

```csharp
[RequireComponent(typeof(UIDocument))]
public class InventoryUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[InventoryUIController](MultiplayerInfrastructure.UI.InventoryUIController.md)

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

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_IsOpened"></a> IsOpened

```csharp
public bool IsOpened { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_RebuildGrid_System_Int32_System_Int32_"></a> RebuildGrid\(int, int\)

```csharp
public void RebuildGrid(int newColumns, int newRows)
```

#### Parameters

`newColumns` int

`newRows` int

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_ToggleRoot_System_Boolean_"></a> ToggleRoot\(bool\)

```csharp
public void ToggleRoot(bool visible)
```

#### Parameters

`visible` bool

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_TrySwapHoveredSlotWithHotbarSlot_System_Int32_"></a> TrySwapHoveredSlotWithHotbarSlot\(int\)

커서가 올라가 있는 인벤토리 슬롯과 지정한 핫바 슬롯의 아이템을 서로 맞바꾼다.
인벤토리가 열려 있는 동안 핫바 숫자 키 입력을 처리하기 위해 PlayerController 가 호출한다.

```csharp
public bool TrySwapHoveredSlotWithHotbarSlot(int hotbarSlotIndex)
```

#### Parameters

`hotbarSlotIndex` int

#### Returns

 bool

실제로 교환이 일어났으면 true.

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_UpdateEquipment_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Player_EquipmentSlotModelDTO__"></a> UpdateEquipment\(IReadOnlyList<EquipmentSlotModelDTO\>\)

PlayerController 의 장비 슬롯 데이터를 뷰에 바인딩합니다.

```csharp
public void UpdateEquipment(IReadOnlyList<EquipmentSlotModelDTO> equipmentSlots)
```

#### Parameters

`equipmentSlots` IReadOnlyList<[EquipmentSlotModelDTO](MultiplayerInfrastructure.Player.EquipmentSlotModelDTO.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_UpdateInventory_System_Collections_Generic_IReadOnlyList_InventorySlotModelDTO__"></a> UpdateInventory\(IReadOnlyList<InventorySlotModelDTO\>\)

```csharp
public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
```

#### Parameters

`slots` IReadOnlyList<InventorySlotModelDTO\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_OnItemAtSelectedSlotChanged"></a> OnItemAtSelectedSlotChanged

```csharp
public event Action OnItemAtSelectedSlotChanged
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_InventoryUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

