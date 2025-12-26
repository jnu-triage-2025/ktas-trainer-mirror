using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity.Definitions
{
  public class LootableItemInteractHandler : MonoBehaviour, IInteractable
  {
    public string DisplayText => "";
    public Sprite DisplayIcon => null;
    public Color DisplayColor => Color.white;
    public void Interact(Transform interactor)
    {
      
    }
  }
}
