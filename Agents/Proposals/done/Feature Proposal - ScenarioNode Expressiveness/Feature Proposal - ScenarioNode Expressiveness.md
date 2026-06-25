### 개요

ScenarioNode 표현력 보강 기능은 재난 시뮬레이션 시나리오(환자 A/B/C)를 데이터(ScenarioGraph JSON)만으로 충실히 표현하기 위한 구현이다. 이 기능은 (1) Dialogue 자동 진행 시간, (2) 도메인 인터랙션 완료 검증, (3) 역할 태그 교대(rotation), (4) 서브그래프 재사용이라는 네 가지 신규/확장 능력을 통해, 현재 InvokeEvent 스텁(`todo.validate.*`)과 외부 핸들러에 숨겨져 있는 핵심 학습 흐름을 시나리오 데이터로 끌어올린다. 환자 A/B/C 시나리오 JSON(`Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.json`, `patient_b_c_ct.json`)을 검증 가능하고 재사용 가능한 형태로 완성하기 위한 기반으로 활용하는 것이 의도되었다.

이 제안은 [기존 ScenarioNode 제안](../Feature%20Proposal%20-%20ScenarioNode.md)이 이미 반영한 노드들(Delay/Interaction/CombineItem/Quiz/StateUpdate/PlayerTag 등)을 **재제안하지 않으며**, 그 이후 환자 A/B/C 변환 과정([`patient-a-b-c-conversion-notes.md`](../../../Documents/requirements/content-definitions/scenario/patient-a-b-c-conversion-notes.md))에서 식별된 **잔여 표현력 격차(TODO-SPEC-1~4)**만 다룬다.

- **TODO-SPEC-1**: DialogueNode 자동 진행(`AutoAdvanceSeconds`) 부재
- **TODO-SPEC-2**: Validator가 "도메인 인터랙션 완료"(아이템 클릭/적용/연결, 구역 진입, 신체부위 클릭 등)를 검증할 수단 부재
- **TODO-SPEC-3**: CPR 교대 등 "플레이어 간 역할 태그 교대"를 데이터로 표현할 수단 부재
- **TODO-SPEC-4**: 동일 처치 서브그래프(환자 B/C/더미)를 재사용할 수단 부재

주요 기술적 제약:
- 본 변경 대상은 `Assets/Modules/MultiplayerInfrastructure`로, 재사용성이 매우 높은 핵심 모듈이다. 따라서 루트 `AGENTS.md` 정책에 따라 **직접 수정 대신 본 제안 + 예시 구현**으로 선행하며, 모든 신규 능력은 **선택적(opt-in)·하위호환**으로 설계한다.
- 직렬화는 `System.Text.Json` 기반(DTO + `[JsonPropertyName]` camelCase) + `ScenarioNodeDTOConverter`의 `nodeType` 분기 + `scenario.schema.json` 검증 파이프라인을 따른다.

### 해결하려는 문제 상황

나는 시뮬레이션 시나리오 설계자로서, 학습자가 실제로 수행해야 진행되는 처치 단계와 자동 진행 안내, 역할 교대, 반복 서브그래프를 **시나리오 데이터만으로** 표현하고 싶다. 왜냐하면 현재는 이 모든 것이 `todo.validate.*` 같은 미구현 InvokeEvent 스텁과 외부 C# 핸들러에 숨어 있어, (a) 시나리오를 데이터 수준에서 검증할 수 없고, (b) 환자 B/C처럼 동일한 흐름을 식별자만 바꿔 통째로 복제해야 하며, (c) CPR 교대 같은 임상적으로 중요한 단계가 정적 태그 매칭 한계로 동작하지 않기 때문이다.

구체적으로 환자 A/B/C 변환 결과 다음이 확인되었다.
- `todo.validate.*` 인터랙션 게이트 약 84개(A 51 / B·C 33)가 전부 핸들러 미등록 → 무동작 스킵.
- Parallel 브랜치가 `requiredPlayerTags`로 분기하나 CPR 사이클(P005↔P006)에서 동일 플레이어의 요구 태그가 사이클마다 바뀜 → 정적 태그로는 교대 표현 불가.
- 환자 C 흐름이 환자 B 흐름을 `_patient_c` 접미사로 완전 복제 → 유지보수·검증 부담.
- Dialogue 자동 진행 시간(`Duration`, 202회 사용)이 스펙에 없어 변환 시 전부 드롭됨.

### 사용자 경험 목표

- 설계자: 시나리오 표/JSON에서 "무엇을 해야 다음으로 진행되는지"가 명시적으로 보인다(InvokeEvent 스텁에 숨지 않는다).
- 설계자: 환자 B/C/더미처럼 같은 흐름을 한 번만 정의하고 파라미터(대상 식별자)만 바꿔 재사용한다.
- 설계자: CPR 교대 같은 역할 전환을 한 노드로 선언한다.
- 검증 도구: 누락된 대상/조건/서브그래프 참조를 로드 시점에 자동 감지한다.
- 플레이어: 안내문이 적절한 시간 뒤 자동 진행되며, 실제 처치를 수행해야만 다음 단계로 넘어간다.

### 제안

네 가지 확장을 제안한다. 모두 선택적이며, 기존 시나리오/스키마와 하위호환된다.

#### 1) TODO-SPEC-1 — DialogueNode 자동 진행 (`autoAdvanceSeconds`)

- `ScenarioDialogueNode`에 `AutoAdvanceSeconds : float?`(JSON `autoAdvanceSeconds`, 기본 `null`) 추가.
- `null`/미지정/`<=0`: 기존과 동일하게 사용자 입력 대기 후 진행(완전 하위호환).
- `>0`: `ExecuteDialogueNode`를 코루틴화하여 표시 후 해당 시간 경과 시 `Advance()`. 사용자가 먼저 진행 입력을 주면 즉시 진행(타이머 취소).
- 근거 위치: `ScenarioController.cs:385` `ExecuteDialogueNode`(현재 입력 대기), `ScenarioDialogueNode.cs`.
- 대안 검토: Dialogue 뒤에 `Delay` 노드를 매번 삽입하는 방식은 노드 수 2배 증가 + UI가 입력 대기로 멈춰 자동 진행이 안 되므로 부적합. 따라서 Dialogue 내장 속성이 적절.

#### 2) TODO-SPEC-2 — Validator 도메인 인터랙션 검증 (`Interaction` 룰 + 이벤트 신호)

핵심 격차: `ScenarioValidatorCondition`은 `PlayerCount*`/`RegistryContains`/`PlayerAssignedTag`만 지원(`ScenarioValidatorNode.cs:7-17`), `ScenarioValidatorRuleType`은 `Registry`만(`:33-36`). 학습자가 "활력징후 측정도구 클릭", "거즈 적용", "경동맥 촉지" 같은 도메인 행위를 완료했는지 검증할 수단이 없다.

두 가지 보완 경로를 함께 제안한다(택1 또는 병행).

- (2-A) **`InteractionSignal` 검증 룰 추가**: 신규 `ScenarioValidatorRuleType.InteractionSignal` + `ScenarioRuntimeStateService`(또는 신규 `ScenarioSignalService`)에 누적되는 "완료 신호" 레지스트리. 게임플레이 코드가 인터랙션 완료 시 `Signal(signalId)`를 올리고, Validator는 `RootCondition.Condition = SignalRaised`, `validationRules[].registryType = RuntimeState`, `registryIdentifier = <signalId>`로 검증. `targetCount`로 복수 신호(예: 후두경 블레이드+손잡이) 요구.
- (2-B) **`Interaction` 노드 게이트 정식화(권장 1차)**: 이미 존재하는 `ScenarioInteractionNode`(`actorScope`/`targetIdentifier`/`requiredItemIdentifier`/`interactionType`/`completionConditionIdentifier`)를 게임플레이 인터랙션 완료와 실제로 연결하는 실행기를 보강한다. 즉 변환 시 생성한 `todo.validate.*` InvokeEvent를 점진적으로 `Interaction` 노드로 치환할 수 있게 한다.
- 효과: `todo.validate.*` 스텁 84개를 데이터로 표현 가능. `disaster_intro`도 동일 패턴으로 정리 가능.
- 자세한 신호/룰 정의는 동봉 [`validator-interaction-signal-spec.md`](./validator-interaction-signal-spec.md) 참조.

#### 3) TODO-SPEC-3 — 역할 태그 교대 (`PlayerTag` 확장: 대상 지정 + Swap)

핵심 격차: `ScenarioPlayerTagScope`는 `All`/`Current`만 지원(`ScenarioPlayerTagScope.cs`), `Operation`은 `Add/Remove/Change`(같은 플레이어 내 교체)만 지원. "현재 cpr_team 태그를 가진 플레이어와 airway_team 태그를 가진 플레이어의 태그를 서로 맞바꾼다"를 표현할 수 없다.

- `ScenarioPlayerTagScope`에 `ByTag` 추가: 특정 태그 보유 플레이어 집합을 대상화.
- `ScenarioPlayerTagOperationType`에 `Swap` 추가 + `ScenarioPlayerTagNode`에 `SwapTagA`/`SwapTagB` 필드: `SwapTagA` 보유자와 `SwapTagB` 보유자의 해당 태그를 상호 교환.
- 효과: P005(A=airway, B=cpr…) → P006(A=cpr, B=airway…) 교대를 단일 PlayerTag(Swap) 노드 1~2개로 표현. 정적 태그 매칭 한계 해소.
- 대안 검토: 사이클마다 브랜치 태그를 고정값으로 다르게 쓰는 방식은 "같은 사람이 교대"라는 의미를 표현 못 함. Swap 연산이 임상 의도와 일치.
- 자세한 의미·엣지케이스는 동봉 [`player-tag-rotation-spec.md`](./player-tag-rotation-spec.md) 참조.

#### 4) TODO-SPEC-4 — 서브그래프 재사용 (`Subgraph` 노드)

핵심 격차: `ScenarioNodeType`에 서브그래프 호출/포함 수단 없음(`ScenarioNodeType.cs`). 환자 C 흐름이 환자 B를 통째로 복제.

- 신규 `ScenarioNodeType.Subgraph` + `ScenarioSubgraphNode`: `SubgraphGraphIdentifier`(재사용할 ScenarioGraph id), `ParameterBindings`(예: `{ "patient": "patientC" }`), `NextIdentifier`.
- 실행기: 호출 시 서브그래프를 현재 owner/파라미터 컨텍스트로 실행하고 종료 후 `NextIdentifier`로 복귀. 파라미터는 노드 식별자/이벤트 식별자 치환 토큰(`${patient}`)으로 바인딩.
- 효과: 환자 B/C/더미 처치 흐름을 1회 정의 후 파라미터만 달리해 재사용. 변환 산출물의 중복 대폭 감소.
- 범위 주의: 본 제안에서는 **단순 1회 인라인 호출 + 파라미터 치환**으로 한정(재귀/순환 금지, 로드시 검출). 자세한 바인딩 규칙은 동봉 [`subgraph-reuse-spec.md`](./subgraph-reuse-spec.md) 참조.
- 우선순위: 4개 중 가장 영향 범위가 크므로 **마지막 단계**로 둔다(우선 1·2·3로 환자 A/B/C 플레이 가능화 후 도입).

### 자세한 달성 목표

- `todo.validate.*` 스텁을 데이터(Interaction/Validator)로 표현 가능해진다.
- Dialogue 자동 진행 연출이 데이터로 복원된다(변환 시 드롭한 Duration 의도 회복).
- CPR 교대가 단일 노드로 표현된다.
- 환자 C 흐름을 환자 B 서브그래프 재사용으로 대체할 수 있다.
- 모든 신규 능력은 기존 시나리오를 깨지 않는다(미사용 시 동작 동일).

### 시나리오 변화(신판) 예시

- 환자 A 활력징후 측정: `todo.validate.click_vital_set`(InvokeEvent 스텁) → `Interaction(target=vital_set, type=Use, completion=...)` 또는 Validator(InteractionSignal, count=1).
- 환자 A 안내문 자동 진행: `Dialogue(duration 8s 의도)` → `Dialogue(autoAdvanceSeconds=8)`.
- CPR 1→2 사이클: 별도 표현 불가 → `PlayerTag(Swap, A=cpr_team↔airway_team)` + `PlayerTag(Swap, C=defib_team↔medication_team)`.
- 환자 C 처치: 환자 B 흐름 복제(현재) → `Subgraph(graph=patient_treatment_common, bind patient=patientC)`.

### 문서화

- `scenario-graph-spec.md`에 신규 속성/노드 정의 추가(autoAdvanceSeconds, InteractionSignal 룰, PlayerTag Swap/ByTag, Subgraph).
- `scenario.schema.json`에 대응 스키마 추가(전부 optional).
- `json-conversion-rules.md`에 "todo.validate.* → Interaction/Validator 치환", "역할 교대 → PlayerTag Swap", "중복 흐름 → Subgraph" 규칙 추가.
- `patient-a-b-c-conversion-notes.md`의 TODO-SPEC 표를 본 제안 링크로 갱신.
- 후처리(문서 색인 갱신 + `validate-documentation-links.sh`) 수행.

### 가용성과 테스트

- 모든 신규 능력은 opt-in이며, 미지정 시 기존 동작과 100% 동일(회귀 위험 최소화).
- 단위 테스트: DTO 직렬/역직렬(신규 필드 round-trip), 스키마 검증(신규 필드 허용/누락 기본값), `ScenarioGraphLoader` 파싱.
- 통합 테스트: 자동 진행 타이머/입력 우선, Interaction 게이트 완료→진행, Swap 후 Parallel 재매칭, Subgraph 인라인 실행·복귀·순환 검출.
- 교차 런타임: 서버 권한 실행 경로(`ChatService.TryDispatchScenario`)에서 owner/tag/branch 동작 확인.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표:
  - 환자 A/B/C 시나리오의 `todo.validate.*` InvokeEvent 스텁 0건(전부 Interaction/Validator로 표현).
  - 환자 C 전용 복제 노드 제거(Subgraph 재사용으로 대체) 후 노드 수 유의미 감소.
  - 플레이모드에서 "No handler registered" 경고 0건.
- 수용 기준:
  - `/scenario execute @s patient_a_critical` 실행 시 ABCDE → CPR 2사이클 → ROSC 전 구간이 실제 인터랙션 게이트와 함께 진행된다.
  - CPR 교대 후 P006 브랜치가 올바른 플레이어에게 매칭된다.
  - 기존 `disaster_intro`/`validating_full`/샘플 그래프가 변경 없이 그대로 로드·실행된다.

### 링크, 참고사항

- 변환 검토/규칙: [`patient-a-b-c-conversion-notes.md`](../../../Documents/requirements/content-definitions/scenario/patient-a-b-c-conversion-notes.md), [`json-conversion-rules.md`](../../../Documents/requirements/content-definitions/scenario/json-conversion-rules.md)
- 선행 제안: [`Feature Proposal - ScenarioNode.md`](../Feature%20Proposal%20-%20ScenarioNode.md)
- 엔진 근거: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`, `Models/ScenarioGraphNodes/*`, `Resources/Schema/scenario.schema.json`
- 동봉 설계 명세: `validator-interaction-signal-spec.md`, `player-tag-rotation-spec.md`, `subgraph-reuse-spec.md`
