# <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition"></a> Class ScenarioActingNpcDefinition

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오가 시작 또는 그래프 진행 중 preset으로 만들고 종료 시 선택적으로 정리할 Acting NPC 정의.
외형과 네트워크 프리팹은 preset catalog가 담당하고, 인스턴스별 데이터는 이 정의가 담당한다.

```csharp
public sealed class ScenarioActingNpcDefinition
```

#### Inheritance

object ← 
[ScenarioActingNpcDefinition](MultiplayerInfrastructure.Scenario.ScenarioActingNpcDefinition.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_ActingNpcType"></a> ActingNpcType

```csharp
public ScenarioActingNpcType ActingNpcType { get; set; }
```

#### Property Value

 [ScenarioActingNpcType](MultiplayerInfrastructure.Scenario.ScenarioActingNpcType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_DespawnOnScenarioEnd"></a> DespawnOnScenarioEnd

```csharp
public bool DespawnOnScenarioEnd { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_DisplayName"></a> DisplayName

```csharp
public string DisplayName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_Interactions"></a> Interactions

```csharp
public IReadOnlyList<ScenarioActingNpcInteractionDefinition> Interactions { get; set; }
```

#### Property Value

 IReadOnlyList<[ScenarioActingNpcInteractionDefinition](MultiplayerInfrastructure.Scenario.ScenarioActingNpcInteractionDefinition.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_PositionX"></a> PositionX

```csharp
public float PositionX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_PositionY"></a> PositionY

```csharp
public float PositionY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_PositionZ"></a> PositionZ

```csharp
public float PositionZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_PresetIdentifier"></a> PresetIdentifier

```csharp
public string PresetIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_RotationX"></a> RotationX

```csharp
public float RotationX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_RotationY"></a> RotationY

```csharp
public float RotationY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_RotationZ"></a> RotationZ

```csharp
public float RotationZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_ShowOverheadName"></a> ShowOverheadName

켜면 DisplayName을 NPC 머리 위 월드 공간 이름표로 표시한다.

```csharp
public bool ShowOverheadName { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_SpawnOnStart"></a> SpawnOnStart

```csharp
public bool SpawnOnStart { get; set; }
```

#### Property Value

 bool

