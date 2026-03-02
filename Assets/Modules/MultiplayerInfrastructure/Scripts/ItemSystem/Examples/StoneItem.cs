using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Item;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem.Examples
{
  /// <summary>
  /// 돌 아이템 구현 예시입니다.
  ///
  /// ■ 등록 방법 (게임 초기화 코드에서 1회):
  ///   Registry.Registry.RegisterItemDefinition&lt;StoneItem&gt;("stone");
  ///
  /// ■ 인스턴스 생성:
  ///   var stone = Registry.Registry.CreateItemInstance("stone") as StoneItem;
  ///
  /// ■ 월드에 스폰:
  ///   ItemObject.Spawn(stone, spawnPosition);
  /// </summary>
  public class StoneItem : Item
  {
    // ── Definitions ────────────────────────────────────────────────────────
    public override string Identifier    => "stone";
    public override string DisplayName   => "돌";
    public override string Description   => "단단한 돌덩이입니다.";
    public override string DetailComment => "";
    public override string Color         => "#9E9E9E";

    public override bool IsStackable   => true;
    public override int  MaxStackCount => 64;

    public override bool HasDurability           => false;
    public override bool EnabledDeltaDurability  => false;
    public override int  MaxDurability           => 0;
    public override int  DeltaDurabilityOnAttack => 0;
    public override int  DeltaDurabilityOnUse    => 0;

    public override float MinReach             => 0.5f;
    public override float MaxReach             => 3.0f;
    public override int   ItemDamage           => 5;
    public override bool  EnabledCooldown      => false;
    public override float CooldownMilliseconds => 0f;

    // ── 고유 파생 속성 ────────────────────────────────────────────────────
    /// <summary>충격 공격 추가 데미지</summary>
    public int ImpactDamage { get; set; } = 5;

    // ── 생성자 ────────────────────────────────────────────────────────────
    public StoneItem() : base() { }

    // ── Handlers ─────────────────────────────────────────────────────────
    public override ActionResult OnAttack(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[StoneItem] OnAttack — player={player?.name}, impactDamage={ImpactDamage}");
      // TODO: target.TakeDamage(CurrentItemDamage + ImpactDamage);
      return ActionResult.Success;
    }

    public override ActionResult OnUse(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[StoneItem] OnUse — player={player?.name}. 돌 블록 설치 시도.");
      return ActionResult.Success;
    }

    public override void OnGet(PlayerController player)
      => Debug.Log($"[StoneItem] 획득 — player={player?.name}");

    public override void OnThrow(PlayerController player)
      => Debug.Log($"[StoneItem] 던지기 — player={player?.name}");

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
        var wrapper = JsonUtility.FromJson<StoneItemSerializedData>(serialized);
        ImpactDamage = wrapper.ImpactDamage;
        MarkDerivedAttributesModified();
      }
      catch
      {
        Debug.LogWarning($"[StoneItem] 직렬화 데이터 파싱 실패: {serialized}");
      }
    }

    [System.Serializable]
    private struct StoneItemSerializedData
    {
      public int ImpactDamage;
    }
  }
}
