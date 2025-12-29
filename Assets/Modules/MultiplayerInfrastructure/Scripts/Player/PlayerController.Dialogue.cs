using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private ScenarioController _scenarioController;
    
    private void OnStartClient_Dialogue()
    {
      if (!IsOwner) return;

      if (_dialoguePanelUIController.IsUnityNull())
        _dialoguePanelUIController = FindDialoguePanelUIController();

      if (!_dialoguePanelUIController.IsUnityNull())
      {
        // Ensure registry has the instance so other systems can resolve it without warnings.
        UIControlRegistry.Register(_dialoguePanelUIController);
      }

      _scenarioController = ScenarioController.Instance;
      _scenarioController.RegisterReferences(
        _dialoguePanelUIController,
        _camControl,
        _interactableHintUI
      );

      if (!_dialoguePanelUIController.IsUnityNull())
      {
        RegisterOverlayLock(_dialoguePanelUIController, locked => _keyHandlingLockedByDialogueUI = locked);
      }
    }

    /// <summary>
    /// DialoguePanelUIController를 씬에서 찾아 반환합니다. Inspector 할당을 권장하지만, 누락 시 이름/타입 검색으로 보완합니다.
    /// </summary>
    private DialoguePanelUIController FindDialoguePanelUIController()
    {
      // 이름으로 우선 검색
      var byName = GameObject.Find("DialoguePanelUI");
      if (byName != null)
      {
        var controller = byName.GetComponent<DialoguePanelUIController>();
        if (!controller.IsUnityNull())
          return controller;
      }

      // 타입으로 검색 (비권장: 성능 비용 있음)
      var found = Object.FindObjectOfType<DialoguePanelUIController>();
      if (!found.IsUnityNull())
      {
        Debug.LogWarning("[PlayerController] DialoguePanelUIController found via FindObjectOfType. Consider assigning it directly in Inspector.");
        return found;
      }

      Debug.LogWarning("[PlayerController] DialoguePanelUIController not found in scene.");
      return null;
    }
  }
}
