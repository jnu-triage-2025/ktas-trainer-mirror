using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    /// <summary>
    /// PlayerController 프리팹은 PlayerCharacterBody와 PlayerSpectatorMarkerObject
    /// 역할 컴포넌트가 붙은 자식들을 각각 하나씩 포함해야 합니다.
    /// 
    /// Body: 플레이어가 일반 상태일 때 활성화되는 GameObject입니다.
    /// SpectatorMarker: 플레이어가 관전자 모드일 때 활성화되는 GameObject입니다.
    /// </summary>

    private GameObject _bodyObject;
    private GameObject _spectatorMarkerObject;

    private void Awake_GameObject()
    {
      var bodyMarker = ResolveUniquePlayerRoleMarker<PlayerCharacterBody>();
      var spectatorMarker = ResolveUniquePlayerRoleMarker<PlayerSpectatorMarkerObject>();

      _bodyObject = bodyMarker != null ? bodyMarker.gameObject : null;
      _spectatorMarkerObject = spectatorMarker != null ? spectatorMarker.gameObject : null;

      if (_bodyObject == null)
        Debug.LogWarning("[PlayerController] PlayerCharacterBody marker was not found.", this);

      if (_spectatorMarkerObject == null)
        Debug.LogWarning("[PlayerController] PlayerSpectatorMarkerObject marker was not found.", this);
    }

    private T ResolveUniquePlayerRoleMarker<T>() where T : Component
    {
      var matches = GetComponentsInChildren<T>(true);
      if (matches.Length == 1)
        return matches[0];
      if (matches.Length > 1)
        Debug.LogError($"[PlayerController] {matches.Length} {typeof(T).Name} markers found; exactly one is required.", this);
      return null;
    }
  }
}

