# <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService"></a> Class ScenarioTTSService

Namespace: [MultiplayerInfrastructure.TTS](MultiplayerInfrastructure.TTS.md)  
Assembly: Assembly\-CSharp.dll  

MultiplayerInfrastructure 측 TTS 재생 서비스.

TextToSpeechService 모듈(<xref href="TextToSpeechService.TTSService" data-throw-if-not-resolved="false"></xref>)에 의존하여
시나리오 흐름에서의 음성 재생을 담당한다. TextToSpeechService 모듈은 순수 합성/파일 처리만
책임지고, 게임 통합(컴포넌트 배치·AudioSource 관리·시나리오 연동)은 이 서비스가 담당한다.

배치:
  · 이 컴포넌트를 GameObject에 붙이면 <xref href="TextToSpeechService.TTSService" data-throw-if-not-resolved="false"></xref> 와
    <xref href="MultiplayerInfrastructure.TTS.ScenarioTTSService.AudioSource" data-throw-if-not-resolved="false"></xref> 가 자동으로 보장(없으면 추가)된다.
  · <xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 의
    _ttsService / _ttsAudioSource 필드에 각각 이 컴포넌트와 <xref href="MultiplayerInfrastructure.TTS.ScenarioTTSService.AudioSource" data-throw-if-not-resolved="false"></xref> 를 연결한다.

```csharp
[RequireComponent(typeof(TTSService))]
[RequireComponent(typeof(AudioSource))]
public sealed class ScenarioTTSService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ScenarioTTSService](MultiplayerInfrastructure.TTS.ScenarioTTSService.md)

## Properties

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_AudioSource"></a> AudioSource

재생에 사용하는 AudioSource (외부 연결용). null이면 지연 확보한다.

```csharp
public AudioSource AudioSource { get; }
```

#### Property Value

 AudioSource

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_IsDynamicCacheDirty"></a> IsDynamicCacheDirty

동적 세그먼트 백그라운드 캐싱 진행 중 여부.

```csharp
public bool IsDynamicCacheDirty { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_IsInitializationFailed"></a> IsInitializationFailed

내부 TTS 엔진 초기화 실패 여부. true면 IsReady를 기다려도 참이 되지 않는다.

```csharp
public bool IsInitializationFailed { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_IsReady"></a> IsReady

내부 TTS 엔진 초기화 완료 여부.

```csharp
public bool IsReady { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_ConfigureScenarioVoiceProfiles_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile__"></a> ConfigureScenarioVoiceProfiles\(IReadOnlyList<ScenarioTTSVoiceProfile\>\)

현재 시나리오 범위의 JSON 프로필을 TTS 엔진에 등록한다.

```csharp
public void ConfigureScenarioVoiceProfiles(IReadOnlyList<ScenarioTTSVoiceProfile> profiles)
```

#### Parameters

`profiles` IReadOnlyList<[ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)\>

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_PlayText_System_String_UnityEngine_AudioSource_System_String_System_String_System_String_"></a> PlayText\(string, AudioSource, string, string, string\)

시나리오 그래프의 인라인 텍스트(Dialogue/Choice/Quiz)를 재생한다.
baked WAV가 있으면 우선 재생하고 없으면 즉석 합성한다.

```csharp
public Coroutine PlayText(string text, AudioSource audioSource = null, string scenarioIdentifier = null, string nodeIdentifier = null, string voiceIdentifier = null)
```

#### Parameters

`text` string

`audioSource` AudioSource

`scenarioIdentifier` string

`nodeIdentifier` string

`voiceIdentifier` string

사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.

#### Returns

 Coroutine

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_PlayTranscript_System_String_UnityEngine_AudioSource_System_Collections_Generic_Dictionary_System_String_System_String__System_String_"></a> PlayTranscript\(string, AudioSource, Dictionary<string, string\>, string\)

transcripts.json에 등록된 identifier의 음성을 재생한다.

```csharp
public Coroutine PlayTranscript(string identifier, AudioSource audioSource = null, Dictionary<string, string> overrideVariables = null, string voiceIdentifier = null)
```

#### Parameters

`identifier` string

`audioSource` AudioSource

`overrideVariables` Dictionary<string, string\>

`voiceIdentifier` string

사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.

#### Returns

 Coroutine

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_PrepareInlineText_System_String_System_String_System_String_System_String_System_Threading_CancellationToken_"></a> PrepareInlineText\(string, string, string, string, CancellationToken\)

재생 전 필요한 인라인 대사만 런타임 캐시에 미리 합성한다.

```csharp
public Coroutine PrepareInlineText(string text, string scenarioIdentifier, string nodeIdentifier, string voiceIdentifier = null, CancellationToken cancellationToken = default)
```

#### Parameters

`text` string

`scenarioIdentifier` string

`nodeIdentifier` string

`voiceIdentifier` string

`cancellationToken` CancellationToken

#### Returns

 Coroutine

### <a id="MultiplayerInfrastructure_TTS_ScenarioTTSService_PrepareTranscriptVariables_System_String_System_Collections_Generic_Dictionary_System_String_System_String__System_Action_System_String_"></a> PrepareTranscriptVariables\(string, Dictionary<string, string\>, Action, string\)

지정된 identifier의 동적 세그먼트를 미리 합성해 캐시한다.

```csharp
public Coroutine PrepareTranscriptVariables(string identifier, Dictionary<string, string> variables, Action onDone = null, string voiceIdentifier = null)
```

#### Parameters

`identifier` string

`variables` Dictionary<string, string\>

`onDone` Action

`voiceIdentifier` string

사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.

#### Returns

 Coroutine

