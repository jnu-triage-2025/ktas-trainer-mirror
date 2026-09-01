# <a id="TriageTrainer_Scenario_Rubric_RubricResultStore"></a> Class RubricResultStore

Namespace: [TriageTrainer.Scenario.Rubric](TriageTrainer.Scenario.Rubric.md)  
Assembly: Assembly\-CSharp.dll  

세션 단위 루브릭 결과 저장소(메모리). 항목×대상(플레이어/팀) 키로 결과를 누적한다.
직렬화/내보내기는 외부(파일/화면)에서 <xref href="TriageTrainer.Scenario.Rubric.RubricResultStore.ExportCsv" data-throw-if-not-resolved="false"></xref> / <xref href="TriageTrainer.Scenario.Rubric.RubricResultStore.Snapshot" data-throw-if-not-resolved="false"></xref> 로 수행한다.

```csharp
public sealed class RubricResultStore
```

#### Inheritance

object ← 
[RubricResultStore](TriageTrainer.Scenario.Rubric.RubricResultStore.md)

## Constructors

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore__ctor_System_String_"></a> RubricResultStore\(string\)

```csharp
public RubricResultStore(string sessionId)
```

#### Parameters

`sessionId` string

## Properties

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_SessionId"></a> SessionId

```csharp
public string SessionId { get; }
```

#### Property Value

 string

## Methods

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_Clear"></a> Clear\(\)

```csharp
public void Clear()
```

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_ExportCsv"></a> ExportCsv\(\)

디브리핑용 CSV 내보내기. 컬럼: sessionId, playerId, itemId, status, retries, updatedAtUtc, note.

```csharp
public string ExportCsv()
```

#### Returns

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_IncrementRetry_System_String_System_String_"></a> IncrementRetry\(string, string\)

사정 퀴즈 오답/재응시 횟수를 누적한다.

```csharp
public void IncrementRetry(string itemId, string playerId)
```

#### Parameters

`itemId` string

`playerId` string

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_Record_System_String_System_String_TriageTrainer_Scenario_Rubric_RubricStatus_System_String_"></a> Record\(string, string, RubricStatus, string\)

항목 상태를 기록/갱신한다. Performed 로 이미 확정된 항목은 NotPerformed 로 되돌리지 않는다
(수행이 우선). Pending → 어떤 상태로든 갱신 가능.

```csharp
public RubricResult Record(string itemId, string playerId, RubricStatus status, string note = null)
```

#### Parameters

`itemId` string

`playerId` string

`status` [RubricStatus](TriageTrainer.Scenario.Rubric.RubricStatus.md)

`note` string

#### Returns

 [RubricResult](TriageTrainer.Scenario.Rubric.RubricResult.md)

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_Snapshot"></a> Snapshot\(\)

```csharp
public IReadOnlyCollection<RubricResult> Snapshot()
```

#### Returns

 IReadOnlyCollection<[RubricResult](TriageTrainer.Scenario.Rubric.RubricResult.md)\>

### <a id="TriageTrainer_Scenario_Rubric_RubricResultStore_TryGet_System_String_System_String_TriageTrainer_Scenario_Rubric_RubricResult__"></a> TryGet\(string, string, out RubricResult\)

```csharp
public bool TryGet(string itemId, string playerId, out RubricResult result)
```

#### Parameters

`itemId` string

`playerId` string

`result` [RubricResult](TriageTrainer.Scenario.Rubric.RubricResult.md)

#### Returns

 bool

