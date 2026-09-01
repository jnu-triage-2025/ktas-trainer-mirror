# <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController"></a> Class DualPatientMonitorController

Namespace: [TriageTrainer.Entity.PatientMonitor.Models](TriageTrainer.Entity.PatientMonitor.Models.md)  
Assembly: Assembly\-CSharp.dll  

Graph/Metrics 자식 UIDocument를 각각 출력하는 2-Plane 컨트롤러입니다.
그래픽 데이터 공급과 환자 상태는 PatientMonitorController에서 공유하고,
출력 대상만 두 개의 PatientMonitorPlane으로 분리합니다.

```csharp
[AddComponentMenu("Triage Trainer/Patient Monitor/Dual Patient Monitor Controller")]
public sealed class DualPatientMonitorController : PatientMonitorController, IInteractable, PatientController.IMonitorSelectionRequester, PatientController.IMedicalStateListener
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md) ← 
[DualPatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.DualPatientMonitorController.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[PatientController.IMonitorSelectionRequester](TriageTrainer.Entity.PatientController.IMonitorSelectionRequester.md), 
[PatientController.IMedicalStateListener](TriageTrainer.Entity.PatientController.IMedicalStateListener.md)

#### Inherited Members

[PatientMonitorController.MonitorPresentationEntityIdentifier](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_MonitorPresentationEntityIdentifier), 
[PatientMonitorController.PresentationEntityIdentifier](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_PresentationEntityIdentifier), 
[PatientMonitorController.Interacts](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_Interacts), 
[PatientMonitorController.SetInteractEnabled\(string, bool\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetInteractEnabled\_System\_String\_System\_Boolean\_), 
[PatientMonitorController.AddInteract\(string, bool\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_AddInteract\_System\_String\_System\_Boolean\_), 
[PatientMonitorController.RemoveInteract\(string\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_RemoveInteract\_System\_String\_), 
[PatientMonitorController.IsInteractEnabled\(string\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_IsInteractEnabled\_System\_String\_), 
[PatientMonitorController.HandlePatientSelected\(PatientController, Transform\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_HandlePatientSelected\_TriageTrainer\_Entity\_PatientController\_UnityEngine\_Transform\_), 
[PatientMonitorController.PatientTrackingMethod](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_PatientTrackingMethod), 
[PatientMonitorController.OnEnterAnotherPatientAlreadyPatientExists](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnEnterAnotherPatientAlreadyPatientExists), 
[PatientMonitorController.DisconnectedPatientDisplayMode](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_DisconnectedPatientDisplayMode), 
[PatientMonitorController.ecgColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ecgColor), 
[PatientMonitorController.plethColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_plethColor), 
[PatientMonitorController.artColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_artColor), 
[PatientMonitorController.cvpColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_cvpColor), 
[PatientMonitorController.lineThickness](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_lineThickness), 
[PatientMonitorController.resolution](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_resolution), 
[PatientMonitorController.EnableDetailedContentOverlay](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_EnableDetailedContentOverlay), 
[PatientMonitorController.OpenPresentation\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OpenPresentation), 
[PatientMonitorController.SetCloseRequestedHandler\(Action\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetCloseRequestedHandler\_System\_Action\_), 
[PatientMonitorController.ArmScenarioClose\(PatientController, string\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ArmScenarioClose\_TriageTrainer\_Entity\_PatientController\_System\_String\_), 
[PatientMonitorController.CloseForScenarioReset\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_CloseForScenarioReset), 
[PatientMonitorController.SetRhythm\(ECGRhythmType, bool\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetRhythm\_TriageTrainer\_Entity\_Patient\_ECGRhythmType\_System\_Boolean\_), 
[PatientMonitorController.SetCustomParameters\(ECGParameters, bool\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetCustomParameters\_TriageTrainer\_Entity\_Patient\_ECGParameters\_System\_Boolean\_), 
[PatientMonitorController.HandleMedicalStateChanged\(PatientMedicalState\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_HandleMedicalStateChanged\_TriageTrainer\_Entity\_Patient\_PatientMedicalState\_), 
[PatientMonitorController.SetARTParameters\(ARTParameters\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetARTParameters\_TriageTrainer\_Entity\_Patient\_ARTParameters\_), 
[PatientMonitorController.SetCVPParameters\(CVPParameters\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetCVPParameters\_TriageTrainer\_Entity\_Patient\_CVPParameters\_), 
[PatientMonitorController.SetPlethParameters\(PlethParameters\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetPlethParameters\_TriageTrainer\_Entity\_Patient\_PlethParameters\_), 
[PatientMonitorController.SetNumericsParameters\(NumericsParameters\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetNumericsParameters\_TriageTrainer\_Entity\_Patient\_NumericsParameters\_), 
[PatientMonitorController.SetNIBPParameters\(NIBPParameters\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetNIBPParameters\_TriageTrainer\_Entity\_Patient\_NIBPParameters\_), 
[PatientMonitorController.SetTemperatureParameters\(TemperatureParameters\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetTemperatureParameters\_TriageTrainer\_Entity\_Patient\_TemperatureParameters\_), 
[PatientMonitorController.SetSTLeadValues\(STLeadValues\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetSTLeadValues\_TriageTrainer\_Entity\_Patient\_STLeadValues\_), 
[PatientMonitorController.MonitoringPatient](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_MonitoringPatient), 
[PatientMonitorController.SetMonitoringPatient\(PatientController\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetMonitoringPatient\_TriageTrainer\_Entity\_PatientController\_), 
[PatientMonitorController.OnStartServer\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnStartServer), 
[PatientMonitorController.OnStartClient\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnStartClient), 
[PatientMonitorController.OnStopClient\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnStopClient)

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController_BuildsSinglePlaneGraphic"></a> BuildsSinglePlaneGraphic

```csharp
protected override bool BuildsSinglePlaneGraphic { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController_CloseDetailedContentOverlay"></a> CloseDetailedContentOverlay\(\)

```csharp
protected override void CloseDetailedContentOverlay()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController_ConfigureDisplayLayout"></a> ConfigureDisplayLayout\(\)

```csharp
protected override void ConfigureDisplayLayout()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController_OnEnable"></a> OnEnable\(\)

```csharp
protected override void OnEnable()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_DualPatientMonitorController_OpenDetailedContentOverlay"></a> OpenDetailedContentOverlay\(\)

```csharp
protected override void OpenDetailedContentOverlay()
```

