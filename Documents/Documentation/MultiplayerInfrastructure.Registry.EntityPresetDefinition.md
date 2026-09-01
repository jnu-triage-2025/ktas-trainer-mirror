# <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition"></a> Class EntityPresetDefinition

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

엔티티 프리셋 정의: 하나의 프리팹에 사전 설정(EntityType 폴백/표시명/네트워크 여부)과 식별자를 부여해 저장한 단위.

하위 오브젝트는 <xref href="MultiplayerInfrastructure.Registry.EntityPresetDefinition.ChildReferences" data-throw-if-not-resolved="false"></xref> 로 표현하며, 각 항목은 <b>다른 EntityPreset 의 식별자</b>를 가리킨다.
(원본 프리팹의 Transform 자식을 경로로 가리키지 않는다.) 스폰 시 루트 프리셋과 각 하위 프리셋이 독립적으로
인스턴스화되며, unwrap 옵션에 따라 하위를 루트의 자식으로 둘지 또는 루트와 동일 계층(형제 루트)에 둘지 결정한다.

```csharp
[Serializable]
public sealed class EntityPresetDefinition
```

#### Inheritance

object ← 
[EntityPresetDefinition](MultiplayerInfrastructure.Registry.EntityPresetDefinition.md)

## Constructors

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition__ctor_System_String_MultiplayerInfrastructure_Registry_EntityType_UnityEngine_GameObject_System_String_System_Boolean_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Registry_EntityPresetChildReference__"></a> EntityPresetDefinition\(string, EntityType, GameObject, string, bool, IReadOnlyList<EntityPresetChildReference\>\)

```csharp
public EntityPresetDefinition(string identifier, EntityType entityType, GameObject prefab, string displayName = null, bool isNetworked = false, IReadOnlyList<EntityPresetChildReference> childReferences = null)
```

#### Parameters

`identifier` string

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

`prefab` GameObject

`displayName` string

`isNetworked` bool

`childReferences` IReadOnlyList<[EntityPresetChildReference](MultiplayerInfrastructure.Registry.EntityPresetChildReference.md)\>

## Properties

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition_ChildReferences"></a> ChildReferences

이 프리셋과 함께 스폰할 하위 엔티티 프리셋 참조 목록(다른 EntityPreset 식별자 기반).
비어 있으면 하위 없이 단일 프리셋으로 스폰된다.

```csharp
public IReadOnlyList<EntityPresetChildReference> ChildReferences { get; }
```

#### Property Value

 IReadOnlyList<[EntityPresetChildReference](MultiplayerInfrastructure.Registry.EntityPresetChildReference.md)\>

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition_DisplayName"></a> DisplayName

```csharp
public string DisplayName { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition_EntityType"></a> EntityType

```csharp
public EntityType EntityType { get; }
```

#### Property Value

 [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition_IsNetworked"></a> IsNetworked

```csharp
public bool IsNetworked { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Registry_EntityPresetDefinition_Prefab"></a> Prefab

```csharp
public GameObject Prefab { get; }
```

#### Property Value

 GameObject

