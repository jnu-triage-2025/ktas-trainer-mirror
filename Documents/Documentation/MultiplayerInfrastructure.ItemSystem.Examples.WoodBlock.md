# <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock"></a> Class WoodBlock

Namespace: [MultiplayerInfrastructure.ItemSystem.Examples](MultiplayerInfrastructure.ItemSystem.Examples.md)  
Assembly: Assembly\-CSharp.dll  

나무 블록 아이템 구현 예시입니다.

■ 등록 방법 (게임 초기화 코드에서 1회):
  Registry.Registry.RegisterItemDefinition&lt;WoodBlock&gt;(WoodBlock.Identifier);

■ 인스턴스 생성:
  var wood = Registry.Registry.CreateItemInstance(WoodBlock.Identifier) as WoodBlock;

■ 월드에 스폰:
  ItemObject.Spawn(wood, spawnPosition);

```csharp
public class WoodBlock : Item
```

#### Inheritance

object ← 
[Item](MultiplayerInfrastructure.ItemSystem.Item.md) ← 
[WoodBlock](MultiplayerInfrastructure.ItemSystem.Examples.WoodBlock.md)

#### Inherited Members

[Item.Identifier](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Identifier), 
[Item.DisplayName](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_DisplayName), 
[Item.Description](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Description), 
[Item.DetailComment](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_DetailComment), 
[Item.Color](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Color), 
[Item.IsStackable](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_IsStackable), 
[Item.MaxStackCount](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_MaxStackCount), 
[Item.HasDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_HasDurability), 
[Item.EnabledDeltaDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_EnabledDeltaDurability), 
[Item.MaxDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_MaxDurability), 
[Item.DeltaDurabilityOnAttack](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_DeltaDurabilityOnAttack), 
[Item.DeltaDurabilityOnUse](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_DeltaDurabilityOnUse), 
[Item.MinReach](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_MinReach), 
[Item.MaxReach](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_MaxReach), 
[Item.ItemDamage](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_ItemDamage), 
[Item.EnabledCooldown](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_EnabledCooldown), 
[Item.CooldownMilliseconds](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CooldownMilliseconds), 
[Item.IsModifiedCurrentSerializedDerivedAttributes](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_IsModifiedCurrentSerializedDerivedAttributes), 
[Item.CurrentIdentifier](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentIdentifier), 
[Item.CurrentDisplayName](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentDisplayName), 
[Item.CurrentDescription](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentDescription), 
[Item.CurrentDetailComment](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentDetailComment), 
[Item.CurrentColor](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentColor), 
[Item.CurrentItemIconTexture](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentItemIconTexture), 
[Item.IsCurrentlyStackable](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_IsCurrentlyStackable), 
[Item.CurrentMaxStackCount](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentMaxStackCount), 
[Item.CurrentStackCount](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentStackCount), 
[Item.HasCurrentDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_HasCurrentDurability), 
[Item.CurrentEnabledDeltaDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentEnabledDeltaDurability), 
[Item.CurrentMaxDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentMaxDurability), 
[Item.CurrentDurability](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentDurability), 
[Item.CurrentDurabilityDeltaOnAttack](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentDurabilityDeltaOnAttack), 
[Item.CurrentDurabilityDeltaOnUse](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentDurabilityDeltaOnUse), 
[Item.CurrentMinReach](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentMinReach), 
[Item.CurrentMaxReach](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentMaxReach), 
[Item.CurrentItemDamage](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentItemDamage), 
[Item.CurrentEnabledCooldown](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentEnabledCooldown), 
[Item.CurrentCooldownMilliseconds](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentCooldownMilliseconds), 
[Item.CurrentCooldownRemainingMilliseconds](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentCooldownRemainingMilliseconds), 
[Item.CurrentSerializedDerivedAttributes](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CurrentSerializedDerivedAttributes), 
[Item.DeferredOnGet](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_DeferredOnGet), 
[Item.InitializeFromDefinitions\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_InitializeFromDefinitions), 
[Item.OnGet\(PlayerController\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnGet\_MultiplayerInfrastructure\_Player\_PlayerController\_), 
[Item.OnThrow\(PlayerController\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnThrow\_MultiplayerInfrastructure\_Player\_PlayerController\_), 
[Item.OnInteract\(PlayerController, Entity\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnInteract\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[Item.OnAttack\(PlayerController, Entity\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnAttack\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[Item.OnUse\(PlayerController, Entity\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnUse\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[Item.SetCurrentSerializedDerivedAttributes\(string\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_SetCurrentSerializedDerivedAttributes\_System\_String\_), 
[Item.GetCurrentSerializedDerivedAttributes\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_GetCurrentSerializedDerivedAttributes), 
[Item.MarkDerivedAttributesModified\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_MarkDerivedAttributesModified), 
[Item.IsValid\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_IsValid), 
[Item.CanStackWith\(Item\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CanStackWith\_MultiplayerInfrastructure\_ItemSystem\_Item\_), 
[Item.Merge\(Item\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Merge\_MultiplayerInfrastructure\_ItemSystem\_Item\_), 
[Item.Clone\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Clone), 
[Item.TryApplyDurabilityOnUse\(out bool\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_TryApplyDurabilityOnUse\_System\_Boolean\_\_), 
[Item.TryConsumeDurabilityOnUse\(out bool\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_TryConsumeDurabilityOnUse\_System\_Boolean\_\_), 
[Item.ToString\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_ToString)

## Constructors

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock__ctor"></a> WoodBlock\(\)

```csharp
public WoodBlock()
```

## Fields

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_Color"></a> Color

```csharp
public const string Color = "#8B5E3C"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_CooldownMilliseconds"></a> CooldownMilliseconds

```csharp
public const float CooldownMilliseconds = 0
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_DeltaDurabilityOnAttack"></a> DeltaDurabilityOnAttack

```csharp
public const int DeltaDurabilityOnAttack = -2
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_DeltaDurabilityOnUse"></a> DeltaDurabilityOnUse

```csharp
public const int DeltaDurabilityOnUse = 0
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_Description"></a> Description

```csharp
public const string Description = "가공된 목재 블록입니다. 설치하면 블록이 됩니다."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_DetailComment"></a> DetailComment

```csharp
public const string DetailComment = ""
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_DisplayName"></a> DisplayName

```csharp
public const string DisplayName = "나무 블록"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_EnabledCooldown"></a> EnabledCooldown

```csharp
public const bool EnabledCooldown = false
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_EnabledDeltaDurability"></a> EnabledDeltaDurability

```csharp
public const bool EnabledDeltaDurability = true
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_HasDurability"></a> HasDurability

```csharp
public const bool HasDurability = true
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_Identifier"></a> Identifier

```csharp
public const string Identifier = "wood_block"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_IsStackable"></a> IsStackable

```csharp
public const bool IsStackable = true
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_ItemDamage"></a> ItemDamage

```csharp
public const int ItemDamage = 2
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_MaxDurability"></a> MaxDurability

```csharp
public const int MaxDurability = 30
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_MaxReach"></a> MaxReach

```csharp
public const float MaxReach = 3
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_MaxStackCount"></a> MaxStackCount

```csharp
public const int MaxStackCount = 64
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_MinReach"></a> MinReach

```csharp
public const float MinReach = 0.5
```

#### Field Value

 float

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_GetCurrentSerializedDerivedAttributes"></a> GetCurrentSerializedDerivedAttributes\(\)

파생 클래스 고유 상태를 직렬화 문자열로 반환합니다.
고유 상태가 없는 아이템은 빈 문자열을 반환합니다.

```csharp
public override string GetCurrentSerializedDerivedAttributes()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_OnAttack_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnAttack\(PlayerController, Entity\)

플레이어가 이 아이템으로 공격할 때 호출됩니다.

```csharp
public override ActionResult OnAttack(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_OnGet_MultiplayerInfrastructure_Player_PlayerController_"></a> OnGet\(PlayerController\)

플레이어가 인벤토리에 이 아이템을 추가할 때 호출됩니다.

```csharp
public override void OnGet(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_OnThrow_MultiplayerInfrastructure_Player_PlayerController_"></a> OnThrow\(PlayerController\)

플레이어가 이 아이템을 던질 때 호출됩니다.

```csharp
public override void OnThrow(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_OnUse_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnUse\(PlayerController, Entity\)

플레이어가 이 아이템을 사용할 때 호출됩니다.

```csharp
public override ActionResult OnUse(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="MultiplayerInfrastructure_ItemSystem_Examples_WoodBlock_SetCurrentSerializedDerivedAttributes_System_String_"></a> SetCurrentSerializedDerivedAttributes\(string\)

파생 클래스 고유 상태를 직렬화 문자열로부터 복원합니다.
역직렬화 후 IsModifiedCurrentSerializedDerivedAttributes를 true로 설정합니다.

```csharp
public override void SetCurrentSerializedDerivedAttributes(string serialized)
```

#### Parameters

`serialized` string

