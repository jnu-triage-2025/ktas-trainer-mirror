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
    // Identifier, DisplayName, Description 은 파생 클래스에서 const 선언
    public const string DetailComment = "";
    public const string Color         = "white";

    // ── Definitions/Stack ────────────────────────────────────────────────
    public const bool IsStackable   = true;
    public const int  MaxStackCount = 64;

    // ── Definitions/Durability ───────────────────────────────────────────
    public const bool HasDurability           = false;
    public const bool EnabledDeltaDurability  = false;
    public const int  MaxDurability           = 1;
    public const int  DeltaDurabilityOnAttack = 0;
    public const int  DeltaDurabilityOnUse    = 0;

    // ── Definitions/ItemUsing ────────────────────────────────────────────
    public const float MinReach             = 1.0f;
    public const float MaxReach             = 2.5f;
    public const int   ItemDamage           = 0;
    public const bool  EnabledCooldown      = false;
    public const float CooldownMilliseconds = 0f;

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
