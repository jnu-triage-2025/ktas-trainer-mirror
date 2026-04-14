---
title: "scenario example"
doc_type: requirement
status: active
updated: 2026-04-14
---

# scenario example

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 야간 순찰과 첫 단서 |
| 요약 | 플레이어가 야간 순찰을 시작하고, 선택지와 간단한 이동/사운드/이벤트를 통해 첫 단서를 발견한다. |
| 주요 등장인물 | 경비원(플레이어), 안내 NPC(미나) |
| 주요 장소 | 서쪽 창고, 경비실 |
| 리소스 식별자 - 사운드 | sfx_door_metal, bgm_night_patrol |
| 리소스 식별자 - 초상화 | portrait_mina |
| 리소스 식별자 - 웨이포인트 | wp_guardroom, wp_warehouse |
| 리소스 식별자 - 카메라 타겟 | obj_guardroom_door |
| 선언 태그(tags) | patrol, clue_found |
| 시작 노드 Identifier | D001 |

## 시나리오 본문

| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D001 | Dialogue | 경비원이 “야간 순찰을 시작한다.”라고 말한다. 즉시 경비실 분위기를 알리는 연출로 넘어간다. | S001 |
| S001 | Sound | bgm_night_patrol을 재생한다. 분위기 음악은 끝까지 유지하므로 WaitUntilFinished는 false로 본다. | CT001 |
| CT001 | CameraTarget | 카메라가 obj_guardroom_door를 향해 천천히 전환된다. OffsetX/Y/Z는 0,0,0이고 BlendTime은 1.2로 서술한다. | C001 |
| C001 | Choice | 미나가 “어디부터 둘러볼까요?”라고 묻는다. PortraitSprite는 portrait_mina를 사용한다. 아래 선택지로 분기된다. | (optional) |
| C001-OPT1 | ChoiceOption | “서쪽 창고부터 간다.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | PM001 |
| C001-OPT2 | ChoiceOption | “경비실을 먼저 점검한다.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | V001 |
| PM001 | PlayerMove | 플레이어는 Waypoint 방식으로 wp_warehouse까지 이동한다. MoveMode는 BySpeed, MoveSpeed는 3.5, IgnoreGroundCheck는 false로 서술한다. | D002 |
| D002 | Dialogue | 경비원이 “창고 문이 살짝 열려 있다.”라고 말한다. | E001 |
| E001 | InvokeEvent | EventIdentifier로 warehouse_door_open_check를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | S002 |
| S002 | Sound | sfx_door_metal을 재생한다. WaitUntilFinished는 true로 서술한다. | TM001 |
| TM001 | TagModification | Operation은 Add, Scope는 Current, Tag는 clue_found로 설정해 현재 플레이어에 단서 확보 태그를 부여한다. | Q001 |
| Q001 | QuestControl | Quest에 대해 Operation은 Add로 수행한다. FailureStrategy는 Overwrite로 서술한다. | D003 |
| D003 | Dialogue | 경비원이 “첫 단서를 확보했다.”라고 말한다. | PM002 |
| PM002 | PlayerMove | 플레이어는 Waypoint 방식으로 wp_guardroom까지 복귀한다. MoveMode는 ByDuration, MoveDuration은 4.0으로 서술한다. | D004 |
| V001 | Validator | 플레이어 수가 1명인지 확인한다. Condition은 PlayerCountEqual, TargetCount는 1이다. 실패 시 Branching으로 D005로 간다. | D004 |
| D004 | Dialogue | 미나가 “이상 무.”라고 말한다. | D006 |
| D005 | Dialogue | 미나가 “인원 확인이 필요해요.”라고 말한다. | D006 |
| D006 | Dialogue | 경비원이 “순찰을 계속한다.”라고 말하고 시나리오는 다음 장면으로 넘어간다. | (end) |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D006 |
| 종료 연출/설명 | 다음 노드가 없으므로 종료된다. 이후 장면 전환은 외부 시스템에서 처리한다. |
