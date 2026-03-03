using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// TriageTrainer 의료 아이템 공통 기반 클래스입니다.
  ///
  /// 파생 클래스는 Identifier, DisplayName, Description 을 반드시 구현합니다.
  /// 특별한 동작이 필요한 경우 OnGet / OnUse / OnAttack 등을 override합니다.
  /// </summary>
  public abstract class MedicalItem : Item
  {
    // ── Definitions/Commons ──────────────────────────────────────────────
    // Identifier, DisplayName, Description 은 파생 클래스에서 구현
    public override string DetailComment => "";
    public override string Color         => "white";

    // ── Definitions/Stack ────────────────────────────────────────────────
    public override bool IsStackable   => true;
    public override int  MaxStackCount => 64;

    // ── Definitions/Durability ───────────────────────────────────────────
    public override bool HasDurability           => false;
    public override bool EnabledDeltaDurability  => false;
    public override int  MaxDurability           => 1;
    public override int  DeltaDurabilityOnAttack => 0;
    public override int  DeltaDurabilityOnUse    => 0;

    // ── Definitions/ItemUsing ────────────────────────────────────────────
    public override float MinReach             => 1.0f;
    public override float MaxReach             => 2.5f;
    public override int   ItemDamage           => 0;
    public override bool  EnabledCooldown      => false;
    public override float CooldownMilliseconds => 0f;

    // ── 생성자 ───────────────────────────────────────────────────────────
    protected MedicalItem() : base() { }

    // ── Handlers (기본 no-op) ─────────────────────────────────────────────
    public override ActionResult OnUse(PlayerController player, MI.Entity.Entity target)
      => ActionResult.Success;

    public override ActionResult OnAttack(PlayerController player, MI.Entity.Entity target)
      => ActionResult.Success;

    // ── Serialization (파생 속성 없음) ────────────────────────────────────
    public override string GetCurrentSerializedDerivedAttributes()
      => string.Empty;

    public override void SetCurrentSerializedDerivedAttributes(string serialized) { }
  }
}
