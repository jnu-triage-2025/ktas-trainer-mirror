# 아이템 자동 조합 시스템 도입 (2026-07-06)

## 변경 목적

특정 아이템 조합을 획득하면 자동으로 합쳐진 아이템으로 변환되는 정규 로직을 도입합니다.

기존에는 `ScenarioCombineItemNode`를 통한 시나리오 흐름 의존 방식만 존재했습니다. 이 방식은 시나리오 그래프 없이는 동작하지 않고, 재료 수량 비율 정의가 불가했습니다. 이번 변경으로 인벤토리 레벨의 범용 자동 조합 로직을 별도로 구축하여 시나리오 흐름과 무관하게 항상 동작합니다.

---

## 핵심 변경 사항

### 1) `ItemCombineRecipe` 신규 클래스

- **파일**: `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/ItemCombineRecipe.cs`
- 하나의 조합 레시피를 나타냅니다.
- 재료 목록(`RecipeIngredient`: Identifier + 필요 수량)과 결과 아이템(Identifier + 생성 수량)으로 구성됩니다.
- 플루언트 API 지원: `.Requires("a", 2).Requires("b", 1).Produces(1)` 형태로 선언합니다.
- `a 2개 + b 1개 → c 1개` 방식의 수량 비율을 정의할 수 있습니다.

### 2) `ItemCombineRecipeRegistry` 신규 클래스

- **파일**: `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/ItemCombineRecipeRegistry.cs`
- 등록된 레시피 목록을 관리하는 정적 레지스트리입니다.
- 주요 API: `Register(recipe)`, `Clear()`, `TryGetMatchingRecipe(inventoryCounts, out recipe)`, `RecipeCanCombine(recipe, inventoryCounts)`
- 인벤토리 수량 맵(`Dictionary<string, int>`)을 받아 첫 번째 충족 레시피를 반환합니다.

### 3) `PlayerController.Inventory` — 자동 조합 처리 추가

- **파일**: `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Inventory.cs`
- `TryAddItemToInventory()` 성공 직후 `TryAutoCombineItems()`를 호출합니다.
- `TryAutoCombineItems()` 동작:
  1. 현재 인벤토리 수량 맵을 수집합니다.
  2. `ItemCombineRecipeRegistry.TryGetMatchingRecipe()`로 충족 레시피를 탐색합니다.
  3. 레시피가 있으면 재료를 소비(`RemoveItemFromInventory`)하고 결과 아이템을 생성(`Registry.CreateItemInstance`)하여 인벤토리에 추가합니다.
  4. while 루프로 더 이상 충족 레시피가 없을 때까지 반복합니다(연속 조합 지원).
  5. 에디터 빌드(`UNITY_EDITOR`)에서는 `Debug.Log`로 조합 결과를 출력합니다.

### 4) `RegisteringMultiplayerInfrastructureSupport.Item.cs` — 레시피 등록 메서드 추가

- **파일**: `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/RegisteringMultiplayerInfrastructureSupport.Item.cs`
- `Awake_Item()`에 `RegisterAllCombineRecipes()` 호출이 추가되었습니다.
- `RegisterAllCombineRecipes()` 메서드에 현재 레시피 2개가 등록됩니다:
  - `후두경_블레이드 × 1 + 후두경_손잡이 × 1 → 후두경 × 1`
  - `기관내관 × 1 + 스타일렛 × 1 → 기관내관_(준비_완료) × 1`
- 새 레시피는 이 메서드에만 추가하면 됩니다.

### 5) 아이템 Description 문구 업데이트

조합 가능한 재료 아이템 4개의 Description에 조합 안내 문구를 추가했습니다.

| 파일 | 변경 내용 |
|---|---|
| `LaryngoScopeBlade.cs` | "[후두경 블레이드, 후두경 손잡이]을 획득하면 [후두경]으로 자동 조합됩니다." 추가 |
| `LaryngoscopeHandle.cs` | "[후두경 블레이드, 후두경 손잡이]을 획득하면 [후두경]으로 자동 조합됩니다." 추가 |
| `EndotrachealTube.cs` | "[기관내관, 스타일렛]을 획득하면 [기관내관 (준비 완료)]으로 자동 조합됩니다." 추가 |
| `Stylet.cs` | "[기관내관, 스타일렛]을 획득하면 [기관내관 (준비 완료)]으로 자동 조합됩니다." 추가 |

---

## 기존 `ScenarioCombineItemNode`와의 관계

이번 변경은 기존 시나리오 그래프의 `CombineItem` 노드를 대체하지 않습니다. 두 방식은 독립적으로 공존합니다.

| 구분 | `ScenarioCombineItemNode` | `ItemCombineRecipeRegistry` (이번 변경) |
|---|---|---|
| 동작 조건 | 시나리오 그래프가 해당 노드에 도달했을 때 | 인벤토리에 재료가 모두 존재할 때 항상 |
| 재료 수량 비율 | 미지원 | 지원 (`Requires("a", 2)`) |
| 연속 조합 | 미지원 | 지원 |
| 적합한 사용처 | 스토리 흐름 제어가 필요한 조합 | 물리적 아이템 조립 (재료가 갖춰지면 즉시 완성) |

---

## 검증

- Play 모드에서 `/give laryngoscope_blade` → `/give laryngoscope_handle` 순으로 지급 시 `후두경`으로 자동 조합 확인.
- Play 모드에서 `/give endotracheal_tube` → `/give stylet` 순으로 지급 시 `기관내관 (준비 완료)`으로 자동 조합 확인.
- 에디터 Console에 `[PlayerController] AutoCombine:` 로그 출력 확인.
- 재료만 있고 다른 쪽 재료가 없으면 조합이 발생하지 않음을 확인.

---

## 신규 문서

- 작업 가이드: `Documents/working-guide/features/items/item-auto-combine-setup-guide.md`
- API 레퍼런스: `Documents/api-references/MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md`
