# <a id="MultiplayerInfrastructure_Variable_SessionVariableService"></a> Class SessionVariableService

Namespace: [MultiplayerInfrastructure.Variable](MultiplayerInfrastructure.Variable.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class SessionVariableService
```

#### Inheritance

object ← 
[SessionVariableService](MultiplayerInfrastructure.Variable.SessionVariableService.md)

## Methods

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_AddObjective_System_String_System_String_System_String__"></a> AddObjective\(string, string, out string\)

```csharp
public static bool AddObjective(string objective, string criteria, out string error)
```

#### Parameters

`objective` string

`criteria` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_AddScore_System_String_System_String_System_Int32_System_String__"></a> AddScore\(string, string, int, out string\)

```csharp
public static bool AddScore(string userIdentifier, string objective, int delta, out string error)
```

#### Parameters

`userIdentifier` string

`objective` string

`delta` int

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_ApplyOperation_System_String_System_String_System_String_System_String_System_String_System_String__"></a> ApplyOperation\(string, string, string, string, string, out string\)

```csharp
public static bool ApplyOperation(string targetIdentifier, string targetObjective, string operation, string sourceIdentifier, string sourceObjective, out string error)
```

#### Parameters

`targetIdentifier` string

`targetObjective` string

`operation` string

`sourceIdentifier` string

`sourceObjective` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_ClearSessionState"></a> ClearSessionState\(\)

```csharp
public static void ClearSessionState()
```

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_ContainsObjective_System_String_"></a> ContainsObjective\(string\)

```csharp
public static bool ContainsObjective(string objective)
```

#### Parameters

`objective` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_GetObjectives"></a> GetObjectives\(\)

```csharp
public static IReadOnlyCollection<SessionVariableService.ObjectiveDefinition> GetObjectives()
```

#### Returns

 IReadOnlyCollection<[SessionVariableService](MultiplayerInfrastructure.Variable.SessionVariableService.md).[ObjectiveDefinition](MultiplayerInfrastructure.Variable.SessionVariableService.ObjectiveDefinition.md)\>

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_GetScoresForUser_System_String_"></a> GetScoresForUser\(string\)

```csharp
public static IReadOnlyDictionary<string, int> GetScoresForUser(string userIdentifier)
```

#### Parameters

`userIdentifier` string

#### Returns

 IReadOnlyDictionary<string, int\>

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_RemoveObjective_System_String_System_String__"></a> RemoveObjective\(string, out string\)

```csharp
public static bool RemoveObjective(string objective, out string error)
```

#### Parameters

`objective` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_RemoveScore_System_String_System_String_System_Int32_System_String__"></a> RemoveScore\(string, string, int, out string\)

```csharp
public static bool RemoveScore(string userIdentifier, string objective, int delta, out string error)
```

#### Parameters

`userIdentifier` string

`objective` string

`delta` int

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_ResetAllScores_System_String_System_String__"></a> ResetAllScores\(string, out string\)

```csharp
public static bool ResetAllScores(string userIdentifier, out string error)
```

#### Parameters

`userIdentifier` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_ResetScore_System_String_System_String_System_String__"></a> ResetScore\(string, string, out string\)

```csharp
public static bool ResetScore(string userIdentifier, string objective, out string error)
```

#### Parameters

`userIdentifier` string

`objective` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_SetScore_System_String_System_String_System_Int32_System_String__"></a> SetScore\(string, string, int, out string\)

```csharp
public static bool SetScore(string userIdentifier, string objective, int value, out string error)
```

#### Parameters

`userIdentifier` string

`objective` string

`value` int

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Variable_SessionVariableService_TryGetScore_System_String_System_String_System_Int32__"></a> TryGetScore\(string, string, out int\)

```csharp
public static bool TryGetScore(string userIdentifier, string objective, out int value)
```

#### Parameters

`userIdentifier` string

`objective` string

`value` int

#### Returns

 bool

