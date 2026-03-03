using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.Examples
{
  /// <summary>
  /// 붕대 아이템 구현 예시입니다.
  ///
  /// ■ 등록 (게임 초기화 코드에서 1회):
  ///   Registry.Registry.RegisterItemDefinition&lt;BandageItem&gt;("bandage");
  ///
  /// ■ 인스턴스 생성:
  ///   var item = Registry.Registry.CreateItemInstance("bandage");
  ///   item.CurrentStackCount = 3;
  ///
  /// ■ 인벤토리에 추가:
  ///   playerController.TryAddItemToInventory(item);
  ///
  /// ■ 월드에 드롭:
  ///   ItemObject.Spawn(item, spawnPosition);
  ///
  /// ■ 파생 속성(HealAmount)이 기본값(30)과 다를 때만 NBT 직렬화됩니다.
  ///   스택 병합(CanStackWith)은 NBT가 수정되지 않은 인스턴스끼리만 허용됩니다.
  /// </summary>
  public class BandageItem : Item
  {
    // ── Definitions ──────────────────────────────────────────────────────
    public override string Identifier    => "bandage";
    public override string DisplayName   => "붕대";
    public override string Description   => "지혈 및 상처 보호에 사용합니다.";
    public override string DetailComment => "기본 회복량: 30 HP";
    public override string Color         => "#FFFFFF";

    public override bool IsStackable   => true;
    public override int  MaxStackCount => 10;

    public override bool HasDurability           => false;
    public override bool EnabledDeltaDurability  => false;
    public override int  MaxDurability           => 0;
    public override int  DeltaDurabilityOnAttack => 0;
    public override int  DeltaDurabilityOnUse    => 0;

    public override float MinReach             => 0f;
    public override float MaxReach             => 1.5f;
    public override int   ItemDamage           => 0;
    public override bool  EnabledCooldown      => true;
    public override float CooldownMilliseconds => 3000f;   // 3초 쿨다운

    // ── 파생 속성 (인스턴스별 고유 상태) ─────────────────────────────────
    /// <summary>사용 시 회복량 (HP). 기본값 30에서 변경하면 NBT로 직렬화됩니다.</summary>
    public int HealAmount { get; set; } = 30;

    // ── 생성자 ────────────────────────────────────────────────────────────
    public BandageItem() : base() { }

    // ── Handlers ─────────────────────────────────────────────────────────

    public override void OnGet(PlayerController player)
      => Debug.Log($"[BandageItem] 획득 — player={player?.name}");

    public override void OnThrow(PlayerController player)
      => Debug.Log($"[BandageItem] 버리기 — player={player?.name}");

    /// <summary>붕대 사용 — target이 null이면 자기 자신에게 사용합니다.</summary>
    public override ActionResult OnUse(PlayerController player, Entity.Entity target)
    {
      var healTarget = target ?? player?.PlayerEntity;
      if (healTarget == null)
        return ActionResult.Cancelled;

      // TODO: healTarget.Heal(HealAmount);
      Debug.Log($"[BandageItem] OnUse — player={player?.name}, target={healTarget.GetType().Name}, heal={HealAmount}");
      return ActionResult.Success;
    }

    // ── 직렬화 ───────────────────────────────────────────────────────────
    /// <summary>기본값(30)이면 직렬화를 생략해 스택 병합을 허용합니다.</summary>
    public override string GetCurrentSerializedDerivedAttributes()
      => HealAmount == 30 ? string.Empty : $"{{\"HealAmount\":{HealAmount}}}";

    public override void SetCurrentSerializedDerivedAttributes(string serialized)
    {
      if (string.IsNullOrWhiteSpace(serialized)) return;
      try
      {
        var data = JsonUtility.FromJson<SerializedData>(serialized);
        HealAmount = data.HealAmount;
        MarkDerivedAttributesModified();
      }
      catch
      {
        Debug.LogWarning($"[BandageItem] NBT 파싱 실패: {serialized}");
      }
    }

    [System.Serializable]
    private struct SerializedData
    {
      public int HealAmount;
    }
  }
}
