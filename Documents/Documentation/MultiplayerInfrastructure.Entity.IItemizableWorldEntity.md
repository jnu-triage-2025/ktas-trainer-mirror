# <a id="MultiplayerInfrastructure_Entity_IItemizableWorldEntity"></a> Interface IItemizableWorldEntity

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

좌클릭으로 월드 아이템으로 되돌릴 수 있는 설치형 엔티티의 규약입니다.

```csharp
public interface IItemizableWorldEntity
```

## Properties

### <a id="MultiplayerInfrastructure_Entity_IItemizableWorldEntity_ItemizationEntityIdentifier"></a> ItemizationEntityIdentifier

서버가 회수 대상을 다시 찾는 데 사용하는 런타임 엔티티 식별자입니다.

```csharp
string ItemizationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Entity_IItemizableWorldEntity_RequestItemization_MultiplayerInfrastructure_Player_PlayerController_"></a> RequestItemization\(PlayerController\)

아이템화를 요청한다. 요청을 수락했으면 true를 반환한다.

```csharp
bool RequestItemization(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_IItemizableWorldEntity_TryItemizeOnServer_MultiplayerInfrastructure_Player_PlayerController_"></a> TryItemizeOnServer\(PlayerController\)

서버 권한 아래서 월드 아이템 생성과 원본 엔티티 제거를 수행합니다.

```csharp
bool TryItemizeOnServer(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

