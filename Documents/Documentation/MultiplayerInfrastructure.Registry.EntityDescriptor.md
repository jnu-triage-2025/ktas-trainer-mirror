# <a id="MultiplayerInfrastructure_Registry_EntityDescriptor"></a> Class EntityDescriptor

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

Registry.Entity 저장소에 등록되는 단일 엔티티 설명자입니다.

- Identifier: 서버/모든 클라이언트에서 동일해야 하는 전역 고유 식별자
- EntityType: 엔티티 종류
- GameObject: 로컬 프로세스에서 이 엔티티를 나타내는 실제 게임 오브젝트
- OwnerUserIdentifier / ClientId: 플레이어 엔티티일 때 연결 정보

```csharp
[Serializable]
public sealed class EntityDescriptor
```

#### Inheritance

object ← 
[EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

## Constructors

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor__ctor_System_String_MultiplayerInfrastructure_Registry_EntityType_UnityEngine_GameObject_System_String_System_String_System_Nullable_System_Int32__System_Boolean_"></a> EntityDescriptor\(string, EntityType, GameObject, string, string, int?, bool\)

```csharp
public EntityDescriptor(string identifier, EntityType entityType, GameObject gameObject, string displayName = null, string ownerUserIdentifier = null, int? clientId = null, bool isNetworked = false)
```

#### Parameters

`identifier` string

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

`gameObject` GameObject

`displayName` string

`ownerUserIdentifier` string

`clientId` int?

`isNetworked` bool

## Properties

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_ClientId"></a> ClientId

```csharp
public int? ClientId { get; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_DisplayName"></a> DisplayName

```csharp
public string DisplayName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_EntityType"></a> EntityType

```csharp
public EntityType EntityType { get; }
```

#### Property Value

 [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_GameObject"></a> GameObject

```csharp
public GameObject GameObject { get; }
```

#### Property Value

 GameObject

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_IsNetworked"></a> IsNetworked

```csharp
public bool IsNetworked { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_OwnerUserIdentifier"></a> OwnerUserIdentifier

```csharp
public string OwnerUserIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Registry_EntityDescriptor_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

