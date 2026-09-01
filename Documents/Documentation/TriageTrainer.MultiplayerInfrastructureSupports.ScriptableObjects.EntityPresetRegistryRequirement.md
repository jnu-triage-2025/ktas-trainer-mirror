# <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement"></a> Struct EntityPresetRegistryRequirement

Namespace: [TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects](TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public struct EntityPresetRegistryRequirement
```

## Fields

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement_childReferences"></a> childReferences

```csharp
[Tooltip("이 프리셋과 함께 스폰할 하위 엔티티 프리셋 참조 목록. 하위는 원본 프리팹이 아니라 '이미 등록된 다른 EntityPreset 의 식별자' 로 가리킨다. 예) 환자 그룹 프리셋이 침대 프리셋(bed_a)을 unwrap 으로 함께 스폰.")]
public EntityPresetChildReference[] childReferences
```

#### Field Value

 [EntityPresetChildReference](MultiplayerInfrastructure.Registry.EntityPresetChildReference.md)\[\]

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement_displayName"></a> displayName

```csharp
public string displayName
```

#### Field Value

 string

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement_fallbackEntityType"></a> fallbackEntityType

```csharp
[Tooltip("폴백 등록용 EntityType(기본값 Undefined). 프리팹이 스스로 레지스트리에 등록하는 컴포넌트(ISpawnedEntityIdentifierReceiver, 예: PatientController/MovingPatientBedController) 를 가지면 이 값은 무시되고 컴포넌트가 자기 EntityType 으로 등록한다. 자가 등록 컴포넌트가 없는 단순 프리팹에만 적용된다.")]
public EntityType fallbackEntityType
```

#### Field Value

 [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement_identifier"></a> identifier

```csharp
public string identifier
```

#### Field Value

 string

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement_isNetworked"></a> isNetworked

```csharp
public bool isNetworked
```

#### Field Value

 bool

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_ScriptableObjects_EntityPresetRegistryRequirement_prefab"></a> prefab

```csharp
public GameObject prefab
```

#### Field Value

 GameObject

