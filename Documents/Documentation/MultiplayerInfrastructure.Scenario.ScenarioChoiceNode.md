# <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode"></a> Class ScenarioChoiceNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioChoiceNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioChoiceNode](MultiplayerInfrastructure.Scenario.ScenarioChoiceNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_AssessmentIdentifier"></a> AssessmentIdentifier

교육 평가 로그에 사용할 안정적인 항목 식별자. 비어 있으면 일반 선택으로 취급한다.

```csharp
public string AssessmentIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_CorrectOptionIndex"></a> CorrectOptionIndex

의도된 정답 옵션 인덱스. null이면 정답 여부를 기록하지 않는다.

```csharp
public int? CorrectOptionIndex { get; set; }
```

#### Property Value

 int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_DialogueContent"></a> DialogueContent

```csharp
public string DialogueContent { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_DialogueContentTTSPassing"></a> DialogueContentTTSPassing

```csharp
public string DialogueContentTTSPassing { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_Options"></a> Options

```csharp
public List<ScenarioChoiceOption> Options { get; set; }
```

#### Property Value

 List<[ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_PlayTTS"></a> PlayTTS

true이면 <xref href="MultiplayerInfrastructure.Scenario.ScenarioChoiceNode.DialogueContent" data-throw-if-not-resolved="false"></xref> 를 표시할 때 TTS로 함께 재생한다.
변수를 포함하지 않는 콘텐츠는 에디터에서 사전 합성(bake)될 수 있으며,
bake되지 않은 경우 런타임에 즉석으로 합성해 재생한다.

```csharp
public bool PlayTTS { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_PortraitSpriteIdentifier"></a> PortraitSpriteIdentifier

```csharp
public string PortraitSpriteIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_SpeakerName"></a> SpeakerName

```csharp
public string SpeakerName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_TtsVoiceIdentifier"></a> TtsVoiceIdentifier

사용할 목소리 프로파일 식별자.
TTSService의 Voice Profiles 목록에 등록된 식별자를 지정한다.
null 또는 빈 문자열이면 TTSService의 기본 목소리를 사용한다.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioChoiceNode.PlayTTS" data-throw-if-not-resolved="false"></xref>가 true일 때만 효과가 있다.

```csharp
public string TtsVoiceIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioChoiceNode_TtsVoiceProfile"></a> TtsVoiceProfile

```csharp
public ScenarioTTSVoiceProfile TtsVoiceProfile { get; set; }
```

#### Property Value

 [ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)

