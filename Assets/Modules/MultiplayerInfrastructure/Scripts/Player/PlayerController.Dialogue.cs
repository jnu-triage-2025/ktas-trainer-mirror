using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private ScenarioController _scenarioController;
    
    private void OnStartClient_Dialogue()
    {
      if (!IsOwner) return;

      _scenarioController = ScenarioController.Instance;
      _scenarioController.RegisterReferences(
        _dialoguePanelUIController,
        _camControl,
        _interactableHintUI
      );
    }
  }
}
