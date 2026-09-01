# <a id="MultiplayerInfrastructure_UI_HotbarControl"></a> Class HotbarControl

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class HotbarControl : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[HotbarControl](MultiplayerInfrastructure.UI.HotbarControl.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_HotbarControl__ctor"></a> HotbarControl\(\)

```csharp
public HotbarControl()
```

## Fields

### <a id="MultiplayerInfrastructure_UI_HotbarControl_MaxSlotSize"></a> MaxSlotSize

```csharp
public const int MaxSlotSize = 10
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_UI_HotbarControl_MinSlotSize"></a> MinSlotSize

```csharp
public const int MinSlotSize = 1
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_UI_HotbarControl_OnHeldItemNameChanged"></a> OnHeldItemNameChanged

현재 선택된(손에 들고 있는) 슬롯의 아이템이 변경되었을 때 호출됩니다.
변경으로 간주되는 경우:
- 선택 슬롯이 바뀐 경우
- 선택 슬롯의 아이템 인스턴스 또는 표시 이름이 바뀐 경우
슬롯이 비어있으면 null 을 전달합니다.

```csharp
public Action<string> OnHeldItemNameChanged
```

#### Field Value

 Action<string\>

### <a id="MultiplayerInfrastructure_UI_HotbarControl_OnSlotSelected"></a> OnSlotSelected

```csharp
public Action<int> OnSlotSelected
```

#### Field Value

 Action<int\>

## Properties

### <a id="MultiplayerInfrastructure_UI_HotbarControl_HotbarSlotCount"></a> HotbarSlotCount

```csharp
[UxmlAttribute("slot-count")]
public int HotbarSlotCount { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_UI_HotbarControl_SlotCount"></a> SlotCount

```csharp
public int SlotCount { get; }
```

#### Property Value

 int

## Methods

### <a id="MultiplayerInfrastructure_UI_HotbarControl_BindInventory_System_Collections_Generic_IReadOnlyList_InventorySlotModelDTO__"></a> BindInventory\(IReadOnlyList<InventorySlotModelDTO\>\)

```csharp
public void BindInventory(IReadOnlyList<InventorySlotModelDTO> inventory)
```

#### Parameters

`inventory` IReadOnlyList<InventorySlotModelDTO\>

### <a id="MultiplayerInfrastructure_UI_HotbarControl_CycleSelection_System_Int32_"></a> CycleSelection\(int\)

```csharp
public void CycleSelection(int direction)
```

#### Parameters

`direction` int

### <a id="MultiplayerInfrastructure_UI_HotbarControl_ForceRefresh"></a> ForceRefresh\(\)

```csharp
public void ForceRefresh()
```

### <a id="MultiplayerInfrastructure_UI_HotbarControl_GetSelectedIndex"></a> GetSelectedIndex\(\)

```csharp
public int GetSelectedIndex()
```

#### Returns

 int

### <a id="MultiplayerInfrastructure_UI_HotbarControl_Initialize_System_Int32_"></a> Initialize\(int\)

```csharp
public void Initialize(int slotCount)
```

#### Parameters

`slotCount` int

### <a id="MultiplayerInfrastructure_UI_HotbarControl_SetSelectedIndex_System_Int32_"></a> SetSelectedIndex\(int\)

```csharp
public void SetSelectedIndex(int index)
```

#### Parameters

`index` int

