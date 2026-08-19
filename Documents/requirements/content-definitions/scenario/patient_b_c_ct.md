---
title: "scenario 환자 B/C 지연 처치"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-08-07
flags: ["refactor-required"]
---

# scenario 환자 B/C 지연 처치

## 줄글 시나리오

재난 현장에 환자 B, 환자 C, 그리고 분류를 위한 더미 환자 1명이 이송되어 온다. 세 환자는 먼저 트리아지 구역에 도착하고, 간호사들은 환자의 상태를 확인할 준비를 한다.
- 기술 노트
  1. 트리아지 구역에 환자 B, C, 더미 D를 각각 스폰한다. 각자 고유의 point를 갖는다.
    - `scen_b:patient_spawnpoint_b`, `scen_b:patient_spawnpoint_c`, `scen_b:patient_spawnpoint_dummy_d_a`
  2. 의사 NPC를 의사 전용 스폰 지점에 스폰한다.
    - NPC 식별자: `npc-doctor-patient-b-c-ct`
    - 스폰 지점 식별자: `scen_b:doctor_spawnpoint`
    - `npc_doctor_preset`을 위 식별자 지점의 위치에 스폰한다.
  3. (스폰이 완료되면 시작):
    - DisinteractableDialogue
      - Speaker: "구내방송"
      - Content: "트리아지 구역에 응급 환자 세명 이송. 담당자는 지금 바로 와주세요."
      - TTS: true
  4. (3 항목과 동시에 시작) 기술 노트: 간호사들이 트리아지 구역으로 이동하도록 퀘스트를 발행한다.
    - 제목: "환자 도착"
    - 목표
      - 표기: "트리아지 구역에 도착한 환자 확인"
      - 처리: waypoint 도달을 만족하면 완료 처리되는 퀘스트 발행
        - `scen_b:quest_arrival_triage_area`

플레이어들이 트리아지 구역에 도착하면 다음과 같이 처리:
1. `nurse_a` 역할인 인물
  1. 퀘스트 발행(*1)
    - 제목: "환자 분류"
    - 목표
      - 표기: "세 명의 환자의 중증도를 모두 분류하기"
      - 처리: 스폰한 환자 엔티티 셋을 모두 중증도 분류 완료
  2. 위 1번의 퀘스트 완료 시
    환자 엔티티 셋 각각의 중증도 분류가 정답 값인지 확인
    - 아니라면 아래의 흐름 실행
      1. DisinteractableDialogue
         - Speaker: null
         - Content: "(중증도가 잘못 분류된 것 같다. 다시 분류하자..)"
         - TTS: false
      2. 1(*1)의 퀘스트 진행 상황을 초기화하고 퀘스트 재발행
    - 정답값이라면 그 외 `nurse_*` 역할인 플레이어들에게 완료 상황을 시그널하고 함께 다음으로 진행
2. 그 외 `nurse_*` 역할인 인물
  1. 퀘스트 발행
    - 제목: "환자 분류"
    - 목표
      - 표기: "환자 분류 담당이 중증도를 모두 분류할 때까지 기다리기"
      - 목표: 없음. 중증도 분류 성공 시그널 발생 시 완료 처리
- 기술노트: 다른 역할인 인물의 중증도 분류를 막지는 않는다. 버그나 예기치못한 동작으로 `nurse_a`가 아닌 인물들이 대신 처리해야하는 상황이 있을 수도 있으므로.
- 중증도 분류 정답값:
  - 환자 B: ktas2, 긴급
  - 환자 C: ktas2, 긴급
  - 더미 환자: ktas5, 비응급

환자 이동시키기
- 전체 인물을 상대로 퀘스트 발행
- 제목: "환자 이동"
- 목표
  - 표기: "KTAS 2로 분류된 환자 두 명을 처치 구역으로 이동시키기"
  - 처리: 환자 B, C를 처치 구역으로 이동시키면 완료 처리
    - 기술 노트: zone구현이 있는지, waypoint로 처리해야하는지는 확인해보아야 함
    - zone이 있다면 zone 구현을 따르기, 다만 관련하여 대비된 것이 없으므로 OverworldInitializer에 내용추가해두어야 함
    - `scen_b:care_area_waypoint`

위 퀘스트 완료 시 (*2) 내용 시작

- 의사 NPC `npc-doctor-patient-b-c-ct`를 환자 처치 구역의 의사 위치로 이동시킨다.
  - 목적지 waypoint 식별자: `scen_b:doctor_care_area_waypoint`
  - 의사 NPC가 해당 식별자 지점에 도착한 뒤 환자 B 처치 지시와 역할별 처치를 시작한다.

### 환자 B 처치

(*2)

환자 B 처치하기
- `nurse_a`에게 퀘스트 발행
  - 제목: "남성 환자 상태 확인"
  - 목표
    - 표기: "남성 환자의 의식 상태를 사정하기"
    - 처리: 환자 B의 의식 상태를 사정하면 완료 처리.
      - 서브목표 1: "남성 환자에게 말 걸기"
        - 기술 노트: 이 목표는 두 가지 방법으로 완료 가능하도록 함
          - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
            - 이 인터렉션이 활성화되면 다음 재생
              - DisinteractableDialogue
                - Speaker: (플레이어 이름)
                - Content: "남성분..! 말씀 들리세요?"
                - TTS: false
              - DisinteractableDialogue
                - Speaker: "남성 환자"
                - Content: "으으.. 아.. 어디지..?"
                - TTS: true
              - DisinteractableDialogue
                - Speaker: null
                - Content: (환자는 잠시 눈을 뜨더니 다시 눈을 감는다.)
                - TTS: false
            - 이후 이 목표 완료 처리
          - 마이크 사용하여 실제로 볼륨을 넣기 (구현을 추가해야함: 구현 추가시 마이크 권한을 요구하는 os 환경에 대해서 대응하여야 함. 이 때 타이틀 등에서 사전에 마이크 권한을 request하기)
            - 볼륨이 1초 이상 일정치를 넘으면 다음 재생
              - DisinteractableDialogue
                - Speaker: "남성 환자"
                - Content: "으으.. 아.. 어디지..?"
                - TTS: true
              - DisinteractableDialogue
                - Speaker: null
                - Content: (환자는 잠시 눈을 뜨더니 다시 눈을 감는다.)
                - TTS: false
            - 이후 이 목표 완료 처리
        - 이에 관해서 설정 추가:
          - `/gamerule UseMicInRecognitionCheck true` (default: false)
            - true로 설정하면 마이크를 사용하여 환자에게 말을 걸어 의식 상태를 확인할 수 있음
          - `/gamerule DisableInteractionInRecognitionCheck false` (default: false)
            - true로 설정하면 Interaction을 사용하여 환자에게 말을 걸어 의식 상태를 확인하는 것이 불가능함. 마이크로만 의식 상태를 확인할 수 있음.
          - `UseMicInRecognitionCheck=false && DisableInteractionInRecognitionCheck=true`이면 두 값이 위와 같이 설정되는 것은 불가능하다는 오류를 발생시키고 직전의 게임룰 변경 시도를 무시함. (즉, 두 값이 동시에 (usemic, disableinteract)=(false, true)가 되도록 설정할 수 없음)
        - 이 게임룰 값을 데이터팩에서도 설정 가능함. `usability` 데이터팩에 (usemic, disableinteract)=(true, false)로 설정
        - 기술노트: (후순위) 마이크 구현이 마무리된 후 이후에 고도화 작업에서는 마이크 사용 중에는 마이크 아이콘 띄우기 추가할 것
      - 서브목표 2: "남성 환자에게 계속해서 말 걸어보기"
        - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
          - 이 인터렉션이 활성화되면 다음 재생
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "환자분..!"
              - TTS: false
            - DisinteractableDialogue
              - Speaker: "남성 환자"
              - Content: "으으으..! 아..! 왜요..!!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 말에 조금 늦게 반응하고 있다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
        - 마이크 사용하여 실제로 볼륨을 넣기
          - 볼륨이 1초 이상 일정치를 넘으면 다음 재생
            - DisinteractableDialogue
              - Speaker: "남성 환자"
              - Content: "으으으..! 아..! 왜요..!!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 말에 조금 늦게 반응하고 있다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
      - 서브 목표 3: "계속해서 남자의 상태 확인하기"
         - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
          - 이 인터렉션이 활성화되면 다음 재생
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "여기가 어딘지 아시겠어요?"
              - TTS: false
            - DisinteractableDialogue
              - Speaker: "남성 환자"
              - Content: "으으악...! 과장님 제가 분명...!!! 기안 올린거 처리해달라고..!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "..."
              - TTS: false
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 무슨 일이 있었는지 기억을 못하는 것 같다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
        - 마이크 사용하여 실제로 볼륨을 넣기
          - 볼륨이 1초 이상 일정치를 넘으면 다음 재생
            - DisinteractableDialogue
              - Speaker: "남성 환자"
              - Content: "으으악...! 과장님 제가 분명...!!! 기안 올린거 처리해달라고..!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "..."
              - TTS: false
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 무슨 일이 있었는지 기억을 못하는 것 같다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
      - 서브 목표 4: "계속해서 남자의 상태 확인하기"
        - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
          - 이 인터렉션이 활성화되면 다음 재생
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "환자분..! 환자분!! 다리 한번 펴보실래요?"
              - TTS: false
            - DisinteractableDialogue
              - Speaker: "남성 환자"
              - Content: "으으으...! 날 가만히 둬..!!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(불평을 하면서도 환자는 다리를 움직인다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
      - 서브 목표를 차례로 완료 시 이 퀘스트를 완료 처리하고 다음의 퀘스트 발행
  - 제목: "남성 환자 상태 확인"
    - 목표
      - 표기: "남성 환자의 상태 정리하기"
      - 처리: 없음. 다음에 이어지는 다이얼로그를 완료하면 목표 달성 처리
    1. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "이 환자의 상태를 정리해보자."
    2. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 AVPU는..."
      - Choices
        - "AVPU A"
          - "아니야, 이 환자는 정확하게 답변을 하지 못하고 있어. V로 분류해야해."
        - "AVPU P"
          - "아니야, 이 환자는 대답도 하고 있어. V로 분류해야해."
        - "AVPU U"
          - "아니야, 이 환자는 대답도 하고 있어. V로 분류해야해."
        - "AVPU V"
          - "질문에 대답은 하지만 정확한 답변을 하지 못하고 있으니 V로 분류하자."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(3.)으로 이동
    3. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 GCS E는..."
      - Choices:
        - "E 4"
          - "아니야, 이 환자는 소리에 반응하고 있어. E 3으로 분류해야해."
        - "E 3"
          - "맞아, 이 환자는 소리에 반응하고 있어. E 3으로 분류하자."
        - "E 2"
          - "아니야, 이 환자는 소리에 반응하고 있어. E 3으로 분류해야해."
        - "E 1"
          - "아니야, 이 환자는 소리에 반응하고 있어. E 3으로 분류해야해."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(4.)으로 이동
    4. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 GCS V는..."
      - Choices:
        - "V 5"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
        - "V 4"
          - "맞아, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류하자."
        - "V 3"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
        - "V 2"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
        - "V 1"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(5.)으로 이동
    5. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 GCS M은..."
      - Choices:
        - "M 6"
          - "맞아, 이 환자는 지시를 따라주었어. M 6으로 분류하자."
        - "M 5"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 4"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 3"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 2"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 1"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(6.)으로 이동
    6. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "E는 3, V는 4.. M은 6이었으니까.."
    7. Dialouge
      - Speaker: (플레이어 이름)
      - Content: "E3 / V4 / M6, GCS 13점입니다."
    8. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "근력은 어떻지..?"
    9. 퀘스트 발행
      - 제목: "남성 환자 근력 확인"
      - 목표
        - 표기: "남성 환자의 근력을 확인하기"
        - 처리: 환자 B 근력 확인 인터렉션을 완료하면 완료 처리
          - 퀘스트 발행 시 환자 B의 근력 확인 인터렉션 활성화
          - "근력 확인" Interaction
            - 이 인터렉션이 활성화되면 다음 재생
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "환자분 오른쪽 다리 한번 들어보세요."
              - ChoiceDialogue
                - Speaker: "남성 환자"
                - Content: "..으 (오른쪽 다리를 들어올린다.)"
                - Choices(1개)
                  - Content: "(환자의 다리를 누른다.)"
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "눌리지 않는다."
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "환자분 이번엔 왼쪽 다리 한번 들어보세요."
              - ChoiceDialogue
                - Speaker: "남성 환자"
                - Content: "... (왼쪽 다리를 들어올린다.)"
                - Choices(1개)
                  - Content: "(환자의 다리를 누른다.)"
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "환자의 다리가 맥없이 눌린다."
            - 여기까지 진행되면 퀘스트 완료 처리
      - 퀘스트 완료시 10번으로 진행
    10. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 좌측 근력은"
      - Choices:
        - "5점입니다": 오답 노드로 진행
        - "4점입니다": 오답 노드로 진행
        - "3점입니다": 정답 노드로 진행
        - "2점입니다": 오답 노드로 진행
        - "1점입니다": 오답 노드로 진행
      - 다음 노드:
        - 오답 노드:
          - Dialogue
            - Speaker: (nurse_c 태그를 갖는 플레이어명, fallback: "???")
            - Content: "방금 이쪽 다리 그냥 눌리지 않았나요? 좌측은 3점으로 고치죠."
        - 정답 노드:
          - Dialogue
            - Speaker: (플레이어 이름)
            - Content: "(3점으로 기록했다.)"
      - 기술 노트: nurse_c 태그를 가져오는 로직 관련 구현
        - 아래의 로직이 필요함
          - 태그를 기준으로 플레이어 이름을 쿼리
          - 쿼리한 플레이어 이름이 있으면 그 이름을 Speaker에 표시
        - 이것을 다음과 같은 지정자로 표현
          - 위 사례에서는 이 값으로 사용 `@t=[nurse_c, ???]`, ???는 지정자 문자 `@`를 갖지 않으므로 string으로 fallback 처리되어야 함
          - 이 지정자는 `@t=[태그명, fallback(string 혹은 다른 지정자)]` 형식으로 구성된다. 태그명을 기준으로 대상을 쿼리하고, 없으면 fallback string 을 사용한다.
          - 예: `/tp @t=[jumper, @a] ~ ~100 ~` -- jumper 태그를 가진 플레이어가 있으면 그 플레이어를, 없으면 모든 플레이어를 공중으로 이동시킴
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
    11. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 우측 근력은"
      - Choices:
        - "5점입니다": 정답 노드로 진행
        - "4점입니다": 오답 노드로 진행
        - "3점입니다": 오답 노드로 진행
        - "2점입니다": 오답 노드로 진행
        - "1점입니다": 오답 노드로 진행
      - 다음 노드:
        - 오답 노드:
          1. Dialogue
            - Speaker: (nurse_c 태그를 갖는 플레이어명, fallback: "???")
            - Content: "오른쪽 다리는 정상 근력인것 같은데.."
            - 이후 정답 노드로 이동
        - 정답 노드:
          1. Dialogue
            - Speaker: (플레이어 이름)
            - Content: "(5점으로 기록했다.)"
    12. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "이 환자 GCS 13점, Motor Grade 우측 5, 좌측3 입니다."
    - 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경
- `nurse_b`에게 퀘스트 발행
  - 제목: "남성 환자 상태 확인"
  - 목표
    1. 서브목표 1
      - 표기: "남성 환자의 활력징후를 환자 모니터를 통해 확인하기"
      - `nurse_b` 플레이어가 환자 B가 위치한 CareZone에 있는 환자모니터에서 자세히 보기 상호작용 수행
    2. 서브목표 2
      - 표기: "남성 환자의 활력징후를 보고하기"
      - 처리: 아래의 다이얼로그를 모두 끝내면 완료 처리
      1. ChoiceDialogue 발생: 서브목표1에서 열었던 환자 모니터 자세히 보기를 닫으면 발생(`nurse_b`에 대해서, 시그널 송수신 관계로 처리하면 될 것)
        - Speaker: (플레이어 이름)
        - Content: "이 환자는.."
        - Choices:
          - "호흡 수 분당 20회, 맥박 분당 100회": 오답 노드로 진행
          - "호흡 수 분당 20회, 맥박 분당 120회": 오답 노드로 진행
          - "호흡 수 분당 24회, 맥박 분당 120회": 정답 노드로 진행
          - "호흡 수 분당 24회, 맥박 분당 140회": 오답 노드로 진행
          - "호흡 수 분당 30회, 맥박 분당 140회": 오답 노드로 진행
        - 다음 노드:
          - 오답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(호흡수 분당 24회, 맥박 분당 120회였어.)"
              - 이후 정답 노드로 이동
          - 정답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(호흡수 분당 24회, 맥박 분당 120회로 보고하고 기록했다.)"
              - 이후 다음 노드로 이동
      2. ChoiceDialogue
        - Speaker: (플레이어 이름)
        - Content: "혈압은..."
        - Choices:
          - "혈압 120/80mmHg": 오답 노드로 진행
          - "혈압 130/85mmHg": 오답 노드로 진행
          - "혈압 140/86mmHg": 정답 노드로 진행
          - "혈압 150/90mmHg": 오답 노드로 진행
          - "혈압 160/100mmHg": 오답 노드로 진행
        - 다음 노드:
          - 오답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(혈압은 140/86mmHg였어.)"
              - 이후 정답 노드로 이동
          - 정답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(혈압 140/86mmHg로 보고하고 기록했다.)"
              - 이후 다음 노드로 이동
      3. ChoiceDialogue
        - Speaker: (플레이어 이름)
        - Content: "체온은..."
        - Choices:
          - "체온 37.3도, 산소포화도 90%": 오답 노드로 진행
          - "체온 37.3도, 산소포화도 93%": 오답 노드로 진행
          - "체온 37.8도, 산소포화도 93%": 정답 노드로 진행
          - "체온 38.0도, 산소포화도 95%": 오답 노드로 진행
          - "체온 38.5도, 산소포화도 95%": 오답 노드로 진행
        - 다음 노드:
          - 오답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(체온은 37.8도, 산소포화도는 93%였어.)"
              - 이후 정답 노드로 이동
          - 정답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(체온 37.8도, 산소포화도 93%로 보고하고 기록했다.)"
              - 이후 다음 노드로 이동
  - 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경
- `nurse_c`에게 퀘스트 발행
  - 제목: "남성 환자 상태 확인"
  - 목표: ""
  - 퀘스트 발행과 함께 다음 처리 수행:
    1. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_c, @s]선생님, 이 환자 동공반사 확인하고 N/S로 IV 확보해 주세요."
      - 목표 표기를 변경 "남성 환자의 동공반사 확인하기"로 변경
    2. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_d, ???]선생님, 산포도가 낮으니 비강캐뉼라로 3L 주시고 지혈해 주세요."
    3. "동공반사 확인" Interaction 활성화
      - Interaction 활성화 시 다음 재생
        1. Dialogue
          - Speaker: (플레이어 이름)
          - Content: "(펜라이트를 환자의 양쪽 눈에 비춘다.)"
        2. Dialogue
          - Speaker: (플레이어 이름)
          - Content: "(이 환자의 좌측 동공이 빛에 반응하지 않는다.)"
        3. Dialogue
          - Speaker: (플레이어 이름)
          - Content: "(이 환자의 우측 동공은 빛에 반응한다.)"
        4. 퀘스트 "남성 환자의 동공반사 확인하기" 목표를 완료처리
        5. 퀘스트 목표 표기를 "남성 환자의 정맥로 확보하기"로 변경
        6. "정맥로 확보" Interaction 활성화
          - Interaction 활성화 시 다음 재생
            - 인벤토리에 `cannula_20g` 아이템이 있는지 확인
              - 없다면 다음 재생
                1. Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(정맥로 확보에 사용할 20게이지 캐뉼라를 갖고 있지 않다.)"
                2. Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(20게이지 캐뉼라를 찾자.)"
              - 있다면 시스템이 1단계(20G 캐뉼라 삽입)를 처리한 뒤 2단계로 진행
        7. 1단계 — 환자에게 20G 캐뉼라 삽입(시스템 처리)
          - 플레이어 인벤토리의 `cannula_20g`를 1개 소모
          - 환자 Display State Descriptor에서 우측 팔 정맥로 확보 상태를 표시하는 오브젝트와 상태 플래그를 활성화
          - 환자 상태값에 20G 캐뉼라가 삽입되었다는(정맥로가 확보되었다는) 상태 플래그 활성화
          - 이 시점에는 IV Line을 연결하지 않고, 2단계의 별도 Interaction에서 연결하는 구조로 변경
          - 퀘스트 목표 표기를 "남성 환자에게 생리식염수 연결하기"로 변경
          - Dialogue 재생
            - Speaker: (플레이어 이름)
            - Content: "(환자에게 생리식염수를 연결해두자.)"
        8. 2단계 — 삽입한 20G 캐뉼라와 생리식염수(N/S) 연결
          - "생리식염수 연결" Interaction을 추가(가시화). 다음 두 조건이 모두 충족되었을 때만 노출
            - 1단계(20G 캐뉼라 삽입)가 완료되어 있을 것
            - 환자가 누워있는 침대의 Attachment가 활성화되어 있고, 그 Attachment에 생리식염수가 적용되어 있을 것
              - 생리식염수 적용 상태는 Display 플래그와 상태 플래그가 모두 활성화되어 있어야 하나, 활성화 여부 판정은 상태 플래그를 기준으로 함
              - 기술 노트: 침대에 생리식염수를 거는 동작은 침대 Attachment의 기존 "N/S 수액 걸기" Interaction을 그대로 사용
          - Interaction 수행 시 다음 처리
            - 환자가 누워있는 침대 Attachment의 생리식염수 오브젝트 자식에 있는 Intravenous Line Connection Point 오브젝트와, 환자의 우측 팔 정맥로의 Intravenous Line Connection Point 오브젝트를 IV Line 연결 처리
              - 환자 측 연결 포인트의 식별자는 `patient_b:iv_point_vein`으로 지정. 포인트가 여러 개 존재할 때를 대비해 Point 클래스로 자식 오브젝트들을 쿼리한 뒤 identifier가 일치하는 포인트를 찾아 사용
          - 기술 노트: 이와 관련한 로직을 미리 구현하고 그래프 노드로 연결시키기
          - 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경
- `nurse_d`에게 퀘스트 발행
  - 제목: "남성 환자 상태 확인"
  - 목표: ""
  - 퀘스트 발행과 함께 다음 처리 수행:
    1. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_c, ???]선생님, 이 환자 동공반사 확인하고 N/S로 IV 확보해 주세요."
    2. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_d, @s]선생님, 산포도가 낮으니 비강 캐뉼라로 3L 주시고 지혈해 주세요."
      - 퀘스트 목표 표기를 "남성 환자에게 산소 공급하기"로 변경
    4. "비강 캐뉼라 적용" Interaction 활성화
      - Interaction 활성화 시 다음 재생
        - 인벤토리에 `nasal_cannula` 아이템이 있는지 확인
          - 없다면 다음 재생
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(비강 캐뉼라를 갖고 있지 않다.)"
            2. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(비강 캐뉼라를 찾자.)"
          - 있다면 다음 처리
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(비강 캐뉼라를 환자에게 적용했다.)"
            2. 조건 분기처리
              - 만약 oxy 오브젝트나 플래그값이 비활성화 상태라면
                - Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(산소 공급 장치가 연결되어 있지 않다.)"
                - Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(산소 공급 장치를 찾아 연결하자.)"
              - 만약 oxy 오브젝트나 플래그값이 활성화 상태라면
                - 환자 Display State Descriptor에서 산소 공급 장치 오브젝트를 참조, 상태 플래그를 활성화
                  - 기술 노트: 오브젝트가 Display되었다는 플래그와 산소 공급 장치가 연결되었다는 플래그를 분리해 고려, 관려해야함
                - 환자가 위치한 CareZone의 flowmeter 오브젝트를 참조, 이 오브젝트의 하위에 위치한 Oxy connection point 오브젝트를 참조하여 환자의 oxy connection point와 연결되도록 처리
              - 기술 노트: 이와 관련한 로직을 미리 구현하고 그래프 노드로 연결시키기
            - 이후 퀘스트의 목표를 완료처리
            - Dialogue
              - Speaker: (플레이어 이름)
              - Content: "산소 넣었습니다."
            - 퀘스트 목표 표기를 "남성 환자 지혈하기"로 변경
    5. "지혈" Interaction 활성화
      - Interaction 활성화 시 다음 재생
        - 인벤토리에 `gauze` 아이템이 있는지 확인
          - 없다면 다음 재생
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(거즈와 플라스터를 갖고 있지 않다.)"
            2. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(거즈와 플라스터를 찾자.)"
          - 있다면 다음 처리
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(거즈를 출혈 부위에 대고 압박 지혈을 시행했다.)"
            2. 1초 지연
            3. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(출혈이 멎어, 거즈 위에 플라스터를 붙였다.)"
            3. 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경

- 전체 인원의 퀘스트가 "퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기" 상태(*a)라면 퀘스트 완료처리
- 기술 노트: 디버그 편의를 위해 시작할 때 nurse_* 태그를 가진 플레이어가 몇 명인지 파악해두기, 인원수만큼 퀘스트 목표가 (*a) 상태가 되면 퀘스트 완료 처리하도록 구현

## 2026-08-02 데이터화 확정 사항

### 이번 변환 범위

- 데이터 정본은 `## 줄글 시나리오` 시작부터 `### 환자 C 처치` 본문 종료까지이다.
- `### 환자 C 처치`는 환자 B 처치 서브그래프를 복제하고 환자·퀘스트·신호·문구·좌우 평가 파라미터만 C 값으로 바꾼다.
- `### CT실 이송`은 환자 C 간호 중재 완료 후 이어서 진행한다.
- `patient_b_c_ct.scenario.json`은 환자 B/C의 CT실 도착 확인 및 완료 안내까지 포함하며 284개 노드로 구성한다.

### 본문 우선 해석과 이전 버전 사용 결과

- 환자 B는 현재 본문의 “남성 환자”를 정본으로 삼아 `sex=Male`로 설정한다. 이전 버전의 `Female` 값은 모순되므로 무시한다.
- 환자 B 동공은 현재 본문대로 좌측 무반응, 우측 반응으로 확정한다. 반대 방향을 적은 이전 기록은 무시한다.
- 환자 B 20G 정맥로의 팔은 현재 본문에 명시되지 않아 이전 버전과 기존 시각물 명세를 참고하여 오른팔로 확정한다.
- 환자 B 활력은 GCS 13(E3/V4/M6), RR 24, HR 120, BP 140/86mmHg, BT 37.8℃, SpO2 93%로 확정한다.
- 환자 C 활력과 GCS는 환자 B와 동일하며, 근력은 현재 C 본문대로 좌측 5점·우측 3점으로 바꾼다.
- 환자 C 동공은 현재 본문과 이전 데이터가 일치하는 좌측 무반응·우측 반응으로 확정한다.
- 환자 C 20G 정맥로는 현재 본문과 `PatientTypeBFemale` 시각물이 일치하는 좌측 팔로 확정한다. 지혈 부위는 현재 본문에 좌우가 없으므로 프리팹이 지원하는 우측 팔 표시를 사용한다.
- 앞선 공통 이동 퀘스트가 환자 B와 C의 처치 구역 도착을 모두 확인하므로, C 본문의 “이제 환자 C를 처치 구역으로 이동”은 중복 이동 이벤트가 아니라 B 완료 후 C 처치로 전환하는 도착 안내로 해석한다.
- 분류용 엔티티는 `patient_dummy_d_b`이다. 현재 본문에 명시된 공간 식별자 `scen_b:patient_spawnpoint_dummy_d_a`는 이름의 `d_a`가 엔티티명과 다르지만 명시값이므로 그대로 사용한다.

### 선행 구현 완료 사항

- `Parallel(ByRole)`의 원격 역할 브랜치에서 Dialogue/Choice를 해당 클라이언트에 표시하고, `(clientId, graph, node)`가 일치하는 선택만 서버 브랜치에 반영한다.
- 병렬 브랜치 안의 Validator가 `Branching`/`FailBranch`를 브랜치 로컬 전이로 처리하도록 보완했다. 이에 따라 오분류 안내 → 진행 신호/퀘스트 초기화 → 재분류 루프를 데이터로 구성한다.
- `@t=[태그, fallback]`와 `@s` 텍스트 지정자를 구현했다. 태그 대상이 없으면 일반 문자열 또는 중첩 지정자인 fallback을 사용한다.
- Choice에 `assessmentIdentifier`, `correctOptionIndex`를 추가했다. 세션 로그에는 선택 답, 의도 답, 정답 여부가 함께 기록된다.
- 의식 확인은 활성 단계에만 “말 걸기” 상호작용을 노출한다. 1~3단계는 마이크 RMS 음량이 0.02 이상으로 1초간 유지되어도 완료되며, 4단계는 상호작용만 사용한다.
- 애플리케이션 시작 후 마이크 권한을 미리 요청한다. 마이크 장치/권한이 없으면 상호작용 경로를 유지한다.
- `/gamerule UseMicInRecognitionCheck`와 `/gamerule DisableInteractionInRecognitionCheck`를 추가했다. `(false, true)` 변경은 오류와 함께 거부되며 직전 값을 유지한다.
- `usability` 데이터팩은 `(UseMicInRecognitionCheck, DisableInteractionInRecognitionCheck)=(true, false)`를 적용한다.
- PlayerController가 시나리오 식별자를 노출하고, Overworld Initializer가 `scen_b:*` 스폰/도착 앵커와 `quest_arrival_triage_area_{id}` 도착 신호 존을 생성한다.
- EntityPresetSpawn은 일반 엔티티와 acting NPC 모두 `positionSourceEntityIdentifier`로 지정한 waypoint 위치를 해석한다. 이에 따라 의사 NPC를 전용 spawnpoint에 생성할 수 있다.
- 의사 NPC는 시나리오 시작 시 `scen_b:doctor_spawnpoint`에서 생성되고, 환자 B/C가 처치 구역에 배치되면 `NPCControl(mode=Control)`로 `scen_b:doctor_care_area_waypoint`까지 이동한다.
- 환자 C 처치 시작 시에도 환자 B와 동일하게 의사가 `nurse_c`에게 동공반사·정맥로 확보를, `nurse_d`에게 산소 공급·지혈을 지시한 뒤 역할별 처치를 시작한다.
- 처치 구역 도착 신호는 실제 zone 이름과 무관한 `carezone_patient_entered_patient_b/c`도 함께 발신한다. 그래프는 이 환자 범위 신호로 B/C 이동 완료를 판정한다.
- 환자 C 의식·근력·동공 확인 이벤트는 환자 B와 동일한 `nurse_a` 역할 검증과 마이크/상호작용 입력 경로를 사용하되, 완료 신호를 `patient_c_*`로 분리한다.
- 환자 C 활력 UI는 `invokeOnRoleClient=true`로 `nurse_b` 클라이언트에서 열고 `close_vital_ui_c`로 서버 진행을 재개한다.

### 데이터 연결 명세

| 구분 | 확정 식별자/신호 |
|---|---|
| 환자 스폰 위치 | `scen_b:patient_spawnpoint_b`, `scen_b:patient_spawnpoint_c`, `scen_b:patient_spawnpoint_dummy_d_a` |
| 간호사 도착 위치 | `scen_b:quest_arrival_triage_area` |
| 간호사 도착 계측 | `quest_arrival_triage_area_{player-id}` 4개 distinct |
| 의사 NPC | `npc-doctor-patient-b-c-ct` (`npc_doctor_preset`) |
| 의사 초기 스폰 위치 | `scen_b:doctor_spawnpoint` |
| 의사 환자 처치 위치 | `scen_b:doctor_care_area_waypoint` |
| 환자 처치구역 도착 | `carezone_patient_entered_patient_b`, `carezone_patient_entered_patient_c` |
| 환자 B 정맥로 | `insert_iv_patient_b_right`, `connect_cannula_and_ns1_patient_b` |
| 환자 B 산소/지혈 | `apply_nasal_cannula_patient_b`, `equipment_connected_oxyflowmeter_patient_b`, `apply_gauze_patient_b`, `apply_plaster_on_gauze_patient_b` |
| 환자 C 정맥로 | `insert_iv_patient_c_left`, `connect_cannula_and_ns1_patient_c` |
| 환자 C 산소/지혈 | `apply_nasal_cannula_patient_c`, `equipment_connected_oxyflowmeter_patient_c`, `apply_gauze_patient_c`, `apply_plaster_on_gauze_patient_c` |

### 에디터 설정 필요 사항

- Overworld Initializer의 기본 `scen_b:*` 위치는 안전한 미확정값 `(-1, -1, -1)`이다. 씬 담당자가 환자 위치와 함께 `scen_b:doctor_spawnpoint`, `scen_b:doctor_care_area_waypoint`의 실제 위치를 지정하고 **Set**을 실행해야 한다.
- 처치 구역의 기존 `PatientCareDescriptionZone`, 베드 스냅 포인트, oxyflowmeter가 실제 씬에 배치되어야 한다.
- 네 역할 태그 `nurse_a`~`nurse_d`가 모두 공급되지 않으면 `Panic` 정책에 따라 역할 병렬 처리를 시작하지 않는다.

### 환자 C 처치

(*3: 환자 B 처치 완료 후 시작)

환자 C 처치하기
- `nurse_a`에게 퀘스트 발행
  - 제목: "환자 의식 상태 확인"
  - 목표
    - 표기: "환자 C의 의식 상태를 사정하기"
    - 처리: 환자 C의 의식 상태를 사정하면 완료 처리.
      - 서브목표 1: "환자 C에게 말 걸기"
        - 기술 노트: 이 목표는 두 가지 방법으로 완료 가능하도록 함
          - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
            - 이 인터렉션이 활성화되면 다음 재생
              - DisinteractableDialogue
                - Speaker: (플레이어 이름)
                - Content: "환자분..! 말씀 들리세요?"
                - TTS: false
              - DisinteractableDialogue
                - Speaker: "환자 C"
                - Content: "으으.. 아.. 어디지..?"
                - TTS: true
              - DisinteractableDialogue
                - Speaker: null
                - Content: (환자는 잠시 눈을 뜨더니 다시 눈을 감는다.)
                - TTS: false
            - 이후 이 목표 완료 처리
          - 마이크 사용하여 실제로 볼륨을 넣기 (구현을 추가해야함: 구현 추가시 마이크 권한을 요구하는 os 환경에 대해서 대응하여야 함. 이 때 타이틀 등에서 사전에 마이크 권한을 request하기)
            - 볼륨이 1초 이상 일정치를 넘으면 다음 재생
              - DisinteractableDialogue
                - Speaker: "환자 C"
                - Content: "으으.. 아.. 어디지..?"
                - TTS: true
              - DisinteractableDialogue
                - Speaker: null
                - Content: (환자는 잠시 눈을 뜨더니 다시 눈을 감는다.)
                - TTS: false
            - 이후 이 목표 완료 처리
        - 이에 관해서 설정 추가:
          - `/gamerule UseMicInRecognitionCheck true` (default: false)
            - true로 설정하면 마이크를 사용하여 환자에게 말을 걸어 의식 상태를 확인할 수 있음
          - `/gamerule DisableInteractionInRecognitionCheck false` (default: false)
            - true로 설정하면 Interaction을 사용하여 환자에게 말을 걸어 의식 상태를 확인하는 것이 불가능함. 마이크로만 의식 상태를 확인할 수 있음.
          - `UseMicInRecognitionCheck=false && DisableInteractionInRecognitionCheck=true`이면 두 값이 위와 같이 설정되는 것은 불가능하다는 오류를 발생시키고 직전의 게임룰 변경 시도를 무시함. (즉, 두 값이 동시에 (usemic, disableinteract)=(false, true)가 되도록 설정할 수 없음)
        - 이 게임룰 값을 데이터팩에서도 설정 가능함. `usability` 데이터팩에 (usemic, disableinteract)=(true, false)로 설정
        - 기술노트: (후순위) 마이크 구현이 마무리된 후 이후에 고도화 작업에서는 마이크 사용 중에는 마이크 아이콘 띄우기 추가할 것
      - 서브목표 2: "환자 C에게 계속해서 말 걸어보기"
        - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
          - 이 인터렉션이 활성화되면 다음 재생
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "환자분..!"
              - TTS: false
            - DisinteractableDialogue
              - Speaker: "환자 C"
              - Content: "으으으..! 아..! 왜요..!!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 말에 조금 늦게 반응하고 있다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
        - 마이크 사용하여 실제로 볼륨을 넣기
          - 볼륨이 1초 이상 일정치를 넘으면 다음 재생
            - DisinteractableDialogue
              - Speaker: "환자 C"
              - Content: "으으으..! 아..! 왜요..!!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 말에 조금 늦게 반응하고 있다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
      - 서브 목표 3: "계속해서 환자 C의 상태 확인하기"
         - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
          - 이 인터렉션이 활성화되면 다음 재생
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "여기가 어딘지 아시겠어요?"
              - TTS: false
            - DisinteractableDialogue
              - Speaker: "환자 C"
              - Content: "으으악...! 과장님 제가 분명...!!! 기안 올린거 처리해달라고..!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "..."
              - TTS: false
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 무슨 일이 있었는지 기억을 못하는 것 같다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
        - 마이크 사용하여 실제로 볼륨을 넣기
          - 볼륨이 1초 이상 일정치를 넘으면 다음 재생
            - DisinteractableDialogue
              - Speaker: "환자 C"
              - Content: "으으악...! 과장님 제가 분명...!!! 기안 올린거 처리해달라고..!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "..."
              - TTS: false
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(이 환자는 무슨 일이 있었는지 기억을 못하는 것 같다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
      - 서브 목표 4: "계속해서 환자 C의 상태 확인하기"
        - "말 걸기" Interaction (이 서브목표가 주어졌을 때만 이 인터렉션 활성화하기)
          - 이 인터렉션이 활성화되면 다음 재생
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "환자분..! 환자분!! 다리 한번 펴보실래요?"
              - TTS: false
            - DisinteractableDialogue
              - Speaker: "환자 C"
              - Content: "으으으...! 날 가만히 둬..!!"
              - TTS: true
            - DisinteractableDialogue
              - Speaker: (플레이어 이름)
              - Content: "(불평을 하면서도 환자는 다리를 움직인다.)"
              - TTS: false
          - 이후 이 목표 완료 처리
      - 서브 목표를 차례로 완료 시 이 퀘스트를 완료 처리하고 다음의 퀘스트 발행
  - 제목: "환자 의식 상태 확인"
    - 목표
      - 표기: "환자 C의 상태 정리하기"
      - 처리: 없음. 다음에 이어지는 다이얼로그를 완료하면 목표 달성 처리
    1. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "이 환자의 상태를 정리해보자."
    2. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 AVPU는..."
      - Choices
        - "AVPU A"
          - "아니야, 이 환자는 정확하게 답변을 하지 못하고 있어. V로 분류해야해."
        - "AVPU P"
          - "아니야, 이 환자는 대답도 하고 있어. V로 분류해야해."
        - "AVPU U"
          - "아니야, 이 환자는 대답도 하고 있어. V로 분류해야해."
        - "AVPU V"
          - "질문에 대답은 하지만 정확한 답변을 하지 못하고 있으니 V로 분류하자."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(3.)으로 이동
    3. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 GCS E는..."
      - Choices:
        - "E 4"
          - "아니야, 이 환자는 소리에 반응하고 있어. E 3으로 분류해야해."
        - "E 3"
          - "맞아, 이 환자는 소리에 반응하고 있어. E 3으로 분류하자."
        - "E 2"
          - "아니야, 이 환자는 소리에 반응하고 있어. E 3으로 분류해야해."
        - "E 1"
          - "아니야, 이 환자는 소리에 반응하고 있어. E 3으로 분류해야해."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(4.)으로 이동
    4. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 GCS V는..."
      - Choices:
        - "V 5"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
        - "V 4"
          - "맞아, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류하자."
        - "V 3"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
        - "V 2"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
        - "V 1"
          - "아니야, 이 환자는 혼란스러운 대답을 하고 있어. V 4로 분류해야해."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(5.)으로 이동
    5. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 GCS M은..."
      - Choices:
        - "M 6"
          - "맞아, 이 환자는 지시를 따라주었어. M 6으로 분류하자."
        - "M 5"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 4"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 3"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 2"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
        - "M 1"
          - "아니야, 이 환자는 지시를 따라주었어. M 6으로 분류해야해."
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
      - 이후 모든 선택지 다음(6.)으로 이동
    6. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "E는 3, V는 4.. M은 6이었으니까.."
    7. Dialouge
      - Speaker: (플레이어 이름)
      - Content: "E3 / V4 / M6, GCS 13점입니다."
    8. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "근력은 어떻지..?"
    9. 퀘스트 발행
      - 제목: "환자 근력 확인"
      - 목표
        - 표기: "환자 C의 근력을 확인하기"
        - 처리: 환자 C 근력 확인 인터렉션을 완료하면 완료 처리
          - 퀘스트 발행 시 환자 C의 근력 확인 인터렉션 활성화
          - "근력 확인" Interaction
            - 이 인터렉션이 활성화되면 다음 재생
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "환자분 오른쪽 다리 한번 들어보세요."
              - ChoiceDialogue
                - Speaker: "환자 C"
                - Content: "..으 (오른쪽 다리를 들어올린다.)"
                - Choices(1개)
                  - Content: "(환자의 다리를 누른다.)"
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "환자의 다리가 맥없이 눌린다."
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "환자분 이번엔 왼쪽 다리 한번 들어보세요."
              - ChoiceDialogue
                - Speaker: "환자 C"
                - Content: "... (왼쪽 다리를 들어올린다.)"
                - Choices(1개)
                  - Content: "(환자의 다리를 누른다.)"
              - Dialogue
                - Speaker: (플레이어 이름)
                - Content: "눌리지 않는다."
            - 여기까지 진행되면 퀘스트 완료 처리
      - 퀘스트 완료시 10번으로 진행
    10. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 좌측 근력은"
      - Choices:
        - "5점입니다": 정답 노드로 진행
        - "4점입니다": 오답 노드로 진행
        - "3점입니다": 오답 노드로 진행
        - "2점입니다": 오답 노드로 진행
        - "1점입니다": 오답 노드로 진행
      - 다음 노드:
        - 오답 노드:
          - Dialogue
            - Speaker: (nurse_c 태그를 갖는 플레이어명, fallback: "???")
            - Content: "방금 이쪽 다리 그냥 눌리지 않았나요? 좌측은 5점으로 고치죠."
        - 정답 노드:
          - Dialogue
            - Speaker: (플레이어 이름)
            - Content: "(5점으로 기록했다.)"
      - 기술 노트: nurse_c 태그를 가져오는 로직 관련 구현
        - 아래의 로직이 필요함
          - 태그를 기준으로 플레이어 이름을 쿼리
          - 쿼리한 플레이어 이름이 있으면 그 이름을 Speaker에 표시
        - 이것을 다음과 같은 지정자로 표현
          - 위 사례에서는 이 값으로 사용 `@t=[nurse_c, ???]`, ???는 지정자 문자 `@`를 갖지 않으므로 string으로 fallback 처리되어야 함
          - 이 지정자는 `@t=[태그명, fallback(string 혹은 다른 지정자)]` 형식으로 구성된다. 태그명을 기준으로 대상을 쿼리하고, 없으면 fallback string 을 사용한다.
          - 예: `/tp @t=[jumper, @a] ~ ~100 ~` -- jumper 태그를 가진 플레이어가 있으면 그 플레이어를, 없으면 모든 플레이어를 공중으로 이동시킴
      - 기술 노트: 세션 로그의 다음의 내용 저장
        - 정답 여부
        - 플레이어가 선택한 답, 의도된 답
    11. ChoiceDialogue
      - Speaker: (플레이어 이름)
      - Content: "환자의 우측 근력은"
      - Choices:
        - "5점입니다": 오답 노드로 진행
        - "4점입니다": 오답 노드로 진행
        - "3점입니다": 정답 노드로 진행
        - "2점입니다": 오답 노드로 진행
        - "1점입니다": 오답 노드로 진행
      - 다음 노드:
        - 오답 노드:
          1. Dialogue
            - Speaker: (nurse_c 태그를 갖는 플레이어명, fallback: "???")
            - Content: "방금 이쪽 다리 그냥 눌리지 않았나요? 우측은 3점으로 고치죠."
            - 이후 정답 노드로 이동
        - 정답 노드:
          1. Dialogue
            - Speaker: (플레이어 이름)
            - Content: "(3점으로 기록했다.)"
    12. Dialogue
      - Speaker: (플레이어 이름)
      - Content: "이 환자 GCS 13점, Motor Grade 좌측 5, 우측 3 입니다."
    - 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경
- `nurse_b`에게 퀘스트 발행
  - 제목: "환자 활력징후 확인"
  - 목표
    1. 서브목표 1
      - 표기: "환자 C의 활력징후를 환자 모니터를 통해 확인하기"
      - `nurse_b` 플레이어가 환자 C가 위치한 CareZone에 있는 환자모니터에서 자세히 보기 상호작용 수행
    2. 서브목표 2
      - 표기: "환자 C의 활력징후를 보고하기"
      - 처리: 아래의 다이얼로그를 모두 끝내면 완료 처리
      1. ChoiceDialogue 발생: 서브목표1에서 열었던 환자 모니터 자세히 보기를 닫으면 발생(`nurse_b`에 대해서, 시그널 송수신 관계로 처리하면 될 것)
        - Speaker: (플레이어 이름)
        - Content: "이 환자는.."
        - Choices:
          - "호흡 수 분당 20회, 맥박 분당 100회": 오답 노드로 진행
          - "호흡 수 분당 20회, 맥박 분당 120회": 오답 노드로 진행
          - "호흡 수 분당 24회, 맥박 분당 120회": 정답 노드로 진행
          - "호흡 수 분당 24회, 맥박 분당 140회": 오답 노드로 진행
          - "호흡 수 분당 30회, 맥박 분당 140회": 오답 노드로 진행
        - 다음 노드:
          - 오답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(호흡수 분당 24회, 맥박 분당 120회였어.)"
              - 이후 정답 노드로 이동
          - 정답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(호흡수 분당 24회, 맥박 분당 120회로 보고하고 기록했다.)"
              - 이후 다음 노드로 이동
      2. ChoiceDialogue
        - Speaker: (플레이어 이름)
        - Content: "혈압은..."
        - Choices:
          - "혈압 120/80mmHg": 오답 노드로 진행
          - "혈압 130/85mmHg": 오답 노드로 진행
          - "혈압 140/86mmHg": 정답 노드로 진행
          - "혈압 150/90mmHg": 오답 노드로 진행
          - "혈압 160/100mmHg": 오답 노드로 진행
        - 다음 노드:
          - 오답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(혈압은 140/86mmHg였어.)"
              - 이후 정답 노드로 이동
          - 정답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(혈압 140/86mmHg로 보고하고 기록했다.)"
              - 이후 다음 노드로 이동
      3. ChoiceDialogue
        - Speaker: (플레이어 이름)
        - Content: "체온은..."
        - Choices:
          - "체온 37.3도, 산소포화도 90%": 오답 노드로 진행
          - "체온 37.3도, 산소포화도 93%": 오답 노드로 진행
          - "체온 37.8도, 산소포화도 93%": 정답 노드로 진행
          - "체온 38.0도, 산소포화도 95%": 오답 노드로 진행
          - "체온 38.5도, 산소포화도 95%": 오답 노드로 진행
        - 다음 노드:
          - 오답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(체온은 37.8도, 산소포화도는 93%였어.)"
              - 이후 정답 노드로 이동
          - 정답 노드:
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(체온 37.8도, 산소포화도 93%로 보고하고 기록했다.)"
              - 이후 다음 노드로 이동
  - 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경
- `nurse_c`에게 퀘스트 발행
  - 제목: "환자 동공반사 및 정맥로 확보"
  - 목표: ""
  - 퀘스트 발행과 함께 다음 처리 수행:
    1. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_c, @s]선생님, 이 환자 동공반사 확인하고 N/S로 IV 확보해 주세요."
      - 목표 표기를 변경 "환자 C의 동공반사 확인하기"로 변경
    2. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_d, ???]선생님, 산포도가 낮으니 비강캐뉼라로 3L 주시고 지혈해 주세요."
    3. "동공반사 확인" Interaction 활성화
      - Interaction 활성화 시 다음 재생
        1. Dialogue
          - Speaker: (플레이어 이름)
          - Content: "(펜라이트를 환자의 양쪽 눈에 비춘다.)"
        2. Dialogue
          - Speaker: (플레이어 이름)
          - Content: "(이 환자의 좌측 동공이 빛에 반응하지 않는다.)"
        3. Dialogue
          - Speaker: (플레이어 이름)
          - Content: "(이 환자의 우측 동공은 빛에 반응한다.)"
        4. 퀘스트 "환자 C의 동공반사 확인하기" 목표를 완료처리
        5. 퀘스트 목표 표기를 "환자 C의 좌측 팔 정맥로 확보하기"로 변경
        6. "좌측 팔 정맥로 확보" Interaction 활성화
          - Interaction 활성화 시 다음 재생
            - 인벤토리에 `cannula_20g` 아이템이 있는지 확인
              - 없다면 다음 재생
                1. Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(정맥로 확보에 사용할 20게이지 캐뉼라를 갖고 있지 않다.)"
                2. Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(20게이지 캐뉼라를 찾자.)"
              - 있다면 다음 처리
                - 환자 Display State Descriptor에서 좌측 팔 정맥로 확보 상태를 표시하는 오브젝트, 상태 플래그를 활성화
                - 환자가 붙어있는 환자 침대의 Attachment에서 normal saline 부분 오브젝트의 iv line 연결 포인트 로드, iv line 연결 포인트와 정맥로 확보 상태를 표시하는 오브젝트를 iv line connection 처리
                - 기술 노트: 이와 관련한 로직을 미리 구현하고 그래프 노드로 연결시키기
                - 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경
- `nurse_d`에게 퀘스트 발행
  - 제목: "환자 산소 공급 및 지혈"
  - 목표: ""
  - 퀘스트 발행과 함께 다음 처리 수행:
    1. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_c, ???]선생님, 이 환자 동공반사 확인하고 N/S로 IV 확보해 주세요."
    2. Dialogue
      - Speaker: "의사"
      - Content: "@t=[nurse_d, @s]선생님, 산포도가 낮으니 비강 캐뉼라로 3L 주시고 지혈해 주세요."
      - 퀘스트 목표 표기를 "환자 C에게 산소 공급하기"로 변경
    4. "비강 캐뉼라 적용" Interaction 활성화
      - Interaction 활성화 시 다음 재생
        - 인벤토리에 `nasal_cannula` 아이템이 있는지 확인
          - 없다면 다음 재생
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(비강 캐뉼라를 갖고 있지 않다.)"
            2. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(비강 캐뉼라를 찾자.)"
          - 있다면 다음 처리
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(비강 캐뉼라를 환자에게 적용했다.)"
            2. 조건 분기처리
              - 만약 oxy 오브젝트나 플래그값이 비활성화 상태라면
                - Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(산소 공급 장치가 연결되어 있지 않다.)"
                - Dialogue
                  - Speaker: (플레이어 이름)
                  - Content: "(산소 공급 장치를 찾아 연결하자.)"
              - 만약 oxy 오브젝트나 플래그값이 활성화 상태라면
                - 환자 Display State Descriptor에서 산소 공급 장치 오브젝트를 참조, 상태 플래그를 활성화
                  - 기술 노트: 오브젝트가 Display되었다는 플래그와 산소 공급 장치가 연결되었다는 플래그를 분리해 고려, 관려해야함
                - 환자가 위치한 CareZone의 flowmeter 오브젝트를 참조, 이 오브젝트의 하위에 위치한 Oxy connection point 오브젝트를 참조하여 환자의 oxy connection point와 연결되도록 처리
              - 기술 노트: 이와 관련한 로직을 미리 구현하고 그래프 노드로 연결시키기
            - 이후 퀘스트의 목표를 완료처리
            - Dialogue
              - Speaker: (플레이어 이름)
              - Content: "산소 넣었습니다."
            - 퀘스트 목표 표기를 "환자 C 지혈하기"로 변경
    5. "지혈" Interaction 활성화
      - Interaction 활성화 시 다음 재생
        - 인벤토리에 `gauze` 아이템이 있는지 확인
          - 없다면 다음 재생
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(거즈와 플라스터를 갖고 있지 않다.)"
            2. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(거즈와 플라스터를 찾자.)"
          - 있다면 다음 처리
            1. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(거즈를 출혈 부위에 대고 압박 지혈을 시행했다.)"
            2. 1초 지연
            3. Dialogue
              - Speaker: (플레이어 이름)
              - Content: "(출혈이 멎어, 거즈 위에 플라스터를 붙였다.)"
            3. 퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기"로 변경

- 전체 인원의 퀘스트가 "퀘스트 목표 완료처리, 퀘스트 목표를 "다른 사람들의 처리가 끝날 때까지 기다리기" 상태(*a)라면 퀘스트 완료처리
- 기술 노트: 디버그 편의를 위해 시작할 때 nurse_* 태그를 가진 플레이어가 몇 명인지 파악해두기, 인원수만큼 퀘스트 목표가 (*a) 상태가 되면 퀘스트 완료 처리하도록 구현

### CT실 이송

- 환자 C 퀘스트 완료 시 시작
- 콘텐츠
  1. 지연 2초
  2. DisinteractableDialogue
    - Speaker: "의사"
    - Content: "음.."
    - TTS: true
  3. Dialogue
    - Speaker: "의사"
    - Content: "남성 환자는 E3 / V4 / M6, GCS 13점, 근력 좌측 5점, 우측 3점, 호흡수24, 맥박120, 혈압 140/86, 체온 37.8, 산소포화도 93%..."
  4. Dialogue
    - Speaker: "의사"
    - Content: "여성 환자는 E3 / V4 / M6, GCS 13점, 근력 좌측 5점, 우측 3점, 호흡수24, 맥박120, 혈압 140/86, 체온 37.8, 산소포화도 93%..."
  5. Dialogue
    - Speaker: "의사"
    - Content: "두분 다 뇌손상이 의심되니 브레인 CT 찍어봅시다. 환자들 CT실로 옮겨주세요."
  6. 퀘스트 발행
    - 제목: "환자 CT실 이송"
    - 목표
      1. 서브목표 1
        - 표기: "환자 B를 CT실로 이동시키기"
        - 처리: 환자 B를 CT실로 이동시키면 완료 처리 ct:patient_target_pos_b
      2. 서브목표 2
        - 표기: "환자 C를 CT실로 이동시키기"
        - 처리: 환자 C를 CT실로 이동시키면 완료 처리 ct:patient_target_pos_c
      - 기술노트: 환자 B, 환자 C에 대해 웨이포인트를 따로 설정했지만, 위치는 같게 지정함. 따라서 도달 위치 목표는 큰 범위로 인식 가능해야함
      - 기술노트: 특정 엔티티가 해당 웨이포인트에 도달했을 때 퀘스트를 완료처리할 수 있는가? 없다면플레이어 이동 목표를 엔티티 이동목표로 확장하고, 유형을 플레이어, 환자, 엔티티 전부 로 구분하여 처리할 수 있도록 구현 필요

위의 6. 환자 CT실 이송 퀘스트 완료 시 다음 처리 수행
1. Delay 1초
2. Title
  - Content: 시나리오 종료
  - Subtitle: 시나리오를 완료하였습니다.

## 정맥로 확보 관련하여 처리 과정

_2026-08-19 Updated_

배경: 정맥로 확보 관련하여 버그 발생하여, 정확한 동작을 정의하고자 함

내용:
- 정맥로 확보는 두 단계로 진행 가능함
    1. 환자에게 20G 캐뉼라 삽입 행위
        - 플레이어가 20G 캐뉼라를 인벤토리에 가지고 있으면 시스템에서 처리 시작
        - 플레이어 인벤토리의 20G 캐뉼라를 1개 소모
        - 환자 Display State Descriptor에서 좌측 팔 정맥로 확보 상태를 표시하는 오브젝트, 상태 플래그를 활성화
        - 환자 상태값에 20G 캐뉼라가 삽입되었다는(혹은 정맥로가 확보되었다는) 상태 플래그 활성화
    2. 환자에게 삽입한 20G 캐뉼라를 PS와 IV Line으로 연결하는 행위
        - 환자가 누워있는 침대의 Attachment가 활성화되어있어야 함.
        - 환자가 누워있는 침대의 Attachment에 PS가 적용되어있어야 함(Display 플래그와 상태 플래그 모두 활성화되어있어야 하나, 활성화 여부 판단은 상태플래그를 기준으로 함)
        - "1. 환자에게 20G 캐뉼라 삽입 행위"가 완료되어있고, 환자가 누워있는 침대의 Attachment에 PS가 활성화되어있으면, "플라즈마 솔루션 연결" 인터렉션을 추가(혹은 가시화)
        - 이 인터렉션을 수행하면, 환자가 누워있는 침대 Attachment의 PS의 자식에 있는 Intravenous Line Connection Point 오브젝트와 환자의 좌측 팔 정맥로의 Intravenous Line Connection Point 오브젝트(이 포인트의 식별자를 "patient_b:iv_point_vein"으로 지정, 포인트가 여러개 존재할 때를 대비하여 Point 클래스로 자식 오브젝트들을 쿼리한 후 identifier가 일치하는 것을 찾도록 하기)를 IV Line Connection 처리
- "1. 환자에게 20G 캐뉼라 삽입 행위"가 완료된 후, 환자에게 삽입한 20G 캐뉼라를 PS와 연결하는 행위를 플레이어에게 지시하기 위해, 
     - (위 시나리오 사이에 추가) 20G 캐뉼라 삽입 행위 이후에 "Plasma Solution을 연결하기" 퀘스트 서브목표를 추가 (AI 지시: 우선 위 내용에서 적절히 텍스트를 추가하여라)
     - (위 시나리오 사이에 추가) 위의 퀘스트 서브목표 추가 동작과 함께, Dialogue로 본인의 이름이 발화자로 된 Dialogue, Content Text가 "(환자에게 Plasma Solution을 연결해두자.)"인 다이얼로그도 발생

### 이후 작업 반영 목록 (2026-08-19, 구현 세션에서 읽는 항목)

위 기획은 `### 환자 B 처치` 본문에 **우측 팔 + 생리식염수(N/S)** 로 확정 반영했다(2026-08-19 사용자 확정. 이 문서의 "좌측 팔"·"PS(플라즈마 솔루션)" 표기는 각각 환자 C 서술 참고/다른 시나리오 물품 참고로 판단하여 N/S로 치환). 아래 항목을 scenario.json과 C#에 반영한다. 환자 C(좌측 팔)도 동일한 2단계 구조로 미러링한다.

- `Assets/Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json`
  - `C_IV_WAIT`(sig.insert_iv_patient_b_right 대기) 통과 직후에 아래 2개 노드를 끼워 넣고 `C_NS_WAIT`(sig.connect_cannula_and_ns1_patient_b 대기)로 재배선
    1. QuestControl Update — 목표 표기 "남성 환자에게 생리식염수 연결하기" 변경(신규 quest 정의 추가 필요, `Assets/Modules/TriageTrainer/Resources/Quest/patient_b_c_ct.quests.quest.json`의 `Quest_B_Pupil_IV`와 같은 형식)
    2. Dialogue — Speaker `@s`, "(환자에게 생리식염수를 연결해두자.)"
  - 환자 C 브랜치 동일 처리: `C_C_IV_WAIT` 통과 직후 목표 표기 "여성 환자에게 생리식염수 연결하기" + 동일 Dialogue 후 `C_C_NS_WAIT`로 재배선
- C# (TriageTrainer)
  - 신규 "생리식염수 연결" Interaction(PatientController): 노출 조건 = ① 1단계 삽입 완료(`PatientBCTreatmentStage.AwaitingNormalSaline` 구간) ② 환자 침대 `MovingPatientBedController`의 `IsIntravenousStandInstalled && IsNormalSalineInstalled`(상태 플래그 기준 판정)
  - 수행 시 침대 N/S 오브젝트(`_intravenousHangerHangedNormalSalineReference`) 자식의 `IntravenousLineConnectionPoint`와 환자 우측 팔 포인트(식별자 `patient_b:iv_point_vein`)를 `LineConnectionService`로 IV Line 연결
  - 기존 하드코딩 식별자 "connect_cannula_and_ns1" 기반 물리 연결 판정(`PatientController.TreatmentDisplay.cs`의 `HasPhysicalPatientBCNormalSalineConnection`, `LineConnectionService.cs`, `IntravenousLineConnectionPoint.cs` 3곳)을 `patient_b:iv_point_vein` 자식 Point 쿼리(identifier 일치 검색) 방식으로 교체·일반화. 연결 완료 신호 `connect_cannula_and_ns1_{identifier}`는 유지
  - 1단계 삽입 처리(`cannula_20g` 1개 소모, `Syringe20GInsertedIntoRightArm` 표시 플래그, `insert_iv_patient_b_right` 신호)는 `PatientController.IntravenousLineCannula.cs`에 이미 구현되어 있음 — 동작 확인만 수행
- 프리팹/씬(에디터 작업)
  - `PatientTypeBMale` 우측 팔 정맥로 표시 오브젝트 하위에 `IntravenousLineConnectionPoint`(Identifier=`patient_b:iv_point_vein`) 추가
  - `PatientTypeBFemale`(환자 C, 좌측 팔) 하위에 상응 포인트 추가(식별자 예: `patient_c:iv_point_vein`)
  - `MovingPatientBed` N/S 걸이 오브젝트 하위의 연결 포인트 존재 확인/추가
- 문서
  - `### 환자 C 처치` 정맥로 확보 블록에도 동일 2단계 구조 반영
  - `### 데이터 연결 명세` 환자 B/C 정맥로 행에 연결 포인트 식별자·신호 정리


## 2026-08-07 단일 플레이어 다중 역할 처리 규칙

환자 이동 퀘스트 완료 이후 `nurse_a`~`nurse_d` 각 역할에 주어지는 퀘스트(그 외 역할 병렬 구간도 동일)는, 한 플레이어에게 두 개 이상의 역할 태그가 부여된 경우 다음과 같이 처리한다.

- 부여된 역할의 브랜치만 실행한다. 부여되지 않은 역할의 브랜치는 실행하지 않고 스킵한다.
- 한 플레이어에게 배정된 복수 역할 브랜치는 그래프 정의 순서(a → b → c → d)로 순차 실행한다. 네 역할이 모두 부여된 경우 a 로직 처리 → a 퀘스트 완료 → b 로직 처리 → b 퀘스트 완료 → … → d 퀘스트 완료 순으로 진행한다.
- 역할이 서로 다른 플레이어에게 나뉘어 부여된 다중 접속 세션에서는 기존과 동일하게 플레이어 간에는 병렬로 진행한다.


# 이전 버전 데이터

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 환자 B/C: 뇌손상 의심 및 좌측 상완 개방성 골절 대응 |
| 요약 | 환자를 처치 구역으로 이동시키고 의식/활력징후 사정, 산소화, 지혈, IV 확보, 동공 반응 확인 후 CT실로 이동한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 B, 환자 C(환자 B와 동일 부상), patient_dummy_d_b(분류용) |
| 주요 장소 | 처치 구역, CT실 |
| 리소스 식별자 - 사운드 | tape_sound |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_treatment_area, wp_ct_room |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | SPAWN_B |
| 시나리오 식별자(JSON) | patient_b_c_ct |

> 2026-07-18 연결성 감사부터 기존 `patient_b_c_ct.scenario.json`은 검증 근거로 사용하지 않는다.
> 이 Markdown과 현재 Scenario node/schema 및 gameplay producer 구현을 기준으로 새 JSON을 생성한다.
> 활력 체온은 원본(_origin) 기준 37.8°로 통일하였다(R7/e-1).

## JSON 변환 전 연결성 감사 (2026-07-18)

### 구조 감사 결과

| 검사 | 결과 | 변환 규칙 |
|---|---|---|
| 시작점 및 도달성 | `SPAWN_B`에서 문서의 220개 노드가 모두 도달 가능 | 시작 노드는 `SPAWN_B`로 고정한다. |
| 일반 전이 | 정의되지 않은 일반 `NextIdentifier`/선택지 대상 없음 | 설명 괄호는 식별자에 포함하지 않고, 종료 노드 `N092`만 `null`로 변환한다. |
| 병렬 합류 | 5개 `Parallel`과 10개 완료 표식이 대응됨 | `CC_*`는 별도 노드가 아니라 브랜치 종료 표식으로 직렬화한다. |
| 이벤트 | 고유 `EventIdentifier` 20개가 모두 `TriageScenarioEventBootstrap`에 등록됨 | Requirements 검증에서 handler 등록을 필수로 한다. |
| 퀘스트 | ~~12개 `Quest_*`가 식별자만 있고 title/content/task definition이 없었다.~~ **해결:** `patient_b_c_ct.quests.quest.json`에 12개 definition의 title/description/questContent를 작성했고 모든 `QuestControl` 참조가 정의와 일치한다. | 완료 조건은 각 Scenario Validator가 게이팅하고 QuestControl이 제거하므로, definition의 빈 `tasks`는 안내 전용 quest의 의도된 구성이다. |
| 런타임 신호 | 고유 신호 42개 중 22개가 둘 이상의 Validator에서 재사용됨 | sticky RuntimeState를 환자·행위 단위로 분리하거나 소비 후 clear해야 한다. |

#### 환자 상태 → Scenario 신호 바인딩 (2026-07-20)

`PRESET_C` 다음에 `BIND_B_GAUZE_APPLIED`, `BIND_B_GAUZE_DRESSING`,
`BIND_C_GAUZE_APPLIED`, `BIND_C_GAUZE_DRESSING`을 직렬로 둔다. 각 노드는 해당 환자의
`TreatmentApplied` 상태 이벤트를 구독하고, B/C의 좌측 상완 상태인
`GauzePatchedOnLeftArm`/`GauzeDressingDoneOnLeftArm`
전이를 각각 `apply_gauze_patient_b/c` 및 `apply_plaster_on_gauze_patient_b/c` signal로 변환한다.
따라서 `V058`/`V059`와 `V077`/`V078`은 다른 환자의 sticky signal로 통과할 수 없다. 이 네 signal의
정본 producer는 ItemUse 코드가 아니라 `EntityStateSignalBinding` 노드다.

같은 위치의 `BIND_B_NASAL_APPLIED`/`BIND_C_NASAL_APPLIED`는 `NasalCannulaApplied` 전이를
`apply_nasal_cannula_patient_b/c`로 변환한다. 따라서 `V055`와 `V074`의 비강캐뉼라 적용 조건도
환자별로 분리된다. 산소·석션 사용 신호는 `PatientCareDescriptionZone`이 환자 객체에 장비를
연결한 뒤 `EquipmentConnected` 상태 이벤트를 환자별
`equipment_connected_wall_suction_patient_b/c`,
`equipment_connected_oxyflowmeter_patient_b/c` 신호로 변환한다.

### 플레이 차단 항목과 보완 위치

| ID | 위치 | 부족한 연결 | 처리 |
|---|---|---|---|
| SPAWN-BC-1 | `SPAWN_B`, `SPAWN_C` | **해결 확인(2026-07-29 감사):** `PatientTypeBMale`/`PatientTypeBFemale`에 NetworkObject, PatientController, CapsuleCollider가 있고 두 GUID가 `DefaultPrefabObjects.asset`에 등록되어 있다. | 프리팹 구성/등록은 완료로 기록한다. 호스트·원격 결합 spawn 플레이 검증은 별도 운영 조건이다. |
| SPAWN-BC-2 | `SPAWN_PATIENT_DUMMY_D_B` | `patient_dummy_d_b`를 spawn하는 JSON 노드는 있으나 EntityPreset Requirements SO와 전용 프리팹 등록을 찾지 못했다. | 분류용 dummy prefab을 확정하고 `patient_dummy_d_b` preset/NetworkObject/producer를 등록한다. |
| ROLE-BC-1 | `P009`~`P013` | 기능 태그와 간호사 역할의 선행 매핑이 없고, 상·하위 브랜치 태그가 불일치했다. | **해결(2026-07-29):** `patient_b_c_ct`의 루트 태그를 `nurse_a`~`nurse_d`로 고정하고 모든 Parallel 브랜치를 단일 식별자 태그로 재매핑했다. P009/P010/P012는 A/B, P011/P013은 C/D가 각각 1:1로 배정된다. `matchMode`도 모두 `All`로 통일했고 `whenBranchingPlayerNotMatched=Panic`으로 자격 없는 재배정을 금지했다. 역할 선택은 `disaster_intro`가 부여하는 동일 식별자 태그를 공급 명세로 사용한다. |
| SIGNAL-BC-1 | `V040_A`/`V040_C`, `V040_B`/`V040_D` | ~~같은 들것 신호를 두 번 기다려 2인 파지를 증명하지 못한다.~~ **해결(2026-07-20):** `MovingPatientBedController`가 서버 권위 `SyncVar` 손잡이 슬롯 두 개에 client ID를 기록한다. 프리팹 `PlayerAttachPoints`도 두 개로 배선했다. | 서버가 각 슬롯을 한 client ID에만 배정하고, 각 소유 클라이언트에 follow anchor를 동기화한다. 참가자 입력은 ServerRpc로 보고되어 서버가 침대를 이동하고 transform을 ObserversRpc로 복제한다. 슬롯 0/1이 각각 `grab_stretcher_patient_b/c_handle_0/1`을 발신하며, 시나리오는 이 두 signal을 별도 Validator로 대기한다. |
| SIGNAL-BC-2 | `COUNT_TRIAGE_ARRIVALS` → `V039` | **코드/JSON 구성은 완료, 씬 배선은 미확인:** `ScenarioTriggerZone._perEntitySignalTemplate`(`enter_triage_zone_{id}`)와 `SignalCounter`(prefix `enter_triage_zone_`, threshold 3)가 구현되어 있다. 그러나 현재 씬 직렬화에서 해당 template 설정을 확인하지 못했다. | 트리아지 존에 `enter_triage_zone_{id}`를 설정하고 B/C/dummy의 세 신호가 실제로 counter를 통과하는지 검증한다. |
| SIGNAL-BC-3 | B/C의 장비·처치 Validator | **코드/JSON 분리는 완료, 씬·상호작용 검증은 미완료:** 거즈·플라스터·비강캐뉼라·wall suction·oxyflowmeter 결과 신호는 환자별로 분리되어 있다. | 실제 장비 배치, PatientCareDescriptionZone 귀속, 아이템 Identifier, 환자별 상호작용 결과를 플레이에서 검증한다. |
| SIGNAL-BC-4 | `V036`, `V046`, `V048`, `V050`, `V052`~`V055`, `V065`, `V069`, `V071`~`V074` | ~~문서가 선행 구현 필요로 표시한 신호 producer가 없다.~~ **부분 해결(2026-07-29):** V054/V073의 wall suction과 V055/V074의 oxyflowmeter는 Zone → PatientController → EntityStateSignalBinding 경로를 사용한다. | 남은 미배선 gameplay producer는 GCS/활력/얼굴/더미 상호작용 및 트리아지 Zone 설정이다. |
| Q-BC-1 | `Q031`~`Q042_1` | ~~12개 quest가 식별자만 있어 실제 오버레이 내용과 완료 task가 비어 있었다.~~ **해결:** `Resources/Quest/patient_b_c_ct.quests.quest.json`에 12개 definition의 title/description/questContent를 작성했고 Add/Remove가 같은 identifier를 참조한다. | 완료는 Validator가 판정하고 QuestControl이 Remove하는 안내형 quest이므로 별도 자동 완료 task는 두지 않는다. |
| PRESET-BC-1 | `PRESET_B`, `PRESET_C` | ~~문서가 요구하는 체온과 SpO2는 현재 `PatientMedicalStatePreset` 필드가 아니다.~~ **해결(2026-07-20):** `bodyTemperatureCelsius`, `spo2` 필드를 프리셋 노드/DTO/로더/컨트롤러/스키마에 추가함. | 체온 37.8°, SpO2 93%를 preset에 직접 기입. 모니터 브리지(temperature.t1, numerics/pleth.spo2) 연결 완료. |
| END-BC-1 | `N092` 및 종료 조건 | ~~fade-out 요구가 서술에만 있고 `N092`는 Dialogue 후 종료된다.~~ | **해결(2026-07-29):** `N092 → E_END_BC_FADE`를 추가하고 `fade_out_patient_b_c` 이벤트가 런타임 검은 화면 오버레이를 1초간 0→1로 보간한 뒤 종료한다. 저장소에는 `CameraFade` 구현이 없어 이를 새로 참조하지 않고 Canvas/Image 기반 오버레이로 구현했다. |

### PatientCareDescriptionZone 장비 귀속 명세 (2026-07-29)

- Zone 안에 환자 한 명만 들어온다(`zone_patient_b` ↔ `patient_b`, `zone_patient_c` ↔ `patient_c`).
- Zone은 `IsAttached=true`인 `wall_suction`과 `oxyflowmeter`만 환자 장비로 연결한다.
- 장비 종류별 Zone 내 활성 인스턴스는 정확히 하나여야 하며, 2개 이상이면 연결을 무효화하고 경고한다.
- Zone → `PatientController.EquipmentConnected` → `EntityStateSignalBinding` 순서로 환자별 신호를 발신한다.
- V054/V073은 `equipment_connected_wall_suction_patient_b/c`, V055/V074는 `equipment_connected_oxyflowmeter_patient_b/c`를 기다린다.
- 시나리오 시작/종료 시 `sig.*` RuntimeState를 초기화하여 재실행 시 이전 플레이의 sticky 신호가 게이트를 통과시키지 않도록 한다.
- Zone에는 `MovingPatientBedPositioningPoint`가 하나 있어야 하며, 베드 스냅은 별도 배치 검증 대상이다.
- Zone당 활성 환자는 한 명만 허용하며, 환자가 Zone 내부 positioning point에 고정된 침대에 연결된 경우에만 장비를 귀속한다.

### 변환 승인 조건

- ROLE-BC-1의 역할 태그 공급 명세가 확정되어야 한다.
- SPAWN-BC-1의 FishNet spawnable prefab 등록은 완료 확인되었다.
- SPAWN-BC-2의 `patient_dummy_d_b` EntityPreset과 spawn 명세가 완료되어야 한다.
- 들것 파지와 구역 도착을 각각 참여자/환자 단위로 계측해야 한다.
- 환자별 처치 결과 신호를 분리하고 미배선 producer를 구현해야 한다.
- 12개 quest definition을 등록해야 한다.
- Requirements Supports Production 검증에서 unresolved `Error`가 0개여야 한다.

### 노드 수 요약

- 본 문서에 서술되는 노드: **220개** = JSON 정본 222개(노드 키) − 조합노드 4개 + 추가한 `PRESET_B`/`PRESET_C` 2개.
  - JSON 정본 전체는 222개 노드 키이며, 그중 `A012`/`A013`/`A014`/`A015`(CombineItem) 4개는 조합(crafting) 시스템으로 이관하여 노드 흐름에서 제거한다(R5). → 218개.
  - 여기에 `PRESET_B`, `PRESET_C`(PatientMedicalStatePreset) 2개를 추가 → 220개.
- 추가: `PRESET_B`, `PRESET_C` (스폰 직후 배치, R7).
- 제거(노드 흐름에서): `A012`, `A013`, `A014`, `A015` (crafting-recipes.md로 이관, R5).

## 조합(crafting) 참조 (R5, d-2)

조합은 노드가 아니라 **crafting 시스템**으로 처리한다. 본 시나리오가 필요로 하는 조합 산출물:

| 산출물(정본) | 입력 | 등록 상태 | 비고 |
|---|---|---|---|
| `humidifier_sterile_distilled_water_bottle` | `humidifier_bottle` + `sterile_distilled_water`(멸균증류수) | 등록됨 | 환자 B/C 산소화 선행. 구 `sdw` → 정본 `sterile_distilled_water` 확정 |
| `oxyflowmeter` | `humidifier_sterile_distilled_water_bottle` + `flowmeter` | 등록됨 | 환자 B·C 공용 단일 산출물. 구 `oxyflowmeter_b`/`oxyflowmeter_c` 통합 확정 |

- 상세 레시피/등록 상태는 [crafting-recipes.md](./crafting-recipes.md) 참조.
- [x] 명명충돌/통합 확정요청: `oxyflowmeter_b`/`oxyflowmeter_c` 는 레시피·입력이 동일하므로 단일 `oxyflowmeter` 로 통합 확정(환자 B·C 공용). (crafting-recipes.md §확정 요청 [x], 2026-07-09).
- [x] 산소화 재료(`humidifier_bottle`, `sterile_distilled_water`, `flowmeter`)와 레시피(`humidifier_sterile_distilled_water_bottle`, `oxyflowmeter`)가 등록됨(crafting-recipes.md 참조).

## 역할·태그 정리 (R10, c; 2026-07-29 확정)

`patient_b_c_ct`에서는 기능 태그를 사용하지 않고 플레이어 식별자 태그만 사용한다. `patient_a_critical`의 CPR 교대 등 다른 시나리오의 기능 태그 의미는 이번 변경 범위에 포함하지 않는다.

| 역할 태그 | 본 시나리오 사용처(병렬) | 비고 |
|---|---|---|
| `nurse_a` | P009/P010/P012 첫 번째 브랜치 | 식별자 역할 |
| `nurse_b` | P009/P010/P012 두 번째 브랜치 | 식별자 역할 |
| `nurse_c` | P011/P013 첫 번째 브랜치 | 식별자 역할 |
| `nurse_d` | P011/P013 두 번째 브랜치 | 식별자 역할 |
| `cpr_team` | (본 시나리오 미사용) | patient_a 계열에서 사용 |
| `defib_team` | (본 시나리오 미사용) | patient_a 계열 |
| `medication_team` | (본 시나리오 미사용) | patient_a 계열 |
| `access_support` | (본 시나리오 미사용) | patient_a 계열 |
| `suction_team` | (본 시나리오 미사용) | patient_a 계열 |
| `procedure_team` | (본 시나리오 미사용) | patient_a 계열 |
| `support_team` | (본 시나리오 미사용) | patient_a 계열 |
| JSON 루트 `tags` | `nurse_a`, `nurse_b`, `nurse_c`, `nurse_d` | 시나리오가 허용하는 태그의 정본 |

- [x] c: 역할↔태그 매핑 확정. 기능 태그의 중복 및 상·하위 브랜치 불일치를 제거하고 식별자 태그로 통일했다.

> 단순화의 범위는 `patient_b_c_ct` JSON과 이 시나리오의 다섯 Parallel에 한정한다. `patient_a_critical`의 기능 태그는 CPR 교대와 별도 역할 의미를 가지므로 자동 치환하지 않는다. A까지 확장하려면 CPR 사이클·인트로 역할 부여·모든 A 브랜치의 1:1 매핑을 별도 회귀 검증해야 한다.

## 시그널 배선 상태(요약) (R11, f)

interaction-signal-integration-spec §5.3 기준으로 게이트별 상태를 분류한다.

- **자동 계측 완료**(코드 경로 존재, 씬/아이템 설정 검증 필요): `enter_triage_zone`(구역 진입, 단 인원수 검증은 별도), `apply_electrode`, `apply_gauze`, `apply_plaster_on_gauze`, `wear_glove`.
- **선행 구현 필요**(게임플레이 미구현, 미배선 시 무한 대기 또는 명시된 timeout 복구): `click_patient_b_face`, `click_patient_c_face`, `click_patient_dummy_d_b`. IV 삽입 producer는 `insert_iv_{patientIdentifier}_{left|right}`를 발행하며 환자 B는 우측, 환자 C는 좌측 표시와 본문 지시를 사용한다. 비강캐뉼라 적용은 `NasalCannulaApplied` 상태 바인딩으로 대체했다. 채택 기획으로, 비강 캐뉼라 표시가 활성화되고 oxyflowmeter가 설치됐을 때 양쪽 어느 쪽에서도 `비강 캐뉼라에 산소 연결`을 제공하며 실행 시 두 산소 포트를 연결한다. 양측 `OxyLineConnectionPoint` 프리팹 배치는 선행 작업이다. `click_humidifier_bottle`, `click_sterile_distilled_water`, `click_flowmeter`는 `MedicalItem.OnGet()`이 자동 발행한다.
- **구현 완료(런타임 UI)**: `close_vital_ui_b`, `close_vital_ui_c` — `PatientMonitorController`가 닫기 버튼을 만들고, B/C 활성화 이벤트가 패널·모니터를 숨긴 뒤 환자별 signal을 발생시킨다.
- **에디터 Identifier 정합 필요**(코드는 있으나 프리팹/에디터 매핑 확정 필요): `check_gcs_patient_b`, `check_gcs_patient_c`, `check_vital_patient_b`, `check_vital_patient_c`, `click_patient_b`, `click_patient_c`.

### IV-BC-1 — 20G 팔/신호 명세

`PatientController.IntravenousLineCannula`는 캐뉼라 사용을 실제 처리하고 `insert_iv_{patientIdentifier}_{left|right}` 신호를 발생시킨다. 처치 표시 프리셋이 한쪽 팔만 지원하면 해당 팔을 우선하고, 양쪽이면 좌측부터 배정한다. 현재 그래프는 환자 B의 우측 신호와 환자 C의 좌측 신호를 각각 명시적으로 기다린다.

- [x] 환자 B는 `PatientTypeBMale` 우측 정맥로 표시를 활성화하고 본문 지시대로 우측 팔을 우선한다(2026-08-02).
- [x] 환자 C는 `PatientTypeBFemale` 좌측 정맥로 표시를 활성화하고 본문 지시대로 좌측 팔을 우선한다(2026-08-02).
- [x] 그래프 Validator를 환자 B 우측·환자 C 좌측 producer 신호와 일치시켰다(2026-08-02).
- [x] 프리셋의 단일 지원 팔을 우선하는 최초 삽입 팔 선택을 구현했다(2026-08-02).

## 시나리오 본문

### [SPAWN_B] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | SPAWN_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **PresetIdentifier** | 문자열 | patient_b |
| **SpawnedEntityIdentifier** | 문자열 | patient_b |
| **NextIdentifier** | 문자열 | SPAWN_C |

---

### [SPAWN_C] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | SPAWN_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **PresetIdentifier** | 문자열 | patient_c |
| **SpawnedEntityIdentifier** | 문자열 | patient_c |
| **NextIdentifier** | 문자열 | SPAWN_PATIENT_DUMMY_D_B |

---

### [SPAWN_PATIENT_DUMMY_D_B] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | SPAWN_PATIENT_DUMMY_D_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **PresetIdentifier** | 문자열 | patient_dummy_d_b |
| **SpawnedEntityIdentifier** | 문자열 | patient_dummy_d_b |
| **NextIdentifier** | 문자열 | PRESET_B |

- [ ] patient_dummy_d_b(patient_dummy_d_b)는 분류용 더미이며 처치 노드는 없음(의도). SPAWN_PATIENT_DUMMY_D_B 스폰만 존재(R12).

---

### [PRESET_B] PatientMedicalStatePresetNode

> R7/d-3/e-1: JSON 정본에는 환자 B/C의 PatientMedicalStatePreset가 없어(스폰만 존재) 공백이었다. 원본(_origin) 값으로 사전설정 노드를 추가한다. 원본상 환자 B와 C는 동일 부상/활력이다.

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | PRESET_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.PatientMedicalStatePreset |
| **TargetEntityIdentifier** | 문자열 | patient_b |
| **TransitionMode** | PatientMedicalStateTransitionMode | Immediate |
| **Sex** | Sex | Female |
| **Age** | 정수 | 53 |
| **ConsciousnessGcs** | 정수 | 13 |
| **ConsciousnessEyeOpening** | EyeOpeningResponse | ToSound (E3) |
| **ConsciousnessVerbal** | VerbalResponse | Confused (V4) |
| **ConsciousnessMotor** | MotorResponse | ObeysCommands (M6) |
| **ConsciousnessLocLabel** | LOCLabel | Drowsy |
| **ConsciousnessPupillaryResponse** | PupillaryResponse | Abnormal (우측 무반응) |
| **RespirationAwRR** | 정수 | 24 |
| **RespirationType** | RespirationType | Regular (C# 프로퍼티는 RespirationTypeValue, JSON 키는 respirationType) |
| **PulseRate** | 정수 | 120 |
| **PulseForceType** | BloodPulseForceType | Normal |
| **BloodPressureSystolic** | 정수 | 140 |
| **BloodPressureDiastolic** | 정수 | 86 |
| **SkinColorHue** | SkinColorHue | Normal |
| **SkinTemperatureType** | SkinTemperatureType | Normal |
| **BodyTemperatureCelsius** | 실수 | 37.8 |
| **Spo2** | 정수 | 93 |
| **IsCardiacArrest** | bool | false |
| **NextIdentifier** | 문자열 | PRESET_C |

- 원본 근거: 체온(BT) 37.8°, SpO2 93%. GCS 13(E3/V4/M6), 우측 동공 무반응(pupil_reflex_patient_b), 좌측 상완 개방성 골절.
- [x] SpO2/체온 필드를 PatientMedicalStatePreset 스키마에 추가함(bodyTemperatureCelsius, spo2). 활력 UI 이벤트와 별개로 프리셋에서 직접 설정 가능.
- [x] 활력 체온 37.8(원본) 채택. 프리셋 노드 및 JSON 정본에 37.8/93 반영.
- [x] d-3: 환자 B/C 상태 사전설정 값 원본(_origin)에서 확인·기록.

---

### [PRESET_C] PatientMedicalStatePresetNode

> R8 감사 정정: JSON의 PatientMedicalStatePreset에는 부상 부위가 직접 저장되지 않는다. C의 JSON 치료 binding은 좌측 상완을 사용하지만 C 프리팹의 치료 표시 지원은 우측 상완이므로, 좌측 상완 기준으로 프리팹·binding·대사·근력 사정을 함께 검수해야 한다.

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | PRESET_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.PatientMedicalStatePreset |
| **TargetEntityIdentifier** | 문자열 | patient_c |
| **TransitionMode** | PatientMedicalStateTransitionMode | Immediate |
| **Sex** | Sex | Female |
| **Age** | 정수 | 53 |
| **ConsciousnessGcs** | 정수 | 13 |
| **ConsciousnessEyeOpening** | EyeOpeningResponse | ToSound (E3) |
| **ConsciousnessVerbal** | VerbalResponse | Confused (V4) |
| **ConsciousnessMotor** | MotorResponse | ObeysCommands (M6) |
| **ConsciousnessLocLabel** | LOCLabel | Drowsy |
| **ConsciousnessPupillaryResponse** | PupillaryResponse | Abnormal (좌측 무반응) |
| **RespirationAwRR** | 정수 | 24 |
| **RespirationType** | RespirationType | Regular (C# 프로퍼티는 RespirationTypeValue, JSON 키는 respirationType) |
| **PulseRate** | 정수 | 120 |
| **PulseForceType** | BloodPulseForceType | Normal |
| **BloodPressureSystolic** | 정수 | 140 |
| **BloodPressureDiastolic** | 정수 | 86 |
| **SkinColorHue** | SkinColorHue | Normal |
| **SkinTemperatureType** | SkinTemperatureType | Normal |
| **BodyTemperatureCelsius** | 실수 | 37.8 |
| **Spo2** | 정수 | 93 |
| **IsCardiacArrest** | bool | false |
| **NextIdentifier** | 문자열 | E038 |

- 부상/활력은 환자 B와 동일(좌측 상완 개방성 골절 + 두부 손상, GCS 13). 동공은 C 브랜치 저작 원본대로 좌측 무반응(pupil_reflex_patient_c)을 유지한다. 거즈/지혈 부위는 "좌측 상완"으로 통일.
- [ ] JSON에는 부상 부위가 직접 저장되지 않는다. 현재 C의 JSON state binding은 좌측 상완을 사용하지만 `PatientTypeBFemale.prefab`의 치료 표시 지원은 우측 상완을 사용하므로, 임상 저작·프리팹 시각물·상태 binding을 좌/우 한 방향으로 확정하고 검수한다.

---

### [E038] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | triage_patient_b_patient_c_patient_dummy_d_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D038 |

---

### [D038] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 세 명이 이송되었습니다. 간호사 A가 중증도 분류를 시행합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N029 |

---

### [N029] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 환자를 왼쪽부터 차례대로 클릭하여 환자의 상태를 확인하고, 중증도 분류를 실시하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q031 |

> R3: 구 md의 SpeakerName `System`(코드값)은 플레이어 대면 대사이므로 콘텐츠 화자 `시스템`으로 정규화한다. 이하 모든 System 화자 대사 동일 적용.

---

### [Q031] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Triage_patientB_patientC |
| **NextIdentifier** | 문자열 | V035 |

---

### [V035] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E039 |

- [ ] f: `click_patient_b`는 §5.3상 신체부위/장비 클릭 계열(에디터 Identifier 정합 필요). `WaitForCondition=true`이므로 미배선 시 자동 통과하지 않고 무한 대기한다.

---

### [E039] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_patient_b_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C036 |

---

### [C036] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C036_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C036_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 1(소생) | | | N029_retry_a |
| KTAS 3(응급) | | | N029_retry_a |
| KTAS 4(준응급) | | | N029_retry_a |
| KTAS 5(비응급) | | | N029_retry_a |
| KTAS 2(긴급) | | | N030 |

---

### [N029_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N029_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C036 |

---

### [N030] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 해당 환자를 KTAS 2로 분류했습니다. 다음 환자를 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V036 |

---

### [V036] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_patient_dummy_d_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E040 |

- [ ] f: `click_patient_dummy_d_b`는 §5.3상 선행 메커닉 필요(신체부위/장비 클릭). patient_dummy_d_b 분류용 게이트.

---

### [E040] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_patient_dummy_d_b_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C037 |

---

### [C037] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C037_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C037_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 1(소생) | | | N030_retry_b |
| KTAS 2(긴급) | | | N030_retry_b |
| KTAS 3(응급) | | | N030_retry_b |
| KTAS 4(준응급) | | | N030_retry_b |
| KTAS 5(비응급) | | | N031 |

---

### [N030_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N030_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C037 |

---

### [N031] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 해당 환자를 KTAS 5로 분류했습니다. 다음 환자를 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V037 |

---

### [V037] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E041 |

- [ ] f: `click_patient_c`는 §5.3상 신체부위/장비 클릭(에디터 Identifier 정합 필요).

---

### [E041] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_patient_c_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C038 |

---

### [C038] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C038_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C038_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 1(소생) | | | N031_retry_c |
| KTAS 3(응급) | | | N031_retry_c |
| KTAS 4(준응급) | | | N031_retry_c |
| KTAS 5(비응급) | | | N031_retry_c |
| KTAS 2(긴급) | | | N032 |

---

### [N031_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N031_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C038 |

---

### [N032] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 해당 환자를 KTAS 2로 분류했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N033 |

---

### [N033] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 이제 입원 구역으로 이송할 긴급 환자 2명을 차례대로 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V038 |

---

### [V038] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.select_patient_b AND sig.select_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | Q031_1 |

- [ ] f: `select_patient_b`/`select_patient_c` 게이트. 타임아웃(120s)+ForceAdvance 설정됨(G-6). 배선 상태 확정요청.

---

### [Q031_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q031_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Triage_patientB_patientC |
| **NextIdentifier** | 문자열 | D039 |

---

### [D039] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | KTAS 2(긴급)으로 분류된 환자 2명을 이송하겠습니다. 간호사 B, C, D선생님 이동 도와주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q032 |

---

### [Q032] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_playerB_playerC_playerD_to_triage |
| **NextIdentifier** | 문자열 | V039 |

---

### [V039] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.enter_triage_zone (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | Q032_1 |

- [ ] f: `enter_triage_zone`는 §5.3상 "계측 완료(구역 진입, ScenarioTriggerZone)"로 분류되나, 구 md의 TargetCount 3(3명 진입) 의미가 단일 시그널 Contains로만 검증됨 → 인원수 검증 배선 정합 필요(에디터 Identifier 정합 필요). 자동 통과 위험.

---

### [Q032_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q032_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_playerB_playerC_playerD_to_triage |
| **NextIdentifier** | 문자열 | E042 |

---

### [E042] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | b_c_d_to_triage |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | P009 |

---

### [P009] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P009_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Panic |
| **NextIdentifier** | 문자열 | D058 |

#### [P009_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|
| V040_A | CC_A_C_patient_b_complete | triage_lead, bleeding_control | - | Any |
| V040_B | CC_B_D_patient_c_complete | airway_team, iv_team | - | Any |

> R10 참고: P009 상위 브랜치 태그(V040_A: triage_lead/bleeding_control, V040_B: airway_team/iv_team)와 하위 P010/P011/P012/P013 태그가 불일치한다(상세는 상단 "역할·태그 정리(예비)"). 저작 원본 그대로 보존한다. P009만 matchMode=Any, 나머지 병렬은 All.

====================================================
# [P009 병렬 브랜치 1] 환자 B 처치 그룹 (플레이어 A, C)
====================================================

### [V040_A] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_b_handle_0 (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | V040_C |

- [x] 서버가 손잡이 0에 고유 client ID를 배정하면 `grab_stretcher_patient_b_handle_0`를 발신한다.

---

### [V040_C] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_b_handle_1 (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E043 |

- [x] 서버가 손잡이 1에 다른 client ID를 배정하면 `grab_stretcher_patient_b_handle_1`를 발신한다. 한 client는 하나의 슬롯만 점유할 수 있다.

---

### [E043] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N034 |

---

### [N034] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 처치 구역에 도착했습니다. 간호사 A는 의식상태를, 간호사 C는 활력징후를 사정하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | P010 |

---

### [P010] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P010_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Panic |
| **NextIdentifier** | 문자열 | D041 |

#### [P010_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|
| N035 | CC_A_gcs_patient_b | neuro_assessment | - | All |
| N043 | CC_C_vital_patient_b | vital_team | - | All |

====================================================
# [P010 병렬 브랜치 1] 플레이어 A (환자 B 의식 사정)
====================================================

### [N035] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 클릭하여 환자의 의식상태를 사정하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q033 |

---

### [Q033] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_B |
| **NextIdentifier** | 문자열 | V041 |

---

### [V041] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.check_gcs_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | N036 |

- [ ] f: `check_gcs_patient_b`는 §5.3상 "계측 완료(환자 프리팹 Assess Actions)"이나, 프리팹 assessSignal 매핑(에디터 Identifier 정합)이 실제로 배선되어 있는지 확정요청. 타임아웃 설정됨.

---

### [N036] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해주세요. 정답 시 계속 진행, 오답 시 재응시 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N037 |

---

### [N037] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C039 |

---

### [C039] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C039_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C039_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) | | | N037_retry_a |
| P(Pain response, 통증에 반응 있음) | | | N037_retry_a |
| U(Unconsciousness, 반응 없음) | | | N037_retry_a |
| V(Verbal response, 음성에 반응 있음) | | | N038 |

---

### [N037_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N037_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C039 |

---

### [N038] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C040 |

---

### [C040] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C040_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C040_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적) | | | N038_retry_b |
| 2점(통증) | | | N038_retry_b |
| 1점(반응 없음) | | | N038_retry_b |
| 3점(명령) | | | N039 |

---

### [N038_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N038_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C040 |

---

### [N039] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C041 |

---

### [C041] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C041_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C041_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) | | | N039_retry_c |
| 3점(부적절한 답변) | | | N039_retry_c |
| 2점(신음소리) | | | N039_retry_c |
| 1점(반응 없음) | | | N039_retry_c |
| 4점(혼란) | | | N040 |

---

### [N039_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N039_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C041 |

---

### [N040] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C042 |

---

### [C042] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C042_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C042_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(통증 원인을 치우려고 손을 뻗음) | | | N040_retry_d |
| 4점(통증에 회피) | | | N040_retry_d |
| 3점(이상 굴곡) | | | N040_retry_d |
| 2점(이상 신전) | | | N040_retry_d |
| 1점(반응 없음) | | | N040_retry_d |
| 6점(명령 수행) | | | N041 |

---

### [N040_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N040_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C042 |

---

### [N041] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N042 |

---

### [N042] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS의 M(Motor Response) 사정 중 왼쪽 다리가 오른쪽 다리의 정상 근력보다 약하고, 저항에 이기지 못하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C043 |

---

### [C043] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 정상인 우측(5점)에 비해, 좌측의 근력 수준은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C043_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C043_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(정상 근력) | | | N042_retry_e |
| 4점(중력+약간의 저항) | | | N042_retry_e |
| 2점(중력에 저항 불가, 좌우 운동) | | | N042_retry_e |
| 1점(약간의 근육 수축) | | | N042_retry_e |
| 0점(움직임 없음) | | | N042_retry_e |
| 3점(중력에 저항 가능) | | | D040 |

---

### [N042_retry_e] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N042_retry_e |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C043 |

---

### [D040] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 현재 시나리오 B 환자의 GCS는 13점, 근력(Motor Grade)은 우측 5점, 좌측 3점입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q033_1 |

---

### [Q033_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q033_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_B |
| **NextIdentifier** | 문자열 | CC_A_gcs_patient_b |

====================================================
# [P010 병렬 브랜치 2] 플레이어 C (환자 B 활력징후 사정)
====================================================

### [N043] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q034 |

---

### [Q034] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_B |
| **NextIdentifier** | 문자열 | V042 |

---

### [V042] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_vital_set AND sig.click_electrode AND sig.click_electrode_cable |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N044 |

- [ ] f: 아이템 클릭 3종 게이트. 배선 정합(에디터 Identifier) 확정요청.

---

### [N044] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 전극을 선택하여 환자의 가슴에 부착하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V043 |

---

### [V043] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_electrode (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N045 |

- [ ] f: `apply_electrode`는 §5.3상 "계측 완료(Attachable Item Visuals Apply Signal)". 자동 계측 가능.

---

### [N045] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V044 |

---

### [V044] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.connect_patient_and_monitor_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N046 |

---

### [N046] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V045 |

---

### [V045] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.check_vital_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E044 |

- [ ] f: `check_vital_patient_b`는 §5.3상 "계측 완료(Assess Actions)". 프리팹 assessSignal 매핑 확정요청.

---

### [E044] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | activate_vital_monitor_ui_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N047 |

---

### [N047] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.8도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 6.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V046 |

- 체온 37.8°(원본), SpO2 93%. PRESET_B와 일치(R7/e-1). 구 md/JSON의 37.3은 오류로 정정.

---

### [V046] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.close_vital_ui_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | Q034_1 |

- [x] f: 모니터의 `닫기` 버튼이 B 전용 callback을 통해 패널·모니터를 숨기고 `close_vital_ui_b`를 발생시킨다.

---

### [Q034_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q034_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_B |
| **NextIdentifier** | 문자열 | CC_C_vital_patient_b |

====================================================
# [P010 병렬 종료 및 P011 진입 (환자 B)]
====================================================

### [D041] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | B 환자의 의식상태는 GCS 13점, 근력 우측 5점/좌측 3점이며, 활력징후는 혈압 140/86, 맥박 120, 호흡수 24, 체온 37.8, SpO2 93% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D042 |

- 체온 37.8°로 정정(R7/e-1).

---

### [D042] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 A 선생님, 펜라이트로 동공반사 확인해주시고, 20게이지로 IV라인 확보하고 생리식염수 1L 수액백 연결해주세요. 간호사 C 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | P011 |

---

### [P011] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P011_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Panic |
| **NextIdentifier** | 문자열 | N062 |

#### [P011_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|
| N048 | CC_A_pupil_iv_patient_b | pupil_check | - | All |
| N052 | CC_C_nasal_pressure_patient_b | bleeding_control | - | All |

====================================================
# [P011 병렬 브랜치 1] 플레이어 A (동공 확인 및 IV 확보)
====================================================

### [N048] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q035 |

---

### [Q035] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_B |
| **NextIdentifier** | 문자열 | V047 |

---

### [V047] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_penlight (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N049 |

---

### [N049] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V048 |

---

### [V048] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_patient_b_face (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | E045 |

- [ ] f: `click_patient_b_face`는 §5.3상 "선행 메커닉 필요(신체부위 클릭 미구현)". 타임아웃 후 `ForceAdvance`되지만 이는 정상 완료가 아닌 복구 경로다.

---

### [E045] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | pupil_reflex_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D043 |

---

### [D043] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 좌측 동공에 비해 우측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N050 |

---

### [N050] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 다음으로 IV 라인을 확보합니다. 환자의 우측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 6.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V049 |

---

### [V049] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_20g AND sig.click_intravenous_set AND sig.click_normal_saline_1000ml |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N051 |

- R1 아이템 식별자 정합: `click_iv_set`→`click_intravenous_set`, `click_ns1`→`click_normal_saline_1000ml`.

---

### [N051] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭해 정맥 라인을 확보하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V050 |

---

### [V050] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.insert_iv_patient_b_left OR sig.insert_iv_patient_b_right (RegistryContains / RuntimeState, matchMode=Any) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | E046 |

- [x] f: 좌·우 실제 producer 신호 중 하나를 받도록 갱신했다. 타임아웃은 producer 또는 프리팹 설정 문제에 대한 안전망으로 유지한다.

---

### [E046] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_20g_right_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N051_1 |

- R2: MoveNextBehavior는 `Immediately`(구 `Immediate` 오타 정정).

---

### [N051_1] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N051_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V051 |

---

### [V051] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.connect_cannula_and_ns1_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E047 |

---

### [E047] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ns1_right_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D044 |

- R2: `Immediately`(구 `Immediate` 정정).

---

### [D044] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 정맥로가 확보되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D045 |

---

### [D045] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q035_1 |

---

### [Q035_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q035_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_B |
| **NextIdentifier** | 문자열 | CC_A_pupil_iv_patient_b |

====================================================
# [P011 병렬 브랜치 2] 플레이어 C (환자 B 산소 투여 및 지혈)
====================================================

### [N052] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 비강캐뉼라를 이용한 산소화를 먼저 실시합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N053 |

---

### [N053] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q036 |

---

### [Q036] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_B |
| **NextIdentifier** | 문자열 | V052 |

---

### [V052] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_humidifier_bottle AND sig.click_sterile_distilled_water |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N054 |

> R5: 구 `A012`(CombineItem, humidifier_sterile_distilled_water_bottle) 노드를 노드 흐름에서 제거하고 V052 → N054 로 재연결한다.
- [x] 조합은 crafting 시스템으로 처리하며 `humidifier_sterile_distilled_water_bottle` 레시피가 등록되어 있다(crafting-recipes.md 참조).
- [x] `click_humidifier_bottle`/`click_sterile_distilled_water`는 각 아이템 획득 시 `MedicalItem.OnGet()`이 발행한다.

---

### [N054] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 유량계를 습득하여 산소 유량계를 완성하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V053 |

---

### [V053] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_flowmeter (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N055 |

> R5: 구 `A013`(CombineItem, oxyflowmeter_b) 노드를 노드 흐름에서 제거하고 V053 → N055 로 재연결한다. 산출물은 단일 `oxyflowmeter` 로 통합 확정(환자 B·C 공용).
- [x] 조합은 crafting 시스템으로 처리하며 `oxyflowmeter`(구 `oxyflowmeter_b`) 레시피가 등록되어 있다(crafting-recipes.md 참조).
- [x] 명명충돌/통합 확정요청: `oxyflowmeter_b`/`oxyflowmeter_c` 는 단일 `oxyflowmeter` 로 통합 확정(레시피·입력 동일, 환자 B·C 공용)(crafting-recipes.md §확정 요청 [x], 2026-07-09).
- [x] `click_flowmeter`는 아이템 획득 시 `MedicalItem.OnGet()`이 발행한다.

---

### [N055] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V054 |

---

### [V054] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.equipment_connected_wall_suction_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N056 |

---

### [N056] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V055 |

---

### [V055] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_nasal_cannula_patient_b AND sig.equipment_connected_wall_suction_patient_b AND sig.equipment_connected_oxyflowmeter_patient_b |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N057 |

- [x] f: 비강캐뉼라는 환자 상태 바인딩으로, wall suction/oxyflowmeter는 `PatientCareDescriptionZone`의 환자별 `EquipmentConnected` 바인딩으로 계측한다.

---

### [N057] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C044 |

---

### [C044] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C044_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C044_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5L | | | N057_retry |
| 10L | | | N057_retry |
| 15L | | | N057_retry |
| 3L | | | D046 |

---

### [N057_retry] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N057_retry |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 처방은 3L 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C044 |

---

### [D046] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 산소 투여가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q036_1 |

---

### [Q036_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q036_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_B |
| **NextIdentifier** | 문자열 | N058 |

---

### [N058] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N058 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q037 |

- 지혈 부위는 좌측 상완(원본, R8).

---

### [Q037] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_B |
| **NextIdentifier** | 문자열 | V056 |

---

### [V056] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_gloves AND sig.click_gauze AND sig.click_plaster |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N059 |

- R1 아이템 식별자 정합: `click_glove`→`click_gloves`.

---

### [N059] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N059 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 멸균장갑을 [우클릭]해 착용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V057 |

---

### [V057] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.wear_glove (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N060 |

- [ ] f: `wear_glove`는 §5.3상 "계측 완료(착용 Apply Signal)". 자동 계측 가능.

---

### [N060] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N060 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V058 |

---

### [V058] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V058 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_gauze (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | E048 |

- [ ] f: `apply_gauze`는 §5.3상 "계측 완료(Apply Signal)". 자동 계측 가능. 타임아웃 설정됨.

---

### [E048] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N061 |

---

### [N061] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N061 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V059 |

---

### [V059] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V059 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_plaster_on_gauze (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E049 |

- [ ] f: `apply_plaster_on_gauze`는 §5.3상 "계측 완료(Apply Signal)". 자동 계측 가능.

---

### [E049] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_with_plaster_patient_b |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | S006 |

---

### [S006] SoundNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | S006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D047 |

---

### [D047] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 지혈 중입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D048 |

---

### [D048] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 산소 적용 및 지혈이 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q037_1 |

---

### [Q037_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q037_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_B |
| **NextIdentifier** | 문자열 | CC_C_nasal_pressure_patient_b |

====================================================
# [P011 병렬 종료 (환자 B 처치 완료)]
====================================================

### [N062] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N062 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 시나리오 B 환자에 대한 간호 중재가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | CC_A_C_patient_b_complete |

====================================================
# [P009 병렬 브랜치 2] 환자 C 처치 그룹 (플레이어 B, D)
====================================================

> R8 감사 기준: C의 활력/GCS는 B와 동일하게 유지한다. 지혈/거즈 JSON binding은 좌측 상완이며, 동공 무반응은 좌측이다. C 프리팹의 치료 표시 지원은 우측 상완이므로 좌측 상완 기준의 시각물과 GCS 근력 사정 좌우를 임상 검수한다.

### [V040_B] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_c_handle_0 (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | V040_D |

- [x] 서버가 손잡이 0에 고유 client ID를 배정하면 `grab_stretcher_patient_c_handle_0`를 발신한다.

---

### [V040_D] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_D |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_c_handle_1 (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E050 |

- [x] 서버가 손잡이 1에 다른 client ID를 배정하면 `grab_stretcher_patient_c_handle_1`를 발신한다. 한 client는 하나의 슬롯만 점유할 수 있다.

---

### [E050] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N063 |

---

### [N063] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N063 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 처치 구역에 도착했습니다. 즉시 의식상태 사정 및 활력징후 사정을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | P012 |

---

### [P012] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P012_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Panic |
| **NextIdentifier** | 문자열 | D049 |

#### [P012_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|
| N064 | CC_B_gcs_patient_c | neuro_assessment | - | All |
| N072 | CC_D_vital_patient_c | vital_team | - | All |

====================================================
# [P012 병렬 브랜치 1] 플레이어 B (환자 C 의식 사정)
====================================================

### [N064] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N064 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 클릭하여 환자의 의식상태를 사정하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q038 |

---

### [Q038] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_C |
| **NextIdentifier** | 문자열 | V060 |

---

### [V060] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V060 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.check_gcs_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | N065 |

- [ ] f: `check_gcs_patient_c`는 §5.3상 "계측 완료(Assess Actions)"이나 프리팹 assessSignal 매핑 확정요청. 타임아웃 설정됨.

---

### [N065] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N065 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N066 |

---

### [N066] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N066 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C045 |

---

### [C045] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C045_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C045_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) | | | N066_retry_a |
| P(Pain response, 통증에 반응 있음) | | | N066_retry_a |
| U(Unconsciousness, 반응 없음) | | | N066_retry_a |
| V(Verbal response, 음성에 반응 있음) | | | N067 |

---

### [N066_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N066_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C045 |

---

### [N067] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N067 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C046 |

---

### [C046] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C046_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C046_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적) | | | N067_retry_b |
| 2점(통증) | | | N067_retry_b |
| 1점(반응 없음) | | | N067_retry_b |
| 3점(명령) | | | N068 |

---

### [N067_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N067_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C046 |

---

### [N068] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N068 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C047 |

---

### [C047] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C047_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C047_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) | | | N068_retry_c |
| 3점(부적절한 답변) | | | N068_retry_c |
| 2점(신음소리) | | | N068_retry_c |
| 1점(반응 없음) | | | N068_retry_c |
| 4점(혼란) | | | N069 |

---

### [N068_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N068_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C047 |

---

### [N069] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N069 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C048 |

---

### [C048] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C048_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C048_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(통증 원인을 치우려고 손을 뻗음) | | | N069_retry_d |
| 4점(통증에 회피) | | | N069_retry_d |
| 3점(이상 굴곡) | | | N069_retry_d |
| 2점(이상 신전) | | | N069_retry_d |
| 1점(반응 없음) | | | N069_retry_d |
| 6점(명령 수행) | | | N070 |

---

### [N069_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N069_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C048 |

---

### [N070] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N070 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N071 |

---

### [N071] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N071 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS의 M(Motor Response) 사정 중 오른쪽 다리가 왼쪽 다리의 정상 근력보다 약하고, 간호사가 가하는 저항에 이기지 못하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C049 |

- [ ] R8 검수: C의 근력 사정 좌우(우측 약함)는 JSON 저작 원본을 보존함. 부상 부위(좌측 상완)와의 정합은 임상 검수 필요.

---

### [C049] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 정상인 좌측(5점)에 비해, 우측의 근력 수준은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C049_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C049_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(정상 근력) | | | N071_retry_e |
| 4점(중력+약간의 저항) | | | N071_retry_e |
| 2점(중력에 저항 불가, 좌우 운동) | | | N071_retry_e |
| 1점(약간의 근육 수축) | | | N071_retry_e |
| 0점(움직임 없음) | | | N071_retry_e |
| 3점(중력에 저항 가능) | | | D050 |

---

### [N071_retry_e] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N071_retry_e |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C049 |

---

### [D050] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 현재 시나리오 C 환자의 GCS는 13점, 근력(Motor Grade)은 좌측 5점, 우측 3점입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q038_1 |

---

### [Q038_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q038_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_C |
| **NextIdentifier** | 문자열 | CC_B_gcs_patient_c |

====================================================
# [P012 병렬 브랜치 2] 플레이어 D (환자 C 활력징후 사정)
====================================================

### [N072] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N072 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q039 |

---

### [Q039] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_C |
| **NextIdentifier** | 문자열 | V061 |

---

### [V061] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V061 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_vital_set AND sig.click_electrode AND sig.click_electrode_cable |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N073 |

---

### [N073] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N073 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 전극을 선택하여 환자의 가슴에 부착하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V062 |

---

### [V062] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V062 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_electrode (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N074 |

- [ ] f: `apply_electrode`는 §5.3상 "계측 완료(Apply Signal)". 자동 계측 가능.

---

### [N074] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N074 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V063 |

---

### [V063] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V063 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.connect_patient_and_monitor_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N075 |

- 참고: 환자 B 대응 시그널은 `connect_patient_and_monitor_b`, 환자 C는 `connect_patient_and_monitor_patient_c`(명명 비대칭). JSON 정본 그대로 표기.

---

### [N075] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N075 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V064 |

---

### [V064] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V064 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.check_vital_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E051 |

- [ ] f: `check_vital_patient_c`는 §5.3상 "계측 완료(Assess Actions)". 프리팹 assessSignal 매핑 확정요청.

---

### [E051] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | activate_vital_monitor_ui_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N076 |

---

### [N076] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N076 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.8도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 6.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V065 |

- 체온 37.8°(원본), SpO2 93%. PRESET_C와 일치(R7/e-1). 구 md/JSON의 37.3은 오류로 정정.

---

### [V065] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V065 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.close_vital_ui_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | Q039_1 |

- [x] f: 모니터의 `닫기` 버튼이 C 전용 callback을 통해 패널·모니터를 숨기고 `close_vital_ui_c`를 발생시킨다.

---

### [Q039_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q039_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_C |
| **NextIdentifier** | 문자열 | CC_D_vital_patient_c |

====================================================
# [P012 병렬 종료 및 P013 진입 (환자 C)]
====================================================

### [D049] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | C 환자의 의식상태는 GCS 13점, 근력 좌측 5점/우측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.8도, SpO2 93% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D051 |

- 체온 37.8°로 정정(R7/e-1). 활력은 환자 B와 동일(R8).

---

### [D051] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 B 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 D 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | P013 |

---

### [P013] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P013_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Panic |
| **NextIdentifier** | 문자열 | N091 |

#### [P013_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|
| N077 | CC_B_pupil_iv_patient_c | pupil_check | - | All |
| N081 | CC_D_nasal_pressure_patient_c | bleeding_control | - | All |

====================================================
# [P013 병렬 브랜치 1] 플레이어 B (동공 확인 및 IV 확보)
====================================================

### [N077] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N077 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q040 |

---

### [Q040] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_C |
| **NextIdentifier** | 문자열 | V066 |

---

### [V066] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V066 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_penlight (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N078 |

---

### [N078] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N078 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V067 |

---

### [V067] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V067 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_patient_c_face (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E052 |

- [ ] f: `click_patient_c_face`는 §5.3상 "선행 메커닉 필요(신체부위 클릭 미구현)". 타임아웃 후 `ForceAdvance`되지만 이는 정상 완료가 아닌 복구 경로다.

---

### [E052] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | pupil_reflex_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D052 |

---

### [D052] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 우측 동공에 비해 좌측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N079 |

- C의 동공 무반응 측은 저작 원본대로 좌측 유지(R8).

---

### [N079] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N079 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 다음으로 IV 라인을 확보합니다. 환자의 좌측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 6.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V068 |

---

### [V068] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V068 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_20g AND sig.click_intravenous_set AND sig.click_normal_saline_1000ml |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N080 |

- R1 아이템 식별자 정합: `click_iv_set`→`click_intravenous_set`, `click_ns1`→`click_normal_saline_1000ml`.

---

### [N080] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N080 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V069 |

---

### [V069] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V069 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.insert_iv_patient_c_left OR sig.insert_iv_patient_c_right (RegistryContains / RuntimeState, matchMode=Any) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | E053 |

- [x] f: 좌·우 실제 producer 신호 중 하나를 받도록 갱신했다. 타임아웃은 producer 또는 프리팹 설정 문제에 대한 안전망으로 유지한다.

---

### [E053] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_20g_left_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N080_1 |

- R2: `Immediately`(구 `Immediate` 정정).

---

### [N080_1] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N080_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V070 |

---

### [V070] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V070 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.connect_cannula_and_ns1_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E054 |

---

### [E054] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ns1_left_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D053 |

- R2: `Immediately`(구 `Immediate` 정정).

---

### [D053] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 정맥로가 확보되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D054 |

---

### [D054] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 환자의 좌측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q040_1 |

---

### [Q040_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q040_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_C |
| **NextIdentifier** | 문자열 | CC_B_pupil_iv_patient_c |

====================================================
# [P013 병렬 브랜치 2] 플레이어 D (환자 C 산소 투여 및 지혈)
====================================================

### [N081] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N081 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 비강캐뉼라를 이용한 산소화를 먼저 실시합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | N082 |

---

### [N082] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N082 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q041 |

---

### [Q041] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_C |
| **NextIdentifier** | 문자열 | V071 |

---

### [V071] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V071 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_humidifier_bottle AND sig.click_sterile_distilled_water |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N083 |

> R5: 구 `A014`(CombineItem, humidifier_sterile_distilled_water_bottle) 노드를 노드 흐름에서 제거하고 V071 → N083 로 재연결한다.
- [x] 조합은 crafting 시스템으로 처리하며 `humidifier_sterile_distilled_water_bottle` 레시피가 등록되어 있다(crafting-recipes.md 참조).
- [x] `click_humidifier_bottle`/`click_sterile_distilled_water`는 각 아이템 획득 시 `MedicalItem.OnGet()`이 발행한다.

---

### [N083] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N083 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 유량계를 습득하여 산소 유량계를 완성하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V072 |

---

### [V072] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V072 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_flowmeter (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N084 |

> R5: 구 `A015`(CombineItem, oxyflowmeter_c) 노드를 노드 흐름에서 제거하고 V072 → N084 로 재연결한다. 산출물은 단일 `oxyflowmeter` 로 통합 확정(환자 B·C 공용).
- [x] 조합은 crafting 시스템으로 처리하며 `oxyflowmeter`(구 `oxyflowmeter_c`) 레시피가 등록되어 있다(crafting-recipes.md 참조).
- [x] 명명충돌/통합 확정요청: `oxyflowmeter_b`/`oxyflowmeter_c` 는 단일 `oxyflowmeter` 로 통합 확정(레시피·입력 동일, 환자 B·C 공용)(crafting-recipes.md §확정 요청 [x], 2026-07-09).
- [x] `click_flowmeter`는 아이템 획득 시 `MedicalItem.OnGet()`이 발행한다.

---

### [N084] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N084 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V073 |

---

### [V073] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V073 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.equipment_connected_wall_suction_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N085 |

---

### [N085] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N085 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V074 |

---

### [V074] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V074 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_nasal_cannula_patient_c AND sig.equipment_connected_wall_suction_patient_c AND sig.equipment_connected_oxyflowmeter_patient_c |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N086 |

- [x] f: 비강캐뉼라는 환자 상태 바인딩으로, wall suction/oxyflowmeter는 `PatientCareDescriptionZone`의 환자별 `EquipmentConnected` 바인딩으로 계측한다.

---

### [N086] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N086 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C050 |

---

### [C050] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C050_Options 표 참조]** |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | null |

#### [C050_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5L | | | N086_retry |
| 10L | | | N086_retry |
| 15L | | | N086_retry |
| 3L | | | D055 |

---

### [N086_retry] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N086_retry |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 처방은 3L 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | C050 |

---

### [D055] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 산소 투여가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q041_1 |

---

### [Q041_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q041_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_C |
| **NextIdentifier** | 문자열 | N087 |

---

### [N087] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N087 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q042 |

- 지혈 부위는 좌측 상완(원본, R8). 구 md의 "무릎 하단" 서술은 폐기.

---

### [Q042] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_C |
| **NextIdentifier** | 문자열 | V075 |

---

### [V075] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V075 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.click_gloves AND sig.click_gauze AND sig.click_plaster |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N088 |

- R1 아이템 식별자 정합: `click_glove`→`click_gloves`.

---

### [N088] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N088 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 멸균장갑을 [우클릭]해 착용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V076 |

---

### [V076] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V076 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.wear_glove (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N089 |

- [ ] f: `wear_glove`는 §5.3상 "계측 완료(착용 Apply Signal)". 자동 계측 가능.

---

### [N089] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N089 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V077 |

---

### [V077] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V077 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_gauze (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E055 |

- [ ] f: `apply_gauze`는 §5.3상 "계측 완료(Apply Signal)". 자동 계측 가능.

---

### [E055] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N090 |

---

### [N090] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N090 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 5.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V078 |

---

### [V078] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V078 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.apply_plaster_on_gauze (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E056 |

- [ ] f: `apply_plaster_on_gauze`는 §5.3상 "계측 완료(Apply Signal)". 자동 계측 가능.

---

### [E056] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_with_plaster_patient_c |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | S007 |

---

### [S007] SoundNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | S007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D056 |

---

### [D056] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 지혈 중입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D057 |

---

### [D057] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 산소 적용 및 지혈이 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | Q042_1 |

---

### [Q042_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q042_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_C |
| **NextIdentifier** | 문자열 | CC_D_nasal_pressure_patient_c |

====================================================
# [P013 병렬 종료 (환자 C 처치 완료)]
====================================================

### [N091] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N091 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 시나리오 C 환자에 대한 간호 중재가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 4.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | CC_B_D_patient_c_complete |

====================================================
# [P009 병렬 종료 및 최종 브리핑 / CT실 이송]
====================================================

### [D058] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D058 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 기전과 사정 결과를 보니 뇌손상이 의심됩니다. 활력징후는 비교적 안정되어 있으니 지금 Brain CT 찍겠습니다. 지금 환자를 CT실로 이동시켜주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | E057 |

---

### [E057] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patients_to_ct |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N092 |

- R1: 구 `move_patients_to_CT`(camelCase) → JSON 정본 `move_patients_to_ct`(snake_case).

---

### [N092] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N092 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 10.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열/null | E_END_BC_FADE |

- 본 노드가 실제 종료 노드이다(NextIdentifier가 null/공백). R9 참조.

## 종료 조건 (R9)

| 항목 | 내용 |
|---|---|
| 종료 노드 | E_END_BC_FADE 이후 종료 |
| 종료 연출/설명 | 두 환자 모두 CT실 도달 후 최종 브리핑(D058) → CT 이송(E057) → 종료 메시지(N092, 10초) → `fade_out_patient_b_c`(1초) → 시나리오 종료. 검은 화면은 종료 메시지 표시 후 덮인다. |

- [x] b-1 수정: 구 종료조건 표의 D063은 미정의 노드였으며 N092 및 최종 fade 이벤트로 정정했다.

## 후속 확인 체크리스트 (요약)

- [x] R1: 이벤트/시그널/아이템 식별자를 JSON 정본 snake_case로 통일(`triage_patient_b_patient_c_patient_dummy_d_b`, `show_patient_b_ui`, `move_patient_b`, `move_patients_to_ct`, `b_c_d_to_triage`, `pupil_reflex_patient_c` 등). Validator 조건을 `sig.*` RuntimeState registryIdentifier로 표기. 아이템 정합(`click_gloves`/`click_intravenous_set`/`click_normal_saline_1000ml`) 반영.
- [x] R2: MoveNextBehavior `Immediate`→`Immediately` (E046/E047/E053/E054).
- [x] R3: 플레이어 대면 System 대사 화자를 `시스템`으로 정규화.
- [x] R4: 주석을 checklist(`- [ ]`/`- [x]`) 문법으로 통일.
- [x] R5: CombineItem 노드(A012/A013/A014/A015) 제거 및 재연결. 조합(crafting) 참조 섹션 추가.
- [x] R6: Delay 노드 불필요(도입하지 않음). `Immediate` 오타 잔존 없음.
- [x] R7: PRESET_B/PRESET_C(PatientMedicalStatePreset) 추가. 체온 37.8° 채택.
- [ ] R8: 환자 C의 좌측 상완 JSON binding, 프리팹 치료 시각물, 근력 사정 대사를 같은 방향으로 임상 검수.
- [x] R9: 종료 노드 D063 → N092 정정.
- [x] R10: 역할·태그 정리(예비) 섹션 추가, 불일치 항목 checklist 명시.
- [x] R11: Validator 게이트별 배선 상태 주석(자동 계측 완료 / 선행 구현 필요 / 에디터 Identifier 정합 필요).
- [x] R12: patient_dummy_d_b(patient_dummy_d_b) 분류용 더미, 처치 노드 없음 명시.
- [ ] JSON/씬 후속 필요: `patient_dummy_d_b` preset 등록, 미배선 gameplay producer, 트리아지 per-entity zone 배선, Bootstrap 연출 참조, C 좌/우 임상·시각물 정합, Production Requirements 검증.
