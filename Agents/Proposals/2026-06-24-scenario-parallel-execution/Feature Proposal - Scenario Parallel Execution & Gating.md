### 개요

"시나리오 병렬 실행·게이팅 보강(Scenario Parallel Execution & Gating)" 기능은 재난 시뮬레이션 시나리오(환자 A/B/C)의 핵심인 **다인 동시 협력 처치**와 **인터랙션 완료 게이팅**을 시나리오 데이터만으로 충실히 실행하기 위한 엔진 보강이다. 이 기능은 (1) 역할 태그 기반 병렬 분배(ByRole) 모드, (2) 병렬 브랜치의 완료 조건(completionCondition) 실제 대기, (3) Validator의 인터랙션 신호 폴링 게이팅, (4) Sound 노드 실제 재생을 통해, 현재 "단일 플레이어로 붕괴되거나, 대기 없이 통과되거나, 스텁으로 남은" 흐름을 의도대로 동작시킨다. `Assets/Modules/MultiplayerInfrastructure`의 `ScenarioController`/`ScenarioParallelNode`/`ScenarioValidatorNode`를 대상으로 하며, 환자 A/B/C 시나리오 JSON(`Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.json`, `patient_b_c_ct.json`)을 실제 플레이 가능 상태로 끌어올리는 것이 의도되었다.

이 제안은 [기존 "ScenarioNode Expressiveness" 제안](../Feature%20Proposal%20-%20ScenarioNode%20Expressiveness/Feature%20Proposal%20-%20ScenarioNode%20Expressiveness.md)(TODO-SPEC-1~4: autoAdvance / interaction-signal validator / playerTag swap / subgraph)이 **다루지 않은 더 근본적인 실행 계층의 차단 요소**를 다룬다. 즉 기존 제안은 "표현력(데이터로 무엇을 쓸 수 있는가)"을, 본 제안은 "실행기(엔진이 그 데이터를 실제로 어떻게 돌리는가)"를 보강한다. 두 제안은 상호 보완적이다.

주요 기술적 제약:
- 본 변경 대상은 `Assets/Modules/MultiplayerInfrastructure`로, 재사용성이 매우 높은 핵심 모듈이다. 루트 `AGENTS.md` 정책에 따라 **본 제안서 + 예시 구현**으로 선행하며, 모든 신규 능력은 **선택적(opt-in)·하위호환**으로 설계한다.
- 현재 `ScenarioController`는 단일 전역 상태기(`_currentNode`, `_state`, `Advance()`)로 동작한다. 병렬 노드(`ExecuteParallelNode`)는 브랜치마다 코루틴을 띄우지만 모두 동일한 전역 `_currentNode`를 변경하므로, 브랜치별 독립 실행 컨텍스트가 없다. 따라서 본 제안의 병렬 보강은 이 단일 상태기 제약 안에서 결정적(deterministic)으로 동작하도록 설계한다.

---

### 해결하려는 문제 상황

나는 시뮬레이션 시나리오 설계자/검증자로서, 변환된 시나리오 JSON이 "기획서가 의도한 대로" 실제로 플레이되기를 원한다. 왜냐하면 데이터는 충실히 변환되었으나(노드·참조 모두 정상), 다음 세 가지가 엔진 미구현으로 인해 실제로는 동작하지 않기 때문이다.

분석에서 식별한 차단 요소(G-1 ~ G-7):

- **G-1 (치명적)** — 역할 기반 병렬 분배 부재. 기획의 핵심은 "간호사 B=활력, C=GCS, D=석션을 *동시에*"이다. JSON은 `allocationType: ByRole`을 의도했으나 `ScenarioParallelAllocationType`에는 `ByRole`이 없어 변환기가 `SelfAll`로 매핑했다. `SelfAll`은 모든 브랜치를 단일 플레이어에게 할당(`ScenarioController.cs:1372`)하여 4인 협력이 1인에게 붕괴된다. `requiredPlayerTags`(triage_lead/airway_team 등)는 보존돼 있으나 분배에 쓰이지 않는다.
- **G-2 (치명적)** — 병렬 브랜치 완료 조건 미구현. `ExecuteBranch`(`ScenarioController.cs:1311`)에 `// TODO: completionCondition 체크 로직` + `yield return null; // 임시`만 있어, 브랜치가 노드 1개만 실행하고 완료를 기다리지 않는다. `CC_*` 완료 식별자가 무시되어 "모두 끝나면 합류(WaitAll)"가 성립하지 않는다.
- **G-3 (치명적)** — Validator 일회성 평가. 변환된 Validator는 `RegistryContains`+`RuntimeState`+`sig.*`로 잘 만들어졌고 엔진도 평가하지만(`:1676`), `ExecuteValidatorNode`(`:1185`)가 한 번만 평가하고 `onFailure: Ignore`면 대기 없이 통과(`:1216`)한다. 인터랙션 게이팅이 사실상 무력화된다.
- **G-4 (치명적)** — `sig.*` 신호 발신 코드 부재. G-3이 고쳐져도 인터랙션 완료 시 `ScenarioInteractionSignals.Raise(...)`를 호출하는 게임플레이 측 구현이 있어야 진행된다. 환자 A 50개·B/C 32개 신호가 미발신 상태(flags `gameplaySignalsToRaise`).
- **G-5 (중간)** — Sound 노드 미구현. `ExecuteSoundNode`(`:448`)가 스텁(`WaitForSeconds(1f)`)이라 사운드가 재생되지 않는다.
- **G-6 (중간)** — Choice를 Quiz 대신 사용. 정/오답·시도횟수 등 평가 데이터 수집 불가(루브릭 채점 연동 곤란).
- **G-7 (중간)** — Dialogue `Duration` 손실. 스키마 미지원으로 드롭됨. **단, 엔진에는 `ScenarioDialogueNode.AutoAdvanceSeconds`(`autoAdvanceSeconds`)가 이미 구현되어 있다**(`ScenarioController.cs:404`). 즉 엔진 보강이 아니라 **변환기/JSON이 이 필드를 쓰도록 재변환**하면 해소된다.

### 사용자 경험 목표

- 플레이어: 4인이 각자 역할(태그)에 맞는 처치를 **동시에** 수행하고, 모두 끝나야 다음 단계로 합류한다.
- 플레이어: 실제 인터랙션(아이템 클릭/적용/연결, 구역 진입, 신체부위 클릭)을 **완료해야만** 다음으로 진행된다(대기 없이 스킵되지 않는다).
- 플레이어: 삽관 테이프 등 사운드가 실제 길이만큼 재생된다.
- 설계자/검증자: 환자 A/B/C가 기획서의 ABCDE → CPR → ROSC → CT 전 구간을 실제 게이트와 함께 플레이된다.

### 제안

7개 항목을 우선순위별로 제안한다. 모두 선택적·하위호환이며, 미사용 시 기존 동작과 동일하다.

#### 1) G-1 — 역할 기반 병렬 분배 `ByRole` (1차 구현 대상)

- `ScenarioParallelAllocationType`에 `ByRole` 추가.
- 의미: 각 브랜치를 **서로 다른** 플레이어에게 1:1로 분배하되, 각 브랜치의 `requiredPlayerTags`/`forbiddenPlayerTags`/`RequiredPlayerTagsMatchMode`(`IsPlayerEligibleForBranch` 재사용)를 만족하는 플레이어에게 할당한다.
- 결정적 매칭: 브랜치를 "자격 후보 수가 적은 것"부터 처리하여(constraint-first) 한 플레이어가 한 브랜치에만 배정되도록 그리디 1:1 매칭. 동일 우선순위는 브랜치 정의 순서.
- 미매칭 처리: `WhenBranchingPlayerNotMatched`(`Panic`/`Ignore`/`Reallocation`) 기존 정책 재사용. `Ignore` 시 해당 브랜치는 `null` 할당(스킵), `Reallocation` 시 라운드로빈 폴백.
- 근거 위치: `TryAllocateParallel`(`ScenarioController.cs:1358`), `IsPlayerEligibleForBranch`(`:1507`).
- 대안 검토: `SpreadOrdinary`도 브랜치별 자격 매칭을 하지만 인덱스 기반(`i % eligible`)이라 중복 배정·비1:1이 발생. 협력 처치는 "한 명이 한 역할"이 핵심이므로 1:1 보장 모드가 필요.

#### 2) G-2 — 병렬 브랜치 완료 조건 실제 대기 (1차 구현 대상)

- `ExecuteBranch`가 브랜치 시작 노드부터 **`NextIdentifier` 체인을 끝까지 따라 실행**하고, `completionConditionIdentifier`가 지정된 경우 그 신호(RuntimeState 레지스트리, `ScenarioInteractionSignals` 기준)가 올라올 때까지 `WaitUntil`로 대기한 뒤 종료하도록 보강.
- 단일 상태기 제약 대응: 브랜치별 독립 미니 실행 컨텍스트를 도입(브랜치 로컬 `currentNode`)하여 전역 `_currentNode`를 덮어쓰지 않고 브랜치 체인을 진행. 브랜치는 자신의 종료 노드(터미널 또는 완료신호 도달) 시 코루틴 종료 → `ExecuteParallelNode`의 `WaitMode`(All/Any/None) 합류 로직이 정상 작동.
- `completionConditionIdentifier`가 비어 있으면(없으면) 기존처럼 체인 종료 즉시 완료로 간주(하위호환).
- 근거 위치: `ExecuteBranch`(`:1283`), `ExecuteParallelNode`(`:1224`).
- 범위 주의: 본 단계는 "브랜치가 자신의 노드 체인을 실행하고 완료 신호를 기다린다"까지. 브랜치 내부에서 또 다른 Parallel 중첩은 본 제안 범위 밖(차후).

#### 3) G-3 — Validator 신호 폴링 게이팅 (1차 구현 대상)

- `ScenarioValidatorNode`에 `WaitForCondition : bool`(JSON `waitForCondition`, 기본 `false`) 추가.
- `false`(기존): 1회 평가 후 `onFailure` 정책대로(하위호환).
- `true`: `ExecuteValidatorNode`를 코루틴에서 `WaitUntil(() => EvaluateValidator(node))`로 대기 후 통과. 즉 인터랙션 신호가 올라올 때까지 진행을 막는 게이트가 된다.
- 근거 위치: `ExecuteValidatorNode`(`:1185`), `EvaluateValidator`(`:1592`).
- 대안 검토: `onFailure`를 `Branching`으로 자기 자신 재시도시키는 방법은 노드 폭증·UI 깜빡임. 명시적 `waitForCondition` 플래그가 단순·명확.

#### 4) G-4 — 게임플레이 `sig.*` 신호 발신 (TODO)

- TriageTrainer 측 인터랙션/이벤트 핸들러에서 완료 시 `ScenarioInteractionSignals.Raise("<cond>")` 호출 추가. 대상 신호 목록은 두 flags 파일의 `gameplaySignalsToRaise`(A 50 / B·C 32).
- 본 제안 1차 범위에서는 **검증용 디버그 훅**(예: 에디터 단축키/치트 커맨드로 신호 Raise)만 제공하여 G-1~G-3 통합 테스트를 가능케 하고, 실제 인터랙션 배선은 후속 TODO로 둔다.

#### 5) G-5 — Sound 노드 실제 재생 (TODO)

- `ExecuteSoundNode`(`:448`)에서 `Resources.Load<AudioClip>` 또는 등록된 사운드 레지스트리로 실제 클립 재생, `WaitUntilFinished` 시 클립 길이만큼 대기. AudioSource 참조는 기존 `_ttsAudioSource` 패턴 참고.

#### 6) G-6 — 퀴즈/평가 채점 연동 (TODO)

- 정/오답 데이터가 필요한 문항(AVPU/GCS/압박깊이/산소량/에피 간격 등)을 `Choice` → `Quiz` 노드(`correctIndex`/`feedback`/`onIncorrectNextIdentifier`)로 재변환하거나, Choice 선택 결과를 루브릭 채점 스토어로 집계하는 훅 추가. 관찰자/평가자 모드 루브릭(수행/미수행) 매핑 설계 포함.

#### 7) G-7 — Dialogue 자동 진행 시간 복원 (TODO, 엔진 변경 불필요)

- 엔진의 `AutoAdvanceSeconds`가 이미 구현되어 있으므로, 변환기/JSON이 원본 `Duration`을 `autoAdvanceSeconds`로 매핑하도록 재변환. 특히 환자 B/C JSON에는 자동 진행 시간이 누락되어 있어 우선 적용.

### 자세한 달성 목표

- 환자 A/B/C 병렬 처치 구간에서 4인이 각자 다른 브랜치를 동시에 수행한다(ByRole).
- 각 브랜치가 완료 신호 전까지 대기하고, WaitAll 시 모두 완료돼야 합류한다.
- 인터랙션 게이트(Validator `waitForCondition`)가 실제로 진행을 막는다.
- 모든 신규 능력은 기존 시나리오(`disaster_intro`/`validating_full`/샘플)를 깨지 않는다(미지정 시 동작 동일).

### 구현 단계 (TODO 트래킹)

| ID | 항목 | 우선순위 | 엔진변경 | 본 PR 구현 |
|---|---|---|---|---|
| G-1 | `ByRole` 병렬 분배 | 치명 | O | ✅ 구현 |
| G-2 | 브랜치 완료조건 대기 | 치명 | O | ✅ 구현 |
| G-3 | Validator `waitForCondition` 폴링 | 치명 | O | ✅ 구현 |
| G-4 | 게임플레이 `sig.*` 발신 | 치명 | △(TriageTrainer) | ⬜ TODO (디버그 훅만) |
| G-5 | Sound 실제 재생 | 중간 | O | ⬜ TODO |
| G-6 | Quiz/루브릭 채점 연동 | 중간 | △ | ⬜ TODO |
| G-7 | Dialogue `autoAdvanceSeconds` 재변환 | 중간 | X(변환기) | ⬜ TODO |

### 문서화

- `scenario-graph-spec.md`에 신규 속성 추가(`allocationType: ByRole`, Validator `waitForCondition`).
- `scenario.schema.json`에 대응 스키마 추가(전부 optional).
- `json-conversion-rules.md`에 "ByRole 분배", "completionCondition 신호", "waitForCondition 게이트", "Duration→autoAdvanceSeconds" 규칙 추가.
- 후처리(문서 색인 갱신 + `validate-documentation-links.sh`) 수행.

### 가용성과 테스트

- 모든 신규 능력은 opt-in이며 미지정 시 기존 동작과 동일(회귀 위험 최소화).
- 단위 테스트: `ByRole` 1:1 매칭(자격 제약/미스매치 정책), 브랜치 완료 신호 대기, Validator 폴링.
- 통합 테스트: 환자 A P003(B/C/D 동시) 분배·합류, 인터랙션 신호 Raise 후 게이트 통과.
- 교차 런타임: 서버 권한 실행 경로(`ChatService.TryDispatchScenario`)에서 owner/tag/branch 동작 확인.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표:
  - 환자 A/B/C 병렬 노드가 `ByRole`로 4(2)인에게 분배되어 동시에 진행된다.
  - 완료 신호 미발생 시 브랜치/Validator가 대기(스킵되지 않음), 발생 시 통과·합류한다.
  - 기존 그래프 회귀 0건.
- 수용 기준:
  - `/scenario execute @s patient_a_critical` 실행 시 ABCDE → CPR → ROSC 구간이 실제 게이트와 함께 진행된다.
  - 디버그 훅으로 `sig.*`를 올리면 해당 단계가 진행된다.

### 링크, 참고사항

- 상호 보완 제안: [`Feature Proposal - ScenarioNode Expressiveness`](../Feature%20Proposal%20-%20ScenarioNode%20Expressiveness/Feature%20Proposal%20-%20ScenarioNode%20Expressiveness.md)
- 변환 산출물: `Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.json`, `patient_b_c_ct.json`, `*.unsupported.flags.json`
- 엔진 근거: `ScenarioController.cs`(`TryAllocateParallel:1358`, `ExecuteBranch:1283`, `ExecuteValidatorNode:1185`, `ExecuteSoundNode:448`), `ScenarioParallelAllocationType.cs`, `ScenarioInteractionSignals.cs`
