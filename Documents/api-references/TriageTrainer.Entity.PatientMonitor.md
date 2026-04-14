# API 레퍼런스: TriageTrainer.Entity.PatientMonitor

> 네임스페이스: TriageTrainer.Entity.PatientMonitor.Models  
> 파일 위치:  
> - Assets/Modules/TriageTrainer/Scripts/Entity/PatientMonitor/PatientMonitorController.cs  
> - Assets/Modules/TriageTrainer/Scripts/Entity/PatientMonitor/Models/ECGParameters.cs  
> - Assets/Modules/TriageTrainer/Scripts/Entity/PatientMonitor/ECGGraphVisualElement.cs

## 0. 개요

PatientMonitor 모듈은 UI Toolkit 기반 ECG 파형 모니터를 제공하며, 시나리오 이벤트에서 환자 상태 변화를 시각화할 때 사용됩니다.

## 1. PatientMonitorController

### 책임

- UIDocument 루트에 ECG 그래프 UI 구성
- ECGParameters 기반 파형 계산
- Update 루프에서 실시간 샘플 생성/그래프 반영

### 주요 필드

| 필드 | 설명 |
|---|---|
| parameters | 심전도 파형 파라미터(심박수, 파형 진폭/폭, 노이즈 등) |
| graphColor | 그래프 색상 |
| lineThickness | 선 두께 |
| resolution | 그래프 해상도 |

### 주요 메서드

| 메서드 | 역할 |
|---|---|
| OnEnable | UIDocument 획득, 그래프 UI 초기화 |
| CreateGraphUI | 컨테이너/그래프 엘리먼트 구성 |
| Update | 비트 간격 계산, 전압 샘플 생성, 그래프 추가 |
| CalculateVoltage | P/QRS/T/U, ST, 노이즈 포함 전압 계산 |
| Gaussian | 가우시안 파형 구성 함수 |
| OnValidate | 인스펙터 변경 즉시 스타일 반영 |

## 2. 파형 모델(ECGParameters)

ECGParameters는 다음 상태를 표현할 수 있습니다.
- 정상 리듬
- 빈맥/서맥 계열
- 불규칙 리듬(노이즈, irregularity 증가)
- 무수축(asystole: bpm<=0)

## 3. 시나리오 연동

TriageScenarioEventBootstrap 이벤트에서 아래와 같이 활용됩니다.
- activate_vital_monitor_ui_patientA/B/C
- patient_crash_ui
- asystole_monitor_ui
- rosc_monitor_ui
- defib_ui_irregular

## 4. 운영 주의

- 모니터 파형 프리셋은 이벤트별 ECGParameters를 명시적으로 분리 관리합니다.
- UI 패널 활성화/비활성화와 모니터 파라미터 적용 순서를 고정해 깜빡임을 줄입니다.

## 5. 관련 문서

- api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md
- requirements/content-definitions/scenario/patient_a_critical.md
- requirements/content-definitions/scenario/patient_b_c_ct.md