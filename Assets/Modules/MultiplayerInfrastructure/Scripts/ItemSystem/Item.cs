using System;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Item;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 모든 아이템의 기반 추상 클래스입니다.
  ///
  /// ■ Definitions (정의 레이어)
  ///   파생 클래스에서 override하는 abstract/virtual 프로퍼티.
  ///   컴파일 타임에 결정되며, 인스턴스화 시 Instance 값의 초기값으로 사용됩니다.
  ///
  /// ■ Instance (상태 레이어)
  ///   런타임 중 변하는 현재 값들. 생성자에서 Definitions 값으로 초기화됩니다.
  ///   CurrentSerializedDerivedAttributes를 통해 파생 클래스 고유 상태를 직렬화/역직렬화합니다.
  ///
  /// ■ 주의
  ///   생성자에서 Definitions 프로퍼티를 읽으므로, 파생 클래스가 프로퍼티를
  ///   컴파일 타임 상수(=> "value")나 필드 초기화값으로 정의해야 합니다.
  ///   생성자 파라미터에 의존하는 프로퍼티 구현은 base() 호출 이후에 유효합니다.
  /// </summary>
  [Serializable]
  public abstract class Item
  {
    // =========================================================================
    // DEFINITIONS — 파생 클래스에서 override
    // =========================================================================

    #region Definitions/Commons
    public abstract string Identifier        { get; }
    public abstract string DisplayName       { get; }
    public abstract string Description       { get; }
    public abstract string DetailComment     { get; }
    /// <summary>HTML hex color 문자열. 예: "#FF8800" 또는 "white"</summary>
    public abstract string Color             { get; }
    #endregion

    #region Definitions/Stack
    public abstract bool IsStackable         { get; }
    public abstract int  MaxStackCount       { get; }
    #endregion

    #region Definitions/Durability
    public abstract bool HasDurability            { get; }
    public abstract bool EnabledDeltaDurability   { get; }
    public abstract int  MaxDurability            { get; }
    public abstract int  DeltaDurabilityOnAttack  { get; }
    public abstract int  DeltaDurabilityOnUse     { get; }
    #endregion

    #region Definitions/ItemUsing
    public abstract float MinReach              { get; }
    public abstract float MaxReach              { get; }
    public abstract int   ItemDamage            { get; }
    public abstract bool  EnabledCooldown       { get; }
    /// <summary>쿨다운 시간 (밀리초)</summary>
    public abstract float CooldownMilliseconds  { get; }
    #endregion

    #region Definitions/Instantiate
    /// <summary>
    /// 파생 클래스 고유 직렬화 속성(NBT)이 기본값에서 수정되었는지 나타냅니다.
    /// 스택을 겹치거나 대량 처리할 때 최적화 힌트로 사용됩니다.
    /// SetCurrentSerializedDerivedAttributes 호출 시 true로 전환됩니다.
    /// </summary>
    public bool IsModifiedCurrentSerializedDerivedAttributes { get; private set; }
    #endregion

    // =========================================================================
    // INSTANCE — 런타임 가변 상태
    // =========================================================================

    #region Instance/Commons
    public string          CurrentIdentifier       { get; protected set; }
    public string          CurrentDisplayName      { get; protected set; }
    public string          CurrentDescription      { get; protected set; }
    public string          CurrentDetailComment    { get; protected set; }
    public UnityEngine.Color CurrentColor          { get; protected set; }
    /// <summary>
    /// 아이템 아이콘 스프라이트. 기본값은 Identifier로 ItemSpriteRegistry를 통해 조회합니다.
    /// </summary>
    public Sprite          CurrentItemIconTexture  { get; protected set; }
    #endregion

    #region Instance/Stack
    public bool IsCurrentlyStackable     { get; protected set;  }
    public int  CurrentMaxStackCount     { get; protected set;  }
    public int  CurrentStackCount        { get; set; }
    #endregion

    #region Instance/Durability
    public bool HasCurrentDurability              { get; protected set; }
    public bool CurrentEnabledDeltaDurability     { get; protected set; }
    public int  CurrentMaxDurability              { get; protected set; }
    public int  CurrentDurability                 { get; set; }
    public int  CurrentDurabilityDeltaOnAttack    { get; protected set; }
    public int  CurrentDurabilityDeltaOnUse       { get; protected set; }
    #endregion

    #region Instance/ItemUsing
    public float CurrentMinReach                      { get; protected set; }
    public float CurrentMaxReach                      { get; protected set; }
    public int   CurrentItemDamage                    { get; protected set; }
    public bool  CurrentEnabledCooldown               { get; protected set; }
    public float CurrentCooldownMilliseconds          { get; protected set; }
    public float CurrentCooldownRemainingMilliseconds { get; set; }
    #endregion

    #region Instance/Inheritance
    /// <summary>
    /// 파생 클래스 고유 직렬화 데이터 (JSON 등).
    /// GetCurrentSerializedDerivedAttributes / SetCurrentSerializedDerivedAttributes 로 관리합니다.
    /// </summary>
    public string CurrentSerializedDerivedAttributes { get; protected set; }
    #endregion

    // =========================================================================
    // CONSTRUCTOR
    // =========================================================================

    /// <summary>
    /// Definitions 값으로 Instance를 초기화합니다.
    /// 파생 클래스에서 base() 를 반드시 호출하거나, parameterless 생성자를 선언하십시오.
    /// </summary>
    protected Item()
    {
      InitializeFromDefinitions();
    }

    /// <summary>
    /// Definitions → Instance 복사. 생성자 이후 리셋이 필요할 때도 재호출할 수 있습니다.
    /// </summary>
    protected void InitializeFromDefinitions()
    {
      // Commons
      CurrentIdentifier    = Identifier;
      CurrentDisplayName   = DisplayName;
      CurrentDescription   = Description;
      CurrentDetailComment = DetailComment;
      CurrentColor = UnityEngine.ColorUtility.TryParseHtmlString(Color, out var parsed)
        ? parsed
        : UnityEngine.Color.white;
      CurrentItemIconTexture = ItemSpriteRegistry.GetOrLoad(Identifier);

      // Stack
      IsCurrentlyStackable = IsStackable;
      CurrentMaxStackCount = IsStackable ? Mathf.Max(1, MaxStackCount) : 1;
      CurrentStackCount    = 1;

      // Durability
      HasCurrentDurability           = HasDurability;
      CurrentEnabledDeltaDurability  = EnabledDeltaDurability;
      CurrentMaxDurability           = HasDurability ? Mathf.Max(0, MaxDurability) : 0;
      CurrentDurability              = CurrentMaxDurability;
      CurrentDurabilityDeltaOnAttack = DeltaDurabilityOnAttack;
      CurrentDurabilityDeltaOnUse    = DeltaDurabilityOnUse;

      // ItemUsing
      CurrentMinReach             = MinReach;
      CurrentMaxReach             = MaxReach;
      CurrentItemDamage           = ItemDamage;
      CurrentEnabledCooldown      = EnabledCooldown;
      CurrentCooldownMilliseconds = CooldownMilliseconds;
      CurrentCooldownRemainingMilliseconds = 0f;

      // Inheritance
      IsModifiedCurrentSerializedDerivedAttributes = false;
      CurrentSerializedDerivedAttributes = GetCurrentSerializedDerivedAttributes();
    }

    // =========================================================================
    // HANDLERS — override하여 아이템 고유 동작 정의
    // =========================================================================

    /// <summary>플레이어가 인벤토리에 이 아이템을 추가할 때 호출됩니다.</summary>
    public virtual void OnGet(PlayerController player) { }

    /// <summary>플레이어가 이 아이템을 던질 때 호출됩니다.</summary>
    public virtual void OnThrow(PlayerController player) { }

    /// <summary>플레이어가 이 아이템으로 오브젝트를 상호작용할 때 호출됩니다.</summary>
    public virtual void OnInteract(PlayerController player, Entity.Entity target) { }

    /// <summary>플레이어가 이 아이템으로 공격할 때 호출됩니다.</summary>
    public virtual ActionResult OnAttack(PlayerController player, Entity.Entity target)
      => ActionResult.Success;

    /// <summary>플레이어가 이 아이템을 사용할 때 호출됩니다.</summary>
    public virtual ActionResult OnUse(PlayerController player, Entity.Entity target)
      => ActionResult.Success;

    // =========================================================================
    // INTERNAL — 시스템 내부 호출 전용
    // =========================================================================

    /// <summary>
    /// 공격 시 인게임 아이템 오브젝트에 애니메이션을 재생합니다.
    /// ItemObject가 존재할 때 OnAttack 처리 직후 호출됩니다.
    /// </summary>
    internal void PlayAnimationOnAttack(ItemObject itemObject)
      => itemObject?.TriggerAttackAnimation();

    /// <summary>
    /// 사용 시 인게임 아이템 오브젝트에 애니메이션을 재생합니다.
    /// ItemObject가 존재할 때 OnUse 처리 직후 호출됩니다.
    /// </summary>
    internal void PlayAnimationOnUse(ItemObject itemObject)
      => itemObject?.TriggerUseAnimation();

    // =========================================================================
    // INTERNAL/VIRTUAL — 반드시 override
    // =========================================================================

    /// <summary>
    /// 파생 클래스 고유 상태를 직렬화 문자열로부터 복원합니다.
    /// 역직렬화 후 IsModifiedCurrentSerializedDerivedAttributes를 true로 설정합니다.
    /// </summary>
    public abstract void SetCurrentSerializedDerivedAttributes(string serialized);

    /// <summary>
    /// 파생 클래스 고유 상태를 직렬화 문자열로 반환합니다.
    /// 고유 상태가 없는 아이템은 빈 문자열을 반환합니다.
    /// </summary>
    public abstract string GetCurrentSerializedDerivedAttributes();

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>SetCurrentSerializedDerivedAttributes 구현 내에서 호출하여 수정 플래그를 세웁니다.</summary>
    protected void MarkDerivedAttributesModified()
    {
      IsModifiedCurrentSerializedDerivedAttributes = true;
      CurrentSerializedDerivedAttributes = GetCurrentSerializedDerivedAttributes();
    }

    /// <summary>현재 스택 카운트가 유효한지 (0보다 크고 최대 스택 이하인지) 검사합니다.</summary>
    public bool IsValid()
      => !string.IsNullOrEmpty(CurrentIdentifier) && CurrentStackCount >= 0
         && (!HasCurrentDurability || CurrentDurability >= 0);

    /// <summary>같은 identifier를 가진 아이템과 스택이 가능한지 검사합니다.</summary>
    public bool CanStackWith(Item other)
    {
      if (other == null || !IsCurrentlyStackable || !other.IsCurrentlyStackable)
        return false;
      return string.Equals(CurrentIdentifier, other.CurrentIdentifier, StringComparison.Ordinal)
             && !IsModifiedCurrentSerializedDerivedAttributes
             && !other.IsModifiedCurrentSerializedDerivedAttributes;
    }

    /// <summary>
    /// other 스택을 가능한 범위 내에서 흡수합니다. 남은 용량은 other.CurrentStackCount에 남아 있습니다.
    /// </summary>
    public Item Merge(Item other)
    {
      if (!CanStackWith(other)) return other;
      int space = CurrentMaxStackCount - CurrentStackCount;
      int moved = Mathf.Min(space, other.CurrentStackCount);
      CurrentStackCount += moved;
      other.CurrentStackCount -= moved;
      return other;
    }

    /// <summary>
    /// 이 아이템의 얕은 카피를 반환합니다. 런타임 상태별로 복사됩니다.
    /// 깊은 복사가 필요한 파생 클래스는 override 해 주세요.
    /// </summary>
    public virtual Item Clone() => (Item)MemberwiseClone();

    public override string ToString()
      => $"[{GetType().Name}] id={CurrentIdentifier} count={CurrentStackCount}";
  }
}
