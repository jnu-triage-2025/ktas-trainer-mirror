using System;

#nullable enable

[Serializable]
public class InventorySlotModelDTO
{
  public ItemInstanceModelDTO? itemInstance;
  public ItemInstanceModelDTO? ItemInstance
  {
    get => itemInstance;
    set => itemInstance = value;
  }

  public InventorySlotModelDTO() { }
  public InventorySlotModelDTO(ItemInstanceModelDTO itemInstance)
  {
    this.itemInstance = itemInstance;
  }

  public bool IsEmpty => ItemInstance == null || !ItemInstance.IsValid();

  public void Clear()
  {
    ItemInstance = null;
  }

  public ItemInstanceModelDTO? Push(InventorySlotModelDTO other)
  {
    if (IsEmpty)
    {
      ItemInstance = other.ItemInstance;
      return null;
    }

    if (other.IsEmpty)
    {
      return null;
    }

    if (ItemInstance!.identifier != other.ItemInstance!.identifier)
    {
      return other.ItemInstance;
    }

    int spaceLeft = ItemInstance!.maxCount - ItemInstance.currCount;
    if (spaceLeft <= 0)
    {
      return other.ItemInstance;
    }
    int toMove = Math.Min(spaceLeft, other.ItemInstance!.currCount);
    ItemInstance!.currCount += toMove;
    other.ItemInstance!.currCount -= toMove;
    if (other.ItemInstance!.currCount > 0)
    {
      return other.ItemInstance;
    }

    return null;
  }

  public ItemInstanceModelDTO? Pop(int count)
  {
    if (IsEmpty || count <= 0)
    {
      return null;
    }

    int toPop = Math.Min(count, ItemInstance!.currCount);
    ItemInstanceModelDTO popped = new ItemInstanceModelDTO(ItemInstance!);
    popped.currCount = toPop;
    ItemInstance!.currCount -= toPop;
    if (ItemInstance!.currCount <= 0)
    {
      Clear();
    }
    return popped;
  }
}
