# <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode"></a> Class ScenarioQuizNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioQuizNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioQuizNode](MultiplayerInfrastructure.Scenario.ScenarioQuizNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_CorrectIndex"></a> CorrectIndex

```csharp
public int CorrectIndex { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_FeedbackCorrect"></a> FeedbackCorrect

```csharp
public string FeedbackCorrect { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_FeedbackCorrectTTSPassing"></a> FeedbackCorrectTTSPassing

```csharp
public string FeedbackCorrectTTSPassing { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_FeedbackIncorrect"></a> FeedbackIncorrect

```csharp
public string FeedbackIncorrect { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_FeedbackIncorrectTTSPassing"></a> FeedbackIncorrectTTSPassing

```csharp
public string FeedbackIncorrectTTSPassing { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_OnCorrectNextIdentifier"></a> OnCorrectNextIdentifier

```csharp
public string OnCorrectNextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_OnIncorrectNextIdentifier"></a> OnIncorrectNextIdentifier

```csharp
public string OnIncorrectNextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_Options"></a> Options

```csharp
public IReadOnlyList<string> Options { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_PlayTTS"></a> PlayTTS

true이면 <xref href="MultiplayerInfrastructure.Scenario.ScenarioQuizNode.Question" data-throw-if-not-resolved="false"></xref> (및 피드백 텍스트)를 표시할 때 TTS로 함께 재생한다.
변수를 포함하지 않는 콘텐츠는 에디터에서 사전 합성(bake)될 수 있으며,
bake되지 않은 경우 런타임에 즉석으로 합성해 재생한다.

```csharp
public bool PlayTTS { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_Question"></a> Question

```csharp
public string Question { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_QuestionTTSPassing"></a> QuestionTTSPassing

```csharp
public string QuestionTTSPassing { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_TtsVoiceIdentifier"></a> TtsVoiceIdentifier

사용할 목소리 프로파일 식별자.
TTSService의 Voice Profiles 목록에 등록된 식별자를 지정한다.
null 또는 빈 문자열이면 TTSService의 기본 목소리를 사용한다.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioQuizNode.PlayTTS" data-throw-if-not-resolved="false"></xref>가 true일 때만 효과가 있다.

```csharp
public string TtsVoiceIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuizNode_TtsVoiceProfile"></a> TtsVoiceProfile

```csharp
public ScenarioTTSVoiceProfile TtsVoiceProfile { get; set; }
```

#### Property Value

 [ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)

