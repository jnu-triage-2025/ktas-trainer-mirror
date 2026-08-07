# 조합 결과 아이템 지연 획득 훅(Deferred OnGet) 발행

> 상태: **반영 완료(done, 2026-08-07)**. `Item.DeferredOnGet` 플래그, 조합 결과물 플래그 설정(`PlayerController.Inventory`), 인벤토리 진입 시 OnGet 발행(`InventoryUIController`)이 구현됨.

### 개요

플레이어가 인벤토리 조합 패널에서 아이템을 조합하면 결과 아이템은 **커서(held item)** 로
지급된 뒤 슬롯에 배치된다. 이 "커서 → 슬롯 배치" 경로는 `PlayerController.TryAddItemToInventory`
를 거치지 않고 `InventoryUIView` 가 슬롯 DTO 를 직접(`slot.SetItem`/`Push`) 변경하므로, 아이템
획득 훅인 `Item.OnGet` 이 영원히 호출되지 않는다.

`Item.OnGet` 은 "플레이어가 아이템을 획득했을 때"의 확장 지점으로, `TriageTrainer` 의
`MedicalItem` 은 이 훅에서 시나리오 게이팅/퀘스트 신호(`sig.<id>`, `sig.click_<id>`)를 발행한다.
따라서 **조합으로 획득한 아이템은 획득 신호를 발행하지 못한다.** 이 제안은 조합 결과물에
"지연 획득(Deferred OnGet)" 상태를 표시하고, 실제 인벤토리 진입 시점에 `OnGet` 을 발행함으로써
조합이 "새 아이템의 획득"이라는 도메인 의미를 시스템 수준에서 일관되게 표현하도록 한다.

> **1차 시도(제작 시점 OnGet) 폐기 이력:** 최초에는 `TryCraftRecipe` 가 조합 성공 직후
> `OnGet` 을 호출하도록 구현했다. 그러나 조합 결과물은 아직 인벤토리에 들어오지 않은
> "커서" 상태이므로, 제작 시점에 획득 신호가 먼저 발행되는 의미 오류가 있었다. 콘텐츠
> 검토 결과, 튜토리얼 시나리오는 제작 시점 신호로 게이팅할 필요가 없고(제출 인터랙션을
> 즉시 재구성), 제작 완료는 **퀘스트 서브목표**(획득 시 OnGet)로 추적하는 것이 올바른
> 설계로 확인되었다. 이에 제작 시점 호출을 폐기하고 **획득(인벤토리 진입) 시점 발행**으로
> 재설계한다.

### 해결하려는 문제 상황

튜토리얼 시나리오(`tutorial.scenario.json`)의 "시계 제작" 흐름에서 학습자는 재료
(`tin_ingot`×3, `small_gear`×5, `small_chain`×1)로 `handy_clock` 을 조합한 뒤 모자 NPC 에게
제출해야 한다.

- 조합 결과 `handy_clock` 은 커서로 지급되어 인벤토리 배치 시 `TryAddItemToInventory` 를
  우회하므로 `OnGet` 이 호출되지 않고, `sig.click_handy_clock` 이 올라오지 않는다.
- 따라서 "시계 제작" 완료(획득)를 감지할 수 없어, 시계 제작 퀘스트 서브목표가 완료되지 않는다.

동시에 기존 시나리오는 `sig.click_handy_clock` 을 `waitForCondition` Validator 게이트
(`V_TUT_CLOCK_CRAFTED`)로 기다려 제출 인터랙션 재구성을 차단했는데, 이는 (a) 신호 미발생 시
**무한 대기** 하고 (b) 제출 인터랙션 재구성을 불필요하게 제작 완료에 결합했다. 콘텐츠 설계상
제출 인터랙션은 **택배 제출 완료 직후 즉시** 시계 제출용으로 재구성되어야 한다.

### 사용자 경험 목표

- 택배를 제출하면 NPC 의 "택배 제출" 인터랙션이 즉시 사라지고, 이어서 "시계 제출" 인터랙션이
  즉시 활성화된다(제출 인터랙션이 택배 상태로 잔존하지 않는다).
- 재료가 지급되고 "시계 제출" 퀘스트가 활성화되며, 퀘스트는 두 서브목표(① 시계 제작 ② 시계 제출)를
  순서대로 추적한다.
- 학습자가 시계를 조합해 인벤토리에 넣으면(획득) ① 번 서브목표가 완료되고, 모자에게 제출하면
  ② 번 서브목표와 시나리오가 완료된다.
- 조합으로 획득하는 모든 아이템(현재/미래)이 월드에서 줍는 아이템과 동일하게 획득 훅(`OnGet`)의
  효과(신호, 로깅 등)를 **실제 획득 시점**에 일관되게 받는다.

### 제안

**1. 시스템 — 지연 획득 훅(Deferred OnGet)**

- `Item` 에 `DeferredOnGet` 플래그를 추가한다("획득 훅이 아직 발행되지 않은 지연 획득 상태").
- `PlayerController.TryCraftRecipe` 는 조합 성공 시 결과 아이템에 `DeferredOnGet = true` 만
  설정하고 반환한다(제작 시점 `OnGet` 호출 없음).
- `InventoryUIController` 는 조합 요청(`HandleCraftRequest`) 시 결과 아이템을 `_pendingAcquisition`
  으로 기억하고, 슬롯 변화(`HandleSlotsMutated`)를 감지하면 해당 아이템이 인벤토리에 진입했는지
  (`CountItemInInventory > 0`) 확인해 진입 확인 시 `OnGet(player)` 를 발행하고 플래그를 해제한다
  (`TryResolvePendingAcquisition`).
- `PlayerController.TryAddItemToInventory`(월드 습득 경로)는 `OnGet` 호출과 함께 `DeferredOnGet` 을
  해제해 두 경로 간 중복 발행을 막는다.

설계 근거:

- **획득 시점 발행.** 조합 결과물은 커서를 거쳐 인벤토리에 진입하므로, 실제 진입 시점에 `OnGet` 을
  발행하는 것이 "획득 = 인벤토리 추가"라는 `OnGet` 의미와 정확히 부합한다.
- **뷰 무수정·저위험.** `InventoryUIView` 의 배치 경로(`slot.SetItem`/`Push`/`SwapWith`)는 그대로 두고,
  컨트롤러가 슬롯 변화 감지로 획득을 판정한다. 뷰의 슬롯 모델은 `PlayerController._slots` 와 동일
  인스턴스를 공유하므로 배치 직후 `CountItemInInventory` 로 진입 여부를 신뢰성 있게 확인할 수 있다.
- **이동 아이템 회귀 없음.** 인벤토리 내 아이템 이동은 `DeferredOnGet` 플래그가 없어 아무것도
  발행하지 않는다. 오직 조합 결과(플래그 보유)만 획득 시 `OnGet` 이 발행된다.
- **기존 규약 재사용.** 신호명(`sig.<id>`, `sig.click_<id>`)을 시스템이 하드코딩하지 않고
  `MedicalItem.OnGet` 의 virtual dispatch 에 위임한다.

**2. 콘텐츠 — 튜토리얼 시나리오/퀘스트 재구성**

- 제출 인터랙션 재구성을 제작 완료 게이트에서 분리한다. 택배 제출 완료 직후:
  `V_TUT_DELIVERY_PACKAGE_SUBMITTED → CONFIG_PACKAGE_SUBMISSION_HIDE`(택배 인터랙션 비활성)
  `→ CONFIG_CLOCK_SUBMISSION`(시계 인터랙션 즉시 활성화, `handy_clock` 요구) `→ …`.
  기존 제작 게이트 `V_TUT_CLOCK_CRAFTED` 와 그 뒤 `D_TUT_SUBMIT_HINT` 는 제거한다.
- 사용자 정의 흐름: `택배 제출 → 택배 제출 인터랙션 제거 → 시계 제출 인터랙션 추가 → 시계 재료
  give → 시계 제출 퀘스트 활성화`.
- `tutorial-quest-crafting` 을 두 서브목표로 정리한다:
  ① `InteractionSignalReceived click_handy_clock`(시계 제작 — 획득 시 `OnGet` 신호)
  ② `InteractionSignalReceived tutorial.crafting.clock.submitted`(시계 제출).

### 자세한 달성 목표

- `TryCraftRecipe` 성공 시 결과 아이템 `DeferredOnGet = true` / 실패 시 `null` 반환(플래그 무의미).
- 조합 결과물이 인벤토리 진입 시 `OnGet` 1회 발행. `MedicalItem` 계열은 `sig.<id>`, `sig.click_<id>` 발행.
- 인벤토리 내 아이템 이동/재배치는 `OnGet` 미발생(회귀 없음).
- 택배 제출 직후 제출 인터랙션이 비활성→시계용으로 즉시 재활성화되고, 시계 제작/제출로 완주한다.
- 시계 제작 퀘스트 서브목표가 시계 획득(인벤토리 진입) 시 완료된다.

### 문서화

- `Documents/requirements/content-definitions/scenario/tutorial.md`
  - TUT-CRAFT-2(제출 대상 재활성화)·TUT-CRAFT-3(조합 결과 OnGet 누락) 해결 이력 갱신.
  - 확정 보완 연결(플로우)을 새 흐름으로 정합화.
- `Documents/working-guide/features/scenario/interaction-signal-integration-spec.md`(선택)
  - 조합 결과 아이템 신호 발행 지점(획득 시 `OnGet`) 보강.
- 코드 주석: `TryCraftRecipe`, `InventoryUIController.TryResolvePendingAcquisition`, `Item.DeferredOnGet`
  에 지연 획득 사유 기록(완료).

### 가용성과 테스트

- 기존 JSON/에셋과 하위호환. `DeferredOnGet` 은 추가 필드(기본 false)로 기존 아이템 무영향.
- 위험:
  - 획득 판정이 슬롯 변화 감지 기반이므로, 드물게 "조합 결과물을 인벤토리 밖으로 버린 뒤 월에서
    재습득" 같은 경로에서는 `OnGet` 이 중복 발행될 수 있다. 신호는 멱등(`RegisterLocal` 전이 판정)
    이며 현존 `OnGet` 구현은 모두 멱등(로그/신호)이라 무해하다.
  - `InventoryUIController` 가 소유자 클라이언트 로컬에서 동작하므로, 원격 클라이언트 조합은 해당
    클라이언트의 컨트롤러가 동일 로직으로 처리한다(튜토리얼은 단일 호스트).
- 테스트(제안):
  - 단기: 튜토리얼 PlayMode 수동 검증(택배→시계 제작/제출 완주, `Signal raised: sig.click_handy_clock` 확인).
  - 중기: `PlayerController` 인벤토리/조합을 네트워크 독립 테스트 더블로 분리해 "조합 → 인벤토리
    진입 → OnGet 1회 발행" 회귀 테스트 추가.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 튜토리얼에서 시계를 조합해 인벤토리에 넣으면 `sig.click_handy_clock` 이 등록되고
  ("Signal raised: sig.click_handy_clock"), 시계 제출까지 `TUT_END` 로 완주한다.
- 수용 기준:
  - 조합 성공 → 인벤토리 진입 시 `OnGet` 1회 / 조합 실패 시 0회.
  - 택배 제출 직후 제출 인터랙션이 비활성화되었다가 즉시 `handy_clock` 요구로 재활성화된다.
  - 인벤토리 아이템 이동 시 `OnGet` 미발생(회귀 없음).
  - 시계 제작 퀘스트 서브목표가 획득 시 완료된다.

### 링크, 참고사항

- 관련 코드:
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/ItemSystem/Item.cs` (`DeferredOnGet`)
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Inventory.cs`
    (`TryCraftRecipe`, `TryAddItemToInventory`)
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/InventoryUIController.cs`
    (`HandleCraftRequest`, `TryResolvePendingAcquisition`)
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/InventoryUIViewElement.cs`
    (`OnGet` 우회 배치 경로 — 본 제안에서는 미수정)
  - `Assets/Modules/TriageTrainer/Scripts/Items/MedicalItem.cs` (`OnGet` 신호 발행 규약)
- 관련 콘텐츠/문서:
  - `Assets/Modules/TriageTrainer/Resources/Scenario/tutorial.scenario.json`
  - `Assets/Modules/TriageTrainer/Resources/Quest/tutorial.quests.quest.json`
  - `Documents/requirements/content-definitions/scenario/tutorial.md` (TUT-CRAFT-2/3)
