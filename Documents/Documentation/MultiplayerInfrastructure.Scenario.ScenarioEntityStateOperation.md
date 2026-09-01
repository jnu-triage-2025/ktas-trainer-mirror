# <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateOperation"></a> Class ScenarioEntityStateOperation

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode" data-throw-if-not-resolved="false"></xref> 가 대상 엔티티에 적용하는 단일 초기 상태 항목.

<p><xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Kind" data-throw-if-not-resolved="false"></xref> 에 따라 의미가 달라진다.</p>
<ul><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperationKind.StateStore" data-throw-if-not-resolved="false"></xref>:
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Key" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Value" data-throw-if-not-resolved="false"></xref> 를 시나리오 상태 저장소에 기록한다.</li><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperationKind.DisplayState" data-throw-if-not-resolved="false"></xref>:
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Key" data-throw-if-not-resolved="false"></xref> 가 표시/부착 상태 이름(예: 환자의 <code>CervicalCollarOnNeck</code>)이고
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.DisplayActive" data-throw-if-not-resolved="false"></xref> 로 표시(true)/비표시(false)를 정한다.</li></ul>

```csharp
public sealed class ScenarioEntityStateOperation
```

#### Inheritance

object ← 
[ScenarioEntityStateOperation](MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateOperation_DisplayActive"></a> DisplayActive

DisplayState: 표시(true)/비표시(false). 기본값 true.

```csharp
public bool DisplayActive { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateOperation_Key"></a> Key

StateStore: 상태 키. DisplayState: 표시/부착 상태 이름.

```csharp
public string Key { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateOperation_Kind"></a> Kind

```csharp
public ScenarioEntityStateOperationKind Kind { get; set; }
```

#### Property Value

 [ScenarioEntityStateOperationKind](MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperationKind.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateOperation_Value"></a> Value

StateStore: 기록할 값. DisplayState 에서는 사용하지 않는다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.DisplayActive" data-throw-if-not-resolved="false"></xref> 사용).

```csharp
public string Value { get; set; }
```

#### Property Value

 string

