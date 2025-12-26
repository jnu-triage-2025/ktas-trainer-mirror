using TriageTrainer.Dialogue;

namespace TriageTrainer.Player
{
  public partial class PlayerController
  {
    private DialogueController _dialogueController;
    
    private void OnStartClient_Dialogue()
    {
      if (!IsOwner) return;

      _dialogueController = DialogueController.Instance;
      _dialogueController.RegisterReferences(
        _dialoguePanelUIController,
        _camControl,
        _interactableHintUI
      );
    }
  }
}
