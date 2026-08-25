using System.Collections.Generic;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 아이템 자동 조합 레시피 정의입니다.
  ///
  /// 레시피는 하나 이상의 재료 아이템(Identifier + 필요 수량)과
  /// 생성될 결과 아이템(Identifier + 생성 수량)으로 구성됩니다.
  ///
  /// 예시: 후두경 블레이드 1개 + 후두경 손잡이 1개 → 후두경 1개
  ///   new ItemCombineRecipe("laryngoscope")
  ///     .Requires("laryngoscope_blade", 1)
  ///     .Requires("laryngoscope_handle", 1)
  ///     .Produces(1)
  ///
  /// 수량 비율 예시: A 2개 + B 1개 → C 1개
  ///   new ItemCombineRecipe("c")
  ///     .Requires("a", 2)
  ///     .Requires("b", 1)
  ///     .Produces(1)
  /// </summary>
  public sealed class ItemCombineRecipe
  {
    /// <summary>생성될 아이템의 Identifier</summary>
    public string OutputItemIdentifier { get; }

    /// <summary>한 번 조합 시 생성되는 아이템 수량</summary>
    public int OutputItemCount { get; private set; }

    /// <summary>재료 목록 (Identifier → 필요 수량)</summary>
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;

    private readonly List<RecipeIngredient> _ingredients = new();

    public ItemCombineRecipe(string outputItemIdentifier)
    {
      OutputItemIdentifier = outputItemIdentifier;
      OutputItemCount = 1;
    }

    /// <summary>재료를 추가합니다. 플루언트(fluent) 체이닝 지원.</summary>
    public ItemCombineRecipe Requires(string itemIdentifier, int count = 1)
    {
      _ingredients.Add(new RecipeIngredient(itemIdentifier, count));
      return this;
    }

    /// <summary>생성 수량을 설정합니다. 플루언트(fluent) 체이닝 지원.</summary>
    public ItemCombineRecipe Produces(int count)
    {
      OutputItemCount = count > 0 ? count : 1;
      return this;
    }

    /// <summary>레시피 재료 항목입니다.</summary>
    public readonly struct RecipeIngredient
    {
      /// <summary>재료 아이템 Identifier</summary>
      public string Identifier { get; }

      /// <summary>조합에 필요한 수량</summary>
      public int RequiredCount { get; }

      public RecipeIngredient(string identifier, int requiredCount)
      {
        Identifier = identifier;
        RequiredCount = requiredCount > 0 ? requiredCount : 1;
      }
    }
  }
}
