# <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode"></a> Class ScenarioPlayTTSNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

TTSService를 통해 지정된 identifier의 음성을 재생합니다.
Variables에 지정된 값으로 동적 세그먼트를 오버라이드합니다.

```csharp
public sealed class ScenarioPlayTTSNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioPlayTTSNode](MultiplayerInfrastructure.Scenario.ScenarioPlayTTSNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_TranscriptIdentifier"></a> TranscriptIdentifier

TTSService에 등록된 스크립트 식별자

```csharp
public string TranscriptIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_TtsVoiceIdentifier"></a> TtsVoiceIdentifier

사용할 목소리 프로파일 식별자.
TTSService의 Voice Profiles 목록에 등록된 식별자를 지정한다.
null 또는 빈 문자열이면 TTSService의 기본 목소리를 사용한다.

```csharp
public string TtsVoiceIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_TtsVoiceProfile"></a> TtsVoiceProfile

사전 스타일 또는 시나리오 JSON 프로필을 직접 지정하는 노드별 TTS 프로필.

```csharp
public ScenarioTTSVoiceProfile TtsVoiceProfile { get; set; }
```

#### Property Value

 [ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_Variables"></a> Variables

동적 세그먼트 변수 오버라이드 (key: 변수명, value: 실제 값)

```csharp
public Dictionary<string, string> Variables { get; set; }
```

#### Property Value

 Dictionary<string, string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayTTSNode_WaitUntilFinished"></a> WaitUntilFinished

true이면 음성 재생이 끝날 때까지 다음 노드로 진행하지 않습니다.

```csharp
public bool WaitUntilFinished { get; set; }
```

#### Property Value

 bool

