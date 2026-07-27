using MultiplayerInfrastructure.Player;

namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// 좌클릭으로 월드 아이템으로 되돌릴 수 있는 설치형 엔티티의 계약입니다.
  /// </summary>
  public interface IItemizableWorldEntity
  {
    /// <summary>서버가 회수 대상을 다시 찾는 데 사용하는 런타임 엔티티 식별자입니다.</summary>
    string ItemizationEntityIdentifier { get; }

    /// <summary>아이템화를 요청한다. 요청을 수락했으면 true를 반환한다.</summary>
    bool RequestItemization(PlayerController player);

    /// <summary>서버 권한 아래서 월드 아이템 생성과 원본 엔티티 제거를 수행합니다.</summary>
    bool TryItemizeOnServer(PlayerController player);
  }
}
