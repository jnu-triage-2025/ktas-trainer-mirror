using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private ScenarioController _scenarioController;

    private void OnStartClient_Dialogue()
    {
      if (!IsOwner)
        return;

      if (_dialoguePanelUIController.IsUnityNull())
        _dialoguePanelUIController = FindDialoguePanelUIController();

      if (!_dialoguePanelUIController.IsUnityNull())
      {
        // 레지스트리에 인스턴스를 등록해 다른 시스템이 경고 없이 조회할 수 있게 한다.
        Registry.Registry.Register(RegistryType.UI, Registry.Registry.TypeKey<DialoguePanelUIController>(), _dialoguePanelUIController);
      }

      // 표시 전용 클라이언트는 시스템 오버레이 씬(ScenarioController 포함)이 플레이어 스폰보다
      // 늦게 준비될 수 있다. 그때 역참조하면 예외로 나머지 UI 바인딩까지 건너뛰므로, 인스턴스가
      // 생기는 시점에 다시 등록한다.
      _scenarioController = ScenarioController.Instance;
      if (_scenarioController.IsUnityNull())
      {
        ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
        ScenarioController.InstanceAvailable += HandleScenarioControllerAvailable;
        Debug.LogWarning("[PlayerController] ScenarioController is not available yet; dialogue references will be registered when it appears.", this);
        return;
      }

      RegisterScenarioReferences();
    }

    private void OnStopClient_Dialogue()
    {
      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
    }

    private void HandleScenarioControllerAvailable(ScenarioController controller)
    {
      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
      if (this == null || !IsOwner || controller.IsUnityNull())
        return;

      _scenarioController = controller;
      RegisterScenarioReferences();
    }

    private void RegisterScenarioReferences()
    {
      if (_scenarioController.IsUnityNull())
        return;

      _scenarioController.RegisterReferences(
        _dialoguePanelUIController,
        _camControl,
        _interactableHintUI
      );
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

      var found = FindFirstObjectByType<DialoguePanelUIController>(FindObjectsInactive.Exclude);
      if (!found.IsUnityNull())
      {
        return found;
      }

      Debug.LogWarning("[PlayerController] DialoguePanelUIController not found in scene.");
      return null;
    }
  }
}
