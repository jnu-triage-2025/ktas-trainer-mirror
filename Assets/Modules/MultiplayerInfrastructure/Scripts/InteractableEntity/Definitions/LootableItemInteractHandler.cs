using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity.Definitions
{
  /// <summary>
  /// 월드에 드롭된 아이템(<see cref="ItemObject"/>)을 플레이어가 획득할 수 있도록 합니다.
  ///
  /// ItemObject.Spawn() 에서 자동으로 추가됩니다.
  /// NearbyInteractablesDetector 가 같은 GameObject의 Collider를 통해 감지하며,
  /// 플레이어가 상호작용(E 키)하면 Interact()가 호출됩니다.
  /// </summary>
  public class LootableItemInteractHandler : MonoBehaviour, IInteractable, IInteract
  {
    // ── IInteractable ────────────────────────────────────────────────────
    public IInteract[] Interacts => new IInteract[] { this };

    // ── IInteract ────────────────────────────────────────────────────────
    public string DisplayText
    {
      get
      {
        var item = GetComponent<ItemObject>()?.Item;
        return item != null ? $"{item.CurrentDisplayName} 획득" : "획득";
      }
    }

    public Sprite DisplayIcon => GetComponent<ItemObject>()?.Item?.CurrentItemIconTexture;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    /// <summary>
    /// 플레이어가 상호작용할 때 호출됩니다.
    /// 아이템을 플레이어 인벤토리에 추가하고 ItemObject를 제거합니다.
    /// </summary>
    public void Interact(Transform interactor)
    {
      var itemObject = GetComponent<ItemObject>();
      if (itemObject == null || itemObject.Item == null)
      {
        Debug.LogWarning("[LootableItemInteractHandler] ItemObject 또는 Item이 null입니다.");
        return;
      }

      var player = interactor.GetComponentInParent<PlayerController>()
                ?? interactor.GetComponent<PlayerController>();
      if (player == null)
      {
        Debug.LogWarning("[LootableItemInteractHandler] interactor에서 PlayerController를 찾지 못했습니다.");
        return;
      }

      bool added = player.TryAddItemToInventory(itemObject.Item);
      if (added)
      {
        itemObject.Item.OnGet(player);
        Destroy(itemObject.gameObject);
      }
    }
  }
}
