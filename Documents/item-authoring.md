# 아이템 구현 가이드

새 아이템을 구현하려면 다음 네 가지 구성 요소를 준비해야 합니다:

| 구성 요소 | 역할 |
|---|---|
| `ItemBaseModelSO` | 아이템의 기본 메타데이터 (식별자, 이름, 최대 스택 등) |
| 아이콘 스프라이트 | 인벤토리/핫바 UI에 표시할 이미지 |
| 프리팹 (`Item` 컴포넌트 포함) | 월드에 배치되는 3D 오브젝트 |
| `ItemData` 서브클래스 | 아이템 고유 동작 로직 (획득/사용/공격) |

---

## 1단계: ItemBaseModelSO 생성

Project 패널에서 **+** → `MultiplayerInfrastructure/Item Base Model` 을 선택합니다.

에디터에서 다음 필드를 채웁니다:

| 필드 | 설명 |
|---|---|
| `identifier` | 시스템 전체에서 고유한 식별자. 소문자 + 언더스코어 권장 (예: `scalpel`, `saline_bag`) |
| `displayName` | UI에 표시될 이름 |
| `description` | 아이템 설명 (선택) |
| `hasDurability` | 내구도 사용 여부. 약품 용량 등을 표현할 때 활용 |
| `maxDurability` | `hasDurability = true`일 때 기본 최대 내구도 |
| `maxStackCount` | 한 인벤토리 슬롯에 쌓을 수 있는 최대 개수 |

> **저장 위치 권장:** `Assets/Modules/TriageTrainer/ScriptableObjects/Items/`

---

## 2단계: 아이콘 스프라이트 준비

### 2-A. Resources 경로에 배치 (자동 로드)

```
Assets/Resources/Textures/ItemIcons/scalpel.png
```

- 파일명은 `ItemBaseModelSO.identifier`와 **동일**해야 합니다.
- Texture Type: `Sprite (2D and UI)`, Sprite Mode: `Single`로 임포트합니다.
- 스프라이트 파일이 이 경로에 있으면 `Item.Lifecycle.cs`에서 자동으로 로드합니다.

### 2-B. RegistryPreloadItemSO에 직접 지정 (명시적 등록)

`RegistryPreloadItemSO`의 `itemRegistryRequirements` 항목에서 `itemSprite` 필드에 스프라이트를 직접 할당하면 Resources 경로와 무관하게 사용할 수 있습니다.

---

## 3단계: ItemData 서브클래스 구현

인벤토리에서 아이템이 사용/공격될 때의 고유 로직은 `ItemData`를 상속하여 구현합니다.

```csharp
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Item;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Items
{
  [System.Serializable]
  public class ScalpelItemData : ItemData
  {
    // 이 아이템 고유의 직렬화 가능한 필드를 여기 추가할 수 있습니다.

    public ScalpelItemData() { }

    public ScalpelItemData(ItemBaseModelSO baseModel, Sprite icon = null)
      : base(baseModel, icon) { }

    // 복사 생성자 — Clone(), 인벤토리 조작 시 호출됩니다.
    public ScalpelItemData(ScalpelItemData other)
      : base(other)
    {
      // 서브클래스 고유 필드 복사
    }

    public override ActionResult OnAttack(PlayerController player, Entity target)
    {
      // 공격 로직 구현
      Debug.Log($"[ScalpelItemData] OnAttack: player={player.name}, target={target?.name}");
      return ActionResult.Success;
    }

    public override ActionResult OnUse(PlayerController player, Entity target)
    {
      // 사용 로직 구현
      Debug.Log($"[ScalpelItemData] OnUse: player={player.name}, target={target?.name}");
      return ActionResult.Success;
    }

    public override ActionResult OnGet(PlayerController player)
    {
      // 획득 시 로직 (선택)
      return ActionResult.Success;
    }

    public override ActionResult OnDrop(PlayerController player)
    {
      // 버리기 시 로직 (선택)
      return ActionResult.Success;
    }
  }
}
```

재정의 가능한 메서드:

| 메서드 | 호출 시점 |
|---|---|
| `OnGet(player)` | 플레이어가 아이템을 인벤토리에 습득할 때 |
| `OnDrop(player)` | 플레이어가 아이템을 버릴 때 |
| `OnAttack(player, target)` | 핫바에서 공격 입력 시 |
| `OnUse(player, target)` | 핫바에서 사용 입력 시 |

반환값:

| `ActionResult` | 의미 |
|---|---|
| `Success` | 처리 완료 |
| `Passed` | 처리하지 않고 통과 |
| `Cancelled` | 취소 |

---

## 4단계: ItemDataInitializerBase 구현

`ItemData` 서브클래스를 프리팹의 `Item` 컴포넌트에 주입하려면 `ItemDataInitializerBase`를 상속하는 컴포넌트를 프리팹에 추가합니다.

```csharp
using MultiplayerInfrastructure.Item;
using UnityEngine;

namespace TriageTrainer.Items
{
  public class ScalpelItemInitializer : ItemDataInitializerBase
  {
    protected override ItemData CreateItemData(ItemBaseModelSO baseModel, Sprite icon)
    {
      return new ScalpelItemData(baseModel, icon);
    }
  }
}
```

이 컴포넌트의 `Awake()`가 같은 GameObject의 `Item.ApplyItemDataOverride()`를 호출하여, 커스텀 `ScalpelItemData` 인스턴스를 주입합니다.

> **참고:** `ItemDataInitializerBase`의 `baseModel`과 `icon` 필드는 `RegistryPreloadItemSO`와 별개로, 이 컴포넌트 자체에 할당해야 합니다.

---

## 5단계: 프리팹 준비

### 5-1. 기본 구성

1. 3D 오브젝트(메쉬, 콜라이더 포함)로 프리팹을 만듭니다.
2. **`Item` 컴포넌트**를 추가합니다.
   - `_itemBaseModel`: 1단계에서 만든 `ItemBaseModelSO` 할당
   - `_itemIcon`: 스프라이트 직접 할당 시 사용 (없으면 Resources에서 자동 로드)
   - `_itemData`: 비워 둡니다 — 커스텀 로직을 쓴다면 `ItemDataInitializerBase`가 주입합니다.
3. **`NetworkObject` 컴포넌트**를 추가합니다 (FishNet 네트워크 스폰에 필수).
4. 레이어를 `PickupItem`으로 설정합니다 (플레이어의 상호작용 감지 레이어).

### 5-2. 커스텀 로직 추가 시

프리팹에 **`ScalpelItemInitializer` 컴포넌트**를 추가하고:
- `baseModel`: 1단계에서 만든 `ItemBaseModelSO` 할당
- `icon`: 아이콘 스프라이트 할당 (선택)

> **컴포넌트 순서:** Unity는 같은 GameObject의 `Awake()`를 인스펙터 컴포넌트 순서대로 호출합니다.  
> `ScalpelItemInitializer`가 `Item` 컴포넌트보다 **아래**에 있어야 합니다.  
> `Item.Awake()`가 먼저 기본 등록을 마친 뒤, `ScalpelItemInitializer.Awake()`가 `ApplyItemDataOverride`로 덮어쓰는 순서입니다.

> **저장 위치 권장:** `Assets/Modules/TriageTrainer/Prefabs/Items/`

---

## 6단계: RegistryPreloadItemSO에 등록

레지스트리에 등록하지 않으면 인벤토리에서 월드 드롭이 불가능합니다. 또한 아이콘 스프라이트가 UI에 표시되지 않습니다.

1. `RegistryPreloadItemSO` 에셋을 엽니다 (없으면 **+** → `MultiplayerInfrastructure/Registry Preload Item SO`로 생성).
2. `itemRegistryRequirements` 목록에 새 항목을 추가합니다.
3. 각 필드를 채웁니다:

| 필드 | 내용 |
|---|---|
| `itemDataModel` | 1단계에서 만든 `ItemBaseModelSO` |
| `itemPrefab` | 5단계에서 만든 프리팹 (Item 컴포넌트 포함 필수) |
| `itemSprite` | UI에 사용할 아이콘 스프라이트 (Resources 자동 로드를 사용한다면 비워도 됨) |

4. `RegistryPreloadItemSO` 에셋을 씬의 `RegistryPreloaderController` → `preloadItemSO` 필드에 연결합니다.

---

## 구현 체크리스트

- [ ] `ItemBaseModelSO` 생성 및 `identifier` 설정
- [ ] 아이콘 스프라이트 준비 (Resources 경로 또는 SO 직접 할당)
- [ ] `ItemData` 서브클래스 작성 (`OnAttack`/`OnUse` 등 필요한 메서드 override)
- [ ] `ItemDataInitializerBase` 구현 및 프리팹에 컴포넌트 추가
- [ ] 프리팹에 `Item`, `NetworkObject` 컴포넌트 추가 및 레이어 `PickupItem` 설정
- [ ] `RegistryPreloadItemSO`에 등록 및 `RegistryPreloaderController`에 연결

---

## 주의사항

- **`identifier`는 시스템 전체에서 유일해야 합니다.** 중복 시 나중에 등록된 항목이 앞선 항목을 덮어씁니다.
- **`ItemData.Clone()`과 인벤토리 조작 메서드**(`Pop`, `TakeAll` 등)는 **base `ItemData`** 복사 생성자를 사용하므로, 서브클래스 고유 필드는 복사되지 않습니다. 수량·내구도 관련 연산에는 문제가 없으나, 서브클래스 필드가 복사되어야 한다면 별도 처리가 필요합니다.
- **`ItemDataInitializerBase.Awake()` 순서:** 같은 GameObject에서 `Item` 컴포넌트보다 아래에 위치시키세요.
- **월드 드롭 시:** `ItemSpawnUtility.TrySpawnDroppedItem`은 `RegistryType.Item`에 등록된 프리팹 템플릿을 인스턴스화합니다. 등록이 누락되면 드롭이 실패합니다.
