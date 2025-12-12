using FishNet.Object;
using TriageTrainer.Scripts.InteractableEntity;
using UnityEngine;

/// <remarks>
/// GroundedItem의 IInteractable 구현 부분
/// </remarks>
public partial class GroundedItem : NetworkBehaviour, IInteractable
{
  // IInteractable
  public string DisplayText => _itemInstanceModel.displayName;
  public Sprite DisplayIcon => Icon;
  public Color DisplayColor => Color.white;

  public void Interact(Transform interactor)
  {
    Debug.Log($"Player interacted with grounded item: {DisplayText}", this);
  }
}
