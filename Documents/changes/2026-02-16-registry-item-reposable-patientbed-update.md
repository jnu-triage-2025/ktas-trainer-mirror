# 2026-02-16 변경 노트 (Registry 전환 + Item 초기화 + 이동식 침대/환자 상호작용)

## 1. 문서 작성 목적

이 문서는 최근 작업에서 발생한 핵심 변경사항을 **변경 배경(왜 바꿨는지)**, **동작 메커니즘(어떻게 동작하는지)**, **사용 시 주의점(어디서 실수하기 쉬운지)** 중심으로 정리한 기록이다.  
특히, 다음 세 가지 흐름이 연결된다.

1) Registry 사용 방식 정리  
2) Item의 Awake 초기화/등록 규칙 정리  
3) 이동식 환자 침대 / 환자 / IReposable 기반 상호작용 정리

또한, 작성 시점에 실제 코드와 문서 내용이 맞는지 재검증하여, 과거 문서의 무효화된 경로/네임스페이스를 교정했다.

---

## 2. 유효성 재검증 결과 (중요)

### 2.1 이전 문서의 무효화된 내용

기존 `Documents/api-references/entities/moving-patient-bed-and-patient.md`에는 아래처럼 현재 코드와 불일치하는 정보가 있었다.

- 경로가 `Prefabs/Entities/...`로 기록되어 있었음
- 네임스페이스가 `TriageTrainer.Prefabs.Entities...`로 기록되어 있었음
- Player carry 확장 파일 경로가 `TriageTrainer/Prefabs/Entities/Common`로 기록되어 있었음

### 2.2 현재 실제 코드 기준

작성 시점 기준 실제 유효한 경로/네임스페이스는 다음과 같다.

- Bed: `Assets/Modules/TriageTrainer/Prefabs/Entity/MovingPatientBed/MovingPatientBedController.cs`
- Patient: `Assets/Modules/TriageTrainer/Prefabs/Entity/Patient/PatientController.cs`
- Player carry 확장: `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.ReposableCarry.cs`
- Bed/Patient 네임스페이스: `TriageTrainer.Entity`

즉, 본 변경 노트와 API 문서는 위 기준으로 업데이트되었다.

---

## 3. Registry 관련 변경 흐름

### 3.1 핵심 아이디어

프로젝트 전반에서 타입/객체 참조를 찾을 때 `Registry`를 사용한다.  
최근 작업에서는 호출 방식이 정리되면서, 일부 코드에서 API 호출 불일치가 발생했다(예: 잘못된 멤버명 접근).

### 3.2 이번에 정리된 포인트

- `ScenarioEventIdentifierRegistry` 사용 시, 존재하지 않는 `Registry` 속성을 거치지 않고 직접 `TryGetHandler(...)` 호출하도록 정정
- `Registry` 호출 시에는 현재 정적 클래스 정의와 맞는 메서드(`Register`, `Get`, `TryGet`, `Unregister`, `TypeKey`)를 사용

### 3.3 학습 포인트

정적 레지스트리 패턴에서 가장 흔한 실수는 다음 둘이다.

1) 클래스 이름과 같은 “중간 속성”이 있다고 착각하는 경우  
2) 과거 버전 API 호출을 복사해 쓰는 경우

이 프로젝트에서는 실제 선언부를 먼저 확인하고, 그 시그니처에 맞춰 호출부를 맞추는 것이 필수다.

---

## 4. Item Awake 초기화/등록 변경

대상 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Item/Item.Lifecycle.cs`

### 4.1 변경 배경

`Item`은 런타임에 데이터가 불완전한 상태로 프리팹에 붙어 있을 수 있다.  
따라서 Awake에서 데이터 준비와 Registry 등록을 순차적으로 안전하게 수행해야 한다.

### 4.2 현재 Awake 로직(순서가 중요)

1) `ItemBaseModelSO` 유효성 검사  
2) 유효한 베이스 모델이 있으면 `ItemData`를 새로 생성  
3) 베이스 모델이 없으면 기존 `ItemData` 유효성으로 fallback  
4) 식별자(`_itemIdentifier`) 보정  
5) 아이콘 비어 있으면 `Resources` 경로에서 로드 시도  
6) 베이스 모델과 ItemData가 모두 무효면 경고 로그 후 등록 중단  
7) 식별자 비어 있으면 경고 로그 후 등록 중단  
8) 모든 조건 통과 시 `Registry`에 자기 자신 등록

### 4.3 “원래 구현 유지”가 의미하는 것

요구사항에 맞춰, 베이스 모델이 없는 경우에는 기존 `_itemData`를 버리지 않고 유효성 검사를 통해 그대로 사용할 수 있게 유지했다.  
즉, **베이스 모델 우선 + ItemData fallback** 구조이다.

### 4.4 주의점

- `_itemIdentifier`가 비어 있으면 등록되지 않는다.
- 베이스 모델이 있어도 `identifier`가 비어 있으면 유효하지 않다.
- 아이콘은 `DefaultsResource.ItemTexturesPath` 하위 `Resources` 로딩 규칙을 따른다.

---

## 5. IReposable + 이동식 환자 침대/환자 변경

### 5.1 추가 인터페이스

대상 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IReposable.cs`

- `int Weight { get; }`를 강제하는 인터페이스
- 침대에 “눕힐 수 있는 대상”의 최소 계약 역할

### 5.2 Player 운반 상태 확장

대상 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.ReposableCarry.cs`

- `IsCarryingReposable`
- `CarriedReposable`
- `TryPickUpReposable(...)`
- `TryDropCarriedReposable(...)`

의미: 플레이어가 IReposable 대상을 들고 있는지/내려놓는지를 시스템적으로 관리

### 5.3 MovingPatientBedController 요약

대상 파일: `Assets/Modules/TriageTrainer/Prefabs/Entity/MovingPatientBed/MovingPatientBedController.cs`

주요 메커니즘:

- `IInteractable`, `IInteract` 구현
- 침대 무게(`_weight`)와 침대 위 대상 무게(`ReposedTarget.Weight`)를 함께 고려하여 필요 인원 계산
- `Toggle`/`Hold` 이동 모드 지원
- 아이템 식별자 ↔ 시각 오브젝트 매핑으로 부착 연출 처리
- 동일 메시지 3초 쿨다운(중복 채팅 스팸 방지)
- 환자를 침대에 눕히기/들어올리기 메서드 제공

### 5.4 PatientController 요약

대상 파일: `Assets/Modules/TriageTrainer/Prefabs/Entity/Patient/PatientController.cs`

주요 메커니즘:

- `IInteractable`, `IInteract`, `IReposable` 구현
- 기본 무게 4
- 침대에 올라간 상태에서 `Interact` 시 플레이어가 환자를 들어올리도록 Bed API 호출
- 아이템 식별자 ↔ 시각 오브젝트 매핑으로 부착 연출 처리
- 동일 메시지 3초 쿨다운

---

## 6. 동시 변경(다른 작업)으로 확인된 사항

이번 기록 시점에 다음 변경이 함께 존재함을 확인했다.

- TriageTrainer의 ItemBaseModelSO 에셋 다수 신규 추가
  - 경로: `Assets/Modules/TriageTrainer/ScriptableObjects/ItemBaseModels/`
  - 예: `16g`, `18g`, `ambubag`, `vital_set`, `yankauer` 등

이 변경은 이번 문서 대상(Registry/Bed/Patient/Item Awake)과 직접 충돌하지는 않지만,  
`Item` Awake의 베이스 모델 기반 초기화 흐름이 실제 에셋 확장과 맞물려 의미가 커졌다는 점은 중요하다.

---

## 7. 정리

이번 변경의 핵심은 “데이터가 완전하지 않아도 안전하게 초기화하고, 조건이 충족되면 레지스트리에 등록하며, 상호작용 대상(환자/침대)은 인터페이스 계약으로 일관되게 묶는다”이다.

요약하면:

- Item: 베이스 모델 우선, ItemData fallback, 실패 시 경고 후 등록 중단
- Scenario: 이벤트 핸들러 레지스트리 호출 방식 정합성 수정
- Bed/Patient: IReposable + Player carry 상태를 통해 운반/거치 상호작용 체계화
- 문서: 현재 코드 경로/네임스페이스 기준으로 전면 정합성 갱신
