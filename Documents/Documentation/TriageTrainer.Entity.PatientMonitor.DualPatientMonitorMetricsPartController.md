# <a id="TriageTrainer_Entity_PatientMonitor_DualPatientMonitorMetricsPartController"></a> Class DualPatientMonitorMetricsPartController

Namespace: [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)  
Assembly: Assembly\-CSharp.dll  

Dual PatientMonitor의 Metrics 출력 자식 오브젝트를 식별하는 컴포넌트입니다.

```csharp
[RequireComponent(typeof(UIDocument))]
[DisallowMultipleComponent]
[AddComponentMenu("Triage Trainer/Patient Monitor/Dual Metrics Part Controller")]
public sealed class DualPatientMonitorMetricsPartController : PatientMonitorPlane
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PatientMonitorPlane](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane.md) ← 
[DualPatientMonitorMetricsPartController](TriageTrainer.Entity.PatientMonitor.DualPatientMonitorMetricsPartController.md)

#### Inherited Members

[PatientMonitorPlane.Type](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane.md\#TriageTrainer\_Entity\_PatientMonitor\_PatientMonitorPlane\_Type), 
[PatientMonitorPlane.LowResolution](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane.md\#TriageTrainer\_Entity\_PatientMonitor\_PatientMonitorPlane\_LowResolution), 
[PatientMonitorPlane.Document](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane.md\#TriageTrainer\_Entity\_PatientMonitor\_PatientMonitorPlane\_Document)

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_DualPatientMonitorMetricsPartController_Type"></a> Type

```csharp
public override PatientMonitorPlaneType Type { get; }
```

#### Property Value

 [PatientMonitorPlaneType](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlaneType.md)

