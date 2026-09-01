# <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition"></a> Class RubricItemDefinition

Namespace: [TriageTrainer.Scenario.Rubric](TriageTrainer.Scenario.Rubric.md)  
Assembly: Assembly\-CSharp.dll  

루브릭 항목 1개의 정의. 시나리오 게이트(Validator 노드/신호)나 사정 퀴즈(Choice)와 연결된다.
자동 판정이 불가능한 항목(예: 의사 전달 pass_*, 신체 사정)은 매핑 필드를 비워 두고
관찰자 수동 체크 대상으로 둔다.

```csharp
[Serializable]
public sealed class RubricItemDefinition
```

#### Inheritance

object ← 
[RubricItemDefinition](TriageTrainer.Scenario.Rubric.RubricItemDefinition.md)

## Properties

### <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition_Area"></a> Area

ABCDE 영역.

```csharp
public RubricArea Area { get; set; }
```

#### Property Value

 [RubricArea](TriageTrainer.Scenario.Rubric.RubricArea.md)

### <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition_AutoSignal"></a> AutoSignal

자동 수행 판정용 인터랙션 신호 조건명(접두사 sig. 제외, 예: "apply_gauze"). 비어 있으면
신호 기반 자동 판정을 하지 않는다.

```csharp
public string AutoSignal { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition_GateNodeIdentifier"></a> GateNodeIdentifier

자동 수행 판정용 시나리오 노드 식별자(Validator 게이트의 Identifier). 비어 있으면
노드 기반 자동 판정을 하지 않는다(수동 또는 신호 기반).

```csharp
public string GateNodeIdentifier { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition_Id"></a> Id

항목 고유 ID(예: "rubric.circulation.stop_bleeding").

```csharp
public string Id { get; set; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition_PerPlayer"></a> PerPlayer

플레이어별로 개별 기록할 항목인지(true) 팀 단위 1건인지(false).

```csharp
public bool PerPlayer { get; set; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Scenario_Rubric_RubricItemDefinition_Title"></a> Title

표시 제목(예: "지혈(거즈 압박)").

```csharp
public string Title { get; set; }
```

#### Property Value

 string

