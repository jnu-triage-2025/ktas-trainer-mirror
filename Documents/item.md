# Item

- Item은 월드 오브젝트로서 FishNet `NetworkBehaviour` 기반으로 동작해야 한다. (스폰/디스폰, 서버 권한 흐름 포함)
- 아이템 획득/상호작용은 서버에서 처리해야 한다. 클라에서는 시각/입력만 처리하고, 서버 RPC 경로로 인벤토리를 갱신한다.
- 아이템 데이터(`ItemData`)는 인벤토리 쪽에서 사용하는 순수 데이터로 유지한다. 네트워크 동기화나 상태 변경은 서버 주도 모델을 따른다.
- 아이템 컴포넌트가 `IInteractable`을 구현할 때, 상호작용 대상 검증과 null 체크를 서버 RPC 앞에서 보장한다.
- 아이콘/비주얼 리소스는 `ItemRegistry`에 의존하지 않도록, 인스턴스 생성 시 주입되는 방식으로 유지한다.

## Item 구현 개요

### 구성 요소
- `Item`은 월드 상의 컴포넌트이며 `NetworkBehaviour` + `IInteractable`을 구현한다.
- `ItemData`는 인벤토리/핫바 UI에서 사용하는 순수 데이터 모델이다.
- 월드 `Item`은 생성 시점에 `ItemData`를 채워 넣는다.

### PlayerController 동작 메커니즘
- 핫바 선택 변경 또는 인벤토리 슬롯 변경 이벤트가 발생하면 `HandlingItem`이 갱신된다.
- `HandlingItem`은 `ItemData`를 복제해서 참조하며, 공격/사용은 `HandlingItem.OnAttack()`/`OnUse()`로 위임된다.
- 실제 공격/사용 시점은 `TriggerAttack`/`TriggerUseItem` → `Update_Item()` → `Attack()`/`UseItem()` 흐름을 따른다.
- `Attack()`은 기본 공격을 호출한 뒤, UI(아이콘 애니메이션 등)를 통해 피드백을 제공한다.
- `UseItem()`은 아이템 사용 피드백을 표시하고, 아이템별 로직은 `OnUse()`에서 처리한다.

### 라이프사이클
- 월드 `Item`은 `Awake()`에서 `_itemBaseModel` 또는 `_itemData`를 검사한다.
- 데이터가 없다면 `ItemData`를 생성하고 아이콘(스프라이트)을 주입한다.
- `Interact()`는 서버 RPC를 통해 인벤토리 추가를 시도하고 성공 시 디스폰한다.

## 아이템 구현 가이드 (상속 확장)

### ItemData 상속 (권장)
아이템 고유 동작은 `ItemData`를 상속하여 구현한다.

- `OnAttack(PlayerController player, Entity target)`
	- 공격 시 필요한 로직 (상태 변화, 버프 적용 등)
- `OnUse(PlayerController player, Entity target)`
	- 사용 시 필요한 로직 (회복, 상호작용 등)

상속 클래스는 반드시 `IsValid()` 조건을 만족하도록 초기화되어야 한다.

### Item 컴포넌트 확장 (제한적)
월드 상에서 필요한 물리/시각 효과가 있다면 `Item`을 partial로 확장한다.

- 입력 처리나 UI 로직은 `PlayerController`에서 처리한다.
- 네트워크 권한과 디스폰 타이밍은 `Item.Interactable` 로직을 따른다.

## 구현 체크리스트
- `ItemData` 생성 시 아이콘이 주입되는지 확인한다.
- `OnAttack`/`OnUse`에서 서버와 클라 역할 분리를 유지한다.
- 인벤토리 변경 후 UI 동기화가 필요하면 슬롯 갱신 이벤트를 사용한다.
