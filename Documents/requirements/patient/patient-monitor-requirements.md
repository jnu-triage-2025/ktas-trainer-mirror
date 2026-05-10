---
title: "PatientMonitor 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

PatientMonitor는 환자 활력징후 파형을 시각화하는 기능이다. 사용자에게는 환자 상태 악화/회복을 직관적으로 보여주는 핵심 피드백 수단이다.

## 상세

- 모니터는 UIDocument 기반 UI에서 ECG, ART, CVP, PLETH 파형을 동시에 실시간 표시해야 한다.
- 정상, 불규칙, 무수축, ROSC 등 시나리오 상태에 맞는 ECG 파형 전환을 지원해야 한다.
- 이벤트별 파라미터 프리셋 적용 시 화면 깜빡임 없이 자연스럽게 갱신되어야 한다.
- 모니터 표시 여부(패널 활성/비활성)와 파라미터 적용 순서가 일관되어야 한다.
- 모니터 파라미터는 네트워크 세션의 모든 관찰자에게 동일하게 전파되어야 한다.
- 코드 구조는 `Models` 폴더 내에서 도메인별 `Parameters + WaveformCalculator` 패턴을 유지해야 한다.

## 기술적 세부 사항

- `PatientMonitorController`가 4개 파형 샘플 계산과 그래프 반영(Update 루프)을 담당한다.
- `PatientMonitorParameters`가 도메인별 파라미터(`ECG/ART/CVP/PLETH`)를 통합 보유한다.
- 각 도메인은 `*Parameters`와 `*WaveformCalculator`를 통해 독립 계산된다.
- 네트워크 동기화는 FishNet `SyncVar` 및 `ServerRpc` 기반으로 수행된다.
- `TriageScenarioEventBootstrap` 이벤트(예: patient_crash_ui, asystole_monitor_ui, ROSC_monitor_ui)와 연동해 상태 전환을 수행한다.

## 참조

- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
- [api:patient_a_critical](../content-definitions/scenario/patient_a_critical.md)
