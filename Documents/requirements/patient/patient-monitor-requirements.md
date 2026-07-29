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
- UI Toolkit 문서는 월드 표면(RenderTexture) 출력 경로를 지원해야 하며, 대상 머티리얼 텍스처 슬롯 바인딩이 가능해야 한다.
- 코드 구조는 `Models` 폴더 내에서 도메인별 `Parameters + WaveformCalculator` 패턴을 유지해야 한다.

## 표시 모드

- `PatientMonitorController.DisplayMode.OnePlane`은 기존 월드 평면 또는 단일 `UIDocument`에 전체 모니터를 표시한다.
- `PatientMonitorController.DisplayMode.TwoPlane`은 컨트롤러의 자식에서 `PatientMonitorPlane` 컴포넌트를 타입으로 검색한다.
  - `Graph` 자식: ECG/PLETH/ART/CVP 파형 및 파형별 수치
  - `Metrics` 자식: ST, BPM, PR, SpO2, 혈압, 체온 등 나머지 수치
- 각 자식에는 `UIDocument`를 함께 두고 필요하면 `UIDocumentWorldSurfaceBinder`를 추가한다. `PatientMonitorPlane`의 기본 RenderTexture는 960×720(4:3) 저해상도 프로파일이다.
- 표시 뷰는 `PatientMonitorDisplayView`로 분리되어 있어 동일한 데이터 공급을 UI `UIDocument` 출력에도 재사용할 수 있다.

## 기술적 세부 사항

- `PatientMonitorController`가 4개 파형 샘플 계산과 그래프 반영(Update 루프)을 담당한다.
- `UIDocumentWorldSurfaceBinder`가 `UIDocument`를 `RenderTexture`로 출력해 월드 `MeshRenderer` 표면에 바인딩한다.
- `PatientMonitorParameters`가 도메인별 파라미터(`ECG/ART/CVP/PLETH`)를 통합 보유한다.
- 각 도메인은 `*Parameters`와 `*WaveformCalculator`를 통해 독립 계산된다.
- 네트워크 동기화는 FishNet `SyncVar` 및 `ServerRpc` 기반으로 수행된다.
- `TriageScenarioEventBootstrap` 이벤트(예: patient_crash_ui, asystole_monitor_ui, ROSC_monitor_ui)와 연동해 상태 전환을 수행한다.

## 참조

- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
- [api:patient_a_critical](../content-definitions/scenario/patient_a_critical.md)
