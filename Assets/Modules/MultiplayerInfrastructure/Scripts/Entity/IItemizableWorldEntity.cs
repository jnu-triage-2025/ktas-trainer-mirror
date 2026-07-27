using MultiplayerInfrastructure.Player;

namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// 좌클릭으로 월드 아이템으로 되돌릴 수 있는 설치형 엔티티의 계약입니다.
  /// </summary>
  public interface IItemizableWorldEntity
  {
    /// <summary>아이템화를 요청한다. 요청을 수락했으면 true를 반환한다.</summary>
    bool RequestItemization(PlayerController player);
  }
}
