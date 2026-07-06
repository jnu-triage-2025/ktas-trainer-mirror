using System.Collections.Generic;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 아이템 자동 조합 레시피를 관리하는 정적 레지스트리입니다.
  ///
  /// 레시피 등록:
  ///   ItemCombineRecipeRegistry.Register(
  ///     new ItemCombineRecipe("laryngoscope")
  ///       .Requires("laryngoscope_blade", 1)
  ///       .Requires("laryngoscope_handle", 1)
  ///       .Produces(1));
  ///
  /// 조합 확인은 <see cref="TryGetMatchingRecipe"/> 로 수행합니다.
  /// </summary>
  public static class ItemCombineRecipeRegistry
  {
    private static readonly List<ItemCombineRecipe> _recipes = new();

    /// <summary>레시피를 등록합니다.</summary>
    public static void Register(ItemCombineRecipe recipe)
    {
      if (recipe == null || string.IsNullOrWhiteSpace(recipe.OutputItemIdentifier))
        return;

      _recipes.Add(recipe);
    }

    /// <summary>등록된 모든 레시피를 반환합니다.</summary>
    public static IReadOnlyList<ItemCombineRecipe> GetAll() => _recipes;

    /// <summary>등록된 레시피를 모두 제거합니다. 테스트 또는 씬 전환 시 사용합니다.</summary>
    public static void Clear() => _recipes.Clear();

    /// <summary>
    /// 인벤토리의 아이템 수량 맵을 기반으로 조합 가능한 레시피를 찾아 반환합니다.
    /// 여러 레시피가 매칭될 경우 등록 순서 기준 첫 번째 레시피를 반환합니다.
    /// </summary>
    /// <param name="inventoryCounts">현재 인벤토리의 아이템 수량 맵 (Identifier → count)</param>
    /// <param name="recipe">매칭된 레시피 (없으면 null)</param>
    /// <returns>조합 가능한 레시피가 있으면 true</returns>
    public static bool TryGetMatchingRecipe(
      IReadOnlyDictionary<string, int> inventoryCounts,
      out ItemCombineRecipe recipe)
    {
      recipe = null;
      if (inventoryCounts == null || _recipes.Count == 0)
        return false;

      for (int i = 0; i < _recipes.Count; i++)
      {
        var candidate = _recipes[i];
        if (RecipeCanCombine(candidate, inventoryCounts))
        {
          recipe = candidate;
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// 지정한 레시피를 적용하기 위해 인벤토리에서 재료가 충분한지 검사합니다.
    /// </summary>
    public static bool RecipeCanCombine(
      ItemCombineRecipe recipe,
      IReadOnlyDictionary<string, int> inventoryCounts)
    {
      if (recipe == null || inventoryCounts == null)
        return false;

      var ingredients = recipe.Ingredients;
      for (int i = 0; i < ingredients.Count; i++)
      {
        var ing = ingredients[i];
        if (!inventoryCounts.TryGetValue(ing.Identifier, out int available))
          return false;
        if (available < ing.RequiredCount)
          return false;
      }

      return true;
    }
  }
}
