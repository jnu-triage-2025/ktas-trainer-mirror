# <a id="TriageTrainer_Entity_Level1RapidInfuserController"></a> Class Level1RapidInfuserController

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

급속 주입기의 상호작용과 상태를 소유한다. 이동은 도메인 독립 공통 모듈
<xref href="MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl" data-throw-if-not-resolved="false"></xref> 에 위임한다.

```csharp
public sealed class Level1RapidInfuserController : MinecraftBoatLikeControl, IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[MinecraftBoatLikeControl](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md) ← 
[Level1RapidInfuserController](TriageTrainer.Entity.Level1RapidInfuserController.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md), 
[ISpawnedEntityIdentifierReceiver](MultiplayerInfrastructure.Registry.ISpawnedEntityIdentifierReceiver.md)

#### Inherited Members

[MinecraftBoatLikeControl.DefaultControlIconResourcePath](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_DefaultControlIconResourcePath), 
[MinecraftBoatLikeControl.ParticipantAssigned](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_ParticipantAssigned), 
[MinecraftBoatLikeControl.Capacity](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Capacity), 
[MinecraftBoatLikeControl.HasParticipants](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_HasParticipants), 
[MinecraftBoatLikeControl.IsLocallyControlled](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_IsLocallyControlled), 
[MinecraftBoatLikeControl.OnStartClient\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStartClient), 
[MinecraftBoatLikeControl.OnStartServer\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStartServer), 
[MinecraftBoatLikeControl.OnStopServer\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStopServer), 
[MinecraftBoatLikeControl.OnStopClient\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStopClient), 
[MinecraftBoatLikeControl.SetMinimumMovementDivisor\(int\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_SetMinimumMovementDivisor\_System\_Int32\_), 
[MinecraftBoatLikeControl.CanToggle\(Transform\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_CanToggle\_UnityEngine\_Transform\_), 
[MinecraftBoatLikeControl.Toggle\(Transform\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Toggle\_UnityEngine\_Transform\_), 
[MinecraftBoatLikeControl.DetachAllParticipants\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_DetachAllParticipants)

## Fields

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_InteractIdAddBloodBag"></a> InteractIdAddBloodBag

```csharp
public const string InteractIdAddBloodBag = "level1_add_blood_bag"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_InteractIdAddNormalSaline"></a> InteractIdAddNormalSaline

```csharp
public const string InteractIdAddNormalSaline = "level1_add_normal_saline"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_InteractIdAddPlasmaSolution"></a> InteractIdAddPlasmaSolution

```csharp
public const string InteractIdAddPlasmaSolution = "level1_add_plasma_solution"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_InteractIdConnectCLine"></a> InteractIdConnectCLine

```csharp
public const string InteractIdConnectCLine = "level1_connect_cline"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_AdditionalInteracts"></a> AdditionalInteracts

```csharp
public IEnumerable<IInteract> AdditionalInteracts { get; }
```

#### Property Value

 IEnumerable<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_ConnectedPatientIdentifier"></a> ConnectedPatientIdentifier

```csharp
public string ConnectedPatientIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_HasBloodBag"></a> HasBloodBag

```csharp
public bool HasBloodBag { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_HasNormalSaline"></a> HasNormalSaline

```csharp
public bool HasNormalSaline { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_HasPlasmaSolution"></a> HasPlasmaSolution

```csharp
public bool HasPlasmaSolution { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_IvConnectionPoint"></a> IvConnectionPoint

```csharp
public Transform IvConnectionPoint { get; }
```

#### Property Value

 Transform

## Methods

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_ApplySpawnedEntityIdentifier_System_String_"></a> ApplySpawnedEntityIdentifier\(string\)

스폰된 인스턴스에 부여할 엔티티 식별자를 적용한다.

```csharp
public void ApplySpawnedEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_ApplyState_TriageTrainer_Entity_Level1RapidInfuserState_"></a> ApplyState\(Level1RapidInfuserState\)

```csharp
public void ApplyState(Level1RapidInfuserState state)
```

#### Parameters

`state` [Level1RapidInfuserState](TriageTrainer.Entity.Level1RapidInfuserState.md)

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_CaptureState"></a> CaptureState\(\)

```csharp
public Level1RapidInfuserState CaptureState()
```

#### Returns

 [Level1RapidInfuserState](TriageTrainer.Entity.Level1RapidInfuserState.md)

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_ConnectPatient_TriageTrainer_Entity_PatientController_"></a> ConnectPatient\(PatientController\)

```csharp
public void ConnectPatient(PatientController patient)
```

#### Parameters

`patient` [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_TryLoadStateFromPatient_TriageTrainer_Entity_PatientController_"></a> TryLoadStateFromPatient\(PatientController\)

```csharp
public bool TryLoadStateFromPatient(PatientController patient)
```

#### Parameters

`patient` [PatientController](TriageTrainer.Entity.PatientController.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnBloodApplied"></a> OnBloodApplied

```csharp
public event Action<RapidInfuserFluidLifecycleEvent> OnBloodApplied
```

#### Event Type

 Action<[RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)\>

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnBloodCancelled"></a> OnBloodCancelled

```csharp
public event Action<RapidInfuserFluidLifecycleEvent> OnBloodCancelled
```

#### Event Type

 Action<[RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)\>

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnBloodTry"></a> OnBloodTry

```csharp
public event Action<RapidInfuserFluidLifecycleEvent> OnBloodTry
```

#### Event Type

 Action<[RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)\>

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnPlasmaApplied"></a> OnPlasmaApplied

```csharp
public event Action<RapidInfuserFluidLifecycleEvent> OnPlasmaApplied
```

#### Event Type

 Action<[RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)\>

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnPlasmaCancelled"></a> OnPlasmaCancelled

```csharp
public event Action<RapidInfuserFluidLifecycleEvent> OnPlasmaCancelled
```

#### Event Type

 Action<[RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)\>

### <a id="TriageTrainer_Entity_Level1RapidInfuserController_OnPlasmaTry"></a> OnPlasmaTry

```csharp
public event Action<RapidInfuserFluidLifecycleEvent> OnPlasmaTry
```

#### Event Type

 Action<[RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)\>

