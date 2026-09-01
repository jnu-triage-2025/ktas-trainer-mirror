# <a id="TriageTrainer_Entity_MovingPatientBedController"></a> Class MovingPatientBedController

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

이동 조종 네트워크 구현은 MinecraftBoatLikeControl로 공통화되었다.
이 partial 파일은 기존 Unity 메타/GUID 호환을 위해 유지한다.

```csharp
public class MovingPatientBedController : MinecraftBoatLikeControl, IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver, IEntityPresetParentLinkReceiver, IQuestPresentationTarget, IScenarioArrivalSignalEntityResolver
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[MinecraftBoatLikeControl](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md) ← 
[MovingPatientBedController](TriageTrainer.Entity.MovingPatientBedController.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md), 
[ISpawnedEntityIdentifierReceiver](MultiplayerInfrastructure.Registry.ISpawnedEntityIdentifierReceiver.md), 
[IEntityPresetParentLinkReceiver](MultiplayerInfrastructure.Registry.IEntityPresetParentLinkReceiver.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[IScenarioArrivalSignalEntityResolver](MultiplayerInfrastructure.Scenario.IScenarioArrivalSignalEntityResolver.md)

#### Inherited Members

[MinecraftBoatLikeControl.DefaultControlIconResourcePath](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_DefaultControlIconResourcePath), 
[MinecraftBoatLikeControl.ResolvedDefaultControlIcon](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_ResolvedDefaultControlIcon), 
[MinecraftBoatLikeControl.ParticipantAssigned](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_ParticipantAssigned), 
[MinecraftBoatLikeControl.Capacity](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Capacity), 
[MinecraftBoatLikeControl.HasParticipants](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_HasParticipants), 
[MinecraftBoatLikeControl.IsLocallyControlled](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_IsLocallyControlled), 
[MinecraftBoatLikeControl.Awake\_MinecraftBoatLikeControl\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Awake\_MinecraftBoatLikeControl), 
[MinecraftBoatLikeControl.OnStartClient\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStartClient), 
[MinecraftBoatLikeControl.OnStartServer\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStartServer), 
[MinecraftBoatLikeControl.OnStopServer\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStopServer), 
[MinecraftBoatLikeControl.OnStopClient\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnStopClient), 
[MinecraftBoatLikeControl.Update\_MinecraftBoatLikeControl\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Update\_MinecraftBoatLikeControl), 
[MinecraftBoatLikeControl.Configure\(int, string\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Configure\_System\_Int32\_System\_String\_), 
[MinecraftBoatLikeControl.SetMinimumMovementDivisor\(int\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_SetMinimumMovementDivisor\_System\_Int32\_), 
[MinecraftBoatLikeControl.CanToggle\(Transform\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_CanToggle\_UnityEngine\_Transform\_), 
[MinecraftBoatLikeControl.Toggle\(Transform\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_Toggle\_UnityEngine\_Transform\_), 
[MinecraftBoatLikeControl.DetachAllParticipants\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_DetachAllParticipants), 
[MinecraftBoatLikeControl.OnServerParticipantEntered\(int, PlayerController, int\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnServerParticipantEntered\_System\_Int32\_MultiplayerInfrastructure\_Player\_PlayerController\_System\_Int32\_), 
[MinecraftBoatLikeControl.OnServerParticipantExited\(int, PlayerController, int\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_OnServerParticipantExited\_System\_Int32\_MultiplayerInfrastructure\_Player\_PlayerController\_System\_Int32\_), 
[MinecraftBoatLikeControl.TryGetSingleMovingParticipantConnection\(out NetworkConnection\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_TryGetSingleMovingParticipantConnection\_FishNet\_Connection\_NetworkConnection\_\_), 
[MinecraftBoatLikeControl.ShouldIgnoreMovementBlocker\(Collider\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_ShouldIgnoreMovementBlocker\_UnityEngine\_Collider\_), 
[MinecraftBoatLikeControl.ShouldLogMovementBlockers](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_ShouldLogMovementBlockers), 
[MinecraftBoatLikeControl.SetAuthoritativeTransform\(Vector3, Quaternion\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_SetAuthoritativeTransform\_UnityEngine\_Vector3\_UnityEngine\_Quaternion\_), 
[MinecraftBoatLikeControl.ClearLocalParticipants\(\)](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md\#MultiplayerInfrastructure\_Entity\_MinecraftBoatLikeControl\_ClearLocalParticipants)

## Fields

### <a id="TriageTrainer_Entity_MovingPatientBedController_InteractIdHangNormalSaline"></a> InteractIdHangNormalSaline

생리식염수 수액 걸기 상호작용 식별자. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdHangNormalSaline = "hang_normal_saline"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_InteractIdHangPlasmaSolution"></a> InteractIdHangPlasmaSolution

플라즈마 솔루션 수액 걸기 상호작용 식별자. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdHangPlasmaSolution = "hang_plasma_solution"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_InteractionIdentifierMoveBed"></a> InteractionIdentifierMoveBed

```csharp
public const string InteractionIdentifierMoveBed = "move_bed"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_MovingPatientBedController_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_DismountCompletionSignal"></a> DismountCompletionSignal

```csharp
public string DismountCompletionSignal { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Entity_MovingPatientBedController_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Entity_MovingPatientBedController_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_InteractionIdentifier"></a> InteractionIdentifier

```csharp
public string InteractionIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_MovingPatientBedController_IsIntravenousStandInstalled"></a> IsIntravenousStandInstalled

```csharp
public bool IsIntravenousStandInstalled { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_IsNormalSalineInstalled"></a> IsNormalSalineInstalled

```csharp
public bool IsNormalSalineInstalled { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_IsPlasmaSolutionInstalled"></a> IsPlasmaSolutionInstalled

```csharp
public bool IsPlasmaSolutionInstalled { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_LatchedPositioningPoint"></a> LatchedPositioningPoint

```csharp
public MovingPatientBedPositioningPoint LatchedPositioningPoint { get; }
```

#### Property Value

 [MovingPatientBedPositioningPoint](TriageTrainer.Entity.MovingPatientBedPositioningPoint.md)

### <a id="TriageTrainer_Entity_MovingPatientBedController_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_ReposedTarget"></a> ReposedTarget

```csharp
public IReposable ReposedTarget { get; }
```

#### Property Value

 [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

### <a id="TriageTrainer_Entity_MovingPatientBedController_ReposedTargetIdentifier"></a> ReposedTargetIdentifier

```csharp
public string ReposedTargetIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedController_RequiredInteractorCount"></a> RequiredInteractorCount

```csharp
public int RequiredInteractorCount { get; }
```

#### Property Value

 int

### <a id="TriageTrainer_Entity_MovingPatientBedController_ShouldLogMovementBlockers"></a> ShouldLogMovementBlockers

Enables collision-origin diagnostics for a specific derived controller.

```csharp
protected override bool ShouldLogMovementBlockers { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_Weight"></a> Weight

```csharp
public int Weight { get; }
```

#### Property Value

 int

## Methods

### <a id="TriageTrainer_Entity_MovingPatientBedController_ApplyParentEntityIdentifier_System_String_"></a> ApplyParentEntityIdentifier\(string\)

엔티티 프리셋 스폰 시 부모(루트) 식별자를 전달받는다(IEntityPresetParentLinkReceiver).
환자(부모) 위에 이 침대(하위)가 결합되는 의미이므로, 부모 환자 식별자를 결합 권위값으로 설정한다.
프리셋 스폰은 서버에서 수행되므로 보통 즉시 권위값이 설정되고 전 피어로 복제된다.

```csharp
public void ApplyParentEntityIdentifier(string parentEntityIdentifier)
```

#### Parameters

`parentEntityIdentifier` string

### <a id="TriageTrainer_Entity_MovingPatientBedController_ApplySpawnedEntityIdentifier_System_String_"></a> ApplySpawnedEntityIdentifier\(string\)

Called by server/network system to assign a runtime entity identifier.
Registers this bed in the global Registry if an identifier is provided.

```csharp
public void ApplySpawnedEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

Server-assigned entity identifier (e.g., "moving_patient_bed:{uuid}"), or null to defer registration.

### <a id="TriageTrainer_Entity_MovingPatientBedController_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_EnsureAllowedPositioningPointIdentifier_System_String_"></a> EnsureAllowedPositioningPointIdentifier\(string\)

특정 포지셔닝 포인트 식별자를 스냅 허용 목록에 보장한다.
허용 목록이 비어 있으면(=모든 포인트 허용) 아무 작업도 하지 않는다.

```csharp
public void EnsureAllowedPositioningPointIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_MovingPatientBedController_EnsureNormalSalineInstalledForScenario"></a> EnsureNormalSalineInstalledForScenario\(\)

시나리오 수동 진입용으로 생리식염수 수액걸이를 즉시 준비한다.

```csharp
public void EnsureNormalSalineInstalledForScenario()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_EnsurePlasmaSolutionInstalledForScenario"></a> EnsurePlasmaSolutionInstalledForScenario\(\)

시나리오 수동 진입용으로 플라즈마 솔루션 수액걸이를 즉시 준비한다.
생리식염수 쪽과 같은 규약이며, 수액이 걸려 있지 않으면
<xref href="TriageTrainer.Entity.MovingPatientBedController.TryGetPlasmaSolutionConnectionPoint(TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint%40)" data-throw-if-not-resolved="false"></xref> 가 연결 지점을 돌려주지 않아
우측 정맥로 복원이 조용히 실패한다.

```csharp
public void EnsurePlasmaSolutionInstalledForScenario()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_ForceReleaseAllParticipants"></a> ForceReleaseAllParticipants\(\)

현재 침대를 잡고 있는 모든 플레이어의 조종 상태를 서버 권위로 강제 해제한다.
스냅 직후 같은 프레임에 호출하면 참가자 고정(anchor)과 조종 상태가 동시에 해제된다.

```csharp
public bool ForceReleaseAllParticipants()
```

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Entity_MovingPatientBedController_IsNormalSalineConnectionPoint_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> IsNormalSalineConnectionPoint\(IntravenousLineConnectionPoint\)

```csharp
public bool IsNormalSalineConnectionPoint(IntravenousLineConnectionPoint point)
```

#### Parameters

`point` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_IsPlasmaSolutionConnectionPoint_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> IsPlasmaSolutionConnectionPoint\(IntravenousLineConnectionPoint\)

```csharp
public bool IsPlasmaSolutionConnectionPoint(IntravenousLineConnectionPoint point)
```

#### Parameters

`point` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnAttacked_MultiplayerInfrastructure_Entity_Entity_System_Int32_"></a> OnAttacked\(Entity, int\)

```csharp
public void OnAttacked(Entity attacker, int damageAmount)
```

#### Parameters

`attacker` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

`damageAmount` int

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnItemUsed_MultiplayerInfrastructure_Entity_Entity_System_String_"></a> OnItemUsed\(Entity, string\)

```csharp
public void OnItemUsed(Entity user, string itemIdentifier)
```

#### Parameters

`user` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

`itemIdentifier` string

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnServerParticipantEntered_System_Int32_MultiplayerInfrastructure_Player_PlayerController_System_Int32_"></a> OnServerParticipantEntered\(int, PlayerController, int\)

```csharp
protected override void OnServerParticipantEntered(int clientId, PlayerController player, int handle)
```

#### Parameters

`clientId` int

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`handle` int

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnServerParticipantExited_System_Int32_MultiplayerInfrastructure_Player_PlayerController_System_Int32_"></a> OnServerParticipantExited\(int, PlayerController, int\)

```csharp
protected override void OnServerParticipantExited(int clientId, PlayerController player, int handle)
```

#### Parameters

`clientId` int

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`handle` int

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_RefreshDisplayName"></a> RefreshDisplayName\(\)

```csharp
public void RefreshDisplayName()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_ResetDismountCompletionTracking"></a> ResetDismountCompletionTracking\(\)

```csharp
public void ResetDismountCompletionTracking()
```

### <a id="TriageTrainer_Entity_MovingPatientBedController_ResolveArrivalSignalEntity"></a> ResolveArrivalSignalEntity\(\)

```csharp
public IScenarioIdentifiedEntity ResolveArrivalSignalEntity()
```

#### Returns

 [IScenarioIdentifiedEntity](MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity.md)

### <a id="TriageTrainer_Entity_MovingPatientBedController_SetIdentifier_System_String_"></a> SetIdentifier\(string\)

런타임 엔티티 식별자를 설정한다. 서버에서 호출되면 SyncVar 로 전 피어에 복제되고,
모든 피어가 동일 식별자로 레지스트리에 등록한다(원격 클라에서도 식별자 기반 조회/결합이 동작하도록).
등록 자체는 SyncVar 경로(OnStartClient/OnChange)에서 수행한다.

```csharp
public void SetIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_MovingPatientBedController_SetMovementInteractionEnabled_System_Boolean_System_Boolean_"></a> SetMovementInteractionEnabled\(bool, bool\)

침대 이동 상호작용(손잡이 탑승/해제)을 런타임에서 활성/비활성화한다.
비활성화 시 현재 참가자를 함께 해제할 수 있다.

```csharp
public void SetMovementInteractionEnabled(bool enabled, bool releaseParticipantsIfDisabled = true)
```

#### Parameters

`enabled` bool

`releaseParticipantsIfDisabled` bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_SetPatientReposeEnabled_System_Boolean_"></a> SetPatientReposeEnabled\(bool\)

침대 이동만 재사용하는 장비가 환자 내려놓기 메뉴를 숨길 수 있게 한다.

```csharp
public void SetPatientReposeEnabled(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_SetReposedTargetByIdentifier_System_String_"></a> SetReposedTargetByIdentifier\(string\)

서버에서 결합 권위값을 설정한다. 식별자가 비어 있으면 결합 해제. 모든 피어로 복제된다.
비서버 컨텍스트에서 호출되면 ServerRpc 로 위임한다.

```csharp
public void SetReposedTargetByIdentifier(string patientIdentifier)
```

#### Parameters

`patientIdentifier` string

### <a id="TriageTrainer_Entity_MovingPatientBedController_ShouldIgnoreMovementBlocker_UnityEngine_Collider_"></a> ShouldIgnoreMovementBlocker\(Collider\)

Derived controls may exclude colliders that move as part of their controlled payload.

```csharp
protected override bool ShouldIgnoreMovementBlocker(Collider collider)
```

#### Parameters

`collider` Collider

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryAttachCurrentHandlingItem_MultiplayerInfrastructure_Entity_Entity_"></a> TryAttachCurrentHandlingItem\(Entity\)

```csharp
public bool TryAttachCurrentHandlingItem(Entity actorEntity)
```

#### Parameters

`actorEntity` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryAttachItem_System_String_"></a> TryAttachItem\(string\)

```csharp
public bool TryAttachItem(string itemIdentifier)
```

#### Parameters

`itemIdentifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryForceSnapToPositioningPoint_System_String_"></a> TryForceSnapToPositioningPoint\(string\)

지정한 포지셔닝 포인트 식별자에 도달했을 때 즉시 스냅을 적용한다.
서버 권위에서만 동작하며, 성공 시 기존 자동 스냅과 동일한 신호를 발행한다.

```csharp
public bool TryForceSnapToPositioningPoint(string pointIdentifier)
```

#### Parameters

`pointIdentifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryForceSnapToPositioningPoint_System_String_System_Boolean_"></a> TryForceSnapToPositioningPoint\(string, bool\)

지정한 포지셔닝 포인트에 스냅한다.

```csharp
public bool TryForceSnapToPositioningPoint(string pointIdentifier, bool teleportToPoint)
```

#### Parameters

`pointIdentifier` string

대상 포지셔닝 포인트 식별자.

`teleportToPoint` bool

true 면 현재 거리와 무관하게 침대를 포인트 위치로 옮긴 뒤 붙인다. 시나리오가 재생 위치를
건너뛰어 침대를 밀고 온 과정 자체가 없었던 경우에 쓴다.
false 면 기존 동작대로 스냅 범위 안에 있을 때만 붙는다.

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryGetNormalSalineConnectionPoint_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint__"></a> TryGetNormalSalineConnectionPoint\(out IntravenousLineConnectionPoint\)

```csharp
public bool TryGetNormalSalineConnectionPoint(out IntravenousLineConnectionPoint point)
```

#### Parameters

`point` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryGetPlasmaSolutionConnectionPoint_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint__"></a> TryGetPlasmaSolutionConnectionPoint\(out IntravenousLineConnectionPoint\)

```csharp
public bool TryGetPlasmaSolutionConnectionPoint(out IntravenousLineConnectionPoint point)
```

#### Parameters

`point` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryLiftTarget_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Entity_IReposable__"></a> TryLiftTarget\(PlayerController, out IReposable\)

```csharp
public bool TryLiftTarget(PlayerController player, out IReposable lifted)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`lifted` [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedController_TryReposeTarget_MultiplayerInfrastructure_Entity_IReposable_UnityEngine_Transform_"></a> TryReposeTarget\(IReposable, Transform\)

```csharp
public bool TryReposeTarget(IReposable target, Transform interactor = null)
```

#### Parameters

`target` [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

`interactor` Transform

#### Returns

 bool

