using System;
using UnityEngine;

#nullable enable

/// <summary>
/// InventorySlotModelDTO는 플레이어의 각 인벤토리 칸을 표현하는 데이터 모델입니다.
/// 하지만 아직 InventorySlotModelDTO에 별도로 정의한 기능이 없으므로, 단순히 아이템 데이터 모델을
/// 래핑하는 용도로만 의도되어있습니다.
/// </summary>
[Serializable]
public class InventorySlotModelDTO
{
  #region Properties
  [SerializeField] private ItemInstanceModelDTO? _itemInstance;
  public ItemInstanceModelDTO? ItemInstance
  {
    get => _itemInstance;
    set => _itemInstance = value;
  }
  #endregion

  #region Constructors
  public InventorySlotModelDTO() { }
  public InventorySlotModelDTO(ItemInstanceModelDTO itemInstance)
  {
    this._itemInstance = itemInstance;
  }
  #endregion

  public bool IsEmpty => ItemInstance == null || !ItemInstance.IsValid();

  public void Clear()
  {
    ItemInstance = null;
  }

  public void SetItem(ItemInstanceModelDTO? item)
  {
    ItemInstance = item;
  }

  public ItemInstanceModelDTO? Push(ItemInstanceModelDTO item) => ItemInstance.Merge(item);

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

  public ItemInstanceModelDTO? TakeAll()
  {
    if (IsEmpty) return null;
    
    ItemInstanceModelDTO taken = ItemInstance!;
    ItemInstance = null;
    return taken;
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

  public ItemInstanceModelDTO? SwapWith(ItemInstanceModelDTO? incoming)
  {
    ItemInstanceModelDTO? previous = ItemInstance;
    ItemInstance = incoming;
    return previous;
  }
}
