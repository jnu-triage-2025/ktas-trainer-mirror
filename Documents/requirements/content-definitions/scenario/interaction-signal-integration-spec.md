---
title: "인터랙션 완료 신호(sig.*) 게임플레이 연결 명세"
doc_type: requirement
domain: content-definitions
progress: "1-designed"
status: active
updated: 2026-06-23
flags: ["refactor-required"]
---

# 인터랙션 완료 신호(sig.*) 게임플레이 연결 명세

시나리오 게이팅은 `Validator(condition=RegistryContains, registryType=RuntimeState, registryIdentifier="sig.<cond>")`
로 "학습자가 해당 인터랙션을 완료했는가"를 검사한다. 게임플레이 코드가 인터랙션 완료 시
`ScenarioInteractionSignals.Raise("<cond>")`(= `Registry.Register(RegistryType.RuntimeState, "sig.<cond>", true)`)
를 호출해야 게이트가 통과한다. 이 문서는 84개 신호를 **어디서 Raise 해야 하는지** 정리한다.

관련:
- 헬퍼: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioInteractionSignals.cs`
- 변환 규칙: [`json-conversion-rules.md`](./json-conversion-rules.md) (Validator 섹션)
- 신호 목록(시나리오별): 각 `*.unsupported.flags.json` 의 `gameplaySignalsToRaise`

## 0. 현재 상태(중요)

- 변환된 113개 Validator 는 모두 `onFailure: Ignore` 다. **신호가 없으면 게이트는 그냥 통과(Advance)** 하므로,
  신호 연결 전에도 시나리오는 끝까지 진행된다(데모 가능). 신호를 연결할수록 "수행해야 진행"이 점진 활성화된다.
- **현재 게임플레이 인터랙션 계층에는 완료 이벤트가 없다.** `IInteract.Interact` 는 `void` 이며
  완료 콜백/이벤트가 없다. 또한 다수 인터랙션(아이템 적용/조립/삽입)은 게임플레이 로직 자체가
  아직 구현되지 않았고, 시각효과만 시나리오 핸들러가 생성한다. 따라서 신호 연결은
  "이벤트 구독"이 아니라 **각 완료 지점 직접 계측** 또는 **선행 게임플레이 구현** 이 필요하다.

## 1. 연결 방식 두 가지

- **(권장, 단기) 완료 지점 직접 호출**: 실제 완료 코드가 있는 지점에 `ScenarioInteractionSignals.Raise("<cond>")` 한 줄 삽입.
- **(장기) 인프라 완료 이벤트 추가**: `InteractableEntityResolver`/`Interactable` 가 `Interact` 후
  `event Action<string actor, string interactId, string targetId>` 를 발생시키고, `TriageScenarioEventBootstrap`
  에서 1회 구독하여 매핑. 단 이는 `MultiplayerInfrastructure` 변경이므로 Feature Proposal 필요.

신호 정규화: `Raise` 는 `sig.` 접두사를 자동 부여하므로 `Raise("click_vital_set")` 또는 `Raise("sig.click_vital_set")` 모두 가능.

## 2. 신호 → 완료 지점 매핑(카테고리별)

> 표기: **[있음]** 실제 완료 코드 존재(즉시 계측 가능) / **[부분]** 일부만 존재 / **[없음]** 게임플레이 미구현(선행 구현 필요).

### connect_* (IV/산소/벽 연결) — [있음 → **계측 완료**]
`IntravenousLineConnectionService.TryCompleteConnection` 성공 시 `RaiseConnectionSignals(startPoint, endPoint)`
가 호출되어 다음 3개 신호를 올린다(2026-06-23 구현):
- `sig.<endPoint.Identifier>` (끝점 단독)
- `sig.<startPoint.Identifier>` (시작점 단독)
- `sig.<startPoint.Identifier>__<endPoint.Identifier>` (시작__끝 쌍)

**별도 코드 없이** 시나리오 게이트를 활성화하는 방법: 해당 연결 지점(`IntravenousLineConnectionPoint`)의
인스펙터 `Identifier` 를 시나리오가 기대하는 조건명으로 지정한다. 예) 좌측 캐뉼라+생리식염수 연결
끝점의 Identifier 를 `connect_cannula_and_ns1` 로 지정하면, 연결 완료 시 `sig.connect_cannula_and_ns1` 가
올라가 `Validator(RegistryContains, RuntimeState, "sig.connect_cannula_and_ns1")` 게이트가 통과된다.

대상 신호: `connect_cannula_and_ns1(_patient_b/_c)`, `connect_ambubag_and_connect_o2_to_ambu`,
`connect_blood_to_lv1`, `connect_ps1_to_lv1`, `connect_patient_and_monitor_b(_patient_c)`,
`connect_wall_component_1/2`, `connect_wall_component_and_yankauer`.

> 운영자 작업: 위 조건명에 대응하는 IV/산소/모니터 연결 지점들의 `Identifier` 를 각 조건명으로 설정.
> 코드 변경 불필요(연결 완료 시 자동으로 해당 sig.* 가 올라감).

### grab_* / move_* (들것 잡기/이동) — [부분]
침대/들것 상호작용(`MovingPatientBedController.TryAttachItem`/이동, `PatientController.TryLiftFromBed`/`TryCarryByInteractor`)
성공 지점에 Raise. 대상: `grab_stretcher_a~d`, `grab_stretcher_patient_b/c`, `move_defibcart_to_patient`, `move_patientA`.

### click_patient* / select_* / check_* (환자·신체부위 클릭, 사정) — [부분]
환자/모니터 선택(`PatientMonitorSelectInteract.Interact`, `PatientController.Interactions`) 지점에 Raise.
AVPU/GCS/활력/맥박/동공 사정은 시나리오 Choice/InvokeEvent 흐름과 묶여 있어, 해당 흐름 완료 시
Raise 하거나 사정 UI 확정 콜백에 연결. 대상: `click_patientA`, `click_patient_b/c`, `click_patient_b/c_face`,
`select_patient_b_and_select_patient_c`, `check_*`(7개), `click_chest`, `click_to_start_comp`.

### click_* (아이템 획득) — [부분 → **공통 계측 완료**]
`MedicalItem.OnGet` 을 override 하여, 아이템 획득(인벤토리 추가) 시 `sig.<identifier>` 와
`sig.click_<identifier>` 두 신호를 올린다(2026-06-23 구현, 모든 의료 아이템 공통 1개 지점).
Validator 의 `validationRules` 는 이미 개별 `sig.click_<item>` 다중 룰로 분해되어 있으므로,
조합(`_and_`) 신호 없이 **개별 아이템 획득만으로** 각 룰이 충족된다.

#### 아이템 식별자 ↔ 조건명 정합

`Raise("click_"+identifier)` 기준으로 시나리오 `sig.click_*` 조건과 대조한 결과:

- **자동 일치(19)**: 조건명 == 아이템 식별자 → 픽업 즉시 통과. 예) `18g, 20g, ambubag, defibpad, electrode,
  electrode_cable, gauze, o2_line, penlight, plaster, reservoir_bag, scissors, stylet, suction_line,
  syringe_20cc, syringe_5cc, vital_set, wall_suction, yankauer`.
- **표기 불일치(아이템 픽업) — 정합 완료(2026-06-23)**: 아래 9건의 시나리오 JSON 조건명을
  실제 아이템 식별자로 일괄 변경하여 픽업 즉시 게이트가 통과되도록 했다(`registryIdentifier` 치환).
  | 변경 전 조건명 | 변경 후(= 아이템 식별자) |
  |---|---|
  | `click_glove` | `click_gloves` |
  | `click_et_tube` | `click_endotracheal_tube` |
  | `click_epi` | `click_epinephrine_ampule` |
  | `click_iv_set` | `click_intravenous_set` |
  | `click_ns1` | `click_normal_saline_1000ml` |
  | `click_ns_20cc` | `click_normal_saline_20ml` |
  | `click_laryngo_blade` | `click_laryngoscope_blade` |
  | `click_laryngo_handle` | `click_laryngoscope_handle` |
  | `click_blood` | `click_blood_transfusion_set` |
- **미해결(콘텐츠 갭) — `ps1`(플라즈마 솔루션 1L)**: 시나리오는 `sig.click_ps1` 을 요구하나
  대응하는 아이템이 TriageTrainer 아이템 정의에 **없다**(생리식염수만 존재). 임의 매핑은 추측이므로
  보류한다. **(TODO-CONTENT-1)**: 플라즈마 솔루션 아이템(예: 식별자 `plasma_solution_1000ml`) 정의를
  추가하고 시나리오 조건명을 그 식별자로 맞춘다. 아이템 추가 전까지 해당 게이트는 신호 미발생으로
  자동 통과(onFailure:Ignore)된다.
- **아이템 픽업이 아닌 click 조건(별도 처리)**: `click_chest`, `click_patient_a/b/c`, `click_patient_*_face`,
  `click_patient_chest`, `click_defib`, `click_to_start_comp`, `click_flowmeter`, `click_oxyflow_wall`,
  `click_humidifierbottle`, `click_tpiece`, `click_nasal`, `click_sdw`, `click_neckstabilizer`,
  `click_dummy_a/b` → 환자/장비/더미 클릭 또는 조립 산출물(prepared) 이므로 각 해당 인터랙션 지점에서 Raise.

권장: 표기 불일치 9건은 시나리오 조건명을 아이템 식별자로 통일(JSON 일괄 치환)하는 편이 단순하다.

### apply_* / wear_* / insert_* / push_* / suction_* / remove_* (적용/착용/삽입/주입/흡인/제거) — [없음]
해당 게임플레이 로직 미구현(`MedicalItem.OnUse` 는 no-op, `RaycastTargetEntity()` 는 stub=null).
**선행 게임플레이 구현 후** 완료 지점에 Raise 해야 한다. 대상: `apply_gauze`, `apply_electrode`,
`apply_plaster_on_*`, `apply_stabilizer_patient_a`, `wear_glove`, `insert_iv_*`, `push_epi`, `push_ns`,
`suction_patient_a`, `remove_intu_stylet`, `remove_tpiece`, `start_ambu`.

### pass_* (의사 NPC 전달) — [없음/부분]
아이템을 NPC 에게 건네는 인터랙션. NPC 상호작용 완료 지점 필요. 대상: `pass_laryngoscope`,
`pass_et_tube_ready`, `pass_syringe`, `pass_central_line_set`.

### enter_* / arrive_* (구역 진입) — [있음(다른 의미)]
`ScenarioTriggerZone` 는 "시나리오 시작" 이벤트만 발생시키고 "구역 진입 완료 신호"는 없다.
구역 진입을 게이트로 쓰려면 트리거 존에 진입 카운트 → `Raise("enter_treatmentroom")` 등을 추가해야 한다.
대상: `enter_treatmentroom_count_2`, `enter_triage_zone_count_3`, `arrive_triagearea`.

### click_flowmeter / click_oxyflow_wall / close_vital_ui_* / show_* — [부분]
UI/장비 클릭. 해당 UI 확정 또는 장비 클릭 콜백에 연결. 대상: `click_flowmeter`, `click_oxyflow_wall`,
`close_vital_ui_b/c`, `show_vital_patient_a`, `click_defib`, `click_penlight`, `click_nasal_and_connect_nasal_and_o2`,
`click_o2_line_and_click_tpiece_and_connect_tpiece_and_oxyflow`, `click_scissors_and_remove_patient_clothing`,
`click_defibpad_and_click_patient_chest`, `click_ns_20cc_and_click_syringe_20cc`, `click_ns1_and_iv_set`, `click_ps1_and_iv_set`, `click_ps1_and_click_blood`, `click_20g_and_click_iv_set_and_click_ns1`, `click_dummyA`, `click_dummy_b`.

## 3. 권장 진행 순서

1. **[있음] connect_* 부터** — 실제 완료 코드가 있어 즉시 1줄 계측으로 동작. 가장 빠른 검증.
2. **[부분] grab/move/click_patient/아이템 픽업** — 기존 완료 지점에 Raise 추가.
3. **[없음] apply/insert/push/suction/pass** — 게임플레이 인터랙션 구현이 선행되어야 함(별도 백로그).
4. 중앙 리스너가 필요하면 `TriageScenarioEventBootstrap`(세션 지속 MonoBehaviour)에 두되, 구독할
   완료 이벤트가 없으므로 (장기) 인프라 완료 이벤트 추가는 Feature Proposal 로 분리.

## 4. 사이클 반복 시 신호 리셋

CPR 2사이클처럼 같은 인터랙션을 반복하는 구간은, 사이클 시작 시 `ScenarioInteractionSignals.Clear("<cond>")`
로 이전 신호를 내려야 다음 사이클의 Validator 가 다시 대기한다(필요 구간 한정).
