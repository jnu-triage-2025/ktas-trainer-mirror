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
    Waypoint,           // Vector3 좋아요 웨이포인트 위치
    Entity,             // PlayerController, QuestManager 등 런타임 엔티티
    InteractableEntity, // IInteractable 구현 컴포넌트
    UI,                 // UIControllerABC 하위 컨트롤러
}
```

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

---

### `TryGet<T>`

```csharp
public static bool TryGet<T>(RegistryType registryType, string identifier, out T value)
```

조회 결과를 `out` 파라미터로 반환합니다. 없거나 캐스팅 실패 시 `false`를 반환합니다.

```csharp
if (Registry.TryGet<QuestManager>(RegistryType.Entity, Registry.TypeKey<QuestManager>(), out var mgr))
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
Registry.Register(RegistryType.Entity, Registry.TypeKey<QuestManager>(), this);

// 조회
var mgr = Registry.Get<QuestManager>(RegistryType.Entity, Registry.TypeKey<QuestManager>());
```

---

## 3. 내장 등록 항목

`Registry`는 앱 시작 시(`RuntimeInitializeOnLoadMethod`) 아이콘 스프라이트를 자동으로 등록합니다.

| 식별자 | 리소스 경로 |
|---|---|
| `"message-circle"` | `Textures/Icons/message-circle` |

---

## 4. 주요 사용 패턴

### 플레이어 등록 (PlayerController.Network.cs)

```csharp
// OnStartClient에서 오너 플레이어가 자신을 등록
Registry.Register(RegistryType.Entity, Registry.TypeKey<PlayerController>(), this);
```

### UI 컨트롤러 조회 (ScenarioController.cs)

```csharp
var dialogueUI = Registry.Get<DialoguePanelUIController>(
    RegistryType.UI,
    Registry.TypeKey<DialoguePanelUIController>()
);
```

### 아이템 등록 (Item.Lifecycle.cs)

```csharp
// Awake에서 아이템 자신을 등록
Registry.Register(RegistryType.Item, itemData.Identifier, this);
```

---

## 5. 주의사항

- `Registry`는 **씬 간 초기화되지 않습니다.** 씬이 언로드되어도 등록 항목이 남아 있을 수 있으므로, `OnDestroy`에서 `Unregister`를 호출하세요.
- `GetAll<T>()` 는 새 딕셔너리를 매번 생성합니다. 반복 호출 시 GC 부하에 주의하세요.
- `RegistryType.ScenarioGraph`에 등록한 `TextAsset`은 최초 `Get<ScenarioGraph>()` 후 `TextAsset` 참조가 제거됩니다 (파싱 결과로 교체됨).
