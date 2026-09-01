# <a id="TriageTrainer_Scenario_Rubric_RubricResult"></a> Class RubricResult

Namespace: [TriageTrainer.Scenario.Rubric](TriageTrainer.Scenario.Rubric.md)  
Assembly: Assembly\-CSharp.dll  

한 항목에 대한 한 대상(플레이어 또는 팀)의 기록 결과.

```csharp
[Serializable]
public sealed class RubricResult
```

#### Inheritance

object ← 
[RubricResult](TriageTrainer.Scenario.Rubric.RubricResult.md)

## Properties

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_ItemId"></a> ItemId

```csharp
public string ItemId { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_Note"></a> Note

판정 근거 메모(예: "gate timeout: ForceAdvance", "signal raised", "manual").

```csharp
public string Note { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_PlayerId"></a> PlayerId

대상 플레이어 식별자. 팀 단위(PerPlayer=false) 항목이면 null.

```csharp
public string PlayerId { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_Retries"></a> Retries

사정 퀴즈 등에서의 오답/재응시 누적 횟수.

```csharp
public int Retries { get; set; }
```

#### Property Value

 int

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_SessionId"></a> SessionId

```csharp
public string SessionId { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_Status"></a> Status

```csharp
public RubricStatus Status { get; set; }
```

#### Property Value

 [RubricStatus](TriageTrainer.Scenario.Rubric.RubricStatus.md)

### <a id="TriageTrainer_Scenario_Rubric_RubricResult_UpdatedAtUtc"></a> UpdatedAtUtc

마지막 갱신 시각(UTC ISO-8601).

```csharp
public string UpdatedAtUtc { get; set; }
```

#### Property Value

 string

