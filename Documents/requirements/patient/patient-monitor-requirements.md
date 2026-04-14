---
title: "PatientMonitor 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

PatientMonitor는 환자 활력징후(특히 ECG 파형)를 시각화하는 기능이다. 사용자에게는 환자 상태 악화/회복을 직관적으로 보여주는 핵심 피드백 수단이다.

## 상세

- 모니터는 UIDocument 기반 UI에서 실시간 파형을 연속적으로 표시해야 한다.
- 정상, 불규칙, 무수축, ROSC 등 시나리오 상태에 맞는 파형 전환을 지원해야 한다.
- 이벤트별 파라미터 프리셋 적용 시 화면 깜빡임 없이 자연스럽게 갱신되어야 한다.
- 모니터 표시 여부(패널 활성/비활성)와 파라미터 적용 순서가 일관되어야 한다.

## 기술적 세부 사항

- `PatientMonitorController`가 파형 샘플 계산과 그래프 반영(Update 루프)을 담당한다.
- `ECGParameters`로 bpm, 잡음, 파형 폭/진폭을 조정한다.
- `TriageScenarioEventBootstrap` 이벤트(예: patient_crash_ui, asystole_monitor_ui, ROSC_monitor_ui)와 연동해 상태 전환을 수행한다.

## 참조

- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
- [api:patient_a_critical](../content-definitions/scenario/patient_a_critical.md)
