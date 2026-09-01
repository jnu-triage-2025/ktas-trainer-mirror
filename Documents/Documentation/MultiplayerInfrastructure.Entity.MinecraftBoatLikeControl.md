# <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl"></a> Class MinecraftBoatLikeControl

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

Minecraft 보트 방식의 탑승/점유/이동을 제공하는 공통 네트워크 모듈.
도메인 객체(환자 침대, 의료 장비 등)의 상태나 상호작용은 소유하지 않는다.

```csharp
[RequireComponent(typeof(NetworkObject))]
public abstract class MinecraftBoatLikeControl : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[MinecraftBoatLikeControl](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md)

#### Derived

[DefibrillatorCartController](TriageTrainer.Entity.DefibrillatorCartController.md), 
[Level1RapidInfuserController](TriageTrainer.Entity.Level1RapidInfuserController.md), 
[MovingPatientBedController](TriageTrainer.Entity.MovingPatientBedController.md)

## Fields

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_DefaultControlIconResourcePath"></a> DefaultControlIconResourcePath

MinecraftBoatLikeControl을 사용하는 모든 조종 가능 엔티티의 기본 상호작용 아이콘
Resources 경로입니다. 인스펙터에서 아이콘을 지정하지 않으면 이 경로에서 자동으로
스프라이트를 로드합니다. 스프라이트 파일만 이 경로에 배치하면 코드 수정 없이
기본 아이콘이 적용됩니다.

```csharp
public const string DefaultControlIconResourcePath = "Textures/Icons/moving-object"
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_Capacity"></a> Capacity

```csharp
public int Capacity { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_HasParticipants"></a> HasParticipants

현재 조종을 위해 이 객체에 탑승한 참가자가 하나 이상 있는지 여부.

```csharp
public bool HasParticipants { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_IsLocallyControlled"></a> IsLocallyControlled

이 클라이언트에서 현재 조종 중인 참가자가 하나 이상 있는지 여부.

```csharp
public bool IsLocallyControlled { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_ResolvedDefaultControlIcon"></a> ResolvedDefaultControlIcon

이 조종체의 기본 아이콘 스프라이트를 반환합니다.
<xref href="MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl._defaultControlIcon" data-throw-if-not-resolved="false"></xref>가 인스펙터에서 지정되면 해당 값을 사용하고,
비어 있으면 <xref href="MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.DefaultControlIconResourcePath" data-throw-if-not-resolved="false"></xref> 경로에서 Resources.Load로
자동 로드합니다. 로드 실패 시 <xref href="MultiplayerInfrastructure.Definitions.DefaultsResource.FallbackSprite" data-throw-if-not-resolved="false"></xref> 를 반환합니다.

```csharp
protected Sprite ResolvedDefaultControlIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_ShouldLogMovementBlockers"></a> ShouldLogMovementBlockers

Enables collision-origin diagnostics for a specific derived controller.

```csharp
protected virtual bool ShouldLogMovementBlockers { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_Awake_MinecraftBoatLikeControl"></a> Awake\_MinecraftBoatLikeControl\(\)

```csharp
protected void Awake_MinecraftBoatLikeControl()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_CanToggle_UnityEngine_Transform_"></a> CanToggle\(Transform\)

```csharp
public bool CanToggle(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_ClearLocalParticipants"></a> ClearLocalParticipants\(\)

```csharp
protected void ClearLocalParticipants()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_Configure_System_Int32_System_String_"></a> Configure\(int, string\)

```csharp
protected void Configure(int maximumParticipants, string exitHint = null)
```

#### Parameters

`maximumParticipants` int

`exitHint` string

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_DetachAllParticipants"></a> DetachAllParticipants\(\)

현재 이 탈것에 붙어 있는 모든 참가자를 분리한다.
입력 기반 하차뿐 아니라 시나리오 그래프 같은 외부 시스템에서도 동일한
네트워크 상태 전이를 사용할 수 있도록 공개한다.

```csharp
public bool DetachAllParticipants()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_OnServerParticipantEntered_System_Int32_MultiplayerInfrastructure_Player_PlayerController_System_Int32_"></a> OnServerParticipantEntered\(int, PlayerController, int\)

```csharp
protected virtual void OnServerParticipantEntered(int clientId, PlayerController player, int handle)
```

#### Parameters

`clientId` int

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`handle` int

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_OnServerParticipantExited_System_Int32_MultiplayerInfrastructure_Player_PlayerController_System_Int32_"></a> OnServerParticipantExited\(int, PlayerController, int\)

```csharp
protected virtual void OnServerParticipantExited(int clientId, PlayerController player, int handle)
```

#### Parameters

`clientId` int

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`handle` int

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_SetAuthoritativeTransform_UnityEngine_Vector3_UnityEngine_Quaternion_"></a> SetAuthoritativeTransform\(Vector3, Quaternion\)

서버(또는 오프라인 실행)가 조종 오브젝트의 위치를 즉시 보정할 때 사용한다.
서버에서 적용한 값은 모든 관찰 클라이언트에도 같은 프레임에 전달된다.

```csharp
protected void SetAuthoritativeTransform(Vector3 position, Quaternion rotation)
```

#### Parameters

`position` Vector3

`rotation` Quaternion

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_SetMinimumMovementDivisor_System_Int32_"></a> SetMinimumMovementDivisor\(int\)

```csharp
public void SetMinimumMovementDivisor(int value)
```

#### Parameters

`value` int

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_ShouldIgnoreMovementBlocker_UnityEngine_Collider_"></a> ShouldIgnoreMovementBlocker\(Collider\)

Derived controls may exclude colliders that move as part of their controlled payload.

```csharp
protected virtual bool ShouldIgnoreMovementBlocker(Collider collider)
```

#### Parameters

`collider` Collider

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_Toggle_UnityEngine_Transform_"></a> Toggle\(Transform\)

```csharp
public void Toggle(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_TryGetSingleMovingParticipantConnection_FishNet_Connection_NetworkConnection__"></a> TryGetSingleMovingParticipantConnection\(out NetworkConnection\)

현재 서버 이동에 실제 입력을 제공하는 참가자가 정확히 한 명일 때만 그 연결을 반환한다.
협동 이동처럼 행동 주체가 여럿인 경우에는 임의의 한 명에게 후속 이벤트를 귀속하지 않는다.

```csharp
protected bool TryGetSingleMovingParticipantConnection(out NetworkConnection connection)
```

#### Parameters

`connection` NetworkConnection

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_Update_MinecraftBoatLikeControl"></a> Update\_MinecraftBoatLikeControl\(\)

```csharp
protected void Update_MinecraftBoatLikeControl()
```

### <a id="MultiplayerInfrastructure_Entity_MinecraftBoatLikeControl_ParticipantAssigned"></a> ParticipantAssigned

```csharp
public event Action<int> ParticipantAssigned
```

#### Event Type

 Action<int\>

