using System.Collections;
using System.Collections.Generic;
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
      }

      if (remaining.CurrentStackCount > 0)
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

    public bool TryPickupWorldItem(ItemObject itemObject)
    {
      if (itemObject == null || itemObject.Item == null)
        return false;

      if (!string.IsNullOrWhiteSpace(itemObject.Identifier))
        return TryPickupWorldItem(itemObject.Identifier);

      bool added = TryAddItemToInventory(itemObject.Item);
      if (!added)
        return false;

      itemObject.Item.OnGet(this);
      RequestDestroyWorldItem(itemObject);
      return true;
    }

    public bool TryPickupWorldItem(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return false;

      var itemObject = Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier);
      if (itemObject == null || itemObject.Item == null)
        return false;

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
      return result;
    }
  }
}
