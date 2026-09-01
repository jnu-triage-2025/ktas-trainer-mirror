# <a id="MultiplayerInfrastructure_Scenario_ScenarioExecuteCommandNode"></a> Class ScenarioExecuteCommandNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 진행 중 인게임 채팅 명령어를 서버 권한으로 실행하는 노드.
명령 문자열 자체에 대상 선택자(<code>@s</code>/<code>@a</code>/<code>@n</code>/<code>fish:&lt;id&gt;</code>)와
파이프라인(<code>|</code>)을 포함할 수 있으므로, "특정 플레이어/서버 기준 실행"을
명령 문자열로 표현한다. 시나리오 전용 <code>give-if-missing &lt;item&gt; [count] [target]</code>
명령은 대상별 인벤토리를 확인해 해당 아이템을 보유하지 않은 대상에게만 지급한다.

```csharp
public sealed class ScenarioExecuteCommandNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioExecuteCommandNode](MultiplayerInfrastructure.Scenario.ScenarioExecuteCommandNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioExecuteCommandNode_CommandLine"></a> CommandLine

실행할 명령 문자열. 선행 슬래시(<code>/</code>)는 없어도 되며, 대상 선택자와 파이프라인을
포함할 수 있다. 서버(또는 오프라인) 컨텍스트에서 시스템 권한으로 실행된다.

```csharp
public string CommandLine { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioExecuteCommandNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioExecuteCommandNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioExecuteCommandNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

