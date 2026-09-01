# <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode"></a> Class ScenarioChatPrintNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 진행 중 임의의 텍스트를 인게임 채팅창 및/또는 Unity 콘솔에 출력하는 노드.
시그널/이벤트 발생을 눈으로 확인하는 디버깅·데모 용도로 사용한다.

```csharp
public sealed class ScenarioChatPrintNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioChatPrintNode](MultiplayerInfrastructure.Scenario.ScenarioChatPrintNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode_Broadcast"></a> Broadcast

true 이면 서버가 전체 클라이언트에게 브로드캐스트한다(InGameChat 대상에 한함).
false(기본)이면 각 피어가 자기 로컬 채팅창/콘솔에만 출력한다.

```csharp
public bool Broadcast { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode_Message"></a> Message

출력할 메시지 본문.

```csharp
public string Message { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChatPrintNode_Targets"></a> Targets

출력 대상(콘솔/인게임 채팅, 플래그 조합 가능). 기본값은 인게임 채팅.

```csharp
public ScenarioChatPrintTarget Targets { get; set; }
```

#### Property Value

 [ScenarioChatPrintTarget](MultiplayerInfrastructure.Scenario.ScenarioChatPrintTarget.md)

