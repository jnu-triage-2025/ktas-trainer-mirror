# Item

## 개요

아이템 시스템은 **순수 C# 데이터 모델**과 **MonoBehaviour 월드 오브젝트**를 분리하여 설계합니다.

| 클래스 | 역할 |
|---|---|
| `Item` (추상 클래스) | 아이템의 정의(Definition)와 런타임 상태(Instance)를 포함하는 순수 C# 클래스 |
| `ItemObject` (MonoBehaviour) | 월드 상에 실제로 배치되는 3D 오브젝트. `Item` 인스턴스를 보유 |
| `Registry` | `System.Type` 기반으로 아이템 클래스를 등록·조회 |

---

## Item 클래스 구조

`Item`은 순수 C#이며 Unity에 종속되지 않습니다.

### Definitions (정의 레이어)

파생 클래스에서 `public const` 필드로 선언합니다. `Item`의 `virtual` 프로퍼티가 런타임 리플렉션으로 읽어오며, 인스턴스 생성 시 `Current*` 프로퍼티에 캐싱됩니다.

```csharp
public class Ambubag : MedicalItem
{
  public const string Identifier   = "ambubag";
  public const string DisplayName  = "앰부백";
  public const string Description  = "설명";
}
```

`MedicalItem`(또는 다른 중간 추상 클래스)에 이미 선언된 `const`를 재정의하려면 `new` 한정자를 사용합니다.

```csharp
public class SpecialItem : MedicalItem
{
  public const string Identifier   = "special_item";
  public const string DisplayName  = "특수 아이템";
  public const string Description  = "설명";
  public new const int  MaxStackCount = 1;          // MedicalItem 기본값(64) 재정의
  public new const bool EnabledCooldown = true;
  public new const float CooldownMilliseconds = 500f;
}
```

### Instance (상태 레이어)

`Current*` 프로퍼티. 생성자의 `InitializeFromDefinitions()`에서 Definitions 값으로 초기화되며, 런타임 중 변경 가능합니다.

| Instance 프로퍼티 | 대응 Definitions const |
|---|---|
| `CurrentIdentifier` | `Identifier` |
| `CurrentDisplayName` | `DisplayName` |
| `CurrentDescription` | `Description` |
| `CurrentStackCount` / `CurrentMaxStackCount` | `IsStackable`, `MaxStackCount` |
| `CurrentDurability` / `CurrentMaxDurability` | `HasDurability`, `MaxDurability` |
| `CurrentItemIconTexture` | `Registry.GetOrLoadIconSprite(Identifier)` 자동 로드 |

### Handlers

아이템 고유 동작은 다음 메서드를 파생 클래스에서 override합니다.

| 메서드 | 호출 시점 |
|---|---|
| `OnGet(player)` | 플레이어의 인벤토리에 습득될 때 |
| `OnThrow(player)` | 아이템을 던질 때 |
| `OnInteract(player, target)` | 특정 오브젝트와 상호작용할 때 |
| `OnAttack(player, target)` → `ActionResult` | 공격 입력 시 |
| `OnUse(player, target)` → `ActionResult` | 사용 입력 시 |

> **단계별 구현 절차는 [item-authoring.md](item-authoring.md)를 참조하세요.**

---

## ItemObject

`ItemObject`는 월드 상에 배치되는 MonoBehaviour입니다. Unity(Rigidbody, Collider)에 관련된 모든 처리가 여기에 있습니다.

```
ItemObject  (Rigidbody, BoxCollider, PickupItem 레이어)
└ ItemGroundedModel  ← Resources/Models/Items/{identifier} 에서 자동 로드
```

팩토리 메서드:

```csharp
// 기본 스폰
ItemObject.Spawn(item, position);

// 드롭/던지기 (Rigidbody impulse 적용)
ItemObject.Spawn(item, position, throwForce);
```

3D 모델 프리팹이 `Resources/Models/Items/{identifier}` 경로에 없으면 기본 큐브로 대체됩니다.
`ItemObject.Spawn()` 호출 시 `LootableItemInteractHandler` 컴포넌트가 **자동으로 부착**됩니다. 별도 설정 없이도 `NearbyInteractablesDetector`가 아이템을 감지하고, 플레이어가 F 키를 누르면 자동으로 인벤토리에 주울 수 있습니다.

### 월드 픽업 흐름

```
NearbyInteractablesDetector (OverlapSphere)
  → LootableItemInteractHandler 감지 (IInteractable)
  → HUD에 "아이템 획득" 표시
  → F 키
  → LootableItemInteractHandler.Interact(interactor)
      ├─ PlayerController.TryAddItemToInventory(item)
      ├─ item.OnGet(player)   (상속 메서드)
      └─ Destroy(ItemObject)
```

> ⚠️ FishNet `NetworkObject` 컴포넌트가 모델 프리팩에 붙어 있으면 FishNet이 처음 인스턴스를 **비활성화**합니다. 프리팩에서 FishNet 컴포넌트를 제거하세요.
---

## PlayerController 아이템 흐름

```
핫바 선택 변경
  → HandlingItem 갱신 (Item 인스턴스)
  → TriggerAttack / TriggerUseItem
  → OnAttack(player, target) / OnUse(player, target)
  → ActionResult 처리
```

월드 드롭:

```csharp
player.TryDropItemInFront(item);   // ItemObject.Spawn 내부 호출
```

---

## Registry 등록

아이템 클래스를 사용하려면 사전에 `RegisterItemDefinition<T>` 로 등록합니다.
`RegistryType.Item`에는 `System.Type`이 저장됩니다 (프리팹이 아닙니다).

```csharp
// 등록
Registry.RegisterItemDefinition<Ambubag>(Ambubag.Identifier);

// 인스턴스 생성
var item = Registry.CreateItemInstance("ambubag");   // → new Ambubag()
```

TriageTrainer에서는 `TTRegistryMonoBehaviourSupport.Awake()`에서 전체 아이템을 등록합니다.
새 아이템을 추가할 때 반드시 이 파일에도 한 줄을 추가해야 합니다.

아이콘 스프라이트는 `RegistryPreloaderController`에 연결된 SO의 `itemSprite` 필드로 명시적으로 등록하거나, `Resources/Textures/ItemIcons/{identifier}.png` 에 배치하면 자동 로드됩니다.

---

## 구현 체크리스트

- [ ] `Item` 파생 클래스 작성 (`const Identifier`, `DisplayName`, `Description`)
- [ ] 아이콘 스프라이트 준비 (`Resources/Textures/ItemIcons/{identifier}.png` 또는 SO 명시 할당)
- [ ] 필요하다면 `OnUse` / `OnAttack` 등 핸들러 override
- [ ] `TTRegistryMonoBehaviourSupport.RegisterItems()`에 `RegisterItemDefinition<T>(identifier)` 추가
- [ ] 3D 모델이 필요하면 `Resources/Models/Items/{identifier}` 에 프리팹 배치

---

## 주의사항

### `const` 기반 정의 시스템

`Item`의 `virtual` 프로퍼티는 런타임 리플렉션으로 파생 클래스의 `public const` 필드를 읽습니다.

- **`const` 누락 시 무음 실패:** `Identifier` 등이 없으면 컴파일 에러 없이 `""` 반환 → Registry 등록·조회 실패
- **상위 클래스 `const` 재정의 시 `new` 사용:** CS0108 경고 억제
- **리플렉션 비용:** `InitializeFromDefinitions()` 호출 시 1회만 실행, `Current*`에 캐싱

자세한 예시와 설명은 [item-authoring.md](item-authoring.md)의 주의사항 절을 참조하세요.
