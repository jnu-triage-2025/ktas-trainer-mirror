using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  public interface IInteractorConditional
  {
    public bool CanInteract(Transform interactor);
  }
}
