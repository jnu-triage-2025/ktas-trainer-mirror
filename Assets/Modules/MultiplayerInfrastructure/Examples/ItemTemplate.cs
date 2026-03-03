using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

// ============================================================================
// 아이템 구현 템플릿
//
// 사용 방법:
//   1. 이 파일을 복사하여 새 파일로 저장합니다.
//   2. 클래스명 TemplateItem 을 원하는 이름으로 변경합니다.
//   3. const 값들을 채워 넣습니다.
//   4. 필요한 핸들러만 override로 구현하고 나머지는 삭제합니다.
//   5. 파생 속성이 없다면 Serialization 섹션 전체를 삭제합니다.
//   6. Registry에 등록합니다:
//        Registry.Registry.RegisterItemDefinition<TemplateItem>(TemplateItem.Identifier);
// ============================================================================

namespace MultiplayerInfrastructure.ItemSystem.Examples
{
  /// <summary>
  /// [TODO: 아이템 설명 작성]
  ///
  /// ■ 등록 (게임 초기화 코드에서 1회):
  ///   Registry.Registry.RegisterItemDefinition&lt;TemplateItem&gt;(TemplateItem.Identifier);
  ///
  /// ■ 인스턴스 생성:
  ///   var item = Registry.Registry.CreateItemInstance(TemplateItem.Identifier) as TemplateItem;
  ///
  /// ■ 월드에 스폰:
  ///   ItemObject.Spawn(item, spawnPosition);
  /// </summary>
  public class TemplateItem : Item
  {
    // ── Definitions/Commons ──────────────────────────────────────────────
    public const string Identifier    = "template_item";   // 레지스트리 등록 키와 일치시킬 것
    public const string DisplayName   = "템플릿 아이템";
    public const string Description   = "아이템 설명.";
    public const string DetailComment = "";                 // 상세 설명 (비워도 됨)
    public const string Color         = "white";            // HTML hex (#RRGGBB) 또는 색상 이름

    // ── Definitions/Stack ────────────────────────────────────────────────
    public const bool IsStackable   = false;
    public const int  MaxStackCount = 1;                    // IsStackable = false 면 사용되지 않음

    // ── Definitions/Durability ───────────────────────────────────────────
    public const bool HasDurability           = false;
    public const bool EnabledDeltaDurability  = false;
    public const int  MaxDurability           = 0;
    public const int  DeltaDurabilityOnAttack = 0;
    public const int  DeltaDurabilityOnUse    = 0;

    // ── Definitions/ItemUsing ────────────────────────────────────────────
    public const float MinReach             = 0f;
    public const float MaxReach             = 2.5f;
    public const int   ItemDamage           = 0;
    public const bool  EnabledCooldown      = false;
    public const float CooldownMilliseconds = 0f;

    // ── 파생 속성 (인스턴스별 가변 상태) ─────────────────────────────────
    // 파생 속성이 없다면 이 섹션과 아래 Serialization 섹션을 모두 삭제하세요.
    // public int SomeValue { get; set; } = 0;

    // ── 생성자 ────────────────────────────────────────────────────────────
    public TemplateItem() : base() { }

    // ── Handlers ─────────────────────────────────────────────────────────
    // 필요한 핸들러만 남기고 나머지는 삭제하세요.
    // 기본 동작(Success 반환 / 로그 없음)이 아닌 경우에만 구현이 필요합니다.

    /// <summary>아이템을 주울 때 호출됩니다.</summary>
    public override void OnGet(PlayerController player)
    {
      Debug.Log($"[TemplateItem] 획득 — player={player?.name}");
    }

    /// <summary>아이템을 버리거나 던질 때 호출됩니다.</summary>
    public override void OnThrow(PlayerController player)
    {
      Debug.Log($"[TemplateItem] 버리기/던지기 — player={player?.name}");
    }

    /// <summary>좌클릭(공격) 시 호출됩니다.</summary>
    public override ActionResult OnAttack(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[TemplateItem] OnAttack — player={player?.name}, target={target?.GetType().Name}");
      return ActionResult.Success;
    }

    /// <summary>우클릭(사용) 시 호출됩니다.</summary>
    public override ActionResult OnUse(PlayerController player, Entity.Entity target)
    {
      Debug.Log($"[TemplateItem] OnUse — player={player?.name}, target={target?.GetType().Name}");
      return ActionResult.Success;
    }

    // ── Serialization ────────────────────────────────────────────────────
    // 파생 속성(위 섹션)이 없다면 이 섹션 전체를 삭제하세요.
    // 파생 속성의 기본값이면 빈 문자열을 반환해 스택 병합을 허용합니다.

    public override string GetCurrentSerializedDerivedAttributes()
    {
      // 파생 속성이 기본값이면 string.Empty 반환
      // return SomeValue == 0 ? string.Empty : $"{{\"SomeValue\":{SomeValue}}}";
      return string.Empty;
    }

    public override void SetCurrentSerializedDerivedAttributes(string serialized)
    {
      if (string.IsNullOrWhiteSpace(serialized)) return;
      // try
      // {
      //   var data = JsonUtility.FromJson<SerializedData>(serialized);
      //   SomeValue = data.SomeValue;
      //   MarkDerivedAttributesModified();
      // }
      // catch
      // {
      //   Debug.LogWarning($"[TemplateItem] NBT 파싱 실패: {serialized}");
      // }
    }

    // [System.Serializable]
    // private struct SerializedData
    // {
    //   public int SomeValue;
    // }
  }
}
