# <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe"></a> Class ItemCombineRecipe

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

아이템 자동 조합 레시피 정의입니다.

레시피는 하나 이상의 재료 아이템(Identifier + 필요 수량)과
생성될 결과 아이템(Identifier + 생성 수량)으로 구성됩니다.

예시: 후두경 블레이드 1개 + 후두경 손잡이 1개 → 후두경 1개
  new ItemCombineRecipe("laryngoscope")
    .Requires("laryngoscope_blade", 1)
    .Requires("laryngoscope_handle", 1)
    .Produces(1)

수량 비율 예시: A 2개 + B 1개 → C 1개
  new ItemCombineRecipe("c")
    .Requires("a", 2)
    .Requires("b", 1)
    .Produces(1)

```csharp
public sealed class ItemCombineRecipe
```

#### Inheritance

object ← 
[ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

## Constructors

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe__ctor_System_String_"></a> ItemCombineRecipe\(string\)

```csharp
public ItemCombineRecipe(string outputItemIdentifier)
```

#### Parameters

`outputItemIdentifier` string

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_Ingredients"></a> Ingredients

재료 목록 (Identifier → 필요 수량)

```csharp
public IReadOnlyList<ItemCombineRecipe.RecipeIngredient> Ingredients { get; }
```

#### Property Value

 IReadOnlyList<[ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md).[RecipeIngredient](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.RecipeIngredient.md)\>

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_OutputItemCount"></a> OutputItemCount

한 번 조합 시 생성되는 아이템 수량

```csharp
public int OutputItemCount { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_OutputItemIdentifier"></a> OutputItemIdentifier

생성될 아이템의 Identifier

```csharp
public string OutputItemIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_Produces_System_Int32_"></a> Produces\(int\)

생성 수량을 설정합니다. 플루언트(fluent) 체이닝 지원.

```csharp
public ItemCombineRecipe Produces(int count)
```

#### Parameters

`count` int

#### Returns

 [ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

### <a id="MultiplayerInfrastructure_ItemSystem_ItemCombineRecipe_Requires_System_String_System_Int32_"></a> Requires\(string, int\)

재료를 추가합니다. 플루언트(fluent) 체이닝 지원.

```csharp
public ItemCombineRecipe Requires(string itemIdentifier, int count = 1)
```

#### Parameters

`itemIdentifier` string

`count` int

#### Returns

 [ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

