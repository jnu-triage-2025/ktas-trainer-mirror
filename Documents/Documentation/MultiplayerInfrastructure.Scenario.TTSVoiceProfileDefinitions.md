# <a id="MultiplayerInfrastructure_Scenario_TTSVoiceProfileDefinitions"></a> Class TTSVoiceProfileDefinitions

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

배포된 모든 voice_styles 파일의 사전 준비된 정의입니다. enum은 이 테이블의 키이며,
TTS 처리 코드는 스타일 이름을 추측하지 않고 여기의 완성된 리터럴을 사용해야 합니다.

```csharp
public static class TTSVoiceProfileDefinitions
```

#### Inheritance

object ← 
[TTSVoiceProfileDefinitions](MultiplayerInfrastructure.Scenario.TTSVoiceProfileDefinitions.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_TTSVoiceProfileDefinitions_All"></a> All

```csharp
public static IReadOnlyList<TTSVoiceProfileDefinition> All { get; }
```

#### Property Value

 IReadOnlyList<[TTSVoiceProfileDefinition](MultiplayerInfrastructure.Scenario.TTSVoiceProfileDefinition.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Scenario_TTSVoiceProfileDefinitions_Get_MultiplayerInfrastructure_Scenario_TTSVoiceStyle_"></a> Get\(TTSVoiceStyle\)

```csharp
public static TTSVoiceProfile Get(TTSVoiceStyle style)
```

#### Parameters

`style` [TTSVoiceStyle](MultiplayerInfrastructure.Scenario.TTSVoiceStyle.md)

#### Returns

 TTSVoiceProfile

