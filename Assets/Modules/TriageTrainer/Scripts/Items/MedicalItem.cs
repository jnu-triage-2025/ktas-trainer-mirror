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
    public new const string DetailComment = "";
    public new const string Color = "white";

    // ── Definitions/Stack ────────────────────────────────────────────────
    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;

    // ── Definitions/Durability ───────────────────────────────────────────
    public new const bool HasDurability = false;
    public new const bool EnabledDeltaDurability = false;
    public new const int MaxDurability = 1;
    public new const int DeltaDurabilityOnAttack = 0;
    public new const int DeltaDurabilityOnUse = 0;

    // ── Definitions/ItemUsing ────────────────────────────────────────────
    public new const float MinReach = 1.0f;
    public new const float MaxReach = 2.5f;
    public new const int ItemDamage = 0;
    public new const bool EnabledCooldown = false;
    public new const float CooldownMilliseconds = 0f;

    // ── 생성자 ───────────────────────────────────────────────────────────
    protected MedicalItem() : base() { }

    // ── Handlers (기본 no-op) ─────────────────────────────────────────────

    /// <summary>
    /// 아이템 획득(인벤토리 추가) 완료 시 시나리오 게이팅용 완료 신호(sig.*)를 올린다.
    /// 아이템 식별자 자체를 신호로 사용하므로(=sig.&lt;identifier&gt; 및 sig.click_&lt;identifier&gt;),
    /// 시나리오 조건명을 아이템 식별자에 맞추면 별도 코드 없이 획득 게이트가 통과된다.
    ///
    /// 주의: 일부 시나리오 조건명(예: click_glove, click_et_tube, click_ns1)은 아이템 식별자
    /// (sterile_gloves, endotracheal_tube, normal_saline_1000ml)와 표기가 다르다. 이 불일치 목록과
    /// 처리 방침은 interaction-signal-integration-spec.md 의 "아이템 식별자 ↔ 조건명 정합" 절 참조.
    /// </summary>
    public override void OnGet(PlayerController player)
    {
      base.OnGet(player);

      string id = CurrentIdentifier;
      if (string.IsNullOrWhiteSpace(id))
      {
        return;
      }

      MI.Scenario.ScenarioInteractionSignals.Raise(id);
      MI.Scenario.ScenarioInteractionSignals.Raise("click_" + id);
      ChecklistPaper.NotifyItemAcquired(player, id);
    }

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
