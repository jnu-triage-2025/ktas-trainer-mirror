# <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint"></a> Class OxyLineConnectionPoint

Namespace: [TriageTrainer.Entity.OxyLine](TriageTrainer.Entity.OxyLine.md)  
Assembly: Assembly\-CSharp.dll  

Marks the position where an oxygen line can be connected.
Connection behavior will be implemented separately.

```csharp
public sealed class OxyLineConnectionPoint : LineConnectionPoint
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md) ← 
[OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

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

### <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_Elasticity"></a> Elasticity

```csharp
public const float Elasticity = 0.2
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_LineWidth"></a> LineWidth

```csharp
public const float LineWidth = 0.035
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_MaterialResourcePath"></a> MaterialResourcePath

```csharp
public const string MaterialResourcePath = "Materials/LineConnectionService/OxyLine"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_DefaultMaterial"></a> DefaultMaterial

```csharp
public static Material DefaultMaterial { get; }
```

#### Property Value

 Material

## Methods

### <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_ApplyLineMaterial_UnityEngine_LineRenderer_"></a> ApplyLineMaterial\(LineRenderer\)

Called by LineConnectionService through a concrete point type branch.

```csharp
public override void ApplyLineMaterial(LineRenderer lineRenderer)
```

#### Parameters

`lineRenderer` LineRenderer

### <a id="TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_NotifyLineConnected_TriageTrainer_Entity_LineConnection_LineConnectionPoint_"></a> NotifyLineConnected\(LineConnectionPoint\)

서버가 산소 라인을 만든 직후, 환자 측 끝점에서만 B/C 산소 처치 완료를 판정한다.
CareZone의 장비 참조 할당만으로는 완료하지 않고 실제 물리 라인이 존재할 때만 호출된다.

```csharp
public override void NotifyLineConnected(LineConnectionPoint other)
```

#### Parameters

`other` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

