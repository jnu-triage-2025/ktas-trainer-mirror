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
  `waitForCondition: true` 다. **주의: `waitForCondition: true` 게이트는 `onFailure` 를 참조하지 않는다.**
  신호가 끝내 올라오지 않으면 게이트는 통과(Advance)하는 것이 아니라 **무한 대기(hang)** 한다
  (엔진 `ScenarioController.ExecuteValidatorNode`/`ExecuteValidatorGate`). 즉 신호 미배선 구간에서는
  세션이 그 지점에서 멈춘다. 따라서 데모/수업을 멈추지 않으려면 게이트별 타임아웃(`waitTimeoutSeconds`
  + `onWaitTimeout`, 아래 §0.1) 또는 `/scenario signal <cond>` 수동 통과가 필요하다.
- **현재 게임플레이 인터랙션 계층에는 완료 이벤트가 없다.** `IInteract.Interact` 는 `void` 이며
  완료 콜백/이벤트가 없다. 또한 다수 인터랙션(아이템 적용/조립/삽입)은 게임플레이 로직 자체가
  아직 구현되지 않았고, 시각효과만 시나리오 핸들러가 생성한다. 따라서 신호 연결은
  "이벤트 구독"이 아니라 **각 완료 지점 직접 계측** 또는 **선행 게임플레이 구현** 이 필요하다.

### 0.1 게이트 정책 결정(G-2, 2026-06-25)

원본 평가 루브릭의 의도는 "필수 처치를 수행해야 진행"이다. 이를 시스템에서 실현하려면 핵심 게이트가
blocking 이어야 한다. 그러나 신호 배선이 끝나기 전에 핵심 게이트를 강제하면 미배선 게이트에서
**코루틴이 영구 대기(hang)** 한다. 따라서 설계 의도를 운영 위험 없이 달성하기 위한 단계적 정책은 다음과 같다.

1. **(완료)** 신호 미배선 구간은 `onFailure: Ignore`/`onWaitTimeout` 미지정 유지 → 무한 대기 기본값이지만,
   필요 시 게이트별 `waitTimeoutSeconds`로 hang을 제거할 수 있다.
2. **(완료, G-6)** `Validator 게이트 타임아웃·실패 분기` 엔진 도입(2026-06-25, 브랜치 `feat/scenario-validator-gate-timeout`).
   게이트별 `waitTimeoutSeconds`(옵션) + `onWaitTimeout`(`KeepWaiting`/`FailBranch`/`ForceAdvance`/`WarnAndKeepWaiting`)
   추가. 미지정 시 기존 동작(무한 대기) 유지 → 하위호환. 변환 규칙: [`json-conversion-rules.md`](./json-conversion-rules.md) Validator 섹션.
   제안서: `Agents/Proposals/done/2026-06-25-scenario-validator-gate-timeout/`.
3. **(목표)** 신호가 배선된 핵심 처치 게이트부터 `waitTimeoutSeconds`+`onWaitTimeout=ForceAdvance`(또는 `FailBranch`)로
   전환. 타임아웃은 `ScenarioController.OnValidatorWaitTimeout` 이벤트로 평가 기록(루브릭, G-3)에 남긴다.

즉, **G-6(게이트 타임아웃)이 도입되었으므로, G-2(수행 강제)는 신호 배선이 끝난 게이트부터 단위로 점진 적용**한다.

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

### apply_* / wear_* / push_* / suction_* / start_ambu (아이템 사용 기반) — [계측 완료, 2026-06-26]
아이템 사용(Use) 입력 → 조준 대상의 `IItemUseTarget.OnItemUsed` 브리지가 구현되었다
(`PlayerController.UseItem` → `RaycastHitObject` → `IItemUseTarget`). `PatientController` 가 이를 구현하며,
**아이템→(처치 시각 표현 + 신호) 매핑은 코드 하드코딩**(`PatientController.TreatmentDisplay.cs` 의 `ItemUseEffects`)이라
인스펙터 입력 없이 동작한다(Reset 무관).
- 설계: 데이터(`PatientDisplayState`: `DisplayState` 플래그 + `ChildGameObjects`)와 적용(컨트롤러가 플래그 set +
  자식 GameObject `SetActive`)을 분리. 부위 구분은 환자 프리팹 hierarchy 가 반영(컨트롤러는 플래그만 켬).
- **운영자 작업**: 환자 프리팹 `PatientDisplayState.ChildGameObjects` 에 처치 표현 오브젝트 연결 + 아이템/환자
  Identifier 정합. 신호명 입력 불필요. 설정 가이드: [item-apply-signal-setup-guide.md](./item-apply-signal-setup-guide.md).
- 대상: `apply_gauze`, `apply_plaster_on_gauze`, `apply_plaster_on_intu`, `apply_stabilizer_{id}`, `wear_glove`,
  `apply_electrode`, `apply_nasal_cannula`, `suction_{id}`, `start_ambu`, `push_epi`, `push_ns`.

### check_* (사정: 의식/활력/맥박) — [계측 완료, 2026-06-25]
환자를 클릭해 사정을 수행하는 동작을 `PatientController` 의 Assess 인터랙션으로 처리한다. 표준 사정 동작
(`assess_avpu_gcs`/`assess_pulse`/`assess_gcs`/`assess_vital`)은 **코드 기본값으로 자동 노출**되며 각각
`check_avpu_gcs_{id}`/`check_pulse_{id}`/`check_gcs_{id}`/`check_vital_{id}` 를 올린다.
- 비표준 신호(`check_gcs_a_rosc` 등)는 환자 Assess Actions 인스펙터에 `AssessSignal` 로 명시.
- `PatientController.SetAssessActionEnabled(id, bool)` 로 노출 제어.
- 대상: `check_avpu_gcs_patient_a`, `check_pulse_patient_a`, `check_gcs_a_rosc`,
  `check_gcs_patient_b/c`, `check_vital_patient_b/c`.
- 잔여: `show_vital_patient_a`, `close_vital_ui_b/c` 는 바이탈 모니터 UI 열기/닫기 콜백이 필요(미구현).

### insert_* / remove_* (삽입/제거) — [없음]
정맥 캐뉼라 삽입(연결과 구분), 스타일렛/T-piece 제거 등은 전용 메커닉이 없어 선행 구현이 필요하다.
대상: `insert_iv_patient_a_left`, `insert_iv_b_right`, `insert_iv_c_left`, `remove_intu_stylet`, `remove_tpiece`.

### pass_* (의사 NPC 전달) — [없음/부분]
아이템을 NPC 에게 건네는 인터랙션. NPC 상호작용 완료 지점 필요. 대상: `pass_laryngoscope`,
`pass_et_tube_ready`, `pass_syringe`, `pass_central_line_set`.

### enter_* / arrive_* (구역 진입) — [계측 완료, 2026-06-25]
`ScenarioTriggerZone` 에 옵션 필드 `_raiseSignalsOnEnter`(string[]) 가 추가되었다. 플레이어가 존에
진입할 때 지정한 신호들을 `ScenarioInteractionSignals.Raise` 로 올린다(시나리오 시작 여부와 독립).
- **운영자 작업(코드 변경 불필요)**: 해당 구역의 `ScenarioTriggerZone` 인스펙터 `_raiseSignalsOnEnter` 에
  조건명(예: `enter_triage_zone`, `arrive_triagearea`)을 입력한다. 비워 두면 기존 동작(신호 없음) 유지.
- 신호 전용 존(시나리오 그래프 미지정)도 허용된다 → 게이트 통과 전용 트리거로 배치 가능.
대상: `enter_triage_zone`, `arrive_triagearea` (필요 시 `enter_treatmentroom` 등 추가).

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

계측 완료(2026-06-25, 설정만으로 동작):
- 구역 진입: `arrive_triagearea`, `enter_triage_zone` → `ScenarioTriggerZone._raiseSignalsOnEnter`(§2 구역 진입 절).
- 부착형 적용/착용: `apply_gauze`, `apply_electrode`, `apply_plaster_on_*`, `apply_stabilizer_patient_a`,
  `wear_glove` → 환자/침대 프리팹 Attachable Item Visuals `Apply Signal`(§2 apply_* 절).
- 아이템 사용(부착 없음): `push_epi`, `push_ns`, `suction_patient_a`, `start_ambu` → Item Use Signals(§2).
- 사정: `check_avpu_gcs_patient_a`, `check_pulse_patient_a`, `check_gcs_a_rosc`, `check_gcs_patient_b/c`,
  `check_vital_patient_b/c` → 환자 프리팹 **Assess Actions**(`assessSignal`)에 매핑(§2 사정 절).

선행 메커닉 필요(미구현):
- 정맥 삽입/제거: `insert_iv_patient_a_left`, `insert_iv_b_right`, `insert_iv_c_left`,
  `remove_intu_stylet`, `remove_tpiece`, `remove_patient_clothing`.
- 의사 NPC 전달: `pass_laryngoscope`, `pass_et_tube_ready`, `pass_syringe`, `pass_central_line_set`.
- 모니터 UI 토글: `show_vital_patient_a`, `close_vital_ui_b/c`(바이탈 UI 열기/닫기 콜백 필요).
- 신체부위/장비 클릭: `click_chest`, `click_patient_chest`, `click_to_start_comp`, `click_defib`,
  `click_flowmeter`, `click_oxyflow_wall`, `click_humidifierbottle`, `click_sdw`, `click_tpiece`,
  `click_neckstabilizer`, `click_nasal`, `click_patient_b_face`, `click_patient_c_face`,
  `click_dummy_b`, `move_defibcart_to_patient`.

> 검증 방법: 배선 전이라도 `/scenario signal <cond>` 커맨드로 각 게이트가 막히고 열리는지 수동 확인 가능.
