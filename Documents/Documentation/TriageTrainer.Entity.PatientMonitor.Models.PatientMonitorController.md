# <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController"></a> Class PatientMonitorController

Namespace: [TriageTrainer.Entity.PatientMonitor.Models](TriageTrainer.Entity.PatientMonitor.Models.md)  
Assembly: Assembly\-CSharp.dll  

PatientMonitor의 공통 데이터/네트워크/파형 기반입니다.
실제 출력 정책은 SinglePatientMonitorController 또는 DualPatientMonitorController가 담당합니다.

```csharp
public abstract class PatientMonitorController : NetworkBehaviour, IInteractable, PatientController.IMonitorSelectionRequester, PatientController.IMedicalStateListener
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md)

#### Derived

[DualPatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.DualPatientMonitorController.md), 
[SinglePatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.SinglePatientMonitorController.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[PatientController.IMonitorSelectionRequester](TriageTrainer.Entity.PatientController.IMonitorSelectionRequester.md), 
[PatientController.IMedicalStateListener](TriageTrainer.Entity.PatientController.IMedicalStateListener.md)

## Fields

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_MonitorPresentationEntityIdentifier"></a> MonitorPresentationEntityIdentifier

특정 환자에 매이지 않는 모니터 인터랙션의 퀘스트 표시 주소.
여기에 바인딩한 마크는 추적 환자와 상관없이 모든 환자 모니터에 함께 붙는다.

```csharp
public const string MonitorPresentationEntityIdentifier = "patient_monitor"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController__displayViews"></a> \_displayViews

```csharp
protected readonly List<PatientMonitorDisplayView> _displayViews
```

#### Field Value

 List<[PatientMonitorDisplayView](TriageTrainer.Entity.PatientMonitor.PatientMonitorDisplayView.md)\>

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_artColor"></a> artColor

```csharp
public Color artColor
```

#### Field Value

 Color

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_cvpColor"></a> cvpColor

```csharp
public Color cvpColor
```

#### Field Value

 Color

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_ecgColor"></a> ecgColor

```csharp
[Header("Graph Appearance")]
public Color ecgColor
```

#### Field Value

 Color

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_lineThickness"></a> lineThickness

```csharp
public float lineThickness
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_plethColor"></a> plethColor

```csharp
public Color plethColor
```

#### Field Value

 Color

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_resolution"></a> resolution

```csharp
[Range(10, 6000)]
public int resolution
```

#### Field Value

 int

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_uiDocument"></a> uiDocument

```csharp
protected UIDocument uiDocument
```

#### Field Value

 UIDocument

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_BuildsSinglePlaneGraphic"></a> BuildsSinglePlaneGraphic

```csharp
protected virtual bool BuildsSinglePlaneGraphic { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_DisconnectedPatientDisplayMode"></a> DisconnectedPatientDisplayMode

```csharp
public DisconnectedPatientDisplayMode DisconnectedPatientDisplayMode { get; }
```

#### Property Value

 [DisconnectedPatientDisplayMode](TriageTrainer.Entity.PatientMonitor.Models.DisconnectedPatientDisplayMode.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_EnableDetailedContentOverlay"></a> EnableDetailedContentOverlay

```csharp
public bool EnableDetailedContentOverlay { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_MonitoringPatient"></a> MonitoringPatient

```csharp
public PatientController MonitoringPatient { get; }
```

#### Property Value

 [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnEnterAnotherPatientAlreadyPatientExists"></a> OnEnterAnotherPatientAlreadyPatientExists

```csharp
public OnEnterAnotherPatientAlreadyPatientExists OnEnterAnotherPatientAlreadyPatientExists { get; }
```

#### Property Value

 [OnEnterAnotherPatientAlreadyPatientExists](TriageTrainer.Entity.PatientMonitor.Models.OnEnterAnotherPatientAlreadyPatientExists.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_PatientTrackingMethod"></a> PatientTrackingMethod

```csharp
public PatientTrackingMethod PatientTrackingMethod { get; }
```

#### Property Value

 [PatientTrackingMethod](TriageTrainer.Entity.PatientMonitor.Models.PatientTrackingMethod.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_AddInteract_System_String_System_Boolean_"></a> AddInteract\(string, bool\)

```csharp
public void AddInteract(string identifier, bool enabled = true)
```

#### Parameters

`identifier` string

`enabled` bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_ArmScenarioClose_TriageTrainer_Entity_PatientController_System_String_"></a> ArmScenarioClose\(PatientController, string\)

```csharp
public void ArmScenarioClose(PatientController patient, string completionSignal)
```

#### Parameters

`patient` [PatientController](TriageTrainer.Entity.PatientController.md)

`completionSignal` string

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_Awake"></a> Awake\(\)

```csharp
protected virtual void Awake()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_CloseDetailedContentOverlay"></a> CloseDetailedContentOverlay\(\)

```csharp
protected virtual void CloseDetailedContentOverlay()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_CloseForScenarioReset"></a> CloseForScenarioReset\(\)

시나리오 수동 진입 준비 체인이 모니터를 단계 이전 상태로 되돌릴 때 호출한다.
열려 있던 상세 오버레이를 닫고, 이전 단계에서 걸어 둔 닫기 완료 신호 arm 을 해제한다.
arm 이 남아 있으면 다음 단계에서 모니터를 닫는 순간 지나간 단계의 완료 신호가 다시 올라간다.

```csharp
public void CloseForScenarioReset()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_ConfigureDisplayLayout"></a> ConfigureDisplayLayout\(\)

```csharp
protected virtual void ConfigureDisplayLayout()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_ConfigurePatientTracking"></a> ConfigurePatientTracking\(\)

```csharp
protected void ConfigurePatientTracking()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_HandleMedicalStateChanged_TriageTrainer_Entity_Patient_PatientMedicalState_"></a> HandleMedicalStateChanged\(PatientMedicalState\)

```csharp
public void HandleMedicalStateChanged(PatientMedicalState state)
```

#### Parameters

`state` [PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_HandlePatientSelected_TriageTrainer_Entity_PatientController_UnityEngine_Transform_"></a> HandlePatientSelected\(PatientController, Transform\)

```csharp
public void HandlePatientSelected(PatientController patient, Transform interactor)
```

#### Parameters

`patient` [PatientController](TriageTrainer.Entity.PatientController.md)

`interactor` Transform

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_IsInteractEnabled_System_String_"></a> IsInteractEnabled\(string\)

```csharp
public bool IsInteractEnabled(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_IsPatientTrackingMethodEnabled_TriageTrainer_Entity_PatientMonitor_Models_PatientTrackingMethod_"></a> IsPatientTrackingMethodEnabled\(PatientTrackingMethod\)

```csharp
protected bool IsPatientTrackingMethodEnabled(PatientTrackingMethod method)
```

#### Parameters

`method` [PatientTrackingMethod](TriageTrainer.Entity.PatientMonitor.Models.PatientTrackingMethod.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected virtual void OnDestroy()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnDisable"></a> OnDisable\(\)

```csharp
protected virtual void OnDisable()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnEnable"></a> OnEnable\(\)

```csharp
protected virtual void OnEnable()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OpenDetailedContentOverlay"></a> OpenDetailedContentOverlay\(\)

```csharp
protected virtual void OpenDetailedContentOverlay()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_OpenPresentation"></a> OpenPresentation\(\)

```csharp
public void OpenPresentation()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_RemoveInteract_System_String_"></a> RemoveInteract\(string\)

```csharp
public void RemoveInteract(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_RequestClose"></a> RequestClose\(\)

```csharp
protected void RequestClose()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_Reset"></a> Reset\(\)

```csharp
protected override void Reset()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_ResolveHorizontalPoints"></a> ResolveHorizontalPoints\(\)

```csharp
protected int ResolveHorizontalPoints()
```

#### Returns

 int

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetARTParameters_TriageTrainer_Entity_Patient_ARTParameters_"></a> SetARTParameters\(ARTParameters\)

```csharp
public void SetARTParameters(ARTParameters parameters)
```

#### Parameters

`parameters` [ARTParameters](TriageTrainer.Entity.Patient.ARTParameters.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetCVPParameters_TriageTrainer_Entity_Patient_CVPParameters_"></a> SetCVPParameters\(CVPParameters\)

```csharp
public void SetCVPParameters(CVPParameters parameters)
```

#### Parameters

`parameters` [CVPParameters](TriageTrainer.Entity.Patient.CVPParameters.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetCloseRequestedHandler_System_Action_"></a> SetCloseRequestedHandler\(Action\)

시나리오가 모니터를 열 때 설정하는 종료 콜백입니다. UI 표현과 시나리오 완료
신호의 결합은 호출자에게 두어, 모니터 자체는 특정 환자/시나리오 식별자를 알지 않습니다.

```csharp
public void SetCloseRequestedHandler(Action handler)
```

#### Parameters

`handler` Action

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetCustomParameters_TriageTrainer_Entity_Patient_ECGParameters_System_Boolean_"></a> SetCustomParameters\(ECGParameters, bool\)

```csharp
public void SetCustomParameters(ECGParameters customParameters, bool animateTransition = true)
```

#### Parameters

`customParameters` [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

`animateTransition` bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetInteractEnabled_System_String_System_Boolean_"></a> SetInteractEnabled\(string, bool\)

```csharp
public void SetInteractEnabled(string identifier, bool enabled)
```

#### Parameters

`identifier` string

`enabled` bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetMonitoringPatient_TriageTrainer_Entity_PatientController_"></a> SetMonitoringPatient\(PatientController\)

```csharp
public void SetMonitoringPatient(PatientController patient)
```

#### Parameters

`patient` [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetNIBPParameters_TriageTrainer_Entity_Patient_NIBPParameters_"></a> SetNIBPParameters\(NIBPParameters\)

```csharp
public void SetNIBPParameters(NIBPParameters parameters)
```

#### Parameters

`parameters` [NIBPParameters](TriageTrainer.Entity.Patient.NIBPParameters.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetNumericsParameters_TriageTrainer_Entity_Patient_NumericsParameters_"></a> SetNumericsParameters\(NumericsParameters\)

```csharp
public void SetNumericsParameters(NumericsParameters parameters)
```

#### Parameters

`parameters` [NumericsParameters](TriageTrainer.Entity.Patient.NumericsParameters.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetPlethParameters_TriageTrainer_Entity_Patient_PlethParameters_"></a> SetPlethParameters\(PlethParameters\)

```csharp
public void SetPlethParameters(PlethParameters parameters)
```

#### Parameters

`parameters` [PlethParameters](TriageTrainer.Entity.Patient.PlethParameters.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetRhythm_TriageTrainer_Entity_Patient_ECGRhythmType_System_Boolean_"></a> SetRhythm\(ECGRhythmType, bool\)

```csharp
public void SetRhythm(ECGRhythmType rhythm, bool animateTransition = true)
```

#### Parameters

`rhythm` [ECGRhythmType](TriageTrainer.Entity.Patient.ECGRhythmType.md)

`animateTransition` bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetSTLeadValues_TriageTrainer_Entity_Patient_STLeadValues_"></a> SetSTLeadValues\(STLeadValues\)

```csharp
public void SetSTLeadValues(STLeadValues values)
```

#### Parameters

`values` [STLeadValues](TriageTrainer.Entity.Patient.STLeadValues.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_SetTemperatureParameters_TriageTrainer_Entity_Patient_TemperatureParameters_"></a> SetTemperatureParameters\(TemperatureParameters\)

```csharp
public void SetTemperatureParameters(TemperatureParameters parameters)
```

#### Parameters

`parameters` [TemperatureParameters](TriageTrainer.Entity.Patient.TemperatureParameters.md)

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_UnconfigurePatientTracking"></a> UnconfigurePatientTracking\(\)

```csharp
protected void UnconfigurePatientTracking()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_Update"></a> Update\(\)

```csharp
protected virtual void Update()
```

### <a id="TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_UpdatePatientTrackingLocation"></a> UpdatePatientTrackingLocation\(\)

```csharp
protected void UpdatePatientTrackingLocation()
```

