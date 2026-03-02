using MultiplayerInfrastructure.Item;
using MultiplayerInfrastructure.Player;
using IS = MultiplayerInfrastructure.ItemSystem;
using UnityEngine;

namespace MultiplayerInfrastructure.Examples.Item
{
  [CreateAssetMenu(fileName = "WoodItemBaseModel", menuName = "MultiplayerInfrastructure/Examples/Wood Item")]
  public class WoodItemBaseModelSO : ItemBaseModelSO
  {
    [Header("Wood 고유 설정")]
    [SerializeField] public int durabilityPerSwing = 1;

    public override ActionResult OnAttack(PlayerController player, MultiplayerInfrastructure.Entity.Entity target, IS.Item item)
    {
      if (item.HasCurrentDurability && item.CurrentDurability <= 0)
      {
        Debug.Log($"[WoodItemBaseModelSO] OnAttack 취소 — 내구도 소진 (player={player?.name})", player);
        return ActionResult.Cancelled;
      }
      if (item.HasCurrentDurability)
        item.CurrentDurability -= durabilityPerSwing;
      Debug.Log(
        $"[WoodItemBaseModelSO] OnAttack — player={player?.name}, target={target?.GetType().Name}, 남은 내구도={item.CurrentDurability}",
        player
      );
      return ActionResult.Success;
    }

    public override ActionResult OnUse(PlayerController player, MultiplayerInfrastructure.Entity.Entity target, IS.Item item)
    {
      Debug.Log(
        $"[WoodItemBaseModelSO] OnUse — player={player?.name}, target={target?.GetType().Name}. 나무 블록 설치 시도.",
        player
      );
      return ActionResult.Success;
    }

    public override ActionResult OnGet(PlayerController player, IS.Item item)
    {
      Debug.Log($"[WoodItemBaseModelSO] 획득 — player={player?.name}", player);
      return ActionResult.Success;
    }

    public override ActionResult OnDrop(PlayerController player, IS.Item item)
    {
      Debug.Log($"[WoodItemBaseModelSO] 드롭 — player={player?.name}", player);
      return ActionResult.Success;
    }
  }
}

