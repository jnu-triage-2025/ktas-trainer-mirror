using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using TriageTrainer.Definitions;
using NUnit.Framework;

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

  public ItemInstanceModelDTO Push(ItemInstanceModelDTO itemInstance)
  {
    for (int i = 0; i < inventorySlots.Count; i++)
    {
      var slot = inventorySlots[i];
      if (slot.IsEmpty || slot.ItemInstance!.CanStackWith(itemInstance))
      {
        var leftover = slot.Push(new InventorySlotModelDTO(itemInstance));
        inventorySlots[i] = slot;
        if (leftover == null || !leftover.IsValid())
        {
          return null;
        }
        itemInstance = leftover;
      }
    }
    return itemInstance;
  }
}
