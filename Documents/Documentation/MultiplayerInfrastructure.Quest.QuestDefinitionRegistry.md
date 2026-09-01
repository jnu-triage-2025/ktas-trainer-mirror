# <a id="MultiplayerInfrastructure_Quest_QuestDefinitionRegistry"></a> Class QuestDefinitionRegistry

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class QuestDefinitionRegistry : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[QuestDefinitionRegistry](MultiplayerInfrastructure.Quest.QuestDefinitionRegistry.md)

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestDefinitionRegistry_EnsureIncludesLoaded_System_Collections_Generic_IReadOnlyList_System_String__"></a> EnsureIncludesLoaded\(IReadOnlyList<string\>\)

```csharp
public static void EnsureIncludesLoaded(IReadOnlyList<string> includes)
```

#### Parameters

`includes` IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Quest_QuestDefinitionRegistry_InvalidateResourceCache"></a> InvalidateResourceCache\(\)

Editor authoring tools call this after changing a Resources/Quest asset.

```csharp
public static void InvalidateResourceCache()
```

### <a id="MultiplayerInfrastructure_Quest_QuestDefinitionRegistry_TryGet_System_String_MultiplayerInfrastructure_Quest_QuestDefinition__"></a> TryGet\(string, out QuestDefinition\)

```csharp
public bool TryGet(string identifier, out QuestDefinition definition)
```

#### Parameters

`identifier` string

`definition` [QuestDefinition](MultiplayerInfrastructure.Quest.QuestDefinition.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestDefinitionRegistry_TryGetGlobal_System_String_MultiplayerInfrastructure_Quest_QuestDefinition__"></a> TryGetGlobal\(string, out QuestDefinition\)

```csharp
public static bool TryGetGlobal(string identifier, out QuestDefinition definition)
```

#### Parameters

`identifier` string

`definition` [QuestDefinition](MultiplayerInfrastructure.Quest.QuestDefinition.md)

#### Returns

 bool

