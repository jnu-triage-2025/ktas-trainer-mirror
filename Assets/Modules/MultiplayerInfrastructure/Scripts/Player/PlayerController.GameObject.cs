using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    /// <summary>
    /// PlayerController 프리팹은 Hierarchy 상 Body와 SpectatorMarker를 이름으로 갖는
    /// GameObject 자식들을 포함해야 합니다.
    /// 
    /// Body: 플레이어가 일반 상태일 때 활성화되는 GameObject입니다.
    /// SpectatorMarker: 플레이어가 관전자 모드일 때 활성화되는 GameObject입니다.
    /// </summary>

    private GameObject _bodyObject;
    private GameObject _spectatorMarkerObject;

    private void Awake_GameObject()
    {
      var bodyTransform = transform.Find("Body");
      var spectatorMarkerTransform = transform.Find("SpectatorMarker");

      _bodyObject = bodyTransform != null ? bodyTransform.gameObject : null;
      _spectatorMarkerObject = spectatorMarkerTransform != null ? spectatorMarkerTransform.gameObject : null;

      if (_bodyObject == null)
        Debug.LogWarning("[PlayerController] Child object 'Body' was not found.", this);

      if (_spectatorMarkerObject == null)
        Debug.LogWarning("[PlayerController] Child object 'SpectatorMarker' was not found.", this);
    }
  }
}

