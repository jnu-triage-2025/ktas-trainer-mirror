using MultiplayerInfrastructure.Item;
using MultiplayerInfrastructure.Player;
using IS = MultiplayerInfrastructure.ItemSystem;
using UnityEngine;

namespace MultiplayerInfrastructure.Examples.Item
{
  [CreateAssetMenu(fileName = "StoneItemBaseModel", menuName = "MultiplayerInfrastructure/Examples/Stone Item")]
  public class StoneItemBaseModelSO : ItemBaseModelSO
  {
    [Header("Stone 고유 설정")]
    [SerializeField] public int impactDamage = 5;

    public override ActionResult OnAttack(PlayerController player, MultiplayerInfrastructure.Entity.Entity target, IS.Item item)
    {
      Debug.Log(
        $"[StoneItemBaseModelSO] OnAttack — player={player?.name}, target={target?.GetType().Name}, impactDamage={impactDamage}",
        player
      );
      return ActionResult.Success;
    }

    public override ActionResult OnUse(PlayerController player, MultiplayerInfrastructure.Entity.Entity target, IS.Item item)
    {
      Debug.Log(
        $"[StoneItemBaseModelSO] OnUse — player={player?.name}, target={target?.GetType().Name}. 돌 블록 설치 시도.",
        player
      );
      return ActionResult.Success;
    }

    public override ActionResult OnGet(PlayerController player, IS.Item item)
    {
      Debug.Log($"[StoneItemBaseModelSO] 획득 — player={player?.name}", player);
      return ActionResult.Success;
    }

    public override ActionResult OnDrop(PlayerController player, IS.Item item)
    {
      Debug.Log($"[StoneItemBaseModelSO] 드롭 — player={player?.name}", player);
      return ActionResult.Success;
    }
  }
}

