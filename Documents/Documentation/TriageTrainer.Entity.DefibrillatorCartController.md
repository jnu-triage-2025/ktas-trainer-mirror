# <a id="TriageTrainer_Entity_DefibrillatorCartController"></a> Class DefibrillatorCartController

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

제세동 카트의 1인 조종을 전담하는 컨트롤러.
탑승/점유/이동의 네트워크 구현은 도메인 독립 공통 모듈
<xref href="MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl" data-throw-if-not-resolved="false"></xref> 에 위임한다(<xref href="TriageTrainer.Entity.Level1RapidInfuserController" data-throw-if-not-resolved="false"></xref> 와 동일한 상속 패턴).

카트가 허용된 snap point에 도달하면 서버 권위로 스냅하고 시나리오 신호
defibrillator_cart_snap_point_reached_{카트 식별자}_{포인트 식별자} 를 발생시킨다.

```csharp
public sealed class DefibrillatorCartController : MinecraftBoatLikeControl, IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[MinecraftBoatLikeControl](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md) ← 
[DefibrillatorCartController](TriageTrainer.Entity.DefibrillatorCartController.md)

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

## Properties

### <a id="TriageTrainer_Entity_DefibrillatorCartController_AedConnectionPoints"></a> AedConnectionPoints

카트 측 AED 라인 연결 지점 목록. 참조가 비어 있으면(배선 누락)
자식 오브젝트에서 클래스 기준으로 찾는 fallback 을 수행한다.

```csharp
public IReadOnlyList<AEDLineConnectionPoint> AedConnectionPoints { get; }
```

#### Property Value

 IReadOnlyList<[AEDLineConnectionPoint](TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint.md)\>

### <a id="TriageTrainer_Entity_DefibrillatorCartController_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartController_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Entity_DefibrillatorCartController_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Entity_DefibrillatorCartController_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_DefibrillatorCartController_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_DefibrillatorCartController_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_DefibrillatorCartController_LatchedSnapPoint"></a> LatchedSnapPoint

```csharp
public DefibrillatorCartSnapPoint LatchedSnapPoint { get; }
```

#### Property Value

 [DefibrillatorCartSnapPoint](TriageTrainer.Entity.DefibrillatorCartSnapPoint.md)

### <a id="TriageTrainer_Entity_DefibrillatorCartController_ShouldLogMovementBlockers"></a> ShouldLogMovementBlockers

Enables collision-origin diagnostics for a specific derived controller.

```csharp
protected override bool ShouldLogMovementBlockers { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Entity_DefibrillatorCartController_ApplySpawnedEntityIdentifier_System_String_"></a> ApplySpawnedEntityIdentifier\(string\)

엔티티 프리셋 스폰 시 식별자를 주입받는다(ISpawnedEntityIdentifierReceiver).

```csharp
public void ApplySpawnedEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_DefibrillatorCartController_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartController_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Entity_DefibrillatorCartController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="TriageTrainer_Entity_DefibrillatorCartController_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="TriageTrainer_Entity_DefibrillatorCartController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="TriageTrainer_Entity_DefibrillatorCartController_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

### <a id="TriageTrainer_Entity_DefibrillatorCartController_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_DefibrillatorCartController_PlaceForScenario_UnityEngine_Vector3_UnityEngine_Quaternion_"></a> PlaceForScenario\(Vector3, Quaternion\)

시나리오 수동 진입 준비 체인이 카트를 지정한 자리로 옮길 때 사용한다.
정박 판정은 <xref href="TriageTrainer.Entity.DefibrillatorCartController.TrySnapToSnapPoint" data-throw-if-not-resolved="false"></xref> 가 매 프레임 거리로 수행하므로, 이 메서드는
위치만 서버 권위로 옮기고 정박·도달 신호는 올리지 않는다. 신호는 "플레이어가 카트를 실제로
밀고 왔다"는 근거이므로 준비 체인이 대신 만들어서는 안 된다.

<p>
이전 회차에서 어딘가에 정박해 있었다면 그 참조를 먼저 끊는다. 정박 참조가 남아 있으면
다음 프레임의 판정이 이탈 처리를 거치면서 정박 해제 신호를 한 번 더 올린다.
</p>

```csharp
public void PlaceForScenario(Vector3 position, Quaternion rotation)
```

#### Parameters

`position` Vector3

`rotation` Quaternion

### <a id="TriageTrainer_Entity_DefibrillatorCartController_SetIdentifier_System_String_"></a> SetIdentifier\(string\)

런타임 엔티티 식별자를 설정한다. 서버에서 호출되면 SyncVar로 전 피어에 복제되고,
모든 피어가 동일 식별자로 레지스트리에 등록한다.

```csharp
public void SetIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_DefibrillatorCartController_ShouldIgnoreMovementBlocker_UnityEngine_Collider_"></a> ShouldIgnoreMovementBlocker\(Collider\)

카트는 바닥 높이에서 이동한다. 카트 원점보다 위로 솟지 않는 바닥 콜라이더가
수평 레이를 타일 이음새 충돌로 오인하지 않도록 무시한다(침대와 동일한 정책).

```csharp
protected override bool ShouldIgnoreMovementBlocker(Collider collider)
```

#### Parameters

`collider` Collider

#### Returns

 bool

