# API Reference: MultiplayerInfrastructure.ItemSystem.ItemCombineRecipeRegistry

## Namespace
`MultiplayerInfrastructure.ItemSystem`

## Purpose
Static registry that manages a collection of `ItemCombineRecipe` objects and provides lookup based on an inventory count map.

## Thread Safety
All methods must be called from the Unity main thread. The class is not thread‑safe.

## Fields
| Field | Type | Description |
|-------|------|-------------|
| `_recipes` | `private static List<ItemCombineRecipe>` | Internal list storing registered recipes. |

## Methods

### Register
```csharp
public static void Register(ItemCombineRecipe recipe)
```
Registers a new recipe. Throws `ArgumentException` if a recipe with the same identifier (derived from its result item) already exists.

#### Parameters
- `recipe`: The `ItemCombineRecipe` to register.

### Clear
```csharp
public static void Clear()
```
Removes all registered recipes. Primarily used for testing or resetting state between play sessions.

### TryGetMatchingRecipe
```csharp
public static bool TryGetMatchingRecipe(IReadOnlyDictionary<string,int> inventoryCounts, out ItemCombineRecipe recipe)
```
Attempts to find the first recipe whose ingredient requirements are satisfied by the supplied inventory counts.

#### Parameters
- `inventoryCounts`: A read‑only dictionary mapping item identifier to the quantity currently in the inventory.
- `recipe`: Output parameter receiving the matched recipe, or `null` if none matches.

#### Returns
`true` if a matching recipe was found; otherwise `false`.

#### Matching Logic
Iterates over `_recipes` in registration order. For each recipe, checks that for every `Ingredient` the inventory contains at least the required amount. The first recipe that satisfies all its ingredients is returned.

### RecipeCanCombine
```csharp
public static bool RecipeCanCombine(ItemCombineRecipe recipe, IReadOnlyDictionary<string,int> inventoryCounts)
```
Determines whether a specific recipe can be crafted with the given inventory.

#### Parameters
- `recipe`: The recipe to test.
- `inventoryCounts`: Inventory counts as described above.

#### Returns
`true` if the inventory satisfies all ingredient requirements; otherwise `false`.

## Usage Example

```csharp
// Register a recipe (typically done during initialization)
ItemCombineRecipeRegistry.Register(
    new ItemCombineRecipe()
        .Requires("iron_ingot", 2)
        .Requires("wood", 4)
        .Produces("steel_pillar", 1)
);

// Later, when the player's inventory changes:
var counts = new Dictionary<string,int>();
foreach (var kvp in playerInventory.Items) // itemId -> list
    counts[kvp.Key] = kvp.Value.Count;

if (ItemCombineRecipeRegistry.TryGetMatchingRecipe(counts, out var recipe))
{
    // Consume ingredients
    foreach (var ing in recipe.Ingredients)
        playerInventory.RemoveItemFromInventory(creteItemId, ing.RequiredCount);

    // Add result
    var result = Registry.CreateItemInstance(recipe.ResultItemId);
    playerInventory.AddItemToInventory(result, recipe.ResultCount);
}
```

## Notes
- The registry does **not** auto‑remove recipes when their result item is unregistered; it is the caller’s responsibility to keep the registry in sync with the item system if dynamic unloading occurs.
- For performance-critical scenarios with many recipes, consider sorting or indexing the list; however, the current implementation is intended for modest recipe counts (< hundreds).
- Recipes are evaluated in registration order; earlier registrations take priority when multiple recipes are satisfied simultaneously.

