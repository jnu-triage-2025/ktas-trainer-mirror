using System.Collections;
using System.Collections.Generic;
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
    private bool _inventoryVisible;
    private bool _inventoryRenderRequired = true;  // like a dirty bit

    void Start_Inventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = UIControlRegistry.Get<InventoryUIController>();

      for (int i = 0; i < _inventoryConf.sizeWidth * _inventoryConf.sizeHeight; i++)
        _slots.Add(new InventorySlotModelDTO());
    }

    void Update_Inventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = UIControlRegistry.Get<InventoryUIController>();

      if (_inventoryRenderRequired && _inventoryUI != null && _inventoryUI.IsOpened)
      {
        _inventoryUI.UpdateInventory(_slots);
        _inventoryRenderRequired = false;
      }
    }

    private void ToggleInventory()
    {
      if (_inventoryUI == null)
        _inventoryUI = UIControlRegistry.Get<InventoryUIController>();

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

    public bool TryAddItemToInventory(ItemInstanceModelDTO item)
    {
      if (item == null || !item.IsValid()) return false;

      // Work on a copy to avoid mutating the source reference passed by callers
      ItemInstanceModelDTO remaining = new ItemInstanceModelDTO(item);

      // Pass 1: stack onto existing slots of the same item
      foreach (var slot in _slots)
      {
        if (slot.IsEmpty) continue;
        if (!slot.ItemInstance!.CanStackWith(remaining)) continue;

        var leftover = slot.Push(remaining);
        remaining = leftover ?? new ItemInstanceModelDTO { identifier = remaining.identifier, displayName = remaining.displayName, currCount = 0, maxCount = remaining.maxCount };
        if (remaining.currCount <= 0)
          return OnInventoryChangedAndReturn(true);
      }

      // Pass 2: place into the first empty slot
      foreach (var slot in _slots)
      {
        if (!slot.IsEmpty) continue;
        slot.SetItem(new ItemInstanceModelDTO(remaining));
        return OnInventoryChangedAndReturn(true);
      }

      // No room
      return false;
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
