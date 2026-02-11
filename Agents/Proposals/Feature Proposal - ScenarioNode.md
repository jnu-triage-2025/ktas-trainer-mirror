### 개요

재난 시뮬레이션 시나리오를 ScenarioGraph로 마이그레이션할 때, 현재 ScenarioNode 정의만으로는 플레이어 상호작용, 퀴즈, 상태 변화, 시간 대기, 역할 배정 등 핵심 단계가 명시적으로 표현되지 않는다. 이로 인해 대부분을 InvokeEvent에 위임해야 하며, 시나리오 데이터의 가독성과 검증 가능성이 낮아진다. 이를 보완하기 위해 상호작용과 학습 평가를 표현하는 신규 ScenarioNode 정의를 제안한다.

### 해결하려는 문제 상황

나는 시뮬레이션 시나리오 설계자로서 플레이어의 행동(아이템 사용, 장비 조립, 환자 상호작용), 학습 평가(퀴즈), 상태 갱신(활력징후 변화), 시간 대기, 역할 배정 등을 시나리오 데이터만으로 명확히 표현하고 싶다. 그래야 구현체에 의존하지 않고 시나리오를 검증하고, 추후 편집 및 유지보수를 쉽게 할 수 있다.

### 사용자 경험 목표

- 시나리오 표에서 행동 흐름이 명확히 보인다.
- 이벤트 스크립트에 모든 로직을 숨기지 않아도 된다.
- 시나리오 검증 도구가 누락된 필드를 자동으로 감지할 수 있다.

### 제안

다음 ScenarioNode 확장을 제안한다. 기존 InvokeEvent 기반 흐름을 유지하면서, 반복적으로 사용되는 행동을 데이터로 분리한다.

1) NotificationNode
- 시스템 메시지/안내문/알림 텍스트를 표현한다.
- 속성: Identifier, NodeType(Notification), Message, DisplayMode(Overlay/Toast/Subtitle), Duration(optional), NextIdentifier

2) DelayNode
- 일정 시간 대기 또는 조건부 대기를 표현한다.
- 속성: Identifier, NodeType(Delay), DurationSeconds, WaitUntil(Immediately/WaitUntilDone), NextIdentifier

3) InteractionNode
- 플레이어가 특정 대상과 상호작용하거나 아이템을 사용하도록 요구한다.
- 속성: Identifier, NodeType(Interaction), ActorScope(Player/Role/Any), TargetIdentifier, RequiredItemIdentifier(optional), InteractionType(Use/Inspect/Attach/Detach), CompletionConditionIdentifier, NextIdentifier

4) CombineItemNode
- 후두경 손잡이+블레이드, ET-tube+스타일렛 등 조립 과정을 표현한다.
- 속성: Identifier, NodeType(CombineItem), InputItemIdentifiers, OutputItemIdentifier, AutoCombine(bool), NextIdentifier

5) QuizNode
- 교육용 객관식 문제와 정답, 피드백을 표로 명시한다.
- 속성: Identifier, NodeType(Quiz), Question, Options(list), CorrectIndex, OnCorrectNextIdentifier, OnIncorrectNextIdentifier(optional), FeedbackCorrect(optional), FeedbackIncorrect(optional)

6) StateUpdateNode
- 환자 상태(활력징후, 의식, 리듬 등)를 명시적으로 갱신한다.
- 속성: Identifier, NodeType(StateUpdate), TargetEntityIdentifier, StateKey, StateValue, NextIdentifier

7) RoleAssignmentNode
- 플레이어 역할 선택과 확정을 데이터로 표현한다.
- 속성: Identifier, NodeType(RoleAssignment), RoleOptions, AssignmentMode(Select/Auto), NextIdentifier

추가로, ScenarioParallelBranch에 RequiredRoleIdentifiers(optional)를 추가하여 병렬 브랜치를 역할에 따라 명시적으로 매핑할 수 있도록 한다.

### 시나리오 변화(신판) 예시

아래는 재난 시뮬레이션 시나리오를 신규 노드로 마이그레이션할 때의 변화 예시이다. 기존 InvokeEvent 의존도를 낮추고, 상호작용과 학습 평가를 데이터로 명시한다.

1) 재난 초기 대응 및 중증도 분류
- 역할 선택: Choice + InvokeEvent(select_role_*) -> RoleAssignmentNode(role_options=[A,B,C,D])
- 상황 공유 안내: Dialogue(시스템 메시지) -> NotificationNode(message, display=Overlay)
- 물품 준비 완료 대기: InvokeEvent(prep_*) -> InteractionNode 또는 DelayNode로 분해(가능 범위)
- 병렬 브랜치 역할 지정: ParallelBranch + RequiredRoleIdentifiers 적용

2) 환자 A 중증 처치
- 삽관 보조 조립: InvokeEvent(assemble_laryngoscope) -> CombineItemNode(input=[handle, blade], output=laryngoscope)
- 장비 전달/부착: InvokeEvent(hand_over_*) -> InteractionNode(actor=Role, target=doctor, required_item=...)
- CPR 퀴즈: InvokeEvent(show_quiz) -> QuizNode(question, options, correct_index)
- 리듬/활력징후 변화: InvokeEvent(update_vitals) -> StateUpdateNode(target=patient_a, key=vitals.rhythm, value=PEA/Asystole/QRS)

3) 환자 B/C 지연 처치
- 동공 반응 확인: InvokeEvent(check_pupil) -> InteractionNode(target=patient_eye, type=Inspect) + StateUpdateNode
- 산소 3L 설정: InvokeEvent(set_oxygen) -> InteractionNode(target=oxygen_flowmeter, type=Use)
- CT 이동 안내: Dialogue -> NotificationNode 또는 PlayerMoveNode(팀 이동 지원 시)

### 자세한 달성 목표

- 재난 시뮬레이션의 아이템 조립, 상호작용, 퀴즈, 상태 변화가 ScenarioNode로 표현된다.
- InvokeEvent에 집중된 로직을 단계적으로 데이터화할 수 있다.
- 시나리오 데이터의 검증, 로컬라이제이션, 테스트 자동화가 쉬워진다.

### 문서화

- scenario-graph 문서에 신규 ScenarioNode 정의 섹션 추가.
- 시나리오 템플릿에 신규 노드 예시 추가.

### 가용성과 테스트

- 기존 시나리오와의 호환성을 유지하기 위해 신규 NodeType은 선택적 사용으로 설계한다.
- ScenarioNode 파서와 실행기의 단위 테스트에 신규 노드를 추가한다.
- 리소스 식별자 유효성 검사(아이템, 대상, 역할 등)를 테스트한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 재난 시뮬레이션 시나리오가 InvokeEvent 없이도 50% 이상 노드로 표현된다.
- 수용 기준:
  - InteractionNode, CombineItemNode, QuizNode, StateUpdateNode가 실행기에서 정상 작동한다.
  - 시나리오 파서가 신규 노드를 인식하고 유효성 검사를 수행한다.
  - 기존 시나리오가 변경 없이 재생된다.

### 링크, 참고사항

- scenario-graph 문서
- 재난 시뮬레이션 시나리오(시나리오 표)
