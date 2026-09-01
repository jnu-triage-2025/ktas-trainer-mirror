# <a id="MultiplayerInfrastructure_UI_InventoryUIView"></a> Class InventoryUIView

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

Pure view responsible for inventory slot visuals and interactions.
Intended to be instantiated from UXML; controller owns lifecycle and data binding.

```csharp
[UxmlElement]
public class InventoryUIView : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[InventoryUIView](MultiplayerInfrastructure.UI.InventoryUIView.md)

## Properties

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_BoundSlots"></a> BoundSlots

```csharp
public IReadOnlyList<InventorySlotModelDTO> BoundSlots { get; }
```

#### Property Value

 IReadOnlyList<InventorySlotModelDTO\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_EquipmentSlots"></a> EquipmentSlots

현재 바인딩된 장비 슬롯 데이터 (PlayerController 소유).

```csharp
public IReadOnlyList<EquipmentSlotModelDTO> EquipmentSlots { get; }
```

#### Property Value

 IReadOnlyList<[EquipmentSlotModelDTO](MultiplayerInfrastructure.Player.EquipmentSlotModelDTO.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_IsVisible"></a> IsVisible

```csharp
public bool IsVisible { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_BindEquipment_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Player_EquipmentSlotModelDTO__"></a> BindEquipment\(IReadOnlyList<EquipmentSlotModelDTO\>\)

PlayerController 의 장비 슬롯 데이터를 뷰에 바인딩합니다.
공유 참조를 사용하므로 뷰의 변경이 PlayerController 에 즉각 반영됩니다.

```csharp
public void BindEquipment(IReadOnlyList<EquipmentSlotModelDTO> equipmentSlots)
```

#### Parameters

`equipmentSlots` IReadOnlyList<[EquipmentSlotModelDTO](MultiplayerInfrastructure.Player.EquipmentSlotModelDTO.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_Initialize_System_Int32_System_Int32_UnityEngine_UIElements_VisualTreeAsset_UnityEngine_Texture2D_"></a> Initialize\(int, int, VisualTreeAsset, Texture2D\)

```csharp
public void Initialize(int columns, int rows, VisualTreeAsset slotTemplate, Texture2D defaultIcon)
```

#### Parameters

`columns` int

`rows` int

`slotTemplate` VisualTreeAsset

`defaultIcon` Texture2D

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_RebuildGrid_System_Int32_System_Int32_"></a> RebuildGrid\(int, int\)

```csharp
public void RebuildGrid(int newColumns, int newRows)
```

#### Parameters

`newColumns` int

`newRows` int

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_ReturnHeldItemToInventoryOnClose"></a> ReturnHeldItemToInventoryOnClose\(\)

```csharp
public void ReturnHeldItemToInventoryOnClose()
```

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_SetCraftingCallbacks_System_Func_System_String_System_Int32__System_Func_MultiplayerInfrastructure_UI_InventoryUIView_CraftableRecipeDisplay_MultiplayerInfrastructure_ItemSystem_Item__"></a> SetCraftingCallbacks\(Func<string, int\>, Func<CraftableRecipeDisplay, Item\>\)

컨트롤러가 조합에 필요한 콜백을 주입한다.

```csharp
public void SetCraftingCallbacks(Func<string, int> heldCountResolver, Func<InventoryUIView.CraftableRecipeDisplay, Item> craftRequestHandler)
```

#### Parameters

`heldCountResolver` Func<string, int\>

`craftRequestHandler` Func<[InventoryUIView](MultiplayerInfrastructure.UI.InventoryUIView.md).[CraftableRecipeDisplay](MultiplayerInfrastructure.UI.InventoryUIView.CraftableRecipeDisplay.md), [Item](MultiplayerInfrastructure.ItemSystem.Item.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_SetVisible_System_Boolean_"></a> SetVisible\(bool\)

```csharp
public void SetVisible(bool visible)
```

#### Parameters

`visible` bool

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_TrySwapHoveredSlotWithHotbarSlot_System_Int32_"></a> TrySwapHoveredSlotWithHotbarSlot\(int\)

커서가 올라가 있는 인벤토리 슬롯과 지정한 핫바 슬롯의 아이템을 서로 맞바꾼다.
핫바는 인벤토리 그리드의 첫 번째 행과 같은 데이터를 공유하므로,
핫바 슬롯 인덱스를 그대로 인벤토리 슬롯 인덱스로 사용한다.
커서에 아이템을 들고 있는 동안에는 어느 쪽으로 옮길지가 모호하므로 동작하지 않는다.

```csharp
public bool TrySwapHoveredSlotWithHotbarSlot(int hotbarSlotIndex)
```

#### Parameters

`hotbarSlotIndex` int

#### Returns

 bool

실제로 교환이 일어났으면 true.

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_UpdateCraftableRecipes_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_UI_InventoryUIView_CraftableRecipeDisplay__"></a> UpdateCraftableRecipes\(IReadOnlyList<CraftableRecipeDisplay\>\)

조합 가능한 레시피 목록을 갱신한다. 컨트롤러가 인벤토리 변화 시마다 호출한다.
선택 상태는 가능하면 유지하되, 더 이상 조합 불가능한 레시피면 선택을 해제한다.

```csharp
public void UpdateCraftableRecipes(IReadOnlyList<InventoryUIView.CraftableRecipeDisplay> recipes)
```

#### Parameters

`recipes` IReadOnlyList<[InventoryUIView](MultiplayerInfrastructure.UI.InventoryUIView.md).[CraftableRecipeDisplay](MultiplayerInfrastructure.UI.InventoryUIView.CraftableRecipeDisplay.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_UpdateInventory_System_Collections_Generic_IReadOnlyList_InventorySlotModelDTO__"></a> UpdateInventory\(IReadOnlyList<InventorySlotModelDTO\>\)

```csharp
public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
```

#### Parameters

`slots` IReadOnlyList<InventorySlotModelDTO\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_CraftLeftoverReturned"></a> CraftLeftoverReturned

커서 스택 한도를 초과해 커서에 담지 못한 조합 결과 잔량을 인벤토리로 돌려보내기 위한 이벤트.
컨트롤러가 구독하여 플레이어 인벤토리에 다시 추가한다.

```csharp
public event Action<Item> CraftLeftoverReturned
```

#### Event Type

 Action<[Item](MultiplayerInfrastructure.ItemSystem.Item.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_EquipmentSlotMutated"></a> EquipmentSlotMutated

장비 슬롯의 아이템 변화(장착/해제) 발생 시.

```csharp
public event Action EquipmentSlotMutated
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_ItemDroppedOutside"></a> ItemDroppedOutside

```csharp
public event Action<Item> ItemDroppedOutside
```

#### Event Type

 Action<[Item](MultiplayerInfrastructure.ItemSystem.Item.md)\>

### <a id="MultiplayerInfrastructure_UI_InventoryUIView_SlotsMutated"></a> SlotsMutated

```csharp
public event Action SlotsMutated
```

#### Event Type

 Action

