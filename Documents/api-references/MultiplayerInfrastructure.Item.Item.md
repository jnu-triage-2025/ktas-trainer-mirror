# MultiplayerInfrastructure.Item.Item (Current)

## 0. 문서 목적

이 문서는 `Item` 구현의 현재 동작을 설명한다.  
특히 최근 변경으로 중요해진 `Awake` 초기화/Registry 등록 흐름을 중심으로, 학부생 수준에서도 이해할 수 있도록 단계적으로 정리한다.

---

## 1. 핵심 구성

`Item`은 다음 요소의 결합으로 동작한다.

1) `NetworkBehaviour`  
2) `IInteractable` 구현  
3) `ItemBaseModelSO` 기반 데이터 생성(선택)  
4) `ItemData` 기반 fallback  
5) `Registry` 등록

즉, “월드 오브젝트 + 상호작용 + 데이터 초기화 + 시스템 등록”의 역할을 동시에 갖는다.

---

## 2. Awake 초기화 메커니즘

대상 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Item/Item.Lifecycle.cs`

### 2.1 왜 복잡한가?

실제 프리팹 상태는 항상 완전하지 않다.  
어떤 아이템은 `ItemBaseModelSO`가 있고 `ItemData`가 비어 있고, 어떤 아이템은 그 반대일 수 있다.

그래서 Awake는 “가장 신뢰할 수 있는 데이터부터” 순서대로 판단한다.

### 2.2 현재 순서

1. 베이스 모델 유효성 검사
   - `_itemBaseModel != null`
   - `_itemBaseModel.identifier`가 비어 있지 않음

2. `_itemData`가 null이거나 유효하지 않고, 베이스 모델이 유효하면
   - `new ItemData(_itemBaseModel, _itemIcon)`으로 데이터 생성

3. `_itemData`가 이미 유효하면 (예: ItemDataInitializerBase로 주입한 서브클래스)
   - 기존 `_itemData` 유지, `_itemIcon`이 있고 아이콘이 없으면 적용

4. `_itemIdentifier` 보정
   - 비어 있으면 `_itemData.identifier`를 사용

5. 아이콘 후처리
   - `ItemData`가 유효하고 아이콘이 비어 있으면
   - `DefaultsResource.ItemTexturesPath` 및 보조 경로에서 `Resources.Load<Sprite>` 시도

6. 등록 가능 여부 판단
   - ItemData가 무효면 경고 로그 후 종료
   - 식별자 비어 있으면 경고 로그 후 종료

7. Registry 등록
   - `Registry.Registry.Register(RegistryType.Item, _itemIdentifier, this)`

### 2.3 중요한 의미

이 구조는 다음 장점을 만든다.

- 데이터가 부분적으로 비어 있어도 최대한 복구
- 이미 유효한 ItemData 서브클래스(커스텀 로직 포함)가 있으면 덮어쓰지 않음
- 최소 조건 미충족이면 조용히 망가지지 않고 경고를 남김
- 정상 케이스는 Registry에서 조회 가능해짐

---

## 3. 상호작용 동작

### 3.1 Interact

`Interact(Transform interactor)`는 플레이어가 월드 아이템을 집는 진입점이다.

기본 흐름:

1. interactor null 검사  
2. interactor에서 `NetworkObject` 탐색  
3. `ServerHandlePickup(...)` 호출

### 3.2 ServerHandlePickup

서버에서 실제 인벤토리 반영을 수행한다.

1. `PlayerController` 확인  
2. `ItemData` 유효성 확인  
3. `TryAddItemToInventory(...)` 호출  
4. 성공 시 월드 아이템 Despawn

클라이언트는 “요청”, 서버는 “검증/반영” 역할을 분리하는 전형적인 네트워크 패턴이다.

---

## 4. 사용 가이드 (실무 절차)

### 4.1 프리팹 준비

1. 아이템 프리팹에 `Item` 컴포넌트 추가
2. `_itemBaseModel` 또는 `_itemData` 중 최소 1개 유효하게 설정
3. `_itemIdentifier`는 가능하면 명시적으로 입력(비우면 보정되지만 명시가 안전)

### 4.2 추천 패턴

- 고유 로직 없는 아이템: `_itemBaseModel` 설정, `_itemData` 비워둠 (Lifecycle에서 자동 생성)
- 고유 로직이 있는 아이템: `ItemDataInitializerBase` 서브클래스를 프리팹에 추가하여 서브클래스 `ItemData` 주입

> **상세 구현 절차:** [item-authoring.md](../item-authoring.md)

### 4.3 아이콘 규칙

아이콘을 코드에서 직접 넣지 않았다면, `Resources`에서 로드될 수 있도록 경로/파일명을 맞춰야 한다.  
`ItemTextureIdentifier`가 곧 로드 키가 되므로 식별자 통일이 중요하다.

---

## 5. 진단 체크리스트

다음 순서로 확인하면 대부분의 문제를 빠르게 해결할 수 있다.

1. `_itemBaseModel.identifier`가 비어 있지 않은가?
2. `_itemData.IsValid()`가 true인가?
3. `_itemIdentifier`가 최종적으로 비어 있지 않은가?
4. 아이콘 경로가 `Resources` 로드 규칙과 맞는가?
5. Registry에서 `RegistryType.Item` + 식별자로 조회되는가?

---

## 6. 자주 하는 실수

1) 베이스 모델도 없고 ItemData도 비어 있는 프리팹을 배치  
2) identifier를 비워 둔 채로 “왜 등록이 안 되지?”라고 디버깅  
3) 아이콘 파일명과 ItemTextureIdentifier 불일치

이 세 가지가 가장 흔한 실패 지점이다.

---

## 7. 요약

`Item`은 단순히 “주울 수 있는 오브젝트”가 아니라,  
Awake에서 데이터 정합성을 확보하고 Registry에 자신을 등록하는 **초기화 엔트리 포인트**다.

최근 변경 이후의 핵심 한 줄 요약:

- "유효한 ItemData가 있으면 유지, 없으면 베이스 모델에서 생성, 조건 미충족 시 경고 후 등록 중단, 조건 충족 시 Registry 등록"

