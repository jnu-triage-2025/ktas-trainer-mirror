using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.ItemSystem;
using Item = MultiplayerInfrastructure.ItemSystem.Item;
using UnityEngine;

class PlayerInventory : NetworkBehaviour
{
  [Header("Inventory")]
  public int inventorySize = DefaultsPlayerInventory.DefaultInventorySize;
  public List<InventorySlotModelDTO> inventorySlots = new List<InventorySlotModelDTO>();

  void Awake()
  {
    inventorySlots.Clear();
    for (int i = 0; i < inventorySize; i++)
    {
      inventorySlots.Add(new InventorySlotModelDTO());
    }
  }

  public override void OnStartClient()
  {
    base.OnStartClient();

    if (!IsOwner)
    {
      enabled = false;
      return;
    }
  }

  public Item Push(Item itemInstance)
  {
    for (int i = 0; i < inventorySlots.Count; i++)
    {
      var slot = inventorySlots[i];
      if (slot.IsEmpty || slot.ItemInstance!.CanStackWith(itemInstance))
      {
        var leftover = slot.Push(new InventorySlotModelDTO(itemInstance));
        inventorySlots[i] = slot;
        if (leftover == null || leftover.CurrentStackCount <= 0)
        {
          return null;
        }
        itemInstance = leftover;
      }
    }
    return itemInstance;
  }
}
