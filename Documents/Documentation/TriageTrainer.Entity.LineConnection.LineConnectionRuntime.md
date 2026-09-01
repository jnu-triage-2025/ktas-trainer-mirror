# <a id="TriageTrainer_Entity_LineConnection_LineConnectionRuntime"></a> Class LineConnectionRuntime

Namespace: [TriageTrainer.Entity.LineConnection](TriageTrainer.Entity.LineConnection.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class LineConnectionRuntime : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LineConnectionRuntime](TriageTrainer.Entity.LineConnection.LineConnectionRuntime.md)

## Properties

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionRuntime_EndPoint"></a> EndPoint

```csharp
public LineConnectionPoint EndPoint { get; }
```

#### Property Value

 [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionRuntime_StartPoint"></a> StartPoint

```csharp
public LineConnectionPoint StartPoint { get; }
```

#### Property Value

 [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

## Methods

### <a id="TriageTrainer_Entity_LineConnection_LineConnectionRuntime_Bind_TriageTrainer_Entity_LineConnection_LineConnectionPoint_TriageTrainer_Entity_LineConnection_LineConnectionPoint_UnityEngine_LineRenderer_System_Int32_System_Single_System_Single_System_Boolean_System_Int32_System_Int32_System_Single_System_Single_System_Single_System_Boolean_System_Single_UnityEngine_LayerMask_UnityEngine_QueryTriggerInteraction_"></a> Bind\(LineConnectionPoint, LineConnectionPoint, LineRenderer, int, float, float, bool, int, int, float, float, float, bool, float, LayerMask, QueryTriggerInteraction\)

```csharp
public void Bind(LineConnectionPoint startPoint, LineConnectionPoint endPoint, LineRenderer lineRenderer, int segments, float sagAmount, float elasticity, bool usePhysicsSimulation, int simulationStepsPerFrame, int solverIterations, float gravityScale, float velocityDamping, float slackLength, bool collideWithWorld, float collisionRadius, LayerMask collisionMask, QueryTriggerInteraction triggerInteraction)
```

#### Parameters

`startPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`endPoint` [LineConnectionPoint](TriageTrainer.Entity.LineConnection.LineConnectionPoint.md)

`lineRenderer` LineRenderer

`segments` int

`sagAmount` float

`elasticity` float

`usePhysicsSimulation` bool

`simulationStepsPerFrame` int

`solverIterations` int

`gravityScale` float

`velocityDamping` float

`slackLength` float

`collideWithWorld` bool

`collisionRadius` float

`collisionMask` LayerMask

`triggerInteraction` QueryTriggerInteraction

