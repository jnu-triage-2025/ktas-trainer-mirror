using System;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// <see cref="StaticPlacedItem"/> 이 특정 플레이어에게 "사라짐(Vanished)" 상태가 되었을 때의 표현 방식입니다.
  ///
  /// 어떤 값이든 사라짐 상태에서는 상호작용(획득)이 항상 비활성화됩니다.
  /// (보이지 않는데 계속 획득 가능한 혼란을 막기 위함)
  /// </summary>
  [Serializable]
  public enum StaticPlacedItemVanishBehavior
  {
    /// <summary>(기본값) 렌더러를 꺼서 보이지 않게 한다. 상호작용도 함께 비활성화된다.</summary>
    Invisible = 0,

    /// <summary>
    /// 게임 오브젝트를 비활성화(<c>SetActive(false)</c>)한다.
    /// 실제 파괴(Destroy)가 아니므로 플레이어별 개별 사라짐/재접속 동기화와 호환된다.
    /// </summary>
    Deactivate,

    /// <summary>여전히 보이지만 상호작용(획득)만 비활성화한다.</summary>
    DisableInteraction,
  }
}
