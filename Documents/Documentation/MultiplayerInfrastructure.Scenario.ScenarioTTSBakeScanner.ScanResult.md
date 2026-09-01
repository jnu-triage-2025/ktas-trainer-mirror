# <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult"></a> Class ScenarioTTSBakeScanner.ScanResult

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

스캔 결과 요약.

```csharp
public sealed class ScenarioTTSBakeScanner.ScanResult
```

#### Inheritance

object ← 
[ScenarioTTSBakeScanner.ScanResult](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.ScanResult.md)

## Fields

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_DirtyCount"></a> DirtyCount

텍스트 변경으로 stale 파일이 남은 dirty 작업 수.

```csharp
public int DirtyCount
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_Jobs"></a> Jobs

```csharp
public readonly List<ScenarioTTSBakeScanner.InlineTTSJob> Jobs
```

#### Field Value

 List<[ScenarioTTSBakeScanner](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.md).[InlineTTSJob](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.InlineTTSJob.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_MissingCount"></a> MissingCount

아직 bake되지 않은(=현재 해시 파일 없음) 작업 수.

```csharp
public int MissingCount
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_OrphanedBakedPaths"></a> OrphanedBakedPaths

현재 어떤 시나리오 노드에서도 사용하지 않는(=orphan) baked WAV 절대 경로 목록.
노드/시나리오 삭제, PlayTTS 해제, 텍스트 변경 등으로 더 이상 참조되지 않는 파일들이다.
(dirty 작업의 stale 파일도 여기에 포함된다.)

```csharp
public readonly List<string> OrphanedBakedPaths
```

#### Field Value

 List<string\>

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_HasOrphans"></a> HasOrphans

정리할 사용하지 않는 baked 파일이 있으면 true.

```csharp
public bool HasOrphans { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_NeedsBake"></a> NeedsBake

bake가 필요한(missing 또는 dirty) 작업이 있으면 true.

```csharp
public bool NeedsBake { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanResult_OrphanCount"></a> OrphanCount

사용하지 않는 baked 파일 수.

```csharp
public int OrphanCount { get; }
```

#### Property Value

 int

