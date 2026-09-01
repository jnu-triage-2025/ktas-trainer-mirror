# <a id="TriageTrainer_Entity_AEDLine_AEDLineConnectionPoint"></a> Class AEDLineConnectionPoint

Namespace: [TriageTrainer.Entity.AEDLine](TriageTrainer.Entity.AEDLine.md)  
Assembly: Assembly\-CSharp.dll  

Marks the position where an AED line can be connected.
This component is network-aware; connection behavior will be implemented separately.

```csharp
public sealed class AEDLineConnectionPoint : LineConnectionPoint
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md) ← 
[AEDLineConnectionPoint](TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint.md)

#### Inherited Members

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

### <a id="TriageTrainer_Entity_AEDLine_AEDLineConnectionPoint_Elasticity"></a> Elasticity

```csharp
public const float Elasticity = 0.3
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_AEDLine_AEDLineConnectionPoint_LineWidth"></a> LineWidth

```csharp
public const float LineWidth = 0.03
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_AEDLine_AEDLineConnectionPoint_MaterialResourcePath"></a> MaterialResourcePath

```csharp
public const string MaterialResourcePath = "Materials/LineConnectionService/AEDLine"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_AEDLine_AEDLineConnectionPoint_DefaultMaterial"></a> DefaultMaterial

```csharp
public static Material DefaultMaterial { get; }
```

#### Property Value

 Material

## Methods

### <a id="TriageTrainer_Entity_AEDLine_AEDLineConnectionPoint_ApplyLineMaterial_UnityEngine_LineRenderer_"></a> ApplyLineMaterial\(LineRenderer\)

Called by LineConnectionService through a concrete point type branch.

```csharp
public override void ApplyLineMaterial(LineRenderer lineRenderer)
```

#### Parameters

`lineRenderer` LineRenderer

