# API 레퍼런스: `TriageTrainer.Entity.PatientMonitor`

> 네임스페이스: `TriageTrainer.Entity.PatientMonitor`, `TriageTrainer.Entity.PatientMonitor.Models`
>
> 파일 위치:
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorController.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorController.Binding.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorController.Interactions.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorController.Parameters.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorController.TrackingLine.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorGraphVisualElement.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/UIDocumentWorldSurfaceBinder.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/*.cs`

## 0. 개요

PatientMonitor 모듈은 UI Toolkit 기반 환자 모니터를 월드 오브젝트 표면에 표시하고, ECG/ART/CVP/PLETH 파형 및 수치 파라미터를 네트워크 동기화해 시뮬레이션 참가자에게 동일하게 제공한다.

## 1. `PatientMonitorController`

### 책임

- UIDocument 루트에서 4채널 그래프 UI를 구성
- 파형 계산기(`*WaveformCalculator`)로 샘플을 생성해 그래프에 반영
- 모니터 파라미터를 서버 권위로 동기화
- 환자 선택/연동 인터랙션 진입점 제공

### 주요 동작

- `OnStartServer`: 초기 파라미터를 서버 상태에 반영
- `OnStartClient` / `OnStopClient`: 동기화 구독 등록/해제
- 파라미터 적용 API 호출 시 서버/클라이언트 컨텍스트에 맞게 동기화 경로를 분기
- 샘플 틱에서 채널별 그래프에 값 스트리밍

## 2. 그래프 계층 (`PatientMonitorGraphElement`)

`PatientMonitorGraphVisualElement.cs`의 `PatientMonitorGraphElement`는 채널별 범위를 가진 UI Toolkit 커스텀 그래프다.

- 채널 식별자: `ECG`, `PLETH`, `ART`, `CVP`
- 채널별 기본 값 범위(`GraphRange`) 내장
- `AddValue(float)`로 새 샘플을 추가하고 최대 포인트 수 유지
- `SetChannel(...)`, `SetRange(...)`로 채널 특성 전환

## 3. 월드 표면 바인더 (`UIDocumentWorldSurfaceBinder`)

`UIDocumentWorldSurfaceBinder`는 UIDocument를 RenderTexture에 렌더링하고 `MeshRenderer` 머티리얼 텍스처 슬롯으로 연결한다.

### 인스펙터 핵심 필드

- Target Surface
  - `_targetRenderer`
  - `_texturePropertyName`(기본 `_BaseMap`)
  - `_instantiateMaterial`
- RenderTexture
  - `_resolution`
  - `_depthBuffer`
  - `_format`
  - `_filterMode`

### 라이프사이클

- `OnEnable`
  - `UIDocument.panelSettings`를 런타임 인스턴스로 복제
  - RenderTexture 생성 후 panel targetTexture로 연결
  - 필요 시 머티리얼 인스턴스화 후 텍스처 슬롯 바인딩
- `OnDisable`
  - 원본 PanelSettings 복원
  - RenderTexture 해제/파기
  - 런타임 머티리얼/패널 설정 파기

## 4. 모델/계산기 구조

`Models` 폴더는 채널별 `Parameters + WaveformCalculator` 패턴을 사용한다.

- `ECGParameters` + `ECGWaveformCalculator`
- `ARTParameters` + `ARTWaveformCalculator`
- `CVPParameters` + `CVPWaveformCalculator`
- `PlethParameters` + `PlethWaveformCalculator`
- `PatientMonitorParameters`(통합 컨테이너)

## 5. 시나리오 연동

시나리오 이벤트 계층(`TriageScenarioEventBootstrap`)은 환자 상태 변화에 따라 모니터 파라미터를 갱신한다. 이를 통해 UI 표시(모니터 on/off)와 파형 상태 전환이 시나리오 진행과 일치한다.

## 6. 관련 문서

- `Documents/requirements/patient/patient-monitor-requirements.md`
- `Documents/api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md`
