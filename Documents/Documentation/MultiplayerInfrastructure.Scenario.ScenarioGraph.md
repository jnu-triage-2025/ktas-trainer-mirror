# <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph"></a> Class ScenarioGraph

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioGraph
```

#### Inheritance

object ← 
[ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_ActingNpcs"></a> ActingNpcs

시나리오 시작/종료 수명주기에 종속되는 NPC 등 actingNpc 정의.

```csharp
public IReadOnlyList<ScenarioActingNpcDefinition> ActingNpcs { get; set; }
```

#### Property Value

 IReadOnlyList<[ScenarioActingNpcDefinition](MultiplayerInfrastructure.Scenario.ScenarioActingNpcDefinition.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_ActiveRoleTags"></a> ActiveRoleTags

Connected player roles which form this graph's authoritative active roster.

```csharp
public IReadOnlyList<string> ActiveRoleTags { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_ChecklistItemSetsByPlayerTag"></a> ChecklistItemSetsByPlayerTag

플레이어 태그별 체크리스트 아이템 묶음입니다. 한 플레이어가 여러 태그를 보유하면
해당 태그의 묶음을 모두 합쳐 표시합니다.

```csharp
public IReadOnlyDictionary<string, IReadOnlyList<ScenarioChecklistItemRequirement>> ChecklistItemSetsByPlayerTag { get; set; }
```

#### Property Value

 IReadOnlyDictionary<string, IReadOnlyList<[ScenarioChecklistItemRequirement](MultiplayerInfrastructure.Scenario.ScenarioChecklistItemRequirement.md)\>\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_ClientSignalIdentifiers"></a> ClientSignalIdentifiers

Exact generic signals that clients may report for this graph.

```csharp
public IReadOnlyList<string> ClientSignalIdentifiers { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_ClientSignalPrefixes"></a> ClientSignalPrefixes

Generic client signal prefixes. Use only for bounded, gameplay-owned namespaces.

```csharp
public IReadOnlyList<string> ClientSignalPrefixes { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_DefaultEntrypoint"></a> DefaultEntrypoint

startNodeIdentifier 없이 시나리오를 시작할 때 사용할 기본 진입 노드 식별자.

```csharp
public string DefaultEntrypoint { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_Nodes"></a> Nodes

```csharp
public Dictionary<string, IScenarioNode> Nodes { get; }
```

#### Property Value

 Dictionary<string, [IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_QuestDefinitionIncludes"></a> QuestDefinitionIncludes

이 시나리오에서 사용할 퀘스트 정의 include 목록.
각 항목은 Resources/Quest 하위 .quest.json(TextAsset) 파일명을 가리킨다.

```csharp
public IReadOnlyList<string> QuestDefinitionIncludes { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_SkipAbsentRoleBranches"></a> SkipAbsentRoleBranches

Allows ByRole branches for declared roles absent from the active roster to be skipped.

```csharp
public bool SkipAbsentRoleBranches { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_Tags"></a> Tags

시나리오에서 사용할 태그 사전 선언 목록.
선언되지 않은 태그가 노드/분기에서 사용되면 로딩 시 경고를 출력합니다.

```csharp
public IReadOnlyList<string> Tags { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_TtsVoiceProfiles"></a> TtsVoiceProfiles

이 시나리오에만 적용되는 사용자 정의 TTS 프로필(JSON)입니다.

```csharp
public IReadOnlyList<ScenarioTTSVoiceProfile> TtsVoiceProfiles { get; set; }
```

#### Property Value

 IReadOnlyList<[ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_Waypoints"></a> Waypoints

시나리오 시작 전에 생성·등록할 waypoint anchor 정의.

```csharp
public IReadOnlyList<ScenarioWaypointDefinition> Waypoints { get; set; }
```

#### Property Value

 IReadOnlyList<[ScenarioWaypointDefinition](MultiplayerInfrastructure.Scenario.ScenarioWaypointDefinition.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_Add_MultiplayerInfrastructure_Scenario_IScenarioNode_"></a> Add\(IScenarioNode\)

```csharp
public void Add(IScenarioNode node)
```

#### Parameters

`node` [IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_IsTagDeclared_System_String_"></a> IsTagDeclared\(string\)

```csharp
public bool IsTagDeclared(string tag)
```

#### Parameters

`tag` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGraph_TryGetNode_System_String_MultiplayerInfrastructure_Scenario_IScenarioNode__"></a> TryGetNode\(string, out IScenarioNode\)

```csharp
public bool TryGetNode(string identifier, out IScenarioNode node)
```

#### Parameters

`identifier` string

`node` [IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

#### Returns

 bool

