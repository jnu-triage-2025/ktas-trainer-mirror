using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private GameEscapeMenuUIController _escapeMenuUIController;

    private void OnClientStart_EscapeMenu()
    {
      EnsureEscapeMenuController();
    }

    private void EnsureEscapeMenuController()
    {
      if (!_escapeMenuUIController.IsUnityNull())
        return;

      // Registry 조회를 우선 시도한다(정상 경로: UIControllerABC.Awake에서 등록됨).
      _escapeMenuUIController = Registry.Registry.Get<GameEscapeMenuUIController>(
        RegistryType.UI, Registry.Registry.TypeKey<GameEscapeMenuUIController>());

      if (!_escapeMenuUIController.IsUnityNull())
        return;

      // 폴백: 씬 로드 순서/초기화 경쟁 등으로 Registry 조회가 실패한 경우를 대비해
      // QuestUIController(PlayerController.Quest.cs)와 동일한 방식으로 씬에서 직접 탐색한다.
      _escapeMenuUIController = FindFirstObjectByType<GameEscapeMenuUIController>(FindObjectsInactive.Exclude);

      if (_escapeMenuUIController.IsUnityNull())
      {
        Debug.LogWarning(
          "[PlayerController] GameEscapeMenuUIController를 Registry와 씬 어디에서도 찾을 수 없습니다. " +
          "Esc 메뉴가 열리지 않습니다. 씬에 'MI: GameEscapeMenuUI'가 존재/활성 상태인지 확인하세요.");
      }
    }
  }
}
