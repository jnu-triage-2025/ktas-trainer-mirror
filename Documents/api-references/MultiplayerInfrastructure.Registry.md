# API 레퍼런스: `MultiplayerInfrastructure.Registry`

> **네임스페이스:** `MultiplayerInfrastructure.Registry`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/`

---

## 0. 문서 목적

`Registry`는 모듈 전체에서 공유되는 정적 중앙 저장소입니다.  
싱글톤이나 직접 참조 대신 `Registry`를 통해 컴포넌트와 에셋을 등록·조회하면 시스템 간 결합도를 낮출 수 있습니다.

---

## 1. RegistryType

```csharp
public enum RegistryType
{
    Item,               // Item 컴포넌트 (프리팹 템플릿)
    ScenarioGraph,      // ScenarioGraph 또는 TextAsset (JSON)
    IconSprite,         // Sprite (아이콘)
    Npc,                // NPC GameObject
    Waypoint,           // Vector3 웨이포인트 위치
    Entity,             // EntityDescriptor 기반 월드 엔티티 저장소
    Service,            // QuestManager, MainCameraController 등 서비스 오브젝트
    RuntimeState,       // SessionInformation, UserDisplayName 등 전역 상태값
    InteractableEntity, // IInteractable 구현 컴포넌트
    UI,                 // UIControllerABC 하위 컨트롤러
    PlayerTag,          // List<string> — 키: UserDescriptor.Identifier(UUID)
}
```

`PlayerTag` 설계 메모:

- 플레이어 태그는 `RegistryType.Entity`의 `EntityDescriptor`(플레이어 기술자)에 직접 보관하지 않고, `RegistryType.PlayerTag`에 별도 저장합니다.
- 목적은 태그 기반 플레이어 조회를 단순화하는 것입니다. 키를 `UserDescriptor.Identifier(UUID)`로 고정해 `PlayerTagService`에서 일관되게 질의/변경할 수 있습니다.
- `EntityDescriptor`에서 태그로 들어가는 역방향 참조(플레이어 기술자 -> 태그 인덱스)까지 함께 유지하면, 태그 변경/플레이어 재접속/엔티티 재등록 시 동기화 지점이 늘어나 관리 비용이 크게 증가합니다.
- 이 이유로 역방향 구조는 의도적으로 추가하지 않았습니다.

---

## 2. 공개 메서드

### `Register`

```csharp
public static void Register(RegistryType registryType, string identifier, object definition)
```

지정된 레지스트리에 오브젝트를 등록합니다.  
`identifier`가 비어있거나 `definition`이 `null`이면 무시됩니다. 기존 값이 있으면 덮어씁니다.

---

### `Unregister`

```csharp
public static void Unregister(RegistryType registryType, string identifier)
```

지정된 식별자의 항목을 제거합니다.

---

### `Get<T>`

```csharp
public static T Get<T>(RegistryType registryType, string identifier)
```

등록된 값을 `T`로 캐스팅해 반환합니다.  
없거나 캐스팅 실패 시 `default`를 반환합니다.

**ScenarioGraph 특수 동작:** `RegistryType.ScenarioGraph`에 `TextAsset`이 등록되어 있으면, 최초 `Get<ScenarioGraph>()` 호출 시 JSON을 파싱하여 파싱 결과 `ScenarioGraph`로 교체합니다 (이후 호출은 캐시된 결과 반환).

**IconSprite 특수 동작:** `RegistryType.IconSprite`에 리소스 경로 문자열이 등록되어 있으면, 최초 `Get<Sprite>()` 호출 시 `Resources.Load<Sprite>(path)`를 실행하고 결과로 교체합니다.

**Entity 특수 동작:** `RegistryType.Entity`의 값이 `EntityDescriptor`이면 다음 해석을 지원합니다.

- `Get<EntityDescriptor>()` → 설명자 자체
- `Get<GameObject>()` → `descriptor.GameObject`
- `Get<Transform>()` → `descriptor.GameObject.transform`
- `Get<Component>()` → `descriptor.GameObject.GetComponent<T>()`

---

### `TryGet<T>`

```csharp
public static bool TryGet<T>(RegistryType registryType, string identifier, out T value)
```

조회 결과를 `out` 파라미터로 반환합니다. 없거나 캐스팅 실패 시 `false`를 반환합니다.

```csharp
if (Registry.TryGet<QuestManager>(RegistryType.Service, Registry.TypeKey<QuestManager>(), out var mgr))
{
    mgr.AddOrUpdateQuest(...);
}
```

---

### `Contains`

```csharp
public static bool Contains(RegistryType registryType, string identifier)
```

해당 식별자가 지정된 레지스트리에 존재하는지 확인합니다.

---

### `GetAll<T>`

```csharp
public static IReadOnlyDictionary<string, T> GetAll<T>(RegistryType registryType)
```

특정 레지스트리에서 `T`로 캐스팅 가능한 모든 항목을 반환합니다. 새 `Dictionary`가 생성되므로 호출 빈도에 주의하세요.

---

### `PreloadScenarioGraph`

```csharp
public static bool PreloadScenarioGraph(string identifier, bool validateWithSchema = true)
```

지정된 `ScenarioGraph`를 미리 JSON 파싱합니다. 파싱 성공 여부를 반환합니다.  
씬 로드 직후 지연 없이 시나리오를 시작해야 하는 경우에 유용합니다.

---

### `TypeKey<T>` / `TypeKey(Type)`

```csharp
public static string TypeKey<T>()
public static string TypeKey(Type type)
```

타입의 `FullName`을 레지스트리 키로 반환합니다. 타입별 단일 인스턴스를 등록/조회할 때 사용합니다.

```csharp
// 등록
Registry.Register(RegistryType.Service, Registry.TypeKey<QuestManager>(), this);

// 조회
var mgr = Registry.Get<QuestManager>(RegistryType.Service, Registry.TypeKey<QuestManager>());
```

---

### 엔티티 전용 메서드

```csharp
public static void RegisterEntity(EntityDescriptor descriptor)
public static void RegisterEntity(string identifier, EntityType entityType, GameObject gameObject, ...)
public static bool TryGetEntity(string identifier, out EntityDescriptor descriptor)
public static void UnregisterEntity(string identifier)
public static IReadOnlyDictionary<string, EntityDescriptor> GetAllEntities()
public static IReadOnlyDictionary<string, EntityDescriptor> GetAllEntities(EntityType entityType)
public static T GetFirstEntityComponent<T>(EntityType entityType, Predicate<T> predicate = null) where T : Component
public static bool TryGetEntityByClientId(int clientId, out EntityDescriptor descriptor)
public static bool TryGetEntityByOwnerUserIdentifier(string ownerUserIdentifier, out EntityDescriptor descriptor)
public static void UpdateEntityDisplayName(string identifier, string displayName)
```

```csharp
// 엔티티 등록
Registry.RegisterEntity("player:abcd", EntityType.Player, player.gameObject, displayName: "Alice");

// 엔티티 설명자 조회
var entity = Registry.Get<EntityDescriptor>(RegistryType.Entity, "player:abcd");

// 특정 타입 컴포넌트 직접 조회
var npc = Registry.Get<Npc>(RegistryType.Entity, "npc:triage-desk-01");

// owner 플레이어 찾기
var localPlayer = Registry.GetFirstEntityComponent<PlayerController>(
    EntityType.Player,
    each => each != null && each.IsOwner);
```

---

## 3. 내장 등록 항목

`Registry`는 앱 시작 시(`RuntimeInitializeOnLoadMethod`) 아이콘 스프라이트를 자동으로 등록합니다.

| 식별자 | 리소스 경로 |
|---|---|
| `"message-circle"` | `Textures/Icons/message-circle` |

---

## 4. 등록 방법 가이드

레지스트리에 항목을 등록하는 방법은 크게 두 가지입니다. **등록 대상의 성격에 따라 방법을 선택하세요.**

### 4.1 RegistryPreloaderController를 통한 등록 (기본)

> **에셋/프리팹 템플릿처럼 씬 하이어라키에 상주하지 않는 항목은 이 방법을 사용하세요.**

`RegistryPreloaderController`는 ScriptableObject에 등록할 항목을 모아두고, 씬 시작 시 `Awake()`에서 일괄 등록합니다. 특정 씬에 종속되지 않으며, 에디터에서 인스펙터만으로 관리할 수 있습니다.

**적합한 등록 대상:**
- 아이템 프리팹 템플릿 (`RegistryType.Item`)
- 시나리오 그래프 JSON (`RegistryType.ScenarioGraph`)
- 아이콘 스프라이트 (`RegistryType.IconSprite`)
- NPC 프리팹 (`RegistryType.Npc`)
- 웨이포인트 위치 (`RegistryType.Waypoint`)
- 미리 배치된 UI 컨트롤러 (`RegistryType.UI`)
- 서비스성 런타임 오브젝트 (`RegistryType.Service`)

설정 방법은 [섹션 5. RegistryPreloaderController](#5-registrypreloadercontroller)를 참조하세요.

---

### 4.2 자기 등록 (런타임 예외)

> **런타임에 동적으로 생성·소멸되는 오브젝트에 한해 사용하세요.**

씬에 배치되거나 네트워크로 스폰되는 오브젝트는 자신의 `Awake()` / `OnStartClient()` 등 Lifecycle에서 직접 `Registry.Register`를 호출하고, `OnDestroy()`에서 `Registry.Unregister`를 호출합니다.

**적합한 등록 대상:**
- 네트워크 스폰 플레이어 (`PlayerController` — 서버가 엔티티 ID 발급)
- 씬에 배치된 NPC/웨이포인트/시나리오 인터랙터블/트리거 존
- 서버가 발급한 월드 아이템 인스턴스 (`ItemObject`)

```csharp
Registry.RegisterEntity(
    entityIdentifier,
    EntityType.Player,
    gameObject,
    displayName: userDisplayName,
    ownerUserIdentifier: userIdentifier,
    clientId: Owner.ClientId,
    isNetworked: true);
```

> **주의:** 자기 등록을 사용할 때는 반드시 `OnDestroy()` 또는 해당 Lifecycle 종료 시점에 `Unregister`를 쌍으로 호출하세요.

---

## 4.3 조회 패턴

### 일반 조회

```csharp
var questManager = Registry.Get<QuestManager>(RegistryType.Service, Registry.TypeKey<QuestManager>());
```

### 안전한 조회

```csharp
if (Registry.TryGet<QuestManager>(RegistryType.Service, Registry.TypeKey<QuestManager>(), out var mgr))
{
    mgr.AddOrUpdateQuest(...);
}
```

### UI 컨트롤러 조회 (ScenarioController.cs)

```csharp
var dialogueUI = Registry.Get<DialoguePanelUIController>(
    RegistryType.UI,
    Registry.TypeKey<DialoguePanelUIController>()
);
```

---

## 5. RegistryPreloaderController

씬에 배치되는 `MonoBehaviour` 컴포넌트입니다. 게임 오브젝트의 `Awake()`에서 각 ScriptableObject를 순회하며 지정된 항목을 레지스트리에 일괄 등록합니다.

### 5.1 존재 이유

개별 컴포넌트(`Item`, `WaypointAnchor` 등)는 각자 Unity Lifecycle(`Awake`) 안에서 스스로를 레지스트리에 등록합니다. 그러나 프리팹 템플릿처럼 씬 내 하이어라키에 상주하지 않는 항목은 Lifecycle이 실행되지 않기 때문에 자동으로 등록되지 않습니다.  
`RegistryPreloaderController`는 ScriptableObject에 에셋 레퍼런스를 보관하여, 씬에 종속되지 않는 방식으로 이러한 항목들을 게임 시작 시 등록합니다.

### 5.2 설정 방법

1. 씬의 하이어라키에 `RegistryPreloaderController`가 부착된 게임 오브젝트를 배치합니다.
2. 필요한 RegistryType에 해당하는 ScriptableObject를 생성합니다 (`Assets/Create/MultiplayerInfrastructure/` 아래).
3. 각 SO에 등록하려는 항목을 채운 뒤 컨트롤러의 해당 필드에 연결합니다.
4. 등록이 필요 없는 RegistryType의 필드는 비워 두어도 됩니다 (null 안전).

### 5.3 ScriptableObject 및 Requirement 구조체

각 RegistryType마다 전용 SO와 requirement 구조체가 쌍으로 존재합니다.

| ScriptableObject | Requirement struct | 등록 대상 |
|---|---|---|
| `RegistryPreloadItemSO` | `ItemRegistryRequirement` | `RegistryType.Item`, `RegistryType.IconSprite` |
| `RegistryPreloadScenarioGraphSO` | `ScenarioGraphRegistryRequirement` | `RegistryType.ScenarioGraph` |
| `RegistryPreloadIconSpriteSO` | `IconSpriteRegistryRequirement` | `RegistryType.IconSprite` |
| `RegistryPreloadNpcSO` | `NPCRegistryRequirements` | `RegistryType.Npc` |
| `RegistryPreloadWaypointSO` | `WaypointRequirements` | `RegistryType.Waypoint` |
| `RegistryPreloadEntitySO` | `EntityRegistryRequirement` | `RegistryType.Entity` |
| `RegistryPreloadInteractableEntitySO` | `WaypointRequirements` | `RegistryType.InteractableEntity` |
| `RegistryPreloadUIControllerSO` | `UIControllerRegistryRequirement` | `RegistryType.UI` |

#### Requirement 구조체 상세

```csharp
// 아이템 — itemDataModel.identifier를 키로 사용
struct ItemRegistryRequirement
{
    ItemBaseModelSO itemDataModel;   // 필수: identifier 제공
    GameObject      itemPrefab;      // Item 컴포넌트가 있으면 RegistryType.Item 에 등록
    Sprite          itemSprite;      // 있으면 RegistryType.IconSprite 에 함께 등록
}

// 시나리오 그래프 — JSON 파싱은 최초 Get<ScenarioGraph>() 시점에 지연 실행
struct ScenarioGraphRegistryRequirement
{
    string    identifier;
    TextAsset scenarioGraphAsset;
}

// 아이콘 스프라이트 (직접 지정, Resources 경로 우회)
struct IconSpriteRegistryRequirement
{
    string identifier;
    Sprite sprite;
}

// NPC
struct NPCRegistryRequirements
{
    string     Identifier;
    GameObject GameObjectRef;
}

// 웨이포인트 / InteractableEntity — 모두 Vector3 위치 저장
struct WaypointRequirements
{
    string  Identifier;
    Vector3 Position;
}

// 일반 엔티티 (런타임 참조용)
struct EntityRegistryRequirement
{
    string             identifier;
    UnityEngine.Object objectRef;
}

// UI 컨트롤러 — identifier 공백 시 TypeKey(type.FullName) 자동 사용
struct UIControllerRegistryRequirement
{
    string      identifier;   // 비워두면 컨트롤러 타입 FullName 으로 자동 설정
    MonoBehaviour controllerRef;
}
```

### 5.4 타입별 특이 동작

| RegistryType | 특이 동작 |
|---|---|
| `Item` | `itemPrefab`에서 `Item` 컴포넌트를 `TryGetComponent`로 추출해 등록. 프리팹에 컴포넌트가 없으면 건너뜀. `itemSprite`가 있으면 `IconSprite`에도 동시 등록. |
| `ScenarioGraph` | `TextAsset`을 그대로 등록. 실제 파싱(`ScenarioGraph` 객체 생성)은 최초 `Registry.Get<ScenarioGraph>()` 또는 `Registry.PreloadScenarioGraph()` 호출 시 수행됨. |
| `UI` | `identifier`가 비어있으면 `Registry.TypeKey(controllerRef.GetType())`를 키로 사용. 일반적인 조회 패턴(`Registry.Get<T>(RegistryType.UI, Registry.TypeKey<T>())`)과 일치함. |
| `InteractableEntity` | `WaypointRequirements` 구조체를 재사용하며 `Vector3` 위치를 등록. |

### 5.5 등록 순서

`Awake()` 내에서 다음 순서로 실행됩니다:

```
Item → ScenarioGraph → IconSprite → Npc → Waypoint → Entity → InteractableEntity → UIController
```

동일 식별자를 여러 SO에서 등록하면 나중에 실행된 항목이 덮어씁니다(`Registry.Register`는 덮어쓰기 동작).

---

## 6. 주의사항

- `Registry`는 **씬 간 초기화되지 않습니다.** 씬이 언로드되어도 등록 항목이 남아 있을 수 있으므로, `OnDestroy`에서 `Unregister`를 호출하세요.
- `GetAll<T>()` 는 새 딕셔너리를 매번 생성합니다. 반복 호출 시 GC 부하에 주의하세요.
- `RegistryType.ScenarioGraph`에 등록한 `TextAsset`은 최초 `Get<ScenarioGraph>()` 후 `TextAsset` 참조가 제거됩니다 (파싱 결과로 교체됨).
