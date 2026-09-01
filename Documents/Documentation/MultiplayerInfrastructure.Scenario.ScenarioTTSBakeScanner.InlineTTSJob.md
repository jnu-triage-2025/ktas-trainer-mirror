# <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob"></a> Class ScenarioTTSBakeScanner.InlineTTSJob

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

bake 가능한 인라인 TTS 작업 항목.

```csharp
public sealed class ScenarioTTSBakeScanner.InlineTTSJob
```

#### Inheritance

object ← 
[ScenarioTTSBakeScanner.InlineTTSJob](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.InlineTTSJob.md)

## Fields

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_ExpectedBakedPath"></a> ExpectedBakedPath

이 텍스트 내용에 대응하는 baked WAV의 절대 경로 (현재 해시 기준).

```csharp
public string ExpectedBakedPath
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_IsBaked"></a> IsBaked

현재 해시의 baked WAV가 존재하는지 여부.

```csharp
public bool IsBaked
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_IsDirty"></a> IsDirty

같은 노드에 대한 baked WAV가 이전 텍스트 해시로 존재하지만
현재 텍스트와 일치하지 않는 경우 true (=텍스트가 변경된 dirty 상태).

```csharp
public bool IsDirty
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_NodeIdentifier"></a> NodeIdentifier

```csharp
public string NodeIdentifier
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_ScenarioIdentifier"></a> ScenarioIdentifier

```csharp
public string ScenarioIdentifier
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_StaleBakedPaths"></a> StaleBakedPaths

이 노드에 대해 존재하는 stale(구버전) baked 파일 경로 목록.

```csharp
public List<string> StaleBakedPaths
```

#### Field Value

 List<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_Text"></a> Text

```csharp
public string Text
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_InlineTTSJob_VoiceIdentifier"></a> VoiceIdentifier

이 job에 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
baked 경로에 voice 폴더가 포함되는지 결정한다.

```csharp
public string VoiceIdentifier
```

#### Field Value

 string

