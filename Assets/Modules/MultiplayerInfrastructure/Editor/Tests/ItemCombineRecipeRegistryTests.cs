using System.Collections.Generic;
using MultiplayerInfrastructure.ItemSystem;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Editor.Tests
{
  public sealed class ItemCombineRecipeRegistryTests
  {
    [Test]
    public void HasAnyIngredientReturnsTrueWhenOnlyOneIngredientIsOwned()
    {
      var recipe = new ItemCombineRecipe("output")
        .Requires("ingredient-a", 2)
        .Requires("ingredient-b", 1);
      var inventoryCounts = new Dictionary<string, int>
      {
        ["ingredient-a"] = 1
      };

      Assert.That(ItemCombineRecipeRegistry.HasAnyIngredient(recipe, inventoryCounts), Is.True);
      Assert.That(ItemCombineRecipeRegistry.RecipeCanCombine(recipe, inventoryCounts), Is.False);
    }

    [Test]
    public void HasAnyIngredientReturnsFalseWhenNoIngredientIsOwned()
    {
      var recipe = new ItemCombineRecipe("output")
        .Requires("ingredient-a", 1)
        .Requires("ingredient-b", 1);
      var inventoryCounts = new Dictionary<string, int>
      {
        ["unrelated-item"] = 1,
        ["ingredient-a"] = 0
      };

      Assert.That(ItemCombineRecipeRegistry.HasAnyIngredient(recipe, inventoryCounts), Is.False);
    }
  }
}
