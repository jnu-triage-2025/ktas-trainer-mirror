---
title: "시나리오 튜토리얼"
doc_type: requirement
domain: content-definitions
progress: "1-designed"
status: active
updated: 2026-07-22
flags: ["refactor-required"]
---

# 시나리오 튜토리얼

> 이 문서는 `tutorial_delivery_quest_debug.scenario.json`을 비롯한 기존 JSON 또는 부속
> 에셋을 변환 근거로 사용하지 않는다. 아래 감사와 본문의 명시된 계약만으로 새
> `tutorial.scenario.json`을 작성한다.

## JSON 변환 전 연결성 감사 (2026-07-19)

현재 본문은 연출 순서는 제시하지만, Scenario 그래프의 시작/종료와 게임플레이 신호의
생산자가 명시되지 않아 그대로 JSON으로 옮기면 첫 Validator 또는 delivery quest에서
정지할 수 있다. 다음 보완을 적용하거나, `인간 판단 필요` 항목을 확정하기 전에는
플레이 가능 Scenario로 승인하지 않는다.

### 확정 보완 연결

아래 식별자와 연결은 서술만으로도 결정 가능하므로 변환 시 삽입한다. 실제 JSON에서는
도메인 행동을 `RegistryContains(RuntimeState)` Validator와 동일 이름의 runtime signal로
게이팅한다.

```text
TUT_START
  -> Q_TUT_MOVE_ADD
  -> V_TUT_PLAYER_MOVED
  -> Q_TUT_MOVE_REMOVE
  -> DL_TUT_MOVE_CALL_DELAY
  -> D_TUT_CALL
  -> Q_TUT_FIND_HAT_ADD
  -> V_TUT_HAT_TALK_START
  -> Q_TUT_FIND_HAT_REMOVE
  -> C_TUT_GREETING
       yes -> D_TUT_HAT_STARTING_2
       no  -> D_TUT_HAT_STARTING_NO_1 -> ... -> D_TUT_HAT_STARTING_NO_7
                                                -> D_TUT_HAT_STARTING_2
  -> D_TUT_HAT_STARTING_3
  -> Q_TUT_DELIVERY_ADD
  -> DECOY_WATCHER_FORK (Parallel, waitMode: None)
       -> [bg] branch_decoy_watch -> V_TUT_DECOY_PICKUPED -> D_TUT_DECOY_HINT
  -> V_TUT_DELIVERY_WAYPOINT_REACHED
  -> D_TUT_PACKAGE_PROMPT
  -> V_TUT_DELIVERY_PACKAGE_ACQUIRED
  -> V_TUT_DELIVERY_PACKAGE_SUBMITTED
  -> CONFIG_PACKAGE_SUBMISSION_HIDE (ItemSubmissionConfig, enabled: false — 택배 제출 인터랙션 비활성)
  -> CONFIG_CLOCK_SUBMISSION (ItemSubmissionConfig, enabled: true, handy_clock 요구 — 시계 제출 인터랙션 즉시 활성화)
  -> Q_TUT_DELIVERY_REMOVE
  -> D_TUT_DELIVERY_COMPLETE
  -> D_TUT_CRAFTING_REQUEST
  -> C_TUT_CRAFTING_ACCEPT
  -> GIVE_TIN -> GIVE_GEAR -> GIVE_CHAIN
  -> Q_TUT_CRAFTING_ADD
  -> D_TUT_CRAFTING_HINT
  -> V_TUT_CLOCK_SUBMITTED
  -> Q_TUT_CRAFTING_REMOVE
  -> D_TUT_CRAFTING_COMPLETE
  -> TUT_END
```

> **제출 인터랙션 재구성 시점(2026-07-25 개정):** 제출 인터랙션(`tutorial-guide-hat-package-submission`)
> 재구성은 시계 "제작 완료"가 아니라 **택배 제출 완료 직후** 이루어진다. 택배 인터랙션을 비활성
> (`CONFIG_PACKAGE_SUBMISSION_HIDE`)하고 곧바로 시계 제출용으로 재활성(`CONFIG_CLOCK_SUBMISSION`,
> `handy_clock` 요구)한다. 제작 완료는 시나리오 게이트가 아니라 시계 제출 퀘스트의 서브목표
> (획득 시 `OnGet` 신호 `sig.click_handy_clock`)로 추적한다. 따라서 제작 게이트 `V_TUT_CLOCK_CRAFTED`와
> `D_TUT_SUBMIT_HINT`는 제거했다.

- 시계 제작 흐름(`D_TUT_CRAFTING_REQUEST`~`D_TUT_CRAFTING_COMPLETE`)은 부분 3 감사 대사 직후에
  연결된다. `GIVE_TIN`/`GIVE_GEAR`/`GIVE_CHAIN`은 `ExecuteCommand`(`give @s`)로 플레이어
  인벤토리에 재료를 지급한다. `V_TUT_CLOCK_CRAFTED`는 `MedicalItem.OnGet`이 자동 발행하는
  `sig.click_handy_clock`을 `waitForCondition` 게이트로 기다린다. `CONFIG_CLOCK_SUBMISSION`은
  기존 택배 제출과 같은 `tutorial-guide-hat-package-submission` 대상을 재사용하며,
  `completionSignalIdentifier`를 `tutorial.crafting.clock.submitted`로 구분한다.
- `TUT_START`는 이 문서의 시작 노드 identifier로 사용한다. `TUT_END`만
  `nextIdentifier: null`인 종료 노드로 둔다.
- 이동 완료 뒤에는 `DL_TUT_MOVE_CALL_DELAY`가 500 ms를 대기한 뒤 `D_TUT_CALL`로 진행한다.
  `D_TUT_CALL`의 `autoAdvanceSeconds`는 표시 후 자동 진행 시간이지 표시 전 지연이 아니므로
  사용하지 않는다.
- `V_TUT_HAT_TALK_START`는 NPC의 상호작용 identifier
  `npc-tutorial-guide-hat__interaction-talk-start`가 발생시키는
  `tutorial_hat_talk_start` signal을 기다린다. 퀘스트 완료 자체가 대화 시작을 대신하지
  않도록, `Q_TUT_FIND_HAT_REMOVE`는 이 Validator 뒤에 둔다.
- greeting choice의 1·2번 선택지는 모두 `D_TUT_HAT_STARTING_2`로, 3번은 no 체인으로
  연결한다. no 체인의 두 선택지는 모두 다음 no 대사로 연결한다.
- `V_TUT_DELIVERY_WAYPOINT_REACHED`가 `tutorial_delivery_waypoint_reached` signal을
  받으면 quest는 계속 유지하고 `D_TUT_PACKAGE_PROMPT`만 한 번 표시한다. 그 뒤의 package
  획득/제출 Validator가 각각 2·3번 task를 완료한다. 이렇게 해야 waypoint 완료가 전체
  quest 완료로 오인되지 않는다.
- 제출 signal은 `tutorial_delivery_package_submitted`로 고정하고, 대상 NPC와 item
  identifier를 함께 검증한다. 단순 인벤토리에서 item이 사라졌다는 사실만으로는 제출
  성공을 판정하지 않는다.

### 플레이 차단 항목과 인간 판단 위치

| ID | 위치 | 부족한 연결 | 처리 |
|---|---|---|---|
| TUT-START-1 | `TUT_START` 이전 | 플레이어 spawn/소유권, 튜토리얼 씬 로드 완료, waypoint·StaticPlacedObject 배치의 선행 조건이 없다. 모자 NPC는 `actingNpcs`의 `npc-tutorial-guide-hat`가 `npc_tutorial_hat` preset으로 시작 전에 생성한다. | **인간 판단 필요:** 튜토리얼 전용 scene 및 Scenario 시작 시점(씬 로드 후)을 확정한다. 시작 requirements에 `delivery-storage-spot`, 택배 object를 선언한다. |
| TUT-SIG-1 | `V_TUT_PLAYER_MOVED` | ~~“WASD와 마우스 입력 또는 이동”은 서로 다른 완료 의미이며 현재 signal identifier/producer가 없다.~~ **해결(2026-07-21):** `BasicMovementControlTutorialQuestResolver`가 활성 `tutorial-move` 퀘스트에서 WASD, 마우스 버튼, 마우스 이동(카메라 회전) 중 하나라도 입력된 프레임의 시간을 중복 없이 누적한다. 합계가 1초를 **초과**하면 `tutorial_player_moved`를 발신하고 퀘스트를 완료한다. | `tutorial-move` definition은 `Resources/Quest/tutorial.quests.quest.json`에 등록한다. 이 전용 resolver는 `QuestManager`가 런타임에 부착한다. |
| TUT-SIG-2 | 모자 상호작용 | `actingNpcs[npc-tutorial-guide-hat]`의 `Signal` 상호작용 `npc-tutorial-guide-hat__interaction-talk-start`가 `tutorial_hat_talk_start`를 발생시킨다. | Requirements Supports에서 RuntimeSignal producer와 validator consumer를 검증한다. |
| TUT-FLOW-1 | “Title 발생” | Title의 UI 종류, 지속시간, 닫는 조건, 그래프 전이 여부가 정의되지 않았다. | **인간 판단 필요:** 단순 안내 UI라면 event `show_tutorial_interaction_hint`를 `Q_TUT_FIND_HAT_ADD` 직후 InvokeEvent로 추가한다. 진행을 막는 UI라면 완료 signal과 Validator를 별도 정의한다. |
| TUT-FLOW-2 | 이동 완료 후 0.5초 | ~~현재 Scenario 스키마에는 일반 Delay node가 명시되어 있지 않아, 서술 그대로의 0.5초 지연을 직렬 노드로 저장할 수 없다.~~ **해결(2026-07-21):** 공통 시간값 `ScenarioTimeValue`를 쓰는 `Delay` node로 명시한다. | `DL_TUT_MOVE_CALL_DELAY`의 `duration`은 `{ "value": 500, "unit": "Milliseconds" }`, `waitUntil`은 `WaitUntilDone`이다. |
| TUT-QUEST-1 | `Q_TUT_MOVE_ADD`, `Q_TUT_FIND_HAT_ADD`, `Q_TUT_DELIVERY_ADD` | 세 quest에 title/content/task definition 및 Add/Remove의 공통 definition identifier가 없다. | `tutorial-move`, `tutorial-find-hat`, `tutorial-quest-delivery`의 별도 quest definition을 작성한다. delivery는 본문에 적힌 3개 task와 순서를 보존한다. |
| TUT-SIG-3 | delivery waypoint/획득/제출 | ~~`delivery-storage-spot` 도착, `tutorial_delivery_package` pickup, 모자에게 submit 모두 producer가 없고, quest task 완료가 Scenario signal을 Raise한다는 계약도 없다.~~ **해결(2026-07-25):** 세 signal 모두 배선 완료. 단 신호명은 원안에서 변경됨 — waypoint: quest task `WaypointReached.onCompleteSignalIdentifier`(`tutorial.quest.delivery.storage.reached`), 획득: `MedicalItem.OnGet` 관례 신호 `sig.tutorial_delivery_package`(`V_TUT_DELIVERY_PACKAGE_ACQUIRED`의 `registryIdentifier`를 이에 맞게 수정. 원안 `tutorial_delivery_package_acquired`는 producer가 없어 제출 후 진행 불능 버그의 원인이었음), 제출: ItemSubmission 상호작용 `completionSignalIdentifier`(`tutorial.delivery.package.submitted`). | waypoint tracker, StaticPlacedObject pickup, NPC item-submit callback이 각각 `tutorial_delivery_waypoint_reached`, `tutorial_delivery_package_acquired`, `tutorial_delivery_package_submitted`를 Raise하도록 구현/배선한다. |
| TUT-ASSET-1 | 택배 및 오답 아이템 | ~~택배 StaticPlacedObject의 scene object identifier·spawn 위치·item definition이 없고, “여러 개” 오답 아이템의 목록/독백 트리거도 미정이다.~~ **해결(2026-07-21):** 아래 「택배 보관소 아이템 배치 확정」의 정답 1개와 오답 3개를 StaticPlacedItem으로 배치한다. | 정답 물품만 `tutorial_delivery_package`를 지급하며, 오답 물품은 각각 1회성 독백만 재생하고 퀘스트 진행 상태를 바꾸지 않는다. |
| TUT-END-1 | 부분 4 이후 | ~~감사 인사 뒤 화면 전환, 다음 Scenario, 종료 메시지 중 어느 것도 없다.~~ **부분 4 시계 제작 퀘스트 추가(2026-07-22):** 택배 감사 대사 뒤에 시계 제작 요청→재료 지급→조합→제출 흐름이 연결된다. 최종 종료는 시계 제작 완료 대사(`D_TUT_CRAFTING_COMPLETE`) 이후이며, 화면 전환·다음 Scenario는 여전히 미결정이다. | **인간 판단 필요:** `D_TUT_CRAFTING_COMPLETE` 뒤의 종료 UX를 확정한다. 확정 전에는 `TUT_END`(`nextIdentifier: null`)로만 종료한다. |
| TUT-CRAFT-1 | 시계 제작 아이템 | `tin_ingot`, `small_gear`, `small_chain`, `handy_clock`의 아이콘/3D 모델 리소스가 없다. | **코드 등록 완료(2026-07-22):** C# 클래스 정의 및 레지스트리 등록, 조합 레시피 등록 완료. 리소스(아이콘 스프라이트, 3D 모델 프리팹)는 추후 추가 필요. `ValidateItemResources()`에서 누락 Warning 출력은 정상. |
| TUT-CRAFT-2 | 시계 제출 대상 | `CONFIG_CLOCK_SUBMISSION`이 `tutorial-guide-hat-package-submission`을 재사용한다. 택배 제출 후 해당 Interactable이 택배 요구 상태로 잔존하면 시계 제출 UX가 깨진다. | **해결(2026-07-25 개정):** 제출 인터랙션 재구성을 제작 완료가 아닌 **택배 제출 완료 직후**로 이동했다. `V_TUT_DELIVERY_PACKAGE_SUBMITTED` 직후 `CONFIG_PACKAGE_SUBMISSION_HIDE`(`enabled: false`)로 택배 인터랙션을 비활성(잔존 제거)하고, **곧바로** `CONFIG_CLOCK_SUBMISSION`(`enabled: true`, `handy_clock` 요구)으로 동일 대상을 시계 제출용으로 재활성화·재구성한다. 이후 `GIVE_*`(재료 지급) → `Q_TUT_CRAFTING_ADD`(퀘스트 활성화) 순서로 진행한다. |
| TUT-CRAFT-3 | 시계 조합 게이팅 신호 | 구 설계는 `V_TUT_CLOCK_CRAFTED`가 `MedicalItem.OnGet`의 `sig.click_handy_clock`을 `waitForCondition`으로 기다려 제출 인터랙션 재구성을 게이팅했다. 그러나 (a) 조합 결과는 커서로 지급되어 인벤토리 배치 시 `TryAddItemToInventory`를 우회(`slot.SetItem`/`Push`)하므로 `OnGet`이 호출되지 않아 신호가 영원히 올라오지 않고(**무한 대기**), (b) 제출 인터랙션 재구성을 제작 완료에 결합할 이유가 없었다. | **해결(2026-07-25 개정):** ① 시나리오에서 제작 게이트 `V_TUT_CLOCK_CRAFTED`/`D_TUT_SUBMIT_HINT`를 제거하고 제출 인터랙션을 택배 제출 직후 즉시 재구성(TUT-CRAFT-2). ② 제작 완료 추적을 **퀘스트 서브목표**로 이동: `tutorial-quest-crafting`을 `InteractionSignalReceived click_handy_clock`(시계 제작) + `InteractionSignalReceived tutorial.crafting.clock.submitted`(시계 제출) 2개로 정비. ③ 시스템: 조합 결과 `OnGet` 누락을 **지연 획득(Deferred OnGet)** 으로 수정 — `Item.DeferredOnGet` 플래그를 `TryCraftRecipe`가 설정하고, `InventoryUIController`가 인벤토리 진입 감지 시 `OnGet`을 발행(제작 시점이 아닌 실제 획득 시점). 제안서: `Agents/Proposals/done/2026-07-25-crafting-result-onget-acquisition/`. 인벤토리 내 아이템 이동은 `OnGet` 미발생(회귀 없음). |

### 변환 승인 조건

- TUT-START-1의 scene/preset 및 필수 배치물 requirement가 해소되어야 한다.
- TUT-SIG-1~3의 signal producer가 실제 gameplay callback에 배선되어야 한다.
- TUT-QUEST-1의 세 quest definition과 task 완료 계약이 등록되어야 한다.
- TUT-FLOW-1 및 TUT-END-1의 인간 판단이 확정되어야 한다.
- 변환 후 Requirements Supports Production 검증에서 unresolved `Error`가 0개여야 한다.

### [DL_TUT_MOVE_CALL_DELAY] DelayNode

`Q_TUT_MOVE_REMOVE` 다음에 두고, 호출 대사 `D_TUT_CALL` 전에만 실행한다. 이동 퀘스트의
완료 signal이나 대사 자동 진행 시간으로 이 지연을 대체하지 않는다.

```json
{
  "identifier": "DL_TUT_MOVE_CALL_DELAY",
  "nodeType": "Delay",
  "duration": { "value": 500, "unit": "Milliseconds" },
  "waitUntil": "WaitUntilDone",
  "nextIdentifier": "D_TUT_CALL"
}
```

### 택배 보관소 아이템 배치 확정

아래 아이템은 튜토리얼 전용 씬의 waypoint `delivery-storage-spot` 주변에 `StaticPlacedItem`으로
배치한다. 기준점은 waypoint의 정면을 향한 보관 선반이며, 좌/중/우는 플레이어가 waypoint에서 선반을
바라보는 방향이다. 정답 물품은 중앙 선반에 두어 텍스트를 읽은 플레이어가 자연스럽게 선택할 수 있게
하고, 오답 물품은 서로 다른 선반 칸에 분산해 마우스 휠 선택과 F 상호작용을 연습하게 한다.

| 구분 | 표시 이름 | scene object identifier | 배치 컴포넌트 | 지급 item definition identifier | 배치 위치 | 상호작용 결과 |
|---|---|---|---|---|---|---|
| 정답 | `택배: 모자님 앞` | `tutorial-delivery-storage-package-hat-attn` | **StaticPlacedItem** | `tutorial_delivery_package` | 중앙 선반, waypoint에서 1.0 m 앞·허리 높이 | 인벤토리에 `tutorial_delivery_package` 1개를 넣는다. 이후 모자 NPC에게 제출 가능하다. |
| 오답 | `배달: 밤샜음 청년` | `tutorial-delivery-storage-decoy-overnight-youth` | **ScenarioInteractable** | `tutorial_delivery_decoy_overnight_youth` | 좌측 상단 선반, waypoint에서 1.0 m 앞·0.7 m 좌측 | 상호작용 시 공통 오답 signal 발생 + DisinteractableDialogue 표시 |
| 오답 | `택배: 8909` | `tutorial-delivery-storage-decoy-8909` | **ScenarioInteractable** | `tutorial_delivery_decoy_8909` | 우측 상단 선반, waypoint에서 1.0 m 앞·0.7 m 우측 | 상호작용 시 공통 오답 signal 발생 + DisinteractableDialogue 표시 |
| 오답 | `우편: 김강산님` | `tutorial-delivery-storage-decoy-kim-gangsan-mail` | **ScenarioInteractable** | `tutorial_delivery_decoy_kim_gangsan_mail` | 좌측 하단 선반, waypoint에서 1.0 m 앞·0.7 m 좌측·0.45 m 아래 | 상호작용 시 공통 오답 signal 발생 + DisinteractableDialogue 표시 |

> **배치 컴포넌트 구분:** 정답 아이템은 `StaticPlacedItem`으로 배치하여 서버 권위 획득 프로토콜(예약→확인→vanish)을
> 통해 인벤토리에 아이템을 지급한다. 오답 아이템은 `ScenarioInteractable`로 배치하여 아이템 지급 없이
> 상호작용 signal만 발생시킨다. 오답은 인벤토리에 들어가지 않으므로 퀘스트 완료 조건에 영향을 주지 않는다.

### [DECOY_WATCHER_FORK] 오답 공통 독백

세 오답 아이템은 `ScenarioInteractable`로 배치되므로 아이템 지급 없이 상호작용 시
**동일한 signal** `tutorial-decoy-package-on-pickuped`를 발생시킨다.
정답(`StaticPlacedItem`)과 달리 인벤토리에 들어가지 않는다.

시나리오 그래프에서는 `add_delivery_quest` 직후 `DECOY_WATCHER_FORK`(`Parallel`,
`waitMode: None`)로 백그라운드 감시 브랜치를 시작한다. 이 브랜치는
`Validator`(`waitForCondition: true`, `sig.tutorial-decoy-package-on-pickuped`)로 signal을
기다렸다가 `DisinteractableDialogue`(“이 물건은 아닌 것 같다.”, 3초)를 표시한다.
`waitMode: None`이므로 이 Parallel은 즉시 `nextIdentifier`(`watch_delivery_quest`)로
진행하며, 오답 독백은 배달 퀘스트 흐름과 독립적으로 재생된다.

```json
{
  “identifier”: “D_TUT_DECOY_HINT”,
  “nodeType”: “DisinteractableDialogue”,
  “speakerName”: null,
  “dialogueContent”: “이 물건은 아닌 것 같다.”,
  “fadeInDuration”: { “value”: 0.2, “unit”: “Seconds” },
  “displayDuration”: { “value”: 3, “unit”: “Seconds” },
  “fadeOutDuration”: { “value”: 0.2, “unit”: “Seconds” }
}
```

오답 `ScenarioInteractable`은 소멸 개념이 없으므로 플레이어가 반복해 상호작용할 수 있다.
이 signal은 퀘스트 완료 조건이나 제출 signal에 사용하지 않는다.

## 원본 시나리오 서술

## 튜토리얼 씬

### 부분 1

1. 이동
  
퀘스트 발행 및 미니 퀘스트 오버레이 발생
- 이동하기
- 텍스트 콘텐츠: "wasd 키와 마우스를 사용하여 캐릭터를 이동하세요."
- 기술노트: 이 콘텐츠에 한하여, wasd와 마우스 입력 혹은 플레이어의 이동 자체를 감지해서 다음으로 진행해야 함

2. 상호작용

Disinteractable dialogue

- 발생 조건: 1.의 퀘스트가 완료되었다면 0.5초 후에 발생
- 발화자: "???"
- 텍스트: "저기요! 잠시만요!"

퀘스트 발행 및 미니 퀘스트 오버레이 발생

- 발생 조건: 위의 Disinteractable dialogue가 완료된 즉시
- 텍스트 콘텐츠: "앞에서 자신을 부르는 사람을 찾기"
- 완료 조건: 플레이어가 NPC에게 말 걸기 상호작용을 수행하면 완료
- 기술노트: npc는 사전에 생성됨.(*1)

*1 NPC 정보
- 식별자: npc-tutorial-guide-hat
- 이름: 모자
- 상호작용
  - identifier: npc-tutorial-guide-hat__interaction-talk-start

Title 발생

- 주변에 인물이 존재한다면, F 키를 눌러 말을 걸 수 있습니다.  

----

### 부분 2

1. npc-tutorial-guide-hat__interaction-talk-start 상호작용 시
- npc-tutorial-guide-hat 에 할당됨
- 시나리오 계속
  - 다이얼로그 발생
    - 발화자: "???"
    - 텍스트: "안녕하세요! 신규 선생님이시죠?"
    - 선택지: 
      - "...네..?" -- 1
      - "네!" -- 2
      - "아니요?" -- 3
      - 1, 2 선택 시: yes, 3 선택 시: no

- 다이얼로그 계속
  - 위에서 no 선택시에만: id: `hat-starting-no-1`
    - 발화자: "???"
    - "아, 아니에요? 어쩐다.. 지금 급한 일이 있어서.. 도와줄 분을 찾고 있는데.."
    - 선택지:
      - "..."
      - "...에.."
      - 선택지에 무관하게 다음으로 진행
  - no 선택시의 진행 계속: `hat-starting-no-1` 다음
    - id: `hat-statring-no-2`
    - 발화자: null
    - 텍스트: "(모르는 사람인데, 반론은 받지 않겠다는 표정으로 뻔히 쳐다본다.)"
  - no 선택시의 상황 계속: `hat-statring-no-2` 다음
    - id: `hat-starting-no-3`
    - 발화자: "???"
    - 텍스트: "..."
  - no 선택시의 상황 계속: `hat-starting-no-3` 다음
    - id: `hat-starting-no-4`
    - 발화자: null
    - 텍스트: "...."
  - no 선택시의 상황 계속: `hat-starting-no-4` 다음
    - id: `hat-starting-no-5`
    - 발화자: "???":
    - 텍스트: "...."
  - no 선택시의 상황 계속: `hat-starting-no-5` 다음
    - id: `hat-starting-no-6`
    - 발화자: null
    - 텍스트: "....."
  - no 선택시의 상황 계속: `hat-starting-no-6` 다음
    - id: `hat-starting-no-7`
    - 발화자: null
    - 텍스트: "....네, 그래서요?"


  - 위에서 yes 선택 혹은 `hat-starting-no-7` 다음
    - id: `hat-starting-2`
    - 발화자: "???"
    - "아 네! 반가워요, 저는.. 어... 일단 모자라고 불러주세요"
  
  - 다이얼로그 계속: `hat-starting-2` 다음
    - id: `hat-starting-3`
    - 발화자: "???"
    - 텍스트: "혹시 로비에서 택배를 가져다 주실 수 있나요? 지금 제가 여기서 움직일 수 없는 상황이라서요.."
  
  - 다이얼로그 계속: `hat-starting-3` 다음
    - 퀘스트 발행 및 미니 퀘스트 오버레이 발생
      - 퀘스트: `tutorial-quest-delivery`
        - 텍스트 콘텐츠 "택배 가져오기"
        - 완료조건
          1. waypoint `delivery-storage-spot`으로 이동
            - 표시: "택배 보관소로 가기"
            - 기술노트: 이 조건을 완료했을 때 `disinteractable dialogue` 발생해야함:
              - 발화자: null
              - 텍스트: "아이템 앞에서 F 키를 눌러 택배를 획득하자."
              이 구현을 만족하려면 1번 완료조건이 처리되었을 때 시그널이 발생하여 두 개 갈래로 분기되어 이 다이얼로그 발생이 처리되어야 함. 따라서 완료조건에 onComplete를 추가하고 oncomplete 시에 시그널을 발생하도록 구현을 수정하여야 한다. 
          2. 택배 획득(StaticPlacedObject) 시 아이템 `tutorial_delivery_package` 획득
            - 표시: "택배 획득하기"
          3. `tutorial-guide-hat`에게 아이템 `tutorial_delivery_package` 제출
            - 표시: "모자에게 택배 전달하기"
      * 코멘트: 마우스 휠 인터렉션 조정도 익힐 수 있게 하기 위해 여러개 아이템을 흩뿌려놓고 정답이 아닌 것을 이건 아닌 것 같다고 독백하는 다이얼로그 띄우도록 하기

### 부분 3
  - `tutorial-quest-delivery` 완료 시
    - 다이얼로그 발생
      - 발화자: "모자"
      - 텍스트: "감사합니다! 덕분에 일이 잘 해결되었네요..!"

### 부분 4 — 시계 제작

  1. 모자 NPC 다이얼로그
     - 발화자: "모자"
     - 텍스트: "아 혹시 여기 이 물건들로 시계를 만들어주실 수 있나요? 제가 손기술이 없어서.."

  2. 플레이어 선택 (선택지 1개)
     - "(수락)" → 시계 제작 흐름 진입

  3. 게임 시스템: 아이템 지급 (ExecuteCommand)
     - 주석 주괴 `tin_ingot` ×3
     - 소형 톱니 `small_gear` ×5
     - 소형 사슬 `small_chain` ×1

  4. 퀘스트 발행: `tutorial-quest-crafting`
     - 텍스트 콘텐츠: "시계 제작"
     - 완료조건 (순차):
       1. 재료 확인: tin_ingot 3, small_gear 5, small_chain 1 인벤토리 보유
       2. 시계 조합: handy_clock 1 인벤토리 보유
       3. 시계 제출: 모자에게 handy_clock 제출

  5. 아이템 조합
     - 사전 등록 레시피: tin_ingot 3 + small_gear 5 + small_chain 1 → handy_clock 1
     - 플레이어가 인벤토리 UI에서 조합 수행

  6. 조합 완료 후 안내 다이얼로그
     - 발화자: "시스템"
     - 텍스트: "시계를 조합했습니다! 모자에게 시계를 전달하세요."

  7. 제출 (ItemSubmissionConfig)
     - 대상: `tutorial-guide-hat-package-submission` (기존 택배 제출 대상 재사용)
     - 요구 아이템: `handy_clock` ×1
     - 완료 신호: `tutorial.crafting.clock.submitted`

  8. 제출 완료 다이얼로그
     - 발화자: "모자"
     - 텍스트: "멋진 시계네요! 감사합니다!"

  - 아이템 정의 (코드 등록만, 리소스 미포함):

    | 식별자 | 표시 이름 | 스택 | 최대 |
    |---|---|---|---|
    | `tin_ingot` | 주석 주괴 | O | 64 |
    | `small_gear` | 소형 톱니 | O | 64 |
    | `small_chain` | 소형 사슬 | O | 64 |
    | `handy_clock` | 시계 | X | 1 |
