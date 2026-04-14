# 2026-03-03 변경 노트: 아이템 픽업 구현 + 예제 + 버그 수정

## 요약

이번 변경은 아이템 월드 픽업 시스템을 완성하고, 예제 아이템을 추가하며, 관련 버그를 수정하는 작업입니다.
핵심은 다음 4가지입니다.

1. `LootableItemInteractHandler` 완전 구현 → 아이템 월드 픽업 동작
2. `ItemObject.Spawn()`에 `LootableItemInteractHandler` 자동 부착
3. `StoneBlock`·`WoodBlock` 예제 아이템 + `ItemTemplate.cs` 추가
4. `TTRegistryMonoBehaviourSupport` 정식화 + 런타임 버그 수정 3종

---

## 1) `LootableItemInteractHandler` 완전 구현

**파일:** `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/Definitions/LootableItemInteractHandler.cs`

기존에 빈 스텁이었던 클래스를 `IInteractable` + `IInteract` 완전 구현체로 작성했습니다.

```csharp
public class LootableItemInteractHandler : MonoBehaviour, IInteractable, IInteract
{
    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText  => $"{GetComponent<ItemObject>()?.Item?.CurrentDisplayName} 획득";
    public Sprite DisplayIcon  => GetComponent<ItemObject>()?.Item?.CurrentItemIconTexture;
    public bool   AllowDisplayIconFallback => true;
    public Color  DisplayColor => Color.white;

    public void Interact(Transform interactor)
    {
        // interactor에서 PlayerController 탐색
        // TryAddItemToInventory → OnGet → Destroy(gameObject)
    }
}
```

`Interact()` 흐름(작성 당시 기준):
1. `interactor` 계층에서 `PlayerController` 탐색
2. 로컬 인벤토리 추가 시도
3. 성공 시 `item.OnGet(player)` 호출
4. 월드 오브젝트 제거

> 참고: 현재 구현은 여기서 더 확장되어, entity ID 기반 서버 승인 픽업과 실패 시 롤백 복구를 사용합니다. 최신 동작은 [item.md](../item.md), [item-authoring.md](../working-guide/item-authoring.md), [api-references/MultiplayerInfrastructure.InteractableEntity.md](../api-references/MultiplayerInfrastructure.InteractableEntity.md)를 따르십시오.

---

## 2) `ItemObject.Spawn()` 변경

**파일:** `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/ItemObject.cs`

`Spawn()` 팩토리 메서드에 두 가지를 추가했습니다.

### LootableItemInteractHandler 자동 부착

```csharp
var comp = go.AddComponent<ItemObject>();
comp.Initialize(item);
go.AddComponent<LootableItemInteractHandler>(); // ← 추가
```

별도 설정 없이 `ItemObject.Spawn()`만 호출하면 자동으로 픽업 가능한 상태가 됩니다.

### Layer 범위 에러 수정

`LayerMask.NameToLayer()`는 레이어가 등록되지 않으면 `-1`을 반환합니다.  
이를 `go.layer`에 대입하면 `ArgumentOutOfRangeException`이 발생하므로 폴백으로 `0`을 사용합니다.

```csharp
int pickupLayer = LayerMask.NameToLayer("PickupItem");
go.layer = pickupLayer >= 0 ? pickupLayer : 0;   // ← 수정 전: go.layer = pickupLayer
```

---

## 3) 예제 아이템 + 템플릿 추가

### StoneBlock

**파일:** `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/Examples/StoneBlock.cs`  
(구 `StoneItem.cs`에서 파일명·클래스명 변경)

- identifier: `"stone_block"`
- 내구도 없음, 스택 가능

### WoodBlock

**파일:** `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/Examples/WoodBlock.cs`  
(신규)

- identifier: `"wood_block"`
- `MaxDurability = 30`, `DeltaDurabilityOnAttack = -2`

### ItemTemplate.cs

**파일:** `Assets/Modules/MultiplayerInfrastructure/Examples/ItemTemplate.cs`

새 아이템 구현 시작점 템플릿입니다. `public const` Definitions 전체 + 핸들러 스텁 + Serialization 섹션을 포함합니다.

---

## 4) `TTRegistryMonoBehaviourSupport` 정식화

**파일:** `Assets/Modules/TriageTrainer/MultiplayerInfrastructure/TTRegistryMonoBehaviourSupport.cs`

기존 `TTRegistryPreloader`를 `TTRegistryMonoBehaviourSupport`로 대체합니다.

변경 내용:
- `StoneBlock`, `WoodBlock` 등록 추가
- `using MIExamples = MultiplayerInfrastructure.ItemSystem.Examples` 별칭 추가 (CS0234 해소)
- 이중 `using MultiplayerInfrastructure.Definitions` 제거
- `ValidateItemResources()` — 아이콘·모델 장원 유효성 검증 메서드 추가

> **전체 문서에서 `TTRegistryPreloader` 레퍼런스는 `TTRegistryMonoBehaviourSupport`로 교체되었습니다.**

---

## 5) 버그 수정 요약

| 증상 | 원인 | 해결 |
|---|---|---|
| `ArgumentOutOfRangeException: Layer XXX is out of range` | `LayerMask.NameToLayer` 미등록 레이어 → `-1` 반환 | `pickupLayer >= 0 ? pickupLayer : 0` 폴백 |
| 3D 모델이 씬에서 보이지 않음 | 모델 프리팹에 `FishNet.Object.NetworkObject` 부착 → FishNet이 미등록 오브젝트 비활성화 | **프리팹에서 FishNet 컴포넌트 직접 제거** (코드 해결 불가) |
| F 키를 눌러도 아이템 픽업 안됨 | `ItemObject`에 `IInteractable` 없어 `NearbyInteractablesDetector` 미감지 | `LootableItemInteractHandler` 자동 부착 + 구현 |
| CS0234: `ItemSystem` 네임스페이스 없음 | `TriageTrainer.MultiplayerInfrastructure` 내에서 동명 네임스페이스 충돌 | `using MIExamples = MultiplayerInfrastructure.ItemSystem.Examples` 별칭 사용 |

---

## 영향받는 파일

| 파일 | 변경 종류 |
|---|---|
| `ItemObject.cs` | 수정 (layer 폴백, LootableItemInteractHandler 부착, 디버그 코루틴 제거) |
| `LootableItemInteractHandler.cs` | 수정 (빈 스텁 → 완전 구현) |
| `TTRegistryMonoBehaviourSupport.cs` | 수정 (별칭 추가, StoneBlock·WoodBlock 등록, ValidateItemResources 추가) |
| `StoneBlock.cs` | 수정 (StoneItem에서 파일명·클래스명 변경) |
| `WoodBlock.cs` | 신규 |
| `ItemTemplate.cs` | 신규 |

---

## 관련 문서

- [item.md](../item.md)
- [item-authoring.md](../item-authoring.md)
- [api-references/MultiplayerInfrastructure.Item.Item.md](../api-references/MultiplayerInfrastructure.Item.Item.md)
- [api-references/MultiplayerInfrastructure.InteractableEntity.md](../api-references/MultiplayerInfrastructure.InteractableEntity.md)
