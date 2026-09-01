# <a id="TriageTrainer_ItemDefinitions_MedicalItem"></a> Class MedicalItem

Namespace: [TriageTrainer.ItemDefinitions](TriageTrainer.ItemDefinitions.md)  
Assembly: Assembly\-CSharp.dll  

TriageTrainer 의료 아이템 공통 기반 클래스입니다.

파생 클래스는 Identifier, DisplayName, Description 을 반드시 구현합니다.
특별한 동작이 필요한 경우 OnGet / OnUse / OnAttack 등을 override합니다.

```csharp
public abstract class MedicalItem : Item
```

#### Inheritance

object ← 
[Item](MultiplayerInfrastructure.ItemSystem.Item.md) ← 
[MedicalItem](TriageTrainer.ItemDefinitions.MedicalItem.md)

#### Derived

[Ambubag](TriageTrainer.ItemDefinitions.Ambubag.md), 
[BloodBag](TriageTrainer.ItemDefinitions.BloodBag.md), 
[BloodTransfusionSet](TriageTrainer.ItemDefinitions.BloodTransfusionSet.md), 
[Cannula16g](TriageTrainer.ItemDefinitions.Cannula16g.md), 
[Cannula18g](TriageTrainer.ItemDefinitions.Cannula18g.md), 
[Cannula20g](TriageTrainer.ItemDefinitions.Cannula20g.md), 
[Cannula22g](TriageTrainer.ItemDefinitions.Cannula22g.md), 
[Cannula24g](TriageTrainer.ItemDefinitions.Cannula24g.md), 
[CentralLineSet](TriageTrainer.ItemDefinitions.CentralLineSet.md), 
[CervicalCollar](TriageTrainer.ItemDefinitions.CervicalCollar.md), 
[ContaminatedGloves](TriageTrainer.ItemDefinitions.ContaminatedGloves.md), 
[DefibrillatorPad](TriageTrainer.ItemDefinitions.DefibrillatorPad.md), 
[ElasticBand](TriageTrainer.ItemDefinitions.ElasticBand.md), 
[Electrode](TriageTrainer.ItemDefinitions.Electrode.md), 
[ElectrodeCable](TriageTrainer.ItemDefinitions.ElectrodeCable.md), 
[EndotrachealTube](TriageTrainer.ItemDefinitions.EndotrachealTube.md), 
[EndotrachealTubeReady](TriageTrainer.ItemDefinitions.EndotrachealTubeReady.md), 
[Epinephrine16g20ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine16g20ccSyringe.md), 
[Epinephrine16g50ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine16g50ccSyringe.md), 
[Epinephrine16g5ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine16g5ccSyringe.md), 
[Epinephrine18g20ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine18g20ccSyringe.md), 
[Epinephrine18g50ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine18g50ccSyringe.md), 
[Epinephrine18g5ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine18g5ccSyringe.md), 
[Epinephrine20ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine20ccSyringe.md), 
[Epinephrine20g20ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine20g20ccSyringe.md), 
[Epinephrine20g50ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine20g50ccSyringe.md), 
[Epinephrine20g5ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine20g5ccSyringe.md), 
[Epinephrine22g20ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine22g20ccSyringe.md), 
[Epinephrine22g50ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine22g50ccSyringe.md), 
[Epinephrine22g5ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine22g5ccSyringe.md), 
[Epinephrine24g20ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine24g20ccSyringe.md), 
[Epinephrine24g50ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine24g50ccSyringe.md), 
[Epinephrine24g5ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine24g5ccSyringe.md), 
[Epinephrine50ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine50ccSyringe.md), 
[Epinephrine5ccSyringe](TriageTrainer.ItemDefinitions.Epinephrine5ccSyringe.md), 
[EpinephrineAmpule](TriageTrainer.ItemDefinitions.EpinephrineAmpule.md), 
[FacialMask](TriageTrainer.ItemDefinitions.FacialMask.md), 
[Flowmeter](TriageTrainer.ItemDefinitions.Flowmeter.md), 
[Gauze](TriageTrainer.ItemDefinitions.Gauze.md), 
[HandyClock](TriageTrainer.ItemDefinitions.HandyClock.md), 
[Humidifier](TriageTrainer.ItemDefinitions.Humidifier.md), 
[HumidifierSterileDistilledWaterBottle](TriageTrainer.ItemDefinitions.HumidifierSterileDistilledWaterBottle.md), 
[IntravenousSet](TriageTrainer.ItemDefinitions.IntravenousSet.md), 
[Laryngoscope](TriageTrainer.ItemDefinitions.Laryngoscope.md), 
[LaryngoscopeBlade](TriageTrainer.ItemDefinitions.LaryngoscopeBlade.md), 
[LaryngoscopeHandle](TriageTrainer.ItemDefinitions.LaryngoscopeHandle.md), 
[Level1RapidInfuser](TriageTrainer.ItemDefinitions.Level1RapidInfuser.md), 
[NasalCannula](TriageTrainer.ItemDefinitions.NasalCannula.md), 
[Norepinephrine16g20ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine16g20ccSyringe.md), 
[Norepinephrine16g50ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine16g50ccSyringe.md), 
[Norepinephrine16g5ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine16g5ccSyringe.md), 
[Norepinephrine18g20ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine18g20ccSyringe.md), 
[Norepinephrine18g50ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine18g50ccSyringe.md), 
[Norepinephrine18g5ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine18g5ccSyringe.md), 
[Norepinephrine20ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine20ccSyringe.md), 
[Norepinephrine20g20ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine20g20ccSyringe.md), 
[Norepinephrine20g50ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine20g50ccSyringe.md), 
[Norepinephrine20g5ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine20g5ccSyringe.md), 
[Norepinephrine22g20ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine22g20ccSyringe.md), 
[Norepinephrine22g50ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine22g50ccSyringe.md), 
[Norepinephrine22g5ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine22g5ccSyringe.md), 
[Norepinephrine24g20ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine24g20ccSyringe.md), 
[Norepinephrine24g50ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine24g50ccSyringe.md), 
[Norepinephrine24g5ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine24g5ccSyringe.md), 
[Norepinephrine50ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine50ccSyringe.md), 
[Norepinephrine5ccSyringe](TriageTrainer.ItemDefinitions.Norepinephrine5ccSyringe.md), 
[NorepinephrineAmpule](TriageTrainer.ItemDefinitions.NorepinephrineAmpule.md), 
[NormalSaline1000ml](TriageTrainer.ItemDefinitions.NormalSaline1000ml.md), 
[NormalSaline16g20ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline16g20ccSyringe.md), 
[NormalSaline16g50ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline16g50ccSyringe.md), 
[NormalSaline16g5ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline16g5ccSyringe.md), 
[NormalSaline18g20ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline18g20ccSyringe.md), 
[NormalSaline18g50ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline18g50ccSyringe.md), 
[NormalSaline18g5ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline18g5ccSyringe.md), 
[NormalSaline20ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline20ccSyringe.md), 
[NormalSaline20g20ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline20g20ccSyringe.md), 
[NormalSaline20g50ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline20g50ccSyringe.md), 
[NormalSaline20g5ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline20g5ccSyringe.md), 
[NormalSaline20ml](TriageTrainer.ItemDefinitions.NormalSaline20ml.md), 
[NormalSaline22g20ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline22g20ccSyringe.md), 
[NormalSaline22g50ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline22g50ccSyringe.md), 
[NormalSaline22g5ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline22g5ccSyringe.md), 
[NormalSaline24g20ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline24g20ccSyringe.md), 
[NormalSaline24g50ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline24g50ccSyringe.md), 
[NormalSaline24g5ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline24g5ccSyringe.md), 
[NormalSaline50ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline50ccSyringe.md), 
[NormalSaline5ccSyringe](TriageTrainer.ItemDefinitions.NormalSaline5ccSyringe.md), 
[NormalSalineIntravenousReady](TriageTrainer.ItemDefinitions.NormalSalineIntravenousReady.md), 
[O2Line](TriageTrainer.ItemDefinitions.O2Line.md), 
[Oxyflowmeter](TriageTrainer.ItemDefinitions.Oxyflowmeter.md), 
[Paper](TriageTrainer.ItemDefinitions.Paper.md), 
[PatientMonitor](TriageTrainer.ItemDefinitions.PatientMonitor.md), 
[Penlight](TriageTrainer.ItemDefinitions.Penlight.md), 
[PlasmaSolution1000ml](TriageTrainer.ItemDefinitions.PlasmaSolution1000ml.md), 
[PlasmaSolutionIntravenousReady](TriageTrainer.ItemDefinitions.PlasmaSolutionIntravenousReady.md), 
[Plaster](TriageTrainer.ItemDefinitions.Plaster.md), 
[ReservoirBag](TriageTrainer.ItemDefinitions.ReservoirBag.md), 
[Scissors](TriageTrainer.ItemDefinitions.Scissors.md), 
[SmallChain](TriageTrainer.ItemDefinitions.SmallChain.md), 
[SmallGear](TriageTrainer.ItemDefinitions.SmallGear.md), 
[SterileDistilledWater](TriageTrainer.ItemDefinitions.SterileDistilledWater.md), 
[SterileGloves](TriageTrainer.ItemDefinitions.SterileGloves.md), 
[Stylet](TriageTrainer.ItemDefinitions.Stylet.md), 
[SuctionCatheter](TriageTrainer.ItemDefinitions.SuctionCatheter.md), 
[SuctionLine](TriageTrainer.ItemDefinitions.SuctionLine.md), 
[Swab](TriageTrainer.ItemDefinitions.Swab.md), 
[Syringe20cc](TriageTrainer.ItemDefinitions.Syringe20cc.md), 
[Syringe50cc](TriageTrainer.ItemDefinitions.Syringe50cc.md), 
[Syringe5cc](TriageTrainer.ItemDefinitions.Syringe5cc.md), 
[TPieceSet](TriageTrainer.ItemDefinitions.TPieceSet.md), 
[TinIngot](TriageTrainer.ItemDefinitions.TinIngot.md), 
[TutorialDecoy8909](TriageTrainer.ItemDefinitions.TutorialDecoy8909.md), 
[TutorialDecoyKimGangsanMail](TriageTrainer.ItemDefinitions.TutorialDecoyKimGangsanMail.md), 
[TutorialDecoyOvernightYouth](TriageTrainer.ItemDefinitions.TutorialDecoyOvernightYouth.md), 
[TutorialDeliveryPackage](TriageTrainer.ItemDefinitions.TutorialDeliveryPackage.md), 
[VitalSet](TriageTrainer.ItemDefinitions.VitalSet.md), 
[WallSuction](TriageTrainer.ItemDefinitions.WallSuction.md), 
[Yankauer](TriageTrainer.ItemDefinitions.Yankauer.md), 
[YankauerSuctionReady](TriageTrainer.ItemDefinitions.YankauerSuctionReady.md)

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

### <a id="TriageTrainer_ItemDefinitions_MedicalItem__ctor"></a> MedicalItem\(\)

```csharp
protected MedicalItem()
```

## Fields

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_Color"></a> Color

```csharp
public const string Color = "white"
```

#### Field Value

 string

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_CooldownMilliseconds"></a> CooldownMilliseconds

```csharp
public const float CooldownMilliseconds = 0
```

#### Field Value

 float

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_DeltaDurabilityOnAttack"></a> DeltaDurabilityOnAttack

```csharp
public const int DeltaDurabilityOnAttack = 0
```

#### Field Value

 int

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_DeltaDurabilityOnUse"></a> DeltaDurabilityOnUse

```csharp
public const int DeltaDurabilityOnUse = 0
```

#### Field Value

 int

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_DetailComment"></a> DetailComment

```csharp
public const string DetailComment = ""
```

#### Field Value

 string

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_EnabledCooldown"></a> EnabledCooldown

```csharp
public const bool EnabledCooldown = false
```

#### Field Value

 bool

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_EnabledDeltaDurability"></a> EnabledDeltaDurability

```csharp
public const bool EnabledDeltaDurability = false
```

#### Field Value

 bool

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_HasDurability"></a> HasDurability

```csharp
public const bool HasDurability = false
```

#### Field Value

 bool

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_IsStackable"></a> IsStackable

```csharp
public const bool IsStackable = true
```

#### Field Value

 bool

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_ItemDamage"></a> ItemDamage

```csharp
public const int ItemDamage = 0
```

#### Field Value

 int

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_MaxDurability"></a> MaxDurability

```csharp
public const int MaxDurability = 1
```

#### Field Value

 int

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_MaxReach"></a> MaxReach

```csharp
public const float MaxReach = 2.5
```

#### Field Value

 float

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_MaxStackCount"></a> MaxStackCount

```csharp
public const int MaxStackCount = 64
```

#### Field Value

 int

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_MinReach"></a> MinReach

```csharp
public const float MinReach = 1
```

#### Field Value

 float

## Methods

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_GetCurrentSerializedDerivedAttributes"></a> GetCurrentSerializedDerivedAttributes\(\)

파생 클래스 고유 상태를 직렬화 문자열로 반환합니다.
고유 상태가 없는 아이템은 빈 문자열을 반환합니다.

```csharp
public override string GetCurrentSerializedDerivedAttributes()
```

#### Returns

 string

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_OnAttack_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnAttack\(PlayerController, Entity\)

플레이어가 이 아이템으로 공격할 때 호출됩니다.

```csharp
public override ActionResult OnAttack(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_OnGet_MultiplayerInfrastructure_Player_PlayerController_"></a> OnGet\(PlayerController\)

아이템 획득(인벤토리 추가) 완료 시 시나리오 게이팅용 완료 신호(sig.*)를 올린다.
아이템 식별자 자체를 신호로 사용하므로(=sig.&lt;identifier&gt; 및 sig.click_&lt;identifier&gt;),
시나리오 조건명을 아이템 식별자에 맞추면 별도 코드 없이 획득 게이트가 통과된다.

주의: 일부 시나리오 조건명(예: click_glove, click_et_tube, click_ns1)은 아이템 식별자
(sterile_gloves, endotracheal_tube, normal_saline_1000ml)와 표기가 다르다. 이 불일치 목록과
처리 방침은 interaction-signal-integration-spec.md 의 "아이템 식별자 ↔ 조건명 정합" 절 참조.

```csharp
public override void OnGet(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_OnUse_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_Entity_"></a> OnUse\(PlayerController, Entity\)

플레이어가 이 아이템을 사용할 때 호출됩니다.

```csharp
public override ActionResult OnUse(PlayerController player, Entity target)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`target` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="TriageTrainer_ItemDefinitions_MedicalItem_SetCurrentSerializedDerivedAttributes_System_String_"></a> SetCurrentSerializedDerivedAttributes\(string\)

파생 클래스 고유 상태를 직렬화 문자열로부터 복원합니다.
역직렬화 후 IsModifiedCurrentSerializedDerivedAttributes를 true로 설정합니다.

```csharp
public override void SetCurrentSerializedDerivedAttributes(string serialized)
```

#### Parameters

`serialized` string

