# scenario template

이 문서는 ScenarioGraph를 표로 설계하기 위한 템플릿이다. 각 행에 노드 식별자, 타입, 핵심 내용을 서술한다.

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | |
| 요약 | |
| 주요 등장인물 | |
| 주요 장소 | |
| 리소스 식별자 - 사운드 | |
| 리소스 식별자 - 초상화 | |
| 리소스 식별자 - 웨이포인트 | |
| 리소스 식별자 - 카메라 타겟 | |
| 시작 노드 Identifier | |

## 시나리오 본문

| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D___ | Dialogue | SpeakerName가 DialogueContent를 말한다. PortraitSprite가 있다면 명시한다. | ___ |
| C___ | Choice | SpeakerName가 선택지를 제시한다. DialogueContent를 서술하고 Options를 줄글로 정리한다. | (optional) ___ |
| C___-OPT1 | ChoiceOption | DisplayText / (optional) DisplayIconIdentifier / DisplayColor / NextNodeIdentifier를 서술한다. | NextNodeIdentifier |
| C___-OPT2 | ChoiceOption | DisplayText / (optional) DisplayIconIdentifier / DisplayColor / NextNodeIdentifier를 서술한다. | NextNodeIdentifier |
| S___ | Sound | SoundResourceIdentifier를 재생한다. WaitUntilFinished 여부를 서술한다. | ___ |
| PM___ | PlayerMove | DestinationType에 따라 이동한다. Position이면 DestinationX/Y/Z, Waypoint면 DestinationIdentifier를 명시한다. MoveMode, MoveSpeed/MoveDuration, IgnoreGroundCheck를 서술한다. | ___ |
| NM___ | NPCMove | NPCIdentifier가 이동한다. DestinationType에 따른 목적지와 MoveMode, MoveSpeed/MoveDuration, IgnoreGroundCheck를 서술한다. | ___ |
| CT___ | CameraTarget | TargetObjectIdentifier를 바라보도록 전환한다. OffsetX/Y/Z와 BlendTime을 서술한다. | ___ |
| P___ | Parallel | Branches의 Identifier를 나열하고 WaitMode, AllocationType, WhenBranchingPlayerNotMatched를 요약한다. | ___ |
| P___-B___ | ParallelBranch | 브랜치 시작 노드와 CompletionConditionIdentifier를 서술한다. | CompletionConditionIdentifier |
| E___ | InvokeEvent | EventIdentifier를 호출한다. MoveNextBehavior에 따른 진행 방식을 서술한다. | ___ |
| V___ | Validator | Condition과 TargetCount로 검증한다. 실패 시 OnFailure 흐름과 FailureNextIdentifier(Branching)를 서술한다. | ___ |
| Q___ | QuestControl | Quest에 대해 Operation을 수행한다. FailureStrategy에 따른 처리 방식을 서술한다. | ___ |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | |
| 종료 연출/설명 | |
