using System;
using MultiplayerInfrastructure.ItemSystem;

#nullable enable

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// 장비 슬롯의 종류를 나타냅니다.
  /// </summary>
  public enum EquipmentSlotType
  {
    Glove,
  }

  /// <summary>
  /// 플레이어 장비 슬롯 하나를 나타냅니다.
  /// <see cref="EquipmentSlotType"/> 에 해당하는 아이템만 장착할 수 있으며,
  /// 내부에 <see cref="InventorySlotModelDTO"/> 를 보유하여 아이템을 관리합니다.
  /// </summary>
  [Serializable]
  public class EquipmentSlotModelDTO
  {
    public readonly EquipmentSlotType SlotType;

    private readonly InventorySlotModelDTO _slot = new();

    /// <summary>현재 장착된 아이템 인스턴스 (null = 비어있음).</summary>
    public Item? ItemInstance
    {
      get => _slot.ItemInstance;
      private set => _slot.ItemInstance = value;
    }

    /// <summary>이 슬롯이 비어 있는지 여부.</summary>
    public bool IsEmpty => _slot.IsEmpty;

    public EquipmentSlotModelDTO(EquipmentSlotType slotType)
    {
      SlotType = slotType;
    }

    /// <summary>
    /// 지정 아이템이 이 장비 슬롯에 장착 가능한지 확인합니다.
    /// 아이템 클래스에 적용된 장비 Attribute 를 리플렉션으로 검사합니다.
    /// </summary>
    public bool CanAccept(Item? item)
    {
      if (item == null)
        return false;

      return SlotType switch
      {
        EquipmentSlotType.Glove => EquipmentAttributeHelper.IsEquippableGlove(item),
        _ => false,
      };
    }

    /// <summary>
    /// 아이템을 장착합니다. <see cref="CanAccept"/> 검사를 먼저 수행해야 합니다.
    /// 기존에 장착된 아이템을 반환합니다 (없으면 null).
    /// </summary>
    public Item? Equip(Item? newItem)
    {
      var previous = _slot.TakeAll();
      if (newItem != null)
        _slot.SetItem(newItem);
      return previous;
    }

    /// <summary>장착된 아이템을 해제(반환)합니다. 비어 있으면 null.</summary>
    public Item? Unequip()
    {
      return _slot.TakeAll();
    }

    /// <summary>장비 슬롯을 비웁니다.</summary>
    public void Clear() => _slot.Clear();

    /// <summary>
    /// 내부 슬롯 DTO 에 대한 참조를 반환합니다 (UI 바인딩용).
    /// </summary>
    public InventorySlotModelDTO InnerSlot => _slot;
  }
}
