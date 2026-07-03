using System;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// <see cref="StaticPlacedItem"/> 을 1회 획득(Pickup)할 때 지급되는 보상 및 상태 변화 정의입니다.
  /// (사양의 OnPickupTry)
  /// </summary>
  [Serializable]
  public struct StaticPlacedItemPickupReward
  {
    [Tooltip("획득할 아이템 식별자입니다. Registry에 등록된 Item Identifier(예: \"gauze\")여야 합니다.")]
    [SerializeField] private string _itemIdentifier;

    [Tooltip("획득할 아이템 수량입니다.")]
    [Min(1)]
    [SerializeField] private int _amount;

    [Tooltip("1회 획득 시 남은 획득 가능 횟수(Remains)를 얼마나 줄일지 결정합니다. 0이면 감소하지 않습니다(무한).")]
    [SerializeField] private int _decreaseRemains;

    /// <summary>획득할 아이템 식별자.</summary>
    public string ItemIdentifier
    {
      get => _itemIdentifier;
      set => _itemIdentifier = value;
    }

    /// <summary>획득할 아이템 수량(최소 1).</summary>
    public int Amount
    {
      get => Mathf.Max(1, _amount);
      set => _amount = Mathf.Max(1, value);
    }

    /// <summary>1회 획득 시 Remains 감소량(음수 방지, 0이면 감소하지 않음).</summary>
    public int DecreaseRemains
    {
      get => Mathf.Max(0, _decreaseRemains);
      set => _decreaseRemains = Mathf.Max(0, value);
    }

    public static StaticPlacedItemPickupReward CreateDefault()
      => new StaticPlacedItemPickupReward
      {
        _itemIdentifier = string.Empty,
        _amount = 1,
        _decreaseRemains = 1,
      };
  }
}
