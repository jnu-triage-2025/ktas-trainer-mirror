# <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorPlane"></a> Class PatientMonitorPlane

Namespace: [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)  
Assembly: Assembly\-CSharp.dll  

2-Plane 모드에서 자식 표시 평면의 역할을 식별하는 마커입니다.

```csharp
[RequireComponent(typeof(UIDocument))]
[DisallowMultipleComponent]
public abstract class PatientMonitorPlane : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PatientMonitorPlane](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane.md)

#### Derived

[DualPatientMonitorGraphPartController](TriageTrainer.Entity.PatientMonitor.DualPatientMonitorGraphPartController.md), 
[DualPatientMonitorMetricsPartController](TriageTrainer.Entity.PatientMonitor.DualPatientMonitorMetricsPartController.md)

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorPlane_Document"></a> Document

```csharp
public UIDocument Document { get; }
```

#### Property Value

 UIDocument

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorPlane_LowResolution"></a> LowResolution

```csharp
public Vector2Int LowResolution { get; }
```

#### Property Value

 Vector2Int

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorPlane_Type"></a> Type

```csharp
public abstract PatientMonitorPlaneType Type { get; }
```

#### Property Value

 [PatientMonitorPlaneType](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlaneType.md)

