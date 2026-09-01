# <a id="TriageTrainer_Entity_PatientMonitor_Models_SinglePatientMonitorController"></a> Class SinglePatientMonitorController

Namespace: [TriageTrainer.Entity.PatientMonitor.Models](TriageTrainer.Entity.PatientMonitor.Models.md)  
Assembly: Assembly\-CSharp.dll  

기존 PatientMonitor 구현의 단일 출력 컨트롤러입니다.
환자 상태, 네트워크 동기화, 파형 계산과 단일 UIDocument 그래픽을 보존합니다.

```csharp
[RequireComponent(typeof(UIDocument))]
[AddComponentMenu("Triage Trainer/Patient Monitor/Single Patient Monitor Controller")]
public class SinglePatientMonitorController : PatientMonitorController, IInteractable, PatientController.IMonitorSelectionRequester, PatientController.IMedicalStateListener
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md) ← 
[SinglePatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.SinglePatientMonitorController.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[PatientController.IMonitorSelectionRequester](TriageTrainer.Entity.PatientController.IMonitorSelectionRequester.md), 
[PatientController.IMedicalStateListener](TriageTrainer.Entity.PatientController.IMedicalStateListener.md)

#### Inherited Members

[PatientMonitorController.MonitorPresentationEntityIdentifier](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_MonitorPresentationEntityIdentifier), 
[PatientMonitorController.PresentationEntityIdentifier](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_PresentationEntityIdentifier), 
[PatientMonitorController.IsPatientTrackingMethodEnabled\(PatientTrackingMethod\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_IsPatientTrackingMethodEnabled\_TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientTrackingMethod\_), 
[PatientMonitorController.Interacts](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_Interacts), 
[PatientMonitorController.Awake\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_Awake), 
[PatientMonitorController.OnDisable\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnDisable), 
[PatientMonitorController.OnDestroy\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnDestroy), 
[PatientMonitorController.SetInteractEnabled\(string, bool\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetInteractEnabled\_System\_String\_System\_Boolean\_), 
[PatientMonitorController.AddInteract\(string, bool\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_AddInteract\_System\_String\_System\_Boolean\_), 
[PatientMonitorController.RemoveInteract\(string\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_RemoveInteract\_System\_String\_), 
[PatientMonitorController.IsInteractEnabled\(string\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_IsInteractEnabled\_System\_String\_), 
[PatientMonitorController.HandlePatientSelected\(PatientController, Transform\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_HandlePatientSelected\_TriageTrainer\_Entity\_PatientController\_UnityEngine\_Transform\_), 
[PatientMonitorController.ConfigurePatientTracking\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ConfigurePatientTracking), 
[PatientMonitorController.UnconfigurePatientTracking\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_UnconfigurePatientTracking), 
[PatientMonitorController.UpdatePatientTrackingLocation\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_UpdatePatientTrackingLocation), 
[PatientMonitorController.Reset\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_Reset), 
[PatientMonitorController.PatientTrackingMethod](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_PatientTrackingMethod), 
[PatientMonitorController.OnEnterAnotherPatientAlreadyPatientExists](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnEnterAnotherPatientAlreadyPatientExists), 
[PatientMonitorController.DisconnectedPatientDisplayMode](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_DisconnectedPatientDisplayMode), 
[PatientMonitorController.ecgColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ecgColor), 
[PatientMonitorController.plethColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_plethColor), 
[PatientMonitorController.artColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_artColor), 
[PatientMonitorController.cvpColor](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_cvpColor), 
[PatientMonitorController.lineThickness](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_lineThickness), 
[PatientMonitorController.resolution](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_resolution), 
[PatientMonitorController.uiDocument](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_uiDocument), 
[PatientMonitorController.\_displayViews](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_\_displayViews), 
[PatientMonitorController.EnableDetailedContentOverlay](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_EnableDetailedContentOverlay), 
[PatientMonitorController.OnEnable\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnEnable), 
[PatientMonitorController.BuildsSinglePlaneGraphic](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_BuildsSinglePlaneGraphic), 
[PatientMonitorController.ConfigureDisplayLayout\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ConfigureDisplayLayout), 
[PatientMonitorController.OpenDetailedContentOverlay\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OpenDetailedContentOverlay), 
[PatientMonitorController.CloseDetailedContentOverlay\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_CloseDetailedContentOverlay), 
[PatientMonitorController.OpenPresentation\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OpenPresentation), 
[PatientMonitorController.SetCloseRequestedHandler\(Action\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_SetCloseRequestedHandler\_System\_Action\_), 
[PatientMonitorController.ArmScenarioClose\(PatientController, string\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ArmScenarioClose\_TriageTrainer\_Entity\_PatientController\_System\_String\_), 
[PatientMonitorController.CloseForScenarioReset\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_CloseForScenarioReset), 
[PatientMonitorController.RequestClose\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_RequestClose), 
[PatientMonitorController.Update\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_Update), 
[PatientMonitorController.OnValidate\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_OnValidate), 
[PatientMonitorController.ResolveHorizontalPoints\(\)](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md\#TriageTrainer\_Entity\_PatientMonitor\_Models\_PatientMonitorController\_ResolveHorizontalPoints), 
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

