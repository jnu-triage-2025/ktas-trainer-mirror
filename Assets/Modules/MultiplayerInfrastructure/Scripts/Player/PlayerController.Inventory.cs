using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.Item;
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
    public ItemData HandlingItem = null;
    
    private bool _inventoryVisible;
    private bool _inventoryRenderRequired = true;  // like a dirty bit

    void Start_Inventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = Registry.Registry.Get<InventoryUIController>(RegistryType.UI, Registry.Registry.TypeKey<InventoryUIController>());

      for (int i = 0; i < _inventoryConf.sizeWidth * _inventoryConf.sizeHeight; i++)
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

    public bool TryAddItemToInventory(ItemData item)
    {
      if (item == null || !item.IsValid()) return false;

      // Work on a copy to avoid mutating the source reference passed by callers
      ItemData remaining = new ItemData(item);

      // Pass 1: stack onto existing slots of the same item
      foreach (var slot in _slots)
      {
        if (slot.IsEmpty) continue;
        if (!slot.ItemInstance!.CanStackWith(remaining)) continue;

        var leftover = slot.Push(remaining);
        remaining = leftover ?? new ItemData { identifier = remaining.identifier, displayName = remaining.displayName, currCount = 0, maxCount = remaining.maxCount };
        if (remaining.currCount <= 0)
          return OnInventoryChangedAndReturn(true);
      }

      // Pass 2: place into the first empty slot
      foreach (var slot in _slots)
      {
        if (!slot.IsEmpty) continue;
        slot.SetItem(new ItemData(remaining));
        return OnInventoryChangedAndReturn(true);
      }

<<<<<<< Updated upstream
      // No room
      return false;
=======
      if (changed)
      {
        OnInventoryChangedAndReturn(true);
        item.OnGet(this);
      }

      if (remaining.currCount > 0)
      {
        leftover = remaining;
        return false;
      }

      return true;
    }

    public int ClearInventory()
    {
      int removed = 0;

      foreach (var slot in _slots)
      {
        if (slot == null || slot.IsEmpty || slot.ItemInstance == null)
          continue;

        removed += Mathf.Max(0, slot.ItemInstance.currCount);
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

        if (!string.Equals(slot.ItemInstance.identifier, itemIdentifier, System.StringComparison.Ordinal))
          continue;

        int take = Mathf.Min(remainToRemove, slot.ItemInstance.currCount);
        slot.ItemInstance.currCount -= take;
        removed += take;
        remainToRemove -= take;

        if (slot.ItemInstance.currCount <= 0)
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

        if (!string.Equals(slot.ItemInstance.identifier, itemIdentifier, System.StringComparison.Ordinal))
          continue;

        removed += Mathf.Max(0, slot.ItemInstance.currCount);
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

        if (!string.Equals(slot.ItemInstance.identifier, itemIdentifier, System.StringComparison.Ordinal))
          continue;

        total += Mathf.Max(0, slot.ItemInstance.currCount);
      }

      return total;
    }

    public bool TryDropItemInFront(ItemData itemData)
    {
      if (itemData == null || !itemData.IsValid() || itemData.currCount <= 0)
        return false;

      Vector3 forward = transform.forward.sqrMagnitude > 0.0001f ? transform.forward.normalized : Vector3.forward;
      Vector3 spawnPosition = transform.position + forward * 1.25f + Vector3.up * 0.35f;

      bool dropped = ItemSpawnUtility.TrySpawnDroppedItem(itemData, spawnPosition, forward, out _);
      if (dropped)
        itemData.OnDrop(this);
      return dropped;
>>>>>>> Stashed changes
    }

    private bool OnInventoryChangedAndReturn(bool result)
    {
      _inventoryRenderRequired = true;

      // Keep the hotbar visuals in sync with inventory mutations
      _hotbarUI?.BindInventory(_slots);
      return result;
    }
  }
}
