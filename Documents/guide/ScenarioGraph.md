---
title: "ScenarioGraph"
domain: "content-definitions"
progress: "3-implemented"
flags: []
---

# ScenarioGraph

시나리오 그래프는 인게임 환경에서 대화, 컷씬, 퀘스트 발행, 환자/NPC 상태 설정 등 인게임 시나리오 흐름을 재생하는 데 필요한 컨트롤을 데이터 값으로 정의할 수 있도록 설계된 시스템 모듈입니다.  

이 시스템은 `.scenario.json` 포맷으로 데이터를 입출력할 수 있고, `.editor.scenario.json` 포맷을 추가적으로 사용할 수 있습니다. `.editor.scenario.json`은 에디터 전용이므로 빌드에 포함할 필요는 없습니다.  

## 개요

- 코드 위치: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Models/ScenarioGraphNodes/`
- 실행기: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`
- JSON 직렬화: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/`
- 모든 노드는 공통으로 `IScenarioNode` 인터페이스를 구현하며, 아래 3개 필드를 갖습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `identifier` | `string` | 노드의 고유 식별자 | `intro-dialogue-01` |
| `nodeType` | `string` | 노드 종류를 나타내는 문자열(JSON 역직렬화 판별 키) | `Dialogue` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자. 마지막 노드이거나 다른 방식(옵션/조건)으로 분기하는 노드는 `null`일 수 있다 | `next-node-identifier` |

JSON 상에서 그래프는 `nodes`라는 `Dictionary<string, Node>` 형태로 저장되며, 각 노드의 딕셔너리 키는 해당 노드의 `identifier`와 일치해야 합니다. 그래프에는 시작 노드를 명시하는 필드가 없으며, 시작 노드는 `ScenarioController.StartScenario(graph, startNodeIdentifier, ...)` 호출 시 외부에서 지정합니다(미지정 시 `Nodes` 딕셔너리에서 임의의 첫 항목이 선택되는 임시 로직이 존재하므로, 실제 사용 시 시작 노드 식별자를 항상 명시적으로 지정해야 합니다).

### 다이얼로그류 노드의 공통 입력 규칙

`Dialogue`, `Choice`, `Quiz` 노드처럼 화면에 대사창을 표시하는 노드는 아래와 같은 공통 입력 규칙을 따릅니다(`PlayerController.Input.cs`, `DialoguePanelUIController.cs` 참고).

- 텍스트가 타이핑 애니메이션 중일 때 입력(마우스 좌클릭, `Space`, `F`)을 주면 타이핑 애니메이션만 즉시 완료되며 다음 노드로는 진행하지 않습니다.
- 텍스트 타이핑이 끝난 상태에서 동일한 입력을 다시 주면 다음 노드로 진행하거나(Dialogue), 현재 강조된 선택지를 확정합니다(Choice/Quiz).
- 선택지가 있는 노드(Choice/Quiz)에서 강조된 옵션을 바꾸는 것은 마우스 스크롤 휠 또는 `-`/`+`(키패드 포함) 키로 수행하며, 숫자 키를 이용한 직접 선택은 지원하지 않습니다.

## ScenarioGraph

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `identifier` | `string` | 시나리오 그래프 자신의 식별자 | `disaster-intro` |
| `tags` | `string[]` | 그래프에서 사용할 태그 사전 선언 목록. 선언되지 않은 태그가 노드/브랜치에서 사용되면 로딩 시 경고가 출력된다(옵션) | `["nurse", "doctor"]` |
| `questDefinitionIncludes` | `string[]` | 미리 로드할 퀘스트 정의 파일명 목록(`Resources/Quest/*.quest.json`) | `["main-quest.quest.json"]` |
| `nodes` | `Dictionary<string, Node>` | 노드 식별자를 키로 하는 노드 맵 | - |

---

### Dialogue

플레이어에게 대화 다이얼로그를 재생합니다. 다이얼로그는 마우스 클릭, `Space` 또는 `F` 키 입력으로 타이핑 연출을 스킵하거나 다음 다이얼로그로 진행할 수 있습니다. `autoAdvanceSeconds`가 지정되면 입력 없이도 시간 경과 후 자동으로 다음 노드로 진행합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `speakerName` | `string` | 대사를 말하는 캐릭터의 표시 이름 | `김철수` |
| `dialogueContent` | `string` | 표시할 대사 내용 | `여기는 위험합니다. 서둘러 이동하세요.` |
| `portraitSpriteIdentifier` | `string` | (optional) 대화 중 표시할 캐릭터 초상화 스프라이트 식별자 | `portrait_kim` |
| `interactionRequired` | `bool` | 참이면 이 다이얼로그가 월드 상호작용에 종속된 팝업으로 취급되어, 사용자 입력으로만 닫힌다(자동 진행 없음) | `false` |
| `autoAdvanceSeconds` | `float` (nullable) | 자동 진행까지 대기할 시간(초). `null` 또는 0 이하이면 사용자 입력을 기다린다(기본값). 값이 있으면 표시 후 해당 시간이 지나면 자동으로 다음 노드로 진행하며, 그 전에 사용자 입력이 오면 즉시 진행하고 타이머는 취소된다 | `3.0` |
| `playTTS` | `bool` | 참이면 `dialogueContent`를 표시할 때 TTS로 함께 재생합니다. 변수(`{...}`)를 포함하지 않는 콘텐츠는 에디터에서 사전 합성(bake)될 수 있으며, bake되지 않은 경우 런타임에 즉석 합성됩니다(optional, 기본 false) | `true` |
| `ttsVoiceIdentifier` | `string` | 사용할 목소리 프로파일 식별자입니다. `null`/빈 문자열이면 TTSService의 기본 목소리를 사용합니다. `playTTS`가 true일 때만 효과가 있습니다(optional) | `nurse-voice-01` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Choice

플레이어에게 선택 가능한 분기 대화를 제시합니다. 각 선택지를 고르면 `options` 목록에서 해당 옵션의 `nextNodeIdentifier`로 이동합니다. 선택지가 있는 노드이므로 자체 `nextIdentifier`는 사용하지 않으며(`null`이어야 함), 옵션 강조는 마우스 스크롤 또는 `-`/`+` 키로, 확정은 마우스 클릭/`Space`/`F` 키로 수행합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `speakerName` | `string` | 대사를 말하는 캐릭터의 표시 이름 | `간호사` |
| `dialogueContent` | `string` | 선택지를 제시하기 위한 대사 내용 | `어떤 처치를 먼저 하시겠습니까?` |
| `portraitSpriteIdentifier` | `string` | (optional) 대화 중 표시할 캐릭터 초상화 스프라이트 식별자 | `portrait_nurse` |
| `options` | `ScenarioChoiceOption[]` | 선택지 목록(하단 참고) | - |
| `playTTS` | `bool` | 참이면 `dialogueContent`를 표시할 때 TTS로 함께 재생합니다. 세부 동작은 Dialogue의 `playTTS`와 동일(optional, 기본 false) | `false` |
| `ttsVoiceIdentifier` | `string` | 사용할 목소리 프로파일 식별자(optional) | `nurse-voice-01` |
| `nextIdentifier` | `string` | 스키마 규칙상 반드시 `null`이어야 한다(옵션을 통해서만 분기) | `null` |

#### ScenarioChoiceOption

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `displayText` | `string` | 선택지에 표시될 텍스트 | `지혈부터 시작한다` |
| `displayIconIdentifier` | `string` | (optional) 선택지에 표시될 아이콘 식별자 | `icon_bandage` |
| `displayColor` | `{r,g,b,a}` | 선택지 표시 색상 | `{"r":1,"g":1,"b":1,"a":1}` |
| `nextNodeIdentifier` | `string` | 이 선택지를 고를 때 이동할 다음 노드 식별자 | `treat-bleeding` |

### Sound

지정한 사운드 리소스를 1회 재생합니다. `waitUntilFinished`가 참이면 재생이 끝날 때까지 다음 노드로 진행하지 않고 대기합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `soundResourceIdentifier` | `string` | 재생할 사운드 리소스 식별자(`Resources/Sound/<id>` 또는 `Resources/<id>`에서 조회) | `explosion_01` |
| `waitUntilFinished` | `bool` | 재생 완료까지 다음 노드 진행을 대기할지 여부 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PlayerMove

로컬 플레이어 엔티티를 지정한 목적지로 이동시킵니다. 목적지는 좌표 또는 사전 정의된 웨이포인트로 지정할 수 있고, 이동 방식(즉시/속도 기반/시간 기반)을 선택할 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `destinationType` | `string` (`Position`\|`Waypoint`) | 목적지 지정 방식 | `Waypoint` |
| `destinationIdentifier` | `string` | (`destinationType`이 `Waypoint`일 때) 목적지 웨이포인트 식별자 | `wp_entrance` |
| `destinationX` / `destinationY` / `destinationZ` | `float` | (`destinationType`이 `Position`일 때) 목적지 좌표 | `10.0` |
| `ignoreGroundCheck` | `bool` | 지면 체크(스냅)를 무시할지 여부 | `false` |
| `moveMode` | `string` (`Instant`\|`BySpeed`\|`ByDuration`) | 이동 방식. `Instant`=즉시 이동, `BySpeed`=지정 속도로 이동(기본값), `ByDuration`=지정 시간 동안 이동 | `BySpeed` |
| `moveSpeed` | `float` | (`moveMode`가 `BySpeed`일 때) 이동 속도 | `3.5` |
| `moveDuration` | `float` | (`moveMode`가 `ByDuration`일 때) 이동에 걸리는 시간(초) | `2.0` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### NPCMove

지정한 NPC 엔티티를 목적지로 이동시킵니다. `PlayerMove`와 필드 구성이 동일하며, 대상이 NPC로 바뀐 점만 다릅니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `npcIdentifier` | `string` | 이동할 NPC의 식별자 | `npc_doctor` |
| `destinationType` | `string` (`Position`\|`Waypoint`) | 목적지 지정 방식 | `Position` |
| `destinationIdentifier` | `string` | (`Waypoint`일 때) 목적지 웨이포인트 식별자 | `wp_bed_a` |
| `destinationX` / `destinationY` / `destinationZ` | `float` | (`Position`일 때) 목적지 좌표 | `5.0` |
| `ignoreGroundCheck` | `bool` | 지면 체크를 무시할지 여부 | `false` |
| `moveMode` | `string` (`Instant`\|`BySpeed`\|`ByDuration`) | 이동 방식(기본값 `BySpeed`) | `Instant` |
| `moveSpeed` | `float` | (`BySpeed`일 때) 이동 속도 | `2.0` |
| `moveDuration` | `float` | (`ByDuration`일 때) 이동 시간(초) | `1.5` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### CameraTarget

플레이어 카메라의 초점 대상을 변경합니다. 컷씬 연출 등에서 특정 오브젝트를 비추도록 사용합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetObjectIdentifier` | `string` | 카메라가 바라볼 대상 오브젝트의 식별자 | `npc_doctor` |
| `offsetX` / `offsetY` / `offsetZ` | `float` | 대상 기준 오프셋(기본값 0) | `0.0` |
| `blendTime` | `float` | 대상 전환 블렌드 시간(초, 기본값 1) | `1.0` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Parallel

여러 브랜치 체인을 동시에 실행합니다. 브랜치를 접속 중인 플레이어들에게 어떻게 분배할지, 완료를 어떻게 기다릴지를 지정할 수 있으며, 다인 협력 처치(예: 여러 간호사가 각자 다른 처치를 동시에 수행)를 표현하는 데 사용합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `branches` | `ScenarioParallelBranch[]` | 동시에 실행할 브랜치 목록(하단 참고) | - |
| `waitMode` | `string` (`All`\|`Any`\|`None`) | 브랜치 완료 감시 정책. `All`=모든 브랜치가 끝나야 진행, `Any`=하나라도 끝나면 진행, `None`=시작 즉시 진행(fire-and-forget) | `All` |
| `allocationType` | `string` (`SelfAll`\|`RandomOneAll`\|`SpreadRandom`\|`SpreadOrdinary`\|`ByRole`) | 브랜치를 플레이어에게 분배하는 방식. `ByRole`은 각 브랜치를 자격(태그)에 맞는 서로 다른 플레이어에게 1:1로 배정한다(다인 동시 협력용) | `ByRole` |
| `whenBranchingPlayerNotMatched` | `string` (`Panic`\|`Ignore`\|`Reallocation`) | 플레이어 수와 브랜치 수가 일치하지 않을 때의 처리 방식 | `Panic` |
| `nextIdentifier` | `string` | `waitMode` 조건 충족 후 이동할 다음 노드 식별자 | `next-node-identifier` |

#### ScenarioParallelBranch

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `identifier` | `string` | 브랜치의 시작 노드 식별자 | `branch-vitals` |
| `completionConditionIdentifier` | `string` | 브랜치 완료 조건(수렴 라벨) 식별자. 브랜치 체인의 마지막 노드의 `nextIdentifier`가 이 값을 가리키면 해당 브랜치가 완료된 것으로 간주된다 | `branch-vitals-done` |
| `requiredPlayerTags` | `string[]` | (optional) 이 브랜치 실행 대상이 되기 위해 필요한 플레이어 태그 목록 | `["nurse"]` |
| `forbiddenPlayerTags` | `string[]` | (optional) 이 브랜치 실행 대상에서 제외할 플레이어 태그 목록 | `["doctor"]` |
| `requiredPlayerTagsMatchMode` | `string` (`All`\|`Any`) | `requiredPlayerTags` 매칭 모드(기본값 `All`) | `All` |

### ServerInternalSignal

서버 권위 하에 동작하는 IPC 유사 내부 신호를 등록(register)하거나 발생(resolve)시킵니다. 한 플레이어의 시나리오 흐름이 다른 플레이어(또는 서버 이벤트)의 상태 변화에 의존할 때 사용합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetIdentifier` | `string` | 신호의 목적지. `@m`은 서버 권위 대상(기본값), `@s`는 실행자 자신을 뜻한다 | `@m` |
| `signalIdentifier` | `string` | 신호 이름 | `sig.patient-a-triaged` |
| `operation` | `string` (`Register`\|`Resolve`) | `Register`=신호를 기다린다(선등록 시 이후 발생하는 동일 신호에 즉시 resolve), `Resolve`=신호를 발생시킨다(선발생 시 이후 등록하는 대기자에게 즉시 resolve) | `Register` |
| `waitForResolution` | `bool` | 참이면 신호가 resolve될 때까지 다음 노드 진행을 막는다(기본값 true) | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### InvokeEvent

이벤트 식별자를 통해 게임 코드에 정의된 커스텀 이벤트 핸들러(예: TriageTrainer의 `TriageScenarioEventBootstrap.Event.*`)를 호출합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `eventIdentifier` | `string` | 호출할 이벤트의 식별자 | `patient-a.spawn` |
| `moveNextBehavior` | `string` (`False`\|`Immediately`\|`WaitUntilDone`) | 이벤트 호출 후 다음 노드로 진행하는 시점. `False`=자동으로 진행하지 않음, `Immediately`=이벤트를 발동시킨 직후 즉시 진행, `WaitUntilDone`=핸들러가 완료될 때까지 대기(기본값) | `WaitUntilDone` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Validator

조건(플레이어 수, 태그 보유 여부, 레지스트리 상태 등)을 검사하여 통과/실패에 따라 진행을 분기하거나, 조건이 충족될 때까지 대기하는 게이트로 동작합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `rootConditions` | `ScenarioValidatorRootCondition[]` | 평가할 조건 목록(하단 참고) | - |
| `onFailure` | `string` (`Panic`\|`Branching`\|`Ignore`) | 조건 실패 시 동작. `Panic`=오류 처리, `Branching`=`failureNextIdentifier`로 분기, `Ignore`=무시하고 계속 진행 | `Branching` |
| `failureReportTargets` | `string` (flags: `UnityConsole`\|`InGameChat`) | 실패 보고 대상(플래그 조합 가능) | `UnityConsole` |
| `failureNextIdentifier` | `string` | (`onFailure`가 `Branching`일 때) 실패 시 이동할 노드 식별자 | `validator-fail-branch` |
| `waitForCondition` | `bool` | 참이면 조건이 충족될 때까지 진행을 막고 폴링 대기하는 게이트로 동작합니다. 미지정/false이면 1회만 평가하고 `onFailure` 정책을 따릅니다(하위호환, 기본값 false) | `true` |
| `waitTimeoutSeconds` | `float` (nullable) | `waitForCondition` 게이트의 타임아웃(초). `null`/0 이하면 타임아웃 없이 무한 대기한다(기본값) | `30.0` |
| `onWaitTimeout` | `string` (`KeepWaiting`\|`FailBranch`\|`ForceAdvance`\|`WarnAndKeepWaiting`) | `waitTimeoutSeconds` 초과 시 동작. `KeepWaiting`=계속 대기(기본값), `FailBranch`=`failureNextIdentifier`로 분기, `ForceAdvance`=`nextIdentifier`로 강제 진행, `WarnAndKeepWaiting`=경고 후 계속 대기 | `KeepWaiting` |
| `nextIdentifier` | `string` | 조건 충족(또는 `Ignore`/`ForceAdvance`) 시 이동할 다음 노드 식별자 | `next-node-identifier` |

#### ScenarioValidatorRootCondition

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `condition` | `string` (`PlayerCountEqual` 등, 하단 참고) | 평가할 조건 종류 | `PlayerAssignedTag` |
| `targetCount` | `int` | 비교 대상 수(PlayerCount* 조건에서 사용) | `2` |
| `playerTag` | `string` | 비교할 플레이어 태그(`PlayerAssignedTag` 조건에서 사용) | `nurse` |
| `playerScope` | `string` (`Any`\|`All`\|`Owner`) | 조건을 검사할 플레이어 범위(기본값 `Any`) | `Any` |
| `validationRules` | `ScenarioValidatorRule[]` | (`RegistryContains` 조건에서 사용) 레지스트리 검증 규칙 목록 | - |

조건(`condition`) 종류: `PlayerCountEqual`, `PlayerCountNotEqual`, `PlayerCountLessThan`, `PlayerCountLessThanOrEqual`, `PlayerCountGreaterThan`, `PlayerCountGreaterThanOrEqual`, `RegistryContains`, `PlayerAssignedTag`

#### ScenarioValidatorRule

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `type` | `string` (`Registry`) | 규칙 종류 | `Registry` |
| `condition` | `string` (`Contains`) | 규칙 평가 방식 | `Contains` |
| `registryType` | `string` | 조회할 레지스트리 종류(예: `Waypoint`) | `Waypoint` |
| `registryIdentifier` | `string` | 조회할 레지스트리 항목 식별자 | `wp_entrance` |

### QuestControl

퀘스트를 추가/갱신/삭제합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `operation` | `string` (`Add`\|`Update`\|`Remove`) | 퀘스트 조작 종류 | `Add` |
| `failureStrategy` | `string` (`Overwrite`\|`Ignore`\|`Panic`) | 충돌(이미 존재하는 퀘스트 등) 처리 방식(기본값 `Overwrite`) | `Overwrite` |
| `questDefinitionIdentifier` | `string` | 참조할 퀘스트 정의 식별자(`.quest.json`) | `main-quest` |
| `quest` | `QuestData` | 인라인 퀘스트 데이터(제목/설명/내용/추적 여부 등). `questDefinitionIdentifier`와 함께 또는 대신 사용 가능 | - |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### QuestWaypointHighlight

퀘스트 진행 안내용 웨이포인트 마커를 강조 표시합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `waypointIdentifier` | `string` | 강조할 웨이포인트 식별자 | `wp_entrance` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Delay

지정한 시간만큼 대기합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `durationSeconds` | `float` | 대기할 시간(초) | `2.0` |
| `waitUntil` | `string` (`Immediately`\|`WaitUntilDone`) | `Immediately`=대기를 시작만 하고 즉시 다음 노드로 진행, `WaitUntilDone`=대기가 끝날 때까지 다음 노드 진행을 막음(기본값) | `WaitUntilDone` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Interaction

플레이어(또는 임의의 액터)가 특정 대상과 상호작용을 완료해야 다음으로 진행되는 노드입니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `actorScope` | `string` (`Player`\|`Any`) | 상호작용을 수행해야 하는 주체 범위(기본값 `Player`) | `Player` |
| `targetIdentifier` | `string` | 상호작용 대상의 식별자 | `door_01` |
| `requiredItemIdentifier` | `string` | (optional) 상호작용에 필요한 아이템 식별자 | `keycard` |
| `interactionType` | `string` (`Use`\|`Inspect`\|`Attach`\|`Detach`) | 상호작용 종류(기본값 `Use`) | `Use` |
| `completionConditionIdentifier` | `string` | (optional) 완료 신호/이벤트 식별자 | `door-opened` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### CombineItem

여러 입력 아이템을 하나의 결과 아이템으로 조합합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `inputItemIdentifiers` | `string[]` | 조합에 필요한 입력 아이템 식별자 목록 | `["gauze", "bandage"]` |
| `outputItemIdentifier` | `string` | 조합 결과 아이템 식별자 | `wound_dressing_kit` |
| `autoCombine` | `bool` | 참이면 수동 조작 없이 자동으로 조합된다 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Quiz

객관식 퀴즈를 제시하고 정답/오답에 따라 분기합니다. 선택 입력 방식은 Choice 노드와 동일합니다(스크롤/`±` 키로 강조, 클릭/`Space`/`F`로 확정).

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `question` | `string` | 문제 문항 | `이 환자의 KTAS 등급은?` |
| `options` | `string[]` | 객관식 보기 목록 | `["1등급", "2등급", "3등급"]` |
| `correctIndex` | `int` | 정답 인덱스(0-base) | `1` |
| `onCorrectNextIdentifier` | `string` | 정답 선택 시 다음 노드 식별자 | `quiz-correct-feedback` |
| `onIncorrectNextIdentifier` | `string` | (optional) 오답 선택 시 다음 노드 식별자(예: 재시도 루프) | `quiz-retry` |
| `feedbackCorrect` | `string` | (optional) 정답 시 표시할 피드백 텍스트 | `정답입니다!` |
| `feedbackIncorrect` | `string` | (optional) 오답 시 표시할 피드백 텍스트 | `다시 확인해보세요.` |
| `playTTS` | `bool` | 참이면 문항(및 피드백)을 표시할 때 TTS로 함께 재생한다(optional, 기본 false) | `false` |
| `ttsVoiceIdentifier` | `string` | 사용할 목소리 프로파일 식별자(optional) | `doctor-voice-01` |

### StateUpdate

시나리오 상태 저장소(`_stateStore`)에 임의의 키-값을 기록합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetEntityIdentifier` | `string` | 상태를 갱신할 대상(엔티티) 식별자 | `patient_a` |
| `stateKey` | `string` | 상태 키(예: `vitals.rhythm`) | `vitals.rhythm` |
| `stateValue` | `string` | 저장할 상태 값 | `PEA` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PlayTTS

대화창 없이 독립적으로 TTS 대본을 재생합니다. 변수 치환을 지원합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `transcriptIdentifier` | `string` | `TTSService`에 등록된 대본(Transcript) 식별자 | `triage-move-patient` |
| `variables` | `Dictionary<string,string>` | (optional) 대본 내 동적 세그먼트 변수 오버라이드. 필요하지만 값이 없는 변수는 경고 후 해당 세그먼트를 건너뜀 | `{"patient-name":"김철수"}` |
| `waitUntilFinished` | `bool` | 참이면 모든 클립 재생이 끝날 때까지 다음 노드 진행을 대기한다(기본값 true) | `true` |
| `ttsVoiceIdentifier` | `string` | (optional) 사용할 목소리 프로파일 식별자. 미지정 시 기본 목소리 사용 | `doctor-voice-01` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PlayerTag (JSON `nodeType`: `TagModification`, 구버전 호환 별칭 `PlayerTag`)

플레이어의 태그를 추가/제거/변경/교환합니다. 다인 플레이 시 역할(간호사/의사 등) 배정에 사용됩니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `operation` | `string` (`Add`\|`Remove`\|`Change`\|`Swap`) | 태그 조작 종류. `Add`=태그 추가, `Remove`=태그 제거, `Change`=`fromTag`를 `toTag`로 교체, `Swap`=`swapTagA`/`swapTagB` 보유자 간 태그 교환(역할 교대) | `Add` |
| `scope` | `string` (`All`\|`Current`\|`ByTag`) | 대상 플레이어 범위. `All`=현재 접속한 모든 플레이어, `Current`=시나리오를 실행 중인 플레이어(기본값), `ByTag`=특정 태그 보유 플레이어만(Add/Remove 한정) | `Current` |
| `tag` | `string` | `Add`/`Remove` 시 사용할 태그 값 | `nurse` |
| `fromTag` | `string` | `Change` 시 교체 전 태그 | `nurse-candidate` |
| `toTag` | `string` | `Change` 시 교체 후 태그 | `nurse` |
| `swapTagA` | `string` | `Swap` 시 교환 대상 A 태그(A 보유자가 B를 갖게 됨) | `nurse` |
| `swapTagB` | `string` | `Swap` 시 교환 대상 B 태그(B 보유자가 A를 갖게 됨) | `doctor` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### EntityPresetSpawn

등록된 엔티티 프리셋을 식별자로 스폰합니다. 하위 오브젝트 구성은 프리셋 정의에 사전 설정되어 있으므로 이 노드는 재정의하지 않습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `presetIdentifier` | `string` | 스폰할 엔티티 프리셋 식별자 | `patient_preset_a` |
| `spawnedEntityIdentifier` | `string` | 스폰된 루트 인스턴스에 부여할 엔티티 식별자. 비어 있으면 GUID 기반 식별자가 자동 부여된다 | `patient_a` |
| `positionSourceEntityIdentifier` | `string` | (optional) 다른 엔티티의 위치를 기준으로 스폰할 때 그 엔티티의 식별자 | `bed_a` |
| `positionX` / `positionY` / `positionZ` | `float` | 스폰 좌표(`positionSourceEntityIdentifier`가 없을 때 사용) | `0.0` |
| `resultStateKey` | `string` | (optional) 결과로 생성된 엔티티 식별자를 상태 저장소에 기록할 키 | `patient_a.entityId` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### EntityTag

플레이어가 아닌 엔티티에 대해 태그 조작을 수행합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `operation` | `string` (`Add`\|`Remove`\|`Change`) | 태그 조작 종류 | `Add` |
| `targetEntityIdentifier` | `string` | 대상 엔티티 식별자(직접 지정) | `patient_a` |
| `targetEntityStateKey` | `string` | (optional) 상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키. `targetEntityIdentifier`가 비어 있을 때 사용 | `patient_a.entityId` |
| `tag` | `string` | `Add`/`Remove` 시 사용할 태그 값 | `critical` |
| `fromTag` | `string` | `Change` 시 교체 전 태그 | `stable` |
| `toTag` | `string` | `Change` 시 교체 후 태그 | `critical` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### EntityInit

엔티티를 준비(스폰 또는 기존 엔티티 참조)하고 초기 상태를 설정합니다. 주 용도는 환자에게 부착된 처치 부착물(주사기, 거즈, 경부보호대 등)의 초기 표시 여부 설정입니다. `presetIdentifier`가 지정되면 스폰이 기존 엔티티 참조보다 우선합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `presetIdentifier` | `string` | (optional) 스폰할 엔티티 프리셋 식별자. 지정되면 새 인스턴스를 스폰한다 | `patient_preset_b` |
| `positionSourceEntityIdentifier` | `string` | (optional) 스폰 위치 기준이 되는 기존 엔티티 식별자 | `bed_b` |
| `positionX` / `positionY` / `positionZ` | `float` | 스폰 좌표 | `0.0` |
| `targetEntityIdentifier` | `string` | (프리셋 스폰이 아닐 때) 제어 대상 엔티티 식별자(직접) | `patient_b` |
| `targetEntityStateKey` | `string` | (프리셋 스폰이 아닐 때) 제어 대상 엔티티 식별자를 상태 저장소에서 조회할 키(간접) | `patient_b.entityId` |
| `entityIdentifier` | `string` | 이후 그래프가 이 엔티티를 계속 제어하기 위해 부여/사용할 식별자. 비어 있으면 자동(GUID) 부여 | `patient_b` |
| `resultStateKey` | `string` | (optional) 확정된 대상 엔티티 식별자를 기록할 상태 저장소 키 | `patient_b.entityId` |
| `stateOperations` | `ScenarioEntityStateOperation[]` | 대상 엔티티에 적용할 초기 상태 항목 목록(하단 참고) | - |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

#### ScenarioEntityStateOperation

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `kind` | `string` (`StateStore`\|`DisplayState`) | 항목 종류. `StateStore`=상태 저장소에 `"{entityIdentifier}.{key}" = value` 기록, `DisplayState`=엔티티 컴포넌트의 표시/부착 상태 설정(기본값) | `DisplayState` |
| `key` | `string` | `StateStore`일 때 상태 키, `DisplayState`일 때 표시/부착 상태 이름(예: `CervicalCollarOnNeck`) | `CervicalCollarOnNeck` |
| `value` | `string` | (`StateStore`일 때) 기록할 값. `DisplayState`에서는 사용하지 않음 | `attached` |
| `displayActive` | `bool` | (`DisplayState`일 때) 표시(true)/비표시(false) 여부(기본값 true) | `true` |

### TriageAssessControl

특정 환자(엔티티)에 대해 트리아지(Triage) 평가 인터랙션을 활성화/비활성화합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetEntityIdentifier` | `string` | 트리아지 평가를 제어할 대상 환자(엔티티) 식별자 | `patient_a` |
| `assessable` | `bool` | 참이면 트리아지 평가 인터랙션 활성화, 거짓이면 비활성화 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PatientMedicalStatePreset

환자 엔티티의 의료 상태(환자 기술자 및 의료 상태)를 일괄 초기화/덮어씁니다. 모든 상태 필드는 nullable이며, `null`인 항목은 현재 값을 유지합니다. 수치 필드에 `-1`을 지정하면 "무의식/호흡 없음/측정 불가" 등 값이 존재하지 않는 상태를 의미합니다(이는 "현재 값 유지"를 뜻하는 `null`과는 다른 의미).

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetEntityIdentifier` | `string` | 상태를 초기화할 환자 엔티티 식별자 | `patient_a` |
| `targetEntityStateKey` | `string` | (optional) `targetEntityIdentifier`가 비어 있을 때 상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키 | `patient_a.entityId` |
| `transitionMode` | `string` (`Immediate`\|`Gradual`) | 프리셋 값 적용 방식. `Immediate`=즉시 덮어쓰기(기본값), `Gradual`=`transitionDurationSeconds` 동안 수치 값을 점차 보간(비수치 필드는 종료 시점에 적용, `-1`은 보간 대상 아님) | `Immediate` |
| `transitionDurationSeconds` | `float` | (`Gradual`일 때) 보간 소요 시간(초). 0 이하이면 즉시 적용과 동일 | `0.0` |
| `name` | `string` | 환자 성명. `null`이면 현재 값 유지 | `김철수` |
| `sex` | `string` (`Male`\|`Female`, nullable) | 환자 성별 | `Male` |
| `age` | `int` (nullable) | 환자 나이 | `45` |
| `bloodType` | `string` (nullable) | 환자 혈액형 | `A` |
| `intendedTriage` | `string` (nullable) | 의도된(정답) 트리아지 등급 | `Red` |
| `consciousnessGcs` | `int` (nullable) | GCS 점수(3~15). `-1`이면 무의식(측정 불가) | `15` |
| `consciousnessEyeOpening` | `string` (nullable) | GCS의 E(눈뜨기 반응) 세부 항목(1~4점) | `Spontaneous` |
| `consciousnessVerbal` | `string` (nullable) | GCS의 V(언어 반응) 세부 항목(1~5점) | `Oriented` |
| `consciousnessMotor` | `string` (nullable) | GCS의 M(운동 반응) 세부 항목(1~6점) | `ObeysCommands` |
| `consciousnessLocLabel` | `string` (nullable) | 의식수준 5단계(LOC) 라벨 | `Alert` |
| `consciousnessPupillaryResponse` | `string` (nullable) | 동공 반사 상태 | `Normal` |
| `respirationAwRR` | `int` (nullable) | 분당 호흡수. `-1`이면 호흡 없음(측정 불가) | `18` |
| `respirationTypeValue` | `string` (nullable) | 호흡 유형 | `Normal` |
| `pulseRate` | `int` (nullable) | 분당 맥박수. `-1`이면 맥박 없음(측정 불가) | `80` |
| `pulseForceType` | `string` (nullable) | 맥박 세기 유형 | `Normal` |
| `bloodPressureSystolic` | `int` (nullable) | 수축기 혈압(mmHg). `-1`이면 측정 불가 | `120` |
| `bloodPressureDiastolic` | `int` (nullable) | 이완기 혈압(mmHg). `-1`이면 측정 불가 | `80` |
| `skinColorHue` | `string` (nullable) | 피부 색조 | `Normal` |
| `skinTemperatureType` | `string` (nullable) | 피부 표면 온도 유형 | `Warm` |
| `isCardiacArrest` | `bool` (nullable) | 심정지 여부 | `false` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### ItemSubmissionConfig

아이템 제출(Item Submission) 인터랙터블을 사전 설정합니다. 프리셋을 새로 스폰하거나 기존 인터랙터블을 참조해 요구 아이템, 완료 신호, 활성 여부 등을 오버라이드합니다. 제출이 완료되면 서버 권한 경로로 완료 신호가 올라가며 이를 `Validator` 노드로 게이팅할 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `presetIdentifier` | `string` | (optional) 스폰할 EntityPreset 식별자(ItemSubmissionInteractable 프리팹). 지정 시 프리셋 스폰 경로 사용(서버/오프라인 컨텍스트에서만 수행) | `item_submission_preset` |
| `spawnedEntityIdentifier` | `string` | (optional) 스폰된 인스턴스에 부여할 엔티티 식별자. 비어 있으면 자동 생성 | `submission_a` |
| `positionSourceEntityIdentifier` | `string` | (optional) 스폰 위치 기준이 되는 기존 엔티티 식별자 | `npc_doctor` |
| `positionX` / `positionY` / `positionZ` | `float` | 스폰 좌표 | `0.0` |
| `targetIdentifier` | `string` | (optional) 프리셋을 스폰하지 않고 기존 Interactable을 참조할 때의 식별자 | `submission_a` |
| `targetStateKey` | `string` | (optional) 대상 식별자를 상태 저장소 키에서 해석할 때 사용(예: 이전 스폰 노드의 결과) | `submission_a.entityId` |
| `requiredItems` | `ScenarioItemRequirement[]` | 요구 아이템 목록(식별자+수량). 비어 있으면 프리셋 기본값 유지 | `[{"itemIdentifier":"gauze","count":2}]` |
| `completionSignalIdentifier` | `string` | (optional) 제출 성공 시 올릴 서버 세션 전역 신호 식별자(`sig.` 접두사는 자동 정규화) | `sig.item-submitted` |
| `enabled` | `bool` | 대상 Interactable의 활성/비활성(기본값 true) | `true` |
| `resultStateKey` | `string` | (optional) 확정된 대상 식별자를 기록할 상태 저장소 키 | `submission_a.entityId` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

#### ScenarioItemRequirement

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `itemIdentifier` | `string` | 요구 아이템 식별자 | `gauze` |
| `count` | `int` | 요구 수량(기본값 1) | `2` |

### NpcInteractControl

NPC에 부착된(혹은 참조로 연결할) 인터랙터블을 추가/제거하거나 활성/비활성 전환합니다. 예: 의사 NPC에게 "아이템 제출" 상호작용을 시나리오 진행 시점에 활성화하거나, 시나리오 종료 후 비활성화합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `npcIdentifier` | `string` | 대상 NPC의 레지스트리 식별자 | `npc_doctor` |
| `interactableIdentifier` | `string` | 대상 Interactable의 식별자. `Add` 시 레지스트리에서 해당 식별자의 인터랙트 컴포넌트를 찾아 NPC의 커스텀 소스로 추가하고, `Enable`/`Disable` 시 대상이 토글 가능한 인터랙터블이면 활성 상태를 전환한다 | `submission_a` |
| `operation` | `string` (`Add`\|`Remove`\|`Enable`\|`Disable`) | 수행할 동작. `Add`=추가, `Remove`=제거, `Enable`=활성화, `Disable`=비활성화(기본값 `Add`) | `Add` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |
