# <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem"></a> Class StaticPlacedItem

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

유니티 에디터에 사전 배치되는 정적 아이템입니다.

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.ItemObject" data-throw-if-not-resolved="false"></xref>(월드 드롭 아이템)와 달리 Rigidbody 물리로 스폰/산란하지 않습니다.
네트워크 스폰 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이며, 엔티티가 아닌 것에 가깝지만
(Registry에는 편의를 위해 <xref href="MultiplayerInfrastructure.Registry.EntityType.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 로 등록됨) Interactable 합니다.
상호작용(획득)하면 아이템을 얻고, 설정에 따라 맵에서 사라지게 할 수 있습니다.
</p>

<p>
배치가 에디터 표현 그대로 유지되므로 서버 시작 시 위치가 흩어지거나 서로 충돌하는 문제가 없습니다.
</p>

<p>
상태(Remains)의 진실 원천은 서버이며 <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemService" data-throw-if-not-resolved="false"></xref> 가 관리합니다.
상호작용 요청은 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 서버 권위 픽업 프로토콜로 위임됩니다.
</p>

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class StaticPlacedItem : Interactable, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget, IInteractorConditional
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md) ← 
[StaticPlacedItem](MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md)

#### Inherited Members

[Interactable.DisplayText](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayText), 
[Interactable.DisplayIcon](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcon), 
[Interactable.DisplayIcons](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcons), 
[Interactable.AllowDisplayIconFallback](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_AllowDisplayIconFallback), 
[Interactable.DisplayColor](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayColor), 
[Interactable.PresentationEntityIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_PresentationEntityIdentifier), 
[Interactable.InteractionIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_InteractionIdentifier), 
[Interactable.Interacts](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interacts), 
[Interactable.Interact\(Transform\)](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interact\_UnityEngine\_Transform\_)

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public override Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public override string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_EntityIdentifier"></a> EntityIdentifier

이 정적 아이템의 전역 식별자(서버/모든 클라이언트 동일).

```csharp
public string EntityIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_HasAnyReward"></a> HasAnyReward

유효한(아이템 식별자가 비어 있지 않은) 보상이 하나라도 존재하는지 여부입니다.
목록이 비어 있거나 모든 항목이 무효하면 이 아이템은 획득 대상에서 무시됩니다.

```csharp
public bool HasAnyReward { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_InitialRemains"></a> InitialRemains

```csharp
public int InitialRemains { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_PickupRewards"></a> PickupRewards

획득 시 지급되는 보상 목록(읽기 전용). 아이템 식별자가 비어 있는 항목은 유효하지 않은 것으로 간주됩니다.

```csharp
public IReadOnlyList<StaticPlacedItemPickupReward> PickupRewards { get; }
```

#### Property Value

 IReadOnlyList<[StaticPlacedItemPickupReward](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemPickupReward.md)\>

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_ValidRewardCount"></a> ValidRewardCount

유효한 보상 항목의 개수입니다.

```csharp
public int ValidRewardCount { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_VanishBehavior"></a> VanishBehavior

```csharp
public StaticPlacedItemVanishBehavior VanishBehavior { get; }
```

#### Property Value

 [StaticPlacedItemVanishBehavior](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishBehavior.md)

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_VanishMode"></a> VanishMode

```csharp
public StaticPlacedItemVanishMode VanishMode { get; }
```

#### Property Value

 [StaticPlacedItemVanishMode](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishMode.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_ApplyRestored"></a> ApplyRestored\(\)

로컬 프로세스에서 사라짐 상태를 해제하고 다시 존재/획득 가능하게 되돌립니다.

<p>
현재 프로토콜은 사라짐을 단방향으로만 처리하므로 이 메서드는 의도적으로 호출되지 않는다.
향후 런타임 복원(관리자 리셋 / Remains 재충전 / 리스폰) 기능이 추가될 때 사용하기 위한 예약 API 이다.
</p>

```csharp
public void ApplyRestored()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_ApplyVanished"></a> ApplyVanished\(\)

로컬 프로세스에서 이 정적 아이템을 사라짐(Vanished) 상태로 적용합니다.
VanishBehavior 에 따라 렌더러/오브젝트/상호작용을 처리합니다.

```csharp
public void ApplyVanished()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

사라짐(Vanished) 상태에서는 어떤 <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishBehavior" data-throw-if-not-resolved="false"></xref> 든 상호작용을 항상 차단합니다.

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public override void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItem_TryGetPrimaryReward_MultiplayerInfrastructure_ItemSystem_StaticPlacedItemPickupReward__"></a> TryGetPrimaryReward\(out StaticPlacedItemPickupReward\)

표시(아이콘/모델)의 기준이 되는 대표 보상(첫 번째 유효 항목)을 반환합니다.
유효한 보상이 없으면 false 를 반환합니다.

```csharp
public bool TryGetPrimaryReward(out StaticPlacedItemPickupReward primary)
```

#### Parameters

`primary` [StaticPlacedItemPickupReward](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemPickupReward.md)

#### Returns

 bool

