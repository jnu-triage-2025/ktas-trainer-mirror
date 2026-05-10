# API 레퍼런스: TriageTrainer.Entity.PatientMonitor

> 네임스페이스: `TriageTrainer.Entity.PatientMonitor.Models`, `TriageTrainer.Entity.PatientMonitor`  
> 파일 위치:  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/PatientMonitorController.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/ECGGraphVisualElement.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/UIDocumentWorldSurfaceBinder.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/ECGParameters.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/ECGWaveformCalculator.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/ARTParameters.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/ARTWaveformCalculator.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/CVPParameters.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/CVPWaveformCalculator.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/PlethParameters.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/PlethWaveformCalculator.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Entities/PatientMonitor/Models/PatientMonitorParameters.cs`

## 0. 개요

PatientMonitor 모듈은 UI Toolkit 기반 환자 모니터를 제공하며, ECG/ART/CVP/PLETH 파형을 동시에 렌더링한다. 파형 파라미터는 FishNet `SyncVar`를 통해 네트워크로 동기화된다.

## 1. PatientMonitorController

### 책임

- UIDocument 루트에 4채널(ECG, PLETH, ART, CVP) 그래프 UI 구성
- 파형 계산기(`*WaveformCalculator`)를 사용해 샘플 생성
- 서버 권위 파라미터를 `SyncVar`로 전파하고 클라이언트에서 반영

### 주요 필드

| 필드 | 설명 |
|---|---|
| monitorParameters | ECG/ART/CVP/PLETH 통합 파라미터 |
| _syncEcgParameters, _syncArtParameters, _syncCvpParameters, _syncPlethParameters | 도메인별 SyncVar |
| _syncRhythmPreset | ECG 리듬 프리셋 SyncVar |
| ecgGraphElement, plethGraphElement, artGraphElement, cvpGraphElement | 채널별 그래프 엘리먼트 |

### 주요 메서드

| 메서드 | 역할 |
|---|---|
| OnStartServer | 초기 파라미터를 SyncVar에 설정 |
| OnStartClient / OnStopClient | SyncVar 변경 콜백 등록/해제 |
| SetRhythm / SetCustomParameters | ECG 리듬/파라미터 적용(클라 호출 시 ServerRpc 경유) |
| SetARTParameters / SetCVPParameters / SetPlethParameters | 비ECG 파라미터 적용(네트워크 전파) |
| TickSample | 모든 채널 샘플 생성 및 그래프 반영 |

## 2. 모델/계산기 구조 (Models 통합)

`Models` 폴더 내에서 도메인별로 다음 패턴을 동일하게 사용한다.

- `ECGParameters` + `ECGWaveformCalculator`
- `ARTParameters` + `ARTWaveformCalculator`
- `CVPParameters` + `CVPWaveformCalculator`
- `PlethParameters` + `PlethWaveformCalculator`
- `PatientMonitorParameters` (통합 컨테이너)

## 3. 그래프/표시 계층

- `ECGGraphVisualElement`는 단일 채널 그래프를 렌더링하며, 채널 타입별 기본 범위 설정을 지원한다.
- `UIDocumentWorldSurfaceBinder`는 UIDocument를 RenderTexture로 출력해 월드 오브젝트 표면에 표시한다.

## 4. 시나리오 연동

`TriageScenarioEventBootstrap`에서 환자 상태 이벤트에 맞춰 `PatientMonitorController` 파라미터를 변경한다. 기존 ECG 중심 이벤트 흐름을 유지하면서도, 필요 시 ART/CVP/PLETH API를 동일한 방식으로 확장 가능하다.

## 5. 관련 문서

- `Documents/requirements/patient/patient-monitor-requirements.md`
- `Documents/api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md`
