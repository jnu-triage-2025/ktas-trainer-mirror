# API 레퍼런스: `MultiplayerInfrastructure.ItemSystem.StaticPlacedItem`

> 네임스페이스: `MultiplayerInfrastructure.ItemSystem`
> 주요 파일:
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/StaticPlacedItem.cs`
> - `.../StaticPlacedItemVanishMode.cs`, `.../StaticPlacedItemVanishBehavior.cs`
> - `.../StaticPlacedItemPickupReward.cs`, `.../StaticPlacedItemState.cs`
> - `.../StaticPlacedItemService.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.StaticPlacedItem.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Editor/ItemSystem/SceneItemPlacementConverter.cs`

## 0. 개요

`StaticPlacedItem` 은 **에디터에 사전 배치되어 맵의 일부처럼 제자리에 고정되는 정적 아이템**이다.
`ItemObject`(월드 드롭 아이템)와 달리 Rigidbody 물리로 스폰/산란하지 않으며, 네트워크 스폰 오브젝트가
아니라 각 프로세스에 로컬로 존재한다. 엔티티가 아닌 것에 가깝지만(편의상 Registry에는
`EntityType.StaticPlacedItem` 로 등록) `Interactable` 하다. 상호작용(획득)하면 아이템을 지급하고,
설정에 따라 사라지게 할 수 있다.

배치가 에디터 표현 그대로 유지되므로, 기존 `SceneItemPlacement` → `ItemObject` 경로에서 발생하던
**스폰 시 위치 산란/충돌 문제가 없다**.

## 1. 컴포넌트: `StaticPlacedItem`

`Interactable`(→ `MonoBehaviour`, `IInteractable`, `IInteract`)을 상속하고 `IInteractorConditional` 을 구현한다.
`[RequireComponent(typeof(Collider))]`.

### 직렬화 필드

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `_entityIdentifier` | `string` | 자동 생성(`static-item:...`) | 서버/모든 클라이언트에서 동일해야 하는 전역 식별자 |
| `_pickupReward` | `StaticPlacedItemPickupReward` | item="" / amount=1 / decreaseRemains=1 | 1회 획득 시 지급/상태변화 |
| `_initialState` | `StaticPlacedItemState` | remains=1 | Remains 초기값 |
| `_vanishMode` | `StaticPlacedItemVanishMode` | `VanishedGlobalOnPickup` | 획득 처리 방식 |
| `_vanishBehavior` | `StaticPlacedItemVanishBehavior` | `Invisible` | 사라짐 시 표현 |
| `_autoLoadModel` | `bool` | `false` | true면 `Resources/Models/Items/{ItemIdentifier}` 자동 로드 |

### 라이프사이클

- `Awake`: entityIdentifier 보정 → (옵션) 모델 로드 → **`EnsureInteractionCollider`** → Registry 등록.
  - `EnsureInteractionCollider`: Collider가 없거나 BoxCollider 크기가 사실상 0이면 자식 Renderer bounds 로 BoxCollider 크기를 보정한다(에디터 변환 도구가 이미 크기를 설정했으면 건드리지 않음).
- `OnEnable`: Registry 재등록.
- `OnDisable`: **단, `Deactivate` behavior 로 인한 Vanished 상태면 Registry 등록을 유지**한다(신규 접속자 동기화/서버 조회가 대상을 계속 찾을 수 있도록). 그 외에는 등록 해제.
- `OnDestroy`: 등록 해제.

### 상호작용

- `Interact(Transform)`: Vanished 상태면 무시. 아니면 `PlayerController.TryPickupStaticPlacedItem(entityIdentifier)` 위임.
- `CanInteract(Transform)`: Vanished 상태에서는 항상 `false`(어떤 behavior 든 상호작용 차단).
- `ApplyVanished()`: behavior 별 로컬 표현 적용(Invisible=렌더러 off / Deactivate=SetActive(false) / DisableInteraction=표현 유지, 상호작용만 차단).
- `ApplyRestored()`: **현재 프로토콜은 사라짐이 단방향이므로 호출되지 않는다.** 향후 런타임 복원(관리자 리셋/Remains 재충전/리스폰) 기능용 예약 API.

## 2. Enum

### `StaticPlacedItemVanishMode`

| 값 | 의미 |
|---|---|
| `VanishedGlobalOnPickup`(=0, 기본) | 서버 전역에서 단일 Remains. 소진되면 모든 플레이어에게서 사라짐 |
| `VanishedLocalOnPickup` | 플레이어(UserIdentifier)별 Remains. 소진된 개별 플레이어에게만 사라짐 |
| `AlwaysExists` | 상태 처리 없음. 항상 존재/항상 획득 가능 |

### `StaticPlacedItemVanishBehavior`

| 값 | 의미 |
|---|---|
| `Invisible`(=0, 기본) | 렌더러 off. 상호작용도 함께 차단 |
| `Deactivate` | `gameObject.SetActive(false)`. 실제 파괴가 아니라 Local 모드/재접속 동기화와 호환 |
| `DisableInteraction` | 보이되 상호작용만 차단 |

> **주의**: 어떤 behavior 든 Vanished 상태에서는 상호작용이 항상 차단된다. `Deactivate` 는 파괴(`Destroy`)가 아니다 — Local 모드 개별 숨김·신규 접속자 동기화·복원 가능성을 위해 의도적으로 비활성화만 수행한다.

## 3. Struct

### `StaticPlacedItemPickupReward` (사양의 OnPickupTry)

| 프로퍼티 | 타입 | 기본값 | 비고 |
|---|---|---|---|
| `ItemIdentifier` | `string` | `""` | Registry 등록된 Item Identifier |
| `Amount` | `int` | `1` | 최소 1 강제 |
| `DecreaseRemains` | `int` | `1` | 음수 방지, `0`이면 감소하지 않음(무한) |

### `StaticPlacedItemState`

| 프로퍼티 | 타입 | 기본값 | 비고 |
|---|---|---|---|
| `Remains` | `int` | `1` | 초기 획득 가능 횟수. 런타임 진실 원천은 서버 서비스 |

## 4. 서버 상태 서비스: `StaticPlacedItemService`

`PlayerTagService` 와 동일한 static 서비스 패턴. **모든 변이 메서드는 서버에서만 유효**하다(비서버 호출 시 경고 후 무시).

- 전역: `EnsureGlobalRemains` / `GetGlobalRemains` / `DecreaseGlobalRemains` / `RestoreGlobalRemains`
- 유저별: `EnsureLocalRemains` / `GetLocalRemains` / `DecreaseLocalRemains` / `RestoreLocalRemains`
- 정리: `ClearUser`(재접속 초기화) / `ClearEntity` / `ClearAll`(씬 리로드/서버 재시작 시 호출 권장)

키: 전역 = `entityIdentifier`, 유저별 = `(entityIdentifier, userIdentifier)`.

## 5. 픽업 프로토콜 (`PlayerController.StaticPlacedItem.cs`)

`StaticPlacedItem` 은 비네트워크이므로 RPC 를 가질 수 없다. `NetworkBehaviour` 인 `PlayerController` 를 통해
서버 권위 **예약(pending-claim)** 프로토콜로 처리하며, 월드아이템(`ItemObject`) 픽업과 동일한 구조다.

```
(Owner) TryPickupStaticPlacedItem(entityId)
   └ 로컬 in-flight 쿨다운(2s)으로 연타/중복요청 방지
   → [ServerRpc] CmdRequestPickupStaticPlacedItem
(Server) ServerApprovePickupStaticPlacedItem
   ├ 소유자/거리 검증
   ├ 이미 미확정 예약이 있으면 거부(단일 사용 보장)
   ├ ServerTryReserveStaticPickup: Remains 검증 + 선점 감소(예약 생성)
   └ [TargetRpc] TargetConfirmPickupStaticPlacedItem(요청자에게만)
(Claimant) 전량 수용 가능(all-or-nothing)하면 인벤토리 추가
   ├ 성공 → [ServerRpc] CmdAcknowledgePickupStaticPlacedItemSuccess
   └ 실패 → [ServerRpc] CmdReportPickupStaticPlacedItemFailure
(Server) 성공: ServerConfirmStaticPickupSuccess
   ├ 예약 존재 + claimant 일치 검증 후 단일 소비(무단/재전송 ack 차단)
   └ 남은 Remains ≤ 0 이면 사라짐 브로드캐스트
        · Global → [ObserversRpc] RpcVanishStaticPlacedItemGlobal (모든 관전자)
        · Local  → [TargetRpc]   TargetVanishStaticPlacedItemLocal (요청자)
(Server) 실패/접속종료: ServerRestoreStaticPickup / RestorePendingStaticPickupsForClaimant
   └ 선점 감소한 Remains 복원 + 예약 제거
```

### 보안/정합성 설계 근거

- **선점 감소 + 단일 사용 예약**으로 다음을 차단한다:
  - 무단 ack: ack 는 서버가 만든 예약이 있고 claimant 가 일치할 때만 유효.
  - double-grant 경합: 승인 시점에 Remains 를 이미 감소하고, 미확정 예약이 있으면 추가 승인을 거부.
  - ack 재전송 과다 감소: 예약을 단일 소비하므로 중복 ack 는 무시.
- **all-or-nothing**: 전량 수용 가능할 때만 인벤토리에 추가(부분 추가로 인한 복제/유실 방지). 실패 시 서버가 Remains 복원.
- **신규 접속자 동기화**: `OnSpawnServer` → `SyncStaticPlacedItemsToConnection` 이 이미 전역 소진된 Global 모드 아이템을 그 연결에만 사라진 상태로 맞춘다. Local 모드는 재접속 초기화 정책이라 동기화하지 않는다.
- **접속 종료 처리**: `OnStopServer` 에서 미확정 예약 복원(`RestorePendingStaticPickupsForClaimant`) → Local 상태 purge(`ClearUser`) 순으로 수행한다.

## 6. Registry / EntityType

`EntityType` 에 `StaticPlacedItem` 값이 append-only 로 추가되었다(기존 값 순서 불변). `StaticPlacedItem` 은
`Registry.RegisterEntity(..., EntityType.StaticPlacedItem, ...)` 로 각 프로세스에 로컬 등록되며 `IsNetworked=false` 다.

## 7. 씬 마이그레이션: `SceneItemPlacementConverter` (Editor)

메뉴 `Tools/Multiplayer Infrastructure/Static Placed Item/`:
- `Convert Selected SceneItemPlacements`: 선택 오브젝트(및 자식)의 `SceneItemPlacement` 변환.
- `Convert All In Active Scene`: 활성 씬 전체 변환(확인 대화상자 표시).

변환 매핑:
- `_entityIdentifier` 유지, `itemIdentifier` → `PickupReward.ItemIdentifier`, `stackCount` → `PickupReward.Amount`.
- VanishMode=`VanishedGlobalOnPickup`, VanishBehavior=`Invisible`, Remains=1, DecreaseRemains=1(기본값).
- Collider 가 없으면 Renderer bounds 기반 `BoxCollider` 자동 추가.
- 원본 `SceneItemPlacement` 제거. Undo 지원. 완료 후 씬 저장 필요.

## 8. 알려진 제약 / 후속

- `ApplyRestored()` 는 현재 미사용(단방향 사라짐). 런타임 복원 기능은 후속 과제.
- Local 모드의 영구 지속(재접속 후에도 유지)은 서비스 API 로 가능하나, 현재 정책상 재접속 시 초기화한다.
- `StaticPlacedItemService` 의 static 상태는 씬 리로드/서버 재시작 시 `ClearAll()` 호출 지점을 게임모드 초기화에 연결할 것을 권장한다.
