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

    private void OnStartClient_Crosshair()
    {
      if (!IsOwner)
        return;

      // Inspector 미할당 시 레지스트리에서 탐색
      if (_crosshairUI.IsUnityNull())
        _crosshairUI = Registry.Registry.Get<CrosshairUIController>(
          RegistryType.UI,
          Registry.Registry.TypeKey<CrosshairUIController>()
        );

      if (_crosshairUI.IsUnityNull())
      {
        Debug.LogWarning("[PlayerController][UIBinding] CrosshairUIController is not available yet; binding will keep retrying.", this);
        StartCoroutine(BindCrosshairWhenReady());
        return;
      }

      // 크로스헤어 UI를 보이게 설정
      _crosshairUI.SetCrosshairVisible(true);
      Debug.Log("[PlayerController][UIBinding] Crosshair UI bound during client startup.", this);
    }

    private System.Collections.IEnumerator BindCrosshairWhenReady()
    {
      float startedAt = Time.unscaledTime;
      bool delayedWarningLogged = false;
      while (IsOwner && _crosshairUI.IsUnityNull())
      {
        yield return null;
        _crosshairUI = Registry.Registry.Get<CrosshairUIController>(
          RegistryType.UI,
          Registry.Registry.TypeKey<CrosshairUIController>());
        if (_crosshairUI.IsUnityNull())
          _crosshairUI = FindFirstObjectByType<CrosshairUIController>(FindObjectsInactive.Include);

        if (!delayedWarningLogged && Time.unscaledTime - startedAt >= 5f)
        {
          delayedWarningLogged = true;
          Debug.LogWarning("[PlayerController][UIBinding] Crosshair UI is still unavailable after 5s.", this);
        }
      }

      if (_crosshairUI.IsUnityNull())
        yield break;

      _crosshairUI.SetCrosshairVisible(true);
      Debug.Log($"[PlayerController][UIBinding] Crosshair UI bound after {Time.unscaledTime - startedAt:F2}s.", this);
    }
  }
}
