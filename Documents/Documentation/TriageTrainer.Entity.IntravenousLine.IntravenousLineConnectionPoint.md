# <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint"></a> Class IntravenousLineConnectionPoint

Namespace: [TriageTrainer.Entity.IntravenousLine](TriageTrainer.Entity.IntravenousLine.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[RequireComponent(typeof(SphereCollider))]
public class IntravenousLineConnectionPoint : LineConnectionPoint, IInteractable
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md) ← 
[IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md)

#### Inherited Members

[LineConnectionPoint.OnValidate\(\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_OnValidate), 
[LineConnectionPoint.LineMaterial](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_LineMaterial), 
[LineConnectionPoint.ConnectionIdentifier](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_ConnectionIdentifier), 
[LineConnectionPoint.HasAnyConnection](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_HasAnyConnection), 
[LineConnectionPoint.CanAcceptAdditionalConnection](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_CanAcceptAdditionalConnection), 
[LineConnectionPoint.CanConnectTo\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_CanConnectTo\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.CanPlayerCompleteConnection\(PlayerController, LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_CanPlayerCompleteConnection\_MultiplayerInfrastructure\_Player\_PlayerController\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.TryConsumeConnectionRequirement\(PlayerController\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_TryConsumeConnectionRequirement\_MultiplayerInfrastructure\_Player\_PlayerController\_), 
[LineConnectionPoint.SetAllowsMultipleConnections\(bool\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_SetAllowsMultipleConnections\_System\_Boolean\_), 
[LineConnectionPoint.RegisterConnectedLineObject\(GameObject\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_RegisterConnectedLineObject\_UnityEngine\_GameObject\_), 
[LineConnectionPoint.UnregisterConnectedLineObject\(GameObject\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_UnregisterConnectedLineObject\_UnityEngine\_GameObject\_), 
[LineConnectionPoint.TryGetAnyConnectedLineObject\(out GameObject\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_TryGetAnyConnectedLineObject\_UnityEngine\_GameObject\_\_), 
[LineConnectionPoint.IsPhysicallyConnectedTo\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_IsPhysicallyConnectedTo\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.TryGetConnectedLineObjectTo\(LineConnectionPoint, out GameObject\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_TryGetConnectedLineObjectTo\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_UnityEngine\_GameObject\_\_), 
[LineConnectionPoint.ApplyLineMaterial\(LineRenderer\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_ApplyLineMaterial\_UnityEngine\_LineRenderer\_), 
[LineConnectionPoint.NotifyConnectionStarted\(\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_NotifyConnectionStarted), 
[LineConnectionPoint.NotifyConnectionCompleted\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_NotifyConnectionCompleted\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.NotifyLineConnected\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_NotifyLineConnected\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.NotifyLineDisconnected\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_NotifyLineDisconnected\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.NotifyReplicatedLineConnected\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_NotifyReplicatedLineConnected\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_), 
[LineConnectionPoint.NotifyReplicatedLineDisconnected\(LineConnectionPoint\)](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md\#TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_NotifyReplicatedLineDisconnected\_TriageTrainer\_Entity\_LineConnection\_LineConnectionPoint\_)

## Fields

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_ConnectStartSignalPrefix"></a> ConnectStartSignalPrefix

연결 작업 시작(한 점 연결) 시 인게임 서버로 올리는 신호 접두사. 뒤에 지점 Identifier 가 붙는다.

```csharp
public const string ConnectStartSignalPrefix = "iv_connect_start_"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_ConnectedSignalPrefix"></a> ConnectedSignalPrefix

연결 완료 시 인게임 서버로 올리는 신호 접두사. 뒤에 지점 Identifier 가 붙는다.

```csharp
public const string ConnectedSignalPrefix = "iv_connected_"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_DisconnectedSignalPrefix"></a> DisconnectedSignalPrefix

연결 끊김 시 인게임 서버로 올리는 신호 접두사. 뒤에 지점 Identifier 가 붙는다.

```csharp
public const string DisconnectedSignalPrefix = "iv_disconnected_"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_Elasticity"></a> Elasticity

```csharp
public const float Elasticity = 0.21
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_InteractIdConnectHere"></a> InteractIdConnectHere

```csharp
public const string InteractIdConnectHere = "intravenous_line_connect_here"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_InteractIdDisconnect"></a> InteractIdDisconnect

```csharp
public const string InteractIdDisconnect = "intravenous_line_disconnect"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_InteractIdStartConnectionMode"></a> InteractIdStartConnectionMode

```csharp
public const string InteractIdStartConnectionMode = "intravenous_line_connect_mode_start"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_LineWidth"></a> LineWidth

```csharp
public const float LineWidth = 0.015
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_MaterialResourcePath"></a> MaterialResourcePath

```csharp
public const string MaterialResourcePath = "Materials/LineConnectionService/IntravenousLine"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_DefaultMaterial"></a> DefaultMaterial

```csharp
public static Material DefaultMaterial { get; }
```

#### Property Value

 Material

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

## Methods

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_ApplyLineMaterial_UnityEngine_LineRenderer_"></a> ApplyLineMaterial\(LineRenderer\)

Called by LineConnectionService through a concrete point type branch.

```csharp
public override void ApplyLineMaterial(LineRenderer lineRenderer)
```

#### Parameters

`lineRenderer` LineRenderer

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_CanPlayerCompleteConnection_MultiplayerInfrastructure_Player_PlayerController_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> CanPlayerCompleteConnection\(PlayerController, LineConnectionPoint\)

```csharp
public override bool CanPlayerCompleteConnection(PlayerController player, LineConnectionPoint other)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_HasRequiredItem_MultiplayerInfrastructure_Player_PlayerController_"></a> HasRequiredItem\(PlayerController\)

```csharp
public bool HasRequiredItem(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_IsInteractEnabled_System_String_"></a> IsInteractEnabled\(string\)

```csharp
public bool IsInteractEnabled(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyConnectStart"></a> NotifyConnectStart\(\)

연결 작업 시작(한 점 연결)을 알린다. C# 이벤트를 발화하고, 인게임 서버에
이 지점 Identifier 와 함께 "연결 시도" 시그널을 올린다.

```csharp
public void NotifyConnectStart()
```

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyConnected_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> NotifyConnected\(IntravenousLineConnectionPoint\)

연결 완료를 알린다. C# 이벤트를 발화하고, 인게임 서버에 이 지점 Identifier 와
함께 "연결 완료" 시그널을 올린다.

```csharp
public void NotifyConnected(IntravenousLineConnectionPoint other)
```

#### Parameters

`other` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyConnectionCompleted_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyConnectionCompleted\(LineConnectionPoint\)

Called once from the start point after a line is created.

```csharp
public override void NotifyConnectionCompleted(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyConnectionStarted"></a> NotifyConnectionStarted\(\)

Called when this point begins a line-connection operation.

```csharp
public override void NotifyConnectionStarted()
```

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyDisconnected_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> NotifyDisconnected\(IntravenousLineConnectionPoint\)

줄 하나의 연결 끊김을 알린다. C# 이벤트를 발화하고, 인게임 서버에 이 지점
Identifier 와 함께 "연결 끊김" 시그널을 올린다. 끊긴 줄마다 호출되며,
이 지점에 다른 연결이 남아 있는지 여부와 무관하게 매번 알린다.

```csharp
public void NotifyDisconnected(IntravenousLineConnectionPoint other = null)
```

#### Parameters

`other` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

끊긴 줄의 상대 지점(알 수 없으면 null).

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyLineConnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyLineConnected\(LineConnectionPoint\)

Called for each endpoint after a line is created.

```csharp
public override void NotifyLineConnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyLineDisconnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyLineDisconnected\(LineConnectionPoint\)

Called for each endpoint when one of its lines is removed.

```csharp
public override void NotifyLineDisconnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyReplicatedLineConnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyReplicatedLineConnected\(LineConnectionPoint\)

Observer-only local lifecycle; must not emit authoritative signals.

```csharp
public override void NotifyReplicatedLineConnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_NotifyReplicatedLineDisconnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyReplicatedLineDisconnected\(LineConnectionPoint\)

Observer-only local lifecycle; must not emit authoritative signals.

```csharp
public override void NotifyReplicatedLineDisconnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_ResolveController"></a> ResolveController\(\)

```csharp
public LineConnectionService ResolveController()
```

#### Returns

 [LineConnectionService](TriageTrainer.Entity.LineConnection.LineConnectionService.md)

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_SetAllInteractionsEnabled_System_Boolean_"></a> SetAllInteractionsEnabled\(bool\)

이 연결 지점이 제공하는 모든 플레이어 상호작용을 일괄로 켜거나 끈다.

```csharp
public void SetAllInteractionsEnabled(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_SetIdentifier_System_String_"></a> SetIdentifier\(string\)

```csharp
public void SetIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_TryConsumeConnectionRequirement_MultiplayerInfrastructure_Player_PlayerController_"></a> TryConsumeConnectionRequirement\(PlayerController\)

Consumes a point-specific connection requirement after common connection
validation succeeds. Points without a requirement accept by default.

```csharp
public override bool TryConsumeConnectionRequirement(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_TryConsumeRequiredItem_MultiplayerInfrastructure_Player_PlayerController_"></a> TryConsumeRequiredItem\(PlayerController\)

```csharp
public bool TryConsumeRequiredItem(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_OnConnectStart"></a> OnConnectStart

이 지점에서 수액 줄 연결 작업이 시작될 때(한 점만 연결된 상태) 발생한다.
인자: 연결 시작 지점(this).

```csharp
public event Action<IntravenousLineConnectionPoint> OnConnectStart
```

#### Event Type

 Action<[IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)\>

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_OnConnected"></a> OnConnected

이 지점을 포함하는 수액 줄 연결이 완료되었을 때 발생한다.
인자: (this 지점, 상대 지점). 양 끝점 모두에서 각각 발생한다.

```csharp
public event Action<IntravenousLineConnectionPoint, IntravenousLineConnectionPoint> OnConnected
```

#### Event Type

 Action<[IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md), [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)\>

### <a id="TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_OnDisconnected"></a> OnDisconnected

이 지점을 포함하던 수액 줄 하나가 끊겼을 때 발생한다.
규약: "줄 단위" 이벤트다. 끊긴 줄마다 그 줄의 양 끝점에서 각각 발생하며,
지점에 다른 연결이 더 남아 있는지 여부와 무관하게 매번 발생한다
(OnConnected 와 대칭). 지점이 완전히 비연결 상태가 되는 시점만 알고 싶다면
수신 측에서 HasAnyConnection 로 확인한다.
인자: (this 지점, 끊긴 줄의 상대 지점 또는 알 수 없으면 null).

```csharp
public event Action<IntravenousLineConnectionPoint, IntravenousLineConnectionPoint> OnDisconnected
```

#### Event Type

 Action<[IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md), [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)\>

