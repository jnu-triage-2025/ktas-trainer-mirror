# <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization"></a> Class ScenarioClientSignalAuthorization

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

Client-origin generic signal RPC의 서버측 capability 집합. 그래프가 소비하는 입력만 허용하고,
그래프가 생성하는 출력은 동일 식별자가 validator에 있어도 client 입력에서 제외한다.

```csharp
public sealed class ScenarioClientSignalAuthorization
```

#### Inheritance

object ← 
[ScenarioClientSignalAuthorization](MultiplayerInfrastructure.Scenario.ScenarioClientSignalAuthorization.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_CanClear_System_Int32_System_String_System_String__"></a> CanClear\(int, string, out string\)

```csharp
public bool CanClear(int clientId, string normalizedSignalId, out string error)
```

#### Parameters

`clientId` int

`normalizedSignalId` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_CanRaise_System_Int32_System_String_System_String_System_String__"></a> CanRaise\(int, string, string, out string\)

```csharp
public bool CanRaise(int clientId, string playerIdentifier, string normalizedSignalId, out string error)
```

#### Parameters

`clientId` int

`playerIdentifier` string

`normalizedSignalId` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_ClearAll"></a> ClearAll\(\)

```csharp
public void ClearAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_ClearScenario"></a> ClearScenario\(\)

```csharp
public void ClearScenario()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_ConfigureScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_"></a> ConfigureScenario\(ScenarioGraph\)

```csharp
public void ConfigureScenario(ScenarioGraph graph)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_Grant_System_Int32_System_String_System_Boolean_System_Boolean_"></a> Grant\(int, string, bool, bool\)

```csharp
public void Grant(int clientId, string normalizedSignalId, bool allowClear = false, bool isPrefix = false)
```

#### Parameters

`clientId` int

`normalizedSignalId` string

`allowClear` bool

`isPrefix` bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioClientSignalAuthorization_Revoke_System_Int32_"></a> Revoke\(int\)

```csharp
public void Revoke(int clientId)
```

#### Parameters

`clientId` int

