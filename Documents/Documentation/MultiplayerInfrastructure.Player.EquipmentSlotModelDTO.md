# <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO"></a> Class EquipmentSlotModelDTO

Namespace: [MultiplayerInfrastructure.Player](MultiplayerInfrastructure.Player.md)  
Assembly: Assembly\-CSharp.dll  

플레이어 장비 슬롯 하나를 나타냅니다.
<xref href="MultiplayerInfrastructure.Player.EquipmentSlotType" data-throw-if-not-resolved="false"></xref> 에 해당하는 아이템만 장착할 수 있으며,
내부에 <xref href="InventorySlotModelDTO" data-throw-if-not-resolved="false"></xref> 를保有하여 아이템을 관리합니다.

```csharp
[Serializable]
public class EquipmentSlotModelDTO
```

#### Inheritance

object ← 
[EquipmentSlotModelDTO](MultiplayerInfrastructure.Player.EquipmentSlotModelDTO.md)

## Constructors

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO__ctor_MultiplayerInfrastructure_Player_EquipmentSlotType_"></a> EquipmentSlotModelDTO\(EquipmentSlotType\)

```csharp
public EquipmentSlotModelDTO(EquipmentSlotType slotType)
```

#### Parameters

`slotType` [EquipmentSlotType](MultiplayerInfrastructure.Player.EquipmentSlotType.md)

## Fields

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_SlotType"></a> SlotType

```csharp
public readonly EquipmentSlotType SlotType
```

#### Field Value

 [EquipmentSlotType](MultiplayerInfrastructure.Player.EquipmentSlotType.md)

## Properties

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_InnerSlot"></a> InnerSlot

내부 슬롯 DTO 에 대한 참조를 반환합니다 (UI 바인딩용).

```csharp
public InventorySlotModelDTO InnerSlot { get; }
```

#### Property Value

 InventorySlotModelDTO

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_IsEmpty"></a> IsEmpty

이 슬롯이 비어 있는지 여부.

```csharp
public bool IsEmpty { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_ItemInstance"></a> ItemInstance

현재 장착된 아이템 인스턴스 (null = 비어있음).

```csharp
public Item? ItemInstance { get; }
```

#### Property Value

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)?

## Methods

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_CanAccept_MultiplayerInfrastructure_ItemSystem_Item_"></a> CanAccept\(Item?\)

지정 아이템이 이 장비 슬롯에 장착 가능한지 확인합니다.
아이템 클래스에 적용된 장비 Attribute 를 리플렉션으로 검사합니다.

```csharp
public bool CanAccept(Item? item)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)?

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_Clear"></a> Clear\(\)

장비 슬롯을 비웁니다.

```csharp
public void Clear()
```

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_Equip_MultiplayerInfrastructure_ItemSystem_Item_"></a> Equip\(Item?\)

아이템을 장착합니다. <xref href="MultiplayerInfrastructure.Player.EquipmentSlotModelDTO.CanAccept(MultiplayerInfrastructure.ItemSystem.Item)" data-throw-if-not-resolved="false"></xref> 검사를 먼저 수행해야 합니다.
기존에 장착된 아이템을 반환합니다 (없으면 null).

```csharp
public Item? Equip(Item? newItem)
```

#### Parameters

`newItem` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)?

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)?

### <a id="MultiplayerInfrastructure_Player_EquipmentSlotModelDTO_Unequip"></a> Unequip\(\)

장착된 아이템을 해제(반환)합니다. 비어 있으면 null.

```csharp
public Item? Unequip()
```

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)?

