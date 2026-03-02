# 예제 아이템 프리팹

이 폴더의 아이템은 `item-authoring.md` 가이드 패턴을 보여주는 **예시**입니다.  
실제 콘텐츠 아이템은 `Assets/Modules/TriageTrainer/` 하위에 추가하십시오.

---

## 포함된 예제 아이템

| 프리팹 | identifier | 특이 로직 |
|---|---|---|
| `Stone.prefab` | `stone_block` | `OnAttack` — `impactDamage` 만큼 충격 데미지 (내구도 없음) |
| `Wood.prefab` | `wood_block` | `OnAttack` — `durabilityPerSwing`만큼 내구도 소모, 내구도 0이면 취소 |

---

## 관련 스크립트

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Examples/Items/
  StoneItemData.cs          — ItemData 서브클래스 (OnAttack/OnUse override)
  StoneItemInitializer.cs   — ItemDataInitializerBase 구현, 프리팹에 부착
  WoodItemData.cs           — ItemData 서브클래스 (내구도 소모 로직)
  WoodItemInitializer.cs    — ItemDataInitializerBase 구현, 프리팹에 부착
```

---

## 에디터 설정 체크리스트

스크립트가 컴파일된 후 Unity 에디터에서 아래 단계를 완료해야 합니다.

### 1. ItemBaseModelSO 생성

`Assets/Modules/MultiplayerInfrastructure/ScriptableObjects/ItemBaseModels/` 에 두 개의 SO를 생성합니다.  
(`+` → `MultiplayerInfrastructure/Item Base Model`)

**StoneBlock**

| 필드 | 값 |
|---|---|
| `identifier` | `stone_block` |
| `displayName` | `Stone Block` |
| `description` | 단단한 돌 블록입니다. |
| `hasDurability` | `false` |
| `maxStackCount` | `64` |

**WoodBlock**

| 필드 | 값 |
|---|---|
| `identifier` | `wood_block` |
| `displayName` | `Wood Block` |
| `description` | 나무 블록입니다. 사용할수록 내구도가 소모됩니다. |
| `hasDurability` | `true` |
| `maxDurability` | `10` |
| `maxStackCount` | `64` |

### 2. 프리팹 컴포넌트 설정

**Stone.prefab**

1. `Item` 컴포넌트 → `_itemBaseModel`: StoneBlock SO 할당
2. `StoneItemInitializer` 컴포넌트 추가 (`Item` 컴포넌트 **아래**)
   - `baseModel`: StoneBlock SO 할당
   - `impactDamage`: `5` (기본값)
3. `NetworkObject` 컴포넌트가 있는지 확인
4. 레이어: `PickupItem`

**Wood.prefab**

1. `Item` 컴포넌트 → `_itemBaseModel`: WoodBlock SO 할당
2. `WoodItemInitializer` 컴포넌트 추가 (`Item` 컴포넌트 **아래**)
   - `baseModel`: WoodBlock SO 할당
   - `durabilityPerSwing`: `1` (기본값)
3. `NetworkObject` 컴포넌트가 있는지 확인
4. 레이어: `PickupItem`

### 3. RegistryPreloadItemSO 등록

`RegistryPreloadItemSO` 에셋의 `itemRegistryRequirements` 목록에 두 항목을 추가합니다.

| `itemDataModel` | `itemPrefab` | `itemSprite` |
|---|---|---|
| StoneBlock SO | Stone.prefab | (아이콘 스프라이트 또는 비워두면 Resources 경로에서 자동 로드) |
| WoodBlock SO | Wood.prefab | (아이콘 스프라이트 또는 비워두면 Resources 경로에서 자동 로드) |

> 아이콘 자동 로드를 사용하려면 `Assets/Resources/Textures/ItemIcons/stone_block.png`,  
> `Assets/Resources/Textures/ItemIcons/wood_block.png` 파일을 배치하십시오.
