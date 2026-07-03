# Feature Proposal - Static Placed Item 후속 과제

> **전제**: `StaticPlacedItem` 기본 구현 완료
> (`Agents/Proposals/done/2026-07-03-static-placed-item/`)

이 제안서는 기본 구현 완료 후 검증되지 않았거나, 의도적으로 이후로 미룬 항목을 정리한다.

---

## 1. 런타임 교차 검증 (인간 작업자 필수)

기본 구현의 수용 기준을 호스트/클라이언트 분리 환경에서 직접 검증해야 한다.

### 검증 항목

- [ ] **위치 고정**: OverworldScene 실행 시 아이템이 제자리에 고정되고, 물리로 흩어지지 않는다.
- [ ] **상호작용 힌트**: 플레이어가 StaticPlacedItem 에 다가가면 `"{DisplayText} 획득"` 힌트가 뜬다.
- [ ] **VanishedGlobalOnPickup**: 서버에 접속한 플레이어 A 가 획득 시 Remains 가 0이 되면, 플레이어 B 에게도 사라진다. 새로 접속한 플레이어 C 에게도 처음부터 사라진 상태로 보인다.
- [ ] **VanishedLocalOnPickup**: 플레이어 A 가 획득해도 플레이어 B 에게는 여전히 존재한다. A 가 재접속하면 다시 획득 가능 상태가 된다.
- [ ] **AlwaysExists**: 여러 번 획득해도 사라지지 않는다.
- [ ] **인벤토리 만석 실패**: 인벤토리가 가득 찬 상태에서 획득 시도 → 아이템 지급 안 됨, Remains 복원됨 (복제/유실 없음).
- [ ] **연타 방지**: 빠르게 연속 상호작용해도 Remains 상한이 초과되지 않는다.
- [ ] **접속 종료 중 미확정 예약 복원**: 서버가 획득 확정(TargetRpc)을 보낸 직후 클라이언트가 접속 종료 시, 서버가 Remains 를 복원한다.
- [ ] **Deactivate behavior**: 사라진 아이템의 gameObject 가 SetActive(false) 처리되어 보이지 않지만, 신규 접속자 동기화는 여전히 작동한다.

---

## 2. StaticPlacedItemService.ClearAll() 호출 지점 연결

### 문제

`StaticPlacedItemService` 는 static 상태를 메모리에 보유한다. 현재 씬 리로드나 서버 재시작 시 이 상태가 초기화되지 않아, 재시작 후에도 이전 세션의 Remains 값이 남아 있을 수 있다.

### 해결 방향

게임 모드 초기화 지점(서버 시작/씬 로드 전 후)에서 `StaticPlacedItemService.ClearAll()`을 호출하도록 연결한다. 기존 코드에서 적절한 훅(예: GamemodeService, ScenarioController 초기화, 또는 씬 로드 이벤트)을 찾아 연결할 것.

---

## 3. ApplyRestored() 배선 — 런타임 복원 기능 (선택적)

### 문제

`StaticPlacedItem.ApplyRestored()` 가 현재 어디에서도 호출되지 않는다. 사라진 아이템을 런타임에 다시 나타나게 하는 경로가 없다.

### 해결 방향

필요하다면 다음 기능을 추가한다:
- `StaticPlacedItemService` 에 Remains 를 초기값으로 재설정하는 API 추가.
- `PlayerController.StaticPlacedItem.cs` 에 서버 권위 복원 RPC(`RpcRestoreStaticPlacedItem`) 추가.
- 이를 통해 씬 리셋, 시나리오 리플레이, 관리자 명령 등에서 특정 StaticPlacedItem 을 복원할 수 있게 한다.

구현 여부는 실제 사용 사례 발생 시 결정.

---

## 우선순위

| 항목 | 우선순위 | 담당 |
|---|---|---|
| 런타임 교차 검증 | 높음 | 인간 작업자 (Unity 에디터 플레이 모드) |
| ClearAll() 연결 | 중간 | 개발자 |
| ApplyRestored() 배선 | 낮음 | 사용 사례 발생 시 결정 |
