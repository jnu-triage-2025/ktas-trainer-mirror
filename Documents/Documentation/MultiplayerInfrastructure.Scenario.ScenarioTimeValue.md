# <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue"></a> Struct ScenarioTimeValue

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

단위와 값을 함께 보존하는 시나리오 공통 시간 값.

```csharp
[Serializable]
public struct ScenarioTimeValue
```

## Constructors

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue__ctor_System_Double_MultiplayerInfrastructure_Scenario_ScenarioTimeUnit_"></a> ScenarioTimeValue\(double, ScenarioTimeUnit\)

```csharp
public ScenarioTimeValue(double value, ScenarioTimeUnit unit)
```

#### Parameters

`value` double

`unit` [ScenarioTimeUnit](MultiplayerInfrastructure.Scenario.ScenarioTimeUnit.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue_Unit"></a> Unit

```csharp
[JsonPropertyName("unit")]
public ScenarioTimeUnit Unit { get; set; }
```

#### Property Value

 [ScenarioTimeUnit](MultiplayerInfrastructure.Scenario.ScenarioTimeUnit.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue_Value"></a> Value

```csharp
[JsonPropertyName("value")]
public double Value { get; set; }
```

#### Property Value

 double

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue_Seconds_System_Double_"></a> Seconds\(double\)

```csharp
public static ScenarioTimeValue Seconds(double value)
```

#### Parameters

`value` double

#### Returns

 [ScenarioTimeValue](MultiplayerInfrastructure.Scenario.ScenarioTimeValue.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue_ToSeconds"></a> ToSeconds\(\)

```csharp
public double ToSeconds()
```

#### Returns

 double

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeValue_ToTicks"></a> ToTicks\(\)

```csharp
public uint ToTicks()
```

#### Returns

 uint

