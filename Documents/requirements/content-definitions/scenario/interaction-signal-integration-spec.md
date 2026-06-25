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

- 변환된 Validator 게이트(patient_a 57개 + patient_b_c 47개 = 104개)는 모두 `onFailure: Ignore`,
  `waitForCondition: true` 다. **신호가 없으면 게이트는 그냥 통과(Advance)** 하므로,
  신호 연결 전에도 시나리오는 끝까지 진행된다(데모 가능). 신호를 연결할수록 "수행해야 진행"이 점진 활성화된다.
- **현재 게임플레이 인터랙션 계층에는 완료 이벤트가 없다.** `IInteract.Interact` 는 `void` 이며
  완료 콜백/이벤트가 없다. 또한 다수 인터랙션(아이템 적용/조립/삽입)은 게임플레이 로직 자체가
  아직 구현되지 않았고, 시각효과만 시나리오 핸들러가 생성한다. 따라서 신호 연결은
  "이벤트 구독"이 아니라 **각 완료 지점 직접 계측** 또는 **선행 게임플레이 구현** 이 필요하다.

### 0.1 게이트 정책 결정(G-2, 2026-06-25)

원본 평가 루브릭의 의도는 "필수 처치를 수행해야 진행"이다. 이를 시스템에서 실현하려면 핵심 게이트가
blocking 이어야 한다. 그러나 신호 배선이 끝나기 전에 `onFailure` 를 `Panic`/`Branching` 으로 바꾸면
미배선 게이트에서 **코루틴이 영구 대기(hang)** 한다(현재 엔진은 `waitForCondition=true` 게이트에
타임아웃·실패 분기가 없음). 따라서 설계 의도를 운영 위험 없이 달성하기 위한 단계적 정책은 다음과 같다.

1. **(현재)** 신호 미배선 구간은 `onFailure: Ignore` 유지 → 데모/수업이 멈추지 않음.
2. **(선행 조건)** `Validator 게이트 타임아웃·실패 분기` 도입
   (제안서: `Agents/Proposals/스케줄됨/2026-06-25-scenario-validator-gate-timeout/`).
   하위호환(미지정 시 기존 동작)으로 hang 위험을 제거한다.
3. **(목표)** 신호가 배선된 핵심 처치 게이트부터 blocking 으로 전환(타임아웃+미수행 기록).
   미수행은 평가 기록(루브릭, G-3)으로 남긴다.

즉, **G-2(수행 강제)는 G-6(게이트 타임아웃) 도입 후에 게이트 단위로 점진 적용**한다.

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

### click_patient* / select_* (환자 선택/이송) — [부분 → **계측 완료**]
`PatientController.RaisePatientInteractionSignals()` 가 다음 3개 완료 지점에서 호출된다(2026-06-23 구현):
- 환자 선택 클릭(`PatientMonitorSelectInteract.Interact`)
- 침대에서 들어올리기(`TryLiftFromBed` 성공)
- 이송 들기(`TryCarryByInteractor` 성공)

올라가는 신호: `sig.<patientId>`, `sig.click_<patientId>`, `sig.select_<patientId>`.
**운영자 작업**: 각 환자(`PatientController`)의 `Identifier` 를 `patient_a` / `patient_b` / `patient_c` 로
지정하면 `click_patient_a`, `select_patient_b` 등의 게이트가 통과된다.

> 주의: `click_patient_b_face/c_face`(동공반사용 얼굴 클릭)와 `click_chest`(가슴압박 위치 클릭),
> `check_*`(AVPU/GCS/활력/맥박/동공 사정), `click_to_start_comp` 는 별도의 신체부위/사정 인터랙션이라
> 위 환자-선택 신호로는 충족되지 않는다. 이들은 해당 신체부위 클릭/사정 UI 확정 지점에서 별도 Raise 필요([부분] 잔여).

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
- **`ps1`(플라즈마 솔루션 1L) — 해결됨(TODO-CONTENT-1 완료, 2026-06-23)**: `PlasmaSolution1000ml`
  아이템(식별자 `plasma_solution_1000ml`)을 신설·등록하고, 시나리오 조건 `sig.click_ps1` →
  `sig.click_plasma_solution_1000ml` 로 정합했다. 이제 모든 아이템 픽업 게이트가 식별자와 1:1 일치한다.
  (단, 아이콘 스프라이트/3D 모델 리소스는 별도 추가 필요 — `ValidateItemResources` 경고 참조.)
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

## 5. 운영자 Editor 정합 체크리스트 (2026-06-25 실측 검증)

실제 JSON 의 게이트 신호(`sig.*`)를 두 시나리오에서 모두 추출하여, 현재 게임플레이 코드가
올리는 신호와 대조한 결과다. 이 체크리스트만 따르면 "코드 변경 없이" 통과시킬 수 있는 게이트가
명확해진다.

### 5.1 아이템 픽업 게이트 — 정합 완료(코드/설정 변경 불필요)
`MedicalItem.OnGet` 이 `sig.click_<Identifier>` 를 올리며, 아래 29개 픽업 게이트는 모두 실제
아이템 `Identifier` 와 1:1 일치함을 확인했다(불일치 0건).

`18g, 20g, ambubag, blood_transfusion_set, defibpad, electrode, electrode_cable,
endotracheal_tube, epinephrine_ampule, gauze, gloves, intravenous_set, laryngoscope_blade,
laryngoscope_handle, normal_saline_1000ml, normal_saline_20ml, o2_line, penlight,
plasma_solution_1000ml, plaster, reservoir_bag, scissors, stylet, suction_line, syringe_20cc,
syringe_5cc, vital_set, wall_suction, yankauer`

- [ ] 운영자 확인사항: 위 아이템 프리팹들이 씬에 배치되어 있고 획득 가능한지.
- [ ] `plasma_solution_1000ml` 아이콘 스프라이트/3D 모델 리소스 추가(`ValidateItemResources` 경고 해소).

### 5.2 환자/연결지점 Identifier 지정 (코드 변경 불필요, 에디터 설정 필수)
아래는 코드는 자동으로 신호를 올리지만, **에디터에서 Identifier 를 조건명으로 맞춰야** 통과한다.

- [ ] `PatientController` Identifier: 환자 A=`patient_a`, B=`patient_b`, C=`patient_c`, 더미=`dummy_b`.
- [ ] IV/산소/벽/모니터 연결지점(`IntravenousLineConnectionPoint`) Identifier 를 조건명으로 지정:
      `connect_cannula_and_ns1`, `connect_cannula_and_ns1_patient_b`, `connect_cannula_and_ns1_patient_c`,
      `connect_wall_component_1`, `connect_wall_component_2`, `connect_wall_component_and_yankauer`,
      `connect_ambubag`, `connect_o2_to_ambu`, `connect_blood_to_lv1`, `connect_ps1_to_lv1`,
      `connect_tpiece_and_oxyflow`, `connect_patient_and_monitor_b`, `connect_patient_and_monitor_patient_c`,
      `connect_nasal_and_o2`.
- [ ] 들것/침대 잡기 지점 Identifier: `grab_stretcher_a~d`, `grab_stretcher_patient_b`, `grab_stretcher_patient_c`.

### 5.3 게임플레이 미구현 — 신호 배선 선행 필요(별도 백로그)
아래 신호는 게임플레이 인터랙션 자체가 없거나 완료 이벤트가 없어, 코드 구현 후 `Raise` 가 필요하다.
이들이 배선되기 전까지 해당 게이트는 `onFailure: Ignore` 로 자동 통과되며 "수행 검사"가 되지 않는다.

- 적용/착용/삽입/주입/흡인/제거: `apply_gauze`, `apply_electrode`, `apply_plaster_on_gauze`,
  `apply_plaster_on_intu`, `apply_stabilizer_patient_a`, `wear_glove`, `insert_iv_patient_a_left`,
  `insert_iv_b_right`, `insert_iv_c_left`, `push_epi`, `push_ns`, `suction_patient_a`,
  `remove_intu_stylet`, `remove_tpiece`, `remove_patient_clothing`, `start_ambu`.
- 의사 NPC 전달: `pass_laryngoscope`, `pass_et_tube_ready`, `pass_syringe`, `pass_central_line_set`.
- 사정 확정: `check_avpu_gcs_patient_a`, `check_pulse_patient_a`, `check_gcs_a_rosc`,
  `check_gcs_patient_b/c`, `check_vital_patient_b/c`, `show_vital_patient_a`, `close_vital_ui_b/c`.
- 신체부위/장비/구역: `click_chest`, `click_patient_chest`, `click_to_start_comp`, `click_defib`,
  `click_flowmeter`, `click_oxyflow_wall`, `click_humidifierbottle`, `click_sdw`, `click_tpiece`,
  `click_neckstabilizer`, `click_nasal`, `click_patient_b_face`, `click_patient_c_face`,
  `click_dummy_b`, `move_defibcart_to_patient`, `arrive_triagearea`, `enter_triage_zone`.

> 검증 방법: 배선 전이라도 `/scenario signal <cond>` 커맨드로 각 게이트가 막히고 열리는지 수동 확인 가능.
