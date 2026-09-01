# <a id="TriageTrainer_Scenario_Rubric_RubricRecorder"></a> Class RubricRecorder

Namespace: [TriageTrainer.Scenario.Rubric](TriageTrainer.Scenario.Rubric.md)  
Assembly: Assembly\-CSharp.dll  

평가 루브릭 수행/미수행 기록 코어(G-3 첫 증분).

<p>
<xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 의 이벤트를 구독하여, 시나리오 진행 중 각 루브릭 항목의
수행/미수행을 자동 판정·기록한다. UI(관찰자 모드)·영속화는 본 증분 범위 밖이며,
본 컴포넌트는 <xref href="TriageTrainer.Scenario.Rubric.RubricResultStore" data-throw-if-not-resolved="false"></xref> 에 결과를 누적하고 CSV 내보내기까지 제공한다.
</p>

<p>판정 규칙</p>
<ul><li>수행(Performed): 매핑된 Validator 게이트 노드를 통과(다음 노드 진입)하면 기록.</li><li>미수행(NotPerformed): 게이트가 <xref href="MultiplayerInfrastructure.Scenario.ScenarioController.OnValidatorWaitTimeout" data-throw-if-not-resolved="false"></xref> 로
  타임아웃(ForceAdvance/FailBranch)되면 기록(G-6 연계).</li></ul>

자동 신호가 없는 항목(pass_*, 신체 사정 등)은 본 증분에서 자동 판정하지 않으며
관찰자 수동 체크(<xref href="TriageTrainer.Scenario.Rubric.RubricRecorder.MarkManual(System.String%2cSystem.String%2cTriageTrainer.Scenario.Rubric.RubricStatus%2cSystem.String)" data-throw-if-not-resolved="false"></xref>) 진입점만 제공한다.

```csharp
public sealed class RubricRecorder : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[RubricRecorder](TriageTrainer.Scenario.Rubric.RubricRecorder.md)

## Properties

### <a id="TriageTrainer_Scenario_Rubric_RubricRecorder_Store"></a> Store

현재 세션의 결과 저장소. 세션 시작 전에는 null.

```csharp
public RubricResultStore Store { get; }
```

#### Property Value

 [RubricResultStore](TriageTrainer.Scenario.Rubric.RubricResultStore.md)

## Methods

### <a id="TriageTrainer_Scenario_Rubric_RubricRecorder_MarkManual_System_String_System_String_TriageTrainer_Scenario_Rubric_RubricStatus_System_String_"></a> MarkManual\(string, string, RubricStatus, string\)

관찰자 수동 체크 등 외부에서 항목 상태를 직접 표기한다.

```csharp
public void MarkManual(string itemId, string playerId, RubricStatus status, string note = "manual")
```

#### Parameters

`itemId` string

`playerId` string

`status` [RubricStatus](TriageTrainer.Scenario.Rubric.RubricStatus.md)

`note` string

### <a id="TriageTrainer_Scenario_Rubric_RubricRecorder_RecordRetry_System_String_System_String_"></a> RecordRetry\(string, string\)

사정 퀴즈 오답/재응시 횟수 누적(외부에서 호출).

```csharp
public void RecordRetry(string itemId, string playerId)
```

#### Parameters

`itemId` string

`playerId` string

