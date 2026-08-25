using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  public abstract class Interactable : MonoBehaviour, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget
  {
    [Header("Display")]
    [SerializeField] private string _displayText = string.Empty;
    // 기존 씬/프리팹의 직렬화 데이터를 보존하기 위한 단일 아이콘 필드입니다.
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private List<Sprite> _displayIcons = new();
    [SerializeField] private Color _displayColor = Color.white;
    [SerializeField] private string _presentationEntityIdentifier = string.Empty;
    [SerializeField] private string _interactionIdentifier = string.Empty;

    public virtual string DisplayText => _displayText;
    public virtual Sprite DisplayIcon => _displayIcon;
    public virtual IReadOnlyList<Sprite> DisplayIcons => _displayIcons;
    public virtual bool AllowDisplayIconFallback => true;
    public virtual Color DisplayColor => _displayColor;
    public virtual string PresentationEntityIdentifier => _presentationEntityIdentifier;
    public virtual string InteractionIdentifier => _interactionIdentifier;
    public virtual IInteract[] Interacts => new IInteract[] { this };

    public abstract void Interact(Transform interactor);
  }
}
