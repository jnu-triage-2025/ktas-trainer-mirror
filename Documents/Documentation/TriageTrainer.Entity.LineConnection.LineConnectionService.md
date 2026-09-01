# <a id="TriageTrainer_Entity_LineConnection_LineConnectionService"></a> Class LineConnectionService

Namespace: [TriageTrainer.Entity.LineConnection](TriageTrainer.Entity.LineConnection.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class LineConnectionService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LineConnectionService](TriageTrainer.Entity.LineConnection.LineConnectionService.md)

## Properties

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_ActiveAuthoritativePairCount"></a> ActiveAuthoritativePairCount

```csharp
public int ActiveAuthoritativePairCount { get; }
```

#### Property Value

 int

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_TopologyService"></a> TopologyService

```csharp
public static LineConnectionService TopologyService { get; }
```

#### Property Value

 [LineConnectionService](TriageTrainer.Entity.LineConnection.LineConnectionService.md)

## Methods

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_AddAuthoritativePair_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> AddAuthoritativePair\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool AddAuthoritativePair(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_ApplyReplicatedConnection_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> ApplyReplicatedConnection\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool ApplyReplicatedConnection(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_ApplyReplicatedDisconnect_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> ApplyReplicatedDisconnect\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool ApplyReplicatedDisconnect(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_ApplyReplicatedSnapshotPair_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> ApplyReplicatedSnapshotPair\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool ApplyReplicatedSnapshotPair(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_BeginConnectionMode_MultiplayerInfrastructure_Player_PlayerController_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> BeginConnectionMode\(PlayerController, LineConnectionPoint\)

```csharp
public void BeginConnectionMode(PlayerController player, LineConnectionPoint startPoint)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`startPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_BeginReplicatedTopologySnapshot"></a> BeginReplicatedTopologySnapshot\(\)

```csharp
public void BeginReplicatedTopologySnapshot()
```

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_BeginReplicatedTopologySnapshot_System_Int64_"></a> BeginReplicatedTopologySnapshot\(long\)

```csharp
public void BeginReplicatedTopologySnapshot(long version)
```

#### Parameters

`version` long

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_CancelConnectionMode_MultiplayerInfrastructure_Player_PlayerController_"></a> CancelConnectionMode\(PlayerController\)

```csharp
public void CancelConnectionMode(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_ContainsAuthoritativePair_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> ContainsAuthoritativePair\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool ContainsAuthoritativePair(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_CreateAndRegisterConnection_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> CreateAndRegisterConnection\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool CreateAndRegisterConnection(LineConnectionPoint startPoint, LineConnectionPoint endPoint)
```

#### Parameters

`startPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`endPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_DisconnectAutomaticConnection_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> DisconnectAutomaticConnection\(LineConnectionPoint, LineConnectionPoint\)

Removes only the specified automatically managed endpoint pair.

```csharp
public bool DisconnectAutomaticConnection(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_DisconnectFromPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_MultiplayerInfrastructure_Player_PlayerController_"></a> DisconnectFromPoint\(LineConnectionPoint, PlayerController\)

```csharp
public void DisconnectFromPoint(LineConnectionPoint point, PlayerController player = null)
```

#### Parameters

`point` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_EndReplicatedTopologySnapshot"></a> EndReplicatedTopologySnapshot\(\)

```csharp
public void EndReplicatedTopologySnapshot()
```

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_EndReplicatedTopologySnapshot_System_Int64_"></a> EndReplicatedTopologySnapshot\(long\)

```csharp
public void EndReplicatedTopologySnapshot(long version)
```

#### Parameters

`version` long

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_HasPendingStartPoint_MultiplayerInfrastructure_Player_PlayerController_TriageTrainer_Entity_LineConnection_LineConnectionPoint__"></a> HasPendingStartPoint\(PlayerController, out LineConnectionPoint\)

```csharp
public bool HasPendingStartPoint(PlayerController player, out LineConnectionPoint startPoint)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`startPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_IsExactPatientNormalSalineEndpointPair_TriageTrainer_Entity_PatientController_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> IsExactPatientNormalSalineEndpointPair\(PatientController, IntravenousLineConnectionPoint, LineConnectionPoint, LineConnectionPoint\)

```csharp
public static bool IsExactPatientNormalSalineEndpointPair(PatientController patient, IntravenousLineConnectionPoint salinePoint, LineConnectionPoint startPoint, LineConnectionPoint endPoint)
```

#### Parameters

`patient` [PatientController](TriageTrainer.Entity.PatientController.md)

`salinePoint` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

`startPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`endPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_RemoveAuthoritativePair_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> RemoveAuthoritativePair\(LineConnectionPoint, LineConnectionPoint\)

```csharp
public bool RemoveAuthoritativePair(LineConnectionPoint first, LineConnectionPoint second)
```

#### Parameters

`first` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`second` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_TryCompleteConnection_MultiplayerInfrastructure_Player_PlayerController_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> TryCompleteConnection\(PlayerController, LineConnectionPoint\)

Completes a generic line connection after the start point consumes any
point-specific requirement it defines.

```csharp
public bool TryCompleteConnection(PlayerController player, LineConnectionPoint endPoint)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`endPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionService_TryCreateAutomaticConnection_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> TryCreateAutomaticConnection\(LineConnectionPoint, LineConnectionPoint\)

Creates an authoritative connection without a player interaction or item
consumption. This is reserved for newly installed CareZone equipment.

```csharp
public bool TryCreateAutomaticConnection(LineConnectionPoint startPoint, LineConnectionPoint endPoint)
```

#### Parameters

`startPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`endPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

#### Returns

 bool

