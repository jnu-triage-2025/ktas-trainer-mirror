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

아이템 식별자와 자식 시각 오브젝트를 인스펙터 리스트로 등록하고 런타임 딕셔너리로 조회한다.

- `TryAttachCurrentHandlingItem(Entity actorEntity)`
  - 공격/사용 주체 플레이어의 `HandlingItem.CurrentIdentifier`를 기준으로 부착 시도
- `TryAttachItem(string itemIdentifier)`
  - 식별자 매핑이 있으면 오브젝트 활성화
- `OnAttacked(...)`, `OnItemUsed(...)`
  - 위 API를 호출하는 엔트리 포인트

## 5. 의료 상태 API

### 5.1 핵심 모델

- `Descriptor`: `PatientDescriptor` (환자 기본 프로필)
- `MedicalState`: `PatientMedicalState` (진단/활력/모니터 파라미터)

### 5.2 의료 상태 변경 알림

- `RegisterMedicalStateListener(IMedicalStateListener listener)`
- `UnregisterMedicalStateListener(IMedicalStateListener listener)`
- `MarkMedicalStateDirty()`

리스너는 `HandleMedicalStateChanged(PatientMedicalState state)`를 통해 스냅샷을 전달받는다.

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
- Display: `_liftDisplayText`, `_carryDisplayText`, `_monitorSelectDisplayText` 및 각 아이콘
- Patient: `_weight`
- Medical: `_patientDescriptor`, `_medicalState`
- Interact: `_interactConfigs`
- Collider: `_capsuleCenter`, `_capsuleHeight`, `_capsuleRadius`
- Visual: `_attachableItemVisualPairs`

## 9. 관련 문서

- `Documents/requirements/patient/triage-patient-models-requirements.md`
- `Documents/requirements/interaction/triage-moving-patient-bed-requirements.md`
- `Documents/api-references/TriageTrainer.Entity.PatientMonitor.md`
