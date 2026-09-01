# <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState"></a> Class ScenarioParallelAssignmentState

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

서버가 확정해 TargetRpc로 전달한 병렬 브랜치 배정의 클라이언트측 읽기 모델.

그래프 실행 권위는 보유하지 않는다. 표현/퀘스트/상호작용 어댑터가 "이 클라이언트가
이 parallel node의 어느 branch를 맡았는가"를 읽는 유일한 복제 결과다.

```csharp
public static class ScenarioParallelAssignmentState
```

#### Inheritance

object ← 
[ScenarioParallelAssignmentState](MultiplayerInfrastructure.Scenario.ScenarioParallelAssignmentState.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState_Apply_System_String_System_String_System_Collections_Generic_IEnumerable_System_String__"></a> Apply\(string, string, IEnumerable<string\>\)

```csharp
public static void Apply(string graphIdentifier, string parallelNodeIdentifier, IEnumerable<string> branchIdentifiers)
```

#### Parameters

`graphIdentifier` string

`parallelNodeIdentifier` string

`branchIdentifiers` IEnumerable<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState_ClearAll"></a> ClearAll\(\)

현재 클라이언트의 모든 복제 배정을 지운다. 시나리오 종료/테스트 정리에서 사용한다.

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState_ClearGraph_System_String_"></a> ClearGraph\(string\)

```csharp
public static void ClearGraph(string graphIdentifier)
```

#### Parameters

`graphIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState_IsAssigned_System_String_System_String_System_String_"></a> IsAssigned\(string, string, string\)

```csharp
public static bool IsAssigned(string graphIdentifier, string parallelNodeIdentifier, string branchIdentifier)
```

#### Parameters

`graphIdentifier` string

`parallelNodeIdentifier` string

`branchIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState_TryGetAssignedBranches_System_String_System_String_System_Collections_Generic_IReadOnlyList_System_String___"></a> TryGetAssignedBranches\(string, string, out IReadOnlyList<string\>\)

```csharp
public static bool TryGetAssignedBranches(string graphIdentifier, string parallelNodeIdentifier, out IReadOnlyList<string> branches)
```

#### Parameters

`graphIdentifier` string

`parallelNodeIdentifier` string

`branches` IReadOnlyList<string\>

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelAssignmentState_Changed"></a> Changed

```csharp
public static event Action<string, string, IReadOnlyList<string>> Changed
```

#### Event Type

 Action<string, string, IReadOnlyList<string\>\>

