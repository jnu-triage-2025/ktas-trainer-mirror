# API 레퍼런스: ScenarioGraph 노드 타입 전체 레퍼런스

> **네임스페이스:** `MultiplayerInfrastructure.Scenario`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Models/ScenarioGraphNodes/`

---

## 0. 문서 목적

시나리오 그래프를 구성하는 모든 노드 타입의 필드, 동작, JSON 작성법을 정리합니다.  
각 노드는 `IScenarioNode` 인터페이스를 구현하며, JSON 직렬화 시 `nodeType` 필드로 타입을 구분합니다.

---

## 1. 공통 인터페이스

모든 노드가 구현하는 `IScenarioNode`:

| 필드 | 타입 | 설명 |
|---|---|---|
| `identifier` | `string` | 이 노드의 고유 식별자 (그래프 내 유일) |
| `nodeType` | `ScenarioNodeType` | 노드 타입 (직렬화 시 자동 설정) |
| `nextIdentifier` | `string` | 실행 완료 후 이동할 다음 노드 식별자. `null`이면 시나리오 종료 |

---

## 2. 노드 타입 목록

| 열거형 값 | 클래스 | 요약 |
|---|---|---|
| `Dialogue` | `ScenarioDialogueNode` | 대화창 표시 |
| `Choice` | `ScenarioChoiceNode` | 분기 선택지 |
| `Sound` | `ScenarioSoundNode` | 사운드 재생 |
| `PlayerMove` | `ScenarioPlayerMoveNode` | 플레이어 이동 |
| `NPCMove` | `ScenarioNPCMoveNode` | NPC 이동 |
| `CameraTarget` | `ScenarioCameraTargetNode` | 카메라 타겟 전환 |
| `Parallel` | `ScenarioParallelNode` | 병렬 브랜치 실행 |
| `InvokeEvent` | `ScenarioInvokeEventNode` | 외부 이벤트 핸들러 호출 |
| `ServerInternalSignal` | `ScenarioServerInternalSignalNode` | 서버 내부 신호 등록/해제 |
| `Validator` | `ScenarioValidatorNode` | 조건 검사 / 게이트 대기 |
| `QuestControl` | `ScenarioQuestControlNode` | 퀘스트 추가/변경/제거 |
| `QuestWaypointHighlight` | `ScenarioQuestWaypointHighlightNode` | 웨이포인트 강조 |
| `Delay` | `ScenarioDelayNode` | 시간 대기 |
| `Interaction` | `ScenarioInteractionNode` | 인터랙션 완료 대기 |
| `CombineItem` | `ScenarioCombineItemNode` | 아이템 합성 |
| `Quiz` | `ScenarioQuizNode` | 정답/오답 분기 퀴즈 |
| `StateUpdate` | `ScenarioStateUpdateNode` | 엔티티 상태 변수 갱신 |
| `PlayTTS` | `ScenarioPlayTTSNode` | TTS 음성 재생 |
| `PlayerTag` | `ScenarioPlayerTagNode` | 플레이어 태그 조작 |
| `EntityPresetSpawn` | `ScenarioEntityPresetSpawnNode` | 엔티티 프리셋 스폰 |
| `EntityTag` | `ScenarioEntityTagNode` | 엔티티 태그 조작 |
| `EntityInit` | `ScenarioEntityInitNode` | 엔티티 초기 상태 설정 |
| `TriageAssessControl` | `ScenarioTriageAssessControlNode` | 트리아지 평가 활성/비활성 |
| `PatientMedicalStatePreset` | `ScenarioPatientMedicalStatePresetNode` | 환자 의료 상태 일괄 설정 |
| `ItemSubmissionConfig` | `ScenarioItemSubmissionConfigNode` | 아이템 제출 Interactable 설정 |
| `NpcInteractControl` | `ScenarioNpcInteractControlNode` | NPC Interactable 활성/비활성 |
| `ChatPrint` | `ScenarioChatPrintNode` | 채팅/콘솔 텍스트 출력 |
| `ExecuteCommand` | `ScenarioExecuteCommandNode` | 인게임 커맨드 실행 |
| `TimeControl` | `ScenarioTimeControlNode` | HUD 타이머 제어 |

---

## 3. 노드별 상세

---

### 3.1 `Dialogue` — 대화창 표시

```json
{
  "nodeType": "Dialogue",
  "identifier": "intro_dialogue",
  "nextIdentifier": "next_node",
  "speakerName": "의사",
  "dialogueContent": "환자 상태를 확인해주세요.",
  "portraitSpriteIdentifier": null,
  "interactionRequired": false,
  "autoAdvanceSeconds": null,
  "playTTS": false,
  "ttsVoiceIdentifier": null
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `speakerName` | `string` | `null` | 대화창 상단에 표시될 화자 이름 |
| `dialogueContent` | `string` | `null` | 대화 본문 |
| `portraitSpriteIdentifier` | `string` | `null` | 화자 초상화 스프라이트 식별자 |
| `interactionRequired` | `bool` | `false` | true이면 플레이어 입력 없이 자동으로 넘어가지 않음 |
| `autoAdvanceSeconds` | `float?` | `null` | 지정 시간(초) 후 자동 진행. null/0 이하이면 사용자 입력 대기 |
| `playTTS` | `bool` | `false` | true이면 `dialogueContent`를 TTS로 함께 재생 |
| `ttsVoiceIdentifier` | `string` | `null` | 사용할 TTS 목소리 프로파일 식별자. null이면 기본 목소리 |

**동작:** `ScenarioController.State.ExecutingDialogue`로 전환. `Advance()` 호출 또는 `autoAdvanceSeconds` 경과 시 `nextIdentifier`로 진행.

---

### 3.2 `Choice` — 분기 선택지

```json
{
  "nodeType": "Choice",
  "identifier": "treatment_choice",
  "nextIdentifier": null,
  "speakerName": "간호사",
  "dialogueContent": "어떻게 처치하시겠습니까?",
  "portraitSpriteIdentifier": null,
  "playTTS": false,
  "ttsVoiceIdentifier": null,
  "options": [
    {
      "displayText": "수액 투여",
      "displayIconIdentifier": null,
      "displayColor": { "r": 1, "g": 1, "b": 1, "a": 1 },
      "nextNodeIdentifier": "give_iv"
    },
    {
      "displayText": "기도 확보",
      "nextNodeIdentifier": "airway_node"
    }
  ]
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `options` | `List<ScenarioChoiceOption>` | 선택지 목록 |
| `options[].displayText` | `string` | 선택지 표시 텍스트 |
| `options[].displayIconIdentifier` | `string` | 선택지 아이콘 식별자 (null 가능) |
| `options[].displayColor` | `Color` | 선택지 표시 색상 |
| `options[].nextNodeIdentifier` | `string` | 선택 시 이동할 노드 식별자 |

**동작:** `ExecutingChoice`로 전환. `SelectOption(index)` 호출 시 선택된 `options[index].nextNodeIdentifier`로 분기.  
**주의:** 선택지가 없으면 다음 노드로 진행 불가.

---

### 3.3 `Sound` — 사운드 재생

```json
{
  "nodeType": "Sound",
  "identifier": "alarm_sound",
  "nextIdentifier": "next",
  "soundResourceIdentifier": "alarm_beep",
  "waitUntilFinished": false
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `soundResourceIdentifier` | `string` | `null` | Resources 폴더 내 사운드 클립 식별자 |
| `waitUntilFinished` | `bool` | `false` | true이면 재생 완료 후 진행 |

---

### 3.4 `PlayerMove` — 플레이어 이동

```json
{
  "nodeType": "PlayerMove",
  "identifier": "move_to_triage",
  "nextIdentifier": "next",
  "destinationType": "Waypoint",
  "destinationIdentifier": "triage_room_waypoint",
  "ignoreGroundCheck": false,
  "moveMode": "BySpeed",
  "moveSpeed": 3.0,
  "moveDuration": 0.0
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `destinationType` | `ScenarioMoveDestinationType` | `Position` | `Position`(좌표) 또는 `Waypoint`(웨이포인트 식별자) |
| `destinationIdentifier` | `string` | `null` | `Waypoint` 타입 시 웨이포인트 식별자 |
| `destinationX/Y/Z` | `float` | `0` | `Position` 타입 시 목적지 좌표 |
| `ignoreGroundCheck` | `bool` | `false` | true이면 지면 체크 무시 |
| `moveMode` | `ScenarioMoveMode` | `BySpeed` | `Instant`(순간이동) / `BySpeed`(속도 기반) / `ByDuration`(시간 기반) |
| `moveSpeed` | `float` | `0` | `BySpeed` 모드에서 이동 속도 |
| `moveDuration` | `float` | `0` | `ByDuration` 모드에서 이동 시간(초) |

---

### 3.5 `NPCMove` — NPC 이동

`PlayerMove`와 동일한 이동 필드에 추가로:

| 필드 | 타입 | 설명 |
|---|---|---|
| `npcIdentifier` | `string` | 이동시킬 NPC 엔티티 식별자 |

```json
{
  "nodeType": "NPCMove",
  "identifier": "doctor_move",
  "nextIdentifier": "next",
  "npcIdentifier": "doctor_npc",
  "destinationType": "Waypoint",
  "destinationIdentifier": "treatment_room"
}
```

---

### 3.6 `CameraTarget` — 카메라 타겟 전환

```json
{
  "nodeType": "CameraTarget",
  "identifier": "focus_patient",
  "nextIdentifier": "next",
  "targetObjectIdentifier": "patient_a",
  "offsetX": 0.0,
  "offsetY": 1.5,
  "offsetZ": -2.0,
  "blendTime": 1.0
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `targetObjectIdentifier` | `string` | `null` | 카메라가 바라볼 엔티티 식별자 |
| `offsetX/Y/Z` | `float` | `0` | 타겟 위치로부터의 오프셋 |
| `blendTime` | `float` | `1` | 카메라 전환 블렌드 시간(초) |

---

### 3.7 `Parallel` — 병렬 브랜치 실행

```json
{
  "nodeType": "Parallel",
  "identifier": "parallel_tasks",
  "nextIdentifier": "merge_node",
  "waitMode": "All",
  "allocationType": "SelfAll",
  "whenBranchingPlayerNotMatched": "Panic",
  "branches": [
    {
      "identifier": "task_a_start",
      "completionConditionIdentifier": "task_a_complete",
      "requiredPlayerTags": ["role_a"],
      "forbiddenPlayerTags": [],
      "requiredPlayerTagsMatchMode": "All"
    }
  ]
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `branches` | `IReadOnlyList<ScenarioParallelBranch>` | — | 동시 실행할 브랜치 목록 |
| `waitMode` | `ScenarioWaitMode` | `All` | `All`(모든 완료 대기) / `Any`(하나 완료 시 진행) / `None`(즉시 진행) |
| `allocationType` | `ScenarioParallelAllocationType` | `SelfAll` | 플레이어-브랜치 할당 방식 |
| `whenBranchingPlayerNotMatched` | `ScenarioParallelMismatchHandling` | `Panic` | 플레이어 수와 브랜치 수 불일치 처리 |

**`ScenarioParallelBranch` 필드:**

| 필드 | 타입 | 설명 |
|---|---|---|
| `identifier` | `string` | 브랜치 시작 노드 식별자 |
| `completionConditionIdentifier` | `string` | 브랜치 완료 조건 식별자 |
| `requiredPlayerTags` | `IReadOnlyList<string>` | 이 브랜치를 배정받을 플레이어가 보유해야 할 태그 목록 |
| `forbiddenPlayerTags` | `IReadOnlyList<string>` | 이 브랜치 배정을 금지하는 태그 목록 |
| `requiredPlayerTagsMatchMode` | `ScenarioPlayerTagMatchMode` | 태그 매칭 모드 (`All` / `Any`) |

**`ScenarioParallelAllocationType` 값:**

| 값 | 설명 |
|---|---|
| `SelfAll` | 모든 플레이어가 모든 브랜치를 동시에 진행 |
| `RandomOneAll` | 무작위로 하나의 브랜치만 선택, 전원 동일하게 진행 |
| `SpreadRandom` | 플레이어를 랜덤 분산 (역할 배분) |
| `SpreadOrdinary` | 플레이어를 순서대로 분산 (역할 배분) |

---

### 3.8 `InvokeEvent` — 외부 이벤트 핸들러 호출

```json
{
  "nodeType": "InvokeEvent",
  "identifier": "trigger_patient_collapse",
  "nextIdentifier": "next",
  "eventIdentifier": "patient_a_collapse",
  "moveNextBehavior": "WaitUntilDone"
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `eventIdentifier` | `string` | — | `ScenarioEventIdentifierRegistry`에 등록된 이벤트 식별자 |
| `moveNextBehavior` | `ScenarioInvokeEventMoveNextBehavior` | `WaitUntilDone` | `False`(수동 진행) / `Immediately`(발화 즉시 진행) / `WaitUntilDone`(핸들러 완료 대기) |

**동작:** `ScenarioEventIdentifierRegistry`에서 `eventIdentifier`에 해당하는 핸들러를 찾아 실행합니다.  
`MoveNextBehavior.WaitUntilDone`이면 핸들러가 `UniTask`를 반환할 경우 완료를 기다립니다.

---

### 3.9 `ServerInternalSignal` — 서버 내부 신호

```json
{
  "nodeType": "ServerInternalSignal",
  "identifier": "register_iv_signal",
  "nextIdentifier": "next",
  "targetIdentifier": "server",
  "signalIdentifier": "sig.iv_line_connected",
  "operation": "Register",
  "waitForResolution": true
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `targetIdentifier` | `string` | `"server"` | 신호 대상 식별자 (기본값 `ScenarioServerInternalSignalRegistry.ServerTarget`) |
| `signalIdentifier` | `string` | — | 등록/해제할 신호 식별자 |
| `operation` | `ScenarioServerInternalSignalOperationType` | `Register` | `Register`(등록) / `Unregister`(해제) |
| `waitForResolution` | `bool` | `true` | true이면 신호가 해소될 때까지 대기 |

---

### 3.10 `Validator` — 조건 검사 / 게이트

```json
{
  "nodeType": "Validator",
  "identifier": "wait_for_iv",
  "nextIdentifier": "iv_done",
  "failureNextIdentifier": "iv_failed",
  "onFailure": "Branching",
  "failureReportTargets": 2,
  "waitForCondition": true,
  "waitTimeoutSeconds": 30.0,
  "onWaitTimeout": "FailBranch",
  "rootConditions": [
    {
      "condition": "PlayerCountGreaterThanOrEqual",
      "targetCount": 1,
      "playerScope": "Any",
      "validationRules": [
        {
          "type": "Registry",
          "condition": "Contains",
          "registryType": "RuntimeState",
          "registryIdentifier": "sig.iv_line_connected"
        }
      ]
    }
  ]
}
```

**`ScenarioValidatorRootCondition` 필드:**

| 필드 | 타입 | 설명 |
|---|---|---|
| `condition` | `ScenarioValidatorCondition` | 검사 조건 종류 |
| `targetCount` | `int` | `PlayerCount*` 조건 비교 대상 수 |
| `playerTag` | `string` | `PlayerAssignedTag` 조건 대상 태그 |
| `playerScope` | `ScenarioValidatorPlayerScope` | `Any`(누구든) / `All`(모두) / `Owner`(소유자) |
| `validationRules` | `IReadOnlyList<ScenarioValidatorRule>` | 추가 규칙 목록 |

**`ScenarioValidatorCondition` 값:**

| 값 | 설명 |
|---|---|
| `PlayerCountEqual` | 접속 플레이어 수 == targetCount |
| `PlayerCountNotEqual` | != targetCount |
| `PlayerCountLessThan` | < targetCount |
| `PlayerCountLessThanOrEqual` | <= targetCount |
| `PlayerCountGreaterThan` | > targetCount |
| `PlayerCountGreaterThanOrEqual` | >= targetCount |
| `RegistryContains` | 레지스트리에 특정 항목 존재 여부 |
| `PlayerAssignedTag` | 플레이어가 특정 태그 보유 여부 |

**게이트 타임아웃 (`waitForCondition: true` 시):**

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `waitForCondition` | `bool` | `false` | true이면 조건 충족까지 무한 대기 게이트로 동작 |
| `waitTimeoutSeconds` | `float?` | `null` | 타임아웃(초). null/0 이하이면 무한 대기 |
| `onWaitTimeout` | `ScenarioValidatorWaitTimeoutBehavior` | `KeepWaiting` | 타임아웃 시 동작 정책 |

**`ScenarioValidatorWaitTimeoutBehavior` 값:**

| 값 | 설명 |
|---|---|
| `KeepWaiting` | 타임아웃 무시, 계속 대기 (기본) |
| `FailBranch` | `failureNextIdentifier`로 분기 (없으면 KeepWaiting 폴백) |
| `ForceAdvance` | `nextIdentifier`로 강제 진행 |
| `WarnAndKeepWaiting` | 콘솔+인게임챗 경고 후 계속 대기 |

---

### 3.11 `QuestControl` — 퀘스트 제어

```json
{
  "nodeType": "QuestControl",
  "identifier": "add_triage_quest",
  "nextIdentifier": "next",
  "operation": "Add",
  "failureStrategy": "Overwrite",
  "questDefinitionIdentifier": null,
  "quest": {
    "title": "환자 트리아지",
    "description": "환자 3명을 트리아지하세요."
  }
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `operation` | `ScenarioQuestOperationType` | — | `Add` / `Update` / `Remove` |
| `failureStrategy` | `ScenarioQuestFailureStrategy` | `Overwrite` | 퀘스트 충돌 시 처리: `Overwrite`(덮어쓰기) / `Ignore`(무시) / `Panic`(오류) |
| `questDefinitionIdentifier` | `string` | `null` | 사전 정의된 퀘스트 식별자 (null이면 `quest` 인라인 사용) |
| `quest` | `QuestData` | `null` | 인라인 퀘스트 데이터 |

---

### 3.12 `QuestWaypointHighlight` — 웨이포인트 강조

```json
{
  "nodeType": "QuestWaypointHighlight",
  "identifier": "highlight_triage_room",
  "nextIdentifier": "next",
  "waypointIdentifier": "triage_room"
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `waypointIdentifier` | `string` | 강조할 웨이포인트 식별자 |

---

### 3.13 `Delay` — 시간 대기

```json
{
  "nodeType": "Delay",
  "identifier": "wait_5_seconds",
  "nextIdentifier": "next",
  "durationSeconds": 5.0,
  "waitUntil": "WaitUntilDone"
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `durationSeconds` | `float` | `0` | 대기 시간(초) |
| `waitUntil` | `ScenarioDelayWaitUntil` | `WaitUntilDone` | `Immediately`(즉시 다음 노드 진행, 지연 없이) / `WaitUntilDone`(지정 시간 완료 후 진행) |

---

### 3.14 `Interaction` — 인터랙션 완료 대기

```json
{
  "nodeType": "Interaction",
  "identifier": "wait_iv_attach",
  "nextIdentifier": "next",
  "actorScope": "Player",
  "targetIdentifier": "iv_attach_point",
  "requiredItemIdentifier": "intravenous_set",
  "interactionType": "Attach",
  "completionConditionIdentifier": null
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `actorScope` | `ScenarioInteractionActorScope` | `Player` | `Player`(플레이어만) / `Any`(모든 행위자) |
| `targetIdentifier` | `string` | — | 인터랙션 대상 엔티티 식별자 |
| `requiredItemIdentifier` | `string` | `null` | 인터랙션에 필요한 아이템 식별자 |
| `interactionType` | `ScenarioInteractionType` | `Use` | `Use` / `Inspect` / `Attach` / `Detach` |
| `completionConditionIdentifier` | `string` | `null` | 인터랙션 완료 조건 식별자 |

---

### 3.15 `CombineItem` — 아이템 합성

```json
{
  "nodeType": "CombineItem",
  "identifier": "combine_iv_set",
  "nextIdentifier": "next",
  "inputItemIdentifiers": ["intravenous_set", "normal_saline_1000ml"],
  "outputItemIdentifier": "normal_saline_intravenous_ready",
  "autoCombine": false
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `inputItemIdentifiers` | `IReadOnlyList<string>` | 합성에 사용할 아이템 식별자 목록 |
| `outputItemIdentifier` | `string` | 합성 결과 아이템 식별자 |
| `autoCombine` | `bool` | true이면 조건 충족 시 자동으로 합성 실행 |

---

### 3.16 `Quiz` — 정답/오답 분기 퀴즈

```json
{
  "nodeType": "Quiz",
  "identifier": "triage_quiz",
  "nextIdentifier": null,
  "question": "이 환자의 KTAS 등급은?",
  "options": ["1등급", "2등급", "3등급"],
  "correctIndex": 0,
  "onCorrectNextIdentifier": "quiz_correct",
  "onIncorrectNextIdentifier": "quiz_incorrect",
  "feedbackCorrect": "정답입니다!",
  "feedbackIncorrect": "다시 확인하세요.",
  "playTTS": false,
  "ttsVoiceIdentifier": null
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `question` | `string` | 퀴즈 질문 텍스트 |
| `options` | `IReadOnlyList<string>` | 선택지 목록 |
| `correctIndex` | `int` | 정답 선택지 인덱스 (0-based) |
| `onCorrectNextIdentifier` | `string` | 정답 선택 시 이동 노드 |
| `onIncorrectNextIdentifier` | `string` | 오답 선택 시 이동 노드 |
| `feedbackCorrect` | `string` | 정답 피드백 텍스트 |
| `feedbackIncorrect` | `string` | 오답 피드백 텍스트 |
| `playTTS` | `bool` | TTS 재생 여부 |
| `ttsVoiceIdentifier` | `string` | TTS 목소리 프로파일 식별자 |

---

### 3.17 `StateUpdate` — 엔티티 상태 변수 갱신

```json
{
  "nodeType": "StateUpdate",
  "identifier": "mark_iv_done",
  "nextIdentifier": "next",
  "targetEntityIdentifier": "patient_a",
  "stateKey": "iv_inserted",
  "stateValue": "true"
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `targetEntityIdentifier` | `string` | 상태를 업데이트할 엔티티 식별자 |
| `stateKey` | `string` | 상태 키 |
| `stateValue` | `string` | 저장할 값 |

---

### 3.18 `PlayTTS` — TTS 음성 재생

```json
{
  "nodeType": "PlayTTS",
  "identifier": "play_doctor_speech",
  "nextIdentifier": "next",
  "transcriptIdentifier": "doctor_intro_speech",
  "variables": {
    "patientName": "홍길동"
  },
  "waitUntilFinished": true,
  "ttsVoiceIdentifier": "voice_doctor_female"
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `transcriptIdentifier` | `string` | — | `TTSService`에 등록된 스크립트 식별자 |
| `variables` | `Dictionary<string, string>` | `{}` | 동적 세그먼트 오버라이드 (키: 변수명, 값: 실제 값) |
| `waitUntilFinished` | `bool` | `true` | true이면 음성 재생 완료 후 다음 노드 진행 |
| `ttsVoiceIdentifier` | `string` | `null` | 사용할 TTS 목소리 프로파일 식별자 |

---

### 3.19 `PlayerTag` — 플레이어 태그 조작

```json
{
  "nodeType": "PlayerTag",
  "identifier": "assign_role_a",
  "nextIdentifier": "next",
  "operation": "Add",
  "scope": "Current",
  "tag": "role_a",
  "fromTag": null,
  "toTag": null,
  "swapTagA": null,
  "swapTagB": null
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `operation` | `ScenarioPlayerTagOperationType` | `Add` | `Add` / `Remove` / `Change` / `Swap` |
| `scope` | `ScenarioPlayerTagScope` | `Current` | `All`(전체) / `Current`(시나리오 소유자) / `ByTag`(특정 태그 보유자) |
| `tag` | `string` | `null` | `Add`/`Remove` 시 대상 태그 |
| `fromTag` | `string` | `null` | `Change` 시 교체 대상 원래 태그 |
| `toTag` | `string` | `null` | `Change` 시 새 태그 |
| `swapTagA` | `string` | `null` | `Swap` 시 교환 대상 A 태그 |
| `swapTagB` | `string` | `null` | `Swap` 시 교환 대상 B 태그 |

---

### 3.20 `EntityPresetSpawn` — 엔티티 프리셋 스폰

```json
{
  "nodeType": "EntityPresetSpawn",
  "identifier": "spawn_patient_a",
  "nextIdentifier": "next",
  "presetIdentifier": "patient_type_a_preset",
  "spawnedEntityIdentifier": "patient_a",
  "positionSourceEntityIdentifier": "spawn_point_a",
  "positionX": 0.0,
  "positionY": 0.0,
  "positionZ": 0.0,
  "resultStateKey": "patient_a_instance"
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `presetIdentifier` | `string` | 스폰할 EntityPreset 식별자 |
| `spawnedEntityIdentifier` | `string` | 스폰된 인스턴스에 부여할 엔티티 식별자. 빈 값이면 GUID 자동 부여 |
| `positionSourceEntityIdentifier` | `string` | 스폰 위치를 제공하는 기준 엔티티 식별자 |
| `positionX/Y/Z` | `float` | 직접 스폰 좌표 (positionSourceEntityIdentifier 미지정 시 사용) |
| `resultStateKey` | `string` | 스폰된 인스턴스 식별자를 기록할 상태 저장소 키 |

---

### 3.21 `EntityTag` — 엔티티 태그 조작

`PlayerTag`와 유사하지만 엔티티를 대상으로 합니다.

```json
{
  "nodeType": "EntityTag",
  "identifier": "tag_patient_critical",
  "nextIdentifier": "next",
  "operation": "Add",
  "targetEntityIdentifier": "patient_a",
  "targetEntityStateKey": null,
  "tag": "critical_state",
  "fromTag": null,
  "toTag": null
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `targetEntityIdentifier` | `string` | 대상 엔티티 식별자 (직접 지정) |
| `targetEntityStateKey` | `string` | 상태 저장소에서 대상 식별자를 조회할 키 (간접) |
| `operation` | `ScenarioPlayerTagOperationType` | `Add` / `Remove` / `Change` / `Swap` |
| `tag` / `fromTag` / `toTag` | `string` | PlayerTag와 동일 |

---

### 3.22 `EntityInit` — 엔티티 초기 상태 설정

```json
{
  "nodeType": "EntityInit",
  "identifier": "init_patient_a",
  "nextIdentifier": "next",
  "presetIdentifier": null,
  "targetEntityIdentifier": "patient_a",
  "targetEntityStateKey": null,
  "entityIdentifier": null,
  "resultStateKey": null,
  "stateOperations": [
    {
      "kind": "DisplayState",
      "key": "CervicalCollarOnNeck",
      "displayActive": false
    },
    {
      "kind": "StateStore",
      "key": "admission_status",
      "value": "arrived"
    }
  ]
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `presetIdentifier` | `string` | 스폰할 프리셋 식별자 (지정 시 프리셋 스폰 우선) |
| `targetEntityIdentifier` | `string` | 기존 엔티티 식별자 직접 지정 |
| `targetEntityStateKey` | `string` | 상태 저장소 키로 간접 조회 |
| `entityIdentifier` | `string` | 이후 참조용 결과 식별자 |
| `resultStateKey` | `string` | 결과 식별자를 기록할 상태 저장소 키 |
| `stateOperations` | `List<ScenarioEntityStateOperation>` | 적용할 초기 상태 목록 |

**`ScenarioEntityStateOperation` 필드:**

| 필드 | 타입 | 설명 |
|---|---|---|
| `kind` | `ScenarioEntityStateOperationKind` | `StateStore`(상태 저장소 기록) / `DisplayState`(표시 상태 제어) |
| `key` | `string` | StateStore: 상태 키 / DisplayState: 표시 상태 이름 |
| `value` | `string` | StateStore: 저장할 값 (DisplayState에서는 무시) |
| `displayActive` | `bool` | DisplayState: 표시(true)/비표시(false). 기본값 true |

---

### 3.23 `TriageAssessControl` — 트리아지 평가 제어

```json
{
  "nodeType": "TriageAssessControl",
  "identifier": "enable_triage_patient_a",
  "nextIdentifier": "next",
  "targetEntityIdentifier": "patient_a",
  "assessable": true
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `targetEntityIdentifier` | `string` | 트리아지 평가 대상 환자 엔티티 식별자 |
| `assessable` | `bool` | true: 트리아지 평가 활성화 / false: 비활성화 |

**조건:** 대상 엔티티가 `IScenarioTriageAssessTarget`을 구현해야 합니다.

---

### 3.24 `PatientMedicalStatePreset` — 환자 의료 상태 일괄 설정

환자의 의료 상태를 시나리오 중에 변경합니다. `null`인 필드는 현재 값을 유지합니다.

```json
{
  "nodeType": "PatientMedicalStatePreset",
  "identifier": "set_patient_critical",
  "nextIdentifier": "next",
  "targetEntityIdentifier": "patient_a",
  "transitionMode": "Gradual",
  "transitionDurationSeconds": 10.0,
  "consciousnessGcs": 6,
  "respirationAwRR": -1,
  "pulseRate": 120,
  "bloodPressureSystolic": 90,
  "bloodPressureDiastolic": 60,
  "isCardiacArrest": false
}
```

**프리셋 가능 필드 (null이면 현재 값 유지, -1이면 측정 불가):**

| 필드 | 타입 | 설명 |
|---|---|---|
| `targetEntityIdentifier` | `string` | 대상 환자 엔티티 식별자 |
| `targetEntityStateKey` | `string` | 상태 저장소로 간접 조회 |
| `transitionMode` | `PatientMedicalStateTransitionMode` | `Immediate`(즉시) / `Gradual`(점진적 보간) |
| `transitionDurationSeconds` | `float` | `Gradual` 시 보간 시간(초) |
| `name` | `string` | 환자 이름 |
| `sex` | `Sex?` | `Male` / `Female` |
| `age` | `int?` | 나이 |
| `bloodType` | `BloodType?` | 혈액형 |
| `intendedTriage` | `TriageLevel?` | 의도된 트리아지 등급 |
| `consciousnessGcs` | `int?` | GCS 점수 (3~15, -1: 측정 불가) |
| `consciousnessEyeOpening` | `EyeOpeningResponse?` | GCS E 항목 |
| `consciousnessVerbal` | `VerbalResponse?` | GCS V 항목 |
| `consciousnessMotor` | `MotorResponse?` | GCS M 항목 |
| `consciousnessLocLabel` | `LOCLabel?` | 의식수준 5단계 |
| `consciousnessPupillaryResponse` | `PupillaryResponse?` | 동공 반사 상태 |
| `respirationAwRR` | `int?` | 분당 호흡수 (-1: 호흡 없음) |
| `respirationTypeValue` | `RespirationType?` | 호흡 유형 |
| `pulseRate` | `int?` | 분당 맥박수 (-1: 맥박 없음) |
| `pulseForceType` | `BloodPulseForceType?` | 맥박 세기 유형 |
| `bloodPressureSystolic` | `int?` | 수축기 혈압(mmHg, -1: 측정 불가) |
| `bloodPressureDiastolic` | `int?` | 이완기 혈압(mmHg, -1: 측정 불가) |
| `skinColorHue` | `SkinColorHue?` | 피부 색조 |
| `skinTemperatureType` | `SkinTemperatureType?` | 피부 표면 온도 유형 |
| `isCardiacArrest` | `bool?` | 심정지 여부 |

---

### 3.25 `ItemSubmissionConfig` — 아이템 제출 Interactable 설정

```json
{
  "nodeType": "ItemSubmissionConfig",
  "identifier": "config_item_submission",
  "nextIdentifier": "next",
  "presetIdentifier": null,
  "spawnedEntityIdentifier": null,
  "positionSourceEntityIdentifier": "doctor_npc",
  "targetIdentifier": "doctor_submission_point",
  "requiredItems": [
    { "itemIdentifier": "intravenous_set", "count": 1 }
  ],
  "completionSignalIdentifier": "sig.iv_submitted",
  "enabled": true,
  "resultStateKey": null
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `presetIdentifier` | `string` | 스폰할 EntityPreset 식별자 (null이면 기존 참조 방식) |
| `spawnedEntityIdentifier` | `string` | 스폰 시 부여할 식별자 |
| `positionSourceEntityIdentifier` | `string` | 스폰 위치 기준 엔티티 식별자 |
| `targetIdentifier` | `string` | 기존 ItemSubmissionInteractable 식별자 |
| `targetStateKey` | `string` | 상태 저장소로 간접 조회 |
| `requiredItems` | `List<ScenarioItemRequirement>` | 요구 아이템 목록 |
| `completionSignalIdentifier` | `string` | 제출 성공 시 올릴 신호 식별자 |
| `enabled` | `bool` | 활성/비활성 |
| `resultStateKey` | `string` | 결과 식별자를 기록할 상태 저장소 키 |

---

### 3.26 `NpcInteractControl` — NPC Interactable 활성/비활성

```json
{
  "nodeType": "NpcInteractControl",
  "identifier": "enable_doctor_submit",
  "nextIdentifier": "next",
  "npcIdentifier": "doctor_npc",
  "interactableIdentifier": "doctor_submission_interact",
  "operation": "Enable"
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `npcIdentifier` | `string` | — | 대상 NPC 엔티티 식별자 |
| `interactableIdentifier` | `string` | — | 대상 Interactable 식별자 |
| `operation` | `ScenarioNpcInteractControlOperation` | `Add` | `Add`(소스 추가) / `Remove`(소스 제거) / `Enable`(활성화) / `Disable`(비활성화) |

---

### 3.27 `ChatPrint` — 채팅/콘솔 텍스트 출력

```json
{
  "nodeType": "ChatPrint",
  "identifier": "debug_signal",
  "nextIdentifier": "next",
  "message": "[디버그] IV 라인 연결 이벤트 발생",
  "targets": 2,
  "broadcast": false
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `message` | `string` | — | 출력할 메시지 |
| `targets` | `ScenarioChatPrintTarget` (플래그) | `InGameChat(2)` | `UnityConsole(1)` / `InGameChat(2)` / 조합 가능 |
| `broadcast` | `bool` | `false` | true이면 서버가 전체 클라이언트에게 브로드캐스트 |

---

### 3.28 `ExecuteCommand` — 인게임 커맨드 실행

```json
{
  "nodeType": "ExecuteCommand",
  "identifier": "give_all_players_gauze",
  "nextIdentifier": "next",
  "commandLine": "give @a item:gauze"
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `commandLine` | `string` | 실행할 커맨드. 선행 `/` 없어도 됨. 대상 선택자(`@a`, `@s` 등)와 파이프라인(`|`) 지원 |

**동작:** 서버(또는 오프라인) 컨텍스트에서 시스템 권한으로 실행됩니다.

---

### 3.29 `TimeControl` — HUD 타이머 제어

```json
{
  "nodeType": "TimeControl",
  "identifier": "start_countdown",
  "nextIdentifier": "next",
  "operation": "Create",
  "timerId": "main_timer",
  "direction": "Countdown",
  "durationSeconds": 300.0,
  "startSeconds": 0.0
}
```

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `operation` | `ScenarioTimeOperationType` | `Create` | 타이머에 가할 연산 |
| `timerId` | `string` | — | 대상 타이머 식별자 (`Hide` 연산 제외 모두 필수) |
| `direction` | `ScenarioTimeDirection` | `Stopwatch` | `Stopwatch`(정방향) / `Countdown`(역방향) |
| `durationSeconds` | `float` | `0` | `Create`: 카운트다운 목표 시간(초) / `Set`: 카운트다운 목표 재설정 |
| `startSeconds` | `float` | `0` | `Create`: 시작 표시값 / `Set`: 설정할 현재 표시값 |

**`ScenarioTimeOperationType` 값:**

| 값 | 사용 파라미터 | 설명 |
|---|---|---|
| `Create` | timerId, direction, durationSeconds, startSeconds | 타이머 생성/재설정 (정지 상태, 미표시) |
| `Start` | timerId | 타이머 흐름 시작 |
| `Pause` | timerId | 타이머 일시정지 |
| `Resume` | timerId | 일시정지 해제 |
| `Stop` | timerId | 정지 및 시작값으로 초기화 |
| `Set` | timerId, startSeconds, durationSeconds | 현재 표시값 절대 설정 |
| `Show` | timerId | HUD에 표시 (최대 1개, 기존 교체) |
| `Hide` | 없음 | HUD 표시 끄기 |
| `Remove` | timerId | 타이머 삭제 |

**주의:** 카운트다운이 0에 도달해도 자동 숨김이 없습니다. `Hide` 또는 `Remove` 노드를 별도로 배치해야 합니다.

---

## 4. 관련 문서

- [ScenarioController API 레퍼런스](./MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [ScenarioEventIdentifierRegistry API 레퍼런스](./MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)
- [ScenarioGraph 작성 가이드](../working-guide/features/scenario/)
- [scenario-graph-spec.md](../requirements/content-definitions/scenario/scenario-graph-spec.md)
