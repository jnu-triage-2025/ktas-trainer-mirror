# API 레퍼런스: `MultiplayerInfrastructure.Scenario.Requirements`

## 목적

`MultiplayerInfrastructure.Scenario.Requirements`는 scenario graph의 외부 의존성을 canonical manifest로
컴파일하고, sidecar 선언 및 Editor/build/runtime provider snapshot에 대해 같은 계약을 검증한다.

## 주요 API

| 타입 | 역할 |
|---|---|
| `ScenarioRequirementCompiler` | graph와 선택 sidecar를 `ScenarioRequirementManifest`로 컴파일한다. |
| `ScenarioRequirementManifest` | requirement descriptor와 안정 정렬된 diagnostic을 보관한다. |
| `ScenarioRequirementKey` | `(kind, trimmed identifier)`의 Ordinal identity다. |
| `ScenarioRequirementsLoader` | strict JSON으로 sidecar/candidate를 읽는다. |
| `ScenarioRequirementValidationEngine` | scene 또는 runtime provider snapshot을 검증한다. |
| `ScenarioRuntimeRequirementsValidator` | runtime manifest discovery와 readiness/abort 판정을 수행한다. |
| `ScenarioRequirementsCandidatePreviewService` | AI 후보를 비변경 preview로 검토하고 승인 문서를 만든다. |

## 호환성과 정책

`ScenarioRequirementsCollector`와 `ScenarioRequirementsChecker`는 기존 Preflight API를 유지하는 facade다.
sidecar가 없으면 inferred compiler를 사용한다. `ReportOnly`는 결과만 기록하며,
`AbortScenarioStart`는 검증 실패 시 새 scenario 시작을 막는다.

공용 MultiplayerInfrastructure 코드는 TriageTrainer 타입이나 prefab을 직접 참조하지 않는다. 프로젝트
별 공급과 생성은 capability provider/factory 등록으로 연결한다.

## 관련 문서

- [요구사항](../requirements/scenario/scenario-ingame-requirements-support.md)
- [운영 가이드](../working-guide/features/scenario/scenario-ingame-requirements-support-guide.md)
