# <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile"></a> Class ScenarioTTSVoiceProfile

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

노드와 시나리오 정의 탭에서 공유하는 TTS 프로필입니다. Preset을 지정하면 내장된
voice_styles 값이 사용되며, null이면 아래 JSON 필드로 사용자 프로필을 정의합니다.

```csharp
[Serializable]
public sealed class ScenarioTTSVoiceProfile
```

#### Inheritance

object ← 
[ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_Language"></a> Language

```csharp
public string Language { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_Preset"></a> Preset

```csharp
public TTSVoiceStyle? Preset { get; set; }
```

#### Property Value

 [TTSVoiceStyle](MultiplayerInfrastructure.Scenario.TTSVoiceStyle.md)?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_Speed"></a> Speed

```csharp
public float Speed { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_TotalStep"></a> TotalStep

```csharp
public int TotalStep { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_VoiceIdentifier"></a> VoiceIdentifier

```csharp
public string VoiceIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_VoiceStyleName"></a> VoiceStyleName

```csharp
public string VoiceStyleName { get; set; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSVoiceProfile_ToServiceProfile"></a> ToServiceProfile\(\)

```csharp
public TTSVoiceProfile ToServiceProfile()
```

#### Returns

 TTSVoiceProfile

