using System;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// <see cref="StaticPlacedItem"/> 의 획득 처리(Pickup) 방식입니다.
  /// </summary>
  [Serializable]
  public enum StaticPlacedItemVanishMode
  {
    /// <summary>
    /// (기본값) 서버 전역에서 남은 획득 가능 횟수(Remains)를 하나만 관리한다.
    /// 누군가 획득하여 Remains 가 0이 되면 서버 전역(모든 플레이어)에서 사라진다.
    /// </summary>
    VanishedGlobalOnPickup = 0,

    /// <summary>
    /// 남은 획득 가능 횟수(Remains)를 각 플레이어(UserIdentifier)마다 따로 계산한다.
    /// Remains 가 0이 된 개별 플레이어에게서만 보이지 않게 된다.
    /// </summary>
    VanishedLocalOnPickup,

    /// <summary>
    /// 관련 처리를 일체 하지 않는다. 항상 존재하고 항상 획득 가능하다(Remains 무시).
    /// </summary>
    AlwaysExists,
  }
}
