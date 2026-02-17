using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  public abstract class Interactable : MonoBehaviour, IInteractable, IInteract
  {
    [Header("Display")]
    [SerializeField] private string _displayText = string.Empty;
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private Color _displayColor = Color.white;

    public virtual string DisplayText => _displayText;
    public virtual Sprite DisplayIcon => _displayIcon;
    public virtual bool AllowDisplayIconFallback => true;
    public virtual Color DisplayColor => _displayColor;
    public virtual IInteract[] Interacts => new IInteract[] { this };

    public abstract void Interact(Transform interactor);
  }
}
