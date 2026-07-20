---
title: "scenario 환자 B/C 지연 처치"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-07-18
flags: ["refactor-required"]
---

# scenario 환자 B/C 지연 처치

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 환자 B/C: 뇌손상 의심 및 좌측 상완 개방성 골절 대응 |
| 요약 | 환자를 처치 구역으로 이동시키고 의식/활력징후 사정, 산소화, 지혈, IV 확보, 동공 반응 확인 후 CT실로 이동한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 B, 환자 C(환자 B와 동일 부상), 더미 B(분류용) |
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
| 퀘스트 | 12개 `Quest_*`가 식별자만 있고 title/content/task definition이 없음 | 빈 inline quest를 만들지 않고 Q-BC-1 해결 후 definition을 참조한다. |
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
환자별로 분리된다. 산소 연결 신호(`connect_nasal_and_o2`)는 실제 연결점 identifier를 환자별로
분리해야 하므로 SIGNAL-BC-3의 남은 producer 작업으로 유지한다.

### 플레이 차단 항목과 보완 위치

| ID | 위치 | 부족한 연결 | 처리 |
|---|---|---|---|
| SPAWN-BC-1 | `SPAWN_B`, `SPAWN_C` | Unity import에서 `patient_b`의 `PatientTypeBMale`, `patient_c`의 `PatientTypeBFemale` prefab이 FishNet `DefaultPrefabObjects`에 등록되지 않아 `PrefabId`가 미할당된 것으로 확인됐다. 현재 상태로 network spawn하면 런타임 `ObjectId 65535` 오류가 발생한다. | Fish-Networking Spawnable Prefabs에 두 원본 prefab을 등록하고 reserialize한 뒤, Production profile에서 각 EntityPreset의 `SpawnablePreset` capability를 다시 증명한다. |
| ROLE-BC-1 | `P009`~`P013` | `ByRole`은 player tag만 사용하지만 NurseA~D와 태그의 선행 매핑이 없다. 또한 `P009` 상위 태그와 `P012`/`P013` 하위 태그가 다르고 `bleeding_control`은 양쪽 환자 그룹에 중복된다. | **인간 판단 필요:** 세션 role→tag 표를 확정하고 Scenario 시작 전 공급 계약으로 선언한다. 한 플레이어가 동시에 양쪽 환자 브랜치에 배정되지 않도록 태그를 배타적으로 구성한다. |
| SIGNAL-BC-1 | `V040_A`/`V040_C`, `V040_B`/`V040_D` | 같은 들것 신호를 두 번 기다린다. 신호가 sticky라 첫 파지 후 두 번째 Validator도 즉시 통과하여 2인 파지를 증명하지 못한다. | `grab_stretcher_patient_b_a/c`, `grab_stretcher_patient_c_b/d`처럼 손잡이별 신호로 분리하고 각 grab point producer에 연결한다. |
| SIGNAL-BC-2 | `COUNT_TRIAGE_ARRIVALS` → `V039` | ~~`enter_triage_zone` 하나의 존재 여부로는 세 명 도착을 셀 수 없다.~~ **해결(2026-07-20):** `ScenarioTriggerZone._perEntitySignalTemplate`(`enter_triage_zone_{id}`)로 진입 환자별 신호를 발신하고, `SignalCounter`(prefix `enter_triage_zone_`, threshold 3)로 인원 수량 게이트를 구성. | `COUNT_TRIAGE_ARRIVALS`가 `patient_b`/`patient_c`/`dummy_b`의 신호 세 개를 세어 `all_triage_patients_arrived`를 발신하고, `V039`가 이를 대기한다. 운영자는 트리아지 구역 존 인스펙터에 `enter_triage_zone_{id}`를 설정해야 한다. |
| SIGNAL-BC-3 | B/C의 장비·처치 Validator | ~~장비 획득·전극·펜라이트·산소·장갑·거즈 신호 22개가 환자 B와 C 흐름에서 재사용된다. B가 올린 신호 때문에 C 흐름이 실제 행동 없이 통과할 수 있다.~~ **부분 해결(2026-07-20):** 거즈·플라스터·비강캐뉼라의 환자별 결과 신호는 `EntityStateSignalBinding`이 `TreatmentApplied` 전이에서 발신하며, 관련 B/C Validator가 이를 대기한다. | 나머지 장비 "획득" 성격 신호, 전극·장갑, 산소 연결 및 SIGNAL-BC-4 producer 배선은 별도 인간 확정/후속. 환자 상태 전이로 표현되는 결과는 `_patient_b`/`_patient_c`로 분리 완료. |
| SIGNAL-BC-4 | `V036`, `V046`, `V048`, `V050`, `V052`~`V055`, `V065`, `V069`, `V071`~`V074` | 문서가 선행 구현 필요로 표시한 신호 producer가 없다. `WaitForCondition=true`이므로 `OnFailure=Ignore`여도 자동 통과하지 않고 무한 대기한다. 일부 120초 `ForceAdvance`는 실패를 숨길 뿐 정상 플레이 검증이 아니다. | 정식 gameplay callback에서 동일 신호를 Raise한다. timeout은 접근성/복구 정책으로만 유지하고 producer 대체로 사용하지 않는다. |
| Q-BC-1 | `Q031`~`Q042_1` | 12개 quest가 식별자만 있어 실제 오버레이 내용과 완료 task가 비어 있다. | 주변 Dialogue와 Validator를 기반으로 별도 quest definition 12개를 작성하고 Add/Remove가 같은 identifier를 참조하게 한다. |
| PRESET-BC-1 | `PRESET_B`, `PRESET_C` | ~~문서가 요구하는 체온과 SpO2는 현재 `PatientMedicalStatePreset` 필드가 아니다.~~ **해결(2026-07-20):** `bodyTemperatureCelsius`, `spo2` 필드를 프리셋 노드/DTO/로더/컨트롤러/스키마에 추가함. | 체온 37.8°, SpO2 93%를 preset에 직접 기입. 모니터 브리지(temperature.t1, numerics/pleth.spo2) 연결 완료. |
| END-BC-1 | `N092` 및 종료 조건 | fade-out 요구가 서술에만 있고 `N092`는 Dialogue 후 종료된다. | fade handler가 확정되면 `E_END_BC_FADE -> N092`를 명시한다. 현재는 종료 메시지는 동작하지만 fade 연출은 미충족으로 기록한다. |

### 변환 승인 조건

- ROLE-BC-1의 역할 태그 공급 계약이 확정되어야 한다.
- SPAWN-BC-1의 FishNet spawnable prefab 등록이 완료되어야 한다.
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

## 역할·태그 정리(예비) (R10, c)

본 시나리오 및 patient_a 계열에서 등장/예정된 역할 태그를 한 곳에 정리한다. **아래는 예비 정리이며, 로직을 조용히 수정하지 않는다.** 병렬 노드의 태그는 저작 원본 그대로 보존한다.

| 역할 태그 | 본 시나리오 사용처(병렬) | 비고 |
|---|---|---|
| `triage_lead` | P009 V040_A(환자 B 그룹) | 시나리오 tags 목록에 포함 |
| `bleeding_control` | P009 V040_A(환자 B 그룹), P011 N052(환자 B), P013 N081(환자 C) | **B그룹·C그룹 중복 사용** |
| `airway_team` | P009 V040_B(환자 C 그룹) | 상위 브랜치 태그 |
| `iv_team` | P009 V040_B(환자 C 그룹) | 상위 브랜치 태그 |
| `neuro_assessment` | P010 N035(환자 B), P012 N064(환자 C) | |
| `vital_team` | P010 N043(환자 B), P012 N072(환자 C) | |
| `pupil_check` | P011 N048(환자 B), P013 N077(환자 C) | |
| `cpr_team` | (본 시나리오 미사용) | patient_a 계열에서 사용 |
| `defib_team` | (본 시나리오 미사용) | patient_a 계열 |
| `medication_team` | (본 시나리오 미사용) | patient_a 계열 |
| `access_support` | (본 시나리오 미사용) | patient_a 계열 |
| `suction_team` | (본 시나리오 미사용) | patient_a 계열 |
| `procedure_team` | (본 시나리오 미사용) | patient_a 계열 |
| `support_team` | (본 시나리오 미사용) | patient_a 계열 |
| `nurse_a` / `nurse_b` / `nurse_c` / `nurse_d` | 플레이어-역할 매핑(RequiredRoleIdentifiers) | 개별 간호사 역할 |

- [ ] c: 역할↔태그 매핑을 한 곳에서 확정 필요. P009(C브랜치) 상위 태그(airway_team/iv_team)와 하위 P012/P013 태그(neuro_assessment/vital_team/pupil_check/bleeding_control) 불일치. bleeding_control 이 환자 B그룹·C그룹에 중복. P009만 matchMode=Any, 나머지 All. 인간 작업자 정리 요망.

## 시그널 배선 상태(요약) (R11, f)

interaction-signal-integration-spec §5.3 기준으로 게이트별 상태를 분류한다.

- **자동 계측 완료**(설정만으로 동작): `enter_triage_zone`(구역 진입, 단 인원수 검증은 별도), `apply_electrode`, `apply_gauze`, `apply_plaster_on_gauze`, `wear_glove`.
- **선행 구현 필요**(게임플레이 미구현, 미배선 시 무한 대기 또는 명시된 timeout 복구): `insert_iv_b_right`, `insert_iv_c_left`, `click_patient_b_face`, `click_patient_c_face`, `click_dummy_b`. 비강캐뉼라 적용은 `NasalCannulaApplied` 상태 바인딩으로 대체했으며, 산소 연결은 별도 연결점 producer가 필요하다. `click_humidifier_bottle`, `click_sterile_distilled_water`, `click_flowmeter`는 `MedicalItem.OnGet()`이 자동 발행한다.
- **구현 완료(런타임 UI)**: `close_vital_ui_b`, `close_vital_ui_c` — `PatientMonitorController`가 닫기 버튼을 만들고, B/C 활성화 이벤트가 패널·모니터를 숨긴 뒤 환자별 signal을 발생시킨다.
- **에디터 Identifier 정합 필요**(코드는 있으나 프리팹/에디터 매핑 확정 필요): `check_gcs_patient_b`, `check_gcs_patient_c`, `check_vital_patient_b`, `check_vital_patient_c`, `click_patient_b`, `click_patient_c`.

### IV-BC-1 — 20G 팔/신호 계약 충돌 (인간 판단 필요)

`PatientController.IntravenousLineCannula`는 캐뉼라 사용을 실제 처리하고 `insert_iv_{patientIdentifier}_{left|right}` 신호를 발생시킨다. 그러나 현재 B/C 그래프는 `insert_iv_b_right`/`insert_iv_c_left`를 기다린다. 또한 기본 구현은 좌측 우선 배정인데, 환자 B의 문서는 우측을 요구하고 B 프리팹의 20G 시각물은 좌측에만 있다. 따라서 단순 signal 별칭이나 Validator 변경은 잘못된 팔의 처치를 정상 완료로 만들 수 있다.

- [ ] 환자 B/C에 실제 사용할 patient prefab(성별·팔 시각물)과 임상 지시의 좌/우를 확정한다.
- [ ] 확정 후 팔별 interaction point 또는 patient별 최초 삽입 팔 설정을 추가하고, 그래프 조건을 실제 producer (`insert_iv_patient_b_right` 등)와 일치시킨다.

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
| **NextIdentifier** | 문자열 | SPAWN_DUMMY_B |

---

### [SPAWN_DUMMY_B] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | SPAWN_DUMMY_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **PresetIdentifier** | 문자열 | dummy_b |
| **SpawnedEntityIdentifier** | 문자열 | dummy_b |
| **NextIdentifier** | 문자열 | PRESET_B |

- [ ] 더미 B(dummy_b)는 분류용 더미이며 처치 노드는 없음(의도). SPAWN_DUMMY_B 스폰만 존재(R12).

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

> R8: 원본은 환자 C를 "환자 B와 동일 부상"으로 명시한다. 현행 JSON/구 md의 divergence(C=무릎 하단 출혈 / 한쪽 팔)는 폐기하고 B와 동일(좌측 상완 개방성 골절 + 두부 손상)로 통일한다.

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
- [ ] 확정요청: 원본은 환자 C를 'B와 동일 부상'으로 명시. 현행 JSON의 C=무릎하단/한쪽팔 divergence는 폐기하고 B와 동일(좌측 상완)로 통일함. JSON 갱신 및 임상 검수 필요.

---

### [E038] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | triage_patient_b_patient_c_dummy_b |
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
| **Condition** | 문자열 | sig.click_dummy_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E040 |

- [ ] f: `click_dummy_b`는 §5.3상 선행 메커닉 필요(신체부위/장비 클릭). 더미 B 분류용 게이트.

---

### [E040] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_dummy_b_ui |
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
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
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
| **Condition** | 문자열 | sig.grab_stretcher_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | V040_C |

- [ ] f: `grab_stretcher_patient_b` 게이트(간호사 A). 배선 상태 확정요청.

---

### [V040_C] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_b (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E043 |

- [ ] f: `grab_stretcher_patient_b` 게이트(간호사 C). V040_A와 동일 시그널을 2회 대기하는 구조 → 2인 동시 파지 검증 배선 정합 필요.

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
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
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
| **DialogueContent** | 문자열 | 간호사 A 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 C 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요. |
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
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
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
| **Condition** | 문자열 | sig.insert_iv_b_right (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | E046 |

- [ ] f: `insert_iv_b_right`는 §5.3상 "선행 메커닉 필요(정맥 삽입 미구현)". 타임아웃 후 `ForceAdvance`되지만 정상 완료 신호 producer가 필요하다.

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
| **Condition** | 문자열 | sig.connect_wall_component_2 (RegistryContains / RuntimeState) |
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
| **Condition** | 문자열 | sig.apply_nasal_cannula_patient_b AND sig.connect_nasal_and_o2 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N057 |

- [x] f: 비강캐뉼라 적용은 `NasalCannulaApplied` → `apply_nasal_cannula_patient_b` 상태 바인딩으로 계측한다. `connect_nasal_and_o2`의 환자별 연결점 producer는 별도 배선이 필요하다.

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

> R8: 환자 C의 부상/활력은 환자 B와 동일(좌측 상완 개방성 골절 + 두부 손상, GCS 13, 동일 활력)로 통일한다. 지혈/거즈 부위는 좌측 상완. 동공 무반응 측은 C 브랜치 저작 원본대로 좌측(pupil_reflex_patient_c). GCS 근력 사정의 좌우 표현은 JSON 저작 원본을 보존한다.

### [V040_B] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | V040_D |

- [ ] f: `grab_stretcher_patient_c` 게이트(간호사 B). 배선 상태 확정요청.

---

### [V040_D] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_D |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | sig.grab_stretcher_patient_c (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | E050 |

- [ ] f: `grab_stretcher_patient_c` 게이트(간호사 D). V040_B와 동일 시그널 2회 대기 구조 → 2인 동시 파지 검증 배선 정합 필요.

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
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
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
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
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
| **Condition** | 문자열 | sig.insert_iv_c_left (RegistryContains / RuntimeState) |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **WaitTimeoutSeconds** | 실수(float) | 120.0 |
| **OnWaitTimeout** | ScenarioValidatorOnWaitTimeout | ForceAdvance |
| **NextIdentifier** | 문자열 | E053 |

- [ ] f: `insert_iv_c_left`는 §5.3상 "선행 메커닉 필요(정맥 삽입 미구현)". 타임아웃 후 `ForceAdvance`되지만 정상 완료 신호 producer가 필요하다.

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
| **Condition** | 문자열 | sig.connect_wall_component_2 (RegistryContains / RuntimeState) |
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
| **Condition** | 문자열 | sig.apply_nasal_cannula_patient_c AND sig.connect_nasal_and_o2 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N086 |

- [x] f: 비강캐뉼라 적용은 `NasalCannulaApplied` → `apply_nasal_cannula_patient_c` 상태 바인딩으로 계측한다. `connect_nasal_and_o2`의 환자별 연결점 producer는 별도 배선이 필요하다.

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
| **NextIdentifier** | 문자열/null | null (종료 노드) |

- 본 노드가 실제 종료 노드이다(NextIdentifier가 null/공백). R9 참조.

## 종료 조건 (R9)

| 항목 | 내용 |
|---|---|
| 종료 노드 | N092 |
| 종료 연출/설명 | 두 환자 모두 CT실 도달 후 최종 브리핑(D058) → CT 이송(E057) → 종료 메시지(N092)로 종료된다. 검은 화면으로 fade out 되며 "시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다." 메세지를 표시하며 종료된다. |

- [ ] b-1 수정: 구 종료조건 표의 D063은 미정의 노드였음. 실제 종료 노드 N092로 정정함(맥락상 CT 이송 후 종료 메시지 노드).

## 후속 확인 체크리스트 (요약)

- [x] R1: 이벤트/시그널/아이템 식별자를 JSON 정본 snake_case로 통일(`triage_patient_b_patient_c_dummy_b`, `show_patient_b_ui`, `move_patient_b`, `move_patients_to_ct`, `b_c_d_to_triage`, `pupil_reflex_patient_c` 등). Validator 조건을 `sig.*` RuntimeState registryIdentifier로 표기. 아이템 정합(`click_gloves`/`click_intravenous_set`/`click_normal_saline_1000ml`) 반영.
- [x] R2: MoveNextBehavior `Immediate`→`Immediately` (E046/E047/E053/E054).
- [x] R3: 플레이어 대면 System 대사 화자를 `시스템`으로 정규화.
- [x] R4: 주석을 checklist(`- [ ]`/`- [x]`) 문법으로 통일.
- [x] R5: CombineItem 노드(A012/A013/A014/A015) 제거 및 재연결. 조합(crafting) 참조 섹션 추가.
- [x] R6: Delay 노드 불필요(도입하지 않음). `Immediate` 오타 잔존 없음.
- [x] R7: PRESET_B/PRESET_C(PatientMedicalStatePreset) 추가. 체온 37.8° 채택.
- [x] R8: 환자 C 부상/활력을 환자 B와 동일(좌측 상완)로 통일.
- [x] R9: 종료 노드 D063 → N092 정정.
- [x] R10: 역할·태그 정리(예비) 섹션 추가, 불일치 항목 checklist 명시.
- [x] R11: Validator 게이트별 배선 상태 주석(자동 계측 완료 / 선행 구현 필요 / 에디터 Identifier 정합 필요).
- [x] R12: 더미 B(dummy_b) 분류용 더미, 처치 노드 없음 명시.
- [ ] JSON 정본 갱신 필요: (1) 체온 37.3→37.8, (2) 환자 C divergence 폐기 및 B와 동일화, (3) PRESET_B/PRESET_C 노드 신설, (4) A012~A015 조합노드 제거 및 재연결. 인간 작업자/임상 검수 요망.
