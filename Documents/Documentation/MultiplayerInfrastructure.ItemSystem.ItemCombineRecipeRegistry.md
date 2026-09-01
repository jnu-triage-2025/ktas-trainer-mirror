# <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipeRegistry"></a> Class ItemCombineRecipeRegistry

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

아이템 자동 조합 레시피를 관리하는 정적 레지스트리입니다.

레시피 등록:
  ItemCombineRecipeRegistry.Register(
    new ItemCombineRecipe("laryngoscope")
      .Requires("laryngoscope_blade", 1)
      .Requires("laryngoscope_handle", 1)
      .Produces(1));

조합 확인은 <xref href="MultiplayerInfrastructure.ItemSystem.ItemCombineRecipeRegistry.TryGetMatchingRecipe(System.Collections.Generic.IReadOnlyDictionary%7bSystem.String%2cSystem.Int32%7d%2cMultiplayerInfrastructure.ItemSystem.ItemCombineRecipe%40)" data-throw-if-not-resolved="false"></xref> 로 수행합니다.

```csharp
public static class ItemCombineRecipeRegistry
```

#### Inheritance

object ← 
[ItemCombineRecipeRegistry](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipeRegistry.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipeRegistry_Clear"></a> Clear\(\)

등록된 레시피를 모두 제거합니다. 테스트 또는 씬 전환 시 사용합니다.

```csharp
public static void Clear()
```

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipeRegistry_GetAll"></a> GetAll\(\)

등록된 모든 레시피를 반환합니다.

```csharp
public static IReadOnlyList<ItemCombineRecipe> GetAll()
```

#### Returns

 IReadOnlyList<[ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)\>

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipeRegistry_RecipeCanCombine_MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_System_Collections_Generic_IReadOnlyDictionary_System_String_System_Int32__"></a> RecipeCanCombine\(ItemCombineRecipe, IReadOnlyDictionary<string, int\>\)

지정한 레시피를 적용하기 위해 인벤토리에서 재료가 충분한지 검사합니다.

```csharp
public static bool RecipeCanCombine(ItemCombineRecipe recipe, IReadOnlyDictionary<string, int> inventoryCounts)
```

#### Parameters

`recipe` [ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

`inventoryCounts` IReadOnlyDictionary<string, int\>

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipeRegistry_Register_MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_"></a> Register\(ItemCombineRecipe\)

레시피를 등록합니다.

```csharp
public static void Register(ItemCombineRecipe recipe)
```

#### Parameters

`recipe` [ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipeRegistry_TryGetMatchingRecipe_System_Collections_Generic_IReadOnlyDictionary_System_String_System_Int32__MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe__"></a> TryGetMatchingRecipe\(IReadOnlyDictionary<string, int\>, out ItemCombineRecipe\)

인벤토리의 아이템 수량 맵을 기반으로 조합 가능한 레시피를 찾아 반환합니다.
여러 레시피가 매칭될 경우 등록 순서 기준 첫 번째 레시피를 반환합니다.

```csharp
public static bool TryGetMatchingRecipe(IReadOnlyDictionary<string, int> inventoryCounts, out ItemCombineRecipe recipe)
```

#### Parameters

`inventoryCounts` IReadOnlyDictionary<string, int\>

현재 인벤토리의 아이템 수량 맵 (Identifier → count)

`recipe` [ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

매칭된 레시피 (없으면 null)

#### Returns

 bool

조합 가능한 레시피가 있으면 true

