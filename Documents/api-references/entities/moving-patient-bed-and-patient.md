# Moving Patient Bed / Patient API Reference

## 0. 이 문서가 다루는 범위

이 문서는 다음 구현의 현재 API와 동작 흐름을 정리한다.

1. `MultiplayerInfrastructure.Entity.IReposable`
2. `MultiplayerInfrastructure.Player.PlayerController.ReposableCarry`(연계 사용)
3. `TriageTrainer.Entity.MovingPatientBedController`
4. `TriageTrainer.Entity.PatientController`(침대 연동 범위)

## 1. 파일/네임스페이스 맵

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IReposable.cs`
  - `namespace MultiplayerInfrastructure.Entity`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.ReposableCarry.cs`
  - `namespace MultiplayerInfrastructure.Player`
- `Assets/Modules/TriageTrainer/Scripts/Entities/MovingPatientBed/MovingPatientBedController.cs`
  - `namespace TriageTrainer.Entity`
- `Assets/Modules/TriageTrainer/Scripts/Entities/MovingPatientBed/MovingPatientBedPatientAttachPointObject.cs`
  - `namespace TriageTrainer.Entity`
- `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.cs`
  - `namespace TriageTrainer.Entity`
- `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.Interactions.cs`
  - `namespace TriageTrainer.Entity`

## 2. 핵심 메커니즘

### 2.1 `IReposable` 기반 눕힘/들기 계약

침대는 구체 타입 대신 `IReposable` 인터페이스만 사용한다.

- `Weight`로 협동 인원 계산
- `OnMovingPatientBedAttachedEnter/Exit`로 침대 부착 상태 전환
- 플레이어 운반 전환은 `PlayerController.TryPickUpReposable(...)`로 위임

### 2.2 침대 인터랙션 구성

`MovingPatientBedController`는 `IInteractable`, `IInteract`, `IInteractorConditional`을 구현한다.

- 기본 인터랙션(`this`): 침대 이동 모드 참가/해제
- 보조 인터랙션(`BedReposeInteract`): 운반 중인 환자를 침대에 내려놓기
- `Interacts`는 위 두 액션을 모두 반환한다.

### 2.3 협동 이동 계산 방식

이동은 "대표 1인 추종"이 아니라 "참여자 입력 합산" 방식으로 동작한다.

- 각 참여자의 `CurrentMoveInputVector`를 합산
- 전진/회전 비율을 `RequiredInteractorCount` 기준으로 정규화
- `RequiredInteractorCount = max(침대 Weight, 눕혀진 대상 Weight)`
- `_movementBlockingMask` Linecast에 걸리면 해당 프레임 이동 취소

### 2.4 토글/홀드 모드

`BedInteractionMode`:

- `Toggle`: 상호작용 시 참가/해제를 토글
- `Hold`: 상호작용 키를 떼면 참가 해제(`CleanupReleasedHoldInteractors`)

공통으로 `LeftShift` 입력 시 참여 해제가 가능하다.

### 2.5 부착 포인트와 기본 자동 생성

침대는 두 종류의 부착 포인트를 사용한다.

- `PlayerAttachPoints` (`RidableAttachPointObject`)
- `PatientAttachPoints` (`MovingPatientBedPatientAttachPointObject`)

인스펙터에 포인트가 없으면 런타임에 기본 포인트를 자동 생성한다.

- `PlayerAttachPoint` 기본 로컬 위치: `(0, 0, -0.8)`
- `PatientAttachPoint` 기본 로컬 위치: `(0, 0.9, 0)`

`MovingPatientBedPatientAttachPointObject`는 편집기 Gizmo 구체만 표시하는 마커 컴포넌트다.

### 2.6 아이템 시각 오브젝트 활성화

침대/환자 모두 `itemIdentifier -> visualObject` 매핑을 직렬화 리스트로 보관하고, 런타임 딕셔너리로 조회한다.

- `OnAttacked(...)`: 공격자 플레이어의 `HandlingItem.CurrentIdentifier`를 확인
- `OnItemUsed(..., itemIdentifier)`: 전달된 식별자를 즉시 사용
- 매핑이 존재하면 대응 오브젝트를 `SetActive(true)`

### 2.7 채팅 메시지 스로틀

침대/환자 모두 동일 메시지 반복 노출을 제한한다.

- 키: `interactorInstanceId + ":" + message`
- 3초 내 동일 키는 무시

## 3. 주요 API

### 3.1 `MovingPatientBedController`

- `Identifier`: 런타임 식별자(서버 할당값 우선, 없으면 타입 식별자)
- `Weight`: 침대 무게(0 미만 방지)
- `ReposedTarget`: 현재 침대에 눕혀진 `IReposable`
- `RequiredInteractorCount`: 협동 이동 최소 필요 인원
- `SetIdentifier(string identifier)`: 서버가 런타임 엔티티 식별자 할당 및 Registry 등록
- `Interact(Transform interactor)`: 이동 참가/해제
- `CanInteract(Transform interactor)`: 운반 중 플레이어 등의 상호작용 가능 여부
- `TryReposeTarget(IReposable target, Transform interactor = null)`: 대상 눕히기
- `TryLiftTarget(PlayerController player, out IReposable lifted)`: 침대에서 대상 들어올리기
- `TryAttachCurrentHandlingItem(...)` / `TryAttachItem(string itemIdentifier)`: 시각 오브젝트 활성화

### 3.2 `PatientController`(침대 연동 범위)

`PatientController` 자체는 `IInteract`를 직접 구현하지 않고, partial(`PatientController.Interactions.cs`)에서 `Interacts`를 구성한다.

침대 연동에 직접 관련된 API:

- `Weight`, `IsReposed`, `CurrentBed`, `CarryAttachPoint`
- `SetCurrentBed(MovingPatientBedController bed)`
- `OnMovingPatientBedAttachedEnter/Exit()`
- `OnPlayerAttachedEnter/Exit()`
- `TryAttachCurrentHandlingItem(...)` / `TryAttachItem(...)`

환자 상호작용/의료 상태 전체 API는 별도 문서 참조:

- `Documents/api-references/entities/patient-controller-reference.md`

## 4. Unity 설정 체크 포인트

- 침대의 `_reposeAnchor`, `PlayerAttachPoints`, `PatientAttachPoints`를 프리팹 기준으로 확인
- `_attachableItemVisualPairs`에 식별자와 시각 오브젝트 매핑이 누락되지 않았는지 확인
- 환자 `CarryAttachPoint`가 없으면 자동 생성되지만, 의도된 파지 위치가 있으면 명시 지정
- 협동 이동 인원 정책이 의도와 맞는지 `Weight` / 환자 `Weight`로 검증

## 5. 제한 사항

- 침대 이동은 로컬 입력 합산 기반이며, 고급 물리/네트워크 보정은 별도 계층에서 처리 필요
- 눕힘/들기 실패 시 피드백은 채팅 메시지 중심이며, 별도 UI 상태 배지 연동은 제공하지 않음
- 아이템 부착은 "오브젝트 활성화" 방식이며 런타임 생성/조립 파이프라인은 포함하지 않음

## 6. 관련 문서

- `Documents/requirements/interaction/triage-moving-patient-bed-requirements.md`
- `Documents/api-references/MultiplayerInfrastructure.Player.PlayerController.md`
- `Documents/api-references/entities/patient-controller-reference.md`
