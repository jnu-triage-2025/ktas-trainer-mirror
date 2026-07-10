# API Reference: MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe

## 네임스페이스
`MultiplayerInfrastructure.ItemSystem`

## 클래스 정의
```csharp
public class ItemCombineRecipe
```

## 목적
하나의 아이템 조합 레시피를 나타냅니다. 재료 목록과 결과 아이템을 정의하며, 플루언트 API를 통해 직관적으로 생성할 수 있습니다.

## 생성자 및 플루언트 API
| 메서드 | 설명 |
|--------|------|
| `ItemCombineRecipe()` | 기본 생성자 |
| `Requires(string itemId, int count)` | 필요한 아이템과 개수를 추가합니다. 여러 번 호출해 여러 재료를 지정할 수 있습니다. |
| `Produces(string itemId, int count)` | 결과 아이템과 개수를 지정합니다. 반드시 한 번 호출해야 합니다. |
| `Build()` (내부) | 플루언트 체인을 종료하고 불변 객체를 반환합니다 (내부에서 자동 호출). |

### 사용 예시
```csharp
var recipe = new ItemCombineRecipe()
    .Requires("iron_ingot", 2)
    .Requires("wood", 4)
    .Produces("steel_pillar", 1);
```

## 속성 (읽기 전용)
| 속성 | 타입 | 설명 |
|------|------|------|
| `Ingredients` | `IReadOnlyList<RecipeIngredient>` | 필요한 재료 목록 |
| `ResultItemId` | `string` | 결과 아이템 식별자 |
| `ResultCount` | `int` | 결과 아이템 개수 |

### `RecipeIngredient` 구조체
| 필드 | 타입 | 설명 |
|------|------|------|
| `ItemId` | `string` | 재료 아이템 식별자 |
| `RequiredCount` | `int` | 필요한 개수 |

## 정적 메서드 (등록소)
`ItemCombineRecipeRegistry` 클래스를 통해 레시피를 등록하고 조회합니다.

| 메서드 | 설명 |
|--------|------|
| `Register(ItemCombineRecipe recipe)` | 레시피를 등록합니다. 동일 ID 중복 등록은 예외 발생. |
| `TryGetMatchingRecipe(IDictionary<string,int> inventoryCounts, out ItemCombineRecipe recipe)` | 현재 인벤토리 충족 레시피를 반환합니다. 첫 번째로 일치하는 레시피를 반환합니다. |
| `RecipeCanCombine(ItemCombineRecipe recipe, IDictionary<string,int> inventoryCounts)` | 특정 레시피가 현재 인벤토리로 조합 가능한지 검사합니다. |
| `Clear()` | 모든 등록된 레시피를 제거합니다 (주로 테스트용). |

## 사용 예시 (PlayerController 내부)
```csharp
private void TryAutoCombineItems()
{
    var counts = new Dictionary<string,int>();
    foreach (var kvp in Inventory.Items) // 아이템ID -> 개수
        counts[kvp.Key] = kvp.Value.Count;

    while (ItemCombineRecipeRegistry.TryGetMatchingRecipe(counts, out var recipe))
    {
        // 재료 소비
        foreach (var ing in recipe.Ingredients)
            RemoveItemFromInventory(ing.ItemId, ing.RequiredCount);

        // 결과 추가
        var resultItem = Registry.CreateItemInstance(recipe.ResultItemId);
        if (resultItem != null)
            AddItemToInventory(resultItem, recipe.ResultCount);
    }
}
```

## 스레드 안전성
- 등록 및 조회는 메인 스레드에서만 수행해야 합니다 (Unity의 단일 스레드 특성 follow).
- 런타임 중에 등록/변경이 필요할 경우 `Register`/`Clear`를 메인 스레드에서 호출하세요.

## 버전
- 도입 버전: 2026-07-06
- 관련 파일: `ItemCombineRecipe.cs`, `ItemCombineRecipeRegistry.cs`
