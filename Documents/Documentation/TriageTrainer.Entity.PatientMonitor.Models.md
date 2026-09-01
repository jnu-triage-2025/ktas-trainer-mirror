# <a id="TriageTrainer_Entity_PatientMonitor_Models"></a> Namespace TriageTrainer.Entity.PatientMonitor.Models

### Classes

 [ARTWaveformCalculator](TriageTrainer.Entity.PatientMonitor.Models.ARTWaveformCalculator.md)

 [CVPWaveformCalculator](TriageTrainer.Entity.PatientMonitor.Models.CVPWaveformCalculator.md)

 [DualPatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.DualPatientMonitorController.md)

Graph/Metrics 자식 UIDocument를 각각 출력하는 2-Plane 컨트롤러입니다.
그래픽 데이터 공급과 환자 상태는 PatientMonitorController에서 공유하고,
출력 대상만 두 개의 PatientMonitorPlane으로 분리합니다.

 [ECGWaveformCalculator](TriageTrainer.Entity.PatientMonitor.Models.ECGWaveformCalculator.md)

 [PatientMonitorController.InteractEntry](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.InteractEntry.md)

 [PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md)

PatientMonitor의 공통 데이터/네트워크/파형 기반입니다.
실제 출력 정책은 SinglePatientMonitorController 또는 DualPatientMonitorController가 담당합니다.

 [PatientMonitorTrackingLineEndpoint](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorTrackingLineEndpoint.md)

Marks the point on a patient where a PatientMonitor tracking line should end.
Place this component on an empty child object positioned over the patient's torso.

 [PlethWaveformCalculator](TriageTrainer.Entity.PatientMonitor.Models.PlethWaveformCalculator.md)

 [SinglePatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.SinglePatientMonitorController.md)

기존 PatientMonitor 구현의 단일 출력 컨트롤러입니다.
환자 상태, 네트워크 동기화, 파형 계산과 단일 UIDocument 그래픽을 보존합니다.

### Structs

 [ECGRuntimeState](TriageTrainer.Entity.PatientMonitor.Models.ECGRuntimeState.md)

### Enums

 [DisconnectedPatientDisplayMode](TriageTrainer.Entity.PatientMonitor.Models.DisconnectedPatientDisplayMode.md)

환자가 연결되지 않은 동안 모니터에 표시할 데이터의 원천입니다.

 [PatientMonitorController.ECGDisplayMode](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.ECGDisplayMode.md)

 [OnEnterAnotherPatientAlreadyPatientExists](TriageTrainer.Entity.PatientMonitor.Models.OnEnterAnotherPatientAlreadyPatientExists.md)

 [PatientMonitorLineConnectionServiceUnavailableOption](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorLineConnectionServiceUnavailableOption.md)

 [PatientMonitorTrackingLineDisplayOption](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorTrackingLineDisplayOption.md)

 [PatientTrackingMethod](TriageTrainer.Entity.PatientMonitor.Models.PatientTrackingMethod.md)

