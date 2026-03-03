using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem.Examples
{
  /// <summary>
  /// 돌 블록 아이템 구현 예시입니다.
  ///
  /// ■ 등록 방법 (게임 초기화 코드에서 1회):
  ///   Registry.Registry.RegisterItemDefinition&lt;StoneBlock&gt;(StoneBlock.Identifier);
  ///
  /// ■ 인스턴스 생성:
  ///   var stone = Registry.Registry.CreateItemInstance(StoneBlock.Identifier) as StoneBlock;
  ///
  /// ■ 월드에 스폰:
  ///   ItemObject.Spawn(stone, spawnPosition);
  /// </summary>
  public class StoneBlock : Item
  {
    // ── Definitions ────────────────────────────────────────────────────────
    public const string Identifier    = "stone_block";
    public const string DisplayName   = "돌 블록";
    public const string Description   = "단단한 돌덩이입니다. 설치하면 블록이 됩니다.";
    public const string DetailComment = "";
    public const string Color         = "#9E9E9E";

    public const bool IsStackable   = true;
    public const int  MaxStackCount = 64;

    public const bool HasDurability           = false;
    public const bool EnabledDeltaDurability  = false;
    public const int  MaxDurability           = 0;
    public const int  DeltaDurabilityOnAttack = 0;
    public const int  DeltaDurabilityOnUse    = 0;

    public const float MinReach             = 0.5f;
    public const float MaxReach             = 3.0f;
    public const int   ItemDamage           = 5;
    public const bool  EnabledCooldown      = false;
    public const float CooldownMilliseconds = 0f;

    // ── 고유 파생 속성 ────────────────────────────────────────────────────
    /// <summary>충격 공격 추가 데미지</summary>
    public int ImpactDamage { get; set; } = 5;

    // ── 생성자 ────────────────────────────────────────────────────────────
    public StoneBlock() : base() { }

    // ── Handlers ─────────────────────────────────────────────────────────
    public override ActionResult OnAttack(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[StoneBlock] OnAttack — player={player?.name}, impactDamage={ImpactDamage}");
      // TODO: target.TakeDamage(CurrentItemDamage + ImpactDamage);
      return ActionResult.Success;
    }

    public override ActionResult OnUse(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[StoneBlock] OnUse — player={player?.name}. 돌 블록 설치 시도.");
      return ActionResult.Success;
    }

    public override void OnGet(PlayerController player)
      => Debug.Log($"[StoneBlock] 획득 — player={player?.name}");

    public override void OnThrow(PlayerController player)
      => Debug.Log($"[StoneBlock] 던지기 — player={player?.name}");

    // ── Serialization ────────────────────────────────────────────────────
    public override string GetCurrentSerializedDerivedAttributes()
      => ImpactDamage == 5
        ? string.Empty  // 기본값이면 직렬화 생략 (IsModifiedCurrentSerializedDerivedAttributes = false 유지)
        : $"{{\"ImpactDamage\":{ImpactDamage}}}";

    public override void SetCurrentSerializedDerivedAttributes(string serialized)
    {
      if (string.IsNullOrWhiteSpace(serialized)) return;
      // 간단한 수동 파싱 (JsonUtility 사용도 가능)
      try
      {
        var wrapper = JsonUtility.FromJson<StoneBlockSerializedData>(serialized);
        ImpactDamage = wrapper.ImpactDamage;
        MarkDerivedAttributesModified();
      }
      catch
      {
        Debug.LogWarning($"[StoneBlock] 직렬화 데이터 파싱 실패: {serialized}");
      }
    }

    [System.Serializable]
    private struct StoneBlockSerializedData
    {
      public int ImpactDamage;
    }
  }
}
