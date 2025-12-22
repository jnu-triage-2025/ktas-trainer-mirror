using FishNet.Object;
using TriageTrainer.InteractableEntity;
using TriageTrainer.Player;
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

    if (interactor == null)
    {
      Debug.LogWarning("GroundedItem.Interact called with null interactor", this);
      return;
    }

    var interactorNob = interactor.GetComponentInParent<NetworkObject>();
    if (interactorNob == null)
    {
      Debug.LogWarning("GroundedItem.Interact could not find NetworkObject on interactor", this);
      return;
    }

    ServerHandlePickup(interactorNob);
  }

  [ServerRpc(RequireOwnership = false)]
  public void ServerHandlePickup(NetworkObject interactorNob)
  {
    var player = interactorNob.GetComponent<PlayerController>();
    if (player == null)
    {
      Debug.LogWarning("GroundedItem.ServerHandlePickup could not find PlayerController on interactor", this);
      return;
    }

    bool added = player.TryAddItemToInventory(_itemInstanceModel);
    if (!added)
    {
      Debug.Log("Inventory full or invalid item, pickup aborted", this);
      return;
    }

    if (IsSpawned)
      Despawn();
  }
}
