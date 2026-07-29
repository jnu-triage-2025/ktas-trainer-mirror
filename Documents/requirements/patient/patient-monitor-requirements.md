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

- `SinglePatientMonitorController`는 기존 모니터 구현을 보존하며 단일 `UIDocument`에 전체 모니터 그래픽을 표시한다.
- `DualPatientMonitorController`는 컨트롤러의 자식에서 `PatientMonitorPlane` 컴포넌트를 타입으로 검색한다.
  - `DualPatientMonitorGraphPartController`와 `DualPatientMonitorMetricsPartController` 컴포넌트가 붙은 자식 오브젝트가 없으면 컨트롤러가 두 오브젝트를 자동 생성하고 부모 `UIDocument`의 `PanelSettings`를 연결한다.
  - `DualPatientMonitorGraphPartController` 자식: ECG/PLETH/ART/CVP 파형 및 파형별 수치
  - `DualPatientMonitorMetricsPartController` 자식: ST, BPM, PR, SpO2, 혈압, 체온 등 나머지 수치
  - Dual 컨트롤러 부모에는 `UIDocument`를 두지 않고, 컨트롤러의 `_panelSettings`에 PanelSettings asset을 지정한다. 각 자식 UIDocument는 이 PanelSettings를 사용해 독립 패널로 동작한다. `PatientMonitorPlane`의 기본 RenderTexture는 960×720(4:3) 저해상도 프로파일이다.
  - Graph/Metrics 자식 또는 그 하위 오브젝트에 `MeshRenderer`가 있으면 해당 메시 표면에 텍스처를 출력한다. 메시가 없고 `createDefaultSurfaceWhenMissing`이 켜져 있으면 자식 아래에 Quad와 `UIDocumentWorldSurfaceBinder`를 자동 생성한다.
  - 월드 텍스처용 자식 `UIDocument`는 부모 `UIDocument`의 UI 계층에서 분리한 독립 PanelSettings를 사용해야 한다. 이를 지키지 않으면 Unity의 `UIDocument.set_panelSettings` assertion이 발생한다.
- `PatientMonitorDisplayView`가 그래프/UI 그래픽 데이터 원본을 렌더링하고 닫기 버튼 Overlay를 포함한다. `UIDocumentWorldSurfaceBinder`는 이 출력 결과를 인게임 텍스처로 바인딩한다.

## 기술적 세부 사항

- `PatientMonitorController`가 4개 파형 샘플 계산과 그래프 반영(Update 루프)을 담당한다.
- `UIDocumentWorldSurfaceBinder`가 `UIDocument`를 `RenderTexture`로 출력해 월드 `MeshRenderer` 표면에 바인딩한다.
- `PatientMonitorParameters`가 도메인별 파라미터(`ECG/ART/CVP/PLETH`)를 통합 보유한다.
- 각 도메인은 `*Parameters`와 `*WaveformCalculator`를 통해 독립 계산된다.
- 네트워크 동기화는 FishNet `SyncVar` 및 `ServerRpc` 기반으로 수행된다.
- `TriageScenarioEventBootstrap` 이벤트(예: patient_crash_ui, asystole_monitor_ui, ROSC_monitor_ui)와 연동해 상태 전환을 수행한다.

## 환자 프리팹 Tracking Line 도착점

- Tracking Line을 표시하는 PatientMonitor는 환자 루트가 아니라 환자 몸통을 도착점으로 사용해야 한다.
- 모든 환자 프리팹은 루트 하위에 `PatientMonitorTrackingLineEndpoint` 컴포넌트를 가진 Empty Child Object를 포함해야 한다.
- 해당 Empty Child를 환자 몸통 위치에 배치한다. PatientMonitor는 모니터링 대상의 하위에서 이 Marker를 자동 탐색해 Tracking Line의 도착점으로 연결한다.
- Marker가 없으면 환자 루트 Transform으로 fallback하므로, 발바닥 또는 루트 피벗에 선이 연결될 수 있다.

## 참조

- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
- [api:patient_a_critical](../content-definitions/scenario/patient_a_critical.md)
