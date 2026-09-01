# <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelRoleAllocator"></a> Class ScenarioParallelRoleAllocator

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

서버 권위 병렬 실행에서 사용하는 결정적 역할 배정기.

후보 목록은 호출자가 서버의 세션/태그 상태로 계산해 전달한다. 이 타입은 Unity·FishNet·Registry에
의존하지 않으므로, 서버와 테스트가 같은 배정 규칙을 사용한다.

```csharp
public static class ScenarioParallelRoleAllocator
```

#### Inheritance

object ← 
[ScenarioParallelRoleAllocator](MultiplayerInfrastructure.Scenario.ScenarioParallelRoleAllocator.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelRoleAllocator_TryAllocateAllToPlayer_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch__System_Collections_Generic_IReadOnlyDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Collections_Generic_IReadOnlyList_System_Int32___System_Int32_System_Collections_Generic_IDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Nullable_System_Int32___"></a> TryAllocateAllToPlayer\(IReadOnlyList<ScenarioParallelBranch\>, IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int\>\>, int, IDictionary<ScenarioParallelBranch, int?\>\)

모든 브랜치에 적격인 단일 플레이어를 각 브랜치에 중복 배정한다.

```csharp
public static bool TryAllocateAllToPlayer(IReadOnlyList<ScenarioParallelBranch> branches, IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int>> candidatesByBranch, int clientId, IDictionary<ScenarioParallelBranch, int?> allocation)
```

#### Parameters

`branches` IReadOnlyList<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md)\>

`candidatesByBranch` IReadOnlyDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), IReadOnlyList<int\>\>

`clientId` int

`allocation` IDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), int?\>

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelRoleAllocator_TryAllocateAllowingDuplicates_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch__System_Collections_Generic_IReadOnlyDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Collections_Generic_IReadOnlyList_System_Int32___System_Collections_Generic_IDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Nullable_System_Int32___"></a> TryAllocateAllowingDuplicates\(IReadOnlyList<ScenarioParallelBranch\>, IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int\>\>, IDictionary<ScenarioParallelBranch, int?\>\)

가능한 한 서로 다른 플레이어에게 역할을 배정한 뒤, 남은 브랜치는 적격 플레이어에게
중복 배정합니다. 중복 배정된 브랜치는 호출자가 플레이어별로 순차 실행해야 합니다.

```csharp
public static bool TryAllocateAllowingDuplicates(IReadOnlyList<ScenarioParallelBranch> branches, IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int>> candidatesByBranch, IDictionary<ScenarioParallelBranch, int?> allocation)
```

#### Parameters

`branches` IReadOnlyList<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md)\>

`candidatesByBranch` IReadOnlyDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), IReadOnlyList<int\>\>

`allocation` IDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), int?\>

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelRoleAllocator_TryAllocateDistinct_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch__System_Collections_Generic_IReadOnlyDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Collections_Generic_IReadOnlyList_System_Int32___System_Collections_Generic_IDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Nullable_System_Int32___"></a> TryAllocateDistinct\(IReadOnlyList<ScenarioParallelBranch\>, IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int\>\>, IDictionary<ScenarioParallelBranch, int?\>\)

각 브랜치에 서로 다른 적격 플레이어를 하나씩 배정한다. 후보가 적은 브랜치부터 처리하고,
동률은 원래 브랜치 순서를 보존해 모든 피어에서 재현 가능한 결과를 만든다.

```csharp
public static bool TryAllocateDistinct(IReadOnlyList<ScenarioParallelBranch> branches, IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int>> candidatesByBranch, IDictionary<ScenarioParallelBranch, int?> allocation)
```

#### Parameters

`branches` IReadOnlyList<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md)\>

`candidatesByBranch` IReadOnlyDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), IReadOnlyList<int\>\>

`allocation` IDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), int?\>

#### Returns

 bool

