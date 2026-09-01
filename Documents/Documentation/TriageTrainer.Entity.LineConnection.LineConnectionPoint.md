# <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint"></a> Class LineConnectionPoint

Namespace: [TriageTrainer.Entity.LineConnection](TriageTrainer.Entity.LineConnection.md)  
Assembly: Assembly\-CSharp.dll  

Common endpoint for a physical line. Concrete points own their interaction
and domain-specific behaviour; this class owns only connection state and
line presentation configuration.

```csharp
public abstract class LineConnectionPoint : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Derived

[AEDLineConnectionPoint](TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint.md), 
[CentralLineConnectionPoint](TriageTrainer.Entity.CentralLine.CentralLineConnectionPoint.md), 
[ElectricalLineConnectionPoint](TriageTrainer.Entity.ElectricalLine.ElectricalLineConnectionPoint.md), 
[IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md), 
[OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md), 
[SuctionLineConnectionPoint](TriageTrainer.Entity.SuctionLine.SuctionLineConnectionPoint.md)

## Properties

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_CanAcceptAdditionalConnection"></a> CanAcceptAdditionalConnection

```csharp
public bool CanAcceptAdditionalConnection { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_ConnectionIdentifier"></a> ConnectionIdentifier

```csharp
public string ConnectionIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_HasAnyConnection"></a> HasAnyConnection

```csharp
public bool HasAnyConnection { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_LineMaterial"></a> LineMaterial

```csharp
public Material LineMaterial { get; }
```

#### Property Value

 Material

## Methods

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_ApplyLineMaterial_UnityEngine_LineRenderer_"></a> ApplyLineMaterial\(LineRenderer\)

Called by LineConnectionService through a concrete point type branch.

```csharp
public virtual void ApplyLineMaterial(LineRenderer lineRenderer)
```

#### Parameters

`lineRenderer` LineRenderer

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_CanConnectTo_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> CanConnectTo\(LineConnectionPoint\)

A line connects only matching concrete point types by default. A point
that intentionally supports another type must opt in by overriding this.

```csharp
public virtual bool CanConnectTo(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_CanPlayerCompleteConnection_MultiplayerInfrastructure_Player_PlayerController_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> CanPlayerCompleteConnection\(PlayerController, LineConnectionPoint\)

```csharp
public virtual bool CanPlayerCompleteConnection(PlayerController player, LineConnectionPoint other)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_IsPhysicallyConnectedTo_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> IsPhysicallyConnectedTo\(LineConnectionPoint\)

```csharp
public bool IsPhysicallyConnectedTo(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_NotifyConnectionCompleted_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyConnectionCompleted\(LineConnectionPoint\)

Called once from the start point after a line is created.

```csharp
public virtual void NotifyConnectionCompleted(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_NotifyConnectionStarted"></a> NotifyConnectionStarted\(\)

Called when this point begins a line-connection operation.

```csharp
public virtual void NotifyConnectionStarted()
```

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_NotifyLineConnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyLineConnected\(LineConnectionPoint\)

Called for each endpoint after a line is created.

```csharp
public virtual void NotifyLineConnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_NotifyLineDisconnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyLineDisconnected\(LineConnectionPoint\)

Called for each endpoint when one of its lines is removed.

```csharp
public virtual void NotifyLineDisconnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_NotifyReplicatedLineConnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyReplicatedLineConnected\(LineConnectionPoint\)

Observer-only local lifecycle; must not emit authoritative signals.

```csharp
public virtual void NotifyReplicatedLineConnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_NotifyReplicatedLineDisconnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyReplicatedLineDisconnected\(LineConnectionPoint\)

Observer-only local lifecycle; must not emit authoritative signals.

```csharp
public virtual void NotifyReplicatedLineDisconnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_OnValidate"></a> OnValidate\(\)

```csharp
protected virtual void OnValidate()
```

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_RegisterConnectedLineObject_UnityEngine_GameObject_"></a> RegisterConnectedLineObject\(GameObject\)

```csharp
public void RegisterConnectedLineObject(GameObject lineObject)
```

#### Parameters

`lineObject` GameObject

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_SetAllowsMultipleConnections_System_Boolean_"></a> SetAllowsMultipleConnections\(bool\)

```csharp
public void SetAllowsMultipleConnections(bool allow)
```

#### Parameters

`allow` bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_TryConsumeConnectionRequirement_MultiplayerInfrastructure_Player_PlayerController_"></a> TryConsumeConnectionRequirement\(PlayerController\)

Consumes a point-specific connection requirement after common connection
validation succeeds. Points without a requirement accept by default.

```csharp
public virtual bool TryConsumeConnectionRequirement(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_TryGetAnyConnectedLineObject_UnityEngine_GameObject__"></a> TryGetAnyConnectedLineObject\(out GameObject\)

```csharp
public bool TryGetAnyConnectedLineObject(out GameObject lineObject)
```

#### Parameters

`lineObject` GameObject

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_TryGetConnectedLineObjectTo_TriageTrainer_Entity_LineConnection_LineConnectionPoint_UnityEngine_GameObject__"></a> TryGetConnectedLineObjectTo\(LineConnectionPoint, out GameObject\)

```csharp
public bool TryGetConnectedLineObjectTo(LineConnectionPoint other, out GameObject lineObject)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`lineObject` GameObject

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionPoint_UnregisterConnectedLineObject_UnityEngine_GameObject_"></a> UnregisterConnectedLineObject\(GameObject\)

```csharp
public void UnregisterConnectedLineObject(GameObject lineObject)
```

#### Parameters

`lineObject` GameObject

