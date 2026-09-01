# <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore"></a> Class ScenarioSignalParameterStore

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 신호의 마지막 JSON 파라미터를 신호·발신 플레이어별로 보관한다.
서버가 권위 원본을 기록하고, 클라이언트는 네트워크 중계기로 받은 미러만 갱신한다.

```csharp
public static class ScenarioSignalParameterStore
```

#### Inheritance

object ← 
[ScenarioSignalParameterStore](MultiplayerInfrastructure.Scenario.ScenarioSignalParameterStore.md)

## Fields

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_MaxStoredEntries"></a> MaxStoredEntries

```csharp
public const int MaxStoredEntries = 512
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_MaxStoredEntriesPerPlayer"></a> MaxStoredEntriesPerPlayer

```csharp
public const int MaxStoredEntriesPerPlayer = 128
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_ServerPlayerIdentifier"></a> ServerPlayerIdentifier

```csharp
public const string ServerPlayerIdentifier = "server"
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_GetAll_System_String_"></a> GetAll\(string\)

```csharp
public static IReadOnlyList<ScenarioSignalParameter> GetAll(string signalIdentifier = null)
```

#### Parameters

`signalIdentifier` string

#### Returns

 IReadOnlyList<[ScenarioSignalParameter](MultiplayerInfrastructure.Scenario.ScenarioSignalParameter.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_TryGetForPlayer_System_String_System_String_MultiplayerInfrastructure_Scenario_ScenarioSignalParameter__"></a> TryGetForPlayer\(string, string, out ScenarioSignalParameter\)

```csharp
public static bool TryGetForPlayer(string signalIdentifier, string playerIdentifier, out ScenarioSignalParameter value)
```

#### Parameters

`signalIdentifier` string

`playerIdentifier` string

`value` [ScenarioSignalParameter](MultiplayerInfrastructure.Scenario.ScenarioSignalParameter.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_TryGetLatest_System_String_MultiplayerInfrastructure_Scenario_ScenarioSignalParameter__"></a> TryGetLatest\(string, out ScenarioSignalParameter\)

```csharp
public static bool TryGetLatest(string signalIdentifier, out ScenarioSignalParameter value)
```

#### Parameters

`signalIdentifier` string

`value` [ScenarioSignalParameter](MultiplayerInfrastructure.Scenario.ScenarioSignalParameter.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_TryValidateJson_System_String_System_String__"></a> TryValidateJson\(string, out string\)

```csharp
public static bool TryValidateJson(string parameterJson, out string error)
```

#### Parameters

`parameterJson` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_OnFlushed"></a> OnFlushed

```csharp
public static event Action OnFlushed
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameterStore_OnValueRecorded"></a> OnValueRecorded

```csharp
public static event Action<ScenarioSignalParameter> OnValueRecorded
```

#### Event Type

 Action<[ScenarioSignalParameter](MultiplayerInfrastructure.Scenario.ScenarioSignalParameter.md)\>

