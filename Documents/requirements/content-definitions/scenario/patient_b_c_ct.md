---
title: "scenario 환자 B/C 지연 처치"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-07-13
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

> 본 문서는 JSON 정본(`Assets/Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json`)을 기준으로 재작성되었다.
> 식별자/이벤트/시그널은 모두 snake_case JSON 정본을 따른다(R1). 활력 체온은 원본(_origin) 기준 37.8°로 통일하였다(R7/e-1).

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
| `humidifierbottle_ready` | `humidifierbottle` + `sterile_distilled_water`(멸균증류수) | 등록필요(재료 미존재로 보류) | 환자 B/C 산소화 선행. 구 `sdw` → 정본 `sterile_distilled_water` 확정 |
| `oxyflowmeter` | `humidifierbottle_ready` + `flowmeter` | 등록필요(재료 미존재로 보류) | 환자 B·C 공용 단일 산출물. 구 `oxyflowmeter_b`/`oxyflowmeter_c` 통합 확정 |

- 상세 레시피/등록 상태는 [crafting-recipes.md](./crafting-recipes.md) 참조.
- [x] 명명충돌/통합 확정요청: `oxyflowmeter_b`/`oxyflowmeter_c` 는 레시피·입력이 동일하므로 단일 `oxyflowmeter` 로 통합 확정(환자 B·C 공용). (crafting-recipes.md §확정 요청 [x], 2026-07-09).
- [ ] 등록 보류: `humidifierbottle_ready`, `oxyflowmeter` 레시피는 재료 아이템(`humidifierbottle`, `sterile_distilled_water`, `flowmeter`) 미존재로 등록 보류. 재료 선행 추가 후 `RegisterAllCombineRecipes()` 등록 필요 — 인간 작업자/후속 작업 (crafting-recipes.md 참조).

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
- **선행 구현 필요**(게임플레이 미구현, 배선 전 자동 통과): `insert_iv_b_right`, `insert_iv_c_left`, `close_vital_ui_b`, `close_vital_ui_c`, `click_patient_b_face`, `click_patient_c_face`, `click_flowmeter`, `click_humidifierbottle`, `click_sterile_distilled_water`(구 `click_sdw`), `click_nasal`, `click_dummy_b`.
- **에디터 Identifier 정합 필요**(코드는 있으나 프리팹/에디터 매핑 확정 필요): `check_gcs_patient_b`, `check_gcs_patient_c`, `check_vital_patient_b`, `check_vital_patient_c`, `click_patient_b`, `click_patient_c`.

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


| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | PRESET_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.PatientMedicalStatePreset |
| **TargetEntityIdentifier** | 문자열 | patient_b |
| **TransitionMode** | PatientMedicalStateTransitionMode | Immediate |
| **Sex** | Sex | Male |
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
| **IsCardiacArrest** | bool | false |
| **NextIdentifier** | 문자열 | PRESET_C |

- 원본 근거: 체온(BT) 37.8°, SpO2 93%. GCS 13(E3/V4/M6), 좌측 동공 무반응(pupil_reflex_patient_b), 좌측 상완 개방성 골절.
- [ ] SpO2/체온 필드가 PatientMedicalStatePreset 스키마에 없음. 활력 UI 이벤트(activate_vital_monitor_ui_patient_b/c)로만 표기됨. 스키마 확장 여부 확정요청.
- [ ] 확정요청: 활력 체온 37.8(원본) vs JSON 37.3 불일치. 원본 기준 37.8 채택함. 임시치 아님(원본 확정치). JSON 갱신 필요. -> 37.3으로 설정하겠습니다.
- [x] d-3: 환자 B/C 상태 사전설정 값 원본(_origin)에서 확인·기록.
> "좌측 상완 부상, 좌측 동공 무반응"으로 설정.

---

### [PRESET_C] PatientMedicalStatePresetNode


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
| **ConsciousnessPupillaryResponse** | PupillaryResponse | Abnormal (우측 무반응) |
| **RespirationAwRR** | 정수 | 24 |
| **RespirationType** | RespirationType | Regular (C# 프로퍼티는 RespirationTypeValue, JSON 키는 respirationType) |
| **PulseRate** | 정수 | 120 |
| **PulseForceType** | BloodPulseForceType | Normal |
| **BloodPressureSystolic** | 정수 | 140 |
| **BloodPressureDiastolic** | 정수 | 86 |
| **SkinColorHue** | SkinColorHue | Normal |
| **SkinTemperatureType** | SkinTemperatureType | Normal |
| **IsCardiacArrest** | bool | false |
| **NextIdentifier** | 문자열 | E038 |

- 부상/활력은 환자 B와 동일(좌측 상완 개방성 골절 + 두부 손상, GCS 13). 동공은 C 브랜치 저작 원본대로 우측 무반응(pupil_reflex_patient_c)을 유지한다. 거즈/지혈 부위는 "좌측 상완"으로 통일.
- [ ] 확정요청: 원본은 환자 C를 'B와 동일 부상'으로 명시. 현행 JSON의 C=무릎하단/한쪽팔 divergence는 폐기하고 B와 동일(좌측 상완)로 통일함. JSON 갱신 및 임상 검수 필요.
> "우측 상완 부상, 우측 동공 무반응"으로 설정.

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

- [ ] f: `click_patient_b`는 §5.3상 신체부위/장비 클릭 계열(에디터 Identifier 정합 필요). 배선 전 `onFailure: Ignore` 자동 통과.

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
| KTAS 2(긴급) | | | N030 |
| KTAS 3(응급) | | | N029_retry_a |
| KTAS 4(준응급) | | | N029_retry_a |
| KTAS 5(비응급) | | | N029_retry_a |

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
| **DialogueContent** | 문자열 | 오답입니다. 활력징후가 안정적이므로 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다. |
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
| KTAS 2(긴급) | | | N032 |
| KTAS 3(응급) | | | N031_retry_c |
| KTAS 4(준응급) | | | N031_retry_c |
| KTAS 5(비응급) | | | N031_retry_c |

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
| V040_A | CC_A_C_patientB_complete | triage_lead, bleeding_control | - | Any |
| V040_B | CC_B_D_patientC_complete | airway_team, iv_team | - | Any |

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
| N035 | CC_A_gcs_patientB | neuro_assessment | - | All |
| N043 | CC_C_vital_patientB | vital_team | - | All |

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
| V(Verbal response, 음성에 반응 있음) | | | N038 |
| P(Pain response, 통증에 반응 있음) | | | N037_retry_a |
| U(Unconsciousness, 반응 없음) | | | N037_retry_a |

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
| 3점(명령) | | | N039 |
| 2점(통증) | | | N038_retry_b |
| 1점(반응 없음) | | | N038_retry_b |

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
| 4점(혼란) | | | N040 |
| 3점(부적절한 답변) | | | N039_retry_c |
| 2점(신음소리) | | | N039_retry_c |
| 1점(반응 없음) | | | N039_retry_c |

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
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 신체의 좌우가 비대칭적이지만 움직임에 대한 명령에 잘 수행합니다. |
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
| 6점(명령 수행) | | | N041 |
| 5점(통증 원인을 치우려고 손을 뻗음) | | | N040_retry_d |
| 4점(통증에 회피) | | | N040_retry_d |
| 3점(이상 굴곡) | | | N040_retry_d |
| 2점(이상 신전) | | | N040_retry_d |
| 1점(반응 없음) | | | N040_retry_d |

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
| 3점(중력에 저항 가능) | | | D040 |
| 2점(중력에 저항 불가, 좌우 운동) | | | N042_retry_e |
| 1점(약간의 근육 수축) | | | N042_retry_e |
| 0점(움직임 없음) | | | N042_retry_e |

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
| **NextIdentifier** | 문자열 | CC_A_gcs_patientB (P010 브랜치 N035 완료 조건) |

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
| **DialogueContent** | 문자열 | 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 6.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V046 |

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

- [ ] f: `close_vital_ui_b`는 §5.3상 "선행 메커닉 필요(모니터 UI 토글 콜백 미구현)". 배선 전 자동 통과.

---

### [Q034_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q034_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_B |
| **NextIdentifier** | 문자열 | CC_C_vital_patientB (P010 브랜치 N043 완료 조건) |

====================================================
# [P010 병렬 종료 및 P011 진입 (환자 B)]
====================================================

### [D041] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | B 환자의 의식상태는 GCS 13점, 근력 우측 5점/좌측 3점이며, 활력징후는 혈압 140/86, 맥박 120, 호흡수 24, 체온 37.3, SpO2 93% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D042 |

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
| N048 | CC_A_pupil_iv_patientB | pupil_check | - | All |
| N052 | CC_C_nasal_pressure_patientB | bleeding_control | - | All |

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

- [ ] f: `click_patient_b_face`는 §5.3상 "선행 메커닉 필요(신체부위 클릭 미구현)". 타임아웃 설정됨. 배선 전 자동 통과.

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
| **DialogueContent** | 문자열 | 우측 동공에 비해 좌측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다. |
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

- [ ] f: `insert_iv_b_right`는 §5.3상 "선행 메커닉 필요(정맥 삽입 미구현)". 타임아웃 설정됨. 배선 전 자동 통과.

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
| **DialogueContent** | 문자열 | 환자의 좌측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다. |
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
| **NextIdentifier** | 문자열 | CC_A_pupil_iv_patientB (P011 브랜치 N048 완료 조건) |

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
| **Condition** | 문자열 | sig.click_humidifierbottle AND sig.click_sterile_distilled_water |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N054 |

> R5: 구 `A012`(CombineItem, humidifierbottle_ready) 노드를 노드 흐름에서 제거하고 V052 → N054 로 재연결한다.
- [ ] 조합은 crafting 시스템으로 처리. 이 지점은 `humidifierbottle_ready` 가 준비되어 있어야 진행. 단 해당 레시피는 재료(`humidifierbottle`, `sterile_distilled_water`) 미존재로 **등록 보류** 상태(crafting-recipes.md 참조).
- [ ] f: `click_humidifierbottle`/`click_sterile_distilled_water`(구 `click_sdw`)는 §5.3상 "선행 메커닉 필요(장비 클릭 미구현)". 배선 전 자동 통과.

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
- [ ] 조합은 crafting 시스템으로 처리. 이 지점은 `oxyflowmeter`(구 `oxyflowmeter_b`) 가 준비되어 있어야 진행. 단 해당 레시피는 재료(`humidifierbottle_ready`, `flowmeter`) 미존재로 **등록 보류** 상태(crafting-recipes.md 참조).
- [x] 명명충돌/통합 확정요청: `oxyflowmeter_b`/`oxyflowmeter_c` 는 단일 `oxyflowmeter` 로 통합 확정(레시피·입력 동일, 환자 B·C 공용)(crafting-recipes.md §확정 요청 [x], 2026-07-09).
- [ ] f: `click_flowmeter`는 §5.3상 "선행 메커닉 필요(장비 클릭 미구현)".

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
| **Condition** | 문자열 | sig.click_nasal AND sig.connect_nasal_and_o2 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N057 |

- [ ] f: `click_nasal`은 §5.3상 "선행 메커닉 필요(장비 클릭 미구현)".

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
| 3L | | | D046 |
| 5L | | | N057_retry |
| 10L | | | N057_retry |
| 15L | | | N057_retry |

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
| **NextIdentifier** | 문자열 | CC_C_nasal_pressure_patientB (P011 브랜치 N052 완료 조건) |

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
| **NextIdentifier** | 문자열 | CC_A_C_patientB_complete (P009 브랜치 V040_A 완료 조건) |

====================================================
# [P009 병렬 브랜치 2] 환자 C 처치 그룹 (플레이어 B, D)
====================================================

> R8: 환자 C의 부상/활력은 환자 B와 동일(좌측 상완 개방성 골절 + 두부 손상, GCS 13, 동일 활력)로 통일한다. 지혈/거즈 부위는 좌측 상완. 동공 무반응 측은 C 브랜치 저작 원본대로 좌측(pupil_reflex_patient_c). GCS 근력 사정의 좌우 표현은 JSON 저작 원본을 보존한다.
> 수정: 환자 C(Scenario2Female)의 부상은 환자 B(Scenario2Male)와 달리, 우측 상완 개방성 골절 + 두부 손상, GCS 13, 동일 활력이다. 지혈/거즈 부위는 우측 상완, 동공 무반응 측은 우측으로 한다. GCS 및 근력 사정의 표현은 수정함.

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
| **DialogueContent** | 문자열 | 처치 구역에 도착했습니다. 간호사 B는 의식상태를, 간호사 D는 활력징후를 사정하세요. |
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
| N064 | CC_B_gcs_patientC | neuro_assessment | - | All |
| N072 | CC_D_vital_patientC | vital_team | - | All |

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
| V(Verbal response, 음성에 반응 있음) | | | N067 |
| P(Pain response, 통증에 반응 있음) | | | N066_retry_a |
| U(Unconsciousness, 반응 없음) | | | N066_retry_a |

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
| 3점(명령) | | | N068 |
| 2점(통증) | | | N067_retry_b |
| 1점(반응 없음) | | | N067_retry_b |

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
| 4점(혼란) | | | N069 |
| 3점(부적절한 답변) | | | N068_retry_c |
| 2점(신음소리) | | | N068_retry_c |
| 1점(반응 없음) | | | N068_retry_c |

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
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 신체의 좌우가 비대칭적이지만 움직임에 대한 명령에 잘 수행합니다. |
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
| 6점(명령 수행) | | | N070 |
| 5점(통증 원인을 치우려고 손을 뻗음) | | | N069_retry_d |
| 4점(통증에 회피) | | | N069_retry_d |
| 3점(이상 굴곡) | | | N069_retry_d |
| 2점(이상 신전) | | | N069_retry_d |
| 1점(반응 없음) | | | N069_retry_d |

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
> 우측 약함 맞습니다!

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
| 3점(중력에 저항 가능) | | | D050 |
| 2점(중력에 저항 불가, 좌우 운동) | | | N071_retry_e |
| 1점(약간의 근육 수축) | | | N071_retry_e |
| 0점(움직임 없음) | | | N071_retry_e |

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
| **NextIdentifier** | 문자열 | CC_B_gcs_patientC (P012 브랜치 N064 완료 조건) |

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
| **DialogueContent** | 문자열 | 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **AutoAdvanceSeconds** | 실수(float) | 6.0 |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | V065 |

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

- [ ] f: `close_vital_ui_c`는 §5.3상 "선행 메커닉 필요(모니터 UI 토글 콜백 미구현)". 배선 전 자동 통과.

---

### [Q039_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q039_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_C |
| **NextIdentifier** | 문자열 | CC_D_vital_patientC (P012 브랜치 N072 완료 조건) |

====================================================
# [P012 병렬 종료 및 P013 진입 (환자 C)]
====================================================

### [D049] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | C 환자의 의식상태는 GCS 13점, 근력 좌측 5점/우측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **PlayTTS** | bool | true |
| **NextIdentifier** | 문자열 | D051 |


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
| N077 | CC_B_pupil_iv_patientC | pupil_check | - | All |
| N081 | CC_D_nasal_pressure_patientC | bleeding_control | - | All |

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

- [ ] f: `click_patient_c_face`는 §5.3상 "선행 메커닉 필요(신체부위 클릭 미구현)". 배선 전 자동 통과.

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
| **DialogueContent** | 문자열 | 좌측 동공에 비해 우측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다. |
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

- [ ] f: `insert_iv_c_left`는 §5.3상 "선행 메커닉 필요(정맥 삽입 미구현)". 타임아웃 설정됨. 배선 전 자동 통과.

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
| **DialogueContent** | 문자열 | 환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다. |
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
| **NextIdentifier** | 문자열 | CC_B_pupil_iv_patientC (P013 브랜치 N077 완료 조건) |

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
| **Condition** | 문자열 | sig.click_humidifierbottle AND sig.click_sterile_distilled_water |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N083 |

> R5: 구 `A014`(CombineItem, humidifierbottle_ready) 노드를 노드 흐름에서 제거하고 V071 → N083 로 재연결한다.
- [ ] 조합은 crafting 시스템으로 처리. 이 지점은 `humidifierbottle_ready` 가 준비되어 있어야 진행. 단 해당 레시피는 재료(`humidifierbottle`, `sterile_distilled_water`) 미존재로 **등록 보류** 상태(crafting-recipes.md 참조).
- [ ] f: `click_humidifierbottle`/`click_sterile_distilled_water`(구 `click_sdw`)는 §5.3상 "선행 메커닉 필요(장비 클릭 미구현)".

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
- [ ] 조합은 crafting 시스템으로 처리. 이 지점은 `oxyflowmeter`(구 `oxyflowmeter_c`) 가 준비되어 있어야 진행. 단 해당 레시피는 재료(`humidifierbottle_ready`, `flowmeter`) 미존재로 **등록 보류** 상태(crafting-recipes.md 참조).
- [x] 명명충돌/통합 확정요청: `oxyflowmeter_b`/`oxyflowmeter_c` 는 단일 `oxyflowmeter` 로 통합 확정(레시피·입력 동일, 환자 B·C 공용)(crafting-recipes.md §확정 요청 [x], 2026-07-09).
- [ ] f: `click_flowmeter`는 §5.3상 "선행 메커닉 필요(장비 클릭 미구현)".

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
| **Condition** | 문자열 | sig.click_nasal AND sig.connect_nasal_and_o2 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **WaitForCondition** | bool | true |
| **NextIdentifier** | 문자열 | N086 |

- [ ] f: `click_nasal`은 §5.3상 "선행 메커닉 필요(장비 클릭 미구현)".

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
| 3L | | | D055 |
| 5L | | | N086_retry |
| 10L | | | N086_retry |
| 15L | | | N086_retry |

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
| **NextIdentifier** | 문자열 | CC_D_nasal_pressure_patientC (P013 브랜치 N081 완료 조건) |

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
| **NextIdentifier** | 문자열 | CC_B_D_patientC_complete (P009 브랜치 V040_B 완료 조건) |

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
