# <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemState"></a> Struct StaticPlacedItemState

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 의 런타임 상태 정의입니다. (사양의 State)

인스펙터에 배치된 값은 "초기 상태"이며, 런타임 중 서버가 권위 있게 관리합니다.
- <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishMode.VanishedGlobalOnPickup" data-throw-if-not-resolved="false"></xref>: 서버 전역에서 하나의 Remains.
- <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishMode.VanishedLocalOnPickup" data-throw-if-not-resolved="false"></xref>: 플레이어(UserIdentifier)마다 이 초기값에서 시작.

```csharp
[Serializable]
public struct StaticPlacedItemState
```

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemState_Remains"></a> Remains

남은 획득 가능 횟수.

```csharp
public int Remains { get; set; }
```

#### Property Value

 int

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemState_CreateDefault"></a> CreateDefault\(\)

```csharp
public static StaticPlacedItemState CreateDefault()
```

#### Returns

 [StaticPlacedItemState](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemState.md)

