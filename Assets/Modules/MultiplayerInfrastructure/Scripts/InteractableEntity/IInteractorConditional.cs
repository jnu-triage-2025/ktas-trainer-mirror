using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  public interface IInteractorConditional
  {
    bool CanInteract(Transform interactor);
  }
}
