# <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode"></a> Class ScenarioValidatorNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioValidatorNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioValidatorNode](MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_FailureNextIdentifier"></a> FailureNextIdentifier

```csharp
public string FailureNextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_FailureReportTargets"></a> FailureReportTargets

```csharp
public ScenarioValidatorFailureReportTarget FailureReportTargets { get; set; }
```

#### Property Value

 [ScenarioValidatorFailureReportTarget](MultiplayerInfrastructure.Scenario.ScenarioValidatorFailureReportTarget.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_OnFailure"></a> OnFailure

```csharp
public ScenarioValidatorOnFailure OnFailure { get; set; }
```

#### Property Value

 [ScenarioValidatorOnFailure](MultiplayerInfrastructure.Scenario.ScenarioValidatorOnFailure.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_OnWaitTimeout"></a> OnWaitTimeout

<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.WaitTimeoutSeconds" data-throw-if-not-resolved="false"></xref> 초과 시 행동 정책. 기본값은 기존 동작과 동일한
<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorWaitTimeoutBehavior.KeepWaiting" data-throw-if-not-resolved="false"></xref>(계속 대기)이다.

```csharp
public ScenarioValidatorWaitTimeoutBehavior OnWaitTimeout { get; set; }
```

#### Property Value

 [ScenarioValidatorWaitTimeoutBehavior](MultiplayerInfrastructure.Scenario.ScenarioValidatorWaitTimeoutBehavior.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_RootConditions"></a> RootConditions

```csharp
public IReadOnlyList<ScenarioValidatorRootCondition> RootConditions { get; set; }
```

#### Property Value

 IReadOnlyList<[ScenarioValidatorRootCondition](MultiplayerInfrastructure.Scenario.ScenarioValidatorRootCondition.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_WaitForCondition"></a> WaitForCondition

true 이면, 조건이 충족될 때까지 진행을 막고 폴링 대기하는 "게이트"로 동작한다.
(인터랙션 완료 신호 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref> 가 올라올 때까지 대기)
false(기본)이면 기존처럼 1회만 평가하고 <xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.OnFailure" data-throw-if-not-resolved="false"></xref> 정책을 따른다(하위호환).

```csharp
public bool WaitForCondition { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorNode_WaitTimeoutSeconds"></a> WaitTimeoutSeconds

<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.WaitForCondition" data-throw-if-not-resolved="false"></xref> 게이트의 선택적 타임아웃(초). null 또는 0 이하이면 타임아웃 없이
무한 대기한다(기존 동작, 하위호환). 양수이면 그 시간 안에 조건이 충족되지 않을 경우
<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.OnWaitTimeout" data-throw-if-not-resolved="false"></xref> 정책이 적용된다.

```csharp
public float? WaitTimeoutSeconds { get; set; }
```

#### Property Value

 float?

