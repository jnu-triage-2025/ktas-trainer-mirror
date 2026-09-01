# <a id="TriageTrainer_ItemDefinitions_ChecklistPaper"></a> Class ChecklistPaper

Namespace: [TriageTrainer.ItemDefinitions](TriageTrainer.ItemDefinitions.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ChecklistPaper : Paper
```

#### Inheritance

object ← 
[Item](MultiplayerInfrastructure.ItemSystem.Item.md) ← 
[MedicalItem](TriageTrainer.ItemDefinitions.MedicalItem.md) ← 
[Paper](TriageTrainer.ItemDefinitions.Paper.md) ← 
[ChecklistPaper](TriageTrainer.ItemDefinitions.ChecklistPaper.md)

#### Inherited Members

[Paper.Identifier](TriageTrainer.ItemDefinitions.Paper.md\#TriageTrainer\_ItemDefinitions\_Paper\_Identifier), 
[Paper.DisplayName](TriageTrainer.ItemDefinitions.Paper.md\#TriageTrainer\_ItemDefinitions\_Paper\_DisplayName), 
[Paper.Description](TriageTrainer.ItemDefinitions.Paper.md\#TriageTrainer\_ItemDefinitions\_Paper\_Description), 
[Paper.IsStackable](TriageTrainer.ItemDefinitions.Paper.md\#TriageTrainer\_ItemDefinitions\_Paper\_IsStackable), 
[Paper.MaxStackCount](TriageTrainer.ItemDefinitions.Paper.md\#TriageTrainer\_ItemDefinitions\_Paper\_MaxStackCount), 
[MedicalItem.DetailComment](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_DetailComment), 
[MedicalItem.Color](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_Color), 
[MedicalItem.IsStackable](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_IsStackable), 
[MedicalItem.MaxStackCount](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_MaxStackCount), 
[MedicalItem.HasDurability](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_HasDurability), 
[MedicalItem.EnabledDeltaDurability](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_EnabledDeltaDurability), 
[MedicalItem.MaxDurability](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_MaxDurability), 
[MedicalItem.DeltaDurabilityOnAttack](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_DeltaDurabilityOnAttack), 
[MedicalItem.DeltaDurabilityOnUse](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_DeltaDurabilityOnUse), 
[MedicalItem.MinReach](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_MinReach), 
[MedicalItem.MaxReach](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_MaxReach), 
[MedicalItem.ItemDamage](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_ItemDamage), 
[MedicalItem.EnabledCooldown](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_EnabledCooldown), 
[MedicalItem.CooldownMilliseconds](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_CooldownMilliseconds), 
[MedicalItem.OnGet\(PlayerController\)](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_OnGet\_MultiplayerInfrastructure\_Player\_PlayerController\_), 
[MedicalItem.OnUse\(PlayerController, Entity\)](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_OnUse\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[MedicalItem.OnAttack\(PlayerController, Entity\)](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_OnAttack\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[MedicalItem.GetCurrentSerializedDerivedAttributes\(\)](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_GetCurrentSerializedDerivedAttributes), 
[MedicalItem.SetCurrentSerializedDerivedAttributes\(string\)](TriageTrainer.ItemDefinitions.MedicalItem.md\#TriageTrainer\_ItemDefinitions\_MedicalItem\_SetCurrentSerializedDerivedAttributes\_System\_String\_), 
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
[Item.OnGet\(PlayerController\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnGet\_MultiplayerInfrastructure\_Player\_PlayerController\_), 
[Item.OnThrow\(PlayerController\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnThrow\_MultiplayerInfrastructure\_Player\_PlayerController\_), 
[Item.OnInteract\(PlayerController, Entity\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnInteract\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[Item.OnAttack\(PlayerController, Entity\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnAttack\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[Item.OnUse\(PlayerController, Entity\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_OnUse\_MultiplayerInfrastructure\_Player\_PlayerController\_MultiplayerInfrastructure\_Entity\_Entity\_), 
[Item.SetCurrentSerializedDerivedAttributes\(string\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_SetCurrentSerializedDerivedAttributes\_System\_String\_), 
[Item.GetCurrentSerializedDerivedAttributes\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_GetCurrentSerializedDerivedAttributes), 
[Item.IsValid\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_IsValid), 
[Item.CanStackWith\(Item\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_CanStackWith\_MultiplayerInfrastructure\_ItemSystem\_Item\_), 
[Item.Merge\(Item\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Merge\_MultiplayerInfrastructure\_ItemSystem\_Item\_), 
[Item.Clone\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_Clone), 
[Item.TryApplyDurabilityOnUse\(out bool\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_TryApplyDurabilityOnUse\_System\_Boolean\_\_), 
[Item.TryConsumeDurabilityOnUse\(out bool\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_TryConsumeDurabilityOnUse\_System\_Boolean\_\_), 
[Item.ToString\(\)](MultiplayerInfrastructure.ItemSystem.Item.md\#MultiplayerInfrastructure\_ItemSystem\_Item\_ToString)

## Fields

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_Description"></a> Description

```csharp
public const string Description = ""
```

#### Field Value

 string

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_DisplayName"></a> DisplayName

```csharp
public const string DisplayName = "종이"
```

#### Field Value

 string

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_Identifier"></a> Identifier

```csharp
public const string Identifier = "checklist_paper"
```

#### Field Value

 string

## Methods

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_Clone"></a> Clone\(\)

이 아이템의 얕은 카피를 반환합니다. 런타임 상태별로 복사됩니다.
깊은 복사가 필요한 파생 클래스는 override 해 주세요.

```csharp
public override Item Clone()
```

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_GetCurrentSerializedDerivedAttributes"></a> GetCurrentSerializedDerivedAttributes\(\)

파생 클래스 고유 상태를 직렬화 문자열로 반환합니다.
고유 상태가 없는 아이템은 빈 문자열을 반환합니다.

```csharp
public override string GetCurrentSerializedDerivedAttributes()
```

#### Returns

 string

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_NotifyItemAcquired_MultiplayerInfrastructure_Player_PlayerController_System_String_"></a> NotifyItemAcquired\(PlayerController, string\)

의료 아이템의 획득 훅에서 호출됩니다. 인벤토리에 보관 중인 체크리스트만 갱신하므로,
월드 아이템이나 다른 플레이어의 체크리스트에는 영향을 주지 않습니다.

```csharp
public static void NotifyItemAcquired(PlayerController player, string itemIdentifier)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`itemIdentifier` string

### <a id="TriageTrainer_ItemDefinitions_ChecklistPaper_SetCurrentSerializedDerivedAttributes_System_String_"></a> SetCurrentSerializedDerivedAttributes\(string\)

파생 클래스 고유 상태를 직렬화 문자열로부터 복원합니다.
역직렬화 후 IsModifiedCurrentSerializedDerivedAttributes를 true로 설정합니다.

```csharp
public override void SetCurrentSerializedDerivedAttributes(string serialized)
```

#### Parameters

`serialized` string

