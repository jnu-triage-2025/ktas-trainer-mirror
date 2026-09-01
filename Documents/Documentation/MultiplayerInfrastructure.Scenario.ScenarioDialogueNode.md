# <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode"></a> Class ScenarioDialogueNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioDialogueNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioDialogueNode](MultiplayerInfrastructure.Scenario.ScenarioDialogueNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_AutoAdvanceSeconds"></a> AutoAdvanceSeconds

자동 진행까지 대기할 시간(초). null/0 이하이면 기존처럼 사용자 입력을 기다린다(하위호환).
0 보다 크면 표시 후 해당 시간 경과 시 자동으로 다음 노드로 진행하며,
그 전에 사용자가 진행 입력을 주면 즉시 진행하고 타이머는 취소된다.

```csharp
public float? AutoAdvanceSeconds { get; set; }
```

#### Property Value

 float?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_DialogueContent"></a> DialogueContent

```csharp
public string DialogueContent { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_DialogueContentTTSPassing"></a> DialogueContentTTSPassing

```csharp
public string DialogueContentTTSPassing { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_InteractionRequired"></a> InteractionRequired

```csharp
public bool InteractionRequired { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_PlayTTS"></a> PlayTTS

true이면 <xref href="MultiplayerInfrastructure.Scenario.ScenarioDialogueNode.DialogueContent" data-throw-if-not-resolved="false"></xref> 를 표시할 때 TTS로 함께 재생한다.
변수를 포함하지 않는 콘텐츠는 에디터에서 사전 합성(bake)될 수 있으며,
bake되지 않은 경우 런타임에 즉석으로 합성해 재생한다.

```csharp
public bool PlayTTS { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_PortraitSpriteIdentifier"></a> PortraitSpriteIdentifier

```csharp
public string PortraitSpriteIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_SpeakerName"></a> SpeakerName

```csharp
public string SpeakerName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_TtsVoiceIdentifier"></a> TtsVoiceIdentifier

사용할 목소리 프로파일 식별자.
TTSService의 Voice Profiles 목록에 등록된 식별자를 지정한다.
null 또는 빈 문자열이면 TTSService의 기본 목소리를 사용한다.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioDialogueNode.PlayTTS" data-throw-if-not-resolved="false"></xref>가 true일 때만 효과가 있다.

```csharp
public string TtsVoiceIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDialogueNode_TtsVoiceProfile"></a> TtsVoiceProfile

사전 스타일 또는 시나리오 JSON 프로필을 직접 지정하는 노드별 TTS 프로필.

```csharp
public ScenarioTTSVoiceProfile TtsVoiceProfile { get; set; }
```

#### Property Value

 [ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)

