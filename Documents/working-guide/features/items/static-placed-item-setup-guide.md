# StaticPlacedItem 설정 및 씬 변환 가이드

이 가이드는 **에디터에 배치한 아이템이 런타임에 흩어지지 않고 제자리에 고정되도록** 하는
`StaticPlacedItem` 컴포넌트의 설정 방법과, 기존 `SceneItemPlacement` 배치를 일괄 변환하는 방법을
비전문가 작업자도 따라 할 수 있도록 단계별로 설명합니다.

---

## 0. 왜 필요한가

기존 `SceneItemPlacement` 로 배치한 아이템은 게임이 시작될 때 물리(Rigidbody)로 스폰되어 **바닥에 떨어지고
서로 밀려나며 흩어집니다**. 그래서 에디터에서 본 배치와 실제 인게임 배치가 달라집니다.

`StaticPlacedItem` 은 물리 스폰을 하지 않고 배치한 그 자리에 그대로 있습니다. 플레이어가 다가가
상호작용(E)하면 아이템을 획득하고, 설정에 따라 사라집니다.

---

## 1. 새 StaticPlacedItem 하나 만들기 (수동)

1. 아이템을 놓을 위치에 **아이템 3D 모델**을 배치합니다.
   - 보통 `Assets/Modules/TriageTrainer/Resources/Models/Items/{identifier}.prefab` 을 씬에 끌어다 놓습니다.
2. 그 GameObject에 **`Static Placed Item`** 컴포넌트를 추가합니다(Add Component → 검색 "Static Placed Item").
   - 컴포넌트를 추가하면 `Box Collider` 가 자동으로 함께 붙습니다(상호작용 감지에 필요).
3. 인스펙터에서 값을 설정합니다.

### 인스펙터 필드

| 섹션 | 필드 | 설명 |
|---|---|---|
| StaticPlacedItem | **Entity Identifier** | 비워두면 자동 생성됩니다. 서버·모든 클라이언트에서 동일해야 하는 고유 ID. |
| Pickup | **Item Identifier** | 지급할 아이템의 식별자(예: `gauze`). **반드시 Registry에 등록된 값**이어야 합니다. |
| Pickup | **Amount** | 한 번 획득 시 지급 수량(기본 1). |
| Pickup | **Decrease Remains** | 한 번 획득 시 남은 획득 횟수(Remains)를 얼마나 줄일지(기본 1). `0`이면 줄이지 않음(무한). |
| Pickup | **Remains** (Initial State) | 초기 획득 가능 횟수(기본 1). |
| Vanish Behaviour | **Vanish Mode** | 아래 표 참고. |
| Vanish Behaviour | **Vanish Behavior** | 아래 표 참고. |
| Model | **Auto Load Model** | 체크하면 `Resources/Models/Items/{Item Identifier}` 프리팹을 런타임에 자동 로드합니다. 이미 모델을 자식으로 두었다면 **체크 해제**로 둡니다. |

### Vanish Mode (획득 처리 방식)

| 값 | 동작 |
|---|---|
| **Vanished Global On Pickup** (기본) | 서버 전역에서 Remains 하나. 누군가 소진하면 **모든 플레이어**에게서 사라짐. |
| **Vanished Local On Pickup** | 플레이어마다 Remains 따로 계산. 소진한 **그 플레이어에게만** 사라짐. (재접속하면 초기화) |
| **Always Exists** | 사라지지 않고 항상 획득 가능. |

### Vanish Behavior (사라질 때 표현)

| 값 | 동작 |
|---|---|
| **Invisible** (기본) | 안 보이게 함(렌더러 끔). 상호작용도 함께 막힘. |
| **Deactivate** | 오브젝트를 비활성화. (파괴가 아니므로 안전) |
| **Disable Interaction** | 계속 보이지만 상호작용(획득)만 막힘. |

> 어떤 Behavior 든 사라진 상태에서는 **획득이 항상 막힙니다**(보이지 않는데 계속 집히는 혼란 방지).

---

## 2. 기존 SceneItemPlacement 를 일괄 변환하기 (권장)

OverworldScene 처럼 이미 `SceneItemPlacement` 로 배치된 아이템이 많다면, 하나씩 바꾸지 말고
**변환 도구**를 사용하세요.

### 변환 전 준비 (중요)

1. **씬을 커밋하거나 백업**합니다. (변환은 Undo 로 되돌릴 수 있지만, 안전을 위해 권장)
2. 변환할 씬(예: `OverworldScene`)을 엽니다.

### 변환 실행

- **씬 전체 변환**: 상단 메뉴
  `Tools ▸ Multiplayer Infrastructure ▸ Static Placed Item ▸ Convert All In Active Scene`
  → 확인 대화상자에서 **변환** 클릭.
- **일부만 변환**: 하이라키에서 변환할 오브젝트(들)를 선택한 뒤
  `Tools ▸ Multiplayer Infrastructure ▸ Static Placed Item ▸ Convert Selected SceneItemPlacements`

변환 도구가 각 오브젝트에 대해 자동으로 수행하는 일:
1. `Static Placed Item` 컴포넌트 추가 및 값 이관
   - Entity Identifier 유지, Item Identifier/Amount 이관
   - Vanish Mode = Global, Behavior = Invisible, Remains = 1, Decrease Remains = 1 (기본값)
2. Collider 가 없으면 모델 크기에 맞춘 **Box Collider** 자동 추가
3. 원본 `SceneItemPlacement` 컴포넌트 제거

### 변환 후 반드시 할 일

1. **씬을 저장**합니다(Ctrl/Cmd+S). 변환 결과는 저장해야 반영됩니다.
2. 콘솔 로그에서 `성공 N개, 건너뜀 M개` 와 상세 내역을 확인합니다.
   - "건너뜀"으로 표시된 항목은 Item Identifier 가 비었거나 이미 변환된 경우입니다.
3. **플레이 모드로 검증**합니다:
   - 아이템이 제자리에 그대로 보이는지(흩어지지 않는지)
   - 다가가면 상호작용 힌트("{이름} 획득")가 뜨는지
   - 획득 시 인벤토리에 들어오고, 설정대로 사라지는지

> 문제가 생기면 `Edit ▸ Undo` 로 되돌릴 수 있습니다(저장 전).

---

## 3. 자주 겪는 문제

| 증상 | 원인 / 해결 |
|---|---|
| 다가가도 상호작용 힌트가 안 뜸 | Collider 가 없음. 변환 도구는 자동 추가하지만, 수동 생성 시 Box Collider 를 확인하세요. |
| 획득이 안 됨 / 콘솔에 "not found" | Item Identifier 가 Registry 에 등록되지 않음. `TTRegistryPreloader.RegisterItems()` 확인. |
| 획득했는데 안 사라짐 | Vanish Mode 가 `AlwaysExists` 이거나 Decrease Remains 가 0(무한)인지 확인. |
| 다른 플레이어에게도 사라짐(원치 않음) | Vanish Mode 를 `VanishedLocalOnPickup` 으로 변경. |
| 모델이 안 보임 | 모델 프리팹에 FishNet `NetworkObject` 가 붙어 있으면 자동 비활성화됩니다. 제거하세요. |

---

## 4. 관련 문서

- API 레퍼런스(도메인 전문가용): [`../../../api-references/MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.md`](../../../api-references/MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.md)
- 아이템 정의/등록 방법: [`../../item-authoring.md`](../../item-authoring.md)
- 기능 제안서: `Agents/Proposals/scheduled/2026-07-03-static-placed-item/`
