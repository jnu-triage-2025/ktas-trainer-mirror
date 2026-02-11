# ScenarioGraph 마이그레이션 정의(안)

본 문서는 기존 시나리오를 신규 ScenarioNode 정의로 마이그레이션할 때의 기대 변화와 치환 규칙을 정리한다.

## 기대 변화 요약

- InvokeEvent에 숨겨진 행동을 Interaction/CombineItem/Quiz/StateUpdate로 분리한다.
- 알림/안내문은 NotificationNode로 표준화한다.
- 시간 지연은 DelayNode로 명시한다.
- 역할 선택은 RoleAssignmentNode로 분리한다.
- 병렬 브랜치에 RequiredRoleIdentifiers를 부여해 역할-브랜치 매핑을 명확히 한다.

## 치환 규칙(예시)

### 역할 선택

- 기존: Choice + InvokeEvent(select_role_*)
- 변경: RoleAssignmentNode(role_options=[A,B,C,D])

### 아이템 조립

- 기존: InvokeEvent(assemble_laryngoscope)
- 변경: CombineItemNode(input=[handle, blade], output=laryngoscope)

### 대상 상호작용

- 기존: InvokeEvent(apply_bandage)
- 변경: InteractionNode(actor=Role, target=patient_wound, required_item=bandage, type=Attach)

### 퀴즈/평가

- 기존: InvokeEvent(show_quiz)
- 변경: QuizNode(question, options, correct_index)

### 상태 변화

- 기존: InvokeEvent(update_vitals_pea)
- 변경: StateUpdateNode(target=patient_a, key=vitals.rhythm, value=PEA)

### 안내 메시지

- 기존: Dialogue(시스템 메시지)
- 변경: NotificationNode(message="...", display=Overlay)

### 시간 대기

- 기존: InvokeEvent(wait_seconds)
- 변경: DelayNode(duration_seconds=2.0)

## 단계적 이행 전략

1) Notification/Delay/RoleAssignment를 먼저 도입한다.
2) Interaction/CombineItem을 도입하여 아이템/장비 작업을 데이터화한다.
3) Quiz/StateUpdate를 도입하여 교육 평가와 환자 상태 변화를 데이터로 표현한다.
4) 병렬 브랜치에 RequiredRoleIdentifiers를 적용한다.

## 수용 기준

- 신규 노드가 파서와 실행기에서 정상 동작한다.
- 기존 시나리오가 변경 없이 동작한다.
- 신규 노드로 표현된 시나리오가 InvokeEvent 의존도를 50% 이상 낮춘다.
