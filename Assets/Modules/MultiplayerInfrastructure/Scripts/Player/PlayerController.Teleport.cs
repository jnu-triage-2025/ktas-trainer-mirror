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
      // ForcedFollowAnchor 해제는 클라이언트에서만 수행한다.
      // 서버에서 실행하면 시나리오 스크립트가 설정해 둔 서버 측 앵커를 조기 제거할 수 있다.
      // (FishNet 기본 동작: [ObserversRpc]는 서버 인스턴스에서도 RPC 본문을 실행한다.)
      if (!IsServerStarted)
        ClearForcedFollowAnchor();

      MoveToPositionPreservingForcedFollowAnchor(position);
    }

    /// <summary>
    /// 현재 forced-follow 소유자를 변경하지 않고 로컬 플레이어 위치를 안전하게 갱신한다.
    /// 다른 시스템이 플레이어를 고정하고 있을 수 있는 표현 복귀 경로에서 사용한다.
    /// </summary>
    public void MoveToPositionPreservingForcedFollowAnchor(Vector3 position)
    {
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
