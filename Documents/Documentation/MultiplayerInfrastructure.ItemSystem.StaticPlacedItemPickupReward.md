# <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemPickupReward"></a> Struct StaticPlacedItemPickupReward

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 을 1회 획득(Pickup)할 때 지급되는 보상 및 상태 변화 정의입니다.
(사양의 OnPickupTry)

```csharp
[Serializable]
public struct StaticPlacedItemPickupReward
```

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemPickupReward_Amount"></a> Amount

획득할 아이템 수량(최소 1).

```csharp
public int Amount { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemPickupReward_DecreaseRemains"></a> DecreaseRemains

1회 획득 시 Remains 감소량(음수 방지, 0이면 감소하지 않음).

```csharp
public int DecreaseRemains { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemPickupReward_ItemIdentifier"></a> ItemIdentifier

획득할 아이템 식별자.

```csharp
public string ItemIdentifier { get; set; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemPickupReward_CreateDefault"></a> CreateDefault\(\)

```csharp
public static StaticPlacedItemPickupReward CreateDefault()
```

#### Returns

 [StaticPlacedItemPickupReward](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemPickupReward.md)

