# <a id="TriageTrainer_Scenario_Rubric"></a> Namespace TriageTrainer.Scenario.Rubric

### Classes

 [RubricDefinitionSet](TriageTrainer.Scenario.Rubric.RubricDefinitionSet.md)

루브릭 정의 묶음(데이터팩 JSON 역직렬화 대상).

 [RubricItemDefinition](TriageTrainer.Scenario.Rubric.RubricItemDefinition.md)

루브릭 항목 1개의 정의. 시나리오 게이트(Validator 노드/신호)나 사정 퀴즈(Choice)와 연결된다.
자동 판정이 불가능한 항목(예: 의사 전달 pass_*, 신체 사정)은 매핑 필드를 비워 두고
관찰자 수동 체크 대상으로 둔다.

 [RubricRecorder](TriageTrainer.Scenario.Rubric.RubricRecorder.md)

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

 [RubricResult](TriageTrainer.Scenario.Rubric.RubricResult.md)

한 항목에 대한 한 대상(플레이어 또는 팀)의 기록 결과.

 [RubricResultStore](TriageTrainer.Scenario.Rubric.RubricResultStore.md)

세션 단위 루브릭 결과 저장소(메모리). 항목×대상(플레이어/팀) 키로 결과를 누적한다.
직렬화/내보내기는 외부(파일/화면)에서 <xref href="TriageTrainer.Scenario.Rubric.RubricResultStore.ExportCsv" data-throw-if-not-resolved="false"></xref> / <xref href="TriageTrainer.Scenario.Rubric.RubricResultStore.Snapshot" data-throw-if-not-resolved="false"></xref> 로 수행한다.

### Enums

 [RubricArea](TriageTrainer.Scenario.Rubric.RubricArea.md)

평가 루브릭의 ABCDE 영역. 원본 루브릭의 영역 구분에 대응한다.

 [RubricStatus](TriageTrainer.Scenario.Rubric.RubricStatus.md)

한 루브릭 항목의 수행 판정 상태.

