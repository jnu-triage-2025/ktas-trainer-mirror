# <a id="MultiplayerInfrastructure_Scenario_ScenarioNodeVisitTiming"></a> Struct ScenarioNodeVisitTiming

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

단일 노드 방문의 진입 및 이탈 시각.

```csharp
public readonly struct ScenarioNodeVisitTiming
```

## Constructors

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNodeVisitTiming__ctor_System_Int32_System_DateTime_System_Nullable_System_DateTime__"></a> ScenarioNodeVisitTiming\(int, DateTime, DateTime?\)

```csharp
public ScenarioNodeVisitTiming(int sequence, DateTime enteredAt, DateTime? exitedAt)
```

#### Parameters

`sequence` int

`enteredAt` DateTime

`exitedAt` DateTime?

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNodeVisitTiming_EnteredAt"></a> EnteredAt

```csharp
public DateTime EnteredAt { get; }
```

#### Property Value

 DateTime

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNodeVisitTiming_ExitedAt"></a> ExitedAt

```csharp
public DateTime? ExitedAt { get; }
```

#### Property Value

 DateTime?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNodeVisitTiming_Sequence"></a> Sequence

```csharp
public int Sequence { get; }
```

#### Property Value

 int

