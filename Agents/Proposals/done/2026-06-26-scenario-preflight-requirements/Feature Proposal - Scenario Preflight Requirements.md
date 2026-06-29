# Feature Proposal: 시나리오 사전 요구사항 검증(Preflight Requirements Check)

- 작성일: 2026-06-26
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`
- 관련 제안: `Agents/Proposals/done/2026-06-25-scenario-validator-gate-timeout/`
- 관련 명세: `Documents/requirements/scenario/scenario-runtime-validation-requirements.md`,
  `Documents/requirements/content-definitions/scenario/interaction-signal-integration-spec.md`

### 개요

`시나리오 사전 요구사항 검증(Preflight)` 기능은 시나리오를 **시작하기 직전에**, 해당
시나리오 그래프가 요구하는 씬/레지스트리 요소(인터랙션 타깃, 트리거존, 이벤트 핸들러,
아이템, 웨이포인트, 엔티티 프리셋 등)가 현재 실행 환경에 **준비되어 있는지 한 번에 점검**하고,
누락이 있으면 운영자에게 경고하는 안전장치입니다.

- 요약: `StartScenario` 직전에 그래프를 정적으로 훑어 "이 시나리오가 필요로 하는 것"의 목록을
  뽑고, 각 항목이 레지스트리/씬에 존재하는지 검사한다. 누락 시 콘솔과 인게임 채팅으로 경고한다.
- 의도/목표: IndevScene 같은 개발 씬에서 인터랙션/트리거 오브젝트가 없거나 미등록일 때,
  "왜 시나리오가 순식간에 지나가는가 / 왜 게이트에서 멈추는가"를 **시작 시점에 즉시 진단**할 수
  있게 한다. 미배선 상태를 조용히 통과하던 기존 동작을 가시화한다.
- 주요 맥락: 변환된 Validator 게이트는 `waitForCondition=true` 로 신호를 기다리는데, 그 신호를
  올려줄 인터랙션/트리거 오브젝트가 씬에 없으면 게이트가 영구 대기하거나(타임아웃 정책에 따라)
  통과한다. 어느 쪽이든 원인이 "씬 미배선"임을 운영자가 알기 어렵다. Preflight 가 이 간극을 메운다.
- 기술적 제약: `MultiplayerInfrastructure` 는 타 프로젝트 재사용 전제이므로 신중히 변경한다.
  Preflight 는 **순수 추가 기능**이며 기본 동작(경고 후 계속 진행)은 기존 "warn-and-continue"
  철학과 일치한다. 정적 분석만 수행하고, 불확실한 항목은 실패가 아닌 정보성으로 처리한다.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자/개발자**로서, 시나리오를 실행하기 전에 "이 시나리오가 정상
동작하려면 씬에 무엇이 준비되어 있어야 하는지"와 "지금 무엇이 빠졌는지"를 한눈에 확인하고 싶다.
왜냐하면 개발 씬(IndevScene)에는 실제 인터랙션 오브젝트/트리거존/아이템이 없어서, 시나리오가
의도와 다르게 흘러가도(순식간에 통과하거나 게이트에서 멈춤) 그 원인이 "씬 미배선"임을 알기
어렵기 때문이다.

현재 엔진은 미배선 요소를 만나면 노드별로 조용히 경고만 남기거나 통과해버리며, **시작 시점에
전체 요구사항을 종합 점검하지 않는다.** 그래서 운영자는 로그를 일일이 뒤져야 원인을 추정할 수
있다.

### 사용자 경험 목표

- 운영자/개발자: 시나리오 시작 즉시 "누락된 요구사항 N개" 요약과 항목별 상세(무슨 노드가 무슨
  식별자를 요구하는데 어디에서 못 찾았는지)를 콘솔과 인게임 채팅에서 바로 본다.
- 학습자: 정상 배선된 본 게임 씬에서는 경고가 없고 동작 변화도 없다.
- 운영 정책 선택: 누락이 있어도 (a) 경고 후 계속 진행(기본) 또는 (b) 시나리오 시작 중단 중
  하나를 선택할 수 있다. 경고 출력 채널(콘솔/인게임챗)도 각각 켜고 끌 수 있으며 기본은 둘 다 켬.

### 제안

`MultiplayerInfrastructure.Scenario` 에 다음을 추가한다.

1. `ScenarioRequirement` / `ScenarioRequirementKind` — 그래프에서 추출한 "요구 항목" 1건을 표현
   (어떤 노드가, 어떤 종류의, 어떤 식별자를 요구하는지).
2. `ScenarioRequirementsCollector` — `ScenarioGraph` 를 훑어 요구 항목 목록을 만든다(정적, 부작용 없음).
3. `ScenarioRequirementsChecker` — 각 요구 항목을 레지스트리/씬 기준으로 검사해 "충족/누락/불확실"을
   판정하고 결과 리포트를 만든다.
4. `ScenarioPreflightPolicy` — 경고 채널 토글(콘솔/인게임챗, 기본 둘 다 켬)과 누락 시 행동
   (ContinueWithWarning(기본) / AbortStart)을 담는 정책 값.
5. `ScenarioController` 에 직렬화 옵션(`_preflightEnabled`, `_preflightPolicy`)과
   `StartScenario` 진입부의 호출 훅을 추가한다.

검사 대상(요구 종류)과 판정 기준:

| 요구 종류 | 출처 노드 | 검사 방법 | 누락 시 |
| --- | --- | --- | --- |
| InteractionTarget | `Interaction.TargetIdentifier` | `Registry.TryGetEntity` 로 씬 엔티티 존재 확인 | 경고 |
| EventHandler | `InvokeEvent.EventIdentifier`, `Interaction/CombineItem.CompletionCondition/Output` | `ScenarioEventIdentifierRegistry.TryGetHandler` | 정보성 경고(선택적 핸들러일 수 있음) |
| Waypoint | `QuestWaypointHighlight.WaypointIdentifier` | `Registry.Contains(Waypoint)` 또는 `WaypointAnchor.TryGet` | 경고 |
| EntityPreset | `EntityPresetSpawn.PresetIdentifier` | `Registry.TryGetEntityPreset` | 경고 |
| Item | `CombineItem.InputItemIdentifiers/OutputItemIdentifier` | `Registry.TryGetEntity`(ItemObject) 등 best-effort | 정보성 경고 |
| Signal(참고) | `Validator(RegistryContains, RuntimeState, sig.*)` | 정적으로 소스 추적 불가 → "정보성"으로만 보고 | 정보성 |

- Signal(`sig.*`)은 런타임에 게임플레이가 올리는 값이라 정적 점검이 불가능하다. 대신 그 신호를
  올릴 가능성이 높은 InteractionTarget 의 존재 여부로 간접 점검하고, 신호 자체는 정보성으로만
  나열한다(누락 카운트에 포함하지 않음).
- 검사 결과 `누락(Missing)`이 1건 이상이고 정책이 `AbortStart` 면 시작을 중단한다(경고 후 return).
  `ContinueWithWarning` 이면 경고만 하고 정상 진행한다(기본값, 하위호환).
- 경고는 항목을 묶어 1개의 요약 + 항목별 라인으로 출력한다. 콘솔은 `Debug.LogWarning`,
  인게임챗은 기존 `AppendSystemChatMessage` 경로를 재사용한다.

### 자세한 달성 목표

- 시나리오 시작 시 누락 요약이 콘솔+채팅에 1회 출력된다(중복 없이).
- 정책으로 경고 채널을 개별 토글할 수 있고 기본은 둘 다 켜짐이다.
- 정책으로 누락 시 중단/계속을 선택할 수 있고 기본은 계속(경고만)이다.
- Preflight 비활성화 시 기존과 100% 동일하게 동작한다.
- 개발 씬을 위한 보조 유틸(아래)로, 누락 InteractionTarget/TriggerZone 을 간이 오브젝트
  (Cylinder/넓은 트리거 Cube)로 즉석 생성해 흐름을 끝까지 시연할 수 있다.

### 개발 씬 보조 유틸(별도 컴포넌트)

`ScenarioDevStubSpawner`(개발 편의 컴포넌트, MI 또는 TriageTrainer 측):
- 대상 시나리오 그래프의 요구 항목을 수집해, 씬에 없는 InteractionTarget 은 **Cylinder**(인터랙터블),
  TriggerZone 은 **넓고 납작한 통과형 Cube(isTrigger)** 로 생성한다.
- 생성물은 식별자 그대로 레지스트리에 등록되어 Validator 신호가 올라올 수 있게 한다.
- 생성 오브젝트는 간격을 두고 정렬 배치하고, ContextMenu/인게임 명령으로 생성·정리할 수 있다.
- 본 게임 씬 오염을 막기 위해 개발 씬 전용으로만 사용한다(기본 비활성, 명시적 트리거 필요).

### 가용성과 테스트

- 위험: Preflight 가 오탐(있는데 없다고 판정)하면 운영자가 혼란할 수 있음 → 불확실 항목은
  "정보성"으로 분리하고 누락 카운트에서 제외해 보수적으로 판정한다.
- 위험: MI 모듈 변경 → 신규 타입 추가 + `StartScenario` 의 비침습적 훅(옵션 기본 안전값)으로
  하위호환을 보장한다.
- 테스트: IndevScene 에서 `disaster_intro` 실행 시 누락 목록이 출력되는지, 본 게임 씬에서는
  경고가 없는지, 정책 토글이 동작하는지 수동 확인. 가능하면 Collector 의 정적 추출에 대한
  EditMode 단위 테스트 추가.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: IndevScene 에서 `disaster_intro` 시작 시 "왜 흐름이 비정상인지"를 추가 로그 분석
  없이 시작 경고만으로 파악할 수 있다.
- 수용 기준:
  - Preflight 활성 + 미배선 씬 → 시작 시 누락 요약/상세가 콘솔·채팅에 출력.
  - 정책 `AbortStart` → 누락 시 시나리오가 시작되지 않음.
  - 정책 채널 토글 → 해당 채널에만 출력.
  - Preflight 비활성 → 기존과 동일.

### 링크, 참고사항

- `Documents/requirements/scenario/scenario-preflight-requirements.md` (사용자용 요구사항 문서)
- `Documents/requirements/content-definitions/scenario/indev-scene-verification-guide.md`
- `Documents/requirements/scenario/scenario-runtime-validation-requirements.md`
