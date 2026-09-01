# <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode"></a> Class ScenarioSignalCounterNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

접두사(prefix)로 시작하는 서로 다른(distinct) 시나리오 신호가 몇 개나 올라왔는지 세어,
임계치에 도달하면 출력 신호를 발신하는 카운터를 제어하는 노드.

<p>
시나리오 신호는 sticky(존재 여부만, 최초 1회만 <code>OnSignalRegistered</code> 발생)이므로,
"같은 신호가 N번" 을 셀 수는 없다. 대신 <xref href="MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.SourceSignalPrefix" data-throw-if-not-resolved="false"></xref> 로 시작하는
<b>서로 다른 신호 식별자</b>의 개수를 센다. 예:
</p>
<ul><li>트리아지 구역 도착 3명: prefix <code>enter_triage_zone_</code> 로
  <code>enter_triage_zone_patient_b</code> / <code>_patient_c</code> / <code>_patient_dummy_d_b</code> 3개를 세어 threshold=3.</li><li>환자 A 18G 2개: prefix <code>insert_iv_patient_a_</code> 로
  <code>insert_iv_patient_a_left</code> / <code>_right</code> 2개를 세어 threshold=2.</li></ul>

<p>
등록 시점에 이미 올라와 있는(정규화 후 prefix 매칭) 신호도 초기 카운트에 포함한다.
임계치 도달 시 <xref href="MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.OutputSignalIdentifier" data-throw-if-not-resolved="false"></xref> 를 <code>Raise</code> 하고, 카운터는 자동 해제된다(1회성).
시나리오 종료 시 모든 카운터가 정리된다. 동일 <xref href="MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.CounterIdentifier" data-throw-if-not-resolved="false"></xref> 재등록은 교체된다.
</p>

```csharp
public sealed class ScenarioSignalCounterNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioSignalCounterNode](MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_CounterIdentifier"></a> CounterIdentifier

카운터 식별자(등록/해제 매칭용, 필수). 동일 식별자 재등록은 교체된다.

```csharp
public string CounterIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_Operation"></a> Operation

```csharp
public ScenarioSignalCounterOperation Operation { get; set; }
```

#### Property Value

 [ScenarioSignalCounterOperation](MultiplayerInfrastructure.Scenario.ScenarioSignalCounterOperation.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_OutputSignalIdentifier"></a> OutputSignalIdentifier

임계치 도달 시 발신할 신호 식별자.

```csharp
public string OutputSignalIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_SourceSignalPrefix"></a> SourceSignalPrefix

셀 대상 신호의 접두사. 정규화 후 이 접두사로 시작하는 서로 다른 신호 식별자 수를 센다.
(예: "enter_triage_zone_" → sig.enter_triage_zone_* 를 카운트)

```csharp
public string SourceSignalPrefix { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_Threshold"></a> Threshold

임계치. 서로 다른 매칭 신호 수가 이 값 이상이면 출력 신호를 발신한다(1 이상).

```csharp
public int Threshold { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounterNode_UseActiveRoleRosterThreshold"></a> UseActiveRoleRosterThreshold

Uses the graph's current active-role roster instead of the static threshold.

```csharp
public bool UseActiveRoleRosterThreshold { get; set; }
```

#### Property Value

 bool

