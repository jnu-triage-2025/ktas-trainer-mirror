# <a id="TriageTrainer_Entity_PatientController"></a> Class PatientController

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

환자 A의 수액 연결 상호작용 부분 구현. 좌측 정맥로에는 생리식염수(N/S)를, 우측 정맥로에는
플라즈마 솔루션을 연결한다.

<p>
예전에는 플레이어가 <code>IntravenousLineConnectionPoint</code> 의 "수액 줄 연결 시작"과
"여기에 수액 줄 연결"을 차례로 사용해서 두 지점을 직접 이었다. 지금은 그 상호작용이 모든
연결 지점에서 잠겨 있으므로, 환자 B/C 의 "생리식염수 연결"과 같은 방식으로 환자 쪽 전용
상호작용 한 번에 연결을 완성한다. 줄 오브젝트는
<xref href="TriageTrainer.Entity.LineConnection.LineConnectionService.TryCreateAutomaticConnection(TriageTrainer.Entity.LineConnection.LineConnectionPoint%2cTriageTrainer.Entity.LineConnection.LineConnectionPoint)" data-throw-if-not-resolved="false"></xref> 이 생성하고, 시나리오
진행 신호는 이 파일에서 직접 올린다.
</p>

```csharp
[RequireComponent(typeof(CapsuleCollider))]
public class PatientController : NetworkBehaviour, IScenarioEntityStateEventSource, ISpawnedEntityIdentifierReceiver, IInteractable, IReposable, IItemUseTarget, IScenarioEntityInitTarget, IScenarioTriageAssessTarget, IScenarioIdentifiedEntity
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[PatientController](TriageTrainer.Entity.PatientController.md)

#### Implements

[IScenarioEntityStateEventSource](MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource.md), 
[ISpawnedEntityIdentifierReceiver](MultiplayerInfrastructure.Registry.ISpawnedEntityIdentifierReceiver.md), 
[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IReposable](MultiplayerInfrastructure.Entity.IReposable.md), 
[IItemUseTarget](MultiplayerInfrastructure.Entity.IItemUseTarget.md), 
[IScenarioEntityInitTarget](MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget.md), 
[IScenarioTriageAssessTarget](MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget.md), 
[IScenarioIdentifiedEntity](MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity.md)

## Fields

### <a id="TriageTrainer_Entity_PatientController_EquipmentTypeBed"></a> EquipmentTypeBed

```csharp
public const string EquipmentTypeBed = "Bed"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_EquipmentTypeIVFluidLeftArm"></a> EquipmentTypeIVFluidLeftArm

```csharp
public const string EquipmentTypeIVFluidLeftArm = "IVFluidLeftArm"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_EquipmentTypeIVFluidRightArm"></a> EquipmentTypeIVFluidRightArm

```csharp
public const string EquipmentTypeIVFluidRightArm = "IVFluidRightArm"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_EquipmentTypeOxyflowmeter"></a> EquipmentTypeOxyflowmeter

```csharp
public const string EquipmentTypeOxyflowmeter = "Oxyflowmeter"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_EquipmentTypePatientMonitor"></a> EquipmentTypePatientMonitor

```csharp
public const string EquipmentTypePatientMonitor = "PatientMonitor"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_EquipmentTypeWallSuction"></a> EquipmentTypeWallSuction

```csharp
public const string EquipmentTypeWallSuction = "WallSuction"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdCarry"></a> InteractIdCarry

```csharp
public const string InteractIdCarry = "carry_patient"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdIntravenousLineCannula"></a> InteractIdIntravenousLineCannula

정맥라인 캐뉼라 상호작용 식별자. 퀘스트 표시 바인딩(퀘스트 마크)에서 참조한다.

```csharp
public const string InteractIdIntravenousLineCannula = "intravenous_line_cannula"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdItemApply"></a> InteractIdItemApply

손에 든 처치 물품(거즈·플라스터 등) 적용 상호작용. 퀘스트 마크 바인딩에서 참조한다.

```csharp
public const string InteractIdItemApply = "item_apply"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdLiftFromBed"></a> InteractIdLiftFromBed

```csharp
public const string InteractIdLiftFromBed = "lift_from_bed"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdMonitorSelect"></a> InteractIdMonitorSelect

```csharp
public const string InteractIdMonitorSelect = "monitor_select"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdNormalSalineConnect"></a> InteractIdNormalSalineConnect

침대 걸이의 생리식염수를 환자 정맥로에 잇는 상호작용. 퀘스트 마크 바인딩에서 참조한다.

```csharp
public const string InteractIdNormalSalineConnect = "normal_saline_connect"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientANormalSalineConnect"></a> InteractIdPatientANormalSalineConnect

좌측 정맥로에 생리식염수를 잇는 상호작용 식별자. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientANormalSalineConnect = "patient_a_normal_saline_connect"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAPlasmaSolutionConnect"></a> InteractIdPatientAPlasmaSolutionConnect

우측 정맥로에 플라즈마 솔루션을 잇는 상호작용 식별자. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAPlasmaSolutionConnect = "patient_a_plasma_solution_connect"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAUseCervicalCollar"></a> InteractIdPatientAUseCervicalCollar

경추 고정기 적용. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAUseCervicalCollar = "patient_a_use_cervical_collar"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAUseEpinephrineSyringe"></a> InteractIdPatientAUseEpinephrineSyringe

에피네프린 투여(1·2차 공용). 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAUseEpinephrineSyringe = "patient_a_use_epinephrine_5cc_syringe"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAUseGauze"></a> InteractIdPatientAUseGauze

출혈 부위 거즈 압박. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAUseGauze = "patient_a_use_gauze"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAUseNormalSalineSyringe"></a> InteractIdPatientAUseNormalSalineSyringe

루멘 내 잔여 약물 밀어넣기(1·2차 공용). 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAUseNormalSalineSyringe = "patient_a_use_normal_saline_20cc_syringe"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAUsePlasterOnGauze"></a> InteractIdPatientAUsePlasterOnGauze

거즈를 플라스터로 고정. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAUsePlasterOnGauze = "patient_a_use_plaster_on_gauze"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdPatientAUsePlasterOnIntubation"></a> InteractIdPatientAUsePlasterOnIntubation

기관내관을 플라스터로 고정. 퀘스트 표시 바인딩에서 참조한다.

```csharp
public const string InteractIdPatientAUsePlasterOnIntubation = "patient_a_use_plaster_on_intubation"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdTriage"></a> InteractIdTriage

```csharp
public const string InteractIdTriage = "triage_assess"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_InteractIdWallSuctionUse"></a> InteractIdWallSuctionUse

흡인기 사용 상호작용 식별자. 퀘스트 마크 바인딩에서 참조한다.

```csharp
public const string InteractIdWallSuctionUse = "wall_suction_use"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventEquipmentConnected"></a> StateEventEquipmentConnected

장비가 환자에게 연결되었을 때 발생. key는 장비 유형명(<xref href="TriageTrainer.Entity.PatientController.EquipmentTypeBed" data-throw-if-not-resolved="false"></xref> 등).

```csharp
public const string StateEventEquipmentConnected = "EquipmentConnected"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventEquipmentDisconnected"></a> StateEventEquipmentDisconnected

장비가 환자로부터 해제되었을 때 발생. key는 장비 유형명.

```csharp
public const string StateEventEquipmentDisconnected = "EquipmentDisconnected"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventOxyflowmeterAttachmentChanged"></a> StateEventOxyflowmeterAttachmentChanged

이 환자의 zone에 연결된 산소 유량계의 실제 설치/회수 상태가 바뀌었을 때 발생한다.
<xref href="TriageTrainer.Entity.PatientController.StateEventEquipmentConnected" data-throw-if-not-resolved="false"></xref>/Disconnected 와 달리 B/C 처치 단계 크레딧
(<code>ShouldCreditPatientBCEquipmentConnection</code>) 게이팅을 거치지 않는 raw 신호다.
key는 "Attached" 또는 "Detached".

```csharp
public const string StateEventOxyflowmeterAttachmentChanged = "OxyflowmeterAttachmentChanged"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventTreatmentApplied"></a> StateEventTreatmentApplied

상태 이벤트 이름 상수. 시나리오 노드의 eventName 과 일치해야 한다.

```csharp
public const string StateEventTreatmentApplied = "TreatmentApplied"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventTreatmentRemoved"></a> StateEventTreatmentRemoved

```csharp
public const string StateEventTreatmentRemoved = "TreatmentRemoved"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventTriageSubmitted"></a> StateEventTriageSubmitted

```csharp
public const string StateEventTriageSubmitted = "TriageSubmitted"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_StateEventVitalChanged"></a> StateEventVitalChanged

```csharp
public const string StateEventVitalChanged = "VitalChanged"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_TreatmentGauze"></a> TreatmentGauze

```csharp
public const string TreatmentGauze = "gauze"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_TreatmentPlasterOnGauze"></a> TreatmentPlasterOnGauze

```csharp
public const string TreatmentPlasterOnGauze = "plaster_on_gauze"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_PatientController_TreatmentPlasterOnIntubation"></a> TreatmentPlasterOnIntubation

```csharp
public const string TreatmentPlasterOnIntubation = "plaster_on_intubation"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_PatientController_AedConnectionPoints"></a> AedConnectionPoints

제세동 패드와 카트를 잇는 AED 라인의 패드 측 연결 지점 목록.
참조가 비어 있으면(배선 누락) 자식 오브젝트에서 클래스 기준으로 찾는 fallback 을 수행한다.

```csharp
public IReadOnlyList<AEDLineConnectionPoint> AedConnectionPoints { get; }
```

#### Property Value

 IReadOnlyList<[AEDLineConnectionPoint](TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint.md)\>

### <a id="TriageTrainer_Entity_PatientController_AnimationController"></a> AnimationController

```csharp
public HumanoidAnimationController AnimationController { get; }
```

#### Property Value

 [HumanoidAnimationController](TriageTrainer.Entity.HumanoidAnimationController.md)

### <a id="TriageTrainer_Entity_PatientController_AssessedTriage"></a> AssessedTriage

현재 이 환자에 부여된(평가된) 트리아지 등급. 미평가면 <xref href="TriageTrainer.Entity.Patient.TriageLevel.Unassessed" data-throw-if-not-resolved="false"></xref>.

```csharp
public TriageLevel AssessedTriage { get; }
```

#### Property Value

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

### <a id="TriageTrainer_Entity_PatientController_CanInteractIntravenousLineCannula"></a> CanInteractIntravenousLineCannula

정맥라인 캐뉼라 상호작용이 실제로 가능한지(Config가 지원하고, 현재 State도 가능한 경우).
환자 B/C는 동공반사 확인을 끝낸 정맥로 확보 단계(<xref href="TriageTrainer.Entity.PatientController.PatientBCTreatmentStage.AwaitingIv" data-throw-if-not-resolved="false"></xref>)에서만 노출한다.

```csharp
public bool CanInteractIntravenousLineCannula { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_CanPerformTriageOrAssessment"></a> CanPerformTriageOrAssessment

환자가 플레이어에게 들린 상태가 아닐 때만 수행 가능한 정지 상태 사정/분류의 공통 게이트.

```csharp
public bool CanPerformTriageOrAssessment { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_CarryAttachPoint"></a> CarryAttachPoint

```csharp
public Transform CarryAttachPoint { get; }
```

#### Property Value

 Transform

### <a id="TriageTrainer_Entity_PatientController_CentralLineAttachmentPoint"></a> CentralLineAttachmentPoint

C-line(중심정맥관) 환자 측 전용 연결 지점. 단일 연결만 허용한다.

```csharp
public CentralLineConnectionPoint CentralLineAttachmentPoint { get; }
```

#### Property Value

 [CentralLineConnectionPoint](TriageTrainer.Entity.CentralLine.CentralLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_ConfiguredOxygenMaskAttachmentPoint"></a> ConfiguredOxygenMaskAttachmentPoint

```csharp
public OxyLineConnectionPoint ConfiguredOxygenMaskAttachmentPoint { get; }
```

#### Property Value

 [OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_ConfiguredPatientBCIvAttachmentPoint"></a> ConfiguredPatientBCIvAttachmentPoint

Inspector 에 직렬화된 B/C 정맥로 IV 연결 지점 참조(주입 검증용).

```csharp
public IntravenousLineConnectionPoint ConfiguredPatientBCIvAttachmentPoint { get; }
```

#### Property Value

 [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_ConfiguredSuctionLineAttachmentPoint"></a> ConfiguredSuctionLineAttachmentPoint

```csharp
public SuctionLineConnectionPoint ConfiguredSuctionLineAttachmentPoint { get; }
```

#### Property Value

 [SuctionLineConnectionPoint](TriageTrainer.Entity.SuctionLine.SuctionLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_ConnectedOxyflowmeter"></a> ConnectedOxyflowmeter

현재 이 환자에 연결된 산소 유량계. 연결 메커니즘 미구현(null=미연결).

```csharp
public WallAttachedOxyflowmeter ConnectedOxyflowmeter { get; }
```

#### Property Value

 [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

### <a id="TriageTrainer_Entity_PatientController_ConnectedWallSuction"></a> ConnectedWallSuction

현재 이 환자에 연결된 벽면 석션. 연결 메커니즘 미구현(null=미연결).

```csharp
public WallAttachedWallSuction ConnectedWallSuction { get; }
```

#### Property Value

 [WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)

### <a id="TriageTrainer_Entity_PatientController_CurrentBed"></a> CurrentBed

```csharp
public MovingPatientBedController CurrentBed { get; }
```

#### Property Value

 [MovingPatientBedController](TriageTrainer.Entity.MovingPatientBedController.md)

### <a id="TriageTrainer_Entity_PatientController_Descriptor"></a> Descriptor

```csharp
public PatientDescriptor Descriptor { get; }
```

#### Property Value

 [PatientDescriptor](TriageTrainer.Entity.Patient.PatientDescriptor.md)

### <a id="TriageTrainer_Entity_PatientController_EffectiveAssessable"></a> EffectiveAssessable

트리아지 인터랙션이 현재 활성 상태인지(전 피어 일관, 외부 조회용).
서버가 초깃값을 반영하기 전에는 인스펙터 값으로 폴백한다.

```csharp
public bool EffectiveAssessable { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_IVFluidLeftArm"></a> IVFluidLeftArm

```csharp
public MonoBehaviour IVFluidLeftArm { get; }
```

#### Property Value

 MonoBehaviour

### <a id="TriageTrainer_Entity_PatientController_IVFluidRightArm"></a> IVFluidRightArm

```csharp
public MonoBehaviour IVFluidRightArm { get; }
```

#### Property Value

 MonoBehaviour

### <a id="TriageTrainer_Entity_PatientController_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_PatientController_IntendedTriage"></a> IntendedTriage

이 환자의 의도된 트리아지 등급(정답).

```csharp
public TriageLevel IntendedTriage { get; }
```

#### Property Value

 [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

### <a id="TriageTrainer_Entity_PatientController_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_PatientController_IntravenousLineCannulaInteractable"></a> IntravenousLineCannulaInteractable

State: 정맥라인 캐뉼라(18G~20G) 상호작용 가능 여부(런타임).

```csharp
public bool IntravenousLineCannulaInteractable { get; set; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_IntravenousLineCannulaSupported"></a> IntravenousLineCannulaSupported

Preset/Config: 정맥라인 캐뉼라(18G~20G) 상호작용 기능 지원 여부.

```csharp
public bool IntravenousLineCannulaSupported { get; set; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_IsMovingPatientBedAttached"></a> IsMovingPatientBedAttached

```csharp
public bool IsMovingPatientBedAttached { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_IsPlayerAttached"></a> IsPlayerAttached

```csharp
public bool IsPlayerAttached { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_IsReposed"></a> IsReposed

```csharp
public bool IsReposed { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_IvAttachmentPoint"></a> IvAttachmentPoint

여러 수액 줄 연결을 허용하는 환자 IV attachment point.

```csharp
public IntravenousLineConnectionPoint IvAttachmentPoint { get; }
```

#### Property Value

 [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalState"></a> MedicalState

```csharp
public PatientMedicalState MedicalState { get; }
```

#### Property Value

 [PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateART"></a> MedicalStateART

```csharp
public ARTParameters MedicalStateART { get; set; }
```

#### Property Value

 [ARTParameters](TriageTrainer.Entity.Patient.ARTParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateBloodPressure"></a> MedicalStateBloodPressure

```csharp
public BloodPressure MedicalStateBloodPressure { get; set; }
```

#### Property Value

 [BloodPressure](TriageTrainer.Entity.Patient.BloodPressure.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateBodyTemperature"></a> MedicalStateBodyTemperature

```csharp
public BodyTemperature MedicalStateBodyTemperature { get; set; }
```

#### Property Value

 [BodyTemperature](TriageTrainer.Entity.Patient.BodyTemperature.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateCVP"></a> MedicalStateCVP

```csharp
public CVPParameters MedicalStateCVP { get; set; }
```

#### Property Value

 [CVPParameters](TriageTrainer.Entity.Patient.CVPParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateConsciousness"></a> MedicalStateConsciousness

```csharp
public Consciousness MedicalStateConsciousness { get; set; }
```

#### Property Value

 [Consciousness](TriageTrainer.Entity.Patient.Consciousness.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateECG"></a> MedicalStateECG

```csharp
public ECGParameters MedicalStateECG { get; set; }
```

#### Property Value

 [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateHealthProblem"></a> MedicalStateHealthProblem

```csharp
public List<HealthProblem> MedicalStateHealthProblem { get; }
```

#### Property Value

 List<[HealthProblem](TriageTrainer.Entity.Patient.HealthProblem.md)\>

### <a id="TriageTrainer_Entity_PatientController_MedicalStateIsCardiacArrest"></a> MedicalStateIsCardiacArrest

```csharp
public bool MedicalStateIsCardiacArrest { get; set; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_PatientController_MedicalStateNIBP"></a> MedicalStateNIBP

```csharp
public NIBPParameters MedicalStateNIBP { get; set; }
```

#### Property Value

 [NIBPParameters](TriageTrainer.Entity.Patient.NIBPParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateNumerics"></a> MedicalStateNumerics

```csharp
public NumericsParameters MedicalStateNumerics { get; set; }
```

#### Property Value

 [NumericsParameters](TriageTrainer.Entity.Patient.NumericsParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStatePleth"></a> MedicalStatePleth

```csharp
public PlethParameters MedicalStatePleth { get; set; }
```

#### Property Value

 [PlethParameters](TriageTrainer.Entity.Patient.PlethParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStatePulse"></a> MedicalStatePulse

```csharp
public BloodPulse MedicalStatePulse { get; set; }
```

#### Property Value

 [BloodPulse](TriageTrainer.Entity.Patient.BloodPulse.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateRequiredDrugs"></a> MedicalStateRequiredDrugs

```csharp
public List<RequiredDrug> MedicalStateRequiredDrugs { get; }
```

#### Property Value

 List<[RequiredDrug](TriageTrainer.Entity.Patient.RequiredDrug.md)\>

### <a id="TriageTrainer_Entity_PatientController_MedicalStateRespiration"></a> MedicalStateRespiration

```csharp
public Respiration MedicalStateRespiration { get; set; }
```

#### Property Value

 [Respiration](TriageTrainer.Entity.Patient.Respiration.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateSTLeads"></a> MedicalStateSTLeads

```csharp
public STLeadValues MedicalStateSTLeads { get; set; }
```

#### Property Value

 [STLeadValues](TriageTrainer.Entity.Patient.STLeadValues.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateSkin"></a> MedicalStateSkin

```csharp
public Skin MedicalStateSkin { get; set; }
```

#### Property Value

 [Skin](TriageTrainer.Entity.Patient.Skin.md)

### <a id="TriageTrainer_Entity_PatientController_MedicalStateTemperature"></a> MedicalStateTemperature

```csharp
public TemperatureParameters MedicalStateTemperature { get; set; }
```

#### Property Value

 [TemperatureParameters](TriageTrainer.Entity.Patient.TemperatureParameters.md)

### <a id="TriageTrainer_Entity_PatientController_MonitoringPatientMonitor"></a> MonitoringPatientMonitor

```csharp
public PatientMonitorController MonitoringPatientMonitor { get; }
```

#### Property Value

 [PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md)

### <a id="TriageTrainer_Entity_PatientController_OxygenMaskAttachmentPoint"></a> OxygenMaskAttachmentPoint

설치된 산소 마스크의 환자 측 산소 라인 포트. 설정되지 않으면 null이다.

```csharp
public OxyLineConnectionPoint OxygenMaskAttachmentPoint { get; }
```

#### Property Value

 [OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_PatientBCIvAttachmentPoint"></a> PatientBCIvAttachmentPoint

환자 B/C 정맥로(캐뉼라 삽입 부위) 측 IV 연결 지점.

<p>
환자 유형마다 정맥로 위치와 모델 구성이 다르므로, 이 지점은 환자 유형별 State 컴포넌트
(<code>PatientTypeBMaleState</code> 등)가 프리팹에서 직접 참조해 주입한다. 식별자 문자열로
자식 포인트를 검색하거나 런타임에 포인트를 생성하지 않는다. 동적으로 추가한
NetworkBehaviour 는 FishNet 스폰 대상이 아니어서 온라인에서는 자동 연결이 불가능하므로,
네트워크 프리팹에 배치된 포인트를 참조하는 것이 유일하게 유효한 배선 방법이다.
</p>

```csharp
public IntravenousLineConnectionPoint PatientBCIvAttachmentPoint { get; }
```

#### Property Value

 [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_PatientDisplayName"></a> PatientDisplayName

화면에 보여줄 환자 이름. 프로필 이름이 없으면 빈 값이다.
환자를 특정해야 하는 상호작용 문구에서 쓴다(예: 수액 줄 해제).

```csharp
public string PatientDisplayName { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_PatientController_ScenarioEntityIdentifier"></a> ScenarioEntityIdentifier

<xref href="MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity" data-throw-if-not-resolved="false"></xref> 구현: 시나리오 식별자를 노출한다(레지스트리 등록 식별자와 동일).
트리거 존 등이 진입 엔티티의 식별자를 도메인 비의존적으로 조회하는 데 사용한다.

```csharp
public string ScenarioEntityIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_PatientController_SuctionLineAttachmentPoint"></a> SuctionLineAttachmentPoint

환자 측 석션 라인 포트. 설정되지 않거나 비활성이면 null이다.

```csharp
public SuctionLineConnectionPoint SuctionLineAttachmentPoint { get; }
```

#### Property Value

 [SuctionLineConnectionPoint](TriageTrainer.Entity.SuctionLine.SuctionLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_SupportExternalRefs"></a> SupportExternalRefs

```csharp
public PatientSupportExternalRefs SupportExternalRefs { get; }
```

#### Property Value

 [PatientSupportExternalRefs](TriageTrainer.Entity.PatientSupportExternalRefs.md)

### <a id="TriageTrainer_Entity_PatientController_TreatmentState"></a> TreatmentState

```csharp
public PatientTreatmentState TreatmentState { get; }
```

#### Property Value

 [PatientTreatmentState](TriageTrainer.Patient.PatientTreatmentState.md)

### <a id="TriageTrainer_Entity_PatientController_Weight"></a> Weight

```csharp
public int Weight { get; }
```

#### Property Value

 int

## Methods

### <a id="TriageTrainer_Entity_PatientController_ActivatePatientBCNurseCStage"></a> ActivatePatientBCNurseCStage\(\)

```csharp
public void ActivatePatientBCNurseCStage()
```

### <a id="TriageTrainer_Entity_PatientController_ActivatePatientBCNurseDStage"></a> ActivatePatientBCNurseDStage\(\)

```csharp
public bool ActivatePatientBCNurseDStage()
```

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_ActivateRecognitionCheck_System_String_System_Boolean_System_String_"></a> ActivateRecognitionCheck\(string, bool, string\)

서버 시나리오 이벤트가 한 단계의 의식 확인을 활성화한다.

```csharp
public void ActivateRecognitionCheck(string completionSignal, bool allowMicrophone, string displayText = "말 걸기")
```

#### Parameters

`completionSignal` string

`allowMicrophone` bool

`displayText` string

### <a id="TriageTrainer_Entity_PatientController_AddInteract_System_String_System_Boolean_"></a> AddInteract\(string, bool\)

```csharp
public void AddInteract(string identifier, bool enabled = true)
```

#### Parameters

`identifier` string

`enabled` bool

### <a id="TriageTrainer_Entity_PatientController_ApplyMedicalStatePreset_MultiplayerInfrastructure_Scenario_ScenarioPatientMedicalStatePresetNode_"></a> ApplyMedicalStatePreset\(ScenarioPatientMedicalStatePresetNode\)

<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> 의 내용을 이 환자 컨트롤러에 적용하고,
서버 컨텍스트인 경우 변경 내용을 RPC로 모든 클라이언트에 전파한다.

<p>null 인 항목은 현재 값을 유지하고, 비어 있지 않은 항목만 덮어쓴다.
이 메서드는 반드시 서버(또는 오프라인) 컨텍스트에서 호출해야 한다.
(<xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref>의 <code>ExecutePatientMedicalStatePresetNode</code>가 서버 전용 가드를 적용한다.)</p>

<p>새 PatientDescriptor / PatientMedicalState 필드가 추가될 때
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode" data-throw-if-not-resolved="false"></xref> 에 프로퍼티를 추가하고,
이 메서드와 <xref href="TriageTrainer.Entity.PatientController.RpcSyncVitalMedicalState(System.String%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Single%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref> 두 곳에도 동일하게 추가한다:</p>
<pre><code class="lang-csharp">if (preset.NewField.HasValue) _patientDescriptor.NewField = preset.NewField.Value;
// RpcSyncVitalMedicalState 파라미터에도 추가:
int newField = preset.NewField ?? PresetSentinelNone</code></pre>

```csharp
public void ApplyMedicalStatePreset(ScenarioPatientMedicalStatePresetNode preset)
```

#### Parameters

`preset` [ScenarioPatientMedicalStatePresetNode](MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.md)

### <a id="TriageTrainer_Entity_PatientController_ApplyScenarioDisplayState_System_String_System_Boolean_"></a> ApplyScenarioDisplayState\(string, bool\)

시나리오 EntityInit 노드가 부르는 명명된 표시 상태 설정(<xref href="MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget" data-throw-if-not-resolved="false"></xref>).
<code class="paramref">displayStateName</code> 을 <xref href="TriageTrainer.Entity.PatientController.TreatmentDisplay" data-throw-if-not-resolved="false"></xref> 로 해석하여 표시/비표시한다.
환자 부착물 초기 표시 상태 설정의 진입점.

```csharp
public bool ApplyScenarioDisplayState(string displayStateName, bool active)
```

#### Parameters

`displayStateName` string

`active` bool

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_ApplySpawnedEntityIdentifier_System_String_"></a> ApplySpawnedEntityIdentifier\(string\)

엔티티 프리셋 스폰 시 인스턴스 식별자를 주입받는다(ISpawnedEntityIdentifierReceiver).
서버에서 호출되며(프리셋 스폰은 서버 컨텍스트), 스폰 전 SyncVar 에 기록되어 전 피어로 복제된다.

```csharp
public void ApplySpawnedEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_PatientController_CanAuthoritativelyConnectPatientBCNormalSaline"></a> CanAuthoritativelyConnectPatientBCNormalSaline\(\)

```csharp
public bool CanAuthoritativelyConnectPatientBCNormalSaline()
```

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_CanPlayerCompletePatientBCNormalSalineConnection_MultiplayerInfrastructure_Player_PlayerController_"></a> CanPlayerCompletePatientBCNormalSalineConnection\(PlayerController\)

```csharp
public bool CanPlayerCompletePatientBCNormalSalineConnection(PlayerController player)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_ClearConnectedOxyflowmeter"></a> ClearConnectedOxyflowmeter\(\)

```csharp
public void ClearConnectedOxyflowmeter()
```

### <a id="TriageTrainer_Entity_PatientController_ClearConnectedOxyflowmeter_TriageTrainer_Entity_WallAttachedOxyflowmeter_"></a> ClearConnectedOxyflowmeter\(WallAttachedOxyflowmeter\)

```csharp
public void ClearConnectedOxyflowmeter(WallAttachedOxyflowmeter expected)
```

#### Parameters

`expected` [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

### <a id="TriageTrainer_Entity_PatientController_ClearConnectedWallSuction"></a> ClearConnectedWallSuction\(\)

```csharp
public void ClearConnectedWallSuction()
```

### <a id="TriageTrainer_Entity_PatientController_ClearConnectedWallSuction_TriageTrainer_Entity_WallAttachedWallSuction_"></a> ClearConnectedWallSuction\(WallAttachedWallSuction\)

```csharp
public void ClearConnectedWallSuction(WallAttachedWallSuction expected)
```

#### Parameters

`expected` [WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)

### <a id="TriageTrainer_Entity_PatientController_ClearIVFluidConnection_System_Boolean_UnityEngine_MonoBehaviour_"></a> ClearIVFluidConnection\(bool, MonoBehaviour\)

IV 수액 연결을 해제한다.

```csharp
public void ClearIVFluidConnection(bool isLeftArm, MonoBehaviour expectedSource = null)
```

#### Parameters

`isLeftArm` bool

true=좌측 팔, false=우측 팔

`expectedSource` MonoBehaviour

### <a id="TriageTrainer_Entity_PatientController_ClearMonitorSelectionRequester_TriageTrainer_Entity_PatientController_IMonitorSelectionRequester_"></a> ClearMonitorSelectionRequester\(IMonitorSelectionRequester\)

```csharp
public void ClearMonitorSelectionRequester(PatientController.IMonitorSelectionRequester requester)
```

#### Parameters

`requester` [PatientController](TriageTrainer.Entity.PatientController.md).[IMonitorSelectionRequester](TriageTrainer.Entity.PatientController.IMonitorSelectionRequester.md)

### <a id="TriageTrainer_Entity_PatientController_ClearMonitoringPatientMonitor_TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_"></a> ClearMonitoringPatientMonitor\(PatientMonitorController\)

환자 모니터가 이 환자에 대한 모니터링을 해제할 때 호출한다.
현재 바인딩된 모니터와 동일한 모니터만 해제할 수 있다(다른 모니터의 오작동 방지).

```csharp
public void ClearMonitoringPatientMonitor(PatientMonitorController monitor)
```

#### Parameters

`monitor` [PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md)

### <a id="TriageTrainer_Entity_PatientController_DebugLogAllConnections"></a> DebugLogAllConnections\(\)

```csharp
[ContextMenu("Debug/Log All Equipment Connections")]
public void DebugLogAllConnections()
```

### <a id="TriageTrainer_Entity_PatientController_GetConnectionSummary"></a> GetConnectionSummary\(\)

CareZone 장비 인식과 실제 물리 라인 연결 상태를 구분해 요약한다.

```csharp
public string GetConnectionSummary()
```

#### Returns

 string

### <a id="TriageTrainer_Entity_PatientController_GetPatientBCOxygenTreatmentSummary"></a> GetPatientBCOxygenTreatmentSummary\(\)

B/C 환자의 산소 처치 완료 조건을 디버그 요약에 표시한다.
CareZone 장비 참조와 실제 산소 라인 연결을 혼동하지 않기 위한 진단 정보다.

```csharp
public string GetPatientBCOxygenTreatmentSummary()
```

#### Returns

 string

### <a id="TriageTrainer_Entity_PatientController_GetStateEventNames"></a> GetStateEventNames\(\)

이 엔티티가 지원하는 상태 이벤트 이름 목록(검토/검증용).

```csharp
public IReadOnlyList<string> GetStateEventNames()
```

#### Returns

 IReadOnlyList<string\>

### <a id="TriageTrainer_Entity_PatientController_HideTreatmentDisplay_TriageTrainer_Entity_PatientController_TreatmentDisplay_"></a> HideTreatmentDisplay\(TreatmentDisplay\)

처치 표현을 끈다(반복 사이클 리셋 등).

```csharp
public void HideTreatmentDisplay(PatientController.TreatmentDisplay display)
```

#### Parameters

`display` [PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)

### <a id="TriageTrainer_Entity_PatientController_IsClineConnectionAvailable"></a> IsClineConnectionAvailable\(\)

C-line(중심정맥관) 연결이 가능한지 판정한다.
중심정맥관 삽입 시각 표현이 활성화되어 있고, 연결 지점이 존재하며,
아직 연결되지 않았을 때 <code>true</code>를 반환한다.

```csharp
public bool IsClineConnectionAvailable()
```

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_IsInteractEnabled_System_String_"></a> IsInteractEnabled\(string\)

```csharp
public bool IsInteractEnabled(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_IsTreatmentApplied_System_String_"></a> IsTreatmentApplied\(string\)

```csharp
public bool IsTreatmentApplied(string treatmentIdentifier)
```

#### Parameters

`treatmentIdentifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_IsTreatmentDisplayActive_TriageTrainer_Entity_PatientController_TreatmentDisplay_"></a> IsTreatmentDisplayActive\(TreatmentDisplay\)

처치 시각 표현이 이미 켜져 있는지 조회한다.
시나리오 준비 경로가 같은 표현을 매 프레임 다시 켜서 불필요한 RPC 를 내보내지 않도록 공개한다.

```csharp
public bool IsTreatmentDisplayActive(PatientController.TreatmentDisplay display)
```

#### Parameters

`display` [PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_MarkMedicalStateDirty"></a> MarkMedicalStateDirty\(\)

```csharp
public void MarkMedicalStateDirty()
```

### <a id="TriageTrainer_Entity_PatientController_NotifyOxygenLineConnected"></a> NotifyOxygenLineConnected\(\)

유량계 참조가 이미 환자에게 설정된 뒤 산소 라인이 완성된 경우에도
산소 공급 처치의 연결 신호를 한 번 평가한다.

```csharp
public void NotifyOxygenLineConnected()
```

### <a id="TriageTrainer_Entity_PatientController_NotifyPatientBCNormalSalineDisconnected_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> NotifyPatientBCNormalSalineDisconnected\(IntravenousLineConnectionPoint\)

```csharp
public void NotifyPatientBCNormalSalineDisconnected(IntravenousLineConnectionPoint salinePoint)
```

#### Parameters

`salinePoint` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_OnAttacked_MultiplayerInfrastructure_Entity_Entity_System_Int32_"></a> OnAttacked\(Entity, int\)

```csharp
public void OnAttacked(Entity attacker, int damageAmount)
```

#### Parameters

`attacker` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

`damageAmount` int

### <a id="TriageTrainer_Entity_PatientController_OnItemUsed_MultiplayerInfrastructure_Entity_Entity_System_String_"></a> OnItemUsed\(Entity, string\)

아이템 사용 대상으로서의 처리(<xref href="MultiplayerInfrastructure.Entity.IItemUseTarget" data-throw-if-not-resolved="false"></xref>): 코드 하드코딩 매핑(<xref href="TriageTrainer.Entity.PatientController.ApplyItemUse(System.String)" data-throw-if-not-resolved="false"></xref>)에 따라
처치 시각 표현을 켜고 시나리오 게이팅 신호를 올린다.

```csharp
public bool OnItemUsed(Entity user, string itemIdentifier)
```

#### Parameters

`user` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

`itemIdentifier` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_OnMovingPatientBedAttachedEnter"></a> OnMovingPatientBedAttachedEnter\(\)

```csharp
public void OnMovingPatientBedAttachedEnter()
```

### <a id="TriageTrainer_Entity_PatientController_OnMovingPatientBedAttachedExit"></a> OnMovingPatientBedAttachedExit\(\)

```csharp
public void OnMovingPatientBedAttachedExit()
```

### <a id="TriageTrainer_Entity_PatientController_OnPlayerAttachedEnter"></a> OnPlayerAttachedEnter\(\)

```csharp
public void OnPlayerAttachedEnter()
```

### <a id="TriageTrainer_Entity_PatientController_OnPlayerAttachedExit"></a> OnPlayerAttachedExit\(\)

```csharp
public void OnPlayerAttachedExit()
```

### <a id="TriageTrainer_Entity_PatientController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="TriageTrainer_Entity_PatientController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="TriageTrainer_Entity_PatientController_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_PatientController_RefreshPatientDisplayName"></a> RefreshPatientDisplayName\(\)

```csharp
public void RefreshPatientDisplayName()
```

### <a id="TriageTrainer_Entity_PatientController_RegisterMedicalStateListener_TriageTrainer_Entity_PatientController_IMedicalStateListener_"></a> RegisterMedicalStateListener\(IMedicalStateListener\)

```csharp
public void RegisterMedicalStateListener(PatientController.IMedicalStateListener listener)
```

#### Parameters

`listener` [PatientController](TriageTrainer.Entity.PatientController.md).[IMedicalStateListener](TriageTrainer.Entity.PatientController.IMedicalStateListener.md)

### <a id="TriageTrainer_Entity_PatientController_RegisterStateEventListener_System_String_System_String_System_Action_System_String__"></a> RegisterStateEventListener\(string, string, Action<string\>\)

명명된 상태 이벤트에 대한 리스너를 등록한다.

```csharp
public bool RegisterStateEventListener(string eventName, string key, Action<string> onFired)
```

#### Parameters

`eventName` string

이벤트 이름(구현체가 정의; <xref href="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource.GetStateEventNames" data-throw-if-not-resolved="false"></xref> 중 하나).

`key` string

이벤트 세부 대상 필터(구현체가 해석). null/빈 문자열이면 해당 이벤트의 모든 발생에 매칭된다.
콜백에는 실제 발생 key 가 전달된다.

`onFired` Action<string\>

이벤트 발생 시 호출되는 콜백. 인자는 실제 발생 key(없으면 null).

#### Returns

 bool

이벤트 이름을 인식하여 등록했으면 true.

### <a id="TriageTrainer_Entity_PatientController_RemoveInteract_System_String_"></a> RemoveInteract\(string\)

```csharp
public void RemoveInteract(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_PatientController_Reset"></a> Reset\(\)

```csharp
protected override void Reset()
```

### <a id="TriageTrainer_Entity_PatientController_ResetTriageAssessmentForRetry"></a> ResetTriageAssessmentForRetry\(\)

시나리오가 오답인 환자만 다시 분류시킬 때 사용한다. 이전 등급과 정답 제출로 닫힌
상호작용 상태를 함께 초기화해, 재시도 인터랙션이 확실히 다시 노출되게 한다.

```csharp
public void ResetTriageAssessmentForRetry()
```

### <a id="TriageTrainer_Entity_PatientController_RestorePatientAFluidConnectionForScenario_System_Boolean_"></a> RestorePatientAFluidConnectionForScenario\(bool\)

시나리오 수동 진입용으로 해당 팔의 수액 연결을 서버 권위로 복원한다.

<p>
플레이어 상호작용 경로와 달리 수액세트를 소비하지 않고, 단계 진행 신호
(<code>connect_cannula_and_ns1</code>·<code>connect_ps1_right</code>)도 올리지 않는다. 준비 체인은
지나간 단계의 신호를 다시 발생시켜 다음 단계를 통과시키는 방식을 쓰지 않고 상태만 목표
단계에 맞춘다. 다만 줄이 실제로 만들어지면 연결 지점 자신이 <code>iv_connected_</code> 접두사
신호를 올리는데, 이는 연결이 성립한 결과이고 이 시나리오의 게이트가 참조하지 않는다.
캐뉼라 삽입 표시와 침대 수액 설치가 선행되어야 하며, 이미 연결되어 있으면 아무것도 하지
않고 <code>true</code> 를 돌려준다.
</p>

```csharp
public bool RestorePatientAFluidConnectionForScenario(bool isLeftArm)
```

#### Parameters

`isLeftArm` bool

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_SetAssessActionEnabled_System_String_System_Boolean_"></a> SetAssessActionEnabled\(string, bool\)

시나리오 진행에 따라 특정 사정 동작의 노출을 켜고 끈다.

```csharp
public void SetAssessActionEnabled(string identifier, bool enabled)
```

#### Parameters

`identifier` string

`enabled` bool

### <a id="TriageTrainer_Entity_PatientController_SetConnectedOxyflowmeter_TriageTrainer_Entity_WallAttachedOxyflowmeter_"></a> SetConnectedOxyflowmeter\(WallAttachedOxyflowmeter\)

```csharp
public void SetConnectedOxyflowmeter(WallAttachedOxyflowmeter flowmeter)
```

#### Parameters

`flowmeter` [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

### <a id="TriageTrainer_Entity_PatientController_SetConnectedOxyflowmeterConnections_System_Collections_Generic_IReadOnlyList_TriageTrainer_Entity_WallAttachedOxyflowmeter__"></a> SetConnectedOxyflowmeterConnections\(IReadOnlyList<WallAttachedOxyflowmeter\>\)

```csharp
public void SetConnectedOxyflowmeterConnections(IReadOnlyList<WallAttachedOxyflowmeter> sources)
```

#### Parameters

`sources` IReadOnlyList<[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)\>

### <a id="TriageTrainer_Entity_PatientController_SetConnectedWallSuction_TriageTrainer_Entity_WallAttachedWallSuction_"></a> SetConnectedWallSuction\(WallAttachedWallSuction\)

```csharp
public void SetConnectedWallSuction(WallAttachedWallSuction suction)
```

#### Parameters

`suction` [WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)

### <a id="TriageTrainer_Entity_PatientController_SetConnectedWallSuctionConnections_System_Collections_Generic_IReadOnlyList_TriageTrainer_Entity_WallAttachedWallSuction__"></a> SetConnectedWallSuctionConnections\(IReadOnlyList<WallAttachedWallSuction\>\)

```csharp
public void SetConnectedWallSuctionConnections(IReadOnlyList<WallAttachedWallSuction> sources)
```

#### Parameters

`sources` IReadOnlyList<[WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)\>

### <a id="TriageTrainer_Entity_PatientController_SetCurrentBed_TriageTrainer_Entity_MovingPatientBedController_"></a> SetCurrentBed\(MovingPatientBedController\)

```csharp
public void SetCurrentBed(MovingPatientBedController bed)
```

#### Parameters

`bed` [MovingPatientBedController](TriageTrainer.Entity.MovingPatientBedController.md)

### <a id="TriageTrainer_Entity_PatientController_SetIVFluidConnection_System_Boolean_UnityEngine_MonoBehaviour_"></a> SetIVFluidConnection\(bool, MonoBehaviour\)

IV 수액 연결을 설정한다. 기존 연결이 있으면 해제 후 새 연결로 교체한다.

```csharp
public void SetIVFluidConnection(bool isLeftArm, MonoBehaviour fluidSource)
```

#### Parameters

`isLeftArm` bool

true=좌측 팔, false=우측 팔

`fluidSource` MonoBehaviour

수액 공급원 컴포넌트(침대 또는 급속주입기). null이면 해제.

### <a id="TriageTrainer_Entity_PatientController_SetInteractEnabled_System_String_System_Boolean_"></a> SetInteractEnabled\(string, bool\)

```csharp
public void SetInteractEnabled(string identifier, bool enabled)
```

#### Parameters

`identifier` string

`enabled` bool

### <a id="TriageTrainer_Entity_PatientController_SetIntravenousLineCannulaInteractable_System_Boolean_"></a> SetIntravenousLineCannulaInteractable\(bool\)

시나리오 진행에 따라 정맥라인 캐뉼라 상호작용 가능 여부(State)를 켜고 끈다.

```csharp
public void SetIntravenousLineCannulaInteractable(bool interactable)
```

#### Parameters

`interactable` bool

### <a id="TriageTrainer_Entity_PatientController_SetMonitorMedicalState_TriageTrainer_Entity_Patient_ECGParameters_TriageTrainer_Entity_Patient_ARTParameters_TriageTrainer_Entity_Patient_CVPParameters_TriageTrainer_Entity_Patient_PlethParameters_TriageTrainer_Entity_Patient_NumericsParameters_TriageTrainer_Entity_Patient_NIBPParameters_TriageTrainer_Entity_Patient_TemperatureParameters_TriageTrainer_Entity_Patient_STLeadValues_System_Boolean_"></a> SetMonitorMedicalState\(ECGParameters, ARTParameters, CVPParameters, PlethParameters, NumericsParameters, NIBPParameters, TemperatureParameters, STLeadValues, bool\)

```csharp
public void SetMonitorMedicalState(ECGParameters ecg, ARTParameters art, CVPParameters cvp, PlethParameters pleth, NumericsParameters numerics, NIBPParameters nibp, TemperatureParameters temperature, STLeadValues stLeads, bool notify = true)
```

#### Parameters

`ecg` [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

`art` [ARTParameters](TriageTrainer.Entity.Patient.ARTParameters.md)

`cvp` [CVPParameters](TriageTrainer.Entity.Patient.CVPParameters.md)

`pleth` [PlethParameters](TriageTrainer.Entity.Patient.PlethParameters.md)

`numerics` [NumericsParameters](TriageTrainer.Entity.Patient.NumericsParameters.md)

`nibp` [NIBPParameters](TriageTrainer.Entity.Patient.NIBPParameters.md)

`temperature` [TemperatureParameters](TriageTrainer.Entity.Patient.TemperatureParameters.md)

`stLeads` [STLeadValues](TriageTrainer.Entity.Patient.STLeadValues.md)

`notify` bool

### <a id="TriageTrainer_Entity_PatientController_SetMonitorSelectionRequester_TriageTrainer_Entity_PatientController_IMonitorSelectionRequester_"></a> SetMonitorSelectionRequester\(IMonitorSelectionRequester\)

```csharp
public void SetMonitorSelectionRequester(PatientController.IMonitorSelectionRequester requester)
```

#### Parameters

`requester` [PatientController](TriageTrainer.Entity.PatientController.md).[IMonitorSelectionRequester](TriageTrainer.Entity.PatientController.IMonitorSelectionRequester.md)

### <a id="TriageTrainer_Entity_PatientController_SetMonitoringPatientMonitor_TriageTrainer_Entity_PatientMonitor_Models_PatientMonitorController_"></a> SetMonitoringPatientMonitor\(PatientMonitorController\)

환자 모니터가 이 환자를 모니터링 대상으로 바인딩할 때 호출한다.

```csharp
public void SetMonitoringPatientMonitor(PatientMonitorController monitor)
```

#### Parameters

`monitor` [PatientMonitorController](TriageTrainer.Entity.PatientMonitor.Models.PatientMonitorController.md)

### <a id="TriageTrainer_Entity_PatientController_SetNamedChildActive_System_String_System_Boolean_"></a> SetNamedChildActive\(string, bool\)

```csharp
public bool SetNamedChildActive(string childName, bool active)
```

#### Parameters

`childName` string

`active` bool

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_SetOxygenMaskAttachmentPointFromPatientComponent_TriageTrainer_Entity_OxyLine_OxyLineConnectionPoint_"></a> SetOxygenMaskAttachmentPointFromPatientComponent\(OxyLineConnectionPoint\)

```csharp
public void SetOxygenMaskAttachmentPointFromPatientComponent(OxyLineConnectionPoint point)
```

#### Parameters

`point` [OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_SetPatientBCIvAttachmentPointFromPatientComponent_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> SetPatientBCIvAttachmentPointFromPatientComponent\(IntravenousLineConnectionPoint\)

환자 유형별 State 컴포넌트가 프리팹에서 참조한 B/C 정맥로 IV 연결 지점을 주입한다.
(<xref href="TriageTrainer.Entity.PatientController.SetOxygenMaskAttachmentPointFromPatientComponent(TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint)" data-throw-if-not-resolved="false"></xref> 과 동일한 관례.)

```csharp
public void SetPatientBCIvAttachmentPointFromPatientComponent(IntravenousLineConnectionPoint point)
```

#### Parameters

`point` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_PatientController_SetResuscitationMedicationRound_System_Int32_"></a> SetResuscitationMedicationRound\(int\)

동일한 조합 주사기를 사용하는 소생술 투여 회차를 전환합니다.

```csharp
public void SetResuscitationMedicationRound(int round)
```

#### Parameters

`round` int

### <a id="TriageTrainer_Entity_PatientController_SetTreatmentApplied_System_String_System_Boolean_TriageTrainer_Entity_PatientController_TreatmentDisplay_"></a> SetTreatmentApplied\(string, bool, TreatmentDisplay\)

처치 상태를 권위 데이터로 변경하고 대응 Display 상태를 연달아 반영한다.

```csharp
public bool SetTreatmentApplied(string treatmentIdentifier, bool applied, PatientController.TreatmentDisplay display = TreatmentDisplay.None)
```

#### Parameters

`treatmentIdentifier` string

`applied` bool

`display` [PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_SetTreatmentDisplayNetworked_TriageTrainer_Entity_PatientController_TreatmentDisplay_System_Boolean_"></a> SetTreatmentDisplayNetworked\(TreatmentDisplay, bool\)

처치 표시 상태를 네트워크 전체에 동기화해서 설정한다.
서버에서 호출하면 로컬 적용 후 즉시 모든 클라이언트에 전파하고,
클라이언트에서 호출하면 ServerRpc 를 통해 서버를 경유한다.

```csharp
public void SetTreatmentDisplayNetworked(PatientController.TreatmentDisplay display, bool active)
```

#### Parameters

`display` [PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)

`active` bool

### <a id="TriageTrainer_Entity_PatientController_SetTreatmentStateSnapshot_System_Collections_Generic_IEnumerable_System_String__"></a> SetTreatmentStateSnapshot\(IEnumerable<string\>\)

시나리오 수동 진입처럼 여러 처치 상태를 한 번에 복원해야 할 때 사용한다.
개별 상태 전환을 흉내 내지 않고 권위 상태와 클라이언트 복제본을 같은 스냅샷으로 맞춘다.

```csharp
public bool SetTreatmentStateSnapshot(IEnumerable<string> treatmentIdentifiers)
```

#### Parameters

`treatmentIdentifiers` IEnumerable<string\>

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_SetTriageAssessable_System_Boolean_"></a> SetTriageAssessable\(bool\)

시나리오 진행에 따라 트리아지 인터랙션 노출을 켜고 끈다.

```csharp
public void SetTriageAssessable(bool assessable)
```

#### Parameters

`assessable` bool

### <a id="TriageTrainer_Entity_PatientController_ShowTreatmentDisplay_TriageTrainer_Entity_PatientController_TreatmentDisplay_"></a> ShowTreatmentDisplay\(TreatmentDisplay\)

처치 표현을 켠다: <xref href="TriageTrainer.Patient.PatientDisplayState" data-throw-if-not-resolved="false"></xref> 의 <code>DisplayState</code> 플래그를 true 로 설정하고
대응 <code>ChildGameObjects</code> GameObject 를 <code>SetActive(true)</code> 한다. (데이터는 State, 적용은 Controller)

```csharp
public void ShowTreatmentDisplay(PatientController.TreatmentDisplay display)
```

#### Parameters

`display` [PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)

### <a id="TriageTrainer_Entity_PatientController_SubmitTriageAssessment_TriageTrainer_Entity_Patient_TriageLevel_"></a> SubmitTriageAssessment\(TriageLevel\)

트리아지 UI 에서 선택된 등급을 환자에 적용한다(네트워크 전파). 로컬 플레이어 클라이언트에서 호출된다.

```csharp
public void SubmitTriageAssessment(TriageLevel level)
```

#### Parameters

`level` [TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)

### <a id="TriageTrainer_Entity_PatientController_SyncAllDisplayStatesNetworked"></a> SyncAllDisplayStatesNetworked\(\)

현재 패치된 모든 DisplayState 를 한 번에 모든 클라이언트에 동기화한다.
초기 상태 일괄 설정(EntityInit) 후 반드시 서버에서 호출해야 늦은 입장 클라이언트에
전체 상태가 정확히 전달된다.

```csharp
public void SyncAllDisplayStatesNetworked()
```

### <a id="TriageTrainer_Entity_PatientController_TryAttachCurrentHandlingItem_MultiplayerInfrastructure_Entity_Entity_"></a> TryAttachCurrentHandlingItem\(Entity\)

```csharp
public bool TryAttachCurrentHandlingItem(Entity actorEntity)
```

#### Parameters

`actorEntity` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TryCompletePatientBCNormalSalineConnection_TriageTrainer_Entity_IntravenousLine_IntravenousLineConnectionPoint_"></a> TryCompletePatientBCNormalSalineConnection\(IntravenousLineConnectionPoint\)

```csharp
public bool TryCompletePatientBCNormalSalineConnection(IntravenousLineConnectionPoint salinePoint = null)
```

#### Parameters

`salinePoint` [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TryGetLayingOnMovingBedOffsets_UnityEngine_Vector3__UnityEngine_Quaternion__"></a> TryGetLayingOnMovingBedOffsets\(out Vector3, out Quaternion\)

```csharp
public bool TryGetLayingOnMovingBedOffsets(out Vector3 modelLocalPosition, out Quaternion modelLocalRotation)
```

#### Parameters

`modelLocalPosition` Vector3

`modelLocalRotation` Quaternion

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TryPlayAnimation_System_String_System_Single_System_Int32_System_Single_"></a> TryPlayAnimation\(string, float, int, float\)

```csharp
public bool TryPlayAnimation(string stateName, float transitionSeconds = 0, int layer = 0, float normalizedTime = 0)
```

#### Parameters

`stateName` string

`transitionSeconds` float

`layer` int

`normalizedTime` float

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TryResetAnimationTrigger_System_String_"></a> TryResetAnimationTrigger\(string\)

```csharp
public bool TryResetAnimationTrigger(string parameter)
```

#### Parameters

`parameter` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TrySetAnimationBool_System_String_System_Boolean_"></a> TrySetAnimationBool\(string, bool\)

```csharp
public bool TrySetAnimationBool(string parameter, bool value)
```

#### Parameters

`parameter` string

`value` bool

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TrySetAnimationController_UnityEngine_RuntimeAnimatorController_"></a> TrySetAnimationController\(RuntimeAnimatorController\)

```csharp
public bool TrySetAnimationController(RuntimeAnimatorController controller)
```

#### Parameters

`controller` RuntimeAnimatorController

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TrySetAnimationFloat_System_String_System_Single_"></a> TrySetAnimationFloat\(string, float\)

```csharp
public bool TrySetAnimationFloat(string parameter, float value)
```

#### Parameters

`parameter` string

`value` float

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TrySetAnimationInt_System_String_System_Int32_"></a> TrySetAnimationInt\(string, int\)

```csharp
public bool TrySetAnimationInt(string parameter, int value)
```

#### Parameters

`parameter` string

`value` int

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_TrySetAnimationTrigger_System_String_"></a> TrySetAnimationTrigger\(string\)

```csharp
public bool TrySetAnimationTrigger(string parameter)
```

#### Parameters

`parameter` string

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientController_UnregisterMedicalStateListener_TriageTrainer_Entity_PatientController_IMedicalStateListener_"></a> UnregisterMedicalStateListener\(IMedicalStateListener\)

```csharp
public void UnregisterMedicalStateListener(PatientController.IMedicalStateListener listener)
```

#### Parameters

`listener` [PatientController](TriageTrainer.Entity.PatientController.md).[IMedicalStateListener](TriageTrainer.Entity.PatientController.IMedicalStateListener.md)

### <a id="TriageTrainer_Entity_PatientController_UnregisterStateEventListeners_System_String_"></a> UnregisterStateEventListeners\(string\)

등록된 상태 이벤트 리스너를 해제한다.
<code class="paramref">eventName</code> 가 비어 있으면 이 엔티티의 모든 리스너를 해제한다.

```csharp
public void UnregisterStateEventListeners(string eventName)
```

#### Parameters

`eventName` string

### <a id="TriageTrainer_Entity_PatientController_OnEquipmentConnected"></a> OnEquipmentConnected

장비가 이 환자에게 연결되었을 때 발생.
인자: (장비 유형 식별자, 장비 컴포넌트 참조).

```csharp
public event Action<string, MonoBehaviour> OnEquipmentConnected
```

#### Event Type

 Action<string, MonoBehaviour\>

### <a id="TriageTrainer_Entity_PatientController_OnEquipmentDisconnected"></a> OnEquipmentDisconnected

장비가 이 환자로부터 해제되었을 때 발생.
인자: (장비 유형 식별자, 해제된 장비 컴포넌트 참조).

```csharp
public event Action<string, MonoBehaviour> OnEquipmentDisconnected
```

#### Event Type

 Action<string, MonoBehaviour\>

### <a id="TriageTrainer_Entity_PatientController_OnTreatmentApplied"></a> OnTreatmentApplied

처치 표시 항목이 새로 켜졌을 때(false→true) 발생. 인자는 켜진 항목.

```csharp
public event Action<PatientController.TreatmentDisplay> OnTreatmentApplied
```

#### Event Type

 Action<[PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)\>

### <a id="TriageTrainer_Entity_PatientController_OnTreatmentRemoved"></a> OnTreatmentRemoved

처치 표시 항목이 꺼졌을 때(true→false) 발생. 인자는 꺼진 항목.

```csharp
public event Action<PatientController.TreatmentDisplay> OnTreatmentRemoved
```

#### Event Type

 Action<[PatientController](TriageTrainer.Entity.PatientController.md).[TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)\>

### <a id="TriageTrainer_Entity_PatientController_OnTriageSubmitted"></a> OnTriageSubmitted

트리아지 등급이 제출/확정되었을 때 발생. 인자는 확정된 등급.

```csharp
public event Action<TriageLevel> OnTriageSubmitted
```

#### Event Type

 Action<[TriageLevel](TriageTrainer.Entity.Patient.TriageLevel.md)\>

### <a id="TriageTrainer_Entity_PatientController_OnVitalChanged"></a> OnVitalChanged

의료 상태(활력/모니터 수치 포함)가 변경되었을 때 발생. 인자는 변경된 스냅샷.

```csharp
public event Action<PatientMedicalState> OnVitalChanged
```

#### Event Type

 Action<[PatientMedicalState](TriageTrainer.Entity.Patient.PatientMedicalState.md)\>

