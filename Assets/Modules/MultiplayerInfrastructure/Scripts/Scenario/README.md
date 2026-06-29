# Scenario

다이얼로그, 플레이어 이동, 상호작용, 대화 등을 순차적으로 한 번에 재생되도록 합니다. 스토리텔링을 정의하는데 활용하는 것이 의도되었습니다.  

## 시나리오 데이터 작성 및 동작 방식 문서

이 문서는 `MultiplayerInfrastructure.Scenario` 시스템에서 사용하는 **JSON 형식**, **C# DTO 및 도메인 모델**, 그리고 **ScenarioGraphLoader가 데이터를 읽고 작동시키는 전체 흐름**을 설명합니다. 또한 예제 JSON과 제작 시 주의할 점을 포함합니다.

### 1. 데이터 흐름 개요

1. **시나리오 JSON 작성**  
   시나리오 그래프 전체를 JSON으로 정의합니다. 노드별 유형과 세부 필드를 명시합니다.

2. **JSON 스키마 검증 (선택)**  
   `ScenarioJsonSchemaValidator.Validate(json)`을 통해 구조적 오류를 사전에 걸러냅니다.

3. **DTO 역직렬화**  
   `JsonSerializer.Deserialize<ScenarioGraphDTO>(json, SerializerOptions)`로 JSON을 **DTO (Data Transfer Object)** 형태로 로드합니다.  
   - `ScenarioNodeDTOConverter`가 `nodeType`을 읽고 각 노드를 구체 DTO 타입(예: `ScenarioDialogueNodeDTO`)으로 변환합니다.

4. **도메인 모델 변환**  
   `ScenarioGraphLoader.ToDomain(dto)`가 DTO 객체를 **도메인 모델** (`ScenarioGraph`, `ScenarioDialogueNode` 등)로 변환합니다.

5. **런타임 사용**  
   변환된 `ScenarioGraph`는 게임 로직에서 각 노드를 순회/실행하는 데 사용됩니다.

### 2. JSON 구조

최상위 구조는 대개 다음과 같습니다.

```json
{
  "nodes": {
    "nodeId-1": {
      "identifier": "nodeId-1",
      "nodeType": "Dialogue",
      "speakerName": "NPC",
      "dialogueContent": "안녕하세요!"
    },
    "nodeId-2": {
      "identifier": "nodeId-2",
      "nodeType": "Choice",
      "options": [
        {
          "displayText": "반갑습니다.",
          "nextNodeIdentifier": "nodeId-3"
        }
      ]
    }
  }
}
```

#### 공통 규칙

- `nodes`는 **키: 노드ID → 값: 노드 JSON 객체** 형태의 딕셔너리입니다.
- 각 노드 JSON은 반드시 `identifier`와 `nodeType`을 포함해야 합니다.
- 노드 키(`nodeId-1`)와 `identifier` 문자열은 **완전히 동일**해야 합니다.
- `nodeType`은 아래 나열된 지원 타입 중 하나여야 합니다.

| nodeType           | DTO 클래스                    | 도메인 클래스               |
|--------------------|------------------------------|-----------------------------|
| Dialogue           | `ScenarioDialogueNodeDTO`    | `ScenarioDialogueNode`      |
| Choice             | `ScenarioChoiceNodeDTO`      | `ScenarioChoiceNode`        |
| Sound              | `ScenarioSoundNodeDTO`       | `ScenarioSoundNode`         |
| PlayerMove         | `ScenarioPlayerMoveNodeDTO`  | `ScenarioPlayerMoveNode`    |
| CameraTarget       | `ScenarioCameraTargetNodeDTO`| `ScenarioCameraTargetNode`  |
| InvokeEvent        | `ScenarioInvokeEventNodeDTO` | `ScenarioInvokeEventNode`   |
| Validator          | `ScenarioValidatorNodeDTO`   | `ScenarioValidatorNode`     |
| Parallel           | `ScenarioParallelNodeDTO`    | `ScenarioParallelNode`      |
| QuestControl       | `ScenarioQuestControlNodeDTO`| `ScenarioQuestControlNode`  |

### 3. 주요 노드 필드 설명

#### 3.1 Dialogue (`ScenarioDialogueNodeDTO`)

| 필드                   | 타입      | 설명                                 |
|------------------------|-----------|--------------------------------------|
| `speakerName`          | string    | 화자 이름                            |
| `dialogueContent`      | string    | 대사 텍스트                          |
| `portraitSpriteIdentifier` | string | 초상화 리소스 ID (옵션)             |
| `nextIdentifier`       | string    | 다음 노드 ID (옵션, 없으면 종료)    |

#### 3.2 Choice (`ScenarioChoiceNodeDTO`)

| 필드             | 타입                         | 설명                                      |
|------------------|------------------------------|-------------------------------------------|
| `options`        | 배열                         | 선택지 리스트                             |
| `options[].displayText` | string               | 선택지 표시 텍스트                        |
| `options[].displayIconIdentifier` | string     | 아이콘 리소스 ID (옵션)                   |
| `options[].displayColor` | RGBA 객체          | `{ "r":1, "g":1, "b":1, "a":1 }` 구조     |
| `options[].nextNodeIdentifier` | string        | 선택 결과 이동할 노드 ID                  |

#### 3.3 Sound (`ScenarioSoundNodeDTO`)

| 필드                    | 타입    | 설명                                             |
|-------------------------|---------|--------------------------------------------------|
| `soundResourceIdentifier` | string | 재생할 사운드 리소스 ID                          |
| `waitUntilFinished`     | bool?   | 완료까지 대기 여부 (null 시 기본 `true`)         |
| `nextIdentifier`        | string  | 다음 노드 ID                                     |

#### 3.4 PlayerMove (`ScenarioPlayerMoveNodeDTO`)

| 필드                   | 타입    | 설명                                                                               |
|------------------------|---------|------------------------------------------------------------------------------------|
| `destinationType`      | string  | `ScenarioMoveDestinationType` enum 이름 (예: `"Position"`, `"Object"`)              |
| `destinationIdentifier`| string  | 목적지 식별자 (타입에 따라 해석)                                                   |
| `destinationX/Y/Z`     | float?  | 좌표 (미지정 시 0)                                                                  |
| `ignoreGroundCheck`    | bool?   | 지면 체크 무시                                                                      |
| `moveMode`             | string  | `ScenarioMoveMode` enum 이름 (`"BySpeed"`, `"ByDuration"` 등)                        |
| `moveSpeed`            | float?  | 속도로 이동 시 필요한 값                                                            |
| `moveDuration`         | float?  | 시간 기반 이동 시 필요한 값                                                         |
| `nextIdentifier`       | string  | 다음 노드 ID                                                                        |

> ⚠️ `destinationType`, `moveMode`는 문자열이지만 로더에서 각각 `ScenarioMoveDestinationType`, `ScenarioMoveMode` enum으로 변환됩니다. JSON에 **정확한 enum 이름**을 써야 합니다.

#### 3.5 CameraTarget (`ScenarioCameraTargetNodeDTO`)

| 필드                | 타입    | 설명                              |
|---------------------|---------|-----------------------------------|
| `targetObjectIdentifier` | string | 카메라 대상 오브젝트 ID       |
| `offsetX/Y/Z`       | float?  | 오프셋                            |
| `blendTime`         | float?  | 블렌드 시간                       |
| `nextIdentifier`    | string  | 다음 노드 ID                      |

#### 3.6 Parallel (`ScenarioParallelNodeDTO`)

| 필드              | 타입     | 설명                                                      |
|-------------------|----------|-----------------------------------------------------------|
| `waitMode`        | string   | `ScenarioWaitMode` enum 이름 (`"All"`, `"Any"`, 등)       |
| `allocationType`  | string   | `SelfAll` \| `RandomOneAll` \| `SpreadRandom` \| `SpreadOrdinary` |
| `whenBranchingPlayerNotMatched` | string | `Panic` \| `Ignore` \| `Reallocation` |
| `branches`        | 배열     | 병렬 분기 목록                                            |
| `branches[].identifier` | string | 브랜치 고유 ID                                        |
| `branches[].completionConditionIdentifier` | string | 완료 조건 식별자   |

> Parallel 노드는 브랜치 완료를 기다린 뒤 종료되며, `nextIdentifier`를 사용하지 않습니다.

#### 3.7 InvokeEvent (`ScenarioInvokeEventNodeDTO`)

| 필드              | 타입    | 설명                                                             |
|-------------------|---------|------------------------------------------------------------------|
| `eventIdentifier` | string  | `ScenarioEventIdentifierRegistry`에 등록된 이벤트 식별자          |
| `moveNextBehavior`| string  | `False` \| `Immediately` \| `WaitUntilDone`                      |
| `nextIdentifier`  | string  | 이벤트 실행 후 이동할 다음 노드 ID (`moveNextBehavior`가 False면 사용 안함) |

> 핸들러가 코루틴을 반환하면 완료까지 대기하고, `null`을 반환하면 즉시 다음 노드로 진행합니다.

#### 3.8 Validator (`ScenarioValidatorNodeDTO`)

| 필드                     | 타입    | 설명                                                              |
|--------------------------|---------|-------------------------------------------------------------------|
| `condition`              | string  | 플레이어 수 비교: `PlayerCountEqual`, `PlayerCountNotEqual`, `<`, `<=`, `>`, `>=` 대응 |
| `targetCount`            | int     | 비교 대상 값                                                       |
| `onFailure`              | string  | `Panic` \| `Branching` \| `Ignore`                                 |
| `failureNextIdentifier`  | string  | `Branching`(또는 `onWaitTimeout=FailBranch`)일 때 이동할 노드 ID    |
| `waitForCondition`       | bool    | `true`이면 조건 충족까지 진행을 막는 게이트로 동작(기본 `false`)     |
| `waitTimeoutSeconds`     | number  | (옵션) 게이트 타임아웃(초). 미지정/0 이하면 무한 대기(기존 동작)     |
| `onWaitTimeout`          | string  | 타임아웃 시 행동: `KeepWaiting`(기본) \| `FailBranch` \| `ForceAdvance` \| `WarnAndKeepWaiting` |
| `nextIdentifier`         | string  | 검증 성공 시(또는 `onWaitTimeout=ForceAdvance`) 이동할 노드 ID       |

#### 3.9 QuestControl (`ScenarioQuestControlNodeDTO`)

| 필드              | 타입    | 설명                                                                                           |
|-------------------|---------|------------------------------------------------------------------------------------------------|
| `operation`       | string  | `Add` \| `Update` \| `Remove`                                                                  |
| `failureStrategy` | string  | `Overwrite`(기존 덮어쓰기) \| `Ignore`(무시) \| `Panic`(예외 발생)                              |
| `quest`           | object  | 퀘스트 페이로드. `ScenarioQuestDataDTO` 구조를 사용하며 `Id` 필수                             |
| `nextIdentifier`  | string  | 다음 노드 ID                                                                                    |

`ScenarioQuestDataDTO` 필드

| 필드           | 타입              | 설명                    |
|----------------|-------------------|-------------------------|
| `Id`           | string            | 퀘스트 고유 ID (필수)    |
| `Title`        | string \| null    | 제목                    |
| `Description`  | string \| null    | 설명                    |
| `QuestContent` | string \| null    | 상세 내용 또는 본문      |
| `IsTracked`    | boolean           | 추적 여부 (기본 `false`) |

동작 요약: `Add`는 새 퀘스트를 추가하고, `Update`는 ID가 존재할 때 필드를 갱신합니다. `Remove`는 ID 일치 퀘스트를 제거합니다. `failureStrategy`가 `Panic`일 때 실패 시 예외로 중단되고, `Ignore`는 실패를 무시하며, `Overwrite`는 추가/업데이트 시 동일 ID가 있을 경우 덮어씁니다.

### 4. C# DTO & 도메인 모델 관계

- **DTO (`Scenario*NodeDTO`)**: JSON 구조와 1:1로 대응하는 데이터 구조. `internal class`로 선언되어 있으며, 역직렬화 용도.
- **도메인 모델 (`Scenario*Node`)**: 게임 로직에서 사용하는 실제 노드 객체.
- **ScenarioGraphDTO → ScenarioGraph**: DTO 집합을 도메인 그래프로 변환.

변환은 `ScenarioGraphLoader` 내부에서 수행되며, 각 노드 타입마다 별도의 변환 메서드가 존재합니다.

### 5. ScenarioGraphLoader 작동 방식

1. **정적 초기화**  
   - `JsonSerializerOptions` 설정  
     - 대소문자 무시 (`PropertyNameCaseInsensitive = true`)  
     - 주석/트레일링 콤마 허용  
   - `ScenarioNodeDTOConverter` 추가 → `nodeType` 기반으로 구체 DTO 선택

2. **LoadFromJson 호출**  
   - 입력 문자열 검증  
   - 스키마 검증(옵션)
   - JSON → `ScenarioGraphDTO` 역직렬화

3. **ToDomain**  
   - DTO의 `nodes` 딕셔너리를 순회  
   - 키와 `identifier` 일치 여부 확인  
   - `ConvertNode`로 각 DTO → 도메인 노드 변환  
   - `ScenarioGraph.Add(node)` 호출

4. **문자열 → enum 변환**  
   - `ParseWaitMode`, `ParseMoveMode`, `ParseDestinationType` 등 헬퍼 사용  
   - 값이 비어 있으면 기본값 적용, 알 수 없는 문자열이면 `JsonException` 발생

### 6. 시나리오 작성 절차

1. **설계**
   - 그래프 다이어그램을 만든 뒤 노드 ID, 흐름 정의
   - 필요한 노드 타입 및 필수 필드 파악

2. **JSON 작성**
   - `nodes` 내 각 노드 정의
   - `nodeType`에 맞는 필드만 포함
   - enum 필드는 정확한 문자열로 입력

3. **검증 & 테스트**
   - (선택) 스키마 검증
   - 로더로 불러오기 → 예외 없는지 확인
   - 실제 게임에서 실행해 논리 흐름 점검

4. **버전 관리**
   - JSON 파일을 별도 리포지터리나 리소스 폴더에서 관리
   - 변경 시 DTO/도메인 구조와 일치하는지 확인

### 7. 모범 사례 & 주의 사항

- **일관된 식별자 사용**: JSON 키, `identifier`, 다른 노드에서 참조하는 `nextIdentifier` 모두 동일해야 합니다.
- **enum 값 대소문자**: 로더가 `ignoreCase: true`로 파싱하기 때문에 대소문자 차이는 허용되지만, 오탈자 방지를 위해 enum 이름 그대로 쓰는 것을 권장합니다.
- **기본값 명시**: 선택적 필드라도 명시하면 디버깅이 쉬워집니다. (예: `waitUntilFinished: true`)
- **분기 누락 방지**: `Choice` 노드에서 `nextNodeIdentifier`가 누락되면 실행 중 예기치 않은 종료가 발생할 수 있습니다. `Parallel` 노드는 브랜치 식별자와 완료 조건만 사용합니다.
- **스키마 유지보수**: JSON 구조가 변하면 스키마와 DTO를 동시에 업데이트해야 합니다.
- **Converter 등록 확인**: `ScenarioNodeDTOConverter`가 `SerializerOptions.Converters`에 등록되어 있어야 `nodeType` 기반 폴리모픽 역직렬화가 정상 작동합니다.

### 8. 예제: 간단한 시나리오 JSON

```json
{
  "nodes": {
    "intro": {
      "identifier": "intro",
      "nodeType": "Dialogue",
      "speakerName": "Guide",
      "dialogueContent": "어서 오세요!",
      "nextIdentifier": "choice1"
    },
    "choice1": {
      "identifier": "choice1",
      "nodeType": "Choice",
      "speakerName": "Guide",
      "dialogueContent": "어디부터 둘러볼까요?",
      "options": [
        {
          "displayText": "시장",
          "displayColor": { "r": 1, "g": 0.8, "b": 0, "a": 1 },
          "nextNodeIdentifier": "market"
        },
        {
          "displayText": "숲",
          "nextNodeIdentifier": "forest"
        }
      ]
    },
    "market": {
      "identifier": "market",
      "nodeType": "Sound",
      "soundResourceIdentifier": "sfx_market",
      "waitUntilFinished": false,
      "nextIdentifier": "outro"
    },
    "forest": {
      "identifier": "forest",
      "nodeType": "PlayerMove",
      "destinationType": "Position",
      "destinationX": 10,
      "destinationY": 0,
      "destinationZ": 25,
      "moveMode": "BySpeed",
      "moveSpeed": 5,
      "nextIdentifier": "outro"
    },
    "outro": {
      "identifier": "outro",
      "nodeType": "Dialogue",
      "speakerName": "Guide",
      "dialogueContent": "탐험을 즐기셨나요?"
    }
  }
}
```

이 JSON을 `ScenarioGraphLoader.LoadFromJson(jsonText)`로 읽으면, 각 노드가 도메인 객체로 변환되어 게임에서 순차/분기 실행이 가능합니다.

#### 추가 예시: QuestControl로 퀘스트 추가/제거

```json
{
  "nodes": {
    "add-quest": {
      "identifier": "add-quest",
      "nodeType": "QuestControl",
      "operation": "Add",
      "failureStrategy": "Overwrite",
      "quest": {
        "Id": "quest_intro",
        "Title": "마을 사람과 대화",
        "Description": "광장에서 안내인과 대화하기",
        "IsTracked": true
      },
      "nextIdentifier": "remove-quest"
    },
    "remove-quest": {
      "identifier": "remove-quest",
      "nodeType": "QuestControl",
      "operation": "Remove",
      "failureStrategy": "Ignore",
      "quest": { "Id": "quest_intro" }
    }
  }
}
```

`Add`는 동일 ID가 존재해도 `Overwrite`로 덮어쓰며, `Remove`는 없을 경우 `Ignore` 덕분에 실패를 무시합니다. 실패를 에러로 처리하려면 `failureStrategy`를 `Panic`으로 두면 됩니다.
