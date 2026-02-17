using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item
  {
    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _itemData != null ? _itemData.displayName : string.Empty;
    public Sprite DisplayIcon => Icon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public void Interact(Transform interactor)
    {
      Debug.Log($"Player interacted with item: {DisplayText}", this);

      if (interactor == null)
      {
        Debug.LogWarning("Item.Interact called with null interactor", this);
        return;
      }

      var interactorNob = interactor.GetComponentInParent<NetworkObject>();
      if (interactorNob == null)
      {
        Debug.LogWarning("Item.Interact could not find NetworkObject on interactor", this);
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
        Debug.LogWarning("Item.ServerHandlePickup could not find PlayerController on interactor", this);
        return;
      }

      if (_itemData == null || !_itemData.IsValid())
      {
        Debug.LogWarning("Item.ServerHandlePickup has invalid item data", this);
        return;
      }

      bool added = player.TryAddItemToInventory(_itemData);
      if (!added)
      {
        Debug.Log("Inventory full or invalid item, pickup aborted", this);
        return;
      }

      if (IsSpawned)
        Despawn();
    }
  }
}
