# API 레퍼런스: `TriageTrainer.Entity.PatientController`

> 네임스페이스: `TriageTrainer.Entity`
>
> 파일 위치:
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.Interactions.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.MedicalState.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.Network.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.Animation.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.State.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.Collider.cs`

## 0. 개요

`PatientController`는 환자 엔티티의 상호작용, 운반/침대 연동, 의료 상태, 애니메이션 제어를 통합하는 partial 컴포넌트다.

- 구현 인터페이스: `IInteractable`, `IReposable`
- 기반 클래스: `FishNet.Object.NetworkBehaviour`

## 1. partial 파일별 책임

| 파일 | 책임 |
|---|---|
| `PatientController.cs` | 공통 런타임 상태, 침대/플레이어 부착 상태, 아이템 시각 오브젝트 매핑 |
| `PatientController.Interactions.cs` | 환자 상호작용 항목(`lift/carry/monitor_select`) 구성 |
| `PatientController.MedicalState.cs` | 의료 상태 접근 API + 모니터 파라미터 네트워크 동기화 |
| `PatientController.Network.cs` | 환자 엔티티 Registry 등록/해제 |
| `PatientController.Animation.cs` | Humanoid 애니메이션 제어 래퍼 API |
| `PatientController.State.cs` | `PatientDisplayState` 캐시 조회 |
| `PatientController.Collider.cs` | 캡슐 콜라이더 기본값 적용 |

## 2. 상호작용 API

### 2.1 상호작용 식별자

- `InteractIdLiftFromBed = "lift_from_bed"`
- `InteractIdCarry = "carry_patient"`
- `InteractIdMonitorSelect = "monitor_select"`

### 2.2 제공 인터랙션

`Interacts`는 내부적으로 다음 액션을 구성해 반환한다.

- `PatientLiftInteract`: 침대 위 환자 들어올리기
- `PatientCarryInteract`: 침대 밖 환자 직접 들기
- `PatientMonitorSelectInteract`: 모니터 선택 모드에서 환자 선택

`monitor_select`는 항상 활성으로 간주되며, 실제 노출은 `PlayerController.IsPatientSelectionMode`에 의해 제어된다.

### 2.3 상호작용 제어 API

- `SetInteractEnabled(string identifier, bool enabled)`
- `AddInteract(string identifier, bool enabled = true)`
- `RemoveInteract(string identifier)`
- `IsInteractEnabled(string identifier)`

## 3. 운반/침대 연동 API

### 3.1 상태 프로퍼티

- `Identifier`: 환자 식별자
- `Weight`: 환자 무게(음수 방지)
- `IsReposed`: 현재 침대에 눕혀져 있는지 여부
- `CurrentBed`: 현재 연결된 `MovingPatientBedController`
- `CarryAttachPoint`: 플레이어 운반 시 붙는 앵커(없으면 자동 생성)
- `IsMovingPatientBedAttached`, `IsPlayerAttached`

### 3.2 상태 전환 메서드

- `SetCurrentBed(MovingPatientBedController bed)`
- `OnMovingPatientBedAttachedEnter()` / `OnMovingPatientBedAttachedExit()`
- `OnPlayerAttachedEnter()` / `OnPlayerAttachedExit()`

침대 연동 상세는 `Documents/api-references/entities/moving-patient-bed-and-patient.md`를 참조한다.

## 4. 아이템 부착 시각화 API

`TreatmentDisplay` enum과 `PatientTreatmentDisplayModel` 플래그를 기반으로 처치 시각 표현을 관리한다.

### 4.1 초기화 정책

- 스폰 시 `InitializeTreatmentDisplaysFromConfiguredState()`가 **모든** `TreatmentDisplay` 자식 시각 오브젝트를 비활성화한다.
- 프리팹에서 편집 편의를 위해 일부 자식이 활성화된 채 저장되어 있어도, 런타임에서는 전부 숨겨진 상태로 시작한다.
- 시나리오(`ApplyScenarioDisplayState`) 또는 아이템 사용(`ApplyItemUse`)에 의해 명시적으로 켜진 부착물만 보인다.

### 4.2 아이템 적용 흐름

- `OnItemUsed(Entity user, string itemIdentifier)` / `OnAttacked(Entity attacker, int damage)`
  - 아이템 사용/공격 이벤트 진입점
- `TryAttachCurrentHandlingItem(Entity actorEntity)`
  - 공격/사용 주체 플레이어의 `HandlingItem.CurrentIdentifier`를 기준으로 부착 시도
- `ApplyItemUse(string itemIdentifier)`
  - `ItemUseEffects` 코드 하드코딩 딕셔너리에서 아이템 식별자 → `TreatmentDisplay` + 시나리오 신호 매핑 조회
  - `ShowTreatmentDisplay(display)`로 시각 오브젝트 활성화 + `ScenarioInteractionSignals.Raise()`로 신호 발신
- `ApplyScenarioDisplayState(string displayStateName, bool active)`
  - 시나리오 `EntityInit` 노드가 호출하는 명명된 표시 상태 설정 진입점

## 5. 의료 상태 API

### 5.1 핵심 모델

- `Descriptor`: `PatientDescriptor` (환자 기본 프로필)
- `MedicalState`: `PatientMedicalState` (진단/활력/모니터 파라미터)

### 5.2 의료 상태 변경 알림

- `RegisterMedicalStateListener(IMedicalStateListener listener)`
- `UnregisterMedicalStateListener(IMedicalStateListener listener)`
- `MarkMedicalStateDirty()`

리스너는 `HandleMedicalStateChanged(PatientMedicalState state)`를 통해 스냅샷을 전달받는다.

### 5.2.1 세분화 상태 이벤트 (State Events)

`PatientController.StateEvents.cs` (부분 클래스)가 상태 변경을 세분화된 C# 이벤트로 노출하고, 범용 `IScenarioEntityStateEventSource` 를 구현한다. 값은 기존 저장소가 그대로 보유하며 이 파일은 변경 시점만 이벤트로 노출한다(값 이중화 없음).

| C# 이벤트 | 인자 | 상태 이벤트 이름 | 발생 지점 |
|---|---|---|---|
| `OnTreatmentApplied` | `TreatmentDisplay` | `TreatmentApplied` | 처치 표시 false→true 전이 |
| `OnTreatmentRemoved` | `TreatmentDisplay` | `TreatmentRemoved` | 처치 표시 true→false 전이 |
| `OnVitalChanged` | `PatientMedicalState` | `VitalChanged` | 의료 상태 변경 |
| `OnTriageSubmitted` | `TriageLevel` | `TriageSubmitted` | 트리아지 확정 |

시나리오 그래프는 `EntityStateSignalBinding` 노드로 이 이벤트를 시나리오 신호로 변환할 수 있다. 상세: [IScenarioEntityStateEventSource API](../MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource.md), [ScenarioGraphNodes — EntityStateSignalBinding](../MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md).

`GetStateEventNames()` 는 지원 이벤트 이름 목록을 런타임에 반환한다(검토/검증용).

### 5.3 모니터 파라미터 동기화

다음 계열 프로퍼티 변경은 로컬 알림과 네트워크 동기화를 함께 수행한다.

- `MedicalStateECG`, `MedicalStateART`, `MedicalStateCVP`, `MedicalStatePleth`
- `MedicalStateNumerics`, `MedicalStateNIBP`, `MedicalStateTemperature`, `MedicalStateSTLeads`

동기화 경로:

1. 서버에서 변경 시 `ObserversRpc(BufferLast = true)`로 관찰자 반영
2. 클라이언트에서 변경 시 `ServerRpc`로 서버 전달 후 재전파

## 6. 네트워크/레지스트리 동작

`OnStartClient()`에서 `_identifier` 기준으로 엔티티를 Registry에 등록하고, `OnStopClient()`/`OnDestroy()`에서 해제한다.

- 등록 타입: `EntityType.Npc`
- 등록 키: `_identifier`

## 7. 애니메이션 래퍼 API

`HumanoidAnimationController`를 찾은 뒤 제어 메서드를 래핑한다.

- `TrySetAnimationController(RuntimeAnimatorController controller)`
- `TryPlayAnimation(string stateName, float transitionSeconds = 0f, int layer = 0, float normalizedTime = 0f)`
- `TrySetAnimationBool/Float/Int(...)`
- `TrySetAnimationTrigger(string parameter)`
- `TryResetAnimationTrigger(string parameter)`

## 8. 인스펙터 핵심 필드

- Identity: `_identifier`
- Patient: `_weight`
- Medical: `_patientDescriptor`, `_medicalState`
- Interact: 인스펙터 필드 없음. 인터렉션은 `PatientController.InteractionRegistry.cs`가 코드 리터럴로 선언하고(`DeclareInteractions`), 표시 문구·노출 조건은 시나리오 JSON `interactions` 구역이 덮어쓴다. 사정 동작 목록은 `DefaultAssessActions` 코드 상수다
- Collider: `_capsuleCenter`, `_capsuleHeight`, `_capsuleRadius`
- Visual: `_attachableItemVisualPairs`

## 9. 관련 문서

- `Documents/requirements/patient/triage-patient-models-requirements.md`
- `Documents/requirements/interaction/triage-moving-patient-bed-requirements.md`
- `Documents/api-references/TriageTrainer.Entity.PatientMonitor.md`

## 10. 침대 부착 화면 표시 (Attachment Display)

### 10.1 `PatientDisplayState`의 수액걸이 필드

`PatientDisplayState` (partial) 에는 수액걸이 관련 표시 상태 및 참조가 포함되어 있다.

| 필드 | 타입 | 설명 |
|---|---|---|
| `IntravenousStandAttached` | `bool` | 환자에 수액걸이 스탠드가 부착되어있는지 여부 |
| `IntravenousStandReference` | `GameObject` | 스탠드 시각 오브젝트 참조 |
| `IntravenousHangerAttached` | `bool` | 환자에 수액걸이가 부착되어있는지 여부 |
| `IntravenousHangerReference` | `GameObject` | 수액걸이 시각 오브젝트 참조 |
| `IntravenousFluidAttached` | `bool` | 환자에 수액이 부착되어있는지 여부 |
| `IntravenousFluidReference` | `GameObject` | 수액 시각 오브젝트 참조 |

### 10.2 `PatientTreatmentDisplayModel` 확장

수액걸이 표시 플래그가 `PatientTreatmentDisplayModel`에 추가되었다.

```csharp
public bool IntravenousStandAttached;
public bool IntravenousHangerAttached;
public bool IntravenousFluidAttached;
```

### 10.3 `PatientTreatmentDisplayingChildGameObjects` 확장

대응되는 `GameObject` 참조가 추가되었다.

```csharp
public GameObject IntravenousStandAttached;
public GameObject IntravenousHangerAttached;
public GameObject IntravenousFluidAttached;
```

### 10.4 `MovingPatientBedController` Attachment Display partial

침대는 독립적인 표시 상태를 갖는다. `MovingPatientBedController.AttachmentDisplay.cs` (partial) 에서 관리한다.

| 필드 | 타입 | 설명 |
|---|---|---|
| `_intravenousStandAttached` | `bool` | 침대 수액걸이 스탠드 표시 플래그 |
| `_intravenousStandReference` | `GameObject` | 스탠드 시각 오브젝트 |
| `_intravenousHangerAttached` | `bool` | 침대 수액걸이 표시 플래그 |
| `_intravenousHangerReference` | `GameObject` | 수액걸이 시각 오브젝트 |
| `_intravenousFluidAttached` | `bool` | 침대 수액 표시 플래그 |
| `_intravenousFluidReference` | `GameObject` | 수액 시각 오브젝트 |

### 10.5 표시 동기화 메서드

- `SyncPatientAttachmentVisuals(Transform patientAnchor)`: 환자 스냅 시 호출되어 모든 수액 걸이 시각을 업데이트
- `SyncPatientAttachmentVisual(GameObject target, bool isAttached, Transform patientAnchor)`: 개별 시각 오브젝트 활성화 및 앵커 위치 맞춤
