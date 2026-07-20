---
title: "시나리오 튜토리얼"
doc_type: requirement
domain: content-definitions
progress: "1-designed"
status: active
updated: 2026-07-19
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
  -> [TUT-FLOW-2: 0.5초 지연 계약]
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
  -> V_TUT_DELIVERY_WAYPOINT_REACHED
  -> D_TUT_PACKAGE_PROMPT
  -> V_TUT_DELIVERY_PACKAGE_ACQUIRED
  -> V_TUT_DELIVERY_PACKAGE_SUBMITTED
  -> Q_TUT_DELIVERY_REMOVE
  -> D_TUT_DELIVERY_COMPLETE
  -> TUT_END
```

- `TUT_START`는 이 문서의 시작 노드 identifier로 사용한다. `TUT_END`만
  `nextIdentifier: null`인 종료 노드로 둔다.
- 이동 완료 후 0.5초 지연은 아직 노드 identifier로 고정하지 않는다. `D_TUT_CALL`의
  `autoAdvanceSeconds`는 표시 후 자동 진행 시간이지 표시 전 지연이 아니므로 사용할 수
  없다. **현재 스키마에 Delay node가 없으면 인간 판단 항목 TUT-FLOW-2의 handler 방식으로
  처리한다.**
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
| TUT-START-1 | `TUT_START` 이전 | 플레이어 spawn/소유권, 튜토리얼 씬 로드 완료, NPC·waypoint·StaticPlacedObject 배치의 선행 조건이 없다. | **인간 판단 필요:** 튜토리얼 전용 scene/preset 및 Scenario 시작 시점(씬 로드 후)을 확정한다. 시작 requirements에 `npc-tutorial-guide-hat`, `delivery-storage-spot`, 택배 object를 선언한다. |
| TUT-SIG-1 | `V_TUT_PLAYER_MOVED` | “WASD와 마우스 입력 또는 이동”은 서로 다른 완료 의미이며 현재 signal identifier/producer가 없다. | **인간 판단 필요:** 실제 위치 변위만 통과로 할지, 이동+시점 회전 모두 요구할지 확정한다. 확정 뒤 `tutorial_player_moved` producer를 PlayerController에 연결한다. |
| TUT-SIG-2 | 모자 상호작용 | NPC identifier와 interaction identifier는 있지만, 해당 interaction이 runtime signal을 Raise한다는 계약이 없다. | `npc-tutorial-guide-hat__interaction-talk-start -> tutorial_hat_talk_start` producer를 NPC prefab에 배선하고 Requirements Supports로 검증한다. |
| TUT-FLOW-1 | “Title 발생” | Title의 UI 종류, 지속시간, 닫는 조건, 그래프 전이 여부가 정의되지 않았다. | **인간 판단 필요:** 단순 안내 UI라면 event `show_tutorial_interaction_hint`를 `Q_TUT_FIND_HAT_ADD` 직후 InvokeEvent로 추가한다. 진행을 막는 UI라면 완료 signal과 Validator를 별도 정의한다. |
| TUT-FLOW-2 | 이동 완료 후 0.5초 | 현재 Scenario 스키마에는 일반 Delay node가 명시되어 있지 않아, 서술 그대로의 0.5초 지연을 직렬 노드로 저장할 수 없다. | **인간 판단 필요:** (a) delayed event handler가 `tutorial_move_intro_finished` signal을 Raise하게 하거나, (b) Scenario에 Delay node를 추가할지 결정한다. 그 전에는 임의의 Dialogue 자동 진행 시간으로 치환하지 않는다. |
| TUT-QUEST-1 | `Q_TUT_MOVE_ADD`, `Q_TUT_FIND_HAT_ADD`, `Q_TUT_DELIVERY_ADD` | 세 quest에 title/content/task definition 및 Add/Remove의 공통 definition identifier가 없다. | `tutorial-move`, `tutorial-find-hat`, `tutorial-quest-delivery`의 별도 quest definition을 작성한다. delivery는 본문에 적힌 3개 task와 순서를 보존한다. |
| TUT-SIG-3 | delivery waypoint/획득/제출 | `delivery-storage-spot` 도착, `tutorial_delivery_package` pickup, 모자에게 submit 모두 producer가 없고, quest task 완료가 Scenario signal을 Raise한다는 계약도 없다. | waypoint tracker, StaticPlacedObject pickup, NPC item-submit callback이 각각 `tutorial_delivery_waypoint_reached`, `tutorial_delivery_package_acquired`, `tutorial_delivery_package_submitted`를 Raise하도록 구현/배선한다. |
| TUT-ASSET-1 | 택배 및 오답 아이템 | 택배 StaticPlacedObject의 scene object identifier·spawn 위치·item definition이 없고, “여러 개” 오답 아이템의 목록/독백 트리거도 미정이다. | 정답 object와 item definition은 필수로 확정한다. 오답 아이템은 **인간 콘텐츠 판단 필요:** 목록과 각 1회성 signal/독백을 정하거나, 첫 JSON 범위에서 제외한다고 명시한다. |
| TUT-END-1 | 부분 3 이후 | 감사 인사 뒤 화면 전환, 다음 Scenario, 종료 메시지 중 어느 것도 없다. | **인간 판단 필요:** 다음 Scenario identifier 또는 튜토리얼 종료 UX를 확정한다. 확정 전에는 `D_TUT_DELIVERY_COMPLETE -> TUT_END`로만 종료하고, fade/전환을 이미 존재한다고 가정하지 않는다. |

### 변환 승인 조건

- TUT-START-1의 scene/preset 및 필수 배치물 requirement가 해소되어야 한다.
- TUT-SIG-1~3의 signal producer가 실제 gameplay callback에 배선되어야 한다.
- TUT-QUEST-1의 세 quest definition과 task 완료 계약이 등록되어야 한다.
- TUT-FLOW-1/2 및 TUT-END-1의 인간 판단이 확정되어야 한다.
- 변환 후 Requirements Supports Production 검증에서 unresolved `Error`가 0개여야 한다.

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
