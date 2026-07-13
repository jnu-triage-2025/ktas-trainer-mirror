# Feature Proposal: 시나리오 동시 실행 충돌 정책(WARN/CANCEL/PANIC)

- 작성일: 2026-07-13
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`, `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/`
- 선행/유사 패턴: `ScenarioPreflightPolicy`(인스펙터 직렬화 정책 + `Default`), `ScenarioPlayerTagMatchMode`(enum 미러링), `CommandDefinition_Gamemode`(enum 파싱 커맨드)
- 관련 작업: `iv_signal_debug` 흐름 분리 + Interactable 사라짐 버그 수정(2026-07-13, TriageTrainer/시스템 최소 수정으로 이미 반영됨)

> 상태: **제안(scheduled)**. `MultiplayerInfrastructure` 는 타 프로젝트 재사용 전제 모듈이므로,
> 본 제안은 예시 구현만 포함하며 실제 모듈 반영은 인간 작업자 검토·승인 후 진행한다(AGENTS.md 규칙).
> 파일별 예시 코드는 같은 폴더의 [`example-implementation.md`](./example-implementation.md) 참조.

### 개요

`시나리오 동시 실행 충돌 정책` 기능은 서로 다른 두 개 이상의 시나리오 정의(흐름)가 **동시에**
"한 번에 하나만 표시될 수 있는" 대화창 계열 UI(`Dialogue`/`Choice`/`Quiz`)를 점유하려 할 때,
이를 감지하고 설정된 정책에 따라 처리하는 기능이다. 이 기능은 각 시나리오 흐름이 대화창을
점유하는 노드를 실행하는 시점에 "이미 다른 흐름이 대화창을 점유 중인가"를 검사하여,
`WARN`(경고 후 진행), `CANCEL`(뒤에 시작된 것 취소), `PANIC`(전체 중단) 중 하나로 대응한다.

- 요약: `ScenarioConcurrencyConflictPolicy { Warn, Cancel, Panic }`(기본 `Warn`) 를 추가하고,
  UI 점유 노드 실행 진입점에서 충돌을 감지해 정책을 적용한다. 정책은 인게임 커맨드
  `/scenario conflictpolicy <warn|cancel|panic>` 로 런타임 변경 가능하다.
- 의도/목표: 여러 시나리오 흐름이 우연히 겹쳐 대화창이 서로 덮어쓰거나 뒤섞이는 상황을
  "의도되지 않은 동작"으로 명시하고, 운영자가 상황에 맞는 대응(관찰/차단/중단)을 고를 수 있게 한다.
- 주요 맥락: 현재 엔진은 `ScenarioController` 싱글턴 1개만 실행하며, `StartScenario` 는 이전 실행을
  `StopAllCoroutines()` 로 **조용히** 덮어쓴다. 대화창 UI(`DialoguePanelUIController`)도 단일
  인스턴스로 한 번에 하나의 대화/선택/퀴즈만 표시한다. 즉 "동시 점유"는 (a) 한 그래프 내
  병렬 브랜치(`Parallel`)가 각각 대화창 노드를 재생하려 할 때, (b) 하나의 흐름이 진행 중인데 새
  `StartScenario` 가 또 다른 대화창 흐름을 시작할 때 발생할 수 있다.
- 기술적 제약: 하위호환 필수. 기본값 `Warn` 은 "경고 후 undefined behavior 로 흐르도록 두기"이므로,
  기존 동작(그대로 흘러감)과 사실상 동일해야 한다(경고 로그만 추가). `Cancel`/`Panic` 은 옵트인.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자/교육 감독자**로서, 두 개 이상의 시나리오 흐름이 동시에 대화창
(`Dialogue`/`Choice`/`Quiz`)을 재생하려는 상황을 감지하고 원하는 방식으로 처리하기를 원한다.
왜냐하면 대화창은 한 번에 하나만 표시될 수 있어, 두 흐름이 동시에 점유하면 대사/선택지가
서로 덮어써지거나 뒤섞여 학습자에게 잘못된 정보를 주는 "의도되지 않은 동작"이 되기 때문이다.

현재는 이런 충돌이 (a) 조용히 덮어써지거나(`StartScenario` 의 `StopAllCoroutines()`),
(b) 병렬 브랜치가 프롬프트 인터셉터/`_branchPromptActive` 상태를 서로 밟아 예측 불가하게
동작한다. 어떤 경우든 운영자가 "지금 충돌이 일어났다"는 사실조차 인지하기 어렵다.

### 사용자 경험 목표

- 운영자: 충돌이 발생하면 정책에 따라
  - `WARN`: 콘솔/인게임챗에 경고가 남고 시나리오는 (기존처럼) 계속 흐른다.
  - `CANCEL`: 나중에 대화창을 점유하려 한 흐름만 취소되고, 먼저 점유한 흐름은 보존된다.
  - `PANIC`: 진행 중인 모든 시나리오가 안전하게 중단(`EndScenario`)된다.
- 운영자: `/scenario conflictpolicy cancel` 처럼 런타임에 정책을 바꿔 재현/디버깅할 수 있다.
- 학습자: 대화창이 뒤섞여 깨지는 경험을 (`CANCEL`/`PANIC` 채택 시) 겪지 않는다.

### 제안

#### 충돌의 정의(사용자 확정: "UI 점유 노드 실행 시점 감지")

"충돌" = **대화창 계열 UI를 점유하려는 시점**에, 이미 다른 시나리오 흐름이 대화창을 점유 중인 경우.
- 판정 기준값: 대화창이 현재 점유 중인지 여부 + 그 점유 주체(그래프/흐름) 식별.
- 점유 진입점: `DialoguePanelUIController.EnsureDialogueModeActive()`(실제 대화창 노드가
  표시될 때 호출되는 지연 전환 지점) 및 이를 호출하는 `DisplayDialogue`/`DisplayChoice`.
  이 지점은 `iv_signal_debug` 버그 수정에서 확인된 "실제 UI 점유 시점"과 동일하다.

#### 변경 요약 (`ScenarioPreflightPolicy`/`ScenarioPlayerTagMatchMode` 패턴 미러링)

| 접점 | 파일 | 변경 |
|---|---|---|
| 신규 enum | `Scripts/Scenario/ScenarioConcurrencyConflictPolicy.cs` (신규) | `ScenarioConcurrencyConflictPolicy { Warn, Cancel, Panic }`, 기본 `Warn` |
| 정책 필드 | `Scripts/Scenario/ScenarioController.cs` | `_concurrencyConflictPolicy`(기본 `Warn`) + 공개 프로퍼티/세터, `Reset()` 초기화 |
| 점유 추적 | `Scripts/UI/Controllers/DialoguePanelUIController.cs` | 대화창을 점유한 그래프 식별자(`_owningGraphIdentifier`) 기록/해제, `IsDialogueOwnedByOther(graphId)` 조회 API |
| 충돌 감지 | `Scripts/Scenario/ScenarioController.cs` | UI 점유 노드 실행 진입부(`ExecuteDialogueNode`/`ExecuteChoiceNode`/`PresentQuiz` 및 브랜치 대응)에서 `TryClaimDialogueUI(...)` 호출, 정책 적용 |
| 인게임 커맨드 | `Scripts/Command/CommandDefinitions/CommandDefinition.Scenario.cs` | `/scenario conflictpolicy <warn\|cancel\|panic>` 서브커맨드 추가(`CommandDefinition_Gamemode.TryParseGamemode` 패턴) |
| (인스펙터) | `ScenarioController` 헤더 | `[SerializeField]` 로 노출(선택) |

#### 정책별 동작(의사 코드)

```csharp
// ScenarioController — UI 점유 노드 실행 직전 공용 게이트
// 반환 true = 이 흐름이 대화창을 점유해도 됨(계속 진행), false = 이 흐름은 취소되어야 함
private bool TryClaimDialogueUI(string owningGraphIdentifier)
{
  if (_uiController.IsUnityNull())
    return true; // UI 없으면 충돌 개념 없음

  // 이미 "다른" 흐름/그래프가 대화창을 점유 중인가?
  if (!_uiController.IsDialogueOwnedByOther(owningGraphIdentifier))
  {
    _uiController.MarkDialogueOwner(owningGraphIdentifier); // 자기 점유 등록/갱신
    return true;
  }

  // 충돌 발생 → 정책 적용
  string msg = $"[ScenarioController] 대화창 UI 동시 점유 충돌: '{owningGraphIdentifier}' 가 " +
               $"'{_uiController.CurrentDialogueOwner}' 점유 중 대화창을 요청함.";
  switch (_concurrencyConflictPolicy)
  {
    case ScenarioConcurrencyConflictPolicy.Warn:   // 경고 후 undefined behavior(그대로 흐름)
      Debug.LogWarning(msg + " (WARN: 경고 후 계속 진행)");
      AppendSystemChatMessage(msg + " (WARN)");
      _uiController.MarkDialogueOwner(owningGraphIdentifier); // 기존 동작대로 덮어씀
      return true;

    case ScenarioConcurrencyConflictPolicy.Cancel: // 뒤에 실행되는 것 취소
      Debug.LogWarning(msg + " (CANCEL: 뒤에 요청한 흐름 취소)");
      AppendSystemChatMessage(msg + " (CANCEL)");
      return false; // 호출부는 이 노드/브랜치를 진행하지 않고 정리

    case ScenarioConcurrencyConflictPolicy.Panic:  // 시나리오 모두 중단
      Debug.LogError(msg + " (PANIC: 전체 시나리오 중단)");
      AppendSystemChatMessage(msg + " (PANIC)");
      EndScenario();
      return false;
  }
  return true;
}
```

> 단일 싱글턴 한계: 현재는 `_owningGraphIdentifier` 로 "같은 그래프의 서로 다른 흐름/브랜치"와
> "다른 그래프"를 구분한다. `Parallel` 두 브랜치가 각각 대화창을 요청하면 두 번째 요청이 충돌로
> 감지된다(같은 그래프여도 프롬프트가 이미 활성인 경우 `_branchPromptActive` 를 함께 참조).
> 장래에 다중 `ScenarioController` 인스턴스를 지원하면 점유 주체를 인스턴스 단위로 확장한다.

#### 인게임 커맨드

`CommandDefinition_Scenario.Execute` 에 서브커맨드를 추가한다(`CommandDefinition_Gamemode` 스타일):

```
/scenario conflictpolicy               # 현재 정책 조회
/scenario conflictpolicy warn|cancel|panic   # 정책 변경(0/1/2 숫자 별칭 허용)
```

### 자세한 달성 목표

1. `conflictpolicy` 미설정(기본 `Warn`) 시, 충돌이 나도 **경고 로그만 추가**되고 기존과 동일하게 흐른다(회귀 최소).
2. `Cancel` 시, 뒤에 대화창을 점유하려던 흐름/브랜치만 진행을 멈추고, 먼저 점유한 흐름은 보존된다.
3. `Panic` 시, 진행 중 시나리오가 `EndScenario()` 로 안전 종료되고 대화창/Interactable 상태가 복구된다.
4. `EndScenario`/대화창 종료 시 점유자(`_owningGraphIdentifier`)가 해제되어 다음 흐름이 정상 점유 가능.
5. 커맨드로 정책을 런타임 변경할 수 있고, 조회 시 현재 값을 보고한다.

### 문서화

- `Documents/guide/ScenarioGraph.md`: "동시 실행과 대화창 점유 충돌" 절 추가(정책 표 + 커맨드 사용법).
- `Documents/api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md`: 정책 프로퍼티/세터 및
  `OnDialogueConflict`(선택 이벤트) 추가 표기.
- 커맨드 목록 문서(있다면)에 `/scenario conflictpolicy` 추가.
- 채택 시 `Tools/validate-documentation-links.sh` 로 링크 검증.

### 가용성과 테스트

- 위험: `MultiplayerInfrastructure`(엔진/커맨드/대화창 UI) 변경. 하위호환을 깨면 모든 시나리오에 영향.
  → 기본값 `Warn` = "경고 후 그대로 진행"으로 고정해 위험 최소화. `Cancel`/`Panic` 은 명시 옵트인.
- 테스트:
  - 단일 흐름 시나리오(대다수): 충돌 감지 로직이 개입하지 않음(점유자 1명) → 회귀 0.
  - `Parallel` 두 브랜치가 각각 `Dialogue` 재생: `Warn`(경고+진행), `Cancel`(두 번째 브랜치 취소),
    `Panic`(전체 종료) 각각 검증.
  - 진행 중 시나리오에 `/scenario execute` 로 두 번째 대화창 흐름 시작: 정책별 동작 검증.
  - `/scenario conflictpolicy` 조회/변경/잘못된 인자 처리 검증.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 두 개 이상 흐름이 대화창을 동시 점유하려는 상황을 감지해 정책대로 처리 가능.
- 수용 기준:
  1. 기본 `Warn` 에서 기존 시나리오 동작이 (경고 로그 외) 변하지 않음(회귀 0).
  2. `Cancel` 에서 나중 점유 흐름만 취소되고 먼저 점유 흐름은 보존됨.
  3. `Panic` 에서 전체 시나리오가 안전 종료되고 대화창/Interactable 이 복구됨.
  4. `/scenario conflictpolicy` 로 런타임 변경·조회가 동작함.

### 링크, 참고사항

- 단일 싱글턴/덮어쓰기 근거: `ScenarioController.cs` `StartScenario` 의 `StopAllCoroutines()`.
- UI 점유 지점: `DialoguePanelUIController.cs` `EnsureDialogueModeActive()` / `DisplayDialogue` / `DisplayChoice`.
- 미러링 대상: `ScenarioPreflightPolicy`(정책+`Default`+`Reset` 초기화), `CommandDefinition_Gamemode.TryParseGamemode`(enum 파싱).
- 선행 버그 수정: `iv_signal_debug` 흐름 분리 및 "UI 없는 시나리오가 Interactable 을 가리던" 버그 수정.
