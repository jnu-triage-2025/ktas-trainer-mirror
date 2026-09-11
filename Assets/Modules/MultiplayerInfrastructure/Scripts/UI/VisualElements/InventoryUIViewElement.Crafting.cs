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
  ///   (a) 하단 : 현재 보유한 재료와 관련된 결과 아이템 목록
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
    private ScrollView _craftingRecipeScroll;
    private VisualElement _craftingRecipeList;

    /// <summary>조합 목록을 격자로 배치할 때 한 줄에 놓는 슬롯 개수.</summary>
    private const int CraftingRecipeColumns = 4;

    // ── 조합 가능 목록의 표시 줄 수 ─────────────────────────────────
    // 목록은 2.8줄까지만 보여주고 그 이상은 스크롤한다. 마지막 줄이 일부만 보이므로
    // 아래에 더 있다는 것이 드러난다. 아래 값들은 InventoryUI.uss 의
    // .crafting-recipe-slot / .crafting-recipe-list / .crafting-recipe-scroll 정의와 일치해야 한다.
    /// <summary>한 번에 보여줄 줄 수. 소수부는 다음 줄을 일부만 노출하기 위한 것이다.</summary>
    private const float CraftingRecipeVisibleRows = 2.8f;
    /// <summary>.crafting-recipe-slot 의 height.</summary>
    private const float CraftingRecipeSlotHeight = 42f;
    /// <summary>.crafting-recipe-list 의 gap(줄 간격).</summary>
    private const float CraftingRecipeRowGap = 5f;
    /// <summary>.crafting-recipe-list 의 padding-top + .crafting-recipe-scroll 의 상하 border.</summary>
    private const float CraftingRecipeScrollChrome = 8f;

    // ── "필요 아이템" 영역 높이 예약 ────────────────────────────────
    // 레시피를 선택하면 필요 아이템 칸이 생기면서 이 영역이 세로로 늘어나고,
    // 그만큼 조합 패널(=인벤토리 UI 전체) 높이가 함께 늘어난다.
    // 이를 막기 위해 선택 전(idle)에도 "재료 칸 한 줄" 높이를 그대로 확보해 둔다.
    // 실제 높이는 자리만 차지하는 빈 칸(placeholder)이 결정하므로 선택 전후가 항상 같고,
    // 아래 값은 그 높이가 무너지지 않도록 하는 하한(min-height)이다.
    // (InventoryUI.uss 의 .crafting-req-slot height 48px + .crafting-requirements padding 5px·border 1px)
    private const float CraftingRequirementSlotHeight = 48f;
    private const float CraftingRequirementBoxPadding = 12f;

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
      ApplyRequirementsReservedHeight();
      _craftingPanel.Add(_craftingRequirements);

      // 구분선
      var divider = new VisualElement { name = "CraftingDivider" };
      divider.AddToClassList("crafting-divider");
      _craftingPanel.Add(divider);

      // (a) 보유한 재료와 관련된 조합 아이템 목록 섹션
      var listLabel = new Label { text = "조합 아이템", name = "CraftingListLabel" };
      listLabel.AddToClassList("crafting-section-label");
      _craftingPanel.Add(listLabel);

      // 조합 가능 목록은 가변적이므로 세로 스크롤 가능한 ScrollView 로 감싼다.
      // 목록이 넘칠 때만 스크롤바가 나타나도록 Auto 모드를 사용한다.
      _craftingRecipeScroll = new ScrollView(ScrollViewMode.Vertical)
      {
        name = "CraftingRecipeScroll"
      };
      _craftingRecipeScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
      _craftingRecipeScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
      _craftingRecipeScroll.AddToClassList("crafting-recipe-scroll");
      ApplyRecipeScrollVisibleRows();
      _craftingPanel.Add(_craftingRecipeScroll);

      // 실제 슬롯이 배치되는 격자 컨테이너.
      _craftingRecipeList = new VisualElement { name = "CraftingRecipeList" };
      _craftingRecipeList.AddToClassList("crafting-recipe-list");
      _craftingRecipeScroll.Add(_craftingRecipeList);

      // 좌측 인벤토리 패널(InventoryPanel) 다음에 추가하여 우측에 배치한다
      // (.inventory-root: flex-direction: row). BuildCraftingPanel 은 ghost/tooltip 생성보다
      // 먼저 호출되므로, Add 순서상 ghost/tooltip 이 조합 패널보다 나중에 추가되어 위에 그려진다.
      Add(_craftingPanel);

      // 조합 패널 높이를 좌측 인벤토리 패널(소유 아이템 영역) 높이에 고정한다.
      // 조합 가능 아이템 수와 무관하게 UI 전체 높이가 인벤토리 기준으로 유지되도록,
      // 인벤토리 패널의 레이아웃이 바뀔 때마다 조합 패널 높이를 동기화한다.
      if (_inventoryPanel != null)
      {
        _inventoryPanel.RegisterCallback<GeometryChangedEvent>(_ => SyncCraftingPanelHeightToInventory());
        SyncCraftingPanelHeightToInventory();
      }

      RenderRequirements();
      RenderRecipeList();
    }

    /// <summary>
    /// 조합 패널 높이를 좌측 인벤토리 패널 높이에 맞춘다.
    /// 이렇게 하면 UI 전체 높이는 인벤토리(소유 아이템) 영역 기준으로 고정되고,
    /// 조합 가능 아이템 수가 늘어나도 패널 높이가 늘어나지 않는다(대신 목록이 스크롤됨).
    /// </summary>
    private void SyncCraftingPanelHeightToInventory()
    {
      // _inventoryPanel 이 루트(this) 로 폴백된 경우(UXML 없이 코드로 생성) 순환 참조가 되므로 건너뛴다.
      if (_craftingPanel == null || _inventoryPanel == null || _inventoryPanel == this)
        return;

      float h = _inventoryPanel.resolvedStyle.height;
      if (h <= 0f || float.IsNaN(h))
        return;

      _craftingPanel.style.height = h;
    }

    /// <summary>
    /// 보유한 재료와 관련된 레시피 목록을 갱신한다. 컨트롤러가 인벤토리 변화 시마다 호출한다.
    /// 선택 상태는 가능하면 유지하되, 관련 재료가 하나도 남지 않은 레시피면 선택을 해제한다.
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
      if (string.IsNullOrEmpty(outputId))
        return null;
      for (int i = 0; i < _craftableRecipes.Count; i++)
        if (string.Equals(_craftableRecipes[i].OutputIdentifier, outputId, StringComparison.Ordinal))
          return _craftableRecipes[i];
      return null;
    }

    private void RenderRecipeList()
    {
      if (_craftingRecipeList == null)
        return;

      _craftingRecipeList.Clear();

      if (_craftableRecipes.Count == 0)
      {
        var empty = new Label { text = "보유한 재료로 확인할 수 있는 조합 아이템이 없습니다." };
        empty.AddToClassList("crafting-recipe-list__empty");
        _craftingRecipeList.Add(empty);
        return;
      }

      // 인벤토리 슬롯과 동일하게 격자(row 단위)로 배치한다.
      // 이름은 슬롯 옆에 붙이지 않고, 마우스오버 시 툴팁으로 이름/설명을 표시한다.
      VisualElement currentRow = null;
      for (int i = 0; i < _craftableRecipes.Count; i++)
      {
        if (i % CraftingRecipeColumns == 0)
        {
          currentRow = new VisualElement { name = $"RecipeRow_{i / CraftingRecipeColumns}" };
          currentRow.AddToClassList("crafting-recipe-row");
          _craftingRecipeList.Add(currentRow);
        }

        var recipe = _craftableRecipes[i];
        string outputId = recipe.OutputIdentifier;

        var slot = new VisualElement { name = $"Recipe_{outputId}" };
        slot.AddToClassList("crafting-recipe-slot");

        var icon = new Image { name = "RecipeIcon", pickingMode = PickingMode.Ignore };
        icon.AddToClassList("crafting-recipe-slot__icon");
        var sprite = Registry.Registry.GetOrLoadIconSprite(outputId);
        if (sprite != null)
          icon.image = sprite.texture;
        slot.Add(icon);

        bool selected = string.Equals(outputId, _selectedRecipeOutputId, StringComparison.Ordinal);
        slot.EnableInClassList("crafting-recipe-slot--selected", selected);

        string captured = outputId;
        slot.RegisterCallback<PointerDownEvent>(evt => HandleRecipeClicked(captured, evt));
        slot.RegisterCallback<PointerEnterEvent>(evt => ShowTooltipForCraftingItem(captured, evt.position));
        slot.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());

        currentRow.Add(slot);
      }

      // 마지막 줄이 덜 찼으면 빈 칸으로 채운다. 줄이 항상 같은 개수의 칸을 가지므로
      // 좌우 끝에 맞춰 분배되는 간격(.crafting-recipe-row: space-between)이 모든 줄에서 같아진다.
      int remainder = _craftableRecipes.Count % CraftingRecipeColumns;
      if (remainder != 0 && currentRow != null)
      {
        for (int i = remainder; i < CraftingRecipeColumns; i++)
        {
          var filler = new VisualElement { pickingMode = PickingMode.Ignore };
          filler.AddToClassList("crafting-recipe-slot");
          filler.AddToClassList("crafting-recipe-slot--placeholder");
          currentRow.Add(filler);
        }
      }
    }

    /// <summary>
    /// 조합 결과 또는 필요 아이템 슬롯 hover 시, 임시 인스턴스를 만들어 인벤토리 슬롯과 동일한 툴팁을 표시한다.
    /// 조합 정보는 인스턴스가 아닌 identifier 만 가지므로 스택 수량은 표시하지 않는다.
    /// </summary>
    private void ShowTooltipForCraftingItem(string itemIdentifier, UnityEngine.Vector2 panelPosition)
    {
      // 손에 아이템을 들고 있는 동안에는 ghost 가 우선이므로 툴팁을 표시하지 않는다.
      if (_heldItem != null)
      {
        HideTooltip();
        return;
      }

      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
      {
        HideTooltip();
        return;
      }

      ShowTooltipForItem(item, panelPosition, showStack: false);
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
      if (_craftRequestHandler == null)
        return;

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
      if (target == null || _craftingPanel == null)
        return false;
      var cur = target;
      while (cur != null)
      {
        if (cur == _craftingPanel)
          return true;
        cur = cur.parent;
      }
      return false;
    }

    /// <summary>패널이 닫힐 때 선택 상태를 초기화한다.</summary>
    private void ResetCraftingSelection()
    {
      if (_selectedRecipeOutputId == null)
        return;
      _selectedRecipeOutputId = null;
      RenderRecipeList();
      RenderRequirements();
    }

    /// <summary>
    /// "필요 아이템" 영역 높이의 하한을 재료 칸 한 줄 기준으로 확보한다.
    /// 선택 전에는 <see cref="RenderRequirements"/> 가 넣는 빈 칸이 같은 높이를 차지하므로,
    /// 레시피를 선택해도 조합 패널과 인벤토리 UI 전체가 세로로 늘어나지 않는다.
    /// </summary>
    private void ApplyRequirementsReservedHeight()
    {
      if (_craftingRequirements == null)
        return;

      _craftingRequirements.style.minHeight =
        CraftingRequirementSlotHeight + CraftingRequirementBoxPadding;
    }

    /// <summary>
    /// 조합 가능 목록의 표시 높이를 <see cref="CraftingRecipeVisibleRows"/> 줄로 제한한다.
    /// 목록이 더 길면 스크롤되며, 마지막 줄이 일부만 보여 스크롤 가능함이 드러난다.
    /// </summary>
    private void ApplyRecipeScrollVisibleRows()
    {
      if (_craftingRecipeScroll == null)
        return;

      _craftingRecipeScroll.style.maxHeight =
        CraftingRecipeVisibleRows * CraftingRecipeSlotHeight +
        (CraftingRecipeVisibleRows - 1f) * CraftingRecipeRowGap +
        CraftingRecipeScrollChrome;
    }

    private void RenderRequirements()
    {
      if (_craftingRequirements == null)
        return;

      _craftingRequirements.Clear();

      var recipe = FindRecipe(_selectedRecipeOutputId);
      if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
      {
        // 선택 전에도 재료 칸 한 줄만큼의 높이를 그대로 차지하도록 빈 칸을 넣는다.
        // 안내 문구는 절대 배치라 높이에 관여하지 않으므로, 선택 전후의 높이가 항상 같다.
        var placeholder = new VisualElement { name = "ReqPlaceholder", pickingMode = PickingMode.Ignore };
        placeholder.AddToClassList("crafting-req-slot");
        placeholder.AddToClassList("crafting-req-slot--placeholder");
        _craftingRequirements.Add(placeholder);

        var empty = new Label { text = "조합할 아이템을 선택하세요.", pickingMode = PickingMode.Ignore };
        empty.AddToClassList("crafting-requirements__empty");
        _craftingRequirements.Add(empty);
        return;
      }

      foreach (var ing in recipe.Ingredients)
      {
        var slot = new VisualElement();
        slot.AddToClassList("crafting-req-slot");

        string ingredientIdentifier = ing.Identifier;
        slot.RegisterCallback<PointerEnterEvent>(evt =>
          ShowTooltipForCraftingItem(ingredientIdentifier, evt.position));
        slot.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());

        var icon = new Image { name = "ReqIcon", pickingMode = PickingMode.Ignore };
        icon.AddToClassList("crafting-req-slot__icon");
        var sprite = Registry.Registry.GetOrLoadIconSprite(ing.Identifier);
        if (sprite != null)
          icon.image = sprite.texture;
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
