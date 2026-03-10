# 아이템 구현 가이드

새 아이템 구현에 필요한 산출물:

| 산출물 | 역할 |
|---|---|
| `Item` 파생 클래스 | 아이템 정의(const 필드) 및 동작 로직(handler override) |
| 아이콘 스프라이트 | 인벤토리/핫바 UI에 표시할 이미지 |
| 3D 모델 프리팹 (선택) | 월드에 배치되는 시각 오브젝트 |

---

## 1단계: Item 파생 클래스 작성

TriageTrainer 아이템은 `MedicalItem`을 상속합니다. 파일은 `Assets/Modules/TriageTrainer/Scripts/Items/Definitions/` 아래에 **1파일 1클래스**로 작성합니다.

```csharp
namespace TriageTrainer.ItemDefinitions
{
  public class Scalpel : MedicalItem
  {
    public const string Identifier   = "scalpel";
    public const string DisplayName  = "메스";
    public const string Description  = "절개에 사용하는 소형 수술 칼입니다.";
  }
}
```

`MedicalItem`에 이미 선언된 기본값을 재정의하려면 `new` 한정자를 사용합니다:

```csharp
public class SpecialDrug : MedicalItem
{
  public const string Identifier   = "special_drug";
  public const string DisplayName  = "특수 약물";
  public const string Description  = "설명";
  public new const int  MaxStackCount = 1;
  public new const bool EnabledCooldown = true;
  public new const float CooldownMilliseconds = 1000f;
}
```

### MedicalItem 기본값

| 필드 | 기본값 |
|---|---|
| `DetailComment` | `""` |
| `Color` | `"white"` |
| `IsStackable` | `true` |
| `MaxStackCount` | `64` |
| `HasDurability` | `false` |
| `MinReach` / `MaxReach` | `1.0f` / `2.5f` |
| `ItemDamage` | `0` |
| `EnabledCooldown` | `false` |
| `CooldownMilliseconds` | `0f` |

---

## 2단계: 아이콘 스프라이트 준비

### 2-A. Resources 경로에 배치 (자동 로드)

```
Assets/Resources/Textures/ItemIcons/scalpel.png
```

- 파일명은 `Identifier` 값과 **동일**해야 합니다.
- Texture Type: `Sprite (2D and UI)`, Sprite Mode: `Single`로 임포트합니다.
- 이 경로에 파일이 있으면 `Item` 생성 시 자동으로 `CurrentItemIconTexture`에 로드됩니다.

### 2-B. RegistryPreloaderController SO에 직접 지정 (명시적 등록)

`RegistryPreloaderController`에 연결된 SO의 `itemRegistryRequirements` 항목에서 `itemSprite` 필드에 스프라이트를 직접 할당합니다. Resources 경로보다 우선합니다.

---

## 3단계: 핸들러 구현 (선택)

아이템 고유 동작이 필요하면 파생 클래스에서 메서드를 override합니다.

```csharp
using MultiplayerInfrastructure.Player;
using MI = MultiplayerInfrastructure;

namespace TriageTrainer.ItemDefinitions
{
  public class Scalpel : MedicalItem
  {
    public const string Identifier   = "scalpel";
    public const string DisplayName  = "메스";
    public const string Description  = "설명";

    public override ActionResult OnUse(PlayerController player, MI.Entity.Entity target)
    {
      // 사용 로직
      return ActionResult.Success;
    }

    public override ActionResult OnAttack(PlayerController player, MI.Entity.Entity target)
    {
      // 공격 로직
      return ActionResult.Success;
    }
  }
}
```

재정의 가능한 메서드:

| 메서드 | 호출 시점 |
|---|---|
| `OnGet(player)` | 플레이어의 인벤토리에 습득될 때 |
| `OnThrow(player)` | 아이템을 던질 때 |
| `OnInteract(player, target)` | 특정 오브젝트와 상호작용할 때 |
| `OnAttack(player, target)` → `ActionResult` | 공격 입력 시 |
| `OnUse(player, target)` → `ActionResult` | 사용 입력 시 |

반환값:

| `ActionResult` | 의미 |
|---|---|
| `Success` | 처리 완료 |
| `Passed` | 처리하지 않고 통과 |
| `Cancelled` | 취소 |

---

## 4단계: Registry 등록

`Assets/Modules/TriageTrainer/MultiplayerInfrastructure/TTRegistryMonoBehaviourSupport.cs` 의 `RegisterItems()` 메서드에 한 줄을 추가합니다.

```csharp
public void RegisterItems()
{
  // ... 기존 등록 목록 ...
  Registry.RegisterItemDefinition<Scalpel>(Scalpel.Identifier);
}
```

등록하지 않으면 `Registry.CreateItemInstance("scalpel")`이 실패하여 인벤토리 지급·씬 배치가 모두 동작하지 않습니다.

---

## 5단계: 3D 모델 배치 (선택)

월드에서 아이템을 집었을 때 시각적으로 표현할 3D 모델 프리팹을 배치합니다.

```
Assets/Resources/Models/Items/scalpel.prefab
```

- 폴더 경로의 마지막 파일명은 `Identifier` 값과 **동일**해야 합니다.
- 프리팹이 없으면 `ItemObject`가 기본 큐브로 대체합니다.

> ⚠️ **FishNet `NetworkObject` 컴포넌트 주의:** 3D 모델 프리팹에 FishNet의 `NetworkObject` 컴포넌트가 붙어 있으면, FishNet이 네트워크에 등록되지 않은 오브젝트라고 판단하여 자동으로 **비활성화**합니다. 모델이 씬에서 보이지 않는다면 프리팹에서 `NetworkObject`(및 기타 FishNet 컴포넌트)를 제거하십시오.

---

## 구현 체크리스트

- [ ] `Item` 파생 클래스 작성 (`const Identifier`, `DisplayName`, `Description`)
- [ ] 아이콘 스프라이트 준비 (`Resources/Textures/ItemIcons/{identifier}.png` 또는 SO 명시 할당)
- [ ] 필요하다면 `OnUse` / `OnAttack` 등 핸들러 override
- [ ] `TTRegistryMonoBehaviourSupport.RegisterItems()`에 `RegisterItemDefinition<T>(identifier)` 추가
- [ ] 3D 모델이 필요하면 `Resources/Models/Items/{identifier}.prefab` 배치

---

## 씬 배치 (에디터에서 사전 배치)

런타임에 코드로 아이템을 스폰하는 대신, **Unity 에디터에서 씬에 아이템을 미리 배치**하려면 `SceneItemPlacement` 컴포넌트를 사용합니다.

### 사용법

1. 아이템을 놓을 위치에 **빈 GameObject**를 생성합니다.
2. `SceneItemPlacement` 컴포넌트를 추가합니다.
3. 인스펙터의 **Item 드롭다운**에서 아이템을 선택합니다.
   - 프로젝트 내 `Item` 서브클래스가 자동 열거됩니다.
   - 선택값은 `const string Identifier` 기준으로 정렬됩니다.
4. 필요하다면 **Stack Count**를 설정합니다.

### 런타임 동작

```
TTRegistryMonoBehaviourSupport.Awake()  →  모든 아이템 identifier 등록
          ↓
SceneItemPlacement.Start()
  ├─ Registry.CreateItemInstance(identifier)  → Item 인스턴스 생성
  ├─ item.CurrentStackCount = stackCount
  ├─ 클라이언트 전용 피어면 로컬 스폰 생략 후 플레이스홀더 제거
  ├─ 호스트/서버면 _entityIdentifier 보정/유지
  ├─ ItemObject.Spawn(item, transform.position, entityIdentifier: _entityIdentifier)
  │    → 서버 기준 월드 오브젝트 생성 + RegistryType.Entity(EntityType.ItemObject) 등록
  └─ Destroy(this.gameObject)  → 플레이스홀더 제거
```

> **실행 순서:** `Start()`를 사용하므로 모든 `Awake()` 완료 후 실행됩니다.  
> `TTRegistryMonoBehaviourSupport`가 `Awake()`에서 등록하므로 `Start()` 시점에는 항상 Registry가 준비되어 있습니다.

### 씬 뷰 Gizmo

- 초록 구: identifier가 설정된 배치 마커
- 빨간 구: identifier 미설정 경고
- 마커 위에 식별자 레이블 표시

### 주의

- **배치가 되려면 identifier가 `TTRegistryMonoBehaviourSupport`(또는 다른 Preloader)에도 반드시 등록**되어 있어야 합니다.
- **씬 배치 아이템은 authored entity ID를 가져야 합니다.** `SceneItemPlacement`는 `OnValidate()`에서 `_entityIdentifier`를 자동 보정하며, 이 값이 서버와 전체 클라이언트가 공유하는 월드 아이템 ID가 됩니다.
- **클라이언트는 씬 배치 아이템을 직접 확정하지 않습니다.** 접속 후 서버가 현재 `ItemObject` 목록을 다시 보내며, 클라이언트는 그 스냅샷으로 로컬 월드를 재구성합니다.
- `const` 필드가 없는 Item 서브클래스는 드롭다운에 표시되지 않습니다.
- `SceneItemPlacement` GameObject 자체는 Start() 이후 제거됩니다. 연결된 자식 오브젝트는 ItemObject에 포함되지 않으므로 플레이스홀더 GameObject에 다른 컴포넌트를 추가하지 마십시오.

---

## 주의사항

- **`Identifier`는 시스템 전체에서 유일해야 합니다.** 중복 시 나중에 등록된 항목이 앞선 항목을 덮어씁니다.
- **월드 드롭 시:** `player.TryDropItemInFront(item)`는 서버 승인 후에만 월드 아이템을 생성합니다. 서버는 `item:{guid}` 형식의 entity ID를 발급하고, 모든 피어가 같은 ID로 `ItemObject`를 등록합니다.
- **월드 픽업 시:** `LootableItemInteractHandler`는 `TryPickupWorldItem(entityIdentifier)` 경로를 우선 사용합니다. 서버가 아이템 제거를 승인하면 대상 클라이언트만 인벤토리에 반영하고, 실패 시 서버가 보관 중이던 스냅샷으로 같은 entity ID를 복구합니다.
- **`TriageTrainer.MultiplayerInfrastructure` 네임스페이스 충돌:** 이 네임스페이스를 가진 파일에서 `MultiplayerInfrastructure.*`를 참조할 때 `using MI = MultiplayerInfrastructure` 별칭을 사용하지 않으면 CS0234 에러가 발생합니다.

### `const` 기반 정의 시스템 주의사항

`Item` 기반 클래스의 `virtual` 프로퍼티(`Identifier`, `DisplayName`, `Description` 등)는 런타임 리플렉션으로 파생 클래스의 `public const` 필드를 탐색합니다.

- **`const` 누락 시 무음 실패:** 파생 클래스에 해당 `const`가 선언되어 있지 않으면 컴파일 에러 없이 빈 문자열(`""`) 또는 기본값(`0`, `false`)이 반환됩니다. `Identifier`가 비어 있으면 레지스트리 등록·조회가 모두 실패합니다.
  ```csharp
  // ❌ Identifier const 누락 → item.Identifier == ""
  public class BadItem : MedicalItem { }

  // ✅
  public class GoodItem : MedicalItem
  {
    public const string Identifier   = "good_item";
    public const string DisplayName  = "Good Item";
    public const string Description  = "설명";
  }
  ```

- **상위 클래스 `const` 재정의 시 `new` 사용:** `MedicalItem` 등 중간 계층의 `const`를 leaf 클래스에서 다른 값으로 바꾸려면 `new` 한정자를 명시합니다. 생략하면 CS0108 경고가 발생합니다.
  ```csharp
  public class SpecialItem : MedicalItem
  {
    public const string Identifier   = "special_item";
    public const string DisplayName  = "Special Item";
    public const string Description  = "설명";
    public new const int  MaxStackCount = 1;   // MedicalItem 기본값(64)을 재정의
    public new const bool EnabledCooldown = true;
    public new const float CooldownMilliseconds = 500f;
  }
  ```

- **리플렉션 비용:** `InitializeFromDefinitions()`는 아이템 인스턴스 생성 시 1회만 호출됩니다. 결과는 `Current*` 프로퍼티에 캐싱되므로 런타임 핫패스에서 리플렉션이 반복 실행되지 않습니다.
