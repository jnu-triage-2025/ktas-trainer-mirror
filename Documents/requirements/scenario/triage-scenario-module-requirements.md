---
title: "TriageTrainer Scenario 모듈 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

TriageTrainer Scenario 모듈은 트리아지 교육 시나리오를 실제 장면 동작으로 연결하는 확장 계층이다. 사용자에게는 환자 이동, 모니터 변화, 체크리스트 UI, 처치 단계 전환이 시나리오 흐름에 맞게 자연스럽게 실행되어야 한다.

## 상세

- 모듈은 Intro/PatientA와 PatientB/C 흐름을 이벤트 단위로 분리해 유지보수 가능한 구조를 제공해야 한다.
- 시나리오 이벤트 등록/해제는 수명주기(`OnEnable`/`OnDisable`)에 맞춰 자동 처리되어야 한다.
- Inspector 수동 연결이 부족한 경우 Registry/alias 기반 자동 해석으로 런타임 참조 복구를 시도해야 한다.
- 운영자는 ContextMenu 검증(`Validate Event Wiring`, `Run Core Smoke Test`, `Log Registry Snapshot`)으로 배포 전 연결 상태를 확인할 수 있어야 한다.
- 모듈은 환자 모니터 파라미터, UI 패널 표시/숨김, 오브젝트 이동/배치, 애니메이터 상태 토글을 이벤트 핸들러에서 조합해 연출해야 한다.

## 기술적 세부 사항

- 핵심 구현은 partial class 집합으로 구성된다:
  - `TriageScenarioEventBootstrap.cs` (공통 수명주기, 참조 해석, 유틸리티, 검증 도구)
  - `TriageScenarioEventBootstrap.IntroAndPatientAEvents.cs`
  - `TriageScenarioEventBootstrap.PatientBCEvents.cs`
  - 다수의 `TriageScenarioEventBootstrap.Event.*.cs` 파일(세부 이벤트 핸들러)
- `RegisterScenarioGraphs()`는 `TextAsset` 그래프를 `RegistryType.ScenarioGraph`에 등록한다.
- `ResolveRuntimeReferencesIfNeeded()`는 Entity/Npc Registry 조회, alias 매칭, `GameObject.Find` fallback 순으로 참조를 해석한다.
- `RunCoreSmokeTestRoutine()`는 핵심 이벤트를 순차 호출해 기본 런타임 경로를 빠르게 검증한다.
- 모니터 적용 로직은 `PatientMonitorController`와 `ECGParameters`를 결합해 단계별 활력징후 변화를 반영한다.

## 참조

- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:event-registry](../content-definitions/scenario/event-registry.md)
- [api:event-handler-file-index](../content-definitions/scenario/event-handler-file-index.md)
