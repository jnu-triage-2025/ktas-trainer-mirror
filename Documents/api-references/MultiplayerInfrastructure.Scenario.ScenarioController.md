# API 레퍼런스: `MultiplayerInfrastructure.Scenario.ScenarioController`

> **네임스페이스:** `MultiplayerInfrastructure.Scenario`  
> **기반 클래스:** `UnityEngine.MonoBehaviour` (씬 싱글톤)  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`

---

## 0. 문서 목적

`ScenarioController`는 시나리오 그래프의 노드를 순차적으로 실행하는 씬 싱글톤입니다.  
씬에 하나만 배치되며, 다이얼로그·선택지·이벤트 등 모든 시나리오 노드를 처리합니다.

---

## 1. 싱글톤 접근

```csharp
ScenarioController.Instance   // ScenarioController
```

씬에 인스턴스가 없으면 `null`입니다. 접근 전 `null` 체크를 권장합니다.

---

## 2. State 열거형

```csharp
public enum State
{
    Inactive,                     // 시나리오 비활성
    ExecutingDialogue,            // 다이얼로그 표시 중 (수동 진행 대기)
    ExecutingChoice,              // 선택지 표시 중
    ExecutingSound,               // 사운드 재생 중 (구현 예정)
    ExecutingPlayerMove,          // 플레이어 이동 중 (구현 예정)
    ExecutingNPCMove,             // NPC 이동 중 (구현 예정)
    ExecutingCameraTarget,        // 카메라 타겟 전환 중 (구현 예정)
    ExecutingInvokeEvent,         // 외부 이벤트 핸들러 실행 중
    ExecutingValidator,           // 조건 검사 중
    ExecutingParallel,            // 병렬 브랜치 실행 중
    ExecutingQuestControl,        // 퀘스트 추가/변경/제거
    ExecutingQuestWaypointHighlight, // 웨이포인트 강조
    ExecutingNotification,        // 알림 표시
    ExecutingDelay,               // 대기 중
    ExecutingInteraction,         // 인터랙션 대기 중 (구현 예정)
    ExecutingCombineItem,         // 아이템 합성 (구현 예정)
    ExecutingQuiz,                // 퀴즈 선택지 표시 중
    ExecutingStateUpdate,         // 상태 변수 갱신 중 (구현 예정)
    ExecutingRoleAssignment,      // 역할 배정 중
    ExecutingTTS,                 // TTS 재생 중 (PlayTTS 노드)
}
```

---

## 3. 이벤트

```csharp
public event Action OnScenarioStarted;                      // 시나리오 시작됨
public event Action OnScenarioEnded;                        // 시나리오 종료됨
public event Action<IScenarioNode> OnNodeChanged;           // 노드 이동됨
public event Action<ScenarioChoiceOption> OnOptionSelected; // 선택지 선택됨
```

---

## 4. 프로퍼티

```csharp
public bool IsActive { get; }               // 시나리오 진행 중 여부 (State != Inactive)
public State CurrentState { get; }          // 현재 상태
public IScenarioNode CurrentNode { get; }   // 현재 노드
public ScenarioGraph CurrentGraph { get; }  // 현재 그래프
```

---

## 5. 공개 메서드

### `StartScenario`

```csharp
// startNodeIdentifier를 생략하면 그래프의 첫 노드에서 시작
public void StartScenario(ScenarioGraph graph, string startNodeIdentifier = null)

// ownerClientId: 시나리오 소유자 클라이언트 ID (RoleAssignment 등에 사용)
public void StartScenario(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
```

**주의:** 서버 측(`ScenarioCommandRunner`)에서 호출합니다. 직접 호출 시 모든 클라이언트에 동기화되지 않을 수 있습니다.

---

### `EndScenario`

```csharp
public void EndScenario()
```

시나리오를 즉시 종료합니다. 상태 변수, 역할 배정, 진행 중인 선택지가 모두 초기화됩니다.  
`OnScenarioEnded` 이벤트가 발생합니다.

---

### `Advance`

```csharp
public void Advance()
```

현재 노드의 `NextIdentifier`로 이동합니다.  
`ExecutingDialogue` 상태에서 플레이어가 "다음" 버튼을 누를 때 호출됩니다.

---

### `SelectOption`

```csharp
public void SelectOption(int index)
```

`ExecutingChoice` 또는 `ExecutingQuiz` 상태에서 선택지를 선택합니다.  
`OnOptionSelected` 이벤트가 발생하고 해당 선택지의 `NextNodeIdentifier`로 이동합니다.

---

## 6. 노드 종류 및 동작

| 노드 타입 | State | 동작 |
|---|---|---|
| `Dialogue` | `ExecutingDialogue` | 화자 이름+내용 표시. `Advance()` 호출 대기 |
| `Choice` | `ExecutingChoice` | 복수 선택지 표시. `SelectOption(i)` 호출 대기 |
| `Quiz` | `ExecutingQuiz` | 정답/오답 분기 선택지. `SelectOption(i)` 호출 대기 |
| `InvokeEvent` | `ExecutingInvokeEvent` | `ScenarioEventIdentifierRegistry`에서 핸들러 조회·실행 |
| `QuestControl` | `ExecutingQuestControl` | QuestManager 통해 퀘스트 추가/갱신/제거 |
| `QuestWaypointHighlight` | `ExecutingQuestWaypointHighlight` | WaypointAnchor 강조 |
| `Notification` | `ExecutingNotification` | 알림 UI 메시지 표시 |
| `Delay` | `ExecutingDelay` | 지정 시간(초) 대기 후 자동 진행 |
| `Parallel` | `ExecutingParallel` | 복수 브랜치 동시 또는 순차 실행 |
| `Validator` | `ExecutingValidator` | 플레이어 수 등 조건 검사 후 분기 |
| `RoleAssignment` | `ExecutingRoleAssignment` | 역할 자동/수동 배정 |
| `StateUpdate` | `ExecutingStateUpdate` | 내부 상태 변수 갱신 (구현 예정) |
| `Sound` | `ExecutingSound` | 사운드 재생 (구현 예정) |
| `PlayerMove` | `ExecutingPlayerMove` | 플레이어 이동 (구현 예정) |
| `NPCMove` | `ExecutingNPCMove` | NPC 이동 (구현 예정) |
| `CameraTarget` | `ExecutingCameraTarget` | 카메라 타겟 전환 (구현 예정) |
| `Interaction` | `ExecutingInteraction` | 인터랙션 대기 (구현 예정) |
| `CombineItem` | `ExecutingCombineItem` | 아이템 합성 (구현 예정) |
| `PlayTTS` | `ExecutingTTS` | TTSService로 TTS 합성 재생. `WaitUntilFinished`에 따라 완료 대기 |

---

## 7. Parallel 브랜치 배분 모드

`Parallel` 노드는 복수의 하위 노드를 동시에 실행합니다. 플레이어 할당 방식은 아래 4가지입니다.

| 모드 | 설명 |
|---|---|
| `SelfAll` | 모든 플레이어가 모든 브랜치를 동시에 진행 |
| `RandomOneAll` | 무작위로 하나의 브랜치만 선택, 전원 동일하게 진행 |
| `SpreadRandom` | 플레이어를 랜덤 분산 (역할 배분) |
| `SpreadOrdinary` | 플레이어를 순서대로 분산 (역할 배분) |

---

## 8. 내부 상태 변수 (_stateStore)

`StateUpdate` 노드로 조작하는 `string → string` 딕셔너리입니다.  
시나리오 종료(`EndScenario()`) 시 자동으로 초기화됩니다.

---

## 9. RegisterReferences

```csharp
public void RegisterReferences(
    DialoguePanelUIController uiController,
    MainCameraController camController,
    InteractableObjectHintUIController hintUIController)
```

Inspector 참조를 코드로 주입할 때 사용합니다. 일반적으로 Unity Inspector를 통해 설정합니다.

---

## 10. TTS 연동 (PlayTTS 노드)

`ScenarioController`에는 `TTSService` 연동을 위한 두 가지 Inspector 필드가 있습니다.

| 필드 | 타입 | 설명 |
|---|---|---|
| `_ttsService` | `TTSService` | TTS 재생 서비스 참조 |
| `_ttsAudioSource` | `AudioSource` | TTS 오디오를 출력할 `AudioSource` 참조 |

`PlayTTS` 노드 실행 시 동작 순서:

1. `_ttsService` 또는 `_ttsAudioSource`가 `null`이면 경고 로그 후 건너뙡니다.
2. `TTSService.IsReady`가 `false`이면 `true`가 될 때까지 대기합니다.
3. `TTSService.IsDynamicCacheDirty`가 `true`이면 사전 캐싱이 완료될 때까지 대기합니다.
4. `PlayTranscript`를 호출하여 실제 재생합니다.
5. `WaitUntilFinished`가 `true`면 재생 완료를 기다린 후 다음 노드로 진행합니다.

### PrewarmTTSCache 자동 호출

`StartScenario` 실행 시, 그래프 내 모든 `PlayTTS` 노드의 동적 세그먼트를 `TTSService.PrepareTranscriptVariables`로 일괄 사전 합성합니다.  
이를 통해 노드 실행 시점에 TTS 생성 지연을 최소화할 수 있습니다.

---

## 관련 문서

- [multiplayer-infrastructure-overview.md](architecture/multiplayer-infrastructure-overview.md) — 시나리오 시스템 개요
- [scenario-authoring-guide.md](../requirements/content-definitions/scenario/scenario-authoring-guide.md) — 시나리오 작성 가이드
- [scenario-graph-spec.md](../requirements/content-definitions/scenario/scenario-graph-spec.md) — 시나리오 노드 JSON 스펙
- [api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md) — 이벤트 등록 API
- [api-references/TextToSpeechService.TTSService.md](TextToSpeechService.TTSService.md) — TTSService API (`PlayTTS` 노드 연동)
