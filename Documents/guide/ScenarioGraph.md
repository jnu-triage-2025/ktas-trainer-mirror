---
title: "ScenarioGraph"
domain: "content-definitions"
progress: "3-implemented"
flags: []
---

# ScenarioGraph

시나리오 그래프(ScenarioGraph)는 게임 안에서 벌어지는 하나의 "사건"을, 코드를 새로 짜지 않고도 데이터(JSON)로 표현할 수 있게 해주는 시스템입니다. 대화 한 마디, 환자가 침대에 눕는 장면, 카메라가 특정 인물을 비추는 컷씬, 퀴즈로 정답을 확인하는 순간, 여러 명의 간호사가 동시에 각자 다른 처치를 수행하는 협동 시나리오까지 — 이런 흐름 하나하나가 "노드"라는 작은 블록으로 나뉘고, 그 블록들을 화살표처럼 연결한 것이 시나리오 그래프입니다.

노드는 종류에 따라 역할이 다릅니다. 어떤 노드는 화면에 글자를 띄우고, 어떤 노드는 NPC를 움직이고, 어떤 노드는 환자의 혈압을 바꾸는 식입니다. 이 문서는 현재 프로젝트에 존재하는 모든 노드 종류를 하나씩 소개하며, 각 노드가 어떤 값을 받고 그 값이 무엇을 의미하는지 정리합니다. 시나리오를 새로 작성하거나 기존 시나리오 JSON을 읽고 고칠 때 이 문서를 참고하면 됩니다.

시나리오 데이터는 `.scenario.json` 파일로 저장되고 게임에서 그대로 재생됩니다. 이와 별도로 `.editor.scenario.json`이라는 파일도 있는데, 이는 에디터에서 그래프를 보기 편하게 배치하기 위한 부가 정보(노드 위치 등)만 담고 있어서 실제 게임 빌드에는 포함할 필요가 없습니다.

## 개요

시나리오 그래프와 관련된 코드는 아래 위치에서 찾을 수 있습니다.

- 노드 클래스가 정의된 위치: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Models/ScenarioGraphNodes/`
- 실제로 노드를 하나씩 실행해 나가는 실행기: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`
- JSON ↔ 노드 객체 변환(불러오기/저장하기)을 담당하는 코드: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/`

노드 종류는 25가지가 넘지만, 모든 노드는 공통적으로 `IScenarioNode`라는 하나의 규격을 따릅니다. 즉 아래 3개 필드는 노드 종류를 가리지 않고 항상 존재합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `identifier` | `string` | 노드의 고유 식별자 | `intro-dialogue-01` |
| `nodeType` | `string` | 노드 종류를 나타내는 문자열(JSON 역직렬화 판별 키) | `Dialogue` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자. 마지막 노드이거나 다른 방식(옵션/조건)으로 분기하는 노드는 `null`일 수 있다 | `next-node-identifier` |

그래프 전체는 JSON에서 `nodes`라는 하나의 딕셔너리(맵) 형태로 저장되며, 각 노드는 자신의 `identifier`를 키로 사용해 이 딕셔너리에 들어갑니다(즉, 딕셔너리 키와 노드 내부의 `identifier` 값이 서로 같아야 합니다). 그런데 이 그래프에는 "어디서부터 시작할지"를 알려주는 필드가 따로 없습니다. 시작 노드는 그래프 데이터 안이 아니라, `ScenarioController.StartScenario(graph, startNodeIdentifier, ...)`를 호출하는 코드 쪽에서 지정합니다.

> **주의**: 만약 시작 노드 식별자를 지정하지 않으면, 딕셔너리에서 어떤 노드가 먼저 나올지 보장되지 않는 임시 로직이 동작합니다. 실제 시나리오를 재생할 때는 항상 시작 노드 식별자를 명시적으로 지정하는 것을 권장합니다.

### 대화창을 띄우는 노드의 공통 입력 규칙

`Dialogue`, `Choice`, `Quiz` 세 노드는 화면에 대사창을 띄운다는 공통점이 있고, 그만큼 플레이어 입력을 처리하는 방식도 동일합니다(구현 위치: `PlayerController.Input.cs`, `DialoguePanelUIController.cs`). 시나리오를 작성할 때 플레이어가 실제로 어떤 조작을 하게 되는지 알아두면 대사 길이나 선택지 개수를 정할 때 도움이 됩니다.

- 글자가 한 자씩 타이핑되는 애니메이션이 재생되는 동안 아무 입력(마우스 좌클릭, `Space`, `F` 중 하나)이나 주면, 애니메이션만 즉시 끝까지 표시되고 다음 노드로는 넘어가지 않습니다. 즉 첫 입력은 "빨리 보여줘"라는 뜻입니다.
- 글자 표시가 이미 끝난 상태에서 같은 입력을 한 번 더 주면, 그제야 다음 노드로 진행하거나(Dialogue), 지금 강조돼 있는 선택지를 확정합니다(Choice/Quiz). 즉 두 번째 입력이 "다음으로"라는 뜻입니다.
- Choice나 Quiz처럼 여러 선택지 중 하나를 고르는 노드에서는, 마우스 스크롤 휠 또는 `-`/`+`(키패드 포함) 키로 강조된 선택지를 옮길 수 있습니다. 숫자 키를 눌러 곧바로 특정 번호를 고르는 방식은 지원하지 않으므로, 선택지가 너무 많으면 플레이어가 원하는 항목을 찾기 번거로울 수 있습니다. 선택지는 가급적 4~5개 이하로 구성하는 것을 권장합니다.

## ScenarioGraph

시나리오 그래프 파일 하나(즉, `.scenario.json` 하나)는 아래 필드로 구성된 최상위 객체입니다. 여러 개의 짧은 시나리오를 만들 계획이라면, 서로 다른 그래프 사이에서 `identifier`가 겹치지 않도록 주의해야 합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `identifier` | `string` | 시나리오 그래프 자신의 식별자 | `disaster-intro` |
| `tags` | `string[]` | 그래프에서 사용할 태그 사전 선언 목록. 선언되지 않은 태그가 노드/브랜치에서 사용되면 로딩 시 경고가 출력된다(옵션) | `["nurse", "doctor"]` |
| `questDefinitionIncludes` | `string[]` | 미리 로드할 퀘스트 정의 파일명 목록(`Resources/Quest/*.quest.json`) | `["main-quest.quest.json"]` |
| `nodes` | `Dictionary<string, Node>` | 노드 식별자를 키로 하는 노드 맵 | - |

`tags`는 필수는 아니지만, 여러 명이 함께 작업하는 시나리오라면 처음에 사용할 태그를 미리 적어두는 것이 좋습니다. 오타로 다른 태그를 잘못 입력했을 때 경고 로그로 바로 알아챌 수 있기 때문입니다.

---

### Dialogue

가장 기본적이고 가장 자주 쓰이는 노드입니다. 화면에 캐릭터의 대사 한 마디를 띄우는 역할을 합니다. 플레이어는 마우스 클릭, `Space`, `F` 중 아무 입력이나 줘서 타이핑 연출을 건너뛰거나 다음 대사로 넘어갈 수 있습니다.

컷씬처럼 플레이어의 입력을 기다리지 않고 저절로 흘러가야 하는 대화라면 `autoAdvanceSeconds`에 값을 지정하면 됩니다. 이 값을 지정하면 대사가 표시된 뒤 그 시간이 지나면 입력 없이도 자동으로 다음 노드로 넘어갑니다(단, 플레이어가 그 전에 입력을 주면 즉시 넘어갑니다). 반대로 플레이어가 직접 읽는 속도에 맞춰 진행하는 일반적인 대화라면 `autoAdvanceSeconds`를 비워두고 입력을 기다리게 하는 것이 자연스럽습니다.

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

플레이어에게 "어떻게 할지"를 직접 고르게 하는 노드입니다. 여러 개의 선택지(`options`)를 보여주고, 플레이어가 그중 하나를 고르면 그 선택지에 적힌 `nextNodeIdentifier`로 이동합니다. Dialogue 노드와 달리 진행 방향이 하나로 정해져 있지 않기 때문에, 노드 자체의 `nextIdentifier`는 사용하지 않고 항상 `null`로 둬야 합니다(어디로 갈지는 전적으로 선택지가 결정합니다).

플레이어가 선택지를 고르는 방법은 마우스 스크롤 또는 `-`/`+` 키로 원하는 항목을 강조한 뒤, 마우스 클릭/`Space`/`F`로 확정하는 방식입니다. 훈련 시나리오에서 "정답이 없는" 분위기 전환용 선택지와 "실제로 결과가 달라지는" 중요한 선택지를 함께 쓸 때는, `displayText`만으로 결과의 무게가 잘 드러나도록 문구를 명확하게 적어두는 것이 좋습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `speakerName` | `string` | 대사를 말하는 캐릭터의 표시 이름 | `간호사` |
| `dialogueContent` | `string` | 선택지를 제시하기 위한 대사 내용 | `어떤 처치를 먼저 하시겠습니까?` |
| `portraitSpriteIdentifier` | `string` | (optional) 대화 중 표시할 캐릭터 초상화 스프라이트 식별자 | `portrait_nurse` |
| `options` | `ScenarioChoiceOption[]` | 선택지 목록(하단 참고) | - |
| `playTTS` | `bool` | 참이면 `dialogueContent`를 표시할 때 TTS로 함께 재생합니다. 세부 동작은 Dialogue의 `playTTS`와 동일(optional, 기본 false) | `false` |
| `ttsVoiceIdentifier` | `string` | 사용할 목소리 프로파일 식별자(optional) | `nurse-voice-01` |
| `nextIdentifier` | `string` | 스키마 규칙상 반드시 `null`이어야 합니다(옵션을 통해서만 분기) | `null` |

#### ScenarioChoiceOption

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `displayText` | `string` | 선택지에 표시될 텍스트 | `지혈부터 시작한다` |
| `displayIconIdentifier` | `string` | (optional) 선택지에 표시될 아이콘 식별자 | `icon_bandage` |
| `displayColor` | `{r,g,b,a}` | 선택지 표시 색상 | `{"r":1,"g":1,"b":1,"a":1}` |
| `nextNodeIdentifier` | `string` | 이 선택지를 고를 때 이동할 다음 노드 식별자 | `treat-bleeding` |

### Sound

지정한 효과음(SFX)을 한 번 재생하는 간단한 노드입니다. 폭발음, 알람음처럼 짧게 재생하고 바로 다음으로 넘어가도 되는 소리라면 `waitUntilFinished`를 꺼둔 채(false) 사용하면 되고, 소리가 다 끝날 때까지 시나리오가 잠깐 멈춰야 하는 경우(예: 긴박한 알람이 끝나야 다음 대사가 시작되는 연출)라면 `waitUntilFinished`를 켜서(true) 사용하면 됩니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `soundResourceIdentifier` | `string` | 재생할 사운드 리소스 식별자(`Resources/Sound/<id>` 또는 `Resources/<id>`에서 조회) | `explosion_01` |
| `waitUntilFinished` | `bool` | 재생 완료까지 다음 노드 진행을 대기할지 여부 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PlayerMove

플레이어 캐릭터를 시나리오가 원하는 위치로 자동으로 이동시키는 노드입니다. 예를 들어 "환자에게 다가가서 대화를 시작한다" 같은 연출을 만들 때, 플레이어가 직접 걸어가지 않아도 이 노드로 이동을 대신할 수 있습니다.

목적지는 좌표(`Position`)로 직접 찍어도 되고, 미리 씬에 배치해 둔 웨이포인트(`Waypoint`)를 참조해도 됩니다. 씬 구조가 바뀔 가능성이 있다면 좌표보다 웨이포인트를 사용하는 편이 유지보수하기 쉽습니다. 이동 방식은 순간이동(`Instant`), 정해진 속도로 걸어가기(`BySpeed`), 정해진 시간 동안 이동(`ByDuration`) 중에서 상황에 맞게 고르면 됩니다 — 예를 들어 급박한 상황이라면 `Instant`, 자연스러운 도보 이동이라면 `BySpeed`가 어울립니다.

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

의사나 간호사 같은 NPC 캐릭터를 목적지로 이동시키는 노드입니다. 필드 구성은 `PlayerMove`와 완전히 동일하고, "누구를 움직이는지"만 다르다고 생각하면 됩니다. 여러 NPC를 동시에 등장·퇴장시키는 장면에서는 각 NPC마다 별도의 NPCMove 노드를 두고, 필요하면 `Parallel` 노드로 묶어 동시에 움직이게 할 수도 있습니다.

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

카메라가 어디를 바라볼지 바꾸는 노드입니다. 중요한 순간에 특정 NPC나 오브젝트를 클로즈업하는 컷씬 연출에 사용합니다. `blendTime`을 짧게 주면 급격하게 시선이 전환되고, 길게 주면 부드럽게 시선이 이동하므로, 장면의 긴박함 정도에 맞춰 조절하면 좋습니다. 컷씬이 끝나면 다시 플레이어 시점으로 돌려주는 별도의 CameraTarget 노드를 추가하는 것을 잊지 않아야 합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetObjectIdentifier` | `string` | 카메라가 바라볼 대상 오브젝트의 식별자 | `npc_doctor` |
| `offsetX` / `offsetY` / `offsetZ` | `float` | 대상 기준 오프셋(기본값 0) | `0.0` |
| `blendTime` | `float` | 대상 전환 블렌드 시간(초, 기본값 1) | `1.0` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Parallel

"여러 일이 동시에 벌어지는" 상황을 표현하는 노드입니다. 예를 들어 다인 플레이 훈련에서 간호사 역할 세 명이 각각 활력징후 측정, GCS 평가, 석션을 동시에 수행해야 하는 장면을 만들 때 이 노드를 사용합니다. 각 동시 작업은 `branches`라는 목록의 항목 하나하나로 표현되며, 각 브랜치는 그 자체로 독립된 노드 체인(연쇄)입니다.

가장 신경 써야 할 설정은 `allocationType`입니다. 혼자 플레이하는 시나리오라면 `SelfAll`로 두면 되고, 여러 명이 각자 다른 역할(태그)을 맡아 협력하는 시나리오라면 `ByRole`을 사용해 각 브랜치를 알맞은 역할의 플레이어에게 한 명씩 배정할 수 있습니다. `waitMode`는 "모두가 끝나야 다음으로 넘어갈지(`All`)", "한 명이라도 끝나면 넘어갈지(`Any`)", "기다리지 않고 바로 넘어갈지(`None`)"를 결정합니다. 협력 처치처럼 모두의 완료가 중요한 경우 `All`을, 여러 방법 중 하나만 성공해도 되는 경우 `Any`를 사용하면 됩니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `branches` | `ScenarioParallelBranch[]` | 동시에 실행할 브랜치 목록(하단 참고) | - |
| `waitMode` | `string` (`All`\|`Any`\|`None`) | 브랜치 완료 감시 정책. `All`=모든 브랜치가 끝나야 진행, `Any`=하나라도 끝나면 진행, `None`=시작 즉시 진행(fire-and-forget) | `All` |
| `allocationType` | `string` (`SelfAll`\|`RandomOneAll`\|`SpreadRandom`\|`SpreadOrdinary`\|`ByRole`) | 브랜치를 플레이어에게 분배하는 방식. `ByRole`은 각 브랜치를 자격(태그)에 맞는 서로 다른 플레이어에게 1:1로 배정합니다(다인 동시 협력용) | `ByRole` |
| `whenBranchingPlayerNotMatched` | `string` (`Panic`\|`Ignore`\|`Reallocation`) | 플레이어 수와 브랜치 수가 일치하지 않을 때의 처리 방식 | `Panic` |
| `nextIdentifier` | `string` | `waitMode` 조건 충족 후 이동할 다음 노드 식별자 | `next-node-identifier` |

#### ScenarioParallelBranch

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `identifier` | `string` | 브랜치의 시작 노드 식별자 | `branch-vitals` |
| `completionConditionIdentifier` | `string` | 브랜치 완료 조건(수렴 라벨) 식별자. 브랜치 체인의 마지막 노드의 `nextIdentifier`가 이 값을 가리키면 해당 브랜치가 완료된 것으로 간주됩니다 | `branch-vitals-done` |
| `requiredPlayerTags` | `string[]` | (optional) 이 브랜치 실행 대상이 되기 위해 필요한 플레이어 태그 목록 | `["nurse"]` |
| `forbiddenPlayerTags` | `string[]` | (optional) 이 브랜치 실행 대상에서 제외할 플레이어 태그 목록 | `["doctor"]` |
| `requiredPlayerTagsMatchMode` | `string` (`All`\|`Any`) | `requiredPlayerTags` 매칭 모드(기본값 `All`) | `All` |

### ServerInternalSignal

여러 플레이어가 함께 플레이할 때, "한 사람의 진행이 다른 사람의 상황에 달려 있는" 경우에 쓰는 신호 전달용 노드입니다. 예를 들어 의사 역할 플레이어는 간호사 역할 플레이어가 처치를 끝낼 때까지 다음 대사를 기다려야 하는 상황을 생각해 보면, 간호사 쪽 그래프에서는 처치가 끝난 뒤 `Resolve`로 신호를 올리고, 의사 쪽 그래프에서는 `Register`로 그 신호를 기다리면 됩니다. 어느 쪽이 먼저 도착하든(신호가 먼저 발생하든, 대기가 먼저 걸리든) 서버가 두 요청을 짝지어 처리해 주므로 순서를 걱정할 필요는 없습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetIdentifier` | `string` | 신호의 목적지입니다. `@m`은 서버 권위 대상(기본값), `@s`는 실행자 자신을 뜻합니다 | `@m` |
| `signalIdentifier` | `string` | 신호 이름 | `sig.patient-a-triaged` |
| `operation` | `string` (`Register`\|`Resolve`) | `Register`=신호를 기다립니다(선등록 시 이후 발생하는 동일 신호에 즉시 resolve), `Resolve`=신호를 발생시킵니다(선발생 시 이후 등록하는 대기자에게 즉시 resolve) | `Register` |
| `waitForResolution` | `bool` | 참이면 신호가 resolve될 때까지 다음 노드 진행을 막습니다(기본값 true) | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### InvokeEvent

시나리오 그래프의 표준 노드로는 표현하기 어려운, 프로젝트에 특화된 동작을 실행해야 할 때 쓰는 "탈출구" 같은 노드입니다. 실제 동작은 C# 코드(TriageTrainer의 `TriageScenarioEventBootstrap.Event.*` 등)에 미리 등록돼 있어야 하며, 이 노드는 그 코드를 `eventIdentifier`로 호출만 합니다. 새로운 게임플레이 기능이 필요한데 기존 노드로 표현할 수 없다면, 먼저 개발자와 상의해 이벤트 핸들러를 추가한 뒤 이 노드로 연결하는 흐름을 권장합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `eventIdentifier` | `string` | 호출할 이벤트의 식별자 | `patient-a.spawn` |
| `moveNextBehavior` | `string` (`False`\|`Immediately`\|`WaitUntilDone`) | 이벤트 호출 후 다음 노드로 진행하는 시점. `False`=자동으로 진행하지 않음, `Immediately`=이벤트를 발동시킨 직후 즉시 진행, `WaitUntilDone`=핸들러가 완료될 때까지 대기(기본값) | `WaitUntilDone` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Validator

"조건이 맞는지 확인하는" 노드입니다. 예를 들어 "플레이어가 2명 이상 접속했는지", "이 플레이어가 간호사 태그를 갖고 있는지" 같은 조건을 검사한 뒤, 조건이 맞으면 다음으로 넘어가고 맞지 않으면 실패로 처리하도록 만들 수 있습니다.

이 노드는 두 가지 모드로 쓸 수 있습니다. `waitForCondition`을 꺼두면(기본값) "지금 이 순간에" 조건을 딱 한 번 검사하고 그 결과에 따라 `onFailure` 정책(오류로 중단, 다른 노드로 분기, 무시하고 진행)을 적용합니다. 반대로 `waitForCondition`을 켜면 조건이 충족될 때까지 계속 기다리는 "게이트" 역할을 하게 되는데, 이는 예를 들어 "간호사가 처치를 완료하기 전까지 이 지점에서 대기"처럼 다른 플레이어의 작업 완료를 기다려야 하는 상황에 유용합니다. 무한정 기다리는 것이 부담스럽다면 `waitTimeoutSeconds`로 최대 대기 시간을 정해두는 것을 권장합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `rootConditions` | `ScenarioValidatorRootCondition[]` | 평가할 조건 목록(하단 참고) | - |
| `onFailure` | `string` (`Panic`\|`Branching`\|`Ignore`) | 조건 실패 시 동작. `Panic`=오류 처리, `Branching`=`failureNextIdentifier`로 분기, `Ignore`=무시하고 계속 진행 | `Branching` |
| `failureReportTargets` | `string` (flags: `UnityConsole`\|`InGameChat`) | 실패 보고 대상(플래그 조합 가능) | `UnityConsole` |
| `failureNextIdentifier` | `string` | (`onFailure`가 `Branching`일 때) 실패 시 이동할 노드 식별자 | `validator-fail-branch` |
| `waitForCondition` | `bool` | 참이면 조건이 충족될 때까지 진행을 막고 폴링 대기하는 게이트로 동작합니다. 미지정/false이면 1회만 평가하고 `onFailure` 정책을 따릅니다(하위호환, 기본값 false) | `true` |
| `waitTimeoutSeconds` | `float` (nullable) | `waitForCondition` 게이트의 타임아웃(초)입니다. `null`/0 이하면 타임아웃 없이 무한 대기합니다(기본값) | `30.0` |
| `onWaitTimeout` | `string` (`KeepWaiting`\|`FailBranch`\|`ForceAdvance`\|`WarnAndKeepWaiting`) | `waitTimeoutSeconds` 초과 시 동작. `KeepWaiting`=계속 대기(기본값), `FailBranch`=`failureNextIdentifier`로 분기, `ForceAdvance`=`nextIdentifier`로 강제 진행, `WarnAndKeepWaiting`=경고 후 계속 대기 | `KeepWaiting` |
| `nextIdentifier` | `string` | 조건 충족(또는 `Ignore`/`ForceAdvance`) 시 이동할 다음 노드 식별자 | `next-node-identifier` |

#### ScenarioValidatorRootCondition

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `condition` | `string` (`PlayerCountEqual` 등, 하단 참고) | 평가할 조건 종류 | `PlayerAssignedTag` |
| `targetCount` | `int` | 비교 대상 수(PlayerCount* 조건에서 사용) | `2` |
| `playerTag` | `string` | 비교할 플레이어 태그(`PlayerAssignedTag` 조건에서 사용) | `nurse` |
| `playerScope` | `string` (`Any`\|`All`\|`Owner`) | 조건을 검사할 플레이어 범위(기본값 `Any`) | `Any` |
| `matchMode` | `string` (`All`\|`Any`) | `RegistryContains`의 규칙 결합 방식. 생략/`All`은 모두 충족(AND), `Any`는 하나 이상 충족(OR) | `Any` |
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

플레이어의 퀘스트 목록에 새 퀘스트를 추가하거나, 기존 퀘스트 내용을 갱신하거나, 완료된 퀘스트를 제거하는 노드입니다. "환자에게 다가가서 대화하기" 같은 목표를 화면에 안내해 주고 싶을 때 이 노드로 퀘스트를 추가하면 됩니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `operation` | `string` (`Add`\|`Update`\|`Remove`) | 퀘스트 조작 종류 | `Add` |
| `failureStrategy` | `string` (`Overwrite`\|`Ignore`\|`Panic`) | 충돌(이미 존재하는 퀘스트 등) 처리 방식(기본값 `Overwrite`) | `Overwrite` |
| `questDefinitionIdentifier` | `string` | 참조할 퀘스트 정의 식별자(`.quest.json`) | `main-quest` |
| `quest` | `QuestData` | 인라인 퀘스트 데이터(제목/설명/내용/추적 여부 등). `questDefinitionIdentifier`와 함께 또는 대신 사용 가능 | - |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### QuestWaypointHighlight

"여기로 가세요"라고 알려주는 웨이포인트 마커를 화면에 강조 표시하는 노드입니다. 퀘스트를 새로 부여한 직후에 함께 사용하면, 플레이어가 다음에 어디로 이동해야 할지 헤매지 않고 바로 알 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `waypointIdentifier` | `string` | 강조할 웨이포인트 식별자 | `wp_entrance` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Delay

그냥 정해진 시간만큼 잠깐 멈춰 있는 노드입니다. 대사와 대사 사이에 약간의 여백을 주거나, 효과음이 끝날 시간을 벌어주거나, 연출상 "숨 고르는" 타이밍을 만들 때 사용합니다. 뒤에 바로 다음 동작이 이어져야 한다면 `waitUntilFinished`를 켜두고, 대기와 동시에 다른 작업이 진행돼야 한다면 `Immediately`로 두고 다음 노드를 바로 실행하게 만들 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `durationSeconds` | `float` | 대기할 시간(초) | `2.0` |
| `waitUntil` | `string` (`Immediately`\|`WaitUntilDone`) | `Immediately`=대기를 시작만 하고 즉시 다음 노드로 진행, `WaitUntilDone`=대기가 끝날 때까지 다음 노드 진행을 막음(기본값) | `WaitUntilDone` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### TimeControl

화면에 시간을 표시하는 스톱워치/카운트다운(이하 "타이머")을 생성하고, 흐름을 시작·정지·일시정지·재개하며, 표시를 켜고 끄는 노드입니다. 시나리오에서 "8분 안에 처치를 완료하라" 같은 시간 제한이나, 경과 시간을 측정하는 스톱워치를 띄워야 할 때 사용합니다.

이 노드의 특징은 **생성·흐름·표시가 서로 분리**되어 있다는 점입니다. `Create`로 타이머를 만들면 "정지 상태"로 생성되며 화면에 표시되지 않습니다. 흐르게 하려면 `Start`를, 화면에 띄우려면 `Show`를 각각 따로 호출해야 합니다. 만약 `Create` 즉시 `Show` 없이 표시되기를 기대하면 안 됩니다 — 의도한 동작이 아니며, 스펙상 표시는 `Show`로만 켭니다. 반대로 `Hide`는 표시만 끌 뿐 타이머의 흐름과 존재는 유지되므로, 숨겨진 동안에도 시간은 계속 흐릅니다. 타이머가 더 이상 필요 없으면 명시적으로 `Remove`로 삭제해야 합니다.

여러 타이머를 `timerId`로 구분해 동시에 보유할 수 있으나, **화면에 표시되는 타이머는 항상 최대 1개**입니다. `Show`로 새 타이머를 표시하면 기존 표시 대상은 교체됩니다. 카운트다운이 `00:00:00`에 도달해도 자동으로 숨겨지지 않으므로, 종료 연출이 필요하다면 별도의 `Hide`/`Remove` 노드를 둬야 합니다.

카운트다운(`direction: Countdown`)의 경우 `durationSeconds`가 "총 목표 시간", `startSeconds`가 "시작할 때 화면에 표시할 남은값"입니다. `startSeconds`를 생략(또는 0)하면 `durationSeconds`에서 시작합니다. 스톱워치(`direction: Stopwatch`)에서는 `durationSeconds`를 무시하며 `startSeconds`만 시작 표시값(기본 0)으로 사용합니다. `Set` 연산은 흐름 상태(흐르고 있으면 계속 흐름, 멈춰 있으면 계속 멈춤)를 유지한 채 표시값만 절대값으로 바꾸며, `durationSeconds`가 양수면 카운트다운 목표 시간도 함께 재설정합니다(0이면 기존 목표 유지).

모든 연산은 서버 권한으로 모든 클라이언트에 전파되며, 이 노드는 대기 없이 즉시 다음 노드로 진행합니다(흐름 자체는 백그라운드에서 진행됨). 표시 렌더링은 HUD(`TimeDisplayUIController`)가 매 프레임 `ScenarioTimeState`를 조회해 수행합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `operation` | `string` (`Create`\|`Start`\|`Pause`\|`Resume`\|`Stop`\|`Set`\|`Show`\|`Hide`\|`Remove`) | 가할 연산(기본값 `Create`) | `Create` |
| `timerId` | `string` | 대상 타이머 식별자. `Hide` 외 모든 연산에서 사용 | `scenario_timer` |
| `direction` | `string` (`Stopwatch`\|`Countdown`) | 흐름 방향. `Create`에서만 사용. `Stopwatch`=경과 시간 증가, `Countdown`=남은 시간 감소(기본값 `Stopwatch`) | `Countdown` |
| `durationSeconds` | `float` | `Create`에서는 카운트다운 목표(총) 시간(초). `Set`에서는 카운트다운 목표 재설정(0이면 유지). 스톱워치에서는 무시(기본값 0) | `480.0` |
| `startSeconds` | `float` | `Create`에서는 시작 표시값(스톱워치=경과, 카운트다운=남은값. 카운트다운에서 0이면 `durationSeconds`로 대체). `Set`에서는 설정할 절대 표시값. 다른 연산에서는 무시(기본값 0) | `0.0` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

#### 연산별 동작 요약

| `operation` | 사용 파라미터 | 동작 |
|---|---|---|
| `Create` | `timerId`, `direction`, `durationSeconds`, `startSeconds` | 타이머를 "정지 상태"로 생성. 화면에 표시하지 않음 |
| `Start` | `timerId` | 타이머 흐름을 시작(또는 재시작) |
| `Pause` | `timerId` | 흐름을 일시정지 |
| `Resume` | `timerId` | 일시정지된 흐름을 재개 |
| `Stop` | `timerId` | 흐름을 정지하고 표시값을 시작값으로 되돌림(리셋) |
| `Set` | `timerId`, `startSeconds`, `durationSeconds`(0이면 목표 유지) | 표시값을 절대값으로 설정. 흐름 상태는 유지 |
| `Show` | `timerId` | 지정한 타이머를 화면에 표시(기존 표시는 교체). 표시는 항상 최대 1개 |
| `Hide` | (없음) | 화면 표시만 끔. 타이머 상태·흐름은 유지(숨겨진 동안에도 시간은 계속 흐름) |
| `Remove` | `timerId` | 타이머를 삭제(표시 중이면 표시도 꺼짐) |

> **주의**: `Create`만 한 상태에서 `Show` 없이 타이머가 화면에 나타나기를 기대하면 안 됩니다. 표시는 오직 `Show`로만 켜집니다. 반대로 `Hide`는 타이머를 삭제하지 않으므로, 표시를 꺼도 흐름은 계속되고 `Show`로 다시 켤 수 있습니다.

### Interaction

플레이어가 어떤 오브젝트를 직접 조작(사용/조사/부착/탈착)해야만 다음으로 넘어갈 수 있는 노드입니다. 예를 들어 "문을 직접 열어야 다음 장면으로 넘어간다"처럼, 플레이어가 능동적으로 행동하게 만들고 싶을 때 Dialogue 대신 이 노드를 사용하면 몰입감을 높일 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `actorScope` | `string` (`Player`\|`Any`) | 상호작용을 수행해야 하는 주체 범위(기본값 `Player`) | `Player` |
| `targetIdentifier` | `string` | 상호작용 대상의 식별자 | `door_01` |
| `requiredItemIdentifier` | `string` | (optional) 상호작용에 필요한 아이템 식별자 | `keycard` |
| `interactionType` | `string` (`Use`\|`Inspect`\|`Attach`\|`Detach`) | 상호작용 종류(기본값 `Use`) | `Use` |
| `completionConditionIdentifier` | `string` | (optional) 완료 신호/이벤트 식별자 | `door-opened` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### CombineItem

여러 개의 재료 아이템을 하나의 결과 아이템으로 합치는 노드입니다. 붕대와 거즈를 조합해 상처 드레싱 키트를 만드는 식의 훈련 상황을 표현할 수 있습니다. 플레이어가 직접 조합 버튼을 눌러야 하는 경우와, 필요한 재료가 다 모이면 자동으로 조합되는 경우를 `autoCombine`으로 선택할 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `inputItemIdentifiers` | `string[]` | 조합에 필요한 입력 아이템 식별자 목록 | `["gauze", "bandage"]` |
| `outputItemIdentifier` | `string` | 조합 결과 아이템 식별자 | `wound_dressing_kit` |
| `autoCombine` | `bool` | 참이면 수동 조작 없이 자동으로 조합됩니다 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### Quiz

플레이어의 이해도를 확인하는 객관식 퀴즈 노드입니다. 정답을 고르면 `onCorrectNextIdentifier`로, 오답을 고르면 `onIncorrectNextIdentifier`로 이동하도록 만들 수 있습니다. 선택하는 방법은 Choice 노드와 동일합니다(스크롤 또는 `±` 키로 강조한 뒤 클릭/`Space`/`F`로 확정).

오답을 골랐을 때 바로 다음으로 넘어가지 않고 같은 퀴즈로 되돌아가게(`onIncorrectNextIdentifier`를 자기 자신 또는 재시도용 노드로 지정) 만들면, 플레이어가 다시 도전해 정답을 맞힐 때까지 학습을 유도할 수 있습니다. `feedbackCorrect`/`feedbackIncorrect`에 왜 정답인지/오답인지 설명을 짧게 넣어주면 교육 효과를 높일 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `question` | `string` | 문제 문항 | `이 환자의 KTAS 등급은?` |
| `options` | `string[]` | 객관식 보기 목록 | `["1등급", "2등급", "3등급"]` |
| `correctIndex` | `int` | 정답 인덱스(0-base) | `1` |
| `onCorrectNextIdentifier` | `string` | 정답 선택 시 다음 노드 식별자 | `quiz-correct-feedback` |
| `onIncorrectNextIdentifier` | `string` | (optional) 오답 선택 시 다음 노드 식별자(예: 재시도 루프) | `quiz-retry` |
| `feedbackCorrect` | `string` | (optional) 정답 시 표시할 피드백 텍스트 | `정답입니다!` |
| `feedbackIncorrect` | `string` | (optional) 오답 시 표시할 피드백 텍스트 | `다시 확인해보세요.` |
| `playTTS` | `bool` | 참이면 문항(및 피드백)을 표시할 때 TTS로 함께 재생합니다(optional, 기본 false) | `false` |
| `ttsVoiceIdentifier` | `string` | 사용할 목소리 프로파일 식별자(optional) | `doctor-voice-01` |

### StateUpdate

시나리오가 진행되는 동안 잠깐 기억해 둬야 할 값을, 화면에 보이지 않는 "메모장"(상태 저장소)에 키-값 형태로 적어두는 노드입니다. 이후 다른 노드들이 이 값을 다시 읽어 조건 분기 등에 활용할 수 있습니다. 값 자체는 문자열로만 저장되므로, 숫자나 상태를 담더라도 문자열 규칙(예: 부정맥 상태 코드 표기)을 팀 내에서 미리 정해두고 일관되게 사용하는 것이 좋습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetEntityIdentifier` | `string` | 상태를 갱신할 대상(엔티티) 식별자 | `patient_a` |
| `stateKey` | `string` | 상태 키(예: `vitals.rhythm`) | `vitals.rhythm` |
| `stateValue` | `string` | 저장할 상태 값 | `PEA` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PlayTTS

Dialogue 노드처럼 화면에 텍스트 창을 띄우지 않고, 순수하게 음성만 재생하고 싶을 때 사용하는 노드입니다. 예를 들어 무전기에서 들려오는 안내 음성이나, 배경에서 흘러나오는 방송처럼 화면에 자막 창이 없어도 되는 상황에 어울립니다. `variables`를 이용하면 환자 이름이나 목적지 같은 값을 매번 다른 내용으로 바꿔 재생할 수 있어서, 같은 대본을 여러 시나리오에서 재활용하기 좋습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `transcriptIdentifier` | `string` | `TTSService`에 등록된 대본(Transcript) 식별자 | `triage-move-patient` |
| `variables` | `Dictionary<string,string>` | (optional) 대본 내 동적 세그먼트 변수 오버라이드. 필요하지만 값이 없는 변수는 경고 후 해당 세그먼트를 건너뜀 | `{"patient-name":"김철수"}` |
| `waitUntilFinished` | `bool` | 참이면 모든 클립 재생이 끝날 때까지 다음 노드 진행을 대기합니다(기본값 true) | `true` |
| `ttsVoiceIdentifier` | `string` | (optional) 사용할 목소리 프로파일 식별자. 미지정 시 기본 목소리 사용 | `doctor-voice-01` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PlayerTag (JSON `nodeType`: `TagModification`, 구버전 호환 별칭 `PlayerTag`)

다인 플레이 시나리오에서 "이 플레이어는 간호사, 저 플레이어는 의사" 같은 역할을 나눠주는 노드입니다. 태그는 곧 역할표라고 생각하면 됩니다. `Parallel` 노드의 `ByRole` 브랜치 분배나 `Validator`의 태그 조건 검사는 모두 이 노드로 부여한 태그를 참고하므로, 협동 시나리오를 만들 때는 시작 지점에서 역할 태그를 먼저 배정해 두는 흐름이 일반적입니다. 두 플레이어의 역할을 서로 바꿔야 하는 경우(예: 중간에 역할 교대)에는 `Swap` 조작을 사용하면 각자 새 태그를 추가/제거하는 여러 노드를 만들 필요 없이 한 번에 처리할 수 있습니다.

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

미리 만들어 둔 엔티티 프리셋(환자, NPC 등)을 씬에 등장시키는 노드입니다. 프리셋 내부에 어떤 하위 오브젝트를 함께 스폰할지는 이미 프리셋 쪽에서 정해져 있으므로, 이 노드에서는 "어떤 프리셋을, 어디에, 어떤 이름으로" 스폰할지만 정하면 됩니다. 스폰한 뒤에도 계속 그 대상을 제어해야 한다면(예: 이후 `EntityTag`, `StateUpdate` 등으로 상태를 바꿔야 한다면) `spawnedEntityIdentifier`를 꼭 지정해서 나중에 같은 식별자로 다시 찾을 수 있게 해두는 것이 좋습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `presetIdentifier` | `string` | 스폰할 엔티티 프리셋 식별자 | `patient_preset_a` |
| `spawnedEntityIdentifier` | `string` | 스폰된 루트 인스턴스에 부여할 엔티티 식별자입니다. 비어 있으면 GUID 기반 식별자가 자동 부여됩니다 | `patient_a` |
| `positionSourceEntityIdentifier` | `string` | (optional) 다른 엔티티의 위치를 기준으로 스폰할 때 그 엔티티의 식별자 | `bed_a` |
| `positionX` / `positionY` / `positionZ` | `float` | 스폰 좌표(`positionSourceEntityIdentifier`가 없을 때 사용) | `0.0` |
| `resultStateKey` | `string` | (optional) 결과로 생성된 엔티티 식별자를 상태 저장소에 기록할 키 | `patient_a.entityId` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### EntityTag

플레이어가 아니라 환자나 오브젝트 같은 "엔티티"에 태그를 붙이거나 떼는 노드입니다. `PlayerTag`가 사람(플레이어)의 역할표라면, 이 노드는 환자나 사물의 상태 라벨이라고 볼 수 있습니다. 예를 들어 환자에게 `critical` 태그를 붙여두면, 이후 다른 시스템(레지스트리 조회, Validator 조건 등)에서 그 태그를 기준으로 "위급 환자만" 걸러내는 로직을 만들 수 있습니다.

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

새로 등장시킨 환자(혹은 이미 씬에 있는 환자)에, "처음부터 이런 상태로 시작한다"를 한 번에 세팅해 주는 노드입니다. 대표적인 활용 사례는 환자 몸에 이미 경부보호대나 거즈가 붙어 있는 채로 시나리오가 시작되게 만드는 것입니다 — 사고 현장에 응급처치가 이미 일부 되어 있는 상황을 표현할 때 유용합니다.

`presetIdentifier`를 지정하면 새 엔티티를 스폰하면서 초기 상태를 함께 적용하고, 지정하지 않으면 이미 존재하는 엔티티(`targetEntityIdentifier` 또는 `targetEntityStateKey`로 지정)에 초기 상태만 적용합니다. `EntityPresetSpawn`으로 스폰한 뒤 곧바로 `EntityInit`으로 상태를 세팅하는 두 단계 대신, 스폰과 초기화가 한 번에 필요하다면 이 노드 하나로 끝낼 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `presetIdentifier` | `string` | (optional) 스폰할 엔티티 프리셋 식별자입니다. 지정되면 새 인스턴스를 스폰합니다 | `patient_preset_b` |
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

환자를 트리아지(중증도 분류) 평가할 수 있는 상태로 켜거나 끄는 노드입니다. 시나리오 초반에는 아직 환자에게 접근할 수 없게 막아두고, 도입부 대화나 준비 동작이 끝난 뒤에 이 노드로 평가를 열어주는 식으로 활용하면, 플레이어가 준비되지 않은 상태에서 미리 평가를 끝내버리는 것을 방지할 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetEntityIdentifier` | `string` | 트리아지 평가를 제어할 대상 환자(엔티티) 식별자 | `patient_a` |
| `assessable` | `bool` | 참이면 트리아지 평가 인터랙션 활성화, 거짓이면 비활성화 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### PatientMedicalStatePreset

환자의 이름, 나이, 의식 수준, 호흡수, 맥박, 혈압 등 의료 정보를 한 번에 세팅하는 노드입니다. 훈련 시나리오에서 환자의 초기 상태(예: "GCS 8점의 의식 저하 환자")를 만들 때 이 노드를 사용하며, 이후 처치 결과에 따라 상태를 다시 이 노드로 바꿔서 환자 상태가 개선되거나 악화되는 흐름을 표현할 수도 있습니다.

가장 중요한 규칙은 필드 값의 의미입니다. 필드를 아예 적지 않으면(`null`) "지금 값 그대로 유지"이고, 수치 필드에 `-1`을 명시적으로 적으면 "측정할 수 없음/없음"이라는 별도의 의미가 됩니다. 이 둘을 혼동하지 않아야 합니다 — 예를 들어 심정지 환자의 맥박을 표현하려면 `pulseRate`를 `-1`로 명시해야 하며, 단순히 필드를 비워두면 이전 값이 그대로 남아있게 됩니다. 값을 부드럽게 변화시키는 연출(`transitionMode: Gradual`)을 쓰면, 환자가 서서히 나빠지거나 회복되는 모니터링 화면을 만들 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `targetEntityIdentifier` | `string` | 상태를 초기화할 환자 엔티티 식별자 | `patient_a` |
| `targetEntityStateKey` | `string` | (optional) `targetEntityIdentifier`가 비어 있을 때 상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키 | `patient_a.entityId` |
| `transitionMode` | `string` (`Immediate`\|`Gradual`) | 프리셋 값 적용 방식. `Immediate`=즉시 덮어쓰기(기본값), `Gradual`=`transitionDurationSeconds` 동안 수치 값을 점차 보간(비수치 필드는 종료 시점에 적용, `-1`은 보간 대상 아님) | `Immediate` |
| `transitionDurationSeconds` | `float` | (`Gradual`일 때) 보간 소요 시간(초)입니다. 0 이하이면 즉시 적용과 동일합니다 | `0.0` |
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

플레이어가 특정 아이템을 모아서 "제출"해야 하는 상호작용을 설정하는 노드입니다. 예를 들어 "의사 NPC에게 지정된 약품 2개를 가져다줘야 다음으로 넘어간다" 같은 미션을 만들 때 사용합니다. `requiredItems`로 어떤 아이템이 몇 개 필요한지 지정하고, 제출이 완료되면 `completionSignalIdentifier`로 신호를 올려줍니다.

이 노드 하나로 상호작용 대상을 새로 스폰할 수도 있고, 이미 배치된 대상(예: NPC에 붙어 있는 제출 슬롯)을 재사용할 수도 있습니다. 제출 완료 신호는 뒤따르는 `Validator` 노드에서 `waitForCondition`으로 기다리게 만들면, "제출이 끝날 때까지 다음 장면으로 넘어가지 않는" 흐름을 자연스럽게 구성할 수 있습니다.

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

NPC가 특정 시점에만 상호작용 가능하게 만들고 싶을 때 쓰는 노드입니다. 예를 들어 시나리오 중반에 도달하기 전까지는 의사 NPC에게 "아이템 제출" 기능이 없다가, 특정 지점을 지나면 이 노드로 그 기능을 활성화(`Add` 또는 `Enable`)하고, 시나리오가 끝나면 다시 비활성화(`Disable`)하는 식으로 사용합니다. 이렇게 하면 아직 준비되지 않은 상호작용을 플레이어가 미리 눌러버리는 상황을 막을 수 있습니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `npcIdentifier` | `string` | 대상 NPC의 레지스트리 식별자 | `npc_doctor` |
| `interactableIdentifier` | `string` | 대상 Interactable의 식별자. `Add` 시 레지스트리에서 해당 식별자의 인터랙트 컴포넌트를 찾아 NPC의 커스텀 소스로 추가하고, `Enable`/`Disable` 시 대상이 토글 가능한 인터랙터블이면 활성 상태를 전환한다 | `submission_a` |
| `operation` | `string` (`Add`\|`Remove`\|`Enable`\|`Disable`) | 수행할 동작. `Add`=추가, `Remove`=제거, `Enable`=활성화, `Disable`=비활성화(기본값 `Add`) | `Add` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### ChatPrint

시나리오 진행 중 임의의 텍스트를 인게임 채팅창이나 Unity 콘솔에 출력하는 노드입니다. 어떤 시그널(예: 수액 줄 연결/끊김)이 실제로 올라왔는지, 어떤 지점을 지나고 있는지를 눈으로 바로 확인하고 싶을 때 사용하는 디버깅·데모용 노드입니다. 대사창을 띄우는 `Dialogue`와 달리 플레이어 입력을 기다리지 않고 즉시 다음 노드로 넘어갑니다.

`targets`로 출력 위치를 고를 수 있습니다. `UnityConsole`은 개발자용 로그(콘솔)에만, `InGameChat`은 인게임 채팅창에 출력하며, 두 값을 함께(예: `"UnityConsole, InGameChat"`) 지정할 수도 있습니다. 여러 명이 함께 플레이하는 경우, 서버가 판정한 결과를 모든 플레이어에게 한 번씩 보여주고 싶다면 `broadcast`를 켜면 됩니다. `broadcast`가 꺼져 있으면 각 플레이어가 자기 화면(로컬 채팅창/콘솔)에만 출력합니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `message` | `string` | 출력할 메시지 본문 | `[IV 시그널] 연결 완료 감지` |
| `targets` | `string` (flags: `UnityConsole`\|`InGameChat`) | 출력 대상(플래그 조합 가능). 미지정 시 기본값 `InGameChat` | `UnityConsole, InGameChat` |
| `broadcast` | `bool` | 참이면 서버가 전체 클라이언트에게 브로드캐스트(InGameChat 대상에 한함). 거짓(기본)이면 각 피어가 자기 화면에만 출력 | `true` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

### ExecuteCommand

인게임 채팅 명령어(예: `/give`, `/title`, `/tag`)를 시나리오가 서버 권한으로 대신 실행하는 노드입니다. 사람이 채팅창에 직접 명령어를 치는 것과 동일한 효과를 시나리오 흐름 안에서 자동으로 일으킬 수 있습니다. 명령 문자열 안에 대상 선택자(`@s`=실행 주체 자신, `@a`=전체, `@n`=가장 가까운 플레이어, `fish:<clientId>`=특정 클라이언트)와 파이프라인(`|`)을 그대로 쓸 수 있으므로, "특정 플레이어/서버 기준 실행"을 명령 문자열로 표현합니다.

이 노드는 서버(또는 오프라인 단일 플레이) 컨텍스트에서만 실제로 명령을 실행하고, 일반 클라이언트는 진행만 합니다(같은 명령이 여러 번 실행되는 것을 방지). 대기 없이 즉시 다음 노드로 넘어갑니다. 선행 슬래시(`/`)는 있어도 없어도 됩니다.

| 필드 이름 | 값 타입 | 값 | 예시 |
|---|---|---|---|
| `commandLine` | `string` | 실행할 명령 문자열(대상 선택자·파이프라인 포함 가능). 서버 권한 시스템 컨텍스트에서 실행됨 | `title @a title 수액 연결 완료` |
| `nextIdentifier` | `string` | 다음 진행 노드의 식별자 | `next-node-identifier` |

> **활용 예 — 수액 연결 시그널 확인**: 수액 줄 연결 지점(`IntravenousLineConnectionPoint`)은 연결 시도/완료/끊김 시 각각 `iv_connect_start_<지점Identifier>`, `iv_connected_<지점Identifier>`, `iv_disconnected_<지점Identifier>` 시그널을 인게임 서버로 올립니다(RuntimeState 레지스트리에 `sig.` 접두사로 기록). 따라서 `Validator`(condition=`RegistryContains`, registryType=`RuntimeState`, registryIdentifier=`sig.iv_connected_<지점Identifier>`, `waitForCondition: true`) 게이트로 해당 시그널을 기다렸다가, `ChatPrint`로 "연결 완료 감지"를 출력하거나 `ExecuteCommand`로 후속 명령을 실행할 수 있습니다. 완성된 예시는 `Assets/Modules/TriageTrainer/Resources/Scenario/iv_signal_debug.scenario.json`을 참고하세요.
>
> 이 예시 그래프는 두 개의 흐름(자기완결적 시퀀스)으로 구성되어 있습니다. **흐름1**(`flow1_*`)은 감시 등록 안내를 출력하고 `ServerInternalSignal`(operation=`Resolve`, `waitForResolution: false`)로 등록 신호만 올린 뒤 마칩니다 — 진입점 `flow1_intro_print`, 마침점 `flow1_registered_print`. **흐름2**(`flow2_*`)는 진입점 `flow2_wait_connect_start`의 `Validator` 게이트에서 연결 시작 신호를 기다렸다가, 연결/끊김 신호를 순차적으로 감지해 메시지를 출력하고 마침점 `flow2_print_disconnected`에서 종료합니다. 두 흐름은 논리적으로 분리된 시퀀스이며, 예시에서는 흐름1의 마침점이 흐름2의 진입점으로 이어지도록 연결되어 있습니다.
>
> **주의 — UI 없는(신호 대기) 시나리오와 Interactable**: 이 예시처럼 `Dialogue`/`Choice`/`Quiz` 같은 대화창 UI 노드가 하나도 없는 "배경 신호 감시" 시나리오는, 시나리오가 진행 중이어도 월드 Interactable 힌트를 가리지 않습니다. (시나리오 시작 시점에 힌트 UI를 대화 모드로 강제 전환하지 않으며, 실제 대화창 노드가 표시될 때만 지연 전환합니다.) 또한 시나리오가 종료되면 로컬 플레이어의 근처 Interactable을 다시 인식시켜 힌트가 현재 상태로 복구됩니다.
