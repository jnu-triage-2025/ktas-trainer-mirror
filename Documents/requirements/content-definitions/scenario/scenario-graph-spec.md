---
title: "scenario-graph"
doc_type: requirement
status: active
updated: 2026-04-14
---

# scenario-graph

이 프로젝트에서는 시나리오를 재생하기 위해 ScenarioGraph와 ScenarioNode를 사용한다. ScenarioNode의 집합이 ScenaioGraph로 표현된다. 최종적으로 이 그래프는 일반적인 게임에서 스토리 플레이, 컷씬 등을 표현하는데 사용되는 것이 목표이다.  

## ScenarioNode

ScenarioNode는 표현하고자 하는 내용에 따라 다양하게 데이터를 정의하여야 한다. 아래의 데이터 요구된다:

### DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Dialogue |
| SpeakerName | 문자열 | 대사를 말하는 캐릭터의 표시 이름 |
| DialogueContent | 문자열 | 대사 내용 |
| PortraitSprite | 문자열 | (optional) 대화 중 표시할 캐릭터 초상화 스프라이트 식별자 |
| NextNodeIdentifier | 문자열 | 다음 노드의 식별자 |

### ChoiceNode  

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Choice |
| SpeakerName | 문자열 | 대사를 말하는 캐릭터의 표시 이름 |
| DialogueContent | 문자열 | 선택지를 제시하기 위한 대사 내용 |
| PortraitSprite | 문자열 | (optional) 대화 중 표시할 캐릭터 초상화 스프라이트 식별자 |
| Options | ScenarioChoiceOption 목록 | 선택지 목록 |
| NextIdentifier | 문자열 | (optional) 선택지 없이 자동 진행될 때의 다음 노드 식별자 |

#### ScenarioChoiceOption

| 속성 | 타입 | 설명 |
|---|---|---|
| DisplayText | 문자열 | 선택지에 표시될 텍스트 |
| DisplayIconIdentifier | 문자열 | (optional) 선택지에 표시될 아이콘 식별자 |
| DisplayColor | Color | 선택지 표시 색상 |
| NextNodeIdentifier | 문자열 | 선택 시 이동할 다음 노드 식별자 |

### SoundNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Sound |
| SoundResourceIdentifier | 문자열 | 재생할 사운드 리소스 식별자 |
| WaitUntilFinished | bool | 재생 완료까지 대기할지 여부 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### PlayerMoveNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.PlayerMove |
| DestinationType | ScenarioMoveDestinationType | Position 또는 Waypoint |
| DestinationIdentifier | 문자열 | (Waypoint일 때) 목적지 식별자 |
| DestinationX | float | (Position일 때) 목적지 X |
| DestinationY | float | (Position일 때) 목적지 Y |
| DestinationZ | float | (Position일 때) 목적지 Z |
| IgnoreGroundCheck | bool | 지면 체크를 무시할지 여부 |
| MoveMode | ScenarioMoveMode | Instant / BySpeed / ByDuration |
| MoveSpeed | float | (BySpeed일 때) 이동 속도 |
| MoveDuration | float | (ByDuration일 때) 이동 시간 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### NPCMoveNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.NPCMove |
| NPCIdentifier | 문자열 | 이동할 NPC 식별자 |
| DestinationType | ScenarioMoveDestinationType | Position 또는 Waypoint |
| DestinationIdentifier | 문자열 | (Waypoint일 때) 목적지 식별자 |
| DestinationX | float | (Position일 때) 목적지 X |
| DestinationY | float | (Position일 때) 목적지 Y |
| DestinationZ | float | (Position일 때) 목적지 Z |
| IgnoreGroundCheck | bool | 지면 체크를 무시할지 여부 |
| MoveMode | ScenarioMoveMode | Instant / BySpeed / ByDuration |
| MoveSpeed | float | (BySpeed일 때) 이동 속도 |
| MoveDuration | float | (ByDuration일 때) 이동 시간 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### CameraTargetNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.CameraTarget |
| TargetObjectIdentifier | 문자열 | 카메라가 바라볼 대상 식별자 |
| OffsetX | float | 대상 기준 오프셋 X |
| OffsetY | float | 대상 기준 오프셋 Y |
| OffsetZ | float | 대상 기준 오프셋 Z |
| BlendTime | float | 타겟 전환 블렌드 시간 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### ParallelNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Parallel |
| Branches | ScenarioParallelBranch 목록 | 동시에 실행할 브랜치 목록 |
| WaitMode | ScenarioWaitMode | All / Any / None |
| AllocationType | ScenarioParallelAllocationType | 플레이어에게 브랜치를 할당하는 방식. SelfAll / RandomOneAll / SpreadRandom / SpreadOrdinary / **ByRole**(각 브랜치를 자격에 맞는 서로 다른 플레이어에게 1:1 배정, 다인 동시 협력용) |
| WhenBranchingPlayerNotMatched | ScenarioParallelMismatchHandling | 플레이어 수와 브랜치 수 불일치 시 처리 |
| NextIdentifier | 문자열 | 대기 조건 충족 후 이동할 다음 노드 식별자 |

#### ScenarioParallelBranch

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 브랜치의 시작 노드 식별자 |
| CompletionConditionIdentifier | 문자열 | 브랜치 완료 조건(수렴 라벨) 식별자. 브랜치 체인의 마지막 노드 NextIdentifier 가 이 값을 가리키면 브랜치 완료로 간주 |
| ~~RequiredRoleIdentifiers~~ | - | **엔진/스키마 미지원**(DTO 없음). 역할 의도는 RequiredPlayerTags 로 표현할 것 |
| RequiredPlayerTags | 문자열 목록 | (optional) 브랜치 실행 대상 플레이어 태그 목록 |
| ForbiddenPlayerTags | 문자열 목록 | (optional) 브랜치 실행 대상에서 제외할 플레이어 태그 목록 |
| RequiredPlayerTagsMatchMode | ScenarioPlayerTagMatchMode | (optional) 태그 매칭 모드. All(기본), Any |

`RequiredPlayerTags`와 `ForbiddenPlayerTags`를 함께 지정할 수 있다. 이 경우 포함 조건을 만족하면서 제외 조건을 만족하지 않는 플레이어만 브랜치에 할당된다.

ParallelNode는 한 플레이어의 브랜치 실행과 합류만 책임진다. 다른 플레이어로 흐름을 넘기는 동작은 서버 내부 신호로 분리하며, 이때 신호는 서버의 대기 레지스트리에서 register/resolve 방식으로 처리한다.

### ServerInternalSignal

| 항목 | 설명 |
|---|---|
| 목적 | 특정 플레이어의 흐름이 다른 플레이어의 상태 변화에 의존할 때 사용하는 서버 권위 내부 신호 |
| Register | 신호를 수신할 객체가 먼저 대기 요청을 등록한다. 이후 다른 대상이 같은 신호를 발생시키면 즉시 resolve 된다. |
| Resolve | 신호를 발생시키는 대상이 먼저 등록할 수 있다. 이후 수신자가 같은 신호를 등록하면 즉시 resolve 된다. |
| Direct Target | 수신 대상이 명확하면 목적지를 직접 지정한다. |
| 대상 표기 | `@s`는 실행자 자신, `@m`은 서버 권위 대상이다. `@m`은 서버 내부 신호의 기준 목적지로 사용한다. |

서버 내부 신호는 IPC 비슷한 형태로 동작한다. 목적지가 아직 정해지지 않은 경우에는 pending 상태로 보관하고, 목적지가 확정되면 서버가 register/resolve 매칭을 수행한다.

### InvokeEventNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| EventIdentifier | 문자열 | 호출할 이벤트 식별자 |
| MoveNextBehavior | ScenarioInvokeEventMoveNextBehavior | False / Immediately / WaitUntilDone |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Validator |
| Condition | ScenarioValidatorCondition | 비교 조건 |
| TargetCount | int | 비교 대상 수 |
| OnFailure | ScenarioValidatorOnFailure | Panic / Branching / Ignore |
| FailureNextIdentifier | 문자열 | (Branching일 때) 실패 시 이동할 노드 식별자 |
| WaitForCondition | bool (optional) | true 이면 조건 충족까지 진행을 막고 폴링 대기하는 게이트로 동작. 미지정/false 이면 1회 평가 후 OnFailure 정책(하위호환) |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.QuestControl |
| Operation | ScenarioQuestOperationType | Add / Update / Remove |
| FailureStrategy | ScenarioQuestFailureStrategy | Overwrite / Ignore / Panic |
| Quest | QuestData | 대상 퀘스트 데이터 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### NotificationNode (Removed)

`Notification` 노드 타입은 제거되었습니다.

기존 Notification 사용 사례는 아래처럼 `Dialogue`로 이관합니다.

- `nodeType`: `Notification` -> `Dialogue`
- `message` -> `dialogueContent`
- `speakerName`: `System` (고정)
- `portraitSpriteIdentifier`: `null`
- `nextIdentifier`: 그대로 유지

### DelayNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Delay |
| DurationSeconds | float | 대기 시간(초) |
| WaitUntil | ScenarioDelayWaitUntil | Immediately / WaitUntilDone |
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
| CompletionConditionIdentifier | 문자열 | (optional) 완료 이벤트 식별자 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.CombineItem |
| InputItemIdentifiers | 문자열 목록 | 조합 입력 아이템 식별자 목록 |
| OutputItemIdentifier | 문자열 | 조합 결과 아이템 식별자 |
| AutoCombine | bool | 자동 조합 여부 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### QuizNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.Quiz |
| Question | 문자열 | 문제 문항 |
| Options | 문자열 목록 | 객관식 보기 |
| CorrectIndex | int | 정답 인덱스(0-base) |
| OnCorrectNextIdentifier | 문자열 | 정답 시 다음 노드 식별자 |
| OnIncorrectNextIdentifier | 문자열 | (optional) 오답 시 다음 노드 식별자 |
| FeedbackCorrect | 문자열 | (optional) 정답 피드백 |
| FeedbackIncorrect | 문자열 | (optional) 오답 피드백 |

### StateUpdateNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.StateUpdate |
| TargetEntityIdentifier | 문자열 | 상태를 갱신할 대상 식별자 |
| StateKey | 문자열 | 상태 키(예: vitals.rhythm) |
| StateValue | 문자열 | 저장할 상태 값 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### RoleAssignmentNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.RoleAssignment |
| RoleOptions | 문자열 목록 | 선택 가능한 역할 목록 |
| AssignmentMode | ScenarioRoleAssignmentMode | Select / Auto |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### TagModificationNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | 문자열 | `TagModification` (구버전 호환: `PlayerTag`) |
| Operation | ScenarioPlayerTagOperationType | Add / Remove / Change |
| Scope | ScenarioPlayerTagScope | Current / Role / ExplicitPlayer |
| Tag | 문자열 | Add/Remove에서 사용할 태그 |
| FromTag | 문자열 | Change의 변경 전 태그 |
| ToTag | 문자열 | Change의 변경 후 태그 |
| TargetRoleIdentifier | 문자열 | (optional) Scope=Role일 때 대상 역할 |
| TargetPlayerIdentifier | 문자열 | (optional) Scope=ExplicitPlayer일 때 대상 플레이어 식별자 |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

### PlayTTSNode

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 노드의 고유 식별자 |
| NodeType | ScenarioNodeType | ScenarioNodeType.PlayTTS |
| TranscriptIdentifier | 문자열 | `TTSService`에 등록된 Transcript 식별자 (Transcript JSON의 `identifier` 필드) |
| Variables | `Dictionary<string, string>` | (optional) 동적 세그먼트 변수 오버라이드. 없으면 Transcript 기본값 사용 |
| WaitUntilFinished | bool | `true`이면 모든 클립 재생 완료 후 다음 노드 진행. 기본값 `true` |
| NextIdentifier | 문자열 | 다음 노드의 식별자 |

#### 동작 개요

- `TranscriptIdentifier`로 `TTSService`를 조회하여 오디오 클립 목록을 얻어 순서대로 재생합니다.
- `Variables`에 정의된 키-값으로 동적 세그먼트의 텍스트를 오버라이드합니다.
- 필요하지만 값이 없는 variable은 경고 로그를 출력하고 해당 세그먼트를 건너뜁니다.

#### JSON 예시

```json
{
  "identifier": "play-instruction",
  "nodeType": "PlayTTS",
  "transcriptIdentifier": "triage-move-patient",
  "variables": {
    "patient-name": "김철수",
    "destination": "수술실"
  },
  "waitUntilFinished": true,
  "nextIdentifier": "next-node"
}
```

## ScenarioGraph

ScenarioGraph는 `Identifier`를 키로 `IScenarioNode`를 보관한다. 노드를 추가하거나 식별자로 조회할 수 있어야 한다.

| 속성 | 타입 | 설명 |
|---|---|---|
| Identifier | 문자열 | 시나리오 그래프 식별자 |
| Tags | 문자열 목록 | (optional) 그래프에서 사용할 태그 선언 목록 |
| Nodes | `Dictionary<string, IScenarioNode>` | 노드 맵 |

그래프에 `tags`를 선언하면 런타임 로더가 노드/브랜치에서 사용된 태그와 비교한다. 선언되지 않은 태그가 사용되면 경고 로그가 출력된다.
