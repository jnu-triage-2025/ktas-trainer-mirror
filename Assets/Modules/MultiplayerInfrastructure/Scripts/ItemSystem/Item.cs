using System;
using System.Reflection;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 모든 아이템의 기반 추상 클래스입니다.
  ///
  /// ■ Definitions (정의 레이어)
  ///   파생 클래스에서 public const 필드로 선언합니다.
  ///   기반 클래스의 virtual 프로퍼티는 리플렉션으로 읽어오며, 인스턴스화 시 Instance 초기값으로 사용됩니다.
  ///
  ///   예)
  ///     public const string Identifier   = "my_item";
  ///     public const string DisplayName  = "My Item";
  ///     public const string Description  = "설명";
  ///
  ///   MyItem.Identifier 처럼 인스턴스 없이도 컴파일 타임 상수로 접근할 수 있습니다.
  ///   같은 이름의 const를 자식 클래스에서 다시 선언하면 부모 값을 숨깁니다 (new 권고).
  ///
  /// ■ Instance (상태 레이어)
  ///   런타임 중 변하는 현재 값들. 생성자에서 Definitions 값으로 초기화됩니다.
  ///   CurrentSerializedDerivedAttributes를 통해 파생 클래스 고유 상태를 직렬화/역직렬화합니다.
  /// </summary>
  [Serializable]
  public abstract class Item
  {
    // =========================================================================
    // DEFINITIONS — 파생 클래스에서 public const 필드로 선언
    //
    // base 클래스의 virtual 프로퍼티가 리플렉션으로 자신의 Type에서 const 값을 읽어옵니다.
    // 중간 계층(예: MedicalItem)의 const 값은 leaf 클래스가 같은 이름의 const를 선언하지 않으면
    // FlattenHierarchy 탐색으로 자동 사용됩니다. 재정의 시 'new' 한정자를 권고합니다.
    // =========================================================================

    private static readonly BindingFlags ConstFlags =
      BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    private string ReadConstString(string name)
    {
      var f = GetType().GetField(name, ConstFlags);
      return f is not null ? (string)f.GetValue(null) : string.Empty;
    }

    private T ReadConst<T>(string name) where T : struct
    {
      var f = GetType().GetField(name, ConstFlags);
      return f is not null ? (T)f.GetValue(null) : default;
    }

    #region Definitions/Commons
    public virtual string Identifier        => ReadConstString(nameof(Identifier));
    public virtual string DisplayName       => ReadConstString(nameof(DisplayName));
    public virtual string Description       => ReadConstString(nameof(Description));
    public virtual string DetailComment     => ReadConstString(nameof(DetailComment));
    /// <summary>HTML hex color 문자열. 예: "#FF8800" 또는 "white"</summary>
    public virtual string Color             => ReadConstString(nameof(Color));
    #endregion

    #region Definitions/Stack
    public virtual bool IsStackable         => ReadConst<bool>(nameof(IsStackable));
    public virtual int  MaxStackCount       => ReadConst<int>(nameof(MaxStackCount));
    #endregion

    #region Definitions/Durability
    public virtual bool HasDurability            => ReadConst<bool>(nameof(HasDurability));
    public virtual bool EnabledDeltaDurability   => ReadConst<bool>(nameof(EnabledDeltaDurability));
    public virtual int  MaxDurability            => ReadConst<int>(nameof(MaxDurability));
    public virtual int  DeltaDurabilityOnAttack  => ReadConst<int>(nameof(DeltaDurabilityOnAttack));
    public virtual int  DeltaDurabilityOnUse     => ReadConst<int>(nameof(DeltaDurabilityOnUse));
    #endregion

    #region Definitions/ItemUsing
    public virtual float MinReach              => ReadConst<float>(nameof(MinReach));
    public virtual float MaxReach              => ReadConst<float>(nameof(MaxReach));
    public virtual int   ItemDamage            => ReadConst<int>(nameof(ItemDamage));
    public virtual bool  EnabledCooldown       => ReadConst<bool>(nameof(EnabledCooldown));
    /// <summary>쿨다운 시간 (밀리초)</summary>
    public virtual float CooldownMilliseconds  => ReadConst<float>(nameof(CooldownMilliseconds));
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
    /// 아이템 아이콘 스프라이트. 기본값은 Identifier로 Registry.GetOrLoadIconSprite 를 통해 조회합니다.
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

    #region Instance/Acquisition
    /// <summary>
    /// 획득 훅(<see cref="OnGet"/>)이 아직 발행되지 않은 "지연 획득" 상태인지 여부.
    /// 조합 결과처럼 아이템이 먼저 커서(held item)로 지급되어 인벤토리 배치 시
    /// <see cref="Player.PlayerController.TryAddItemToInventory"/> 를 거치지 않는 경우,
    /// 실제 인벤토리에 진입한 시점에 OnGet 을 발행하도록 인벤토리 컨트롤러가 이 플래그를 참고한다.
    /// 월드 습득 등 TryAddItemToInventory 경로에서는 OnGet 호출과 함께 즉시 해제된다.
    /// </summary>
    public bool DeferredOnGet { get; set; }
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
      CurrentItemIconTexture = Registry.Registry.GetOrLoadIconSprite(Identifier, GetType());

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
      if (!string.Equals(CurrentIdentifier, other.CurrentIdentifier, StringComparison.Ordinal)
          || IsModifiedCurrentSerializedDerivedAttributes
          || other.IsModifiedCurrentSerializedDerivedAttributes)
        return false;

      // 한 스택은 사용 중인 아이템 한 개의 내구도만 표현할 수 있다. 손상된 스택끼리 합치면
      // 둘 중 하나의 손상 상태를 보존할 수 없으므로 병합하지 않는다.
      return !IsDurabilityDamaged() || !other.IsDurabilityDamaged();
    }

    /// <summary>
    /// other 스택을 가능한 범위 내에서 흡수합니다. 남은 용량은 other.CurrentStackCount에 남아 있습니다.
    /// </summary>
    public Item Merge(Item other)
    {
      if (!CanStackWith(other)) return other;
      int space = CurrentMaxStackCount - CurrentStackCount;
      int moved = Mathf.Min(space, other.CurrentStackCount);
      if (moved <= 0) return other;

      bool receiveDamagedItem = !IsDurabilityDamaged() && other.IsDurabilityDamaged();
      CurrentStackCount += moved;
      other.CurrentStackCount -= moved;

      // 대상 스택이 온전하고 들어오는 스택만 손상되었다면, 손상된 현재 아이템을 먼저 옮긴다.
      if (receiveDamagedItem)
      {
        CurrentDurability = other.CurrentDurability;
        if (other.CurrentStackCount > 0)
          other.CurrentDurability = other.CurrentMaxDurability;
      }
      return other;
    }

    private bool IsDurabilityDamaged()
      => HasCurrentDurability
         && CurrentMaxDurability > 0
         && CurrentDurability < CurrentMaxDurability;

    /// <summary>
    /// 이 아이템의 얕은 카피를 반환합니다. 런타임 상태별로 복사됩니다.
    /// 깊은 복사가 필요한 파생 클래스는 override 해 주세요.
    /// </summary>
    public virtual Item Clone() => (Item)MemberwiseClone();

    /// <summary>사용 시 정의된 내구도 변화량을 적용하고, 소진 여부를 반환합니다.</summary>
    public bool TryApplyDurabilityOnUse(out bool depleted)
    {
      depleted = false;
      if (!HasCurrentDurability || !CurrentEnabledDeltaDurability)
        return false;

      CurrentDurability = Mathf.Clamp(
        CurrentDurability + CurrentDurabilityDeltaOnUse,
        0,
        CurrentMaxDurability);
      depleted = CurrentDurability <= 0;
      return true;
    }

    /// <summary>
    /// 사용 내구도를 적용하고 내구도가 소진되면 현재 스택에서 아이템 하나를 제거합니다.
    /// 스택이 남아 있으면 다음 아이템의 내구도를 최대치로 초기화합니다.
    /// </summary>
    public bool TryConsumeDurabilityOnUse(out bool stackDepleted)
    {
      stackDepleted = false;
      if (!TryApplyDurabilityOnUse(out bool durabilityDepleted))
        return false;

      if (!durabilityDepleted)
        return true;

      CurrentStackCount = Mathf.Max(0, CurrentStackCount - 1);
      stackDepleted = CurrentStackCount <= 0;
      if (!stackDepleted)
        CurrentDurability = CurrentMaxDurability;
      return true;
    }

    public override string ToString()
      => $"[{GetType().Name}] id={CurrentIdentifier} count={CurrentStackCount}";
  }
}
