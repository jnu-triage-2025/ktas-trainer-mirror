# scenario-graph 표준 신판(안)

본 문서는 재난 시뮬레이션 시나리오에서 확인된 표현 공백을 보완하기 위한 ScenarioGraph 표준 개정안이다. 기존 노드를 유지하면서 신규 노드를 추가하는 방향으로 정의한다.

## ScenarioNode (기존)

기존 정의(대화, 선택, 사운드, 이동, 카메라 타겟, 병렬, 이벤트 호출, 검증, 퀘스트)는 유지한다.

## ScenarioNode (신규)

### NotificationNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Notification |
| Message | 문자열 | 표시할 안내/알림 텍스트 |
| DisplayMode | ScenarioNotificationDisplayMode | Overlay / Toast / Subtitle |
| Duration | float | (optional) 표시 시간(초) |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### DelayNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Delay |
| DurationSeconds | float | 대기 시간(초) |
| WaitUntil | ScenarioDelayWaitMode | Immediately / WaitUntilDone |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### InteractionNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Interaction |
| ActorScope | ScenarioInteractionActorScope | Player / Role / Any |
| TargetIdentifier | 문자열 | 상호작용 대상 식별자 |
| RequiredItemIdentifier | 문자열 | (optional) 필요한 아이템 식별자 |
| InteractionType | ScenarioInteractionType | Use / Inspect / Attach / Detach |
| CompletionConditionIdentifier | 문자열 | 완료 조건 식별자 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.CombineItem |
| InputItemIdentifiers | 문자열 목록 | 조합에 필요한 아이템 목록 |
| OutputItemIdentifier | 문자열 | 결과 아이템 식별자 |
| AutoCombine | bool | 자동 조립 여부 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### QuizNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Quiz |
| Question | 문자열 | 질문 텍스트 |
| Options | 문자열 목록 | 선택지 텍스트 목록 |
| CorrectIndex | int | 정답 인덱스 |
| OnCorrectNextIdentifier | 문자열 | 정답 시 이동할 노드 |
| OnIncorrectNextIdentifier | 문자열 | (optional) 오답 시 이동할 노드 |
| FeedbackCorrect | 문자열 | (optional) 정답 피드백 |
| FeedbackIncorrect | 문자열 | (optional) 오답 피드백 |

### StateUpdateNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.StateUpdate |
| TargetEntityIdentifier | 문자열 | 상태 변경 대상(환자 등) 식별자 |
| StateKey | 문자열 | 변경할 상태 키 |
| StateValue | 문자열 | 변경할 상태 값 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### RoleAssignmentNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.RoleAssignment |
| RoleOptions | 문자열 목록 | 선택 가능한 역할 목록 |
| AssignmentMode | ScenarioRoleAssignmentMode | Select / Auto |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

## ParallelBranch 확장

### ScenarioParallelBranch

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 브랜치의 시작 노드 식별자 |
| CompletionConditionIdentifier | 문자열 | 브랜치 완료 조건 식별자 |
| RequiredRoleIdentifiers | 문자열 목록 | (optional) 역할별 브랜치 매핑 |

## 기대 효과

- 아이템 조립, 상호작용, 퀴즈, 상태 변화가 데이터로 명시된다.
- InvokeEvent 집중도를 낮춰 시나리오 가독성과 검증성이 향상된다.
- 역할 기반 병렬 브랜치 할당이 명확해진다.
