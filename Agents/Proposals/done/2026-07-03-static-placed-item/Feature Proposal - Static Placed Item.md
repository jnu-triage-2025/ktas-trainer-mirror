# Feature Proposal - Static Placed Item

> **상태**: 구현 완료 (2026-07-03)
> **후속 과제**: `Agents/Proposals/scheduled/2026-07-03-static-placed-item-followup/`

## 개요

`StaticPlacedItem` 기능은 **에디터에 사전 배치된 아이템을 맵의 일부처럼 제자리에 고정**하기 위한 구현입니다. 이 기능은 물리 스폰 없이 배치 위치를 그대로 유지하고, 상호작용(획득) 시에만 아이템을 지급하는 서버 권위 프로토콜을 통해, 기존 grounded item(`ItemObject`)이 겪던 "스폰 시 위치 산란" 문제를 해결합니다. 병원/재난 맵 등에 다수의 아이템을 정확한 위치에 배치해야 하는 상황에서 활용하는 것이 의도되었습니다.

- **요약**: 네트워크 스폰 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이며, 엔티티는 아니지만 Interactable 하다. 상호작용 시 아이템을 획득하고 설정에 따라 사라진다.
- **의도/목표**: 에디터에서 본 배치 = 런타임 배치. 위치 충돌/산란 제거. 획득 가능 횟수(Remains)를 전역 또는 플레이어별로 관리.
- **주요 맥락**: 기존 `SceneItemPlacement`는 `Start()`에서 자신을 파괴하고 `ItemObject`(Rigidbody 보유)를 스폰하므로 물리로 흩어진다. `StaticPlacedItem`은 이 경로를 타지 않는다.
- **기술적 제약**: `StaticPlacedItem`은 `MonoBehaviour`(비네트워크)이므로 자체 RPC 를 가질 수 없다. 픽업/사라짐 동기화는 `NetworkBehaviour` 인 `PlayerController` 를 통해 서버 권위로 처리한다. 상태 진실 원천은 서버의 `StaticPlacedItemService`.

## 해결하려는 문제 상황

나는 **콘텐츠 배치 작업자**로서, 유니티 에디터에서 배치한 아이템이 **런타임에도 그 위치 그대로** 존재하기를 원한다. 왜냐하면 기존 `SceneItemPlacement` → `ItemObject` 경로는 스폰 시 Rigidbody 물리와 `throwForce` 로 인해 아이템이 흩어지고 서로 위치가 충돌하여, 에디터에서 의도한 배치와 실제 인게임 배치가 달라지기 때문이다.

## 사용자 경험 목표

- 에디터에서 배치한 위치·회전이 인게임에서 100% 동일하게 보인다.
- 플레이어가 아이템에 다가가 상호작용(E)하면 인벤토리에 획득되고, 설정한 규칙(전역/플레이어별/무한)에 따라 사라진다.
- 여러 아이템을 촘촘히 배치해도 스폰 산란/충돌이 없다.

## 제안

### 신규 타입 (MultiplayerInfrastructure.ItemSystem)

| 타입 | 역할 |
|---|---|
| `StaticPlacedItem : Interactable, IInteractorConditional` | 에디터 배치 컴포넌트. Registry에 `EntityType.StaticPlacedItem` 로 등록. Collider 필수(상호작용 감지). |
| `StaticPlacedItemVanishMode` (enum) | `VanishedGlobalOnPickup`(기본)/`VanishedLocalOnPickup`/`AlwaysExists` |
| `StaticPlacedItemVanishBehavior` (enum) | `Invisible`(기본)/`Deactivate`/`DisableInteraction` |
| `StaticPlacedItemPickupReward` (struct) | `ItemIdentifier`, `Amount`(=1), `DecreaseRemains`(=1) |
| `StaticPlacedItemState` (struct) | `Remains`(=1) |
| `StaticPlacedItemService` (static) | 서버 권위 Remains 상태(전역/유저별) 관리 + 예약 복원 API |

### 픽업 프로토콜 (PlayerController.StaticPlacedItem.cs, partial)

기존 월드아이템(`ItemObject`) 픽업의 **서버 권위 예약(pending-claim) 프로토콜**을 그대로 따른다.

1. (Owner) `TryPickupStaticPlacedItem` → 서버 요청. 로컬 in-flight 쿨다운으로 연타 방지.
2. (Server) 거리/Remains 검증 후 **Remains 선점 감소(예약)** → 요청자에게 확정(TargetRpc).
3. (Claimant) 전량 수용 가능(all-or-nothing)하면 인벤토리 추가 후 성공/실패 보고.
4a. (Server) 성공: 예약을 단일 소비하고, 남은 Remains ≤ 0 이면 사라짐 처리 브로드캐스트(모드별 전역/개별).
4b. (Server) 실패/접속 종료: 선점 감소한 Remains 복원 + 예약 제거.

### 씬 마이그레이션 도구 (Editor)

`SceneItemPlacementConverter` (에디터 메뉴): 씬의 `SceneItemPlacement` 를 `StaticPlacedItem` 으로 일괄 변환하고, 상호작용 감지를 위한 `BoxCollider`(Renderer bounds 기반)를 자동 추가. Undo 지원, 씬 텍스트 직접 편집 없음.

**OverworldScene 변환 결과**: 성공 194개, 건너뜀(동일 item+entity 중복 컴포넌트 제거) 142개, 실패 0개.

## 자세한 달성 목표

- `EntityType` 에 `StaticPlacedItem` 값 1개 추가(기존 값 순서 불변, 하위 호환). ✅
- `PlayerController.Network.cs` 의 `OnSpawnServer`(신규 접속자 전역 사라짐 동기화)와 `OnStopServer`(미확정 예약 복원 + Local 상태 purge) 훅에 최소 침습적으로 연결. ✅
- Local 모드는 기본적으로 재접속 시 초기화(세션 지속) 정책. ✅

## 문서화

- API 레퍼런스: `Documents/api-references/MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.md` ✅
- 설정/변환 가이드: `Documents/working-guide/features/items/static-placed-item-setup-guide.md` ✅
- 본 제안서: 현재 파일 ✅

## 가용성과 테스트

- **가용성 위험**: MultiplayerInfrastructure(재사용 대상 코어)에 대한 변경이므로 신중히 격리했다. `EntityType` enum 값 추가는 append-only 로 기존 직렬화에 영향 없음. `PlayerController` 변경은 신규 partial 파일과 두 훅의 최소 추가로 한정.
- **씬 마이그레이션**: OverworldScene 변환 완료(성공 194, 건너뜀 142). 건너뜀은 완전 중복(동일 item+entity) 컴포넌트 정리로 확인됨. 플레이 모드 런타임 검증은 후속 과제.
- **테스트 커버리지**: 호스트/클라이언트 분리 환경에서의 교차 런타임 검증은 후속 과제로 분리(`scheduled/2026-07-03-static-placed-item-followup/`).

## 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- **성공 지표**: 에디터 배치 위치와 인게임 위치가 동일(산란 0). 획득 시 Remains 규칙대로 사라짐. 서버 권위 우회(무단 ack/연타 double-grant/ack 재전송)로 Remains 상한이 깨지지 않음.
- **수용 기준** (런타임 검증은 후속 과제):
  - [ ] `VanishedGlobalOnPickup`: Remains 소진 시 전 플레이어에게서 사라지고 신규 접속자에게도 사라진 상태로 동기화된다.
  - [ ] `VanishedLocalOnPickup`: 획득한 플레이어에게만 사라지고, 재접속하면 초기화된다.
  - [ ] `AlwaysExists`: 무한 획득 가능.
  - [ ] 인벤토리 만석 시 아이템이 복제/유실되지 않고 Remains 가 복원된다.
  - [ ] 변환 도구로 만든 `StaticPlacedItem` 이 상호작용 힌트에 노출되고 획득된다.

## 링크, 참고사항

- 관련 기존 시스템: `ItemObject`, `SceneItemPlacement`, `LootableItemInteractHandler`, `NearbyInteractablesDetector`
- 픽업 프로토콜 원본: `PlayerController.Network.cs` (world item pickup)
- 코드 리뷰 지적으로 서버 예약(pending-claim) 메커니즘을 도입해 무단 ack/double-grant/ack 재전송 취약점을 차단함.
