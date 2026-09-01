# <a id="MultiplayerInfrastructure_Scenario_ScenarioGraphLoader"></a> Class ScenarioGraphLoader

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class ScenarioGraphLoader
```

#### Inheritance

object ← 
[ScenarioGraphLoader](MultiplayerInfrastructure.Scenario.ScenarioGraphLoader.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraphLoader_LoadFromJson_System_String_System_Boolean_"></a> LoadFromJson\(string, bool\)

```csharp
public static ScenarioGraph LoadFromJson(string json, bool validateWithSchema = true)
```

#### Parameters

`json` string

`validateWithSchema` bool

#### Returns

 [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraphLoader_ReloadSchemaForEditor"></a> ReloadSchemaForEditor\(\)

에디터에서 스키마 TextAsset이 변경되거나 브랜치 전환으로 교체된 뒤에도
이전 정적 캐시를 사용하지 않도록 다음 검증 전에 스키마를 다시 읽게 한다.

```csharp
public static void ReloadSchemaForEditor()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraphLoader_SaveToJson_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_Boolean_"></a> SaveToJson\(ScenarioGraph, bool\)

```csharp
public static string SaveToJson(ScenarioGraph graph, bool validateWithSchema = true)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`validateWithSchema` bool

#### Returns

 string

