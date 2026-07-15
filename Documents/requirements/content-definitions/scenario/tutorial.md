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
      - 퀘스트
        - 텍스트 콘텐츠 "택배 가져오기"
        - 완료조건
          1. waypoint `delivery-storage-spot`으로 이동
            - 표시: "택배 보관소로 가기"
          2. 택배 획득(StaticPlacedObject) 시 아이템 `tutorial_delivery_package` 획득
            - 표시: "택배 획득하기"
          3. `tutorial-guide-hat`에게 아이템 `tutorial_delivery_package` 제출
            - 표시: "모자에게 택배 전달하기"
