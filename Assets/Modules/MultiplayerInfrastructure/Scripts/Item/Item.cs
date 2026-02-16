using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item : NetworkBehaviour, IInteractable, IInteract
  {
    [Header("Item Data")]
    [SerializeField] private ItemBaseModelSO _itemBaseModel;
    [SerializeField] private ItemData _itemData;
    [SerializeField] private Sprite _itemIcon;

    [SerializeField] private string _itemIdentifier;
    public string ItemIdentifier => _itemIdentifier;

    public ItemData Data => _itemData;
  }
}
