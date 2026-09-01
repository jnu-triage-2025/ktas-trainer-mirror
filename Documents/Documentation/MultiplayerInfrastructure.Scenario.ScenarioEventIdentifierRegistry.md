# <a id="MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry"></a> Class ScenarioEventIdentifierRegistry

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

Maps scenario event identifiers to handlers that can be invoked by scenario nodes.
Handlers may return a coroutine to allow asynchronous execution; returning null is treated as an immediate completion.

```csharp
public static class ScenarioEventIdentifierRegistry
```

#### Inheritance

object ← 
[ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_Clear"></a> Clear\(\)

```csharp
public static void Clear()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_Register_System_String_MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_ScenarioEventHandler_"></a> Register\(string, ScenarioEventHandler\)

```csharp
public static void Register(string identifier, ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
```

#### Parameters

`identifier` string

`handler` [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md).[ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_TryGetHandler_System_String_MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_ScenarioEventHandler__"></a> TryGetHandler\(string, out ScenarioEventHandler\)

```csharp
public static bool TryGetHandler(string identifier, out ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
```

#### Parameters

`identifier` string

`handler` [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md).[ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_Unregister_System_String_"></a> Unregister\(string\)

```csharp
public static bool Unregister(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_Unregister_System_String_MultiplayerInfrastructure_Scenario_ScenarioEventIdentifierRegistry_ScenarioEventHandler_"></a> Unregister\(string, ScenarioEventHandler\)

```csharp
public static bool Unregister(string identifier, ScenarioEventIdentifierRegistry.ScenarioEventHandler expectedHandler)
```

#### Parameters

`identifier` string

`expectedHandler` [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md).[ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)

#### Returns

 bool

