# API 레퍼런스: `MultiplayerInfrastructure.InteractableEntity`

> **네임스페이스:** `MultiplayerInfrastructure.InteractableEntity`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/`

---

## 0. 문서 목적

이 모듈은 플레이어가 월드 오브젝트와 상호작용하는 구조를 정의합니다.  
새 상호작용을 추가할 때는 `IInteract`를 구현하여 `Interactable` 컴포넌트에 연결합니다.

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

### Inspector 직렬화 필드

| 필드 | 설명 |
|---|---|
| `handlerSources` | `MonoBehaviour` 목록 (IInteractable 구현체 연결) |

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

**방법 2: `IInteract` 직접 구현 + `Interactable` 결합**

여러 액션을 하나의 오브젝트에서 제공할 때 사용합니다.

```csharp
public class MedCart : MonoBehaviour, IInteractable
{
    [SerializeField] private TakeSuppliesInteract _takeSupplies;
    [SerializeField] private LockCartInteract _lockCart;

    public IInteract[] Interacts => new IInteract[] { _takeSupplies, _lockCart };
}
```

---

## 관련 문서

- [api-references/architecture/multiplayer-infrastructure-overview.md](architecture/multiplayer-infrastructure-overview.md) — 인터랙터블 시스템 개요
- [requirements/gameplay/interaction/interaction-feature-spec.md](../requirements/gameplay/interaction/interaction-feature-spec.md) — 상세 기능 요구사항
- [api-references/MultiplayerInfrastructure.Player.PlayerController.md](MultiplayerInfrastructure.Player.PlayerController.md) — PlayerController API
