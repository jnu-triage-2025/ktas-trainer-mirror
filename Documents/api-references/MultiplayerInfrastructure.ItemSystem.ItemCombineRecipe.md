# API 레퍼런스: `MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe`

> 네임스페이스: `MultiplayerInfrastructure.ItemSystem`
> 주요 파일:
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/ItemCombineRecipe.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/ItemCombineRecipeRegistry.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Inventory.cs` (자동 조합 처리)
> - `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/RegisteringMultiplayerInfrastructureSupport.Item.cs` (레시피 등록)

---

## 0. 개요

`ItemCombineRecipe`는 **복수의 아이템을 인벤토리에 모두 획득했을 때 자동으로 결과 아이템으로 변환하는 레시피**를 정의한다.

`ItemCombineRecipeRegistry`는 이러한 레시피를 정적으로 관리하는 레지스트리다. 인벤토리에 아이템이 추가될 때마다 `PlayerController`가 이 레지스트리를 조회하여 충족 레시피를 찾고, 재료를 소비한 뒤 결과 아이템을 지급한다.

이 시스템은 `ScenarioCombineItemNode`(시나리오 그래프 기반 조합)와 독립적으로 동작하며, 시나리오 진행 상태와 무관하게 항상 적용된다.

---

## 1. `ItemCombineRecipe`

### 선언

```csharp
public sealed class ItemCombineRecipe
```

### 생성자

```csharp
public ItemCombineRecipe(string outputItemIdentifier)
```

결과 아이템의 `Identifier`를 인수로 받아 레시피 인스턴스를 생성한다.

### 프로퍼티

| 이름 | 타입 | 설명 |
|---|---|---|
| `OutputItemIdentifier` | `string` | 결과 아이템의 Identifier |
| `OutputItemCount` | `int` | 조합 1회당 생성 수량 (기본값: 1) |
| `Ingredients` | `IReadOnlyList<RecipeIngredient>` | 재료 목록 |

### 메서드

#### `Requires(string itemIdentifier, int count = 1) : ItemCombineRecipe`

재료를 추가한다. 플루언트 체이닝 지원.

- `itemIdentifier`: 재료 아이템의 `Identifier`
- `count`: 필요 수량 (기본값 1, 0 이하는 1로 보정)
- 반환값: 자기 자신(`this`)

#### `Produces(int count) : ItemCombineRecipe`

생성 수량을 설정한다. 플루언트 체이닝 지원.

- `count`: 결과 아이템 생성 수량 (0 이하는 1로 보정)
- 반환값: 자기 자신(`this`)

### 중첩 구조체: `RecipeIngredient`

```csharp
public readonly struct RecipeIngredient
{
  public string Identifier { get; }   // 재료 아이템 Identifier
  public int RequiredCount { get; }   // 필요 수량
}
```

### 사용 예시

```csharp
// A 2개 + B 1개 → C 1개
var recipe = new ItemCombineRecipe("c")
  .Requires("a", 2)
  .Requires("b", 1)
  .Produces(1);

// 후두경 블레이드 + 후두경 손잡이 → 후두경
var laryngoscopeRecipe = new ItemCombineRecipe(Laryngoscope.Identifier)
  .Requires(LaryngoscopeBlade.Identifier, 1)
  .Requires(LaryngoscopeHandle.Identifier, 1)
  .Produces(1);
```

---

## 2. `ItemCombineRecipeRegistry`

### 선언

```csharp
public static class ItemCombineRecipeRegistry
```

전역 정적 레지스트리. 씬 전환 등으로 재초기화가 필요하면 `Clear()` 후 재등록한다.

### 메서드

#### `Register(ItemCombineRecipe recipe)`

레시피를 등록한다. `recipe`가 `null`이거나 `OutputItemIdentifier`가 비어 있으면 무시한다.

#### `GetAll() : IReadOnlyList<ItemCombineRecipe>`

등록된 모든 레시피를 반환한다.

#### `Clear()`

등록된 레시피를 모두 제거한다.

#### `TryGetMatchingRecipe(IReadOnlyDictionary<string, int> inventoryCounts, out ItemCombineRecipe recipe) : bool`

인벤토리 수량 맵을 기준으로 충족 가능한 첫 번째 레시피를 반환한다.

- `inventoryCounts`: `{ identifier → 현재 수량 }` 형태의 딕셔너리
- `recipe`: 충족 레시피 (없으면 `null`)
- 반환값: 충족 레시피가 있으면 `true`

여러 레시피가 동시에 충족될 경우 **등록 순서 기준 첫 번째 레시피**가 반환된다.

#### `RecipeCanCombine(ItemCombineRecipe recipe, IReadOnlyDictionary<string, int> inventoryCounts) : bool`

특정 레시피를 적용하기 위해 재료가 충족되는지 직접 검사한다.

---

## 3. `PlayerController` 자동 조합 처리

### 처리 흐름

```
TryAddItemToInventory(item)
  → 인벤토리에 아이템 추가 성공
  → item.OnGet(player)
  → TryAutoCombineItems()
      ├─ 인벤토리 수량 맵 수집
      ├─ ItemCombineRecipeRegistry.TryGetMatchingRecipe(counts, out recipe)
      │    ├─ false → 종료
      │    └─ true
      │         ├─ RemoveItemFromInventory(재료 각각 필요 수량)
      │         ├─ Registry.CreateItemInstance(recipe.OutputItemIdentifier)
      │         ├─ outputItem.CurrentStackCount = recipe.OutputItemCount
      │         ├─ TryAddItemToInventory(outputItem)  ← 재귀적 while로 반복
      │         └─ (다시 처음으로)
      └─ 충족 레시피 없을 때까지 반복
```

`TryAddItemToInventory` 내부에서 `TryAutoCombineItems`를 호출하므로, 결과 아이템이 또 다른 레시피의 재료가 되는 연속 조합이 자동으로 처리된다.

### 로그 출력 (에디터 전용)

`UNITY_EDITOR` 빌드에서는 조합이 발생할 때마다 다음과 같은 Debug.Log가 출력된다:

```
[PlayerController] AutoCombine: [laryngoscope_blade×1, laryngoscope_handle×1] → laryngoscope×1
```

---

## 4. 레시피 등록 위치 (TriageTrainer)

TriageTrainer 프로젝트의 레시피는 다음 파일에 등록한다:

```
Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/
  RegisteringMultiplayerInfrastructureSupport.Item.cs
```

`RegisterAllCombineRecipes()` 메서드 안에 `ItemCombineRecipeRegistry.Register(...)` 호출을 추가한다.

### 현재 등록된 레시피

| 재료 | 결과 |
|---|---|
| `laryngoscope_blade` × 1 + `laryngoscope_handle` × 1 | `laryngoscope` × 1 |
| `endotracheal_tube` × 1 + `stylet` × 1 | `endotracheal_tube_ready` × 1 |

---

## 5. `ScenarioCombineItemNode`와의 차이

| 구분 | `ScenarioCombineItemNode` | `ItemCombineRecipeRegistry` |
|---|---|---|
| 동작 조건 | 시나리오 그래프가 해당 노드에 도달했을 때 | 인벤토리에 재료가 모두 존재할 때 항상 |
| 재료 수량 비율 | 미지원 | 지원 |
| 연속 조합 | 미지원 | 지원 |
| 적합한 사용처 | 스토리 흐름 제어가 필요한 조합 | 물리적 아이템 조립 |

---

## 6. 관련 문서

- 설정 가이드(비전문가용): [`../working-guide/features/items/item-auto-combine-setup-guide.md`](../working-guide/features/items/item-auto-combine-setup-guide.md)
- 변경 노트: [`../changes/2026-07-06-item-auto-combine.md`](../changes/2026-07-06-item-auto-combine.md)
- 아이템 정의/등록: [`../working-guide/item-authoring.md`](../working-guide/item-authoring.md)
