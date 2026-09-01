# <a id="MultiplayerInfrastructure_ItemSystem_Item"></a> Class Item

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

모든 아이템의 기반 추상 클래스입니다.

■ Definitions (정의 레이어)
  파생 클래스에서 public const 필드로 선언합니다.
  기반 클래스의 virtual 프로퍼티는 리플렉션으로 읽어오며, 인스턴스화 시 Instance 초기값으로 사용됩니다.

  예)
    public const string Identifier   = "my_item";
    public const string DisplayName  = "My Item";
    public const string Description  = "설명";

  MyItem.Identifier 처럼 인스턴스 없이도 컴파일 타임 상수로 접근할 수 있습니다.
  같은 이름의 const를 자식 클래스에서 다시 선언하면 부모 값을 숨깁니다 (new 권고).

■ Instance (상태 레이어)
  런타임 중 변하는 현재 값들. 생성자에서 Definitions 값으로 초기화됩니다.
  CurrentSerializedDerivedAttributes를 통해 파생 클래스 고유 상태를 직렬화/역직렬화합니다.

```csharp
[Serializable]
public abstract class Item
```

#### Inheritance

object ← 
[Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Derived

[MedicalItem](TriageTrainer.ItemDefinitions.MedicalItem.md), 
[StoneBlock](MultiplayerInfrastructure.ItemSystem.Examples.StoneBlock.md), 
[TemplateItem](MultiplayerInfrastructure.ItemSystem.Examples.TemplateItem.md), 
[WoodBlock](MultiplayerInfrastructure.ItemSystem.Examples.WoodBlock.md)

## Constructors

### <a id="MultiplayerInfrastructure_ItemSystem_Item__ctor"></a> Item\(\)

Definitions 값으로 Instance를 초기화합니다.
파생 클래스에서 base() 를 반드시 호출하거나, parameterless 생성자를 선언하십시오.

```csharp
protected Item()
```

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_Item_Color"></a> Color

HTML hex color 문자열. 예: "#FF8800" 또는 "white"

```csharp
public virtual string Color { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CooldownMilliseconds"></a> CooldownMilliseconds

쿨다운 시간 (밀리초)

```csharp
public virtual float CooldownMilliseconds { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentColor"></a> CurrentColor

```csharp
public Color CurrentColor { get; protected set; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentCooldownMilliseconds"></a> CurrentCooldownMilliseconds

```csharp
public float CurrentCooldownMilliseconds { get; protected set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentCooldownRemainingMilliseconds"></a> CurrentCooldownRemainingMilliseconds

```csharp
public float CurrentCooldownRemainingMilliseconds { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentDescription"></a> CurrentDescription

```csharp
public string CurrentDescription { get; protected set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentDetailComment"></a> CurrentDetailComment

```csharp
public string CurrentDetailComment { get; protected set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentDisplayName"></a> CurrentDisplayName

```csharp
public string CurrentDisplayName { get; protected set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentDurability"></a> CurrentDurability

```csharp
public int CurrentDurability { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentDurabilityDeltaOnAttack"></a> CurrentDurabilityDeltaOnAttack

```csharp
public int CurrentDurabilityDeltaOnAttack { get; protected set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentDurabilityDeltaOnUse"></a> CurrentDurabilityDeltaOnUse

```csharp
public int CurrentDurabilityDeltaOnUse { get; protected set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentEnabledCooldown"></a> CurrentEnabledCooldown

```csharp
public bool CurrentEnabledCooldown { get; protected set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentEnabledDeltaDurability"></a> CurrentEnabledDeltaDurability

```csharp
public bool CurrentEnabledDeltaDurability { get; protected set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentIdentifier"></a> CurrentIdentifier

```csharp
public string CurrentIdentifier { get; protected set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentItemDamage"></a> CurrentItemDamage

```csharp
public int CurrentItemDamage { get; protected set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentItemIconTexture"></a> CurrentItemIconTexture

아이템 아이콘 스프라이트. 기본값은 Identifier로 Registry.GetOrLoadIconSprite 를 통해 조회합니다.

```csharp
public Sprite CurrentItemIconTexture { get; protected set; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentMaxDurability"></a> CurrentMaxDurability

```csharp
public int CurrentMaxDurability { get; protected set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentMaxReach"></a> CurrentMaxReach

```csharp
public float CurrentMaxReach { get; protected set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentMaxStackCount"></a> CurrentMaxStackCount

```csharp
public int CurrentMaxStackCount { get; protected set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentMinReach"></a> CurrentMinReach

```csharp
public float CurrentMinReach { get; protected set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentSerializedDerivedAttributes"></a> CurrentSerializedDerivedAttributes

파생 클래스 고유 직렬화 데이터 (JSON 등).
GetCurrentSerializedDerivedAttributes / SetCurrentSerializedDerivedAttributes 로 관리합니다.

```csharp
public string CurrentSerializedDerivedAttributes { get; protected set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CurrentStackCount"></a> CurrentStackCount

```csharp
public int CurrentStackCount { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_DeferredOnGet"></a> DeferredOnGet

획득 훅(<xref href="MultiplayerInfrastructure.ItemSystem.Item.OnGet(MultiplayerInfrastructure.Player.PlayerController)" data-throw-if-not-resolved="false"></xref>)이 아직 발행되지 않은 "지연 획득" 상태인지 여부.
조합 결과처럼 아이템이 먼저 커서(held item)로 지급되어 인벤토리 배치 시
<xref href="MultiplayerInfrastructure.Player.PlayerController.TryAddItemToInventory(MultiplayerInfrastructure.ItemSystem.Item)" data-throw-if-not-resolved="false"></xref> 를 거치지 않는 경우,
실제 인벤토리에 진입한 시점에 OnGet 을 발행하도록 인벤토리 컨트롤러가 이 플래그를 참고한다.
월드 습득 등 TryAddItemToInventory 경로에서는 OnGet 호출과 함께 즉시 해제된다.

```csharp
public bool DeferredOnGet { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_DeltaDurabilityOnAttack"></a> DeltaDurabilityOnAttack

```csharp
public virtual int DeltaDurabilityOnAttack { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_DeltaDurabilityOnUse"></a> DeltaDurabilityOnUse

```csharp
public virtual int DeltaDurabilityOnUse { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_Description"></a> Description

```csharp
public virtual string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_DetailComment"></a> DetailComment

```csharp
public virtual string DetailComment { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_DisplayName"></a> DisplayName

```csharp
public virtual string DisplayName { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_EnabledCooldown"></a> EnabledCooldown

```csharp
public virtual bool EnabledCooldown { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_EnabledDeltaDurability"></a> EnabledDeltaDurability

```csharp
public virtual bool EnabledDeltaDurability { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_HasCurrentDurability"></a> HasCurrentDurability

```csharp
public bool HasCurrentDurability { get; protected set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_HasDurability"></a> HasDurability

```csharp
public virtual bool HasDurability { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_Identifier"></a> Identifier

```csharp
public virtual string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_IsCurrentlyStackable"></a> IsCurrentlyStackable

```csharp
public bool IsCurrentlyStackable { get; protected set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_IsModifiedCurrentSerializedDerivedAttributes"></a> IsModifiedCurrentSerializedDerivedAttributes

파생 클래스 고유 직렬화 속성(NBT)이 기본값에서 수정되었는지 나타냅니다.
스택을 겹치거나 대량 처리할 때 최적화 힌트로 사용됩니다.
SetCurrentSerializedDerivedAttributes 호출 시 true로 전환됩니다.

```csharp
public bool IsModifiedCurrentSerializedDerivedAttributes { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_IsStackable"></a> IsStackable

```csharp
public virtual bool IsStackable { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_ItemDamage"></a> ItemDamage

```csharp
public virtual int ItemDamage { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_MaxDurability"></a> MaxDurability

```csharp
public virtual int MaxDurability { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_MaxReach"></a> MaxReach

```csharp
public virtual float MaxReach { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Item_MaxStackCount"></a> MaxStackCount

```csharp
public virtual int MaxStackCount { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Item_MinReach"></a> MinReach

```csharp
public virtual float MinReach { get; }
```

#### Property Value

 float

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_Item_CanStackWith_MultiplayerInfrastructure_ItemSystem_Item_"></a> CanStackWith\(Item\)

같은 identifier를 가진 아이템과 스택이 가능한지 검사합니다.

```csharp
public bool CanStackWith(Item other)
```

#### Parameters

`other` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_Clone"></a> Clone\(\)

이 아이템의 얕은 카피를 반환합니다. 런타임 상태별로 복사됩니다.
깊은 복사가 필요한 파생 클래스는 override 해 주세요.

```csharp
public virtual Item Clone()
```

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_GetCurrentSerializedDerivedAttributes"></a> GetCurrentSerializedDerivedAttributes\(\)

파생 클래스 고유 상태를 직렬화 문자열로 반환합니다.
고유 상태가 없는 아이템은 빈 문자열을 반환합니다.

```csharp
public abstract string GetCurrentSerializedDerivedAttributes()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_InitializeFromDefinitions"></a> InitializeFromDefinitions\(\)

Definitions → Instance 복사. 생성자 이후 리셋이 필요할 때도 재호출할 수 있습니다.

```csharp
protected void InitializeFromDefinitions()
```

### <a id="MultiplayerInfrastructure_ItemSystem_Item_IsValid"></a> IsValid\(\)

현재 스택 카운트가 유효한지 (0보다 크고 최대 스택 이하인지) 검사합니다.

```csharp
public bool IsValid()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_MarkDerivedAttributesModified"></a> MarkDerivedAttributesModified\(\)

SetCurrentSerializedDerivedAttributes 구현 내에서 호출하여 수정 플래그를 세웁니다.

```csharp
protected void MarkDerivedAttributesModified()
```

### <a id="MultiplayerInfrastructure_ItemSystem_Item_Merge_MultiplayerInfrastructure_ItemSystem_Item_"></a> Merge\(Item\)

other 스택을 가능한 범위 내에서 흡수합니다. 남은 용량은 other.CurrentStackCount에 남아 있습니다.

```csharp
public Item Merge(Item other)
```

#### Parameters

`other` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_OnAttack_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnAttack\(PlayerController, Entity\)

플레이어가 이 아이템으로 공격할 때 호출됩니다.

```csharp
public virtual ActionResult OnAttack(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_OnGet_MultiplayerInfrastructure_Player_PlayerController_"></a> OnGet\(PlayerController\)

플레이어가 인벤토리에 이 아이템을 추가할 때 호출됩니다.

```csharp
public virtual void OnGet(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_OnInteract_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnInteract\(PlayerController, Entity\)

플레이어가 이 아이템으로 오브젝트를 상호작용할 때 호출됩니다.

```csharp
public virtual void OnInteract(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_OnThrow_MultiplayerInfrastructure_Player_PlayerController_"></a> OnThrow\(PlayerController\)

플레이어가 이 아이템을 던질 때 호출됩니다.

```csharp
public virtual void OnThrow(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_OnUse_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnUse\(PlayerController, Entity\)

플레이어가 이 아이템을 사용할 때 호출됩니다.

```csharp
public virtual ActionResult OnUse(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Item_SetCurrentSerializedDerivedAttributes_System_String_"></a> SetCurrentSerializedDerivedAttributes\(string\)

파생 클래스 고유 상태를 직렬화 문자열로부터 복원합니다.
역직렬화 후 IsModifiedCurrentSerializedDerivedAttributes를 true로 설정합니다.

```csharp
public abstract void SetCurrentSerializedDerivedAttributes(string serialized)
```

#### Parameters

`serialized` string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Item_TryApplyDurabilityOnUse_System_Boolean__"></a> TryApplyDurabilityOnUse\(out bool\)

사용 시 정의된 내구도 변화량을 적용하고, 소진 여부를 반환합니다.

```csharp
public bool TryApplyDurabilityOnUse(out bool depleted)
```

#### Parameters

`depleted` bool

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Item_TryConsumeDurabilityOnUse_System_Boolean__"></a> TryConsumeDurabilityOnUse\(out bool\)

사용 내구도를 적용하고 내구도가 소진되면 현재 스택에서 아이템 하나를 제거합니다.
스택이 남아 있으면 다음 아이템의 내구도를 최대치로 초기화합니다.

```csharp
public bool TryConsumeDurabilityOnUse(out bool stackDepleted)
```

#### Parameters

`stackDepleted` bool

#### Returns

 bool

