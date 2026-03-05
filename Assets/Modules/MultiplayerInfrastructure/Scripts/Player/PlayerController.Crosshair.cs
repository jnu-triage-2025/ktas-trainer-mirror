using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// PlayerController의 크로스헤어 UI 처리 부분 구현.
  /// 
  /// CrosshairUIController를 찾아 등록하고,
  /// 필요에 따라 크로스헤어 UI의 시각성을 제어합니다.
  /// </summary>
  public partial class PlayerController
  {
    [SerializeField] private CrosshairUIController _crosshairUI;

    void OnStartClient_Crosshair()
    {
      if (!IsOwner) return;

      // Inspector 미할당 시 레지스트리에서 탐색
      if (_crosshairUI.IsUnityNull())
        _crosshairUI = Registry.Registry.Get<CrosshairUIController>(
          RegistryType.UI,
          Registry.Registry.TypeKey<CrosshairUIController>()
        );

      if (_crosshairUI.IsUnityNull())
      {
        Debug.LogWarning("[PlayerController] CrosshairUIController를 찾지 못했습니다. " +
                         "씬에 CrosshairUI GameObject가 배치되어 있는지 확인하세요.");
        return;
      }

      // 크로스헤어 UI를 보이게 설정
      _crosshairUI.SetCrosshairVisible(true);
    }
  }
}
