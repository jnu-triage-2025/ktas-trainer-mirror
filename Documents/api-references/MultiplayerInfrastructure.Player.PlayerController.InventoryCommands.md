# MultiplayerInfrastructure.Player.PlayerController.InventoryCommands

## 0. 문서 목적

이 문서는 커맨드(`/give`, `/clean`) 지원을 위해 확장된 `PlayerController.Inventory` API를 설명합니다.
핵심은 서버/시스템이 사용할 수 있는 인벤토리 조작 메서드와 overflow 드롭 처리입니다.

대상 파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Inventory.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Item/ItemSpawnUtility.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Item/Item.RuntimeData.cs`

---

## 1. 추가된 인벤토리 API

### 1) `TryAddItemToInventory(ItemData item, out ItemData leftover)`

- 스택 가능한 슬롯에 먼저 병합
- 이후 빈 슬롯에 신규 배치
- 전체 수량이 다 들어가지 못하면 `leftover` 반환

### 2) `ClearInventory()`

- 전체 슬롯 초기화
- 제거된 총 개수 반환

### 3) `RemoveItemFromInventory(string itemIdentifier, int count)`

- 지정 수량만큼 제거
- 실제 제거량 반환 (`min(보유, count)`)

### 4) `RemoveAllOfItemFromInventory(string itemIdentifier)`

- 해당 식별자 아이템 전량 제거
- 제거량 반환

### 5) `CountItemInInventory(string itemIdentifier)`

- 현재 보유량 조회

### 6) `TryDropItemInFront(ItemData itemData)`

- 플레이어 전방 위치에 아이템 월드 드롭 시도
- 내부적으로 `ItemSpawnUtility` 사용

---

## 2. 월드 드롭 유틸

### ItemSpawnUtility.TrySpawnDroppedItem(...)

동작:
1. 아이템 데이터 유효성 검사
2. `RegistryType.Item`에서 템플릿 `Item` 조회
3. 템플릿 GameObject 인스턴스 생성
4. `ApplyRuntimeItemData`로 런타임 수량/데이터 반영
5. Rigidbody가 있으면 전방 임펄스 적용
6. NetworkObject가 있으면 서버에서 Spawn

실패 시 `error` 메시지 반환

---

## 3. 런타임 Item 데이터 반영

### Item.ApplyRuntimeItemData(ItemData data)

- `_itemData`, `_itemIdentifier` 갱신
- 아이콘 누락 시 `Resources`에서 보정 시도
- 식별자 유효하면 Registry 재등록

의미:
- 템플릿 인스턴스를 “실제 지급/드롭 수량” 상태로 안전하게 변환

---

## 4. 커맨드와의 연동

- `/give`는 `TryAddItemToInventory(..., out leftover)`를 사용
- `leftover`가 있으면 `TryDropItemInFront(...)` 호출
- `/clean`은 `ClearInventory`, `RemoveItemFromInventory`, `RemoveAllOfItemFromInventory`를 사용

---

## 5. 주의점

1. 드롭은 `RegistryType.Item` 템플릿 조회 가능해야 동작
2. 네트워크 드롭은 서버가 실행 중이어야 `Spawn` 가능
3. 인벤토리 변경 후 UI/핫바 동기화는 내부 `OnInventoryChangedAndReturn(...)`에서 처리

---

## 6. 요약

이번 확장으로 `PlayerController` 인벤토리는 단순 UI 상태를 넘어, 커맨드/시스템이 직접 사용할 수 있는 조작 API 계층을 갖추게 되었습니다.
특히 “추가 실패분 자동 드롭” 경로가 추가되어 `/give`의 실사용성이 개선되었습니다.
