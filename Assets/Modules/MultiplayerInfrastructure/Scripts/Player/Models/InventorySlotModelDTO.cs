using System;
using MultiplayerInfrastructure.ItemSystem;
using UnityEngine;

#nullable enable

/// <summary>
/// 플레이어 인벤토리의 슬롯 하나를 나타냅니다.
/// ItemSystem.Item 인스턴스를 보유하며, 스택 병합/분리 등의 슬롯 조작을 제공합니다.
/// </summary>
[Serializable]
public class InventorySlotModelDTO
{
  [SerializeReference] private Item? _itemInstance;
  public Item? ItemInstance
  {
    get => _itemInstance;
    set => _itemInstance = value;
  }

  public bool IsEmpty => _itemInstance == null || _itemInstance.CurrentStackCount <= 0;

  public InventorySlotModelDTO() { }
  public InventorySlotModelDTO(Item item) { _itemInstance = item; }

  public void Clear() => _itemInstance = null;

  public void SetItem(Item? item) => _itemInstance = item;

  /// <summary>item をこのスロットに可能な限り積みます。余りを返します。null なら全部収納できた。</summary>
  public Item? Push(Item item)
  {
    if (IsEmpty)
    {
      _itemInstance = item;
      return null;
    }
    var leftover = _itemInstance!.Merge(item);
    return leftover.CurrentStackCount > 0 ? leftover : null;
  }

  public Item? Push(InventorySlotModelDTO other)
  {
    if (other.IsEmpty) return null;

    if (IsEmpty)
    {
      _itemInstance = other._itemInstance;
      other._itemInstance = null;
      return null;
    }

    if (_itemInstance!.CurrentIdentifier != other._itemInstance!.CurrentIdentifier)
      return other._itemInstance;

    int spaceLeft = _itemInstance.CurrentMaxStackCount - _itemInstance.CurrentStackCount;
    if (spaceLeft <= 0) return other._itemInstance;

    int toMove = Math.Min(spaceLeft, other._itemInstance.CurrentStackCount);
    _itemInstance.CurrentStackCount += toMove;
    other._itemInstance.CurrentStackCount -= toMove;

    return other._itemInstance.CurrentStackCount > 0 ? other._itemInstance : null;
  }

  public Item? TakeAll()
  {
    if (IsEmpty) return null;
    var taken = _itemInstance;
    _itemInstance = null;
    return taken;
  }

  public Item? Pop(int count)
  {
    if (IsEmpty || count <= 0) return null;

    int toPop = Math.Min(count, _itemInstance!.CurrentStackCount);
    var popped = _itemInstance.Clone();
    popped.CurrentStackCount = toPop;
    _itemInstance.CurrentStackCount -= toPop;
    if (_itemInstance.CurrentStackCount <= 0)
      Clear();
    return popped;
  }

  public Item? SwapWith(Item? incoming)
  {
    var previous = _itemInstance;
    _itemInstance = incoming;
    return previous;
  }
}

