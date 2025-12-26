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
    
    GameObject _bodyObject;
    GameObject _spectatorMarkerObject;

    void Awake_GameObject()
    {
      _bodyObject = transform.Find("Body").gameObject;
      _spectatorMarkerObject = transform.Find("SpectatorMarker").gameObject;
    }
  }
}

