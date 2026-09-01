# <a id="MultiplayerInfrastructure_Scenario_ScenarioConditionalSignalListeners"></a> Class ScenarioConditionalSignalListeners

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

게임플레이가 올린 신호를 관찰해, 선언된 전제 신호가 모두 있을 때만 후속 신호를 발생시킨다.

```csharp
public static class ScenarioConditionalSignalListeners
```

#### Inheritance

object ← 
[ScenarioConditionalSignalListeners](MultiplayerInfrastructure.Scenario.ScenarioConditionalSignalListeners.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioConditionalSignalListeners_ClearAll"></a> ClearAll\(\)

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioConditionalSignalListeners_Register_System_String_System_String_System_String_System_Collections_Generic_IEnumerable_System_String__System_Boolean_"></a> Register\(string, string, string, IEnumerable<string\>, bool\)

```csharp
public static void Register(string identifier, string source, string output, IEnumerable<string> required, bool consumeOnce)
```

#### Parameters

`identifier` string

`source` string

`output` string

`required` IEnumerable<string\>

`consumeOnce` bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioConditionalSignalListeners_Unregister_System_String_"></a> Unregister\(string\)

```csharp
public static bool Unregister(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

