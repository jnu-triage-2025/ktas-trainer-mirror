using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("PlayerController.Inventory:")]
    private InventoryUIController _inventoryUI;

    [SerializeField] private PlayerControllerInventoryConfiguration _inventoryConf = new PlayerControllerInventoryConfiguration
    {
      sizeWidth = 9,
      sizeHeight = 4
    };
    [SerializeField] private List<InventorySlotModelDTO> _slots = new();
    public ItemSystem.Item HandlingItem = null;
    
    private bool _inventoryVisible;
    private bool _inventoryRenderRequired = true;  // like a dirty bit
    private bool _isCombining = false;             // 자동 조합 재진입 방지 플래그

    void Start_Inventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = Registry.Registry.Get<InventoryUIController>(RegistryType.UI, Registry.Registry.TypeKey<InventoryUIController>());

      // 멱등성 보장: Start 와 OnStartClient 양쪽에서 호출될 수 있으므로
      // 목표 슬롯 수까지만 채운다(중복 호출 시 슬롯이 2배로 늘어나는 것 방지).
      int targetCount = _inventoryConf.sizeWidth * _inventoryConf.sizeHeight;
      while (_slots.Count < targetCount)
        _slots.Add(new InventorySlotModelDTO());
    }

    void Update_Inventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = Registry.Registry.Get<InventoryUIController>(RegistryType.UI, Registry.Registry.TypeKey<InventoryUIController>());

      if (_inventoryRenderRequired && _inventoryUI != null && _inventoryUI.IsOpened)
      {
        _inventoryUI.UpdateInventory(_slots);
        _inventoryRenderRequired = false;
      }
    }

    private void ToggleInventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = Registry.Registry.Get<InventoryUIController>(RegistryType.UI, Registry.Registry.TypeKey<InventoryUIController>());

      if (_inventoryUI == null)
        return;

      _inventoryVisible = !_inventoryVisible;
      _inventoryUI.ToggleRoot(_inventoryVisible);

      if (_inventoryVisible && _inventoryRenderRequired)
      {
        _inventoryUI.UpdateInventory(_slots);
        _inventoryRenderRequired = false;
      }
    }

    public bool TryAddItemToInventory(ItemSystem.Item item)
    {
      return TryAddItemToInventory(item, out _);
    }

    public bool TryAddItemToInventory(ItemSystem.Item item, out ItemSystem.Item leftover)
    {
      leftover = null;
      if (item == null || item.CurrentStackCount <= 0)
        return false;

      ItemSystem.Item remaining = item.Clone();
      bool changed = false;

      foreach (var slot in _slots)
      {
        if (remaining.CurrentStackCount <= 0) break;
        if (slot.IsEmpty || slot.ItemInstance == null) continue;
        if (!slot.ItemInstance.CanStackWith(remaining)) continue;

        int room = slot.ItemInstance.CurrentMaxStackCount - slot.ItemInstance.CurrentStackCount;
        if (room <= 0) continue;

        int moved = Mathf.Min(room, remaining.CurrentStackCount);
        slot.ItemInstance.CurrentStackCount += moved;
        remaining.CurrentStackCount -= moved;
        changed = true;
      }

      foreach (var slot in _slots)
      {
        if (remaining.CurrentStackCount <= 0)
          break;

        if (!slot.IsEmpty)
          continue;

        int moved = Mathf.Min(remaining.CurrentMaxStackCount, remaining.CurrentStackCount);
        var placed = remaining.Clone();
        placed.CurrentStackCount = moved;

        slot.SetItem(placed);
        remaining.CurrentStackCount -= moved;
        changed = true;
      }

      if (changed)
      {
        OnInventoryChangedAndReturn(true);
        item.OnGet(this);
        // 월드 습득 등 이 경로로 획득한 아이템은 획득 훅이 발행되었으므로 지연 획득 상태를 해제한다.
        item.DeferredOnGet = false;
        // 조합은 이제 인벤토리 UI 의 조합 패널을 통해 수동으로 수행한다(자동 조합 비활성).
        // 기존 자동 조합 로직은 GetCraftableRecipes / TryCraftRecipe 로 대체되었다.
        // if (!_isCombining)
        //   TryAutoCombineItems();
      }

      if (remaining.CurrentStackCount > 0)
      {
        leftover = remaining;
        return false;
      }

      return true;
    }

    /// <summary>
    /// 인벤토리에 등록된 조합 레시피가 충족되는지 검사하고, 충족되면 자동으로 조합을 수행합니다.
    /// 조합이 발생할 때마다 재검사하여 연속 조합도 지원합니다.
    /// _isCombining 플래그로 TryAddItemToInventory 내부로부터의 재진입을 방지합니다.
    /// </summary>
    private void TryAutoCombineItems()
    {
      if (_isCombining) return;
      _isCombining = true;

      try
      {
        bool combined = true;
        while (combined)
        {
          combined = false;

          // 현재 인벤토리 아이템 수량 맵 수집
          var counts = new Dictionary<string, int>(StringComparer.Ordinal);
          foreach (var slot in _slots)
          {
            if (slot == null || slot.IsEmpty || slot.ItemInstance == null) continue;
            string id = slot.ItemInstance.CurrentIdentifier;
            if (string.IsNullOrWhiteSpace(id)) continue;
            counts.TryGetValue(id, out int existing);
            counts[id] = existing + slot.ItemInstance.CurrentStackCount;
          }

          if (!ItemCombineRecipeRegistry.TryGetMatchingRecipe(counts, out var recipe))
            break;

          // 재료 소비
          foreach (var ingredient in recipe.Ingredients)
            RemoveItemFromInventory(ingredient.Identifier, ingredient.RequiredCount);

          // 결과 아이템 생성 및 추가
          var outputItem = Registry.Registry.CreateItemInstance(recipe.OutputItemIdentifier);
          if (outputItem == null)
          {
            Debug.LogWarning($"[PlayerController] AutoCombine: 결과 아이템 '{recipe.OutputItemIdentifier}' 생성 실패. Registry에 등록되지 않은 Identifier일 수 있습니다.");
            break;
          }

          outputItem.CurrentStackCount = recipe.OutputItemCount;
          // _isCombining = true 상태이므로 TryAddItemToInventory 내부에서 TryAutoCombineItems가 재진입하지 않는다.
          TryAddItemToInventory(outputItem);
          combined = true;

#if UNITY_EDITOR
          Debug.Log($"[PlayerController] AutoCombine: [{string.Join(", ", recipe.Ingredients.Select(i => $"{i.Identifier}×{i.RequiredCount}"))}] → {recipe.OutputItemIdentifier}×{recipe.OutputItemCount}");
#endif
        }
      }
      finally
      {
        _isCombining = false;
      }
    }

    // =========================================================================
    // 수동 조합(조합 패널) 지원 API
    // =========================================================================

    /// <summary>
    /// 현재 인벤토리 보유량 기준으로 조합 가능한 레시피 목록을 조합 패널용 DTO로 반환한다.
    /// (등록된 모든 레시피 중, 재료가 충분한 레시피만 포함)
    /// </summary>
    public List<InventoryUIView.CraftableRecipeDisplay> GetCraftableRecipes()
    {
      var result = new List<InventoryUIView.CraftableRecipeDisplay>();

      var counts = BuildInventoryCountMap();
      var recipes = ItemCombineRecipeRegistry.GetAll();

      for (int i = 0; i < recipes.Count; i++)
      {
        var recipe = recipes[i];
        if (recipe == null || string.IsNullOrWhiteSpace(recipe.OutputItemIdentifier))
          continue;
        if (!ItemCombineRecipeRegistry.RecipeCanCombine(recipe, counts))
          continue;

        var ingredients = new List<InventoryUIView.CraftableRecipeDisplay.Ingredient>(recipe.Ingredients.Count);
        foreach (var ing in recipe.Ingredients)
          ingredients.Add(new InventoryUIView.CraftableRecipeDisplay.Ingredient(ing.Identifier, ing.RequiredCount));

        result.Add(new InventoryUIView.CraftableRecipeDisplay
        {
          OutputIdentifier = recipe.OutputItemIdentifier,
          Ingredients = ingredients
        });
      }

      return result;
    }

    /// <summary>
    /// 지정한 결과 식별자의 레시피를 1회 조합한다.
    /// 재료가 충분하면 인벤토리에서 재료를 소비하고, 생성된 결과 아이템 인스턴스를 반환한다.
    /// (결과 아이템은 인벤토리에 추가하지 않는다 — 호출자(조합 패널)가 커서로 pickup 처리)
    /// 재료가 부족하거나 결과 생성에 실패하면 null 을 반환한다.
    ///
    /// 조합 결과물은 커서(held item)로 지급된 뒤 인벤토리 슬롯에 배치되는데, 이 배치 경로
    /// (InventoryUIView 의 slot.SetItem/Push)는 <see cref="TryAddItemToInventory"/> 를 우회하므로
    /// 획득 훅(<see cref="ItemSystem.Item.OnGet"/>)이 생략된다. 이를 보완하려고 조합 "시점"에 OnGet 을
    /// 호출하면 아이템이 아직 인벤토리에 없는 상태에서 획득 신호가 먼저 발행되는 문제가 있다.
    /// 따라서 여기서는 결과 아이템에 <see cref="ItemSystem.Item.DeferredOnGet"/> 플래그만 설정하고,
    /// 실제 인벤토리 진입 시점(InventoryUIController 가 슬롯 배치 감지)에 OnGet 이 발행되도록 한다.
    /// MedicalItem 은 OnGet 에서 시나리오 게이팅/퀘스트 신호(sig.&lt;id&gt;, sig.click_&lt;id&gt;)를 발행한다.
    /// </summary>
    public ItemSystem.Item TryCraftRecipe(string outputItemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(outputItemIdentifier))
        return null;

      var counts = BuildInventoryCountMap();
      var recipes = ItemCombineRecipeRegistry.GetAll();

      ItemCombineRecipe matched = null;
      for (int i = 0; i < recipes.Count; i++)
      {
        var recipe = recipes[i];
        if (recipe == null) continue;
        if (!string.Equals(recipe.OutputItemIdentifier, outputItemIdentifier, StringComparison.Ordinal)) continue;
        if (!ItemCombineRecipeRegistry.RecipeCanCombine(recipe, counts)) continue;
        matched = recipe;
        break;
      }

      if (matched == null)
        return null;

      // 재료 소비.
      foreach (var ingredient in matched.Ingredients)
      {
        int removed = RemoveItemFromInventory(ingredient.Identifier, ingredient.RequiredCount);
        if (removed < ingredient.RequiredCount)
        {
          // 이론상 도달하지 않지만(사전 검사 통과), 방어적으로 로그만 남긴다.
          Debug.LogWarning($"[PlayerController] TryCraftRecipe: 재료 '{ingredient.Identifier}' 소비 부족({removed}/{ingredient.RequiredCount}).");
        }
      }

      var outputItem = Registry.Registry.CreateItemInstance(matched.OutputItemIdentifier);
      if (outputItem == null)
      {
        Debug.LogWarning($"[PlayerController] TryCraftRecipe: 결과 아이템 '{matched.OutputItemIdentifier}' 생성 실패. Registry 미등록 Identifier 일 수 있습니다.");
        return null;
      }

      outputItem.CurrentStackCount = matched.OutputItemCount;

      // 조합 결과물은 커서로 지급되어 인벤토리 배치 시 TryAddItemToInventory 를 우회한다.
      // 실제 인벤토리 진입 시점에 획득 훅(OnGet)이 발행되도록 지연 획득 플래그를 설정한다(위 XML 주석 참조).
      outputItem.DeferredOnGet = true;

      return outputItem;
    }

    /// <summary>현재 인벤토리의 아이템 수량 맵(Identifier → count)을 만든다.</summary>
    private Dictionary<string, int> BuildInventoryCountMap()
    {
      var counts = new Dictionary<string, int>(StringComparer.Ordinal);
      foreach (var slot in _slots)
      {
        if (slot == null || slot.IsEmpty || slot.ItemInstance == null) continue;
        string id = slot.ItemInstance.CurrentIdentifier;
        if (string.IsNullOrWhiteSpace(id)) continue;
        counts.TryGetValue(id, out int existing);
        counts[id] = existing + slot.ItemInstance.CurrentStackCount;
      }
      return counts;
    }

    public int ClearInventory()
    {
      int removed = 0;

      foreach (var slot in _slots)
      {
        if (slot == null || slot.IsEmpty || slot.ItemInstance == null)
          continue;

        removed += Mathf.Max(0, slot.ItemInstance.CurrentStackCount);
        slot.Clear();
      }

      if (removed > 0)
        OnInventoryChangedAndReturn(true);

      return removed;
    }

    public int RemoveItemFromInventory(string itemIdentifier, int count)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier) || count <= 0)
        return 0;

      int removed = 0;
      int remainToRemove = count;

      foreach (var slot in _slots)
      {
        if (remainToRemove <= 0)
          break;

        if (slot == null || slot.IsEmpty || slot.ItemInstance == null)
          continue;

        if (!string.Equals(slot.ItemInstance.CurrentIdentifier, itemIdentifier, System.StringComparison.Ordinal))
          continue;

        int take = Mathf.Min(remainToRemove, slot.ItemInstance.CurrentStackCount);
        slot.ItemInstance.CurrentStackCount -= take;
        removed += take;
        remainToRemove -= take;

        if (slot.ItemInstance.CurrentStackCount <= 0)
          slot.Clear();
      }

      if (removed > 0)
        OnInventoryChangedAndReturn(true);

      return removed;
    }

    public int RemoveAllOfItemFromInventory(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return 0;

      int removed = 0;
      foreach (var slot in _slots)
      {
        if (slot == null || slot.IsEmpty || slot.ItemInstance == null)
          continue;

        if (!string.Equals(slot.ItemInstance.CurrentIdentifier, itemIdentifier, System.StringComparison.Ordinal))
          continue;

        removed += Mathf.Max(0, slot.ItemInstance.CurrentStackCount);
        slot.Clear();
      }

      if (removed > 0)
        OnInventoryChangedAndReturn(true);

      return removed;
    }

    public int CountItemInInventory(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return 0;

      int total = 0;
      foreach (var slot in _slots)
      {
        if (slot == null || slot.IsEmpty || slot.ItemInstance == null)
          continue;

        if (!string.Equals(slot.ItemInstance.CurrentIdentifier, itemIdentifier, System.StringComparison.Ordinal))
          continue;

        total += Mathf.Max(0, slot.ItemInstance.CurrentStackCount);
      }

      return total;
    }

    public bool TryDropItemInFront(ItemSystem.Item itemData)
    {
      if (itemData == null || itemData.CurrentStackCount <= 0)
        return false;

      Vector3 forward = transform.forward.sqrMagnitude > 0.0001f ? transform.forward.normalized : Vector3.forward;
      Vector3 spawnPosition = transform.position + forward * 1.25f + Vector3.up * 0.35f;

      bool dropped = RequestDropWorldItem(itemData, spawnPosition, forward * 2.75f);
      if (!dropped)
        Debug.LogWarning($"[PlayerController] TryDropItemInFront failed: could not spawn ItemObject");
      else
        itemData.OnThrow(this);
      return dropped;
    }

    /// <summary>
    /// 설치형 엔티티 등을 아이템으로 되돌릴 때, 지정 위치에 획득 가능한 월드 아이템을 생성한다.
    /// </summary>
    public bool TrySpawnWorldItem(ItemSystem.Item itemData, Vector3 position, Vector3 throwForce)
    {
      if (itemData == null || itemData.CurrentStackCount <= 0)
        return false;

      bool spawned = RequestDropWorldItem(itemData, position, throwForce);
      if (spawned)
        itemData.OnThrow(this);
      return spawned;
    }

    public bool TryPickupWorldItem(ItemObject itemObject)
    {
      if (itemObject == null || itemObject.Item == null)
        return false;

      if (!string.IsNullOrWhiteSpace(itemObject.Identifier))
        return TryPickupWorldItem(itemObject.Identifier);

      // 부분 추가로 인한 아이템 유실/복제를 막기 위해 전량 수용 가능할 때만 추가한다.
      if (!CanAcceptItem(itemObject.Item))
        return false;

      bool added = TryAddItemToInventory(itemObject.Item);
      if (!added)
        return false;

      // OnGet 은 TryAddItemToInventory 내부에서 1회 호출된다(중복 호출 금지).
      RequestDestroyWorldItem(itemObject);
      return true;
    }

    public bool TryPickupWorldItem(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return false;

      entityIdentifier = entityIdentifier.Trim();
      var itemObject = Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier);
      if (itemObject == null || itemObject.Item == null)
      {
        Debug.LogWarning($"[PlayerController] TryPickupWorldItem failed: entity '{entityIdentifier}' not found or invalid.");
        return false;
      }

      if (!CanAcceptItem(itemObject.Item))
        return false;

      RequestPickupWorldItem(entityIdentifier);
      return true;
    }

    public bool CanAcceptItem(ItemSystem.Item item)
    {
      if (item == null || item.CurrentStackCount <= 0)
        return false;

      int remainingCount = item.CurrentStackCount;

      foreach (var slot in _slots)
      {
        if (remainingCount <= 0) break;
        if (slot == null || slot.IsEmpty || slot.ItemInstance == null) continue;
        if (!slot.ItemInstance.CanStackWith(item)) continue;

        int room = slot.ItemInstance.CurrentMaxStackCount - slot.ItemInstance.CurrentStackCount;
        if (room <= 0) continue;

        remainingCount -= Mathf.Min(room, remainingCount);
      }

      foreach (var slot in _slots)
      {
        if (remainingCount <= 0)
          break;

        if (slot == null || !slot.IsEmpty)
          continue;

        remainingCount -= Mathf.Min(item.CurrentMaxStackCount, remainingCount);
      }

      return remainingCount <= 0;
    }

    private bool OnInventoryChangedAndReturn(bool result)
    {
      _inventoryRenderRequired = true;

      // Keep the hotbar visuals in sync with inventory mutations
      _hotbarUI?.BindInventory(_slots);

      // 데이터(_slots)를 코드로 직접 변경(예: /give, 아이템 획득/제거/조합)한 경우에는
      // HotbarUIController.OnSelectedSlotChanged / InventoryUIController.OnItemAtSelectedSlotChanged
      // 이벤트가 발화되지 않으므로 ResolveHandledItem 이 트리거되지 않는다. 그 결과 현재 선택된
      // hold 슬롯의 ItemInstance 가 교체/충전되어도 HandlingItem 이 갱신되지 않아 뷰모델/아이템
      // 소지 조건부 상호작용이 반영되지 않는 버그가 있었다. 여기서 직접 재해석해 갱신을 보장한다.
      // (ResolveHandledItem 은 HandlingItem 이 실제로 바뀐 경우에만 힌트를 재평가한다.)
      ResolveHandledItem();

      // HandlingItem 이 바뀌지 않았더라도 다른 슬롯의 보유 수량 변화 등으로 상호작용 노출 조건이
      // 달라질 수 있으므로(예: CountItemInInventory 기반 조건), 힌트를 항상 재평가한다.
      RefreshInteractableHintsNow();
      return result;
    }
  }
}
