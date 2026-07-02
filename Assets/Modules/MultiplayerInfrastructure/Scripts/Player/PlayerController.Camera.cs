using FishNet.Object;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// PlayerController의 카메라 및 카메라 홀더 처리 부분 구현
  /// 
  /// PlayerController는 카메라 홀더를 두고, 이를 카메라가 추종하도록 하고 있습니다.
  /// 이것은 플레이어 카메라 홀더에 한 사람만 카메라를 추종하는 것이 아니라,
  /// 여러 명이 한 플레이어의 시점을 공유할 수 있게 할 수 있도록 하기 위함입니다.
  /// 
  /// 이렇게 함으로서, 관전자 모드를 두 가지 방향 (자유 이동, 시점 추종)으로 구현할 수 있습니다.
  /// </summary>
  public partial class PlayerController
  {
    [SerializeField] private MainCameraController _camControl;
    void Awake_Camera()
    {
      // Camera attachment is deferred to owner check in OnStartClient to avoid other players overwriting
      // the global main camera target.
    }

    void OnStartClient_Camera()
    {
      if (!IsOwner) return;
      _camControl = MainCameraController.Instance ?? Registry.Registry.Get<MainCameraController>(RegistryType.Service, Registry.Registry.TypeKey<MainCameraController>());

      if (_camControl.IsUnityNull()) return;
      
      _camControl.SetTarget(this);
    }

    void LateUpdate_Camera()
    {
      if (!IsOwner) return;
      if (_camControl == null) return;

      // 관전 추종(spectate follow) 중에는 다른 플레이어의 카메라 홀더를 따라가야 하므로
      // 로컬 홀더로 되돌리지 않는다. (되돌리면 관전 추종이 매 프레임 풀리는 버그 발생)
      if (_isSpectateFollowing)
      {
        // 추종 대상이 파괴(접속 종료 등)되면 자동으로 관전 추종을 해제한다.
        if (_spectateFollowTarget == null)
          StopSpectateFollow();
        return;
      }

      // Ensure camera sticks to the local owner's holder even if other events tried to retarget.
      if (_camControl.FollowingCameraHolder != _cameraHolderTransform)
        _camControl.SetTarget(this);
    }

    void SwitchCameraViewMode()
    {
      switch (_camControl.CurrentViewMode)
      {
        case CameraViewMode.FirstPerson:
          _camControl.CurrentViewMode = CameraViewMode.ThirdPerson;
          break;
        case CameraViewMode.ThirdPerson:
          _camControl.CurrentViewMode = CameraViewMode.FirstPerson;
          break;
      }
    }
  }
}
