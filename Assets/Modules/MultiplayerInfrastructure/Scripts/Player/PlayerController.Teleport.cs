using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController : NetworkBehaviour
  {
    /// <summary>
    /// 서버에서 이 플레이어를 지정 위치로 즉시 텔레포트합니다.
    /// CharacterController 를 일시 비활성화한 뒤 위치를 설정하여 콜라이더 충돌을 우회합니다.
    /// 변경 사항은 ObserversRpc 를 통해 모든 클라이언트에 전파됩니다.
    /// </summary>
    public void TeleportToServer(Vector3 position)
    {
      if (!IsServerStarted)
        return;

      RpcTeleportTo(position);
    }

    [ObserversRpc]
    private void RpcTeleportTo(Vector3 position)
    {
      // ForcedFollowAnchor가 설정되어 있으면 해제하여 텔레포트가 덮어씌워지지 않도록 한다.
      ClearForcedFollowAnchor();

      // CharacterController 가 활성화된 상태에서 transform.position 을 직접 바꾸면
      // 내부 상태와 충돌이 발생할 수 있으므로 일시 비활성화 후 이동한다.
      bool wasEnabled = _characterController != null && _characterController.enabled;
      if (_characterController != null)
        _characterController.enabled = false;

      transform.position = position;
      _moveDirection = Vector3.zero;

      if (_characterController != null)
        _characterController.enabled = wasEnabled;
    }
  }
}
