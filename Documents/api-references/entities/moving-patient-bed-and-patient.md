# Moving Patient Bed / Patient API Reference (Current)

## 0. 이 문서가 다루는 범위

이 문서는 다음 4개 구현의 현재 사용법을 설명한다.

1. `MultiplayerInfrastructure.Entity.IReposable`  
2. `MultiplayerInfrastructure.Player.PlayerController.ReposableCarry` (partial)  
3. `TriageTrainer.Entity.MovingPatientBedController`  
4. `TriageTrainer.Entity.PatientController`

> 주의: 과거 문서에 있던 `Prefabs/Entities/...` 경로는 현재 기준으로 유효하지 않다.  
> 현재 실제 경로는 `Prefabs/Entity/...`(단수)이다.

---

## 1. 파일/네임스페이스 맵

### 1.1 인터페이스

- 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IReposable.cs`
- 네임스페이스: `MultiplayerInfrastructure.Entity`
- 시그니처: `int Weight { get; }`

### 1.2 플레이어 운반 확장

- 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.ReposableCarry.cs`
- 네임스페이스: `MultiplayerInfrastructure.Player`

### 1.3 이동식 환자 침대

- 파일: `Assets/Modules/TriageTrainer/Prefabs/Entity/MovingPatientBed/MovingPatientBedController.cs`
- 네임스페이스: `TriageTrainer.Entity`
- 구현 인터페이스: `IInteractable`, `IInteract`

### 1.4 환자

- 파일: `Assets/Modules/TriageTrainer/Prefabs/Entity/Patient/PatientController.cs`
- 네임스페이스: `TriageTrainer.Entity`
- 구현 인터페이스: `IInteractable`, `IInteract`, `IReposable`

---

## 2. 핵심 메커니즘 (학부생용 설명)

### 2.1 IReposable이 왜 필요한가?

`IReposable`은 “침대에 올릴 수 있는 대상”의 공통 약속이다.  
침대 입장에서는 객체의 구체 타입(환자, 다른 운반 대상)을 몰라도 되고, `Weight`만 알면 된다.

즉, 침대는 다음만 보면 된다.

- 이 객체가 `IReposable`인가?
- 무게(`Weight`)가 얼마인가?

이렇게 하면 침대 코드는 환자 클래스에 강하게 묶이지 않고 재사용 가능해진다.

### 2.2 PlayerController.ReposableCarry의 역할

플레이어가 “현재 무엇을 들고 있는가”를 전용 상태로 관리한다.

- `IsCarryingReposable`: 운반 중 여부
- `CarriedReposable`: 현재 운반 대상
- `TryPickUpReposable(...)`: 운반 시작
- `TryDropCarriedReposable(...)`: 운반 해제

이 상태가 있어야 침대/환자 상호작용에서 “이미 다른 대상을 들고 있는지”를 일관되게 검사할 수 있다.

### 2.3 침대 이동 규칙

`MovingPatientBedController`는 다음 원리로 동작한다.

1) 상호작용한 플레이어를 `_interactors`에 넣는다.  
2) 최소 필요 인원(`RequiredInteractorCount`)을 계산한다.  
3) 인원이 충족되면 대표 드라이버(첫 인터랙터) 위치를 따라 침대를 이동한다.

`RequiredInteractorCount`는 단순히 침대 무게만 보지 않고,

- 침대 자체 무게 (`_weight`)
- 침대 위 환자(또는 대상)의 무게 (`ReposedTarget.Weight`)

를 비교하여 더 큰 값을 사용한다.  
즉, 무거운 환자를 올리면 필요한 인원이 늘어날 수 있다.

### 2.4 환자 들어올리기/눕히기 흐름

흐름은 다음과 같다.

- 플레이어가 환자를 들고 있고 침대에 놓으면 `TryReposeTarget(...)`
- 침대 위 환자를 다시 들면 `TryLiftTarget(...)`
- 환자 객체는 `SetCurrentBed(...)`로 현재 침대 참조를 유지

그래서 환자는 “자신이 지금 침대에 있는지”(`IsReposed`)를 알 수 있고,  
침대도 “지금 누가 올라가 있는지”(`ReposedTarget`)를 알 수 있다.

### 2.5 아이템 부착 표현 메커니즘

침대/환자 모두 동일한 방식으로 아이템 부착 연출을 처리한다.

1) 인스펙터에서 `아이템 식별자 ↔ 자식 GameObject` 리스트를 등록  
2) 런타임 시작 시 딕셔너리로 캐시  
3) `TryAttachItem(itemIdentifier)` 호출 시 해당 오브젝트를 활성화

즉, 런타임 Instantiate가 아니라, 미리 배치한 오브젝트를 켜는 방식이다.

### 2.6 채팅 메시지 3초 쿨다운

같은 메시지 스팸을 막기 위해, `interactorInstanceId + message`를 키로 사용해 마지막 출력 시간을 저장한다.  
3초 이내 동일 키 메시지는 무시한다.

---

## 3. 주요 API 정리

## 3.1 MovingPatientBedController

- `Interact(Transform interactor)`
  - 침대 이동 인터랙션 on/off
- `TryReposeTarget(IReposable target, Transform interactor = null)`
  - 대상을 침대 앵커에 고정
- `TryLiftTarget(PlayerController player, out IReposable lifted)`
  - 침대에서 대상을 들어 플레이어 운반 상태로 전환
- `TryAttachItem(string itemIdentifier)`
  - 침대 부착 시각 요소 활성화
- `OnAttacked(...)`, `OnItemUsed(...)`
  - 현재 플레이어 손 아이템 식별자 기반 부착 처리 경로

## 3.2 PatientController

- `Interact(Transform interactor)`
  - 환자가 침대 위에 있을 때 플레이어가 들어올리기 시도
- `TryAttachItem(string itemIdentifier)`
  - 환자 부착 시각 요소 활성화
- `SetCurrentBed(MovingPatientBedController bed)`
  - 환자-침대 양방향 상태 동기화

## 3.3 PlayerController.ReposableCarry

- `TryPickUpReposable(IReposable reposable)`
- `TryDropCarriedReposable(out IReposable dropped)`

---

## 4. Unity Editor 설정 가이드

### 4.1 침대 프리팹 설정

1. 침대 프리팹에 `MovingPatientBedController`를 붙인다.
2. `_reposeAnchor`를 침대 위 적절한 위치 Transform으로 지정한다.
3. `_attachableItemVisualPairs`에 아이템 식별자와 자식 시각 오브젝트를 등록한다.
4. 부착 연출용 자식 오브젝트는 초기 비활성 권장.

### 4.2 환자 프리팹 설정

1. 환자 프리팹에 `PatientController`를 붙인다.
2. `_weight` 기본값 4를 유지하거나 시나리오에 맞게 조정한다.
3. `_attachableItemVisualPairs`를 침대와 동일 규칙으로 설정한다.

### 4.3 플레이어 프리팹 설정

1. `PlayerController.ReposableCarry` partial이 포함되어 컴파일되는지 확인한다.
2. `_reposableCarryAnchor`를 어깨/등 위치 Transform에 연결한다.
3. 앵커를 비우면 플레이어 루트 기준으로 운반된다.

---

## 5. 제한 사항 및 후속 작업 포인트

1. 아이템 `Use/Attack`의 대상 판정은 프로젝트 전체 타겟 해석 경로(레이캐스트 등)에 의존한다.  
2. 현재 침대 이동은 대표 인터랙터 추종 방식이며, 물리/네트워크 보정은 향후 개선 가능하다.  
3. `IInteract` 배열(`Interacts`)를 통해 다른 상호작용 UI와의 결합이 가능하지만, UX 정책은 별도 설계가 필요하다.

---

## 6. 빠른 점검 체크리스트

- 컴포넌트 경로가 `Prefabs/Entity`인지 확인했는가?
- 네임스페이스가 `TriageTrainer.Entity`인지 확인했는가?
- 환자/침대에 부착 식별자-오브젝트 매핑이 등록되어 있는가?
- 플레이어 carry anchor가 설정되어 있는가?
- 동일 메시지 쿨다운(3초)이 의도대로 동작하는가?

