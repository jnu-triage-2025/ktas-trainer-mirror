# Scenario Ingame Requirements 구현 진행 현황

- 기준 기획: `Feature Proposal - Scenario Ingame Requirements Support.md`
- 점검일: 2026-07-16
- 브랜치: `feat/scenario-implementation-support`

## Phase 대조

| Phase | 구현 근거 | 상태 | 남은 완료 증거 |
|---|---|---|---|
| 0 | scenario schema 보강, strict duplicate-property/schema validator, runtime lookup registry | 구현됨 | Unity에서 기존 scenario fixture schema 회귀 실행 |
| 1 | `Scripts/Scenario/Requirements/Compilation`, canonical model, legacy Preflight facade | 구현됨 | 모든 node extraction snapshot EditMode 실행 |
| 2 | requirements/candidates schema, loader/writer, merge, candidate preview | 구현됨 | invalid/conflict/stale fixture EditMode 실행 |
| 3 | scene binding, scene/resource scanner, validation engine, requirements window | 구현됨 | 열린/닫힌 additive scene parity EditMode 실행 |
| 4 | factory registry, waypoint factory, apply planner/service, Triage migration | 구현됨 | Preview/Apply/Undo/Redo와 manual object 보존 Unity 실행 |
| 5 | composition profile, read-only build validator, stable build report | 구현됨 | clean checkout batch build와 변경 파일 없음 확인 |
| 6 | runtime manifest registry, provider snapshot, readiness gate, controller integration | 구현됨 | host/server/client 및 readiness timeout PlayMode 실행 |

## 버그 수정 (리뷰 후속)

리뷰에서 확인된 수용 기준 위반/버그를 수정했다.

- H1/H2: scene scanner가 `Interactable`/`SpawnPoint`/`Entity`를 `Complete`로 표시하고, static
  catalog가 scene-complete kind를 `Partial`로 강등하지 못하게 했다. 이제 Editor/build scene snapshot이
  이 kind들의 duplicate를 정본으로 판정한다(수용 기준 4).
- H3: `ScenarioRequirementSceneCapabilityMap`을 단일 source of truth로 도입해 Editor scanner, scene
  binding inference, runtime provider snapshot의 capability를 통일했다(수용 기준 11).
- H4: offline/single-player에서 `IsHost`가 항상 true가 되어 `HostOnly`가 오작동하던 문제를 수정했다.
- H5: build validator가 malformed sidecar에서 early return 하지 않고 inferred fallback으로 나머지
  검사를 계속 수행한다(수용 기준 9).
- M1: `WrongScene`/`Inactive`를 Development에서도 Error로 승격한다(§13).
- M2: Authoring profile에서 구조 진단(SIR501/502/504/506)이 빌드를 강제 실패시키지 않는다(§14).
- M3/L2: 부트 씬이 `MarkReady`에서 no-op 되던 문제를 수정했다.
- M4: fingerprint 일치하지만 invalid한 runtime manifest가 inferred fallback으로 우회되지 않고
  `SIR614`로 차단된다(§14/§15).
- M5/M6/M7: Apply의 삭제 승인/orphan 가드를 일치시키고, orphan 재요구 시 중복 생성 대신 재적용,
  수동 자식이 있는 orphan은 자동 삭제하지 않도록 했다(수용 기준 8, §12).
- M8: import와 compile 간 진단 code를 통일했다(impossible cardinality=`SIR204`,
  binding semantics=`SIR109`, suppression 검증은 compile phase로 일원화).

## 이번 보완

- runtime sidecar 선택을 `(scenarioIdentifier, graph ContentFingerprint)` exact match로 변경했다.
  같은 identifier의 변경된 graph가 오래된 sidecar declaration을 사용하는 일을 방지한다.
- strict runtime mode는 `NotReady` provider 상태에서도 새 scenario 시작을 차단한다.
- test source를 Editor assembly 아래로 배치했다. 현재 프로젝트에는 first-party runtime asmdef가 없어
  별도 test asmdef가 `Assembly-CSharp` runtime code를 참조할 수 없고, 이전 `Tests/EditMode` 위치는
  Unity가 생성한 C# project에 포함되지 않았다.
- compiler, serialization, runtime validation engine EditMode test를 추가했다.
- 요구사항, 운영 가이드, API 레퍼런스와 각 문서 색인을 추가했다.

## 검증 현황

- `git diff --check`: 통과
- requirements schema JSON 파싱: 통과
- 새 문서 링크: 통과
- Unity 6000.2 batch compile: 통과
- 통합 requirements validation command: 통과
- EditMode Test Runner: 11/11 통과 (`/private/tmp/ktas-requirements-editmode-final.xml`), strict runtime abort와
  runtime fallback parity 포함
- Build Validator batch 실행: 오류 0, 경고 9 (profile 미설정 상태의 예상 경고)
- PlayMode Test Runner: runtime smoke 1/1 통과 (`/private/tmp/ktas-requirements-playmode-results.xml`)

추가적인 운영 환경 검증이 필요한 경우 다음 순서로 실행한다.

1. Unity batch compile 및 EditMode/PlayMode requirements test를 실행한다.
2. `Tools > Multiplayer Infrastructure > Validate Scenario Requirements Phase 0-4`를 실행한다.
3. Production composition profile로 build validator를 실행하고 report의 deterministic/read-only 조건을 확인한다.
4. runtime `ReportOnly`와 `AbortScenarioStart`를 host, dedicated server, client에서 검증한다.
