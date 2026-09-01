# <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode"></a> Class ScenarioManualEntrypointNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 흐름의 특정 지점에 별칭을 붙이는 표식 노드.

<p>평상시에는 아무 일도 하지 않고 곧바로 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.NextIdentifier" data-throw-if-not-resolved="false"></xref> 로 넘어간다.
운영자가 <code>/scenario enter &lt;identifier&gt;</code> 명령을 실행하면 재생 위치가 이 노드로
건너뛴다.</p>

<p>명령으로 진입한 경우에 한해 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.ManualEnterSetupIdentifier" data-throw-if-not-resolved="false"></xref> 체인을 먼저
실행한다. 건너뛴 구간에서 만들어졌어야 할 인게임 상황(엔티티 스폰, 퀘스트 발행, 신호 등)을
여기서 맞춰 놓는 용도다. 체인이 끝나면 이 노드로 돌아와 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.NextIdentifier" data-throw-if-not-resolved="false"></xref> 로
진행한다.</p>

```csharp
public sealed class ScenarioManualEntrypointNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioManualEntrypointNode](MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_Description"></a> Description

작성자용 메모. 실행에는 쓰이지 않고 명령 목록에 함께 표시된다.

```csharp
public string Description { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_EntrypointIdentifier"></a> EntrypointIdentifier

명령에서 이 지점을 가리킬 별칭. 비어 있으면 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.Identifier" data-throw-if-not-resolved="false"></xref> 를 그대로 쓴다.

```csharp
public string EntrypointIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_ManualEnterSetupIdentifier"></a> ManualEnterSetupIdentifier

명령으로 진입할 때만 실행할 준비 체인의 시작 노드 식별자.
체인은 다음 중 하나에 닿으면 끝나고 제어가 이 노드로 돌아온다.
(1) NextIdentifier 가 비어 있는 노드, (2) 이 노드의 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.Identifier" data-throw-if-not-resolved="false"></xref>,
(3) 이 노드의 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.NextIdentifier" data-throw-if-not-resolved="false"></xref>.

<p><code>clear-state=true</code>(기본값)로 진입하면 시나리오 상태 저장소가 통째로 비워진다.
이 저장소는 StateUpdate 값뿐 아니라 EntityPresetSpawn·EntityInit·ItemSubmissionConfig 가
남긴 <code>resultStateKey → 엔티티 식별자</code> 해석 표도 겸한다. 월드에 엔티티가 살아 있어도
표가 비면 <code>targetEntityStateKey</code> 로 대상을 찾는 노드들이 전부 대상을 놓치므로,
준비 체인에서 필요한 키를 다시 채워 넣어야 한다.</p>

```csharp
public string ManualEnterSetupIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode_ResolvedEntrypointIdentifier"></a> ResolvedEntrypointIdentifier

명령에서 이 지점을 가리키는 데 쓰는 실제 별칭.

```csharp
public string ResolvedEntrypointIdentifier { get; }
```

#### Property Value

 string

