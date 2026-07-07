using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.ItemSystem;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// InventoryUIView 의 조합(crafting) 확장.
  ///
  /// 인벤토리 우측에 조합 패널을 구성한다.
  ///   (b) 상단 : 현재 선택된 레시피의 "필요 아이템" 칸 (요구 수량 / 보유 수량)
  ///   ───── 구분선 ─────
  ///   (a) 하단 : 현재 조합 가능한 결과 아이템 목록
  ///
  /// 상호작용:
  ///   · (a) 목록에서 아이템을 한 번 클릭 → 선택(=(b) 에 필요 아이템 표시)
  ///   · 선택된 상태에서 같은 아이템을 다시 클릭 → 조합 실행
  ///       1. 인벤토리에서 요구 아이템 소비
  ///       2. 결과 아이템이 커서(held item)로 pickup 됨
  ///       3. 계속 클릭하면 커서의 스택이 누적됨
  /// </summary>
  public partial class InventoryUIView
  {
    /// <summary>조합 패널에 표시할 레시피 1건.</summary>
    public sealed class CraftableRecipeDisplay
    {
      public string OutputIdentifier;
      public IReadOnlyList<Ingredient> Ingredients;

      public readonly struct Ingredient
      {
        public readonly string Identifier;
        public readonly int RequiredCount;

        public Ingredient(string identifier, int requiredCount)
        {
          Identifier = identifier;
          RequiredCount = requiredCount;
        }
      }
    }

    // 컨트롤러가 주입하는 콜백들.
    // - _heldCountResolver     : identifier → 인벤토리 보유 수량
    // - _craftRequestHandler   : 선택된 레시피 조합 실행 요청 → 생성된 결과 Item 반환(실패 시 null)
    private Func<string, int> _heldCountResolver;
    private Func<CraftableRecipeDisplay, Item> _craftRequestHandler;

    /// <summary>
    /// 커서 스택 한도를 초과해 커서에 담지 못한 조합 결과 잔량을 인벤토리로 돌려보내기 위한 이벤트.
    /// 컨트롤러가 구독하여 플레이어 인벤토리에 다시 추가한다.
    /// </summary>
    public event Action<Item> CraftLeftoverReturned;

    private VisualElement _craftingPanel;
    private VisualElement _craftingRequirements;
    private VisualElement _craftingRecipeList;

    private readonly List<CraftableRecipeDisplay> _craftableRecipes = new();

    private string _selectedRecipeOutputId;

    /// <summary>컨트롤러가 조합에 필요한 콜백을 주입한다.</summary>
    public void SetCraftingCallbacks(
      Func<string, int> heldCountResolver,
      Func<CraftableRecipeDisplay, Item> craftRequestHandler)
    {
      _heldCountResolver = heldCountResolver;
      _craftRequestHandler = craftRequestHandler;
    }

    private void BuildCraftingPanel()
    {
      // 루트(InventoryUIView) 직속 자식으로 조합 패널을 붙인다.
      // 좌측 인벤토리 패널(InventoryPanel)과 나란히 배치(.inventory-root: flex-direction: row).
      if (_craftingPanel != null)
      {
        _craftingPanel.RemoveFromHierarchy();
        _craftingPanel = null;
      }

      _craftingPanel = new VisualElement { name = "CraftingPanel" };
      _craftingPanel.AddToClassList("crafting-panel");

      var title = new Label { text = "조합", name = "CraftingTitle" };
      title.AddToClassList("crafting-title");
      _craftingPanel.Add(title);

      // (b) 필요 아이템 섹션
      var reqLabel = new Label { text = "필요 아이템", name = "CraftingRequirementsLabel" };
      reqLabel.AddToClassList("crafting-section-label");
      _craftingPanel.Add(reqLabel);

      _craftingRequirements = new VisualElement { name = "CraftingRequirements" };
      _craftingRequirements.AddToClassList("crafting-requirements");
      _craftingPanel.Add(_craftingRequirements);

      // 구분선
      var divider = new VisualElement { name = "CraftingDivider" };
      divider.AddToClassList("crafting-divider");
      _craftingPanel.Add(divider);

      // (a) 조합 가능한 아이템 목록 섹션
      var listLabel = new Label { text = "조합 가능", name = "CraftingListLabel" };
      listLabel.AddToClassList("crafting-section-label");
      _craftingPanel.Add(listLabel);

      _craftingRecipeList = new VisualElement { name = "CraftingRecipeList" };
      _craftingRecipeList.AddToClassList("crafting-recipe-list");
      _craftingPanel.Add(_craftingRecipeList);

      // 좌측 인벤토리 패널(InventoryPanel) 다음에 추가하여 우측에 배치한다
      // (.inventory-root: flex-direction: row). BuildCraftingPanel 은 ghost/tooltip 생성보다
      // 먼저 호출되므로, Add 순서상 ghost/tooltip 이 조합 패널보다 나중에 추가되어 위에 그려진다.
      Add(_craftingPanel);

      RenderRequirements();
      RenderRecipeList();
    }

    /// <summary>
    /// 조합 가능한 레시피 목록을 갱신한다. 컨트롤러가 인벤토리 변화 시마다 호출한다.
    /// 선택 상태는 가능하면 유지하되, 더 이상 조합 불가능한 레시피면 선택을 해제한다.
    /// </summary>
    public void UpdateCraftableRecipes(IReadOnlyList<CraftableRecipeDisplay> recipes)
    {
      _craftableRecipes.Clear();
      if (recipes != null)
      {
        for (int i = 0; i < recipes.Count; i++)
          if (recipes[i] != null && !string.IsNullOrWhiteSpace(recipes[i].OutputIdentifier))
            _craftableRecipes.Add(recipes[i]);
      }

      // 선택된 레시피가 더 이상 목록에 없으면 선택 해제.
      if (_selectedRecipeOutputId != null && FindRecipe(_selectedRecipeOutputId) == null)
        _selectedRecipeOutputId = null;

      RenderRecipeList();
      RenderRequirements();
    }

    private CraftableRecipeDisplay FindRecipe(string outputId)
    {
      if (string.IsNullOrEmpty(outputId)) return null;
      for (int i = 0; i < _craftableRecipes.Count; i++)
        if (string.Equals(_craftableRecipes[i].OutputIdentifier, outputId, StringComparison.Ordinal))
          return _craftableRecipes[i];
      return null;
    }

    private void RenderRecipeList()
    {
      if (_craftingRecipeList == null) return;

      _craftingRecipeList.Clear();

      if (_craftableRecipes.Count == 0)
      {
        var empty = new Label { text = "조합 가능한 아이템이 없습니다." };
        empty.AddToClassList("crafting-recipe-list__empty");
        _craftingRecipeList.Add(empty);
        return;
      }

      foreach (var recipe in _craftableRecipes)
      {
        string outputId = recipe.OutputIdentifier;

        var row = new VisualElement { name = $"Recipe_{outputId}" };
        row.AddToClassList("crafting-recipe");

        var icon = new Image { name = "RecipeIcon", pickingMode = PickingMode.Ignore };
        icon.AddToClassList("crafting-recipe__icon");
        var sprite = Registry.Registry.GetOrLoadIconSprite(outputId);
        if (sprite != null) icon.image = sprite.texture;
        row.Add(icon);

        var nameLabel = new Label
        {
          name = "RecipeName",
          pickingMode = PickingMode.Ignore,
          text = Registry.Registry.GetItemDisplayName(outputId)
        };
        nameLabel.AddToClassList("crafting-recipe__name");
        row.Add(nameLabel);

        var hint = new Label { name = "RecipeHint", pickingMode = PickingMode.Ignore };
        hint.AddToClassList("crafting-recipe__hint");
        row.Add(hint);

        bool selected = string.Equals(outputId, _selectedRecipeOutputId, StringComparison.Ordinal);
        row.EnableInClassList("crafting-recipe--selected", selected);
        hint.text = selected ? "다시 클릭하여 조합" : "선택";

        string captured = outputId;
        row.RegisterCallback<PointerDownEvent>(evt => HandleRecipeClicked(captured, evt));

        _craftingRecipeList.Add(row);
      }
    }

    private void HandleRecipeClicked(string outputId, PointerDownEvent evt)
    {
      evt.StopPropagation();

      var recipe = FindRecipe(outputId);
      if (recipe == null)
      {
        _selectedRecipeOutputId = null;
        RenderRecipeList();
        RenderRequirements();
        return;
      }

      // 이미 선택된 레시피를 다시 클릭 → 조합 실행.
      if (string.Equals(outputId, _selectedRecipeOutputId, StringComparison.Ordinal))
      {
        CraftSelectedRecipe(recipe, evt.position);
        return;
      }

      // 처음 선택.
      _selectedRecipeOutputId = outputId;
      RenderRecipeList();
      RenderRequirements();
    }

    private void CraftSelectedRecipe(CraftableRecipeDisplay recipe, UnityEngine.Vector2 pointerPosition)
    {
      if (_craftRequestHandler == null) return;

      // 커서에 이미 다른 아이템이 있으면 조합 불가(스택이 섞이는 것을 방지).
      if (_heldItem != null && _heldItem.ItemInstance != null &&
          !string.Equals(_heldItem.ItemInstance.CurrentIdentifier, recipe.OutputIdentifier, StringComparison.Ordinal))
        return;

      // 컨트롤러/플레이어에게 조합 실행을 요청한다.
      // 성공 시 생성된 결과 아이템 인스턴스를 반환한다(재료는 이미 인벤토리에서 소비됨).
      var crafted = _craftRequestHandler.Invoke(recipe);
      if (crafted == null || crafted.CurrentStackCount <= 0)
      {
        // 조합 실패(재료 부족 등) → 목록/필요아이템 갱신만 수행.
        RenderRecipeList();
        RenderRequirements();
        return;
      }

      // 결과 아이템을 커서로 pickup. 이미 같은 아이템을 들고 있으면 스택 누적.
      if (_heldItem == null || _heldItem.IsEmpty)
      {
        _heldItem = new InventorySlotModelDTO(crafted);
      }
      else if (_heldItem.ItemInstance != null && _heldItem.ItemInstance.CanStackWith(crafted))
      {
        var leftover = _heldItem.ItemInstance.Merge(crafted);
        // 스택 한도를 넘긴 잔량은 인벤토리로 돌려보낸다(유실 방지).
        if (leftover != null && leftover.CurrentStackCount > 0)
          CraftLeftoverReturned?.Invoke(leftover);
      }
      else
      {
        // 다른 아이템을 들고 있는 경우(위 가드로 도달하지 않지만 방어적으로) 인벤토리로 반환.
        CraftLeftoverReturned?.Invoke(crafted);
      }

      UpdateHeldItemGhostVisual(_heldItem);
      UpdateHeldItemGhostPosition(pointerPosition);
      HideTooltip();

      // 재료가 인벤토리(_boundSlots)에서 이미 소비되었으므로 슬롯 시각도 즉시 갱신.
      RefreshAllSlots();

      // 재료 소비/결과 생성으로 인벤토리가 변했으므로 알림 → 컨트롤러가 재료/목록을 다시 계산.
      NotifySlotsMutated();

      RenderRecipeList();
      RenderRequirements();
    }

    /// <summary>대상 요소가 조합 패널(또는 그 자식) 내부인지 검사한다.</summary>
    private bool IsWithinCraftingPanel(VisualElement target)
    {
      if (target == null || _craftingPanel == null) return false;
      var cur = target;
      while (cur != null)
      {
        if (cur == _craftingPanel) return true;
        cur = cur.parent;
      }
      return false;
    }

    /// <summary>패널이 닫힐 때 선택 상태를 초기화한다.</summary>
    private void ResetCraftingSelection()
    {
      if (_selectedRecipeOutputId == null) return;
      _selectedRecipeOutputId = null;
      RenderRecipeList();
      RenderRequirements();
    }

    private void RenderRequirements()
    {
      if (_craftingRequirements == null) return;

      _craftingRequirements.Clear();

      var recipe = FindRecipe(_selectedRecipeOutputId);
      if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
      {
        var empty = new Label { text = "조합할 아이템을 선택하세요." };
        empty.AddToClassList("crafting-requirements__empty");
        _craftingRequirements.Add(empty);
        return;
      }

      foreach (var ing in recipe.Ingredients)
      {
        var slot = new VisualElement();
        slot.AddToClassList("crafting-req-slot");

        var icon = new Image { name = "ReqIcon", pickingMode = PickingMode.Ignore };
        icon.AddToClassList("crafting-req-slot__icon");
        var sprite = Registry.Registry.GetOrLoadIconSprite(ing.Identifier);
        if (sprite != null) icon.image = sprite.texture;
        slot.Add(icon);

        int held = _heldCountResolver != null ? _heldCountResolver(ing.Identifier) : 0;
        bool satisfied = held >= ing.RequiredCount;

        var count = new Label
        {
          name = "ReqCount",
          pickingMode = PickingMode.Ignore,
          text = $"{held}/{ing.RequiredCount}"
        };
        count.AddToClassList("crafting-req-slot__count");
        slot.Add(count);

        slot.EnableInClassList("crafting-req-slot--satisfied", satisfied);

        _craftingRequirements.Add(slot);
      }
    }
  }
}
