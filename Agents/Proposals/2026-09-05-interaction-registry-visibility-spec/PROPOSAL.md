# 인터렉션 레지스트리와 가시성 체계 명세

- 제안일: 2026-09-05 (결정 반영: 2026-09-05)
- 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/`, `Scenario/`, `Quest/`, `Tag/`,
  `Assets/Modules/TriageTrainer/Scripts/Patient/`, `Scenario/`, `Entities/`
- 관련 콘텐츠: `patient_a_critical`, `patient_b_c_ct`, `disaster_intro`, `tutorial`과 그 퀘스트 정의
- 상태: 구현 완료(2026-09-06, 미커밋). 인간 작업자가 16개 결정 항목에 답했으며, 그 내용을 본문에 반영하고
  결정 기록과 구현 기록을 마지막 절에 남겼습니다. 남은 것은 EditMode 테스트 실행(에디터 Test Runner)과
  4인 실플레이 수동 검토입니다.
- 근거 자료: 같은 폴더의 [`interaction-inventory.md`](./interaction-inventory.md)(전수조사 결과)

### 개요

인터렉션 레지스트리(Interaction Registry) 체계는 월드 인터렉션의 정의와 노출 상태를 한 곳에서 관리하기 위한
구현입니다. 이 체계는 모든 인터렉션 정의를 초기화 사이클 동안 코드 리터럴과 시나리오 데이터에서 읽어
빈 레지스트리에 채우고, 이후에는 등록된 항목의 가시성만 조건 또는 트리거로 바꾸는 방식으로 동작합니다.
그럼으로써 프리팹에 흩어진 인터렉션 데이터, 시나리오마다 다른 개방 경로, 피어마다 어긋나는 활성
상태를 없애고, 다른 브랜치에서 들어오는 콘텐츠도 같은 형식으로 옮길 수 있게 합니다.

- 요약
  - 인터렉션 정의는 두 출처에서만 옵니다. 엔티티 코드가 선언하는 코드 리터럴과 시나리오 JSON의
    최상위 `interactions` 구역입니다. 프리팹과 씬의 직렬화 필드는 정의 출처에서 제외하고, 이 브랜치의
    구현이 끝나면 제거합니다.
  - 등록은 엔티티 초기화 사이클과 시나리오 초기화 사이클 안에서만 이루어집니다. 그 밖의 시점에 등록하면
    에디터 런타임에서 경고를 남기되 동작은 막지 않습니다.
  - 가시성은 조건 기반(권장)과 트리거 기반 두 방법으로만 바꿉니다. 두 방법 모두 플레이어별과 전역 두 층을
    가지며, 서버 권위로 기록되고 전 피어에 복제되며, 늦게 접속한 피어도 같은 상태를 복원합니다.
  - 조건은 일반화된 조건 절 목록으로 표현합니다. 시나리오 진행 조건뿐 아니라 코드가 판정할 수 있는
    내재 능력 값(보유 아이템, 거리, 운반 상태, 엔티티 속성)도 조건 절로 데이터에서 정의할 수 있습니다.
    같은 조건 형식을 Validator 노드도 사용합니다.
  - 인스펙터에서는 레지스트리의 런타임 상태를 보고 고칠 수 있지만, Unity 에디터에서만 편집할 수 있고 그 값은
    직렬화되지 않는 디버그 지원입니다.
- 주요 맥락
  - `patient_a_critical`은 이미 플레이어별 퀘스트 상태 플래그 풀로 노출을 판정하고 있습니다
    (`PlayerQuestStateFlagService`, 2026-08-27 결정). 이 명세는 그 구조를 시나리오 전체로 일반화하고
    데이터로 표현하는 후속 작업입니다.
  - 운영 그래프는 모두 호환 실행 경로를 탑니다. 즉 네 피어가 각자 그래프를 실행하고 서버 미러 신호로만
    진행을 맞춥니다. 그래서 "노드가 컴포넌트를 직접 켠다"는 방식은 늦은 접속과 재접속에서 상태를
    잃습니다.
- 기술적 제약
  - 기존 시나리오 JSON과 퀘스트 JSON은 스키마 변경 없이 계속 로드되어야 합니다. 새 구역과 노드는
    선택 항목으로 추가하고, 옮겨지는 항목(`actingNpcs[].interactions`)은 로더가 새 형식으로 변환합니다.
  - 태그 저장소는 하나만 둡니다. 엔티티 태그를 위해 별도 서비스를 복제해 만들지 않습니다.

### 해결하려는 문제 상황

나는 시나리오 콘텐츠 작성자로서, 어떤 상호작용이 어느 시점에 누구에게 보이는지를 시나리오 데이터 한
곳에서 정의하고 싶습니다. 왜냐하면 지금은 같은 목적을 위해 일곱 가지 경로가 공존하고, 그 가운데 둘은
피어마다 상태가 어긋나 플레이가 막히기 때문입니다. 전수조사에서 확인한 사실은 다음과 같습니다.

1. 인터렉션 정의가 프리팹(`PatientTypeA.prefab`의 `ScenarioActionInteractable` 12개, 다섯 환자 프리팹의
   `_assessActions` 23개), 씬(IndevScene 데모 NPC, TutorialScene 미끼 3개), ScriptableObject,
   시나리오 JSON(`actingNpcs`), 코드 리터럴(`PatientController` 하위 클래스 20여 종)에 흩어져 있습니다.
2. 컴포넌트에 저장된 활성 플래그(`_enabled`, `_interactConfigs`, `_interactEntries`)는 네트워크 동기화
   대상이 아닙니다. 노드가 켜도 늦게 접속한 피어는 프리팹 기본값만 받습니다. 예를 들어 `ISC_PASS_*`
   노드가 의사 NPC 제출 인터렉션을 켠 뒤 접속한 피어는 `actingNpcs`의 `enabled: false`만 복제받습니다.
3. 조건 기반 노출은 `patient_a_critical` 한 시나리오에만 코드 표(`PatientACriticalQuestStateFlags`)로
   존재합니다. `patient_b_c_ct`는 `SyncList`·`SyncVar`로, `tutorial`은 노드 토글로 각각 다른 방식을 씁니다.
4. 퀘스트 표시 바인딩(`presentationBindings`)이 아홉 개 인터렉션의 노출을 암묵적으로 결정합니다. 아이콘
   데이터가 게이트 역할까지 맡고 있어 작성자가 의도를 읽기 어렵습니다.
5. 인터렉션 식별자가 불완전합니다. `_interactionIdentifier`가 비어 있으면 완료 신호를 대신 쓰고,
   NPC 시나리오 인터렉트는 시나리오 식별자를 인터렉션 식별자로 씁니다. `assess_patient_a_triage`와
   더미 환자 D의 트리아지 액션은 소유 엔티티 식별자가 없습니다.
6. 장비 코드가 시나리오 식별자나 신호 이름을 직접 비교합니다(모니터의 `"patient_a_critical"`, 벽 흡인기의
   `connect_wall_component_1`).
7. `EntityTag` 노드가 부여하는 엔티티 태그는 서버에만 남고 복제되지 않습니다. 가변 엔티티를 태그로
   식별하려면 이 복제가 먼저 필요합니다.

### 사용자 경험 목표

- 콘텐츠 작성자: 시나리오 JSON의 `interactions` 구역만 읽으면 그 시나리오가 다루는 상호작용과 노출 조건을
  모두 알 수 있습니다. 그래프 진단이 정의되지 않은 주소, 조건이 없는 항목, 참조가 끊긴 퀘스트 바인딩을
  알려줍니다.
- 학습자: 어떤 피어로 접속했든, 언제 접속했든, 자기 역할과 퀘스트 단계에 맞는 상호작용만 보입니다.
  호스트가 켠 상호작용이 원격 클라이언트에서 안 보이는 일이 없습니다.
- 운영자·개발자: 에디터에서 플레이할 때 인스펙터로 레지스트리를 열어 특정 플레이어에게 어떤 항목이 왜 보이는지
  (어느 조건 절이 실패했는지) 확인하고, 필요하면 임시로 켜서 진행을 풀 수 있습니다.
- 다른 브랜치 작업자: 프리팹을 건드리지 않고 시나리오 JSON과 코드 리터럴만 옮기면 마이그레이션이 끝납니다.

### 제안

#### 1. 용어

| 용어 | 정의 |
|---|---|
| 인터렉션 주소 | `(엔티티 참조, 인터렉션 식별자)` 쌍. 유일성 범위는 엔티티 안입니다. 퀘스트 바인딩과 QuestMark가 쓰는 `(entityIdentifier, interactionIdentifier)`와 같은 개념입니다. |
| 엔티티 참조 | `id:<엔티티 식별자>` 또는 `tag:<엔티티 태그>`. 태그 참조는 판정 시점에 그 태그를 가진 모든 엔티티로 풀립니다. |
| 정의(Definition) | 표시 정보, 종류, 핸들러 결합 키, 완료 신호, 수행 후 가시성 처리, 초기 가시성, 가시성 조건을 담는 레코드입니다. |
| 핸들러 | 실제 동작을 수행하는 `IInteract` 구현체입니다. 기존 내부 클래스(`PatientAssessInteract` 등)가 그대로 핸들러가 됩니다. |
| 조건 절 | 관찰자 또는 전역 상태를 참·거짓으로 판정하는 데이터 단위입니다. 시나리오 진행 상태와 내재 능력 값을 모두 다룹니다. |
| 조건 상태 제공자 | 엔티티나 플레이어 코드가 조건 절에서 참조할 수 있도록 이름 붙인 값을 노출하는 인터페이스입니다. |
| 가시성 | 특정 플레이어(관찰자)에게 그 항목이 힌트 목록에 나타나는지 여부입니다. |

#### 2. 초기화 사이클의 정의

등록이 허용되는 구간을 두 가지로 정합니다. 구간 밖의 등록은 에디터 런타임에서 `Debug.LogWarning`을
남기고 정상 등록합니다(요구사항 2번).

1. 엔티티 초기화 사이클: 엔티티 컴포넌트의 `Awake` 이후 `OnStartServer`/`OnStartClient`가 끝날 때까지,
   그리고 `ISpawnedEntityIdentifierReceiver.ApplySpawnedEntityIdentifier`가 호출된 직후의 재등록까지입니다.
   이 구간에서 컴포넌트는 `IInteractionDefinitionSource.DeclareInteractions()`로 코드 리터럴 정의를
   등록합니다. EntityPresetSpawn 노드나 `actingNpcs`로 그래프 도중에 스폰된 엔티티도 자기 초기화
   사이클 안에서 등록하므로 경고 대상이 아닙니다.
2. 시나리오 초기화 사이클: `StartScenario`에서 그래프 로드, 퀘스트 include 로드, 시작 시 스폰되는
   `actingNpcs` 처리 다음, 첫 노드 실행 전까지입니다. 이 구간에서 컨트롤러는 그래프의 `interactions`
   구역을 레지스트리에 적용합니다. 대상 엔티티가 아직 없으면 정의를 보류 목록에 두고, 그 엔티티의 초기화
   사이클이 시작될 때 적용합니다(퀘스트 표시 서비스가 늦은 스폰을 처리하는 방식과 같습니다).

시나리오가 끝나면 시나리오 출처의 정의와 모든 가시성 상태를 정리하고, 코드 리터럴 정의는 초기 가시성으로
되돌립니다. 시나리오 재시작과 수동 진입점 진입도 같은 정리 절차를 거칩니다.

#### 3. 레지스트리

`MultiplayerInfrastructure.InteractableEntity.InteractionRegistry`(정적 서비스)를 추가합니다.

- 저장 단위는 인터렉션 주소별 항목이며, 항목은 정의, 핸들러 참조, 출처(코드 리터럴 또는 시나리오 식별자),
  등록 시각 구간(정상 또는 경고)을 가집니다.
- 초기 상태는 비어 있습니다. 씬 로드나 도메인 리로드 비활성 환경에서도 `RuntimeInitializeOnLoadMethod`로
  비웁니다.
- 시나리오 데이터 정의는 같은 주소의 코드 리터럴 정의 위에 병합됩니다. 병합 규칙은 "데이터가 명시한 필드만
  덮어쓴다"입니다. 핸들러 결합 키가 코드 리터럴에 없으면 데이터 정의는 핸들러 없는 항목이 되며 진단이 경고합니다.
  단, 종류가 `Action`, `Signal`, `ItemSubmission`, `StartScenario`인 정의는 범용 핸들러가 있으므로 코드 리터럴
  없이도 완결됩니다(현재 `ScenarioActionInteractable`, NPC 파생 인터렉트, `ScenarioInteractable`이 하던 일을
  범용 핸들러가 맡습니다).
- 조회 API: `TryGet(address)`, `GetForEntity(entityIdentifier)`, `Resolve(entityRef)`.
- 변경 이벤트: `DefinitionChanged`, `VisibilityChanged(address, playerIdentifier?)`. `PlayerController`가
  구독해 `RefreshInteractableHintsNow`를 호출합니다(플래그 풀과 퀘스트 표시 서비스가 이미 쓰는 방식입니다).
- `PlayerController.CollectAvailableInteracts`는 감지된 `IInteractable`의 각 `IInteract`에 대해 레지스트리 항목이
  있으면 가시성 판정(조건 절 전체, 6절)을 거칩니다. 레지스트리에 없는 `IInteract`는 마이그레이션 기간 동안 기존
  `IInteractorConditional` 경로로 처리하되, 에디터에서 경고합니다. 마이그레이션이 끝나면 힌트 노출 판정은
  레지스트리만 담당하고, `CanInteract`는 핸들러 실행 직전의 최종 방어 검사로만 남습니다.

#### 4. 코드 리터럴 정의

```csharp
public interface IInteractionDefinitionSource
{
  // 엔티티 초기화 사이클에서 한 번 호출된다. 반환 항목은 곧바로 등록된다.
  IEnumerable<InteractionDefinition> DeclareInteractions();
}

public sealed class InteractionDefinition
{
  public string EntityIdentifier;      // 소유 엔티티. 스폰 식별자 주입 뒤 다시 선언된다.
  public string InteractionIdentifier; // 엔티티 안에서 유일
  public string HandlerKey;            // 코드 핸들러 결합 키. 범용 종류는 null
  public InteractionKind Kind;         // Custom, Action, Signal, ItemSubmission, StartScenario
  public InteractionDisplay Display;   // 문구, 아이콘 식별자 목록, 색, 우선순위
  public string CompletionSignal;
  public InteractionAfterInteract AfterInteract; // None, HideForPlayer, HideForAll
  public bool InitialVisible;          // 기본 false
  public IReadOnlyList<InteractionCondition> VisibilityConditions;
  public InteractionConditionMatchMode MatchMode;
}
```

- `PatientController`는 `lift_from_bed`, `carry_patient`, `monitor_select`, `triage_assess`, 사정 동작,
  인지 확인, 정맥 라인, 수액 연결, 처치 물품 적용을 코드 리터럴로 선언합니다. 사정 동작의 표시 문구와
  요구 아이템처럼 지금 인스펙터에 있는 값은 `DefaultAssessActions`와 같은 코드 기본값으로 옮기고,
  시나리오마다 다른 문구와 조건은 시나리오 데이터가 덮어씁니다.
- `Npc`는 시나리오 데이터가 정의를 주므로 코드 리터럴을 선언하지 않습니다. 인스펙터의
  `_scenarioInteracts`, `_submissionInteracts`, `_customInteractSources`와 `NPCBaseModelSO`의 같은 목록은
  정의 출처에서 제외하고 구현 완료 시 제거합니다.
- 장비·설치물(`PatientMonitorController`, `IntravenousLineConnectionPoint`, `Level1RapidInfuserController`,
  `MovingPatientBedController`, `WallAttachedWallSuction`, `WallAttachedOxyflowmeter` 등)은 자기
  인터렉션을 코드 리터럴로 선언합니다. 기본값이 `InitialVisible = false`이므로, 시나리오와 무관하게 늘
  보여야 하는 항목(침대 조종, 모니터 설치 등)은 선언에서 명시적으로 `true`를 지정합니다.
- 기존 `IInteract` 구현체는 `IQuestPresentationTarget`이 제공하던 `PresentationEntityIdentifier`,
  `InteractionIdentifier`를 그대로 주소로 씁니다. 비어 있거나 완료 신호로 대신하던 곳은 명시적 식별자를 붙입니다.
- 식별자 명명 규칙: 소문자와 밑줄, 엔티티 안에서 유일, 완료 신호와 별개의 식별자. NPC의 시나리오 시작
  인터렉션도 시나리오 식별자가 아니라 고유 인터렉션 식별자(예: `start_disaster_intro`)를 씁니다.

#### 5. 조건 절과 조건 상태 제공자

조건 절은 `ScenarioCondition` 하나의 형식으로 통일하고, 인터렉션 가시성과 Validator 노드가 함께 씁니다.
코드가 판정할 수 있고 일반화할 수 있는 값은 모두 조건 절로 데이터에서 정의할 수 있어야 합니다(결정 3번).

| 종류 | 필드 | 판정 단위 | 비고 |
|---|---|---|---|
| `PlayerHasTag` | `tag` | 관찰자 | 역할 태그 |
| `PlayerHasQuestFlag` | `flag` | 관찰자 | 퀘스트 상태 플래그 풀 |
| `PlayerHasQuest` | `questIdentifier`, `state`(Active/Completed/Absent), `completionCriteriaIdentifier`(선택: 현재 진행 목표일 때만) | 관찰자 | `HasActiveInteractionBinding` 암묵 판정을 대체 |
| `PlayerHasItem` | `itemIdentifier`, `count`, `heldOnly` | 관찰자 | 인벤토리 보유·손에 든 상태 |
| `PlayerState` | `key`, `op`, `value` | 관찰자 | `PlayerController`가 제공자로 노출하는 값(예: `carrying`, `patientSelectionMode`, `ridingControl`) |
| `PlayerWithinDistance` | `entity`, `meters` | 관찰자 | 관찰자와 엔티티 사이 거리 |
| `SignalRaised` | `signal`, `negate` | 전역 | RuntimeState의 `sig.*` |
| `RegistryContains` | `registryType`, `identifier`, `negate` | 전역 | 기존 Validator 규칙과 동일 |
| `EntityHasTag` | `entity`, `tag` | 전역 | 7절 |
| `EntityState` | `entity`, `key`, `op`, `value` | 전역 | 엔티티가 제공자로 노출하는 값(예: `hasBed`, `cannulaInserted:left`, `connectionAvailable`, `recognition:patient_b_recognition_1`, `bcNurseDStage`) |
| `PlayerCount` | `op`, `value`, `tag`(선택) | 전역 | Validator의 PlayerCount 계열을 흡수 |
| `ScenarioActive` | `scenarioIdentifier`, `negate` | 전역 | 실행 중인 그래프 식별자 |
| `Group` | `matchMode`, `conditions`(중첩) | 상속 | AND/OR 중첩 |

조건 상태 제공자 규약:

```csharp
public interface IConditionStateProvider
{
  // 키는 소문자와 밑줄, 하위 구분은 콜론. 값은 bool, int, float, string 가운데 하나.
  bool TryGetConditionValue(string key, string qualifier, out ConditionValue value);
  IEnumerable<string> ConditionKeys { get; } // 에디터 진단과 인스펙터가 목록을 만드는 데 쓴다
}
```

- `PlayerController`, `PatientController`, 침대, 모니터, 정맥 라인 연결 지점, 급속주입기, 벽 설치물이 이
  규약을 구현해, 지금 `CanInteract`가 코드로 보던 값을 키로 노출합니다. 새 값이 필요하면 키를 추가하는 것으로
  끝나며, 조건 절 종류를 늘리지 않습니다.
- 값이 서버 권위 상태(SyncVar, SyncList, 연결 위상)를 바탕으로 해야 모든 피어에서 같은 판정이 나옵니다.
  제공자는 로컬 추정값을 노출하지 않습니다.
- 에디터 진단은 조건 절이 참조한 키가 대상 엔티티의 `ConditionKeys`에 없으면 경고합니다.

Validator 노드는 `rootConditions` 각 항목에 `conditions: ScenarioCondition[]`를 추가로 받습니다.
기존 `condition`, `playerTag`, `validationRules` 필드는 로더가 같은 의미의 조건 절로 변환하므로 기존 데이터는
그대로 동작합니다. 판정 엔진(`ScenarioConditionEvaluator`)은 하나이며, 가시성 판정과 Validator 게이트가
같은 엔진을 호출합니다. Validator의 관찰자 단위 조건은 기존 `playerScope`(Any/All/Owner)로 대상 플레이어
집합을 정합니다.

#### 6. 가시성 모델

항목 하나의 관찰자별 가시성은 다음 순서로 정합니다.

```
visible(player) =
  override(player)            // 트리거 오버라이드, 플레이어별
  ?? override(All)            // 트리거 오버라이드, 전역
  ?? (conditions.Count > 0 ? evaluate(conditions, player) : initial)
```

- 트리거 오버라이드가 있으면 조건보다 우선합니다. 오버라이드를 `Reset`하면 다시 조건 판정으로 돌아갑니다.
- 소비 완료(`consumeOnce`) 개념은 두지 않습니다(결정 2번). 수행 뒤 사라져야 하는 인터렉션은 정의의
  `afterInteract`로 명시합니다. `HideForPlayer`는 수행한 플레이어에게만, `HideForAll`은 전원에게
  `Hide` 오버라이드를 기록합니다. 이 오버라이드는 트리거 오버라이드와 같은 저장소와 복제 경로를 쓰고,
  `InteractionVisibility` 노드의 `Reset`이나 시나리오 재시작·수동 진입 정리에서 해제됩니다.
- 조건 판정은 각 피어가 자기 관찰자 기준으로 수행합니다. 입력이 되는 역할 태그, 퀘스트 상태 플래그,
  퀘스트 목록, 신호, 레지스트리 상태, 조건 상태 제공자 값은 모두 서버 권위로 복제되는 값입니다.
- 서버는 `Interact` 요청을 받을 때 같은 판정을 요청자 기준으로 다시 수행해 오래된 클릭을 거부합니다
  (현재 `IsCurrentlyAvailableInteract`가 로컬에서 하는 재검사의 서버 측 대응입니다). 거부되면 요청자의 힌트
  목록을 다시 계산하게 합니다.

트리거 방식은 두 진입점을 갖습니다.

1. 그래프 노드 `InteractionVisibility`
   ```json
   { "nodeType": "InteractionVisibility", "identifier": "IV_OPEN_STYLET",
     "operation": "Show",
     "targets": [ { "entity": { "id": "patient_a" }, "interaction": "remove_intu_stylet" } ],
     "playerScope": "ByTag", "playerTags": ["nurse_b", "nurse_a"], "tagMatchMode": "Any" }
   ```
   `operation`은 `Show`, `Hide`, `Reset`입니다. `playerScope`는 `All`, `Current`, `ByTag`입니다.
   이 노드는 서버에서만 상태를 기록하며, 호환 실행 경로에서 클라이언트가 실행하면 서버로 위임합니다
   (`PlayerQuestStateFlagService.Relay`와 같은 방식).
2. 코드 API `InteractionRegistry.SetVisible(entityRef, interactionId, InteractionVisibilityOverride, PlayerScope)`.
   `TriageScenarioEventBootstrap` 이벤트가 필요할 때 호출하되, 새 콘텐츠에서는 조건 방식을 우선합니다.

#### 7. 엔티티 참조와 태그

- 태그 저장소는 기존 `PlayerTagService` 하나만 씁니다(결정 5번). 이 서비스는 이미 임의의 식별자를 키로
  태그를 저장하므로(`AddTagToIdentifier`) 엔티티 태그도 같은 저장소에 둡니다. 별도의 엔티티 태그 서비스를
  만들지 않습니다.
- 복제는 기존 경로를 확장합니다. 지금은 키가 플레이어 소유자일 때만 `PlayerController`가 옵저버에게
  스냅샷을 보냅니다. 소유자가 없는 식별자(엔티티)의 태그 변경은 `ScenarioNetworkRelay`의 기존 상태 스냅샷
  경로(신호 스냅샷과 같은 요청·응답)에 태그 항목을 함께 실어 보냅니다. 새 서비스나 새 저장소를 추가하지 않고
  기존 서비스의 동기화 조건을 넓히는 변경입니다.
- 태그 부여 시점: (1) `EntityPresetSpawn.tags`(스키마 추가), (2) `EntityTag` 노드, (3) 엔티티 코드가 자기
  초기화 사이클에서 부여하는 고정 태그(예: `PatientController`가 `patient`와 환자 유형 태그를 부여), (4) 코드 API.
- `tag:` 참조는 정의 등록과 조건 판정 양쪽에서 쓸 수 있습니다. 정의 등록에서 태그 참조를 쓰면 그 태그를
  가진 엔티티가 초기화될 때마다 같은 정의가 적용됩니다.
- 플레이어 역할과 같은 방식으로, 스폰 식별자가 가변인 엔티티(예: 프리팹 식별자 `patient`가 런타임에
  `patient_b`로 바뀌는 환자)는 식별자 주입 직후 태그를 다시 평가합니다.

#### 8. 시나리오 데이터 정의(직렬화 구조)

시나리오 JSON 최상위에 선택 구역 `interactions`를 추가합니다. `actingNpcs[].interactions`는 이 구역으로
옮기고 `actingNpcs`에서는 제거합니다(결정 15번). 로더는 기존 파일의 `actingNpcs[].interactions`를 읽으면
같은 NPC 식별자를 가진 `interactions` 항목으로 변환하고 에디터에서 경고를 남기며, 저장 시에는 새 형식으로만
씁니다. 스키마의 `actingNpcs[].interactions`는 폐기 예정으로 표시합니다.

```json
"interactions": [
  {
    "entity": { "id": "patient_a" },
    "interaction": "assess_vital",
    "kind": "Custom",
    "display": { "text": "활력징후 사정", "priority": 0 },
    "completionSignal": "check_vital_patient_a",
    "afterInteract": "HideForPlayer",
    "visibility": {
      "initial": false,
      "matchMode": "All",
      "conditions": [
        { "type": "PlayerHasTag", "tag": "nurse_b" },
        { "type": "PlayerHasQuest", "questIdentifier": "Quest_Check_Vital_PatientA",
          "state": "Active", "completionCriteriaIdentifier": "assess-vital-patient-a" },
        { "type": "PlayerHasItem", "itemIdentifier": "vital_set", "count": 1 }
      ]
    }
  },
  {
    "entity": { "id": "npc-doctor-patient-a-critical" },
    "interaction": "submit_laryngoscope",
    "kind": "ItemSubmission",
    "display": { "text": "후두경 전달" },
    "itemSubmission": { "title": "후두경 전달", "submitButtonText": "전달",
                        "requiredItems": [ { "itemIdentifier": "laryngoscope", "count": 1 } ] },
    "completionSignal": "pass_laryngoscope",
    "afterInteract": "HideForAll",
    "visibility": {
      "conditions": [
        { "type": "PlayerHasQuest", "questIdentifier": "Quest_Intubation_PatientA",
          "state": "Active", "completionCriteriaIdentifier": "submit-laryngoscope-patient-a" }
      ]
    }
  },
  {
    "entity": { "tag": "cpr_target" },
    "interaction": "click_to_start_comp",
    "kind": "Action",
    "display": { "text": "가슴압박 수행", "priority": 900 },
    "completionSignal": "click_to_start_comp",
    "afterInteract": "HideForAll",
    "visibility": {
      "conditions": [
        { "type": "PlayerHasTag", "tag": "nurse_b" },
        { "type": "PlayerHasQuestFlag", "flag": "scen_a.cpr1_actions" }
      ]
    }
  }
]
```

- `kind`별 추가 필드: `Action`(`activateObjects`, `deactivateObjects`: 엔티티 하위 오브젝트 경로),
  `Signal`(추가 없음), `ItemSubmission`(`itemSubmission` 객체), `StartScenario`(`scenarioIdentifier`,
  `startNodeIdentifier`), `Custom`(`handlerKey`).
- 아이템 요구는 별도 필드가 아니라 `PlayerHasItem` 조건 절로 표현합니다. 수행 시 소비가 필요하면
  `consumeItems: [ { itemIdentifier, count } ]`를 정의에 둡니다.
- `display.priority`는 정수이고 기본값 0, 값이 클수록 먼저 표시됩니다. 현재 코드 표의 1000·900을 그대로
  데이터로 옮깁니다.

#### 9. 멀티플레이어 규약

| 항목 | 규칙 |
|---|---|
| 권위 | 정의 등록은 각 피어가 로컬로 수행합니다(정의는 결정적 데이터). 가시성 오버라이드(트리거와 `afterInteract`)와 태그는 서버 권위입니다. |
| 복제 | 오버라이드는 주소별 스냅샷으로 옵저버에게 복제합니다. 플레이어 범위 오버라이드는 `PlayerController`의 옵저버 동기화에 얹습니다(플래그 풀과 동일). 전역 오버라이드와 엔티티 태그는 `ScenarioNetworkRelay` 스냅샷에 얹습니다. |
| 늦은 접속 | 접속 완료 시 기존 `RequestRaisedSignalSnapshot` 경로를 확장한 상태 스냅샷 요청으로 전체 상태를 받습니다. 시나리오 정의는 그래프가 클라이언트에도 있으므로 로컬에서 다시 적용합니다. |
| 실행 모드 | 호환 경로: 노드는 모든 피어에서 실행되지만 상태 기록은 서버로 위임합니다. 서버 권위 경로: 서버만 실행하고 결과가 복제됩니다. 두 경로에서 결과가 같아야 합니다. |
| 전용 서버 | 관찰자가 없는 서버에서도 조건 판정 API가 관찰자 없음을 허용해야 하며, 플레이어 단위 조건은 지정 플레이어 식별자로 판정합니다. |
| 재접속 | 재접속한 플레이어의 플레이어 범위 오버라이드는 `UserDescriptor.Identifier` 기준으로 유지됩니다(역할 태그 복원과 같은 규칙). |

#### 10. 디버그 인스펙터

- Unity 에디터에서 플레이할 때 엔티티를 선택하면 인스펙터에 레지스트리 항목 목록이 나옵니다. 각 항목은 주소,
  출처, 초기 가시성, 조건 절과 관찰자별 판정 결과, 오버라이드를 보여줍니다.
- 편집은 Unity 에디터에서만 가능합니다(결정 10번). 빌드에서는 인스펙터가 존재하지 않으므로 편집 경로도
  없습니다. 에디터 안에서는 서버·클라이언트·오프라인 컨텍스트를 가리지 않고 편집을 허용하되, 클라이언트
  컨텍스트의 편집은 서버로 위임 요청을 보내고 그 사실을 표시합니다.
- 인스펙터의 변경은 레지스트리 런타임 상태에만 적용되고 프리팹이나 씬에 직렬화되지 않습니다.
- 마이그레이션 기간 동안 기존 컴포넌트의 인터렉션 관련 직렬화 필드는 `[Obsolete]`와 도움말 상자로 표시하고,
  값이 남아 있으면 `RegistryPreloaderValidationPanel`이 경고합니다. 구현 완료 시 필드를 제거합니다.

#### 11. 장비 코드의 시나리오 결합 해소 방안

전수조사 E-8 항목에 대한 방안입니다(결정 12번). 원칙은 "장비는 자기 상태를 이벤트와 조건 키로 노출하고,
시나리오가 어떤 상태를 어떤 신호와 가시성으로 잇는지는 데이터가 정한다"입니다.

| 결합 지점 | 현재 | 방안 |
|---|---|---|
| 환자 모니터 `detail_overlay`가 `"patient_a_critical"`과 `patient_a`를 비교해 닫기 신호 `close_vital_ui_a`를 무장 | 코드 하드코딩 | 모니터가 `IScenarioEntityStateEventSource` 이벤트 `VitalOverlayClosed`(키: 추적 환자 식별자)를 발생시키고, 시나리오가 `EntityStateSignalBinding` 노드로 `close_vital_ui_a`에 잇습니다. `ArmScenarioClose` 경로와 `VitalMonitorClose` 이벤트 파일은 제거합니다. |
| 벽 흡인기가 `_attachCompletionSignal == "connect_wall_component_1"`로 환자 A 설치 대상을 판정하고, 그때만 `patient_a_wall_suction`/`wall_suction_install` 주소와 양커 인터렉션을 노출 | 프리팹 값에 의한 분기 | 설치물의 엔티티 식별자는 `StaticObjectDisplayment.EntityIdentifier`로 이미 존재하므로 주소는 항상 `id:<설치물 식별자>`를 씁니다. 완료 신호와 양커 인터렉션의 가시성은 시나리오 `interactions`가 해당 설치물 식별자에 대해 선언합니다. 다른 설치물에는 정의가 없으므로 기본값 `initial: false`로 숨겨집니다. |
| 벽 산소유량계가 `IsRaised(조작 신호)`, `IsRaised(equipment_connected_oxyflowmeter_<환자>)`로 회수·조작 노출을 판정 | 신호 이름 규칙 하드코딩 | 유량계는 조건 키 `attached`, `flowSet`, `connectedPatient`를 제공자로 노출하고, 시나리오가 `SignalRaised`나 `EntityState` 조건 절로 노출을 정합니다. 조작 신호 이름은 `interactions`의 `completionSignal`로 옮깁니다. |
| 급속주입기 C라인 연결이 서버에서 `CLineOperatorRoleTag` 상수를 검사 | 코드 상수 | `PlayerHasTag` 조건 절로 데이터에 두고, 서버 측 `Interact` 재검사(6절)가 같은 조건을 다시 판정합니다. 코드 상수는 제거합니다. |
| 환자 A 물품별 사용 인터렉션이 `IsPatientAItemUseArmed`(환자 A + 시나리오 무장)로 통합 `item_apply`를 숨김 | 코드 하드코딩 | 두 인터렉션 모두 코드 리터럴로 선언하고 기본 숨김입니다. `patient_a_critical` 데이터가 물품별 인터렉션을 조건과 함께 켜고, `patient_b_c_ct` 데이터가 `item_apply`를 켭니다. 서로를 숨기는 코드는 제거합니다. |
| `PatientACriticalQuestStateFlags` 표, `RequiresActiveQuestBinding`, `GetInteractionDisplayPriority` | 코드 표 | 표의 각 행을 `interactions` 항목의 조건 절(`PlayerHasQuestFlag`, `PlayerHasQuest`)과 `display.priority`로 옮기고 클래스를 제거합니다. 플래그 상수와 `ArmFor`/`Disarm`의 플래그 풀 정리만 `TriageScenarioEventBootstrap`에 남깁니다. |
| `PatientBCRecognitionActivations`(문구, 역할, 마이크 허용) | 코드 표 | 문구와 역할 조건은 `interactions` 항목으로, 마이크 허용과 완료 신호는 인지 확인 `SyncList` 항목으로 남기되 그 값을 조건 키 `recognition:<신호>`로 노출합니다. 개방 이벤트는 `SyncList` 항목 활성만 담당합니다. |

#### 12. 검증과 진단

- 스키마: `interactions` 구역, `InteractionVisibility` 노드, `ScenarioCondition`(Validator 포함),
  `EntityPresetSpawn.tags`를 `scenario.schema.json`과 에디터 스키마에 추가하고 `actingNpcs[].interactions`를
  폐기 예정으로 표시합니다. `scenario-json-validation` 스킬로 기존 데이터 호환을 확인합니다.
- 그래프 진단: 퀘스트 바인딩·QuestMark·`InteractionVisibility`가 참조하는 주소가 코드 리터럴 카탈로그나
  `interactions` 구역에 없으면 경고합니다. 코드 리터럴 카탈로그는 에디터가 `IInteractionDefinitionSource`
  구현체를 스캔해 만듭니다. 조건 절이 참조한 조건 키가 제공자 목록에 없으면 경고합니다.
- 런타임 진단: 초기화 사이클 밖 등록, 핸들러 없는 정의, 조건도 오버라이드도 없는 `initial: false` 항목을
  에디터 런타임에서 경고합니다.
- 검증 패널: 프리팹·씬·ScriptableObject에 인터렉션 직렬화 필드 값이 남아 있으면 경고합니다.

#### 13. 마이그레이션 계획

전수조사 문서 E절의 분류를 따르며, 이 브랜치 안에서 7단계까지 모두 수행합니다(결정 7번).

1. 기반 추가(MultiplayerInfrastructure): 레지스트리, 정의 모델, `ScenarioCondition`과 판정 엔진, 조건 상태
   제공자 규약, Validator 변환, 태그 복제 확장, 노드와 DTO, 스키마, 복제 경로, 디버그 인스펙터, 진단.
   기존 동작은 바뀌지 않습니다.
2. 코드 리터럴 선언과 제공자 구현(TriageTrainer): 환자, 모니터, 정맥 라인, 급속주입기, 침대, 벽 설치물이
   정의와 조건 키를 선언합니다.
3. `patient_a_critical` 데이터 이전: 플래그 표와 `activate_*` 이벤트를 `interactions` 조건으로 옮깁니다.
   `actingNpcs[].interactions`를 `interactions`로 옮깁니다. 11절의 결합 해소를 함께 적용합니다.
4. `patient_b_c_ct` 데이터 이전: `PatientBCRecognitionActivations`와 처치 단계 조건을 데이터로 옮깁니다.
5. `tutorial`, `disaster_intro`: `ItemSubmissionConfig`의 `enabled`와 `NPCControl` 인터렉트 조작을
   `InteractionVisibility`로 바꿉니다. 미끼 오브젝트에 `DummyInteractTrainer` 식별자를 부여하고 정의를
   데이터로 옮깁니다(결정 14번). IndevScene과 테스트 씬의 데모 NPC 정의도 데이터로 옮깁니다.
6. 프리팹·씬·에셋 정리: `ScenarioActionInteractable`은 범용 `Action` 핸들러 컴포넌트로 남기되 직렬화
   필드를 제거하고, `_assessActions`, `_interactConfigs`, `_interactEntries`, NPC 목록, `NPCBaseModelSO`
   목록, `Interactable` 기반 표시 필드를 제거합니다.
7. 레거시 폐기(결정 13번): `Interaction` 노드, `NpcInteractControl` 노드, `ScenarioInteractable`,
   `InteractableEntityResolver.handlerSources`, `ItemSubmissionConfig`의 `enabled`, `NPCControl`의
   `interactOperation`을 제거합니다. 이들이 하던 "시나리오에서 인터렉션을 제어"하는 기능은
   `interactions` 정의(`StartScenario`, `ItemSubmission` 종류)와 `InteractionVisibility` 노드가 대신합니다.

### 자세한 달성 목표

- 시나리오 JSON 한 파일만으로 그 시나리오의 상호작용 목록, 문구, 노출 조건, 트리거 시점을 모두 재구성할
  수 있어야 합니다.
- 호스트, 원격 클라이언트, 늦게 접속한 클라이언트, 재접속한 클라이언트 네 경우에서 같은 플레이어가 보는
  상호작용 목록이 같아야 합니다.
- 조건 입력이 바뀌면(태그, 플래그, 퀘스트, 신호, 엔티티 태그, 조건 키 값) 다음 프레임 안에 힌트 목록이
  갱신되어야 합니다.
- 서버가 거부한 `Interact` 요청은 클라이언트 힌트를 즉시 다시 계산하게 해야 합니다.
- 프리팹 YAML에는 인터렉션 문구·조건·활성 상태가 남지 않아야 하며, 검증 패널이 0건을 보고해야 합니다.
- 기존 시나리오 JSON과 퀘스트 JSON은 수정 없이 로드되고, 새 구역이 없는 시나리오는 기존과 같이 동작해야 합니다.
- Validator 노드의 기존 조건 데이터가 새 조건 절로 변환되어 같은 판정 결과를 내야 합니다.

### 문서화

- `Documents/api-references/MultiplayerInfrastructure.InteractableEntity.md`: 레지스트리, 정의 출처, 초기화 사이클,
  `IInteractionDefinitionSource`, `IConditionStateProvider`, 가시성 판정 순서를 추가하고 §7 "새 인터랙터블
  추가 가이드"를 갱신합니다.
- `Documents/api-references/MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md`와
  `Documents/requirements/content-definitions/scenario/scenario-graph-spec.md`: `interactions` 구역,
  `InteractionVisibility` 노드, `ScenarioCondition` 형식, Validator 확장을 추가하고 폐기 노드를 표시합니다.
- `Documents/working-guide/features/scenario/interaction-signal-integration-spec.md`와
  `scenario-action-interactable-setup-guide.md`, `scenario-inline-acting-npc-guide.md`: 프리팹·`actingNpcs`
  설정 절차를 데이터 정의 절차로 바꿉니다.
- `Documents/requirements/gameplay/interaction/interaction-feature-spec.md`: IR-003·IR-004에 "정의는 코드
  리터럴 또는 시나리오 데이터에서만 온다"는 요구사항을 추가합니다.
- `Documents/requirements/content-definitions/scenario/patient_a_critical.md`의 2026-08-27 절에 후속 결정
  링크를 답니다.
- `Agents/Templates/Implement - Interactables.md`: 프리팹 중심 절차를 레지스트리 중심 절차로 바꿉니다.

### 가용성과 테스트

- 위험도: 중간. 힌트 목록 수집 경로(`CollectAvailableInteracts`)에 판정 단계가 하나 늘고, 새 복제 경로가
  추가되며, Validator 판정 엔진이 교체됩니다. 마이그레이션 2단계까지는 레지스트리가 비어 있어도 기존 경로가
  그대로 동작하도록 설계합니다.
- EditMode 테스트 추가 대상
  - 레지스트리: 초기화 사이클 안팎 등록의 경고 여부, 시나리오 종료 시 정리, 코드 리터럴과 데이터 병합 규칙,
    `actingNpcs[].interactions` 변환.
  - 조건 평가: 절 종류별 판정, `Group` 중첩, 관찰자 없는 판정, 조건 키 조회, Validator 기존 데이터 변환 동치.
  - 가시성 합성: 플레이어별·전역 오버라이드 우선순위, `Reset`, `afterInteract` 두 단위.
  - DTO 왕복: `interactions`, `InteractionVisibility`, `ScenarioCondition`, `tags` 저장·로드·저장 보존.
  - 진단: 정의되지 않은 주소 참조, 핸들러 없는 정의, 미등록 조건 키.
- 검증 절차(결정 16번): 에이전트가 EditMode 테스트와 MPPM 다중 클라이언트 PlayMode로 환자 A 전체, 환자 B/C
  전체, 튜토리얼을 먼저 검증한 뒤, 사용자가 수동으로 검토합니다. 검증 항목에는 늦은 접속 복원, 서버 거부 후
  힌트 재계산, 역할별 노출 분리가 포함됩니다.
- 기존 테스트(`PatientAInteractionOrderingTests`, `PatientTreatmentInteractionTests`, `QuestSignalScopeDataTests`,
  `QuestPresentationTests`)는 마이그레이션 3단계에서 데이터 기준으로 다시 씁니다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표
  1. 두 모듈의 프리팹·씬·ScriptableObject에서 인터렉션 문구·조건·활성 필드가 0건입니다(검증 패널 기준).
  2. `patient_a_critical`과 `patient_b_c_ct`의 상호작용 개방 이벤트(`activate_*` 26개)가 데이터 조건으로
     대체됩니다. 남는 코드 이벤트는 엔티티 상태 변경만 담당합니다.
  3. 늦은 접속 시나리오에서 제출 인터렉션 미노출 문제가 재현되지 않습니다.
  4. `PatientACriticalQuestStateFlags`, `PatientBCRecognitionActivations`, 장비의 시나리오 식별자·신호 이름
     비교 코드가 제거됩니다.
- 수용 기준
  1. 기존 시나리오 15개가 스키마 검증과 로드를 통과하고, Validator 판정이 이전과 같습니다.
  2. 위 EditMode 테스트가 모두 통과합니다.
  3. 세 시나리오의 MPPM 검증과 사용자 수동 검토를 통과합니다.

### 결정 기록 (2026-09-05)

| 번호 | 항목 | 결정 | 반영 위치 |
|---|---|---|---|
| 1 | 트리거 판정 단위 | 플레이어별과 전역 두 층 | 6절 |
| 2 | 소비 완료 | `consumeOnce` 명세 제거. 수행 후 비가시 전환을 `afterInteract`로 명시하고 플레이어별·전역 두 단위로 처리 | 4절, 6절, 8절 |
| 3 | 내재 능력 조건 | 코드가 판정할 수 있는 값은 모두 조건 상태 제공자와 조건 절로 일반화해 데이터에서 정의 | 5절 |
| 4 | 조건 모델 공유 | 수용. Validator 구현도 같은 판정 엔진으로 일반화 | 5절 |
| 5 | 엔티티 태그 복제 | 별도 서비스 복제 거부. 기존 태그 서비스 하나를 유지하고 동기화 조건만 확장 | 7절 |
| 6 | 기본 가시성 | `initial: false`를 기본값으로 | 4절, 6절 |
| 7 | 프리팹 필드 제거 시점 | 이 브랜치에서 구현 완료 시 즉시 제거 | 13절 6단계 |
| 8 | 퀘스트 바인딩과 가시성 | 조건 절(`PlayerHasQuest`)로 전환, 암묵 판정 제거 | 5절, 11절 |
| 9 | 표시 우선순위 | 수용. `display.priority`로 이전 | 8절 |
| 10 | 디버그 인스펙터 | Unity 에디터에서만 편집 가능 | 10절 |
| 11 | 식별자 명명 규칙 | 수용 | 4절 |
| 12 | 장비 결합 해소 | 방안 제안 요청에 따라 결합 지점별 방안 작성 | 11절 |
| 13 | 레거시 | 구현 완료 시 폐기. 시나리오 데이터로 같은 제어가 가능해야 함 | 13절 7단계 |
| 14 | 씬 배치 오브젝트 식별자 | 튜토리얼 미끼 인터렉션에 `DummyInteractTrainer` 식별자 사용 | 13절 5단계 |
| 15 | `actingNpcs[].interactions` | `interactions` 구역으로 이전하고 `actingNpcs`에서 제거 | 8절 |
| 16 | 검증 범위 | 환자 A 전체, B/C 전체, 튜토리얼. 에이전트 검증 뒤 사용자 수동 검토 | 가용성과 테스트 |

결정 14번의 식별자는 `DummyInteractTrainer`를 엔티티 식별자 접두사로 해석해 세 오브젝트에
`DummyInteractTrainer/parcel_8909`, `DummyInteractTrainer/mail_kim_gangsan`, `DummyInteractTrainer/delivery_overnight_youth`
형태로 부여할 계획입니다. 이 해석이 의도와 다르면 구현 전에 알려 주십시오.

### 구현 기록 (2026-09-06)

명세와 다르게 구현했거나 명세에 없던 결정입니다.

- 환자의 들어올리기(`lift_from_bed`)·업기(`carry_patient`) 인터렉션도 `IQuestPresentationTarget`으로 주소를 드러내
  레지스트리에 선언했습니다. 들어올리기는 어느 시나리오도 쓰지 않으므로 기본 숨김, 업기는 항상 노출입니다.
- `NPCControl` 노드의 `interactOperation`/`interactableIdentifier`/`interactEnabled`와 함께 `resultStateKey`도
  제거했습니다. `Read` 연산이 없어져 기록할 값이 없습니다. 중계기의 NPC 제어 RPC도 표시 이름만 나릅니다.
- 실행 시 아무 동작도 하지 않던 `Interaction` 노드를 삭제했습니다. `disaster_intro`의 `I001`/`I002`와 예시 그래프의
  노드는 앞뒤를 직접 연결했습니다.
- `ScenarioInteractable` 컴포넌트를 삭제했습니다(씬·프리팹 사용처 없음). `EntityType.ScenarioInteractable`
  열거값은 `ItemSubmissionInteractable`이 아직 쓰므로 남겼습니다.
- `InteractableEntityResolver.handlerSources`와 `Npc._customInteractSources`, 그 Add/Remove API를 제거했습니다.
  프리팹·씬의 빈 목록 필드도 지웠습니다.
- 편집기 테스트 중 삭제된 형식을 참조하던 것은 시나리오 데이터와 조건 판정기 기준으로 다시 썼습니다
  (`PatientACriticalIssueFixTests`, `PatientAInteractionOrderingTests`, `PatientADoctorAnimationTests`,
  `PatientRuntimeReferenceResetTests`, `PatientBCScenarioDataTests`, `QuestPresentationTests`).
- 튜토리얼 씬의 미끼 컴포넌트는 `_entityIdentifier`(`DummyInteractTrainer/parcel_8909`,
  `DummyInteractTrainer/mail_kim_gangsan`, `DummyInteractTrainer/delivery_overnight_youth`)만 남기고 문구·대사
  필드를 지웠습니다. 문구는 전역 카탈로그 `Resources/Interactions/global.json`에 있습니다.
- `Assets/Modules/UnitySceneSupports/IngameScene/Scripts/IngameSceneBootstrapper.cs`의 주석은 사라진
  `ScenarioActionInteractable` 이벤트를 언급하지만, 허용 모듈 밖이라 손대지 않았습니다.
- `IntravenousLineConnectionPoint`의 `InteractConfig`/`_interactConfigs`(프리팹·씬 11개 파일)를 제거했습니다. 플레이어가 직접
  수액 줄을 잇는 두 인터렉션은 코드에서 항상 잠겨 있어 직렬화 값이 효력이 없었으므로, 레지스트리 선언 없이
  `IInteractionRegistryExempt`로 두고 잠금만 코드 리터럴로 남겼습니다. 이를 반사로 끄던
  `DisableLegacyPatientAYankauerIvPort`도 지웠습니다.
- 변경 노트: `Documents/changes/2026-09-06-interaction-registry-visibility.md`.

### 링크, 참고사항

- 전수조사: [`interaction-inventory.md`](./interaction-inventory.md)
- 선행 결정: `Documents/requirements/content-definitions/scenario/patient_a_critical.md`의
  "2026-08-27 상호작용 개방을 퀘스트 상태 플래그 풀로 이전", "2026-08-27 처치 물품 적용 상호작용의 노출 기준 정정"
- 관련 제안: `Agents/Proposals/done/2026-08-19-quest-related-interaction-npc-indicators.md`,
  `Agents/Proposals/done/2026-07-26-runtime-interaction-state-hardening`,
  `Agents/Proposals/scheduled/2026-08-14-scenario-trigger-zone-active-gating`,
  `Agents/Proposals/2026-09-05-scenario-gate-skip-command`
- 핵심 코드: `Scripts/InteractableEntity/IInteract.cs`, `Scripts/Player/PlayerController.Interactables.cs`,
  `Scripts/Quest/PlayerQuestStateFlagService.cs`, `Scripts/Tag/PlayerTagService.cs`,
  `Scripts/Scenario/ScenarioController.cs`(NPCControl, ItemSubmissionConfig, NpcInteractControl, Validator 실행부),
  `TriageTrainer/Scripts/Scenario/PatientACriticalQuestStateFlags.cs`,
  `TriageTrainer/Scripts/Scenario/ScenarioActionInteractable.cs`,
  `TriageTrainer/Scripts/Patient/PatientController.Interactions.cs`
- 스키마: `Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json`
