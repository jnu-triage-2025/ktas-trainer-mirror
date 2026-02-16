using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity.Definitions
{
  public class LootableItemInteractHandler : MonoBehaviour, IInteractable, IInteract
  {
    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => "";
    public Sprite DisplayIcon => null;
    public Color DisplayColor => Color.white;
    public void Interact(Transform interactor)
    {
      
    }
  }
}
