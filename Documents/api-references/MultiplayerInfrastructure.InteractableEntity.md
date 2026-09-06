# API 레퍼런스: `MultiplayerInfrastructure.InteractableEntity`

> **네임스페이스:** `MultiplayerInfrastructure.InteractableEntity`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/`

---

## 0. 문서 목적

이 모듈은 플레이어가 월드 오브젝트와 상호작용하는 구조를 정의합니다.  
새 상호작용은 엔티티 코드가 `IInteractionDefinitionSource`로 선언하거나 시나리오 JSON의 최상위 `interactions`
구역에 정의하고, 인터렉션 레지스트리(`InteractionRegistry`, 8절)가 노출 여부를 판정합니다. 프리팹 직렬화 필드는
정의 출처가 아닙니다.

---

## 1. `IInteractable` 인터페이스

```csharp
public interface IInteractable
{
    IInteract[] Interacts { get; }
}
```

상호작용 가능한 오브젝트가 구현해야 하는 인터페이스입니다.  
`Interacts` 배열에 있는 각 `IInteract`가 플레이어 HUD에 독립적인 액션으로 표시됩니다.

---

## 2. `IInteract` 인터페이스

```csharp
public interface IInteract
{
    // HUD에 표시되는 짧은 텍스트
    string DisplayText { get; }

    // HUD에 표시되는 아이콘 (null 허용)
    Sprite DisplayIcon { get; }

    // DisplayIcon이 null일 때 기본 fallback 아이콘 표시 여부
    bool AllowDisplayIconFallback { get; }

    // HUD 표시 색상 (기본: Color.white)
    Color DisplayColor { get; }

    // 플레이어가 상호작용 키를 눌렀을 때 실행
    void Interact(Transform interactor);
}
```

---

## 3. `Interactable` (abstract MonoBehaviour)

`IInteractable`과 `IInteract`를 모두 구현하는 가장 기본적인 기반 클래스입니다.

```csharp
public abstract class Interactable : MonoBehaviour, IInteractable, IInteract
```

### Inspector 직렬화 필드

| 필드 | 기본값 | 설명 |
|---|---|---|
| `_displayText` | `""` | HUD에 표시되는 텍스트 |
| `_displayIcon` | `null` | HUD 아이콘 |
| `_displayColor` | `Color.white` | HUD 표시 색상 |

### 재정의 가능 프로퍼티/메서드

```csharp
public virtual string DisplayText { get; }         // 기본: _displayText
public virtual Sprite DisplayIcon { get; }         // 기본: _displayIcon
public virtual bool AllowDisplayIconFallback => true;
public virtual Color DisplayColor { get; }         // 기본: _displayColor
public virtual IInteract[] Interacts => new IInteract[] { this };

// 반드시 구현해야 함
public abstract void Interact(Transform interactor);
```

### 사용 예시

```csharp
public class ExamRoomDoor : Interactable
{
    public override string DisplayText => "문 열기";

    public override void Interact(Transform interactor)
    {
        _animator.SetTrigger("Open");
    }
}
```

---

## 4. `InteractableEntityResolver`

`PlayerController`에 붙어 실제 `Interact()` 호출을 중계합니다.

```csharp
[DisallowMultipleComponent]
public class InteractableEntityResolver : MonoBehaviour
```

직렬화 필드는 없습니다. 실행할 핸들러는 `NearbyInteractablesDetector`가 찾은 `IInteractable`의 `Interacts`와
인터렉션 레지스트리가 정합니다.

### 공개 메서드

```csharp
// IInteract를 직접 실행
public void Resolve(IInteract interact, Transform interactor)

// IInteractable에서 index 번째 IInteract를 실행
public void Resolve(IInteractable interactable, Transform interactor, int interactIndex = 0)
```

---

## 5. `NearbyInteractablesDetector`

카메라(`MainCamera`)에 붙어 주기적으로 반경 내 `IInteractable`을 감지합니다.

```csharp
public class NearbyInteractablesDetector : NetworkBehaviour
```

### Inspector 직렬화 필드

| 필드 | 기본값 | 설명 |
|---|---|---|
| `detectionRedius` | `1.3f` | 감지 반경(m), 최솟값 0.5 |
| `interactionLayerMask` | 모든 레이어 | 감지 대상 레이어 마스크 |
| `queryInterval` | `0.05f` | 감지 주기(초), 최솟값 0.02 |

### 공개 API

```csharp
// 감지 기준 위치 설정 (PlayerController.Interactables.cs에서 호출)
public void RegisterDetectBased(Transform transform)

// 현재 감지된 인터랙터블 목록 (읽기 전용)
public IReadOnlyList<IInteractable> Nearby { get; }

// 감지된 인터랙터블이 있는지 여부
public bool InteractableNearbyExists { get; }

// 목록이 변경될 때 발생 (PlayerController가 구독)
public event Action<IReadOnlyList<IInteractable>> NearbyUpdated;
```

### 동작 방식

매 프레임 `queryInterval` 간격으로 `Physics.OverlapSphereNonAlloc`을 실행합니다.  
이전 프레임과 결과가 다를 때만 `NearbyUpdated` 이벤트가 발생하여 HUD 업데이트를 최소화합니다.

---

## 6. `LootableItemInteractHandler`

`IInteractable` + `IInteract`를 모두 구현하는 MonoBehaviour입니다.  
`ItemObject.Spawn()` 호출 시 자동으로 GameObject에 추가됩니다. 직접 인스턴스화할 필요는 없습니다.

```csharp
public class LootableItemInteractHandler : MonoBehaviour, IInteractable, IInteract
```

### 동작

| 프로퍼티/메서드 | 반환값/동작 |
|---|---|
| `Interacts` | `new IInteract[] { this }` |
| `DisplayText` | `"{CurrentDisplayName} 획득"` |
| `DisplayIcon` | `Item.CurrentItemIconTexture` |
| `AllowDisplayIconFallback` | `true` |
| `DisplayColor` | `Color.white` |
| `Interact(Transform interactor)` | 아래 참조 |

### `Interact(interactor)` 흐름

1. `interactor` 계층에서 `PlayerController` 탐색
2. `ItemObject.Identifier`가 있으면 `PlayerController.TryPickupWorldItem(entityIdentifier)` 호출
3. identifier가 없는 구형/로컬 오브젝트일 때만 `PlayerController.TryPickupWorldItem(itemObject)` 폴백 호출
4. 서버가 거리/중복/소유권을 검증하고 승인하면 전 관전자에게 월드 아이템 제거 전파
5. 대상 클라이언트만 인벤토리 삽입 후 `item.OnGet(player)` 실행
6. 대상 클라이언트가 실패를 보고하면 서버가 같은 entity ID로 월드 아이템을 롤백 복구

### HUD 감지 조건

`NearbyInteractablesDetector`는 `PickupItem` 레이어 콜라이더에서 `IInteractable` 컴포넌트를 탐색합니다.  
`ItemObject.Spawn()`이 GameObject 레이어를 `PickupItem`(없으면 `Default`)으로 설정하고 `LootableItemInteractHandler`를 부착하므로, 별도 설정 없이 자동 감지됩니다. entity ID가 설정된 월드 아이템은 동시에 `RegistryType.Entity`의 `EntityType.ItemObject`로도 등록됩니다.

---

## 7. 새 인터랙터블 추가 가이드

**방법 1: `Interactable` abstract class 상속**

```csharp
// TriageTrainer 측
public class AEDDevice : Interactable
{
    public override string DisplayText => "AED 사용";

    public override void Interact(Transform interactor)
    {
        // 사용 로직
        GetComponent<AEDController>().StartDefib();
    }
}
```

GameObject에 `AEDDevice` 컴포넌트 추가 + Collider 추가 + 적절한 레이어(interactionLayerMask) 설정.

**방법 2: `IInteract` 직접 구현 + 레지스트리 선언**

여러 액션을 하나의 오브젝트에서 제공할 때 사용합니다. 각 `IInteract`는 `IQuestPresentationTarget`으로
주소(`엔티티/인터렉션`)를 드러내고, 엔티티는 `IInteractionDefinitionSource`로 코드 리터럴 정의를 선언합니다.

```csharp
public class MedCart : MonoBehaviour, IInteractable, IInteractionDefinitionSource
{
    private readonly List<IInteract> _interacts = new();

    public IInteract[] Interacts => _interacts.ToArray();

    public IEnumerable<InteractionDeclaration> DeclareInteractions()
    {
        yield return new InteractionDeclaration(
            InteractionDefinition.Code(EntityIdentifier, "take_supplies", "물품 꺼내기", initialVisible: true),
            _takeSupplies);
        yield return new InteractionDeclaration(
            InteractionDefinition.Code(EntityIdentifier, "lock_cart", "카트 잠금"), // 시나리오 데이터가 연다
            _lockCart);
    }
}
```

엔티티를 `Registry.RegisterEntity`로 등록한 직후 `InteractionRegistry.DeclareCode(EntityIdentifier, this)`를
호출하고, 등록 해제 시 `RemoveCodeDefinitions`를 호출합니다. 레지스트리에 없는 핸들러는 에디터 런타임에서 경고를 남기며
그대로 노출됩니다(`IInteractionRegistryExempt`로 의도적 제외를 표시할 수 있습니다).

---

## 8. 인터렉션 레지스트리 `InteractionRegistry`

경로: `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/Registry/`

정의(무엇이 있는가)와 가시성(누구에게 보이는가)을 한 곳에서 관리하는 정적 레지스트리입니다. 초기화 시 비어 있고,
엔티티 초기화 사이클(코드 리터럴)과 시나리오 초기화 사이클(시나리오 JSON `interactions`, 전역 카탈로그
`Resources/Interactions/*.json`)에서만 채워집니다. 그 밖의 시점에 등록하면 에디터 런타임에서 경고를 남깁니다.

| 구성 | 역할 |
|---|---|
| `InteractionDefinition` | 정의 모델. 코드 리터럴과 데이터 오버레이를 `MergeOverlay`로 합친다 |
| `InteractionAddress` | `엔티티식별자/인터렉션식별자` 주소 |
| `InteractionRegistryEntry` | 주소별 항목. `CodeDefinition`, `DataDefinition`, 합쳐진 `Definition`, `Handler` |
| `IInteractionDefinitionSource` | 엔티티 코드가 코드 리터럴을 선언하는 인터페이스 |
| `IInteractionHandlerFactory` | 데이터 전용 `Custom` 정의의 핸들러를 `handlerKey`로 만들어 주는 인터페이스 |
| `IConditionStateProvider` | 조건 절 `PlayerState`/`EntityState`가 읽는 상태 키를 노출하는 인터페이스 |
| `InteractionVisibilityState` | 오버라이드 저장소(전역 층 + 플레이어별 층). 스냅샷 직렬화 제공 |
| `RegistryActionInteract` 등 | `Action`/`Signal`/`StartScenario`/`ItemSubmission` 종류의 범용 핸들러 |

주요 API:

```csharp
// 정의
IDisposable BeginEntityInitCycle(string entityIdentifier);
IDisposable BeginScenarioInitCycle(string scenarioIdentifier);
void DeclareCode(string entityIdentifier, IInteractionDefinitionSource source);
void RemoveCodeDefinitions(string entityIdentifier);
void ApplyScenarioDefinitions(string scenarioIdentifier, IReadOnlyList<InteractionDefinition> definitions);
void ClearScenarioDefinitions(string scenarioIdentifier);

// 조회
bool TryGet(InteractionAddress address, out InteractionRegistryEntry entry);
bool TryGetByHandler(IInteract handler, out InteractionRegistryEntry entry);
IEnumerable<InteractionRegistryEntry> EntriesForEntity(string entityIdentifier);

// 가시성: 오버라이드(플레이어별 → 전역) → 조건 절 → initial
bool IsVisible(InteractionRegistryEntry entry, PlayerController viewer, out string reason);
void SetVisibilityOverride(InteractionAddress address, InteractionVisibilityOverride value,
    InteractionVisibilityScope scope, IReadOnlyList<string> playerIdentifiers = null);
void ResetOverridesForEntity(string entityIdentifier);

// 수행 통지(afterInteract 적용, Interacted 이벤트)
void NotifyInteracted(IInteract handler, PlayerController player);
void AssignEntityTag(string entityIdentifier, string tag);
```

멀티플레이 규약:

- 정의는 각 피어가 같은 코드·데이터에서 로컬로 만들므로 복제하지 않습니다.
- 오버라이드와 엔티티 태그는 서버 권위입니다. 클라이언트 호출은 `ScenarioNetworkRelay`를 거쳐 서버가 기록하고
  `ObserversRpc`로 미러링하며, 늦게 접속한 피어는 신호 스냅샷 요청 때 함께 복원됩니다.
- 조건 절은 각 피어가 자기 플레이어 기준으로 판정합니다. 입력(퀘스트, 태그, 신호, 엔티티 상태)은 이미 복제된 값입니다.

디버그 인스펙터: `Tools > Multiplayer Infrastructure > Interaction Registry`. 항목별 정의·판정 사유를 보여 주고,
Unity 에디터에서만 오버라이드를 직접 바꿀 수 있습니다.

---

## 관련 문서

- [api-references/architecture/multiplayer-infrastructure-overview.md](architecture/multiplayer-infrastructure-overview.md) — 인터랙터블 시스템 개요
- [requirements/gameplay/interaction/interaction-feature-spec.md](../requirements/gameplay/interaction/interaction-feature-spec.md) — 상세 기능 요구사항
- [api-references/MultiplayerInfrastructure.Player.PlayerController.md](MultiplayerInfrastructure.Player.PlayerController.md) — PlayerController API
- [changes/2026-09-06-interaction-registry-visibility.md](../changes/2026-09-06-interaction-registry-visibility.md) — 인터렉션 레지스트리 도입 변경 노트
- [api-references/MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md](MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md) — `interactions` 구역과 `InteractionVisibility` 노드
