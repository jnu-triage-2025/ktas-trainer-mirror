# 2026-02-16 변경 노트: Interactable 구조 개편 + NPC 다중 인터랙트 + 아이콘 참조 확장

## 요약

이번 변경의 핵심은 **상호작용(Interact) 모델을 단일 액션에서 다중 액션으로 확장**한 것입니다.

기존에는 `IInteractable`이 사실상 하나의 `Interact(...)`만 가지는 구조로 쓰였지만,
이제는 `IInteractable`이 `IInteract[] Interacts`를 제공하고,
실제 실행 단위는 `IInteract`로 분리되어 동작합니다.

이 변화에 맞춰 NPC도 다음처럼 재설계되었습니다.

- 하나의 NPC가 여러 시나리오 시작 액션을 가질 수 있음
- 하나의 NPC가 여러 커스텀 액션(`IInteract`)을 가질 수 있음
- 아이콘은 직접 `Sprite`뿐 아니라 Registry 기반 아이콘 식별자/정의값도 사용할 수 있음

또한, 이전 버전 호환용으로 잠시 두었던 `Legacy` 마이그레이션 필드/로직은 요청에 따라 제거되었습니다.

---

## 1) Interactable 모델 개편

### 변경 전

- `IInteractable` 구현체가 `DisplayText`, `DisplayIcon`, `DisplayColor`, `Interact(...)`를 직접 제공
- 구조상 “객체 1개 = 상호작용 1개”로 수렴되기 쉬움

### 변경 후

- `IInteractable`은 `IInteract[] Interacts`를 제공
- 실제 화면 표시/실행은 `IInteract` 단위로 처리

즉, **하나의 월드 오브젝트(`IInteractable`)가 여러 선택지(`IInteract`)를 노출**할 수 있게 되었습니다.

### 왜 중요한가?

게임 디자인 관점에서 NPC/오브젝트는 점점 복합 기능을 갖게 됩니다.
예: “대화하기 / 퀘스트 받기 / 훈련 시작하기”.

기존 구조는 이를 억지로 분기문으로 처리해야 했지만,
새 구조에서는 액션을 리스트로 명시하면 되므로
- UI 표시가 명확해지고
- 테스트가 쉬워지며
- 기능 추가 시 기존 로직 수정량이 줄어듭니다.

---

## 2) 시스템 전파(핵심 런타임 경로)

다음 계층들이 `IInteract` 기반으로 동작하도록 전파되었습니다.

1. 감지 계층: 근처 `IInteractable` 목록 수집
2. 선택 계층: 각 `IInteractable.Interacts`를 평탄화(Flatten)하여 선택 목록 구성
3. UI 계층: `IInteract` 리스트를 렌더링
4. 실행 계층: 선택된 `IInteract.Interact(interactor)` 호출

핵심 아이디어는
**“감지는 오브젝트 단위(IInteractable), 선택/실행은 액션 단위(IInteract)”** 입니다.

---

## 3) NPC 구조 변경 (중요)

### 기존 NPC

- `IsScenarioEntry` 플래그로 동작 여부를 제어
- 단일 `_scenarioIdentifier`, `_scenarioStartNodeIdentifier` 필드
- 사실상 “NPC당 시나리오 진입 1개” 모델

### 현재 NPC

- `List<NPCScenarioInteractDefinition> _scenarioInteracts`
- `List<MonoBehaviour> _customInteractSources` (여러 `IInteract` 소스)
- `Interacts` 프로퍼티에서 시나리오 액션 + 커스텀 액션을 합쳐 반환

이렇게 바뀌면서, NPC는 이제 **복수 액션 제공자**가 되었습니다.

### 동작 메커니즘

- `RebuildInteracts()`가 내부 캐시 `_resolvedInteracts`를 재구성
- 시나리오 정의는 내부 `ScenarioNpcInteract` 래퍼로 감싸 `IInteract`화
- 커스텀 소스는 `source is IInteract`일 때만 등록
- 선택 실행 시 해당 액션만 독립적으로 동작

---

## 4) NPC Base Model 연동 방식

`NPCBaseModelSO`는 이제 `scenarioInteracts` 리스트를 기준으로 NPC에 설정을 전달합니다.

NPC 인스턴스화(또는 적용) 시:

- `ApplyBaseModel()`에서 base model의 `scenarioInteracts`를 복제(clone)
- 복제 후 NPC 인스턴스가 자체 리스트를 가지므로, 런타임 변형이 모델 원본에 직접 영향 주지 않음

이 “복제 후 사용” 패턴은 ScriptableObject를 런타임에서 안전하게 쓰기 위한 정석 패턴입니다.

---

## 5) 아이콘 참조 확장: Sprite + Registry + Definition

`Commons/IconSpriteReference`가 추가되어,
NPC 시나리오 인터랙트 아이콘을 다음 3가지 경로 중 하나로 해석할 수 있습니다.

1. 직접 `Sprite` 참조
2. Registry 식별자 문자열 (`IconRegistryIdentifier`)로 조회
3. 사전 정의 enum (`IconSpriteDefinitions`) 기반 매핑

해석 순서는 일반적으로 아래와 같습니다.

1) `_sprite`가 있으면 사용
2) `_iconDefinition != None`이면 definition->registry id 매핑 후 조회
3) 그 외 `_iconRegistryIdentifier`로 조회

### 중요 버그 수정

`IconSpriteReference`에 `IconSpriteDefinitions` 필드가 추가된 뒤,
`Clone()`이 해당 필드를 복사하지 않아 NPC 인스턴스화 시 값 유실 문제가 있었습니다.

현재는 `Clone()`에서 `_iconDefinition`도 복사하도록 수정되어
base model -> npc 복제 경로에서 아이콘 정의값이 유지됩니다.

---

## 6) Legacy 제거

요청에 따라, 과거 단일 시나리오 구조를 새 리스트 구조로 자동 변환하던
`legacy` 필드와 마이그레이션 로직은 제거되었습니다.

의미:

- 코드 복잡도 감소
- 의도치 않은 자동 변환 제거
- 데이터는 현재 구조(`scenarioInteracts`)를 기준으로 관리

주의:

- 오래된 에셋이 있다면 자동 변환이 더 이상 일어나지 않으므로,
  에디터에서 `scenarioInteracts`를 직접 채워야 합니다.

---

## 7) TriageTrainer 쪽 인터페이스 정합성 수정

`MovingPatientBedController`, `PatientController`도
`IInteractable.Interacts` 계약을 만족하도록 정리되었습니다.

- 각 클래스가 `IInteract`도 구현
- `Interacts => new IInteract[] { this }`

즉, 기본적으로는 단일 액션 객체지만,
현재 인터랙트 시스템의 새로운 계약에는 정확히 맞게 동작합니다.

---

## 8) 정리: 이번 변경이 가져온 효과

1. 시스템 유연성 증가
   - 오브젝트 1개가 여러 상호작용 선택지를 자연스럽게 제공 가능

2. NPC 확장성 증가
   - 시나리오 엔트리 플래그/단일 필드 모델에서 리스트 기반 모델로 전환

3. UI/실행 분리 명확화
   - 감지(IInteractable)와 실행(IInteract) 책임 분리

4. 아이콘 관리 개선
   - 직접 Sprite, Registry ID, Definition enum 모두 지원

5. 유지보수 단순화
   - Legacy 호환 코드를 제거해 현재 모델 중심으로 코드 경량화

---

## 관련 코드 위치(참고)

- 인터랙트 정의
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/IInteractable.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/InteractableEntity/IInteract.cs`

- NPC
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/Npc.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/NPCBaseModelSO.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/NPCScenarioInteractDefinition.cs`

- 공용 아이콘 참조
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Commons/IconSpriteReference.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Commons/IconSpriteDefinitions.cs`

- Registry 아이콘 로딩
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.Register.cs`

- UI 표시/선택
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/InteractableGameObjectHintUIController.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/InteractableObjectHintList.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/InteractableObjectHintListElement.cs`
