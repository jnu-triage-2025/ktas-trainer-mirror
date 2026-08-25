using System;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// <see cref="StaticPlacedItem"/> 의 런타임 상태 정의입니다. (사양의 State)
  ///
  /// 인스펙터에 배치된 값은 "초기 상태"이며, 런타임 중 서버가 권위 있게 관리합니다.
  /// - <see cref="StaticPlacedItemVanishMode.VanishedGlobalOnPickup"/>: 서버 전역에서 하나의 Remains.
  /// - <see cref="StaticPlacedItemVanishMode.VanishedLocalOnPickup"/>: 플레이어(UserIdentifier)마다 이 초기값에서 시작.
  /// </summary>
  [Serializable]
  public struct StaticPlacedItemState
  {
    [Tooltip("남은 획득 가능 횟수입니다. 0 이하가 되면 사라짐(Vanished) 상태가 됩니다.")]
    [SerializeField] private int _remains;

    /// <summary>남은 획득 가능 횟수.</summary>
    public int Remains
    {
      get => _remains;
      set => _remains = value;
    }

    public static StaticPlacedItemState CreateDefault()
      => new StaticPlacedItemState { _remains = 1 };
  }
}
