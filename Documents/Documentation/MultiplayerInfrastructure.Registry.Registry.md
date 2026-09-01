# <a id="MultiplayerInfrastructure_Registry_Registry"></a> Class Registry

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class Registry
```

#### Inheritance

object ← 
[Registry](MultiplayerInfrastructure.Registry.Registry.md)

## Methods

### <a id="MultiplayerInfrastructure_Registry_Registry_Contains_MultiplayerInfrastructure_Registry_RegistryType_System_String_"></a> Contains\(RegistryType, string\)

```csharp
public static bool Contains(RegistryType registryType, string identifier)
```

#### Parameters

`registryType` [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

`identifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_CreateItemInstance_System_String_"></a> CreateItemInstance\(string\)

RegisterItemDefinition 으로 등록된 클래스로부터 새 Item 인스턴스를 생성합니다.
등록된 클래스가 없으면 null 을 반환합니다.

```csharp
public static Item CreateItemInstance(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

### <a id="MultiplayerInfrastructure_Registry_Registry_Get__1_MultiplayerInfrastructure_Registry_RegistryType_System_String_"></a> Get<T\>\(RegistryType, string\)

```csharp
public static T Get<T>(RegistryType registryType, string identifier)
```

#### Parameters

`registryType` [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

`identifier` string

#### Returns

 T

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_GetAll__1_MultiplayerInfrastructure_Registry_RegistryType_"></a> GetAll<T\>\(RegistryType\)

```csharp
public static IReadOnlyDictionary<string, T> GetAll<T>(RegistryType registryType)
```

#### Parameters

`registryType` [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

#### Returns

 IReadOnlyDictionary<string, T\>

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_GetAllEntities"></a> GetAllEntities\(\)

```csharp
public static IReadOnlyDictionary<string, EntityDescriptor> GetAllEntities()
```

#### Returns

 IReadOnlyDictionary<string, [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)\>

### <a id="MultiplayerInfrastructure_Registry_Registry_GetAllEntities_MultiplayerInfrastructure_Registry_EntityType_"></a> GetAllEntities\(EntityType\)

```csharp
public static IReadOnlyDictionary<string, EntityDescriptor> GetAllEntities(EntityType entityType)
```

#### Parameters

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

#### Returns

 IReadOnlyDictionary<string, [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)\>

### <a id="MultiplayerInfrastructure_Registry_Registry_GetAllEntityPresets"></a> GetAllEntityPresets\(\)

```csharp
public static IReadOnlyDictionary<string, EntityPresetDefinition> GetAllEntityPresets()
```

#### Returns

 IReadOnlyDictionary<string, [EntityPresetDefinition](MultiplayerInfrastructure.Registry.EntityPresetDefinition.md)\>

### <a id="MultiplayerInfrastructure_Registry_Registry_GetAllScenarioEvents"></a> GetAllScenarioEvents\(\)

등록된 모든 시나리오 이벤트를 반환합니다.

```csharp
public static IReadOnlyDictionary<string, ScenarioEventIdentifierRegistry.ScenarioEventHandler> GetAllScenarioEvents()
```

#### Returns

 IReadOnlyDictionary<string, [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md).[ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)\>

### <a id="MultiplayerInfrastructure_Registry_Registry_GetFirstEntityComponent__1_MultiplayerInfrastructure_Registry_EntityType_System_Predicate___0__"></a> GetFirstEntityComponent<T\>\(EntityType, Predicate<T\>\)

```csharp
public static T GetFirstEntityComponent<T>(EntityType entityType, Predicate<T> predicate = null) where T : Component
```

#### Parameters

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

`predicate` Predicate<T\>

#### Returns

 T

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_GetItemDisplayName_System_String_"></a> GetItemDisplayName\(string\)

identifier에 해당하는 아이템의 표시 이름을 반환합니다.

Item.DisplayName 은 인스턴스 프로퍼티이므로, 이 헬퍼는 내부적으로
<xref href="MultiplayerInfrastructure.Registry.Registry.CreateItemInstance(System.String)" data-throw-if-not-resolved="false"></xref> 로 임시 인스턴스를 생성해 이름만 읽어온다.
등록되지 않은 identifier 이면 identifier 자체를 그대로 반환한다(표시용 폴백).

```csharp
public static string GetItemDisplayName(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 string

### <a id="MultiplayerInfrastructure_Registry_Registry_GetOrLoadIconSprite_System_String_"></a> GetOrLoadIconSprite\(string\)

identifier에 해당하는 아이콘 스프라이트를 반환합니다.
<xref href="MultiplayerInfrastructure.Registry.Registry.GetOrLoadIconSprite(System.String%2cSystem.Type)" data-throw-if-not-resolved="false"></xref> 를 사용할 수 없는 경우에만 사용합니다.

```csharp
public static Sprite GetOrLoadIconSprite(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 Sprite

### <a id="MultiplayerInfrastructure_Registry_Registry_GetOrLoadIconSprite_System_String_System_Type_"></a> GetOrLoadIconSprite\(string, Type\)

identifier에 해당하는 아이콘 스프라이트를 반환합니다.

조회 우선순위:
  1. IconSprite 레지스트리 (이미 등록된 Sprite 또는 string 경로로부터 지연 로드)
  2. Resources/{DefaultsItemRegistry.ItemTexturesPath}/{identifier} 에서 직접 로드
  3. DefaultsResource.FallbackSprite 반환

<code class="paramref">itemType</code> 을 전달하면 <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute" data-throw-if-not-resolved="false"></xref>
기반 Type 검사를 우선 수행하여 더 정확한 억제 판정이 가능합니다.

```csharp
public static Sprite GetOrLoadIconSprite(string identifier, Type itemType)
```

#### Parameters

`identifier` string

`itemType` Type

#### Returns

 Sprite

### <a id="MultiplayerInfrastructure_Registry_Registry_IndexScenarioGraphAssetsFromResources"></a> IndexScenarioGraphAssetsFromResources\(\)

Registers Scenario TextAssets by identifier without parsing them. The first typed
Get/TryGet (or an explicit preload) performs schema validation and replaces the
TextAsset with the resolved graph. This keeps scene startup lightweight without
bypassing runtime validation.

```csharp
public static int IndexScenarioGraphAssetsFromResources()
```

#### Returns

 int

### <a id="MultiplayerInfrastructure_Registry_Registry_InvalidateAllIconSprites"></a> InvalidateAllIconSprites\(\)

IconSprite 레지스트리 전체를 비웁니다.

```csharp
public static void InvalidateAllIconSprites()
```

### <a id="MultiplayerInfrastructure_Registry_Registry_InvalidateIconSprite_System_String_"></a> InvalidateIconSprite\(string\)

특정 identifier의 IconSprite 등록을 제거합니다.

```csharp
public static void InvalidateIconSprite(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Registry_Registry_ParseProblemFigureIdentifier_System_String_"></a> ParseProblemFigureIdentifier\(string\)

```csharp
public static string ParseProblemFigureIdentifier(string figureReference)
```

#### Parameters

`figureReference` string

#### Returns

 string

### <a id="MultiplayerInfrastructure_Registry_Registry_PreloadProblemSet_System_String_"></a> PreloadProblemSet\(string\)

```csharp
public static bool PreloadProblemSet(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_PreloadProblemSetFromManifest_System_String_"></a> PreloadProblemSetFromManifest\(string\)

```csharp
public static bool PreloadProblemSetFromManifest(string manifestResourceName = "problem-pack.manifest")
```

#### Parameters

`manifestResourceName` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_PreloadScenarioGraph_System_String_System_Boolean_"></a> PreloadScenarioGraph\(string, bool\)

```csharp
public static bool PreloadScenarioGraph(string identifier, bool validateWithSchema = true)
```

#### Parameters

`identifier` string

`validateWithSchema` bool

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_PreloadScenarioGraphsFromResources_System_Boolean_"></a> PreloadScenarioGraphsFromResources\(bool\)

```csharp
public static int PreloadScenarioGraphsFromResources(bool validateWithSchema = true)
```

#### Parameters

`validateWithSchema` bool

#### Returns

 int

### <a id="MultiplayerInfrastructure_Registry_Registry_Register_MultiplayerInfrastructure_Registry_RegistryType_System_String_System_Object_"></a> Register\(RegistryType, string, object\)

```csharp
public static void Register(RegistryType registryType, string identifier, object definition)
```

#### Parameters

`registryType` [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

`identifier` string

`definition` object

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterEntity_MultiplayerInfrastructure_Registry_EntityDescriptor_"></a> RegisterEntity\(EntityDescriptor\)

```csharp
public static void RegisterEntity(EntityDescriptor descriptor)
```

#### Parameters

`descriptor` [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterEntity_System_String_MultiplayerInfrastructure_Registry_EntityType_UnityEngine_GameObject_System_String_System_String_System_Nullable_System_Int32__System_Boolean_"></a> RegisterEntity\(string, EntityType, GameObject, string, string, int?, bool\)

```csharp
public static void RegisterEntity(string identifier, EntityType entityType, GameObject gameObject, string displayName = null, string ownerUserIdentifier = null, int? clientId = null, bool isNetworked = false)
```

#### Parameters

`identifier` string

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

`gameObject` GameObject

`displayName` string

`ownerUserIdentifier` string

`clientId` int?

`isNetworked` bool

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterEntityPreset_MultiplayerInfrastructure_Registry_EntityPresetDefinition_"></a> RegisterEntityPreset\(EntityPresetDefinition\)

```csharp
public static void RegisterEntityPreset(EntityPresetDefinition definition)
```

#### Parameters

`definition` [EntityPresetDefinition](MultiplayerInfrastructure.Registry.EntityPresetDefinition.md)

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterEntityPreset_System_String_MultiplayerInfrastructure_Registry_EntityType_UnityEngine_GameObject_System_String_System_Boolean_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Registry_EntityPresetChildReference__"></a> RegisterEntityPreset\(string, EntityType, GameObject, string, bool, IReadOnlyList<EntityPresetChildReference\>\)

```csharp
public static void RegisterEntityPreset(string identifier, EntityType entityType, GameObject prefab, string displayName = null, bool isNetworked = false, IReadOnlyList<EntityPresetChildReference> childReferences = null)
```

#### Parameters

`identifier` string

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

`prefab` GameObject

`displayName` string

`isNetworked` bool

`childReferences` IReadOnlyList<[EntityPresetChildReference](MultiplayerInfrastructure.Registry.EntityPresetChildReference.md)\>

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterIconSprite_System_String_UnityEngine_Sprite_"></a> RegisterIconSprite\(string, Sprite\)

스프라이트를 IconSprite 레지스트리에 직접 등록합니다.

```csharp
public static void RegisterIconSprite(string identifier, Sprite sprite)
```

#### Parameters

`identifier` string

`sprite` Sprite

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterItemDefinition__1_System_String_"></a> RegisterItemDefinition<T\>\(string\)

ItemSystem.Item 파생 클래스의 Type을 레지스트리에 등록합니다.
identifier 는 Item.Identifier 와 일치시키는 것을 권장합니다.

```csharp
public static void RegisterItemDefinition<T>(string identifier) where T : Item, new()
```

#### Parameters

`identifier` string

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterProblemFigure_System_String_UnityEngine_Texture2D_"></a> RegisterProblemFigure\(string, Texture2D\)

```csharp
public static void RegisterProblemFigure(string identifier, Texture2D texture)
```

#### Parameters

`identifier` string

`texture` Texture2D

### <a id="MultiplayerInfrastructure_Registry_Registry_RegisterScenarioEvent_System_String_MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_ScenarioEventHandler_"></a> RegisterScenarioEvent\(string, ScenarioEventHandler\)

시나리오 이벤트 핸들러를 Registry와 ScenarioEventIdentifierRegistry에 동시 등록합니다.

```csharp
public static void RegisterScenarioEvent(string identifier, ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
```

#### Parameters

`identifier` string

`handler` [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md).[ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)

### <a id="MultiplayerInfrastructure_Registry_Registry_TryFindFirstEntityComponent__1_MultiplayerInfrastructure_Registry_EntityType___0__System_Predicate___0__"></a> TryFindFirstEntityComponent<T\>\(EntityType, out T, Predicate<T\>\)

```csharp
public static bool TryFindFirstEntityComponent<T>(EntityType entityType, out T component, Predicate<T> predicate = null) where T : Component
```

#### Parameters

`entityType` [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

`component` T

`predicate` Predicate<T\>

#### Returns

 bool

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGet__1_MultiplayerInfrastructure_Registry_RegistryType_System_String___0__"></a> TryGet<T\>\(RegistryType, string, out T\)

```csharp
public static bool TryGet<T>(RegistryType registryType, string identifier, out T value)
```

#### Parameters

`registryType` [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

`identifier` string

`value` T

#### Returns

 bool

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetEntity_System_String_MultiplayerInfrastructure_Registry_EntityDescriptor__"></a> TryGetEntity\(string, out EntityDescriptor\)

```csharp
public static bool TryGetEntity(string identifier, out EntityDescriptor descriptor)
```

#### Parameters

`identifier` string

`descriptor` [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetEntityByClientId_System_Int32_MultiplayerInfrastructure_Registry_EntityDescriptor__"></a> TryGetEntityByClientId\(int, out EntityDescriptor\)

```csharp
public static bool TryGetEntityByClientId(int clientId, out EntityDescriptor descriptor)
```

#### Parameters

`clientId` int

`descriptor` [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetEntityByOwnerUserIdentifier_System_String_MultiplayerInfrastructure_Registry_EntityDescriptor__"></a> TryGetEntityByOwnerUserIdentifier\(string, out EntityDescriptor\)

```csharp
public static bool TryGetEntityByOwnerUserIdentifier(string ownerUserIdentifier, out EntityDescriptor descriptor)
```

#### Parameters

`ownerUserIdentifier` string

`descriptor` [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetEntityPreset_System_String_MultiplayerInfrastructure_Registry_EntityPresetDefinition__"></a> TryGetEntityPreset\(string, out EntityPresetDefinition\)

```csharp
public static bool TryGetEntityPreset(string identifier, out EntityPresetDefinition definition)
```

#### Parameters

`identifier` string

`definition` [EntityPresetDefinition](MultiplayerInfrastructure.Registry.EntityPresetDefinition.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetProblemFigure_System_String_UnityEngine_Texture2D__"></a> TryGetProblemFigure\(string, out Texture2D\)

```csharp
public static bool TryGetProblemFigure(string identifier, out Texture2D texture)
```

#### Parameters

`identifier` string

`texture` Texture2D

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetProblemIdentifierFromManifest_System_String_System_String__"></a> TryGetProblemIdentifierFromManifest\(string, out string\)

```csharp
public static bool TryGetProblemIdentifierFromManifest(string manifestResourceName, out string problemIdentifier)
```

#### Parameters

`manifestResourceName` string

`problemIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetProblemSet_System_String_MultiplayerInfrastructure_Problem_ProblemSetDefinition__System_String__"></a> TryGetProblemSet\(string, out ProblemSetDefinition, out string\)

```csharp
public static bool TryGetProblemSet(string identifier, out ProblemSetDefinition problemSet, out string error)
```

#### Parameters

`identifier` string

`problemSet` [ProblemSetDefinition](MultiplayerInfrastructure.Problem.ProblemSetDefinition.md)

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetScenarioEvent_System_String_MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_ScenarioEventHandler__"></a> TryGetScenarioEvent\(string, out ScenarioEventHandler\)

등록된 시나리오 이벤트 핸들러를 조회합니다.

```csharp
public static bool TryGetScenarioEvent(string identifier, out ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
```

#### Parameters

`identifier` string

`handler` [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md).[ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryGetScenarioGraph_System_String_MultiplayerInfrastructure_Scenario_ScenarioGraph__System_String__"></a> TryGetScenarioGraph\(string, out ScenarioGraph, out string\)

등록된 식별자로 ScenarioGraph를 가져옵니다.
TextAsset으로 등록된 경우 파싱 후 캐싱됩니다.

```csharp
public static bool TryGetScenarioGraph(string identifier, out ScenarioGraph graph, out string error)
```

#### Parameters

`identifier` string

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TryResolveProblemFigureReference_System_String_UnityEngine_Texture2D__"></a> TryResolveProblemFigureReference\(string, out Texture2D\)

```csharp
public static bool TryResolveProblemFigureReference(string figureReference, out Texture2D texture)
```

#### Parameters

`figureReference` string

`texture` Texture2D

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TrySpawnEntityPreset_System_String_UnityEngine_Vector3_UnityEngine_Quaternion_UnityEngine_GameObject__MultiplayerInfrastructure_Registry_EntityDescriptor__System_String__"></a> TrySpawnEntityPreset\(string, Vector3, Quaternion, out GameObject, out EntityDescriptor, out string\)

```csharp
public static bool TrySpawnEntityPreset(string identifier, Vector3 position, Quaternion rotation, out GameObject spawned, out EntityDescriptor descriptor, out string error)
```

#### Parameters

`identifier` string

`position` Vector3

`rotation` Quaternion

`spawned` GameObject

`descriptor` [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TrySpawnEntityPreset_System_String_UnityEngine_Vector3_UnityEngine_Quaternion_System_String_UnityEngine_GameObject__MultiplayerInfrastructure_Registry_EntityDescriptor__System_String__"></a> TrySpawnEntityPreset\(string, Vector3, Quaternion, string, out GameObject, out EntityDescriptor, out string\)

엔티티 프리셋을 스폰한다.

동작:
 1) 프리셋의 프리팹을 (position, rotation) 에 인스턴스화하고, 자가 등록 컴포넌트
    (ISpawnedEntityIdentifierReceiver, 예: PatientController/MovingPatientBedController)가 있으면
    식별자를 주입한다. 없으면 프리셋의 fallbackEntityType 으로 직접 등록한다.
 2) 프리셋에 하위 참조(ChildReferences)가 있으면, 각 하위를 <b>등록된 다른 EntityPreset</b>으로 재귀 스폰한다.
    - unwrapOnSpawn=false: 하위 인스턴스를 루트의 자식으로 부착한다.
    - unwrapOnSpawn=true : 하위 인스턴스를 루트와 동일 계층(형제 루트)에 둔다(독립 루트). 환자+침대 결합 스폰용.
 3) 네트워크 프리셋(IsNetworked=true)이고 서버 컨텍스트이면, NetworkObject 를 FishNet ServerManager.Spawn 으로 복제한다.

<code class="paramref">desiredEntityIdentifier</code> 가 지정되면 루트 인스턴스를 해당 식별자로 등록한다(미지정 시 GUID).

```csharp
public static bool TrySpawnEntityPreset(string identifier, Vector3 position, Quaternion rotation, string desiredEntityIdentifier, out GameObject spawned, out EntityDescriptor descriptor, out string error)
```

#### Parameters

`identifier` string

`position` Vector3

`rotation` Quaternion

`desiredEntityIdentifier` string

`spawned` GameObject

`descriptor` [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Registry_Registry_TypeKey__1"></a> TypeKey<T\>\(\)

```csharp
public static string TypeKey<T>()
```

#### Returns

 string

#### Type Parameters

`T` 

### <a id="MultiplayerInfrastructure_Registry_Registry_TypeKey_System_Type_"></a> TypeKey\(Type\)

```csharp
public static string TypeKey(Type type)
```

#### Parameters

`type` Type

#### Returns

 string

### <a id="MultiplayerInfrastructure_Registry_Registry_Unregister_MultiplayerInfrastructure_Registry_RegistryType_System_String_"></a> Unregister\(RegistryType, string\)

```csharp
public static void Unregister(RegistryType registryType, string identifier)
```

#### Parameters

`registryType` [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

`identifier` string

### <a id="MultiplayerInfrastructure_Registry_Registry_UnregisterEntity_System_String_"></a> UnregisterEntity\(string\)

```csharp
public static void UnregisterEntity(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Registry_Registry_UnregisterEntityPreset_System_String_"></a> UnregisterEntityPreset\(string\)

```csharp
public static void UnregisterEntityPreset(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Registry_Registry_UnregisterScenarioEvent_System_String_"></a> UnregisterScenarioEvent\(string\)

시나리오 이벤트 핸들러 등록을 해제합니다.

```csharp
public static void UnregisterScenarioEvent(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Registry_Registry_UpdateEntityDisplayName_System_String_System_String_"></a> UpdateEntityDisplayName\(string, string\)

```csharp
public static void UpdateEntityDisplayName(string identifier, string displayName)
```

#### Parameters

`identifier` string

`displayName` string

### <a id="MultiplayerInfrastructure_Registry_Registry_OnEntryRegistered"></a> OnEntryRegistered

```csharp
public static event Action<RegistryType, string, object> OnEntryRegistered
```

#### Event Type

 Action<[RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md), string, object\>

### <a id="MultiplayerInfrastructure_Registry_Registry_OnEntryUnregistered"></a> OnEntryUnregistered

```csharp
public static event Action<RegistryType, string> OnEntryUnregistered
```

#### Event Type

 Action<[RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md), string\>

