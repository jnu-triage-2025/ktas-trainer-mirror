using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem.Examples
{
  /// <summary>
  /// 나무 블록 아이템 구현 예시입니다.
  ///
  /// ■ 등록 방법 (게임 초기화 코드에서 1회):
  ///   Registry.Registry.RegisterItemDefinition&lt;WoodBlock&gt;(WoodBlock.Identifier);
  ///
  /// ■ 인스턴스 생성:
  ///   var wood = Registry.Registry.CreateItemInstance(WoodBlock.Identifier) as WoodBlock;
  ///
  /// ■ 월드에 스폰:
  ///   ItemObject.Spawn(wood, spawnPosition);
  /// </summary>
  public class WoodBlock : Item
  {
    // ── Definitions ────────────────────────────────────────────────────────
    public const string Identifier    = "wood_block";
    public const string DisplayName   = "나무 블록";
    public const string Description   = "가공된 목재 블록입니다. 설치하면 블록이 됩니다.";
    public const string DetailComment = "";
    public const string Color         = "#8B5E3C";

    public const bool IsStackable   = true;
    public const int  MaxStackCount = 64;

    public const bool HasDurability           = true;
    public const bool EnabledDeltaDurability  = true;
    public const int  MaxDurability           = 30;
    public const int  DeltaDurabilityOnAttack = -2;
    public const int  DeltaDurabilityOnUse    = 0;

    public const float MinReach             = 0.5f;
    public const float MaxReach             = 3.0f;
    public const int   ItemDamage           = 2;
    public const bool  EnabledCooldown      = false;
    public const float CooldownMilliseconds = 0f;

    // ── 생성자 ────────────────────────────────────────────────────────────
    public WoodBlock() : base() { }

    // ── Handlers ─────────────────────────────────────────────────────────
    public override ActionResult OnAttack(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[WoodBlock] OnAttack — player={player?.name}");
      // TODO: target.TakeDamage(CurrentItemDamage);
      return ActionResult.Success;
    }

    public override ActionResult OnUse(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[WoodBlock] OnUse — player={player?.name}. 나무 블록 설치 시도.");
      return ActionResult.Success;
    }

    public override void OnGet(PlayerController player)
      => Debug.Log($"[WoodBlock] 획득 — player={player?.name}");

    public override void OnThrow(PlayerController player)
      => Debug.Log($"[WoodBlock] 던지기 — player={player?.name}");

    // ── Serialization ────────────────────────────────────────────────────
    // 파생 속성 없음 — 직렬화 불필요
    public override string GetCurrentSerializedDerivedAttributes()
      => string.Empty;

    public override void SetCurrentSerializedDerivedAttributes(string serialized) { }
  }
}
