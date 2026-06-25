---
title: "환자 A/B/C 시나리오 JSON 변환 노트 및 검토 결과"
doc_type: requirement
domain: content-definitions
progress: "3-implemented"
status: active
updated: 2026-06-23
flags: ["refactor-required"]
---

# 환자 A/B/C 시나리오 JSON 변환 노트 및 검토 결과

이 문서는 다음 두 작업의 근거와 결과를 정리한다.

1. [`patient_a_critical.md`](./patient_a_critical.md) / [`patient_b_c_ct.md`](./patient_b_c_ct.md) 가
   원본 텍스트([`_origin/시뮬레이션 사례 + 평가 루브릭 (4차 수정).txt`](./_origin/))와
   scenario JSON 규격을 얼마나 잘 만족하는지에 대한 검토(judgement)
2. 이들 `.md` 를 scenario JSON 으로 변환할 때 따라야 하는 규칙과,
   현재 node 스펙의 표현력 부족으로 발생하는 후속 구현 TODO(플래그)

규격/규칙의 1차 근거는 다음 문서들이며, 본 문서는 환자 A/B/C 변환에 특화된 부속 노트다.

- [`json-conversion-rules.md`](./json-conversion-rules.md)
- [`scenario-graph-spec.md`](./scenario-graph-spec.md)
- `Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json`
- 변환 선례: `Assets/Modules/TriageTrainer/Resources/Scenario/disaster_intro.json`
  및 동봉된 `disaster_intro.unsupported.flags.json`

---

## 1. 검토 결과 요약

### 1-1. 원본 텍스트 충실도 — 양호

`.md` 두 파일은 원본 텍스트의 임상 흐름(ABCDE 순서, 환자 A의 CPR 2 사이클, 환자 B/C의
중증도 분류 → 의식/활력징후 → 산소화·지혈·IV → 동공반사 → CT 이송)을 충실히 반영하고
있으며, 다음과 같이 원본보다 교육적으로 보강된 부분이 있다(의도된 보강으로 판단, 문제 없음).

- 원본의 활력징후 표/관찰 포인트를 Dialogue 출력 + Choice(AVPU/GCS/근력 등) 문답으로 분해.
- 원본의 CPR 중 "압박 깊이/속도/주의사항" 등 메뉴창을 Choice + 오답 재응시 루프로 구현.
- 원본의 "2분 경과 후 리듬 확인" 연출을 Delay + 리듬 확인 Dialogue 로 표현.
- 환자 A의 활력징후 수치는 원본과 일치(GCS 8 = E2/V2/M4, 70/40, HR140, RR8, SpO2 82%).
  단, 체온 35.9도는 원본에 없던 값으로 `.md`에서 추가됨(문제 없으나 출처 메모로 남김).
- 환자 B GCS 13(E3/V4/M6), 140/86, HR120, RR24 등 원본과 일치. SpO2/체온은
  원본 텍스트(라인 561~564)와 `.md`(N047: SpO2 93%, 체온 37.3도) 간 미세 차이 존재
  → 아래 1-3 불일치 항목 참조.

원본에 없으나 합리적으로 신설된 시나리오 분기: 환자 C(=환자 B 동일 부상)를 B와 병렬
처치(P009: A+C가 B, B+D가 C). 원본 라인 527의 "동시에 2명을 각각 간호사 A+C, B+D가 수행"
지시와 일치한다.

### 1-2. JSON 규격 충실도 — 구조는 적합, 단 3가지 체계적 위반 존재

`.md`는 "문서형(설계용) 표기"이며, 그대로 JSON 으로 직렬화하면 `scenario.schema.json`
검증에 **실패**한다. 사용된 nodeType(Dialogue/Choice/CombineItem/Delay/InvokeEvent/
Parallel/QuestControl/Sound/Validator)은 모두 스펙에 존재하므로 nodeType 자체는 문제없다.
다만 아래 3가지가 스펙과 어긋난다. 이는 설계 문서의 결함이 아니라
"문서형 → 엔진형" 변환 시 반드시 치환해야 하는 항목이며, `disaster_intro` 변환에서
이미 동일하게 처리된 선례가 있다.

> 결론: `.md` 자체는 설계 의도 표현으로서 적절하나, **JSON 직렬화 전 아래 치환이 필수**다.

### 1-3. 발견된 사소한 내부 불일치(변환 담당자가 원저자 확인 후 보정)

- 환자 B 활력징후: 원본/요약 Dialogue 간 SpO2·체온 값 표기가 미세하게 다름
  (`patient_b_c_ct.md` N047 "SpO2 93%, 체온 37.3도" vs 원본 라인 563 "체온 37.8도",
  D041 요약 "37.3"). 변환 시 한 값으로 통일 필요. **(TODO-DATA-1)**
- `patient_a_critical.md` C010 두 번째 선택지 라벨이
  "약 600ml (다섯 손가락 모두를 이용해 백을 짠다)"로, 원본 의도(엄지·검지·중지)와
  괄호 설명이 어긋남(복붙 오류로 추정). 변환 시 라벨 보정 필요. **(TODO-DATA-2)**
- 일부 InvokeEvent `EventIdentifier` 대소문자 혼용(`Apply_ambu_patientA`,
  `Stop_ambu_and_comp` vs `stop_ambu_and_comp`). event-registry 와 대조하여
  소문자 스네이크로 통일 필요. **(TODO-DATA-3)**
- `patient_a_critical.md` V017 `Condition`은 3개 항목(`Click_18g, Click_ns1, Click_ps1`)
  인데 `TargetCount`가 4 로 적혀 있음(본문 주석은 "18G 2개 + 수액 2개"). 변환 시
  todo 이벤트로 묶으므로 큰 영향은 없으나 의도 확인 필요. **(TODO-DATA-4)**
- `patient_b_c_ct.md` "종료 조건" 표는 종료 노드를 `D063`으로 적었으나 본문에 `[D063]`
  정의가 없음(최고 D-노드 D058, 실제 그래프 종료는 `N092`). 변환은 `N092`를 터미널로
  사용했고, 종료 조건 표를 `N092`로 보정 권장. **(TODO-DATA-5)**

## 1-4. 변환 산출물 검증 결과 (2026-06-23 완료)

Agent Manager 워크트리 2개(`scenario-patient-a-json`, `scenario-patient-bc-json`)에서
산출된 JSON을 본 브랜치로 통합하고 다음을 검증해 모두 통과했다.

- 노드 커버리지 1:1 (환자 A 344/344, 환자 B/C 219/219 — 누락/신설 0건)
- `scenario.schema.json` 검증 통과(양 파일)
- 잔존 Validator 0 / Duration 0 / RequiredRoleIdentifiers 0 / Choice.nextIdentifier 비-null 0
- 모든 `nextIdentifier`/`nextNodeIdentifier`/브랜치 식별자/`completionConditionIdentifier`
  참조 무결성 통과(dangling 0)
- 태그 전부 사전 선언(로더 경고 0)
- 환자 C 처치 흐름은 환자 B를 `_patient_c` 접미사로 미러링하여 별도 식별자로 생성됨(확인)
- flags 파일: A=unsupported 59 / followUpEvents 51 / enumConv 32,
  B/C=unsupported 140 / followUpEvents 33 / enumConv 14 (+ dataInconsistencies 기록)

---

## 2. JSON 변환 규칙 (환자 A/B/C 공통)

`json-conversion-rules.md`를 그대로 따르되, 본 시나리오에서 실제로 적용되는 항목을 구체화한다.

### 2-1. 루트 구조

```json
{
  "identifier": "<patient_a_critical | patient_b_c_ct>",
  "tags": [ /* 사용하는 모든 playerTag 사전 선언 */ ],
  "nodes": { "<id>": { ... }, ... }
}
```

- 노드 맵의 key는 각 노드의 `identifier`와 반드시 동일해야 한다.
- 시작 노드: 환자 A = `D005`, 환자 B/C = `E038`. (별도 시작 필드는 없고 진입 식별자 운용.)

### 2-2. 필드 표기

- 모든 노드 필드는 **camelCase** 로 직렬화한다(`speakerName`, `dialogueContent`,
  `portraitSpriteIdentifier`, `nextIdentifier`, `nextNodeIdentifier` 등).
- Dialogue/Choice 의 `**PortraitSprite**` 와 `**PortraitSpriteIdentifier**` 는 모두
  `portraitSpriteIdentifier` (값 없으면 `null`) 로 통일한다.
- Dialogue 의 `NextNodeIdentifier` 표기는 모두 `nextIdentifier` 로 직렬화한다
  (Dialogue 노드의 다음 이동 필드명은 `nextIdentifier`. `nextNodeIdentifier`는 Choice
  옵션 전용).

### 2-3. ★드롭: `Duration` 필드 (Dialogue) — 스펙에 없음

- Dialogue 노드의 `**Duration**` 필드는 `ScenarioDialogueNode` 스펙에 존재하지 않는다.
  (DialogueNode 직렬 필드: `speakerName`, `dialogueContent`, `portraitSpriteIdentifier`)
- 변환 시 **무조건 제거**한다. (총 202개 발생: A 121, B/C 81)
- **표현력 부족 플래그 → TODO-SPEC-1** (자동 진행 대기시간이 설계 의도이므로 스펙에
  `duration` 추가를 제안하거나, 필요 시 Delay 노드로 분리해야 함. 현 변환에서는 단순 드롭.)

### 2-4. ★치환: Validator domain 조건 → InvokeEvent (todo.validate.*)

- 현재 `ScenarioValidatorNode.rootConditions[].condition` 은 enum 만 허용한다:
  `PlayerCountEqual/NotEqual/LessThan/LessThanOrEqual/GreaterThan/GreaterThanOrEqual`,
  `RegistryContains`, `PlayerAssignedTag`.
- `.md`의 Validator 들은 전부 도메인 상호작용 조건(`Click_*`, `Apply_*`, `Connect_*`,
  `Insert_*`, `Push_*`, `Suction_*`, `Check_*`, `Remove_*`, `Pass_*`, `Grab_*`,
  `Enter_*`, `Move_*`, `Select_*`, `wear_glove`, `apply_electrode`,
  `connect_patient_and_monitor_b` 등)을 사용한다. (A 57개, B/C 47개)
- `disaster_intro` 선례대로 **InvokeEvent 로 치환**한다.

  변환 패턴(단일 조건):
  ```json
  "<원래 Validator id>": {
    "identifier": "<원래 Validator id>",
    "nodeType": "InvokeEvent",
    "eventIdentifier": "todo.validate.<condition_소문자_스네이크>",
    "moveNextBehavior": "WaitUntilDone",
    "nextIdentifier": "<원래 NextIdentifier>"
  }
  ```
  변환 패턴(복수 조건 `A, B` + TargetCount N): 하나의 todo 이벤트로 병합한다.
  예) `Click_humidifierbottle, Click_sdw` → `todo.validate.click_humidifierbottle_and_sdw`.
- 생성되는 모든 `todo.validate.*` 이벤트는 후속 구현 대상이므로 각 파일별
  `*.unsupported.flags.json` 에 누적 기록한다(아래 3장).
- **표현력 부족 플래그 → TODO-SPEC-2** (아이템 클릭/적용/연결, 구역 진입, 환자 신체부위
  클릭 등 "도메인 인터랙션 완료"를 정식으로 검증할 수단이 Validator에 없음. 장기적으로는
  `Interaction` 노드 + `completionConditionIdentifier`, 또는 Validator 조건 확장 필요.)

### 2-5. Parallel 치환

- `WaitMode`: `WaitAll → All`.
- `AllocationType`: `ByRole`(엔진 정식 지원 — 1차 변환의 `SelfAll` 매핑은 폐기됨).
  각 브랜치를 자격(`requiredPlayerTags`)에 맞는 **서로 다른** 플레이어에게 1:1 배정한다.
  → 다인 동시 협력 처치 의도가 실제로 실현된다(과거 `SelfAll` 은 단일 플레이어로 붕괴시켰음).
- `WhenBranchingPlayerNotMatched`: 비어있으면 `Ignore` 로 채운다(선례 일치).
- 브랜치 필드는 **`identifier`, `completionConditionIdentifier`, `requiredPlayerTags`,
  `forbiddenPlayerTags`, `requiredPlayerTagsMatchMode`** 만 허용
  (`additionalProperties:false`).
- ★`.md`의 `RequiredRoleIdentifiers`(NurseA/NurseB…) 컬럼은 **스키마에 없음** → 드롭한다.
  역할 분기 의도는 `requiredPlayerTags` 로만 표현한다(예: NurseA=`triage_lead`,
  NurseB=`airway_team`/`cpr_team`, NurseC=`bleeding_control`/`defib_team`,
  NurseD=`iv_team`/`medication_team` 등 `.md`의 RequiredPlayerTags 컬럼 사용).
  - 단, CPR 사이클에서 동일 플레이어가 사이클마다 다른 태그를 요구받는다(예: A가 P005에서
    `airway_team`, P006에서 `cpr_team`). 태그가 정적이면 매칭이 깨질 수 있으므로,
    교대(swap) 시 **TagModification(`PlayerTag`) 노드로 태그를 갱신**하거나
    `requiredPlayerTagsMatchMode`/태그 설계를 재검토해야 한다.
    **표현력/설계 플래그 → TODO-SPEC-3**.
- 모든 브랜치의 `completionConditionIdentifier`(예: `CC_A_ambu`)는 해당 브랜치 서브그래프의
  **종료 노드 식별자**로서 `nodes`에 실제 존재해야 한다. `.md`에서 각 브랜치 마지막
  QuestControl(Remove)의 `NextIdentifier`가 `CC_*` 를 가리키므로, 그 `CC_*` 자체를
  실제 노드로 둘지(예: 빈 통과용 Dialogue) 또는 마지막 노드 id를 곧 `CC_*` 로 맞출지
  결정해야 한다. disaster_intro 는 마지막 의미 노드의 `nextIdentifier`를 `CC_*` 로 두고
  `CC_*` 를 별도 노드로 만들지 않은 채 브랜치 종료로 사용했다 → **동일 방식 채택**
  (브랜치는 `completionConditionIdentifier` 도달 시 종료).

### 2-6. Choice 치환

- 분리형 ChoiceOptionNode 는 사용하지 않음(원래 `.md`도 병합형이므로 그대로 둠).
- `options[]` 로 작성, ChoiceNode 의 `nextIdentifier` 는 **반드시 `null`**.
- `DisplayColor` `#88AAFF` → `{ "r":0.533, "g":0.667, "b":1.0, "a":1.0 }` 로 변환
  (또는 disaster_intro 처럼 색 생략 — 생략 시 로더가 흰색 기본값 적용. 본 변환은
  **색 생략** 으로 통일하여 노이즈를 줄인다).
- 오답 선택지는 retry Dialogue 로, 정답 선택지는 다음 노드로 연결(이미 `.md`에 반영됨).

### 2-7. QuestControl / CombineItem / Sound / Delay / InvokeEvent

- QuestControl: `operation`(Add/Remove), `failureStrategy`(Ignore), `quest`.
  `.md`의 `Quest` 값(예: `Quest_Grab_Stretcher`)을 `quest.Id` 로 사용:
  `"quest": { "Id": "Quest_Grab_Stretcher" }` (disaster_intro 와 동일하게 Id만 채움).
- CombineItem: `inputItemIdentifiers`(배열), `outputItemIdentifier`, `autoCombine:true`.
- Sound: `soundResourceIdentifier`, `waitUntilFinished`.
- Delay: `durationSeconds`(숫자), `waitUntil`(`WaitUntilDone` | `Immediately`).
  `.md`의 `4(초)` → `4`.
- InvokeEvent: `eventIdentifier`, `moveNextBehavior`.
  `MoveNextBehavior` 치환: **`Immediate → Immediately`**, `WaitUntilDone → WaitUntilDone`.

### 2-8. 태그 사전 선언

- 루트 `tags` 에 시나리오에서 쓰는 모든 playerTag 를 선언한다. 본 시나리오의 브랜치
  RequiredPlayerTags 컬럼에서 등장하는 값 일체:
  `triage_lead, airway_team, bleeding_control, iv_team, neuro_assessment,
  suction_team, access_support, cpr_team, defib_team, medication_team,
  vital_team, pupil_check` 등. (각 파일에서 실제 사용된 것만 선언; 미선언 태그 사용 시
  로더 경고.) 정확한 집합은 변환 담당이 해당 파일에서 추출한다.

---

## 3. 산출물 및 후속 구현 TODO (플래그)

### 3-1. 산출 파일

- `Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.json`
- `Assets/Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.json`
- `Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.unsupported.flags.json`
- `Assets/Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.unsupported.flags.json`

`.unsupported.flags.json` 은 `disaster_intro.unsupported.flags.json` 스키마를 그대로
따른다: `unsupportedSourceConstructs[]`(각 Validator→InvokeEvent 치환, 드롭된 Duration,
드롭된 RequiredRoleIdentifiers 등), `followUpEventsToImplement[]`(모든 `todo.validate.*`),
`enumConversionsApplied[]`(WaitAll→All, ByRole→SelfAll, Immediate→Immediately).

### 3-2. 스펙 표현력 부족 — 엔진 개선 TODO 및 구현 상태

| ID | 내용 | 상태(2026-06-23) | 비고 |
|---|---|---|---|
| TODO-SPEC-1 | DialogueNode 자동 진행 시간 부재 | **구현됨** | `autoAdvanceSeconds`(opt-in). null/0이하=입력 대기(하위호환) |
| TODO-SPEC-2 | Validator 도메인 인터랙션 완료 검증 불가 | **엔진 구현됨 + JSON 치환 완료** | `RegistryContains`+`RuntimeState`+`ScenarioInteractionSignals`. 세 시나리오의 `todo.validate.*` 스텁 113개(A 57·B/C 47·intro 9)를 `Validator(RegistryContains, RuntimeState, sig.*)` 로 치환 완료(todo 스텁 0). 게임플레이가 인터랙션 완료 시 `ScenarioInteractionSignals.Raise("<cond>")` 로 신호를 올리는 연결만 후속(각 `*.unsupported.flags.json` 의 `gameplaySignalsToRaise` 참조) |
| TODO-SPEC-3 | 역할 태그 교대(CPR 사이클) 표현 불가 | **구현됨** | PlayerTag `Swap`(1:1 교대) + `ByTag` scope 추가 |
| TODO-SPEC-4 | 동일 처치 서브그래프 재사용 수단 없음 | **보류** | 플레이 차단 아님(환자 C 복제로 동작). 침습적이라 우선순위 최하로 연기 |
| GAP-G1 | 병렬 다인 동시 협력 분배 부재(ByRole 없음 → SelfAll 단일 플레이어 붕괴) | **구현됨(2026-06-24)** | `ScenarioParallelAllocationType.ByRole` 추가. 각 브랜치를 자격(태그)에 맞는 서로 다른 플레이어에게 1:1 배정. 두 시나리오 Parallel 11개 전부 `ByRole` 로 재변환 |
| GAP-G2 | 병렬 브랜치 완료조건 미구현(ExecuteBranch TODO, 대기 없음) | **구현됨(2026-06-24)** | `RunBranchChain` 으로 브랜치가 NextIdentifier 체인을 끝까지(또는 `completionConditionIdentifier` 수렴 라벨까지) 실행·대기. WaitAll 합류 정상화 |
| GAP-G3 | Validator 일회성 평가(게이팅 무력) | **구현됨(2026-06-24)** | `ScenarioValidatorNode.WaitForCondition`(opt-in) 추가. true 시 조건 충족까지 폴링 대기. 신호 게이팅 Validator 104개(A 57·B/C 47) `waitForCondition:true` 재변환 |
| GAP-G4 | 게임플레이 `sig.*` 발신 미배선 | **부분(디버그 훅)** | `/scenario signal <cond> [clear]` 커맨드 추가(테스트용). 실제 인터랙션→Raise 배선은 후속 TriageTrainer 작업 |
| GAP-G5 | Sound 노드 미구현 | **구현됨(2026-06-24)** | `ExecuteSoundNode` 가 `Resources/Sound/<id>`(폴백 `Resources/<id>`) 에서 클립 로드 후 `PlayOneShot` 재생, `WaitUntilFinished` 시 실제 클립 길이만큼 대기 |
| GAP-G6 | Choice→Quiz 채점 미연동 | **보류(별도 설계 필요)** | 루브릭(수행/미수행) 집계는 관찰자/평가자 모드·루브릭 데이터모델·영속화가 필요한 신규 기능. 별도 제안 필요. Quiz 노드(correctIndex/feedback)는 이미 정/오답 표현 가능 |
| GAP-G7 | Dialogue Duration 손실 | **구현됨(2026-06-24)** | 엔진 `autoAdvanceSeconds`(opt-in) 로 복원. 원본 `.md` 의 Duration 을 해당 Dialogue 노드 `autoAdvanceSeconds` 로 재변환(A 99건·B/C 81건 적용) |
| GAP-G8 | 시나리오가 클라이언트 로컬 실행(TargetRpc)이라 다인 분배·신호 공유 불성립 | **P1 구현(2026-06-24)** | `ScenarioNetworkRelay`(NetworkBehaviour) 로 완료 신호를 서버 권한화(클라 인터랙션 → ServerRpc → 서버 RuntimeState). 중계기 부재 시 로컬 폴백(회귀 없음). P2(권위 플레이어 풀)·P3(실행 권위/표현 RPC)는 후속. 상세: `Agents/Proposals/2026-06-24-scenario-parallel-execution/server-authoritative-execution-spec.md` |

TODO-SPEC-* 는 모두 엔진(`MultiplayerInfrastructure`) 변경을 필요로 하여, 루트 `AGENTS.md` 정책에 따라
`/Agents/Proposals/Feature Proposal - ScenarioNode Expressiveness/` 에 Feature Proposal + 예시 설계 명세를
먼저 작성하였고(2026-06-23), 이후 SPEC-1·2·3 을 엔진에 구현하였다(전부 opt-in·하위호환, 기존 시나리오 회귀 0건).
GAP-G1~G3(병렬 실행·게이팅 계층)은 별도 제안서
`/Agents/Proposals/2026-06-24-scenario-parallel-execution/` 에 정리 후 엔진에 구현하였다(2026-06-24).
본 변환 작업 자체(JSON 산출)는 엔진 변경 없이 수행되었다.

### 3-2-1. 플레이 가능화 — 엔진 무관 선행 작업 (2026-06-23)

엔진 확장(TODO-SPEC) 승인/구현과 **무관하게, 재작업 위험 없이** 선행 가능한 항목부터 처리한다.

**(완료) 이벤트 식별자 정합 — InvokeEvent 핸들러 매칭**
- 변환 JSON 은 이벤트 식별자를 소문자 스네이크(`move_patient_a_to_treatmentroom`)로 쓰는데,
  기존 핸들러(`TriageScenarioEventBootstrap`)는 camelCase(`move_patientA_to_treatmentroom`)로
  등록되어 있고 레지스트리가 `StringComparer.Ordinal`(대소문자 구분)이라 매칭 실패했다.
- 비-`todo.validate.*` 실제 이벤트 50개 중 23개는 직접 일치, 27개가 불일치였다.
- **방침: JSON 식별자 컨벤션(소문자 스네이크)을 단일 기준으로 유지하고, C# 런타임에서 그 식별자를
  그대로 등록·호출한다.** 별칭 매핑 계층은 두지 않는다(코드 측 컨벤션이 다소 깨지더라도 식별자
  기준을 JSON 하나로 통일).
- 해결: 불일치 27개 핸들러 파일의 `Register("...")` 등록 문자열을 JSON 의 스네이크 식별자로 직접
  변경했다(파일명/메서드명 등 C# 표기는 그대로 두어 변경 범위 최소화).
- `disaster_intro` 와 공유되는 `B_C_D_to_triage` 핸들러는 disaster_intro(camelCase)와
  patient_b_c_ct(`b_c_d_to_triage`, snake) 양쪽 식별자를 같은 핸들러에 **둘 다 직접 등록**하여
  기존 인트로 호환을 유지한다.
- 결과: disaster_intro / disaster_intro_mvp / patient_a_critical / patient_b_c_ct 4개 그래프의
  실제(비-todo) 이벤트가 전부 핸들러로 해소(미해소 0).
- 재작업 위험: 없음. `todo.validate.*` 는 의도적으로 등록 대상에서 **제외**한다(향후 TODO-SPEC-2의
  Interaction/Validator 노드로 치환될 예정이므로 지금 핸들러를 붙이면 폐기 작업이 됨).

**(완료) 초기 역할 태그 부여 — intro 역할 선택 + PlayerTag(Add)**
- 원본 루브릭상 "역할 선택"은 step 0(intro 단계)이고, 환자 A/B/C 는 태그가 이미 있다고 가정하고
  중간(D005/E038)부터 시작한다. 따라서 역할 선택+태그 부여를 **`disaster_intro.json` 도입부**
  (D002 직후)에 배치했다. 환자 A/B/C JSON 은 변경하지 않는다.
- 구현: `C_role_select`(Choice, 4개 역할) → 각 역할별 `PlayerTag(Add, Scope=Current)` 체인 → `P001`.
  각 플레이어가 자기 역할을 선택하면 해당 태그 집합이 본인(시나리오 owner)에게 부여된다.
- 태그 모델: **각 facet 태그를 정확히 한 간호사만 보유(disjoint)** 하도록 cycle-1 기준 배정.
  - nurse_a: triage_lead
  - nurse_b: airway_team, cpr_team, support_team
  - nurse_c: bleeding_control, defib_team, neuro_assessment
  - nurse_d: iv_team, access_support, medication_team, procedure_team, suction_team, vital_team, pupil_check
  - (+ 각자 식별 태그 nurse_a~d)
- 검증(정적 시뮬레이션): 단일 역할 브랜치는 전부 1:1 매칭(intro 3/3, 환자A 20/20, 환자B·C 8/8),
  MULTI(중복 매칭) 0건. dangling 참조 0, 스키마 통과.
- **2인 협업 브랜치(=원본상 2인 공동 수행) — `matchMode:Any` 적용 완료**: 아래 3개는 한 명이
  두 태그를 모두 가질 수 없는 협업 브랜치였다(이전엔 `matchMode:All` 로 매칭 0건).
  - 환자A `P004/N008` `airway_team+triage_lead` (B+A 기관내삽관 보조)
  - 환자B/C `P009/V040_A` `bleeding_control+triage_lead` (A+C 환자 B 이송)
  - 환자B/C `P009/V040_B` `airway_team+iv_team` (B+D 환자 C 이송)
  → `requiredPlayerTagsMatchMode = Any` 로 변경하여, 두 협업 간호사가 모두 적격(MULTI)이 되도록 했다.
    이는 "두 명이 함께 수행"하는 원본 의도와 일치한다(`SelfAll` 할당이 양쪽에 브랜치를 제공).
- **CPR 교대(P005→P006) — 해소 완료(식별자 태그 방식)**: 분석 결과 facet 태그(airway_team/cpr_team)는
  비-CPR 병렬에서도 재사용되어(예: P003 N005 airway=활력측정 담당 B vs P005 N017 airway=앰부 담당 A)
  같은 facet 이 병렬마다 다른 간호사를 가리킨다. 따라서 `PlayerTag Swap` 으로 facet 을 교환하는
  방식은 "한 간호사가 두 facet 을 모두 보유" 문제를 일으켜 부적합했다.
  대신 **CPR 병렬(P005·P006)의 브랜치 태그를 안정적 식별자 태그(`nurse_a`~`nurse_d`)로 재지정**했다.
  - P005(사이클1): N017=nurse_a(앰부), N018=nurse_b(가슴압박), N019=nurse_c(제세동), N020=nurse_d(에피)
  - P006(사이클2): N021=nurse_a(가슴압박), N022=nurse_b(앰부), N023=nurse_c(에피), N024=nurse_d(제세동)
  - 식별자 태그는 intro 역할 선택 prelude 에서 각 간호사에게 1:1 부여되므로, 두 사이클 모두 결정적으로
    1:1 매칭되며 사이클 간 역할 전환이 "어떤 브랜치를 맡는가"로 자연스럽게 표현된다.
    (즉 본 케이스에서는 `PlayerTag Swap` 노드가 불필요. Swap 연산은 다른 교대형 시나리오를 위해 유지.)

**(남은 엔진 무관 작업)**
- 씬/레지스트리 등록: 환자/더미/침대/모니터/간호사/Waypoint 를 Entity/Npc/Waypoint 레지스트리에 등록.

### 3-3. 변환 후 검증 체크리스트

- [ ] 시작 노드(A=`D005`, B/C=`E038`) 존재
- [ ] 모든 `nextIdentifier`/`nextNodeIdentifier`/브랜치 `identifier`/
      `completionConditionIdentifier` 가 실제 노드(또는 정의된 브랜치 종료점)를 가리킴
- [ ] Choice 의 `nextIdentifier` 가 모두 `null`
- [ ] Validator 잔존 0건(전부 InvokeEvent 치환)
- [ ] Duration / RequiredRoleIdentifiers 잔존 0건
- [ ] 루트 `tags` 에 사용 태그 전부 선언(로더 경고 0)
- [ ] `ScenarioGraphLoader.LoadFromJson(json, validateWithSchema:true)` 통과
- [ ] flags 파일에 모든 `todo.validate.*` 및 enum 변환 기록
