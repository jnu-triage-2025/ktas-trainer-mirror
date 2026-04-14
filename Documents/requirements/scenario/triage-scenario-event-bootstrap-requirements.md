---
title: "TriageScenarioEventBootstrap 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

TriageScenarioEventBootstrap은 트리아지 시나리오 이벤트를 실제 게임 오브젝트 동작으로 연결하는 기능이다. 사용자에게는 시나리오 단계별 연출(UI, 이동, 상태 변화)이 실제로 실행되는 핵심 경로다.

## 상세

- 시스템은 시나리오 시작 시 필요한 이벤트 식별자를 일괄 등록해야 한다.
- 시나리오 종료/비활성 시 등록된 핸들러를 누수 없이 해제해야 한다.
- Inspector 수동 연결과 Registry 자동 해석을 함께 지원해 설정 실패를 줄여야 한다.
- 운영 단계에서 wiring 오류를 빠르게 확인할 검증 도구를 제공해야 한다.

## 기술적 세부 사항

- Intro/PatientA, PatientB/C 이벤트를 분할 파일로 구성해 유지보수성을 높인다.
- Awake/OnEnable/OnDisable 수명주기에서 그래프 등록과 핸들러 등록/해제를 처리한다.
- ContextMenu 기반 `Validate Event Wiring`, `Run Core Smoke Test`, `Log Registry Snapshot` 기능을 제공한다.

## 참조

- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
- [api:event-registry](../content-definitions/scenario/event-registry.md)
- [api:event-handler-file-index](../content-definitions/scenario/event-handler-file-index.md)
