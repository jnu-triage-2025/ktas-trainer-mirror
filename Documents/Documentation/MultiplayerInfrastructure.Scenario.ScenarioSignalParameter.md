# <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter"></a> Struct ScenarioSignalParameter

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

신호 식별자·발신 플레이어별 마지막 JSON 파라미터의 읽기 모델.

```csharp
public readonly struct ScenarioSignalParameter
```

## Constructors

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter__ctor_System_String_System_String_System_String_System_String_System_Int64_System_Int64_"></a> ScenarioSignalParameter\(string, string, string, string, long, long\)

```csharp
public ScenarioSignalParameter(string signalIdentifier, string playerIdentifier, string playerDisplayName, string parameterJson, long occurredAtUtcTicks, long sequence)
```

#### Parameters

`signalIdentifier` string

`playerIdentifier` string

`playerDisplayName` string

`parameterJson` string

`occurredAtUtcTicks` long

`sequence` long

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_DisplayParameter"></a> DisplayParameter

```csharp
public string DisplayParameter { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_HasParameter"></a> HasParameter

```csharp
public bool HasParameter { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_OccurredAtUtcTicks"></a> OccurredAtUtcTicks

```csharp
public long OccurredAtUtcTicks { get; }
```

#### Property Value

 long

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_ParameterJson"></a> ParameterJson

```csharp
public string ParameterJson { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_PlayerDisplayName"></a> PlayerDisplayName

```csharp
public string PlayerDisplayName { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_PlayerIdentifier"></a> PlayerIdentifier

```csharp
public string PlayerIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_Sequence"></a> Sequence

```csharp
public long Sequence { get; }
```

#### Property Value

 long

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalParameter_SignalIdentifier"></a> SignalIdentifier

```csharp
public string SignalIdentifier { get; }
```

#### Property Value

 string

