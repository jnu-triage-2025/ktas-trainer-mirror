---
title: "scenario 환자 A 중증 처치"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-07-18
flags: ["refactor-required"]
---

# scenario 환자 A 중증 처치

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 환자 A: 흉부 관통상 및 심정지 대응 |
| 요약 | 환자 A를 처치실로 이동시키고 ABCDE 순서로 처치를 수행한 뒤 ROSC까지 진행한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 A |
| 주요 장소 | 처치실 |
| 리소스 식별자 - 사운드 | tape_sound, oxygen_sound, defib_on_sound, cutting_sound |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_treatment_room |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | SPAWN_A |

- [x] a-3: 콘텐츠 노출 화자명은 `시스템`(한글), 기술/식별자 표기는 `System`(영문)으로 정규화함.
- [x] a-2: MoveNextBehavior/WaitUntil 열거값을 엔진 정본(`Immediately`/`WaitUntilDone`)으로 정규화함(구 md `Immediate` 폐기).
- [x] a-1/a-2: EventIdentifier·Validator 시그널·아이템 식별자를 JSON/C# 정본 snake_case로 통일함.
- [x] a-1 잔여: 병렬 브랜치의 `CompletionConditionIdentifier`는 인간 작업자 코멘트에 따라 `CC_*_patient_a` 계열로 통일함. 이 값은 독립 노드가 아니라 병렬 브랜치 종료 표식이며, 브랜치의 마지막 노드가 이 식별자로 전이할 때 `Parallel` 실행기가 완료로 소비한다(2026-07-18).

## JSON 변환 전 연결성 감사 (2026-07-18)

이 절은 기존 `patient_a_critical.scenario.json`을 참조하지 않고 이 문서만으로 그래프를 재구성한 결과다.
변환기는 아래의 **확정 보완 규칙**을 적용해야 하며, **차단 항목**이 해결되지 않은 상태에서는 플레이 가능
Scenario로 판정하면 안 된다.

### 구조 감사 결과

| 검사 | 결과 | 변환 규칙 |
|---|---|---|
| 시작점 및 도달성 | `SPAWN_A`에서 문서의 337개 노드가 모두 도달 가능 | 시작 노드는 `SPAWN_A`로 고정한다. |
| 일반 전이 | 정의되지 않은 일반 `NextIdentifier`/선택지 대상 없음 | `(end)`만 JSON의 `null`로 변환한다. |
| 병렬 합류 | 6개 `Parallel`과 21개 완료 표식의 시작·종료 연결이 대응됨 | `CC_*`는 `nodes`에 만들지 않고 `CompletionConditionIdentifier` 및 브랜치 마지막 `nextIdentifier`에 같은 문자열로 기록한다. |
| 이벤트 | 문서의 고유 `EventIdentifier` 30개가 모두 `TriageScenarioEventBootstrap`에 등록됨 | `InvokeEvent`로 보존하되 Requirements 검증에서 handler 등록을 필수로 한다. |
| 열거값 | 문서의 `WaitAll`은 현재 엔진/schema에 존재하지 않음 | 모든 병렬 노드의 `WaitMode`를 정본 `All`로 보정했다. |
| 퀘스트 | 23개 퀘스트가 식별자만 있고 제목·본문·완료조건 정의가 없음 | 빈 inline quest를 만들지 않는다. 아래 차단 항목 Q-1 해결 전 JSON 변환 보류. |
| 종료 연결 | `D037` 뒤 fade-out·종료 메시지·다음 Scenario 연결이 서술에만 있음 | 아래 차단 항목 END-1의 인간 결정을 받아 명시 노드를 추가한다. |

### 확정 보완 규칙

1. `RequiredRoleIdentifiers` 열은 현재 `ScenarioParallelBranch` JSON 필드가 아니므로 출력하지 않는다.
   런타임 할당은 `requiredPlayerTags`, `forbiddenPlayerTags`, `requiredPlayerTagsMatchMode`만 사용한다.
2. `FailureNextIdentifier=null`이고 `WaitForCondition=true`인 Validator는 무한 대기 게이트로 유지한다.
   단, 아래 S-1 신호 생산자 계약에 포함되지 않은 신호를 기다리는 Validator는 생성하지 않는다.
3. `Quest_*`를 Add/Remove하는 두 노드는 동일한 대소문자 식별자를 사용해야 한다. 변환 시 임의로
   `questDefinitionIdentifier`로 치환하지 않고, Q-1에서 확정한 quest definition을 참조한다.
4. `CC_*_patientA`였던 합류 표식은 모두 `CC_*_patient_a`로 정규화한다.

### 플레이 차단 항목과 인간 판단 위치

| ID | 위치 | 부족한 연결 | 처리 |
|---|---|---|---|
| SPAWN-A-1 | `SPAWN_A` | Unity import에서 `patient_a`의 `PatientTypeA` prefab이 FishNet `DefaultPrefabObjects`에 등록되지 않아 `PrefabId`가 미할당된 것으로 확인됐다. 현재 상태로 network spawn하면 런타임 `ObjectId 65535` 오류가 발생한다. | Fish-Networking Spawnable Prefabs에 원본 prefab을 등록하고 reserialize한 뒤, Production profile에서 `EntityPreset(patient_a)`의 `SpawnablePreset` capability를 다시 증명한다. |
| ROLE-1 | `P002`, `P003`, `P004`, `P005`, `P006`, `P007` 진입 전 | `ByRole`은 플레이어 태그만 읽지만, 이 문서에는 NurseA~D와 역할 태그를 연결·부여하는 진입 노드가 없다. 태그가 없으면 `Reallocation` 이후에도 브랜치 할당이 보장되지 않는다. | **인간 판단 필요:** 로비/세션이 `triage_lead`, `airway_team`, `neuro_assessment` 등 모든 태그를 선행 부여하는지 확정한다. 아니라면 Scenario 시작 전용 role→tag 바인더 구현 후 `SPAWN_A`의 선행 요구사항으로 둔다. |
| ROLE-2 | `P004`의 `N008`, `N011` 브랜치 | 한 브랜치에 각각 `NurseB, NurseA`와 `NurseD, NurseC` 두 역할을 적었지만 현재 `ByRole`은 한 브랜치에 한 플레이어만 배정한다. `requiredPlayerTagsMatchMode=All`은 두 사람이 아니라 한 사람이 두 태그를 모두 가져야 한다는 뜻이다. | **인간 판단 필요:** (a) 한 명이 전체 브랜치를 수행하도록 역할 표기를 단일화하거나, (b) `N008`의 삽관/산소 및 `N011`의 IV/보조 흐름을 별도 병렬 브랜치와 합류점으로 분리한다. |
| S-1 | `V011_1`, `V013`, `V014_1~V014_4`, `V015`, `V015_1`, `V015_3~V015_4`, `V017_1`, `V018`, `V023_1`, `V024`, `V025~V025_1`, `V027`, `V030`, `V033` | 20개 신호에 실제 gameplay producer가 연결되지 않았다고 문서에 표시되어 있다. 하나라도 생산되지 않으면 해당 Validator에서 영구 정지한다. | 각 노드의 기존 `(b) 선행 구현 필요` 주석을 producer 작업 목록으로 사용한다. 구현 전에는 Debug emitter를 정식 producer로 간주하지 않는다. |
| Q-1 | 모든 `Q006`~`Q030_1` | 23개 `Quest_*`가 표시용 식별자만 있고 title/content/task definition이 없다. 식별자만 가진 inline quest는 빈 오버레이를 만들며 플레이 안내가 느슨해진다. | **인간 콘텐츠 확정 필요:** 각 Add 노드 주변 Dialogue와 이어지는 Validator 조건을 기반으로 title, questContent, task display text를 확정해 별도 quest definition으로 작성한다. Remove 노드는 같은 definition identifier를 사용한다. |
| IV-1 | `V017` / `V017_1` / `V017_3` | ~~18G 2개가 필요한 서술과 신호 3개/`TargetCount` 의미가 일치하지 않는다. 동일 식별자의 두 번째 획득을 `RegistryContains`로 구분할 수 없다.~~ **부분 해결(2026-07-20):** 삽입 단계를 좌/우로 분리. `PatientController.IntravenousLineCannula` 가 삽입 순서로 좌→우를 결정론적 배정하고 `insert_iv_patient_a_left` / `insert_iv_patient_a_right` 신호를 발신(+게이지별 처치 표현). 우측 삽입 게이트 `V017_3` 신설(`N011_3 → V017_3 → E021`). | **획득(V017) 수량 판정은 미해결:** 좌·우 획득 신호 분리 또는 인벤토리 수량 quest condition(`InventoryContains`, `Count=2`)은 여전히 인간 확정 필요. 삽입 좌/우 producer 는 구현 완료. |
| END-1 | `D037` 및 종료 조건 | 문서는 fade-out, 종료 메시지, 다음 Scenario 진행을 요구하지만 `D037 -> (end)`만 정의한다. | **인간 판단 필요:** 다음 Scenario identifier를 확정한다. 이후 `D037 -> END_FADE_OUT -> END_MESSAGE -> START_NEXT_SCENARIO` 연결을 추가하고, 마지막 노드만 `null`로 둔다. |

### 변환 승인 조건

- ROLE-1과 ROLE-2의 할당 정책이 확정되어야 한다.
- SPAWN-A-1의 FishNet spawnable prefab 등록이 완료되어야 한다.
- S-1의 20개 신호에 정식 producer와 동일 식별자가 연결되어야 한다.
- Q-1의 23개 quest definition이 작성되어야 한다.
- IV-1의 18G 수량 판정과 END-1의 다음 Scenario identifier가 확정되어야 한다.
- 변환 후 Requirements Supports에서 NPC/entity/item/event/quest/runtime-signal 요구사항을 컴파일하고,
  Production profile에서 unresolved `Error`가 0개여야 플레이 가능으로 승인한다.

## 조합(crafting) 참조 (d-2)

조합은 게임플레이 **노드가 아니라 시스템**으로 처리한다(`ItemCombineRecipeRegistry`). 구 md의 `CombineItem` 노드(A003~A011)는 이 문서의 노드 흐름에서 **제거**하고, 산출물이 준비(prepared)되어 있다고 전제한다. 상세 레시피는 [`crafting-recipes.md`](./crafting-recipes.md) 참조.

이 시나리오가 요구하는 조합 산출물:

| 산출물(정본) | 입력 | 구 md 노드(제거됨) | 등록 상태 | 명명 주의 |
|---|---|---|---|---|
| `yankauer_suction_ready` | `suction_line` + `yankauer` | A003 | 등록됨(2026-07-09) | 구 `yankauer_ready` → 정본 `yankauer_suction_ready` |
| `laryngoscope` | `laryngoscope_handle` + `laryngoscope_blade` | A004 | 등록됨 | 구 md `laryngo_handle`/`laryngo_blade` → 정본 `laryngoscope_handle`/`laryngoscope_blade` |
| `endotracheal_tube_ready` | `endotracheal_tube` + `stylet` | A005 | 등록됨 | 구 md `et_tube_ready`/`et_tube` → 정본 `endotracheal_tube_ready`/`endotracheal_tube` |
| `humidifier_sterile_distilled_water_bottle` | `humidifier_bottle` + `sterile_distilled_water` | A006 | 등록됨 | 구 `sdw` → 정본 `sterile_distilled_water` |
| `oxyflowmeter` | `humidifier_sterile_distilled_water_bottle` + `flowmeter` | A007 | 등록됨 | 환자 B·C 공용 단일 산출물 |
| `epinephrine_5cc_syringe` | `epinephrine_ampule` + `syringe_5cc` | A008 · A010 | 등록됨(2026-07-09) | 구 md `epi_ready`/`epi`/`epinephrine_syringe` → 정본 `epinephrine_5cc_syringe`/`epinephrine_ampule` |
| `normal_saline_20cc_syringe` | `normal_saline_20ml` + `syringe_20cc` | A009 · A011 | 등록됨(2026-07-09) | 구 `ns_20cc(_ready)` 산출물 제거 → 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1) |
| `normal_saline_intravenous_ready` | `normal_saline_1000ml` + `intravenous_set` | (인트로 사전조합) | 등록됨(2026-07-09) | 구 `ns1_ready` → 정본 `normal_saline_intravenous_ready`(수액 준비물) |
| `plasma_solution_intravenous_ready` | `plasma_solution_1000ml` + `intravenous_set` | (인트로 사전조합) | 등록됨(2026-07-09) | 구 `ps1_ready` → 정본 `plasma_solution_intravenous_ready`(수액 준비물) |

- [x] 명명충돌 확정요청: `et_tube_ready`↔`endotracheal_tube_ready`, `epi_ready`↔`epinephrine_5cc_syringe`, `ns_20cc(_ready)`→`normal_saline_20cc_syringe`, `ns1_ready`→`normal_saline_intravenous_ready`, `ps1_ready`→`plasma_solution_intravenous_ready`, `yankauer_ready`→`yankauer_suction_ready`. C# 정본에 맞춰 시나리오 산출물명 치환 확정(crafting-recipes.md §확정 요청 [x] 참조, 2026-07-09).
- [x] 등록완료 레시피(`yankauer_suction_ready`, `epinephrine_5cc_syringe`, `normal_saline_20cc_syringe`, `normal_saline_intravenous_ready`, `plasma_solution_intravenous_ready`, `laryngoscope`, `endotracheal_tube_ready`)를 `RegisterAllCombineRecipes()`에 추가 완료 (crafting-recipes.md 참조, 2026-07-09).
- [x] 산소화 레시피(`humidifier_sterile_distilled_water_bottle`, `oxyflowmeter`)와 재료(`humidifier_bottle`, `sterile_distilled_water`, `flowmeter`)는 `RegisterAllItems()`/`RegisterAllCombineRecipes()`에 등록됨.

## 환자 A 사전설정 (d-3)

시나리오 시작 시 환자 A 엔티티를 스폰하고 의료 상태를 사전설정한다. 시작 노드는 `SPAWN_A`이다.

### [SPAWN_A] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | SPAWN_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **PresetIdentifier** | 문자열 | patient_a |
| **SpawnedEntityIdentifier** | 문자열 | patient_a |
| **PositionSourceEntityIdentifier** | 문자열/null | null |
| **PositionX / PositionY / PositionZ** | 실수 | 0.0 / 0.0 / 0.0 |
| **NextIdentifier** | 문자열 | PRESET_A |

---

### [PRESET_A] PatientMedicalStatePresetNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | PRESET_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.PatientMedicalStatePreset |
| **TargetEntityIdentifier** | 문자열 | patient_a |
| **Sex** | 문자열 | Male |
| **Age** | 정수 | 35 |
| **ConsciousnessGcs** | 정수 | 8 |
| **ConsciousnessLocLabel** | 문자열 | Stupor |
| **ConsciousnessPupillaryResponse** | 문자열 | Normal |
| **RespirationAwRR** | 정수 | 8 |
| **RespirationType** | 문자열 | Irregular |
| **PulseRate** | 정수 | 140 |
| **PulseForceType** | 문자열 | Weak |
| **BloodPressureSystolic** | 정수 | 70 |
| **BloodPressureDiastolic** | 정수 | 40 |
| **SkinColorHue** | 문자열 | Pale |
| **SkinTemperatureType** | 문자열 | Cold |
| **BodyTemperatureCelsius** | 실수 | 35.9 |
| **Spo2** | 정수 | 82 |
| **IsCardiacArrest** | bool | false |
| **NextIdentifier** | 문자열 | D005 |

- [x] d-3: 환자 A 상태 사전설정 값 명시됨 (원본 _origin 및 JSON 일치).
- [x] 체온(BT) 35.9°, SpO2 82% 프리셋 필드 추가(PatientMedicalStatePreset 스키마 확장 반영).

---

## 시나리오 본문

### [D005] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 A를 처치실로 이동해야 합니다. 플레이어 A, B, C, D는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q006 |


---

### [Q006] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Grab_Stretcher |
| **NextIdentifier** | 문자열 | P002 |


---

### [P002] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P002_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | Q006_1 |

#### [P002_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| V010_A | CC_A_grab | NurseA | triage_lead | - | All |
| V010_B | CC_B_grab | NurseB | airway_team | - | All |
| V010_C | CC_C_grab | NurseC | bleeding_control | - | All |
| V010_D | CC_D_grab | NurseD | iv_team | - | All |


---

### [V010_A] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_A_grab |

#### [V010_A_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_a [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].


---

### [V010_B] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_B_grab |

#### [V010_B_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_b |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_b [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].


---

### [V010_C] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_C_grab |

#### [V010_C_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_c |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_c [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].


---

### [V010_D] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_D |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_D_grab |

#### [V010_D_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_d |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_d [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].


---

### [Q006_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q006_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Grab_Stretcher |
| **NextIdentifier** | 문자열 | E005 |


---

### [E005] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patient_a_to_treatmentroom |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D006 |


---

### [D006] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 활력징후 측정, AVPU 및 GCS 측정, 경추 고정 및 흡인을 시작합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P003 |


---

### [P003] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P003_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D010 |

#### [P003_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N005 | CC_B_vitalcheck_patient_a | NurseB | airway_team | - | All |
| N006 | CC_C_gcs_patient_a | NurseC | neuro_assessment | - | All |
| N007 | CC_D_suction_patient_a | NurseD | suction_team | - | All |


---


<!-- ================= [병렬 브랜치 1] 플레이어 B (활력징후 측정) 흐름 ================= -->

### [N005] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 활력징후를 측정합니다. 활력징후 측정도구를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q007 |


---

### [Q007] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Vital_PatientA |
| **NextIdentifier** | 문자열 | V011 |


---

### [V011] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N005_1 |

#### [V011_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_vital_set |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_vital_set [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N005_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N005_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. 활력징후가 모니터에도 출력됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V011_1 |


---

### [V011_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V011_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E006 |

#### [V011_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.show_vital_patient_a |


- [ ] (b) 선행 구현 필요(미배선): sig.show_vital_patient_a. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [E006] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | activate_vital_monitor_ui_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D007 |


---

### [D007] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 환자 활력징후 출력됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N005_4 |


---

### [N005_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N005_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 혈압 70/40mmHg, 맥박 140회/분 - 약하고 빠름, 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 8.0 |
| **NextIdentifier** | 문자열 | Q007_1 |


---

### [Q007_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q007_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Vital_PatientA |
| **NextIdentifier** | 문자열 | CC_B_vitalcheck_patient_a |


---


<!-- ================= [병렬 브랜치 2] 플레이어 C (AVPU 및 GCS 사정) 흐름 ================= -->

### [N006] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 클릭해 환자의 의식 상태를 사정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | Q008 |


---

### [Q008] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_PatientA |
| **NextIdentifier** | 문자열 | V012 |


---

### [V012] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N006_1 |

#### [V012_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_avpu_gcs_patient_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_avpu_gcs_patient_a [사정(PatientController Assess 자동), spec §5.1~5.3].


---

### [N006_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식 상태(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N006_2 |


---

### [N006_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C004 |


---

### [C004] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C004_Options 표 참조]** |

#### [C004_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) |  | #88AAFF | N006_retry_a |
| V(Verbal response, 음성에 반응 있음) |  | #88AAFF | N006_retry_a |
| P(Pain response, 통증에 반응 있음) |  | #88AAFF | N006_3 |
| U(Unconsciousness, 반응 없음) |  | #88AAFF | N006_retry_a |


---

### [N006_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C004 |


---

### [N006_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C005 |


---

### [C005] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C005_Options 표 참조]** |

#### [C005_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적 반응) |  | #88AAFF | N006_retry_b |
| 3점(구두 명령에 반응) |  | #88AAFF | N006_retry_b |
| 2점(통증에 반응) |  | #88AAFF | N006_4 |
| 1점(반응 없음) |  | #88AAFF | N006_retry_b |


---

### [N006_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 통증 자극에만 반응했음을 유의하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C005 |


---

### [N006_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. "여기가 어디예요?"라고 묻자, 환자는 이해할 수 없는 신음소리만 내고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C006 |


---

### [C006] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C006_Options 표 참조]** |

#### [C006_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) |  | #88AAFF | N006_retry_c |
| 4점(혼란) |  | #88AAFF | N006_retry_c |
| 3점(부적절한 답변) |  | #88AAFF | N006_retry_c |
| 2점(신음소리) |  | #88AAFF | N006_5 |
| 1점(반응 없음) |  | #88AAFF | N006_retry_c |


---

### [N006_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 환자는 알아들을 수 없는 소리만 내고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C006 |


---

### [N006_5] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 팔을 재빨리 굽혀 자극을 피합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C007 |


---

### [C007] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C007_Options 표 참조]** |

#### [C007_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 6점(명령 수행) |  | #88AAFF | N006_retry_d |
| 5점(통증 원인을 치우려고 손을 뻗음) |  | #88AAFF | N006_retry_d |
| 4점(통증에 회피) |  | #88AAFF | N006_6 |
| 3점(이상 굴곡) |  | #88AAFF | N006_retry_d |
| 2점(이상 신전) |  | #88AAFF | N006_retry_d |
| 1점(반응 없음) |  | #88AAFF | N006_retry_d |


---

### [N006_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 통증에 회피하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C007 |


---

### [N006_6] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_6 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS 측정 완료. E2 / V2 / M4 = 총 8점 (Stupor) 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | D008 |


---

### [D008] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | AVPU 중 P이며, 추가 사정한 GCS 결과 8점 확인했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q008_1 |


---

### [Q008_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q008_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_PatientA |
| **NextIdentifier** | 문자열 | CC_C_gcs_patient_a |


---


<!-- ================= [병렬 브랜치 3] 플레이어 D (경추 고정 및 구강 흡인) 흐름 ================= -->

### [N007] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 기도 확보를 위해 환자의 경추를 고정하고 구강 석션을 진행합니다. 경추고정기, 흡인기, 석션 라인, 앙커 팁을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q009 |


---

### [Q009] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Stabilizer_And_Suction_PatientA |
| **NextIdentifier** | 문자열 | E007 |


---

### [E007] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_suction_checklist_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | V013 |


---

### [V013] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E008 |

#### [V013_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_stabilizer_patient_a |
| Registry | Contains | RuntimeState | sig.click_wall_suction |
| Registry | Contains | RuntimeState | sig.click_suction_line |
| Registry | Contains | RuntimeState | sig.click_yankauer |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A003(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A003 → E008 로 재지정함. 산출물 `yankauer_suction_ready` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 `yankauer_ready` → 정본 `yankauer_suction_ready` 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_wall_suction, sig.click_suction_line, sig.click_yankauer [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].

- [x] 목 고정대는 클릭 대상이 아니라 환자에게 사용하는 아이템이다. `cervical_collar` 사용이 기존 환자 표시 상태를 켜고 `sig.apply_stabilizer_patient_a`를 올린다.


---

### [E008] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | hide_suction_checklist_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N007_1 |


---

### [N007_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 경추 고정기를 환자에게 적용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_1 |


---

### [V013_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N007_2 |

#### [V013_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_stabilizer_patient_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_stabilizer_patient_a [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].


---

### [N007_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 흡인기를 벽에 설치하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_2 |


---

### [V013_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N007_3 |

#### [V013_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_wall_component_1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_wall_component_1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [N007_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 앙커 팁을 흡인기에 연결하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_3 |


---

### [V013_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N007_4 |

#### [V013_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_wall_component_and_yankauer |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_wall_component_and_yankauer [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [N007_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 흡인기를 클릭한 뒤 환자를 클릭해 구강 흡인을 진행하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_4 |


---

### [V013_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D009 |

#### [V013_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.suction_patient_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.suction_patient_a [아이템 사용(Item Use Signal), spec §5.1~5.3].


---

### [D009] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 경추 고정 및 구강 흡인 완료했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q009_1 |


---

### [Q009_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q009_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Stabilizer_And_Suction_PatientA |
| **NextIdentifier** | 문자열 | CC_D_suction_patient_a |


---


<!-- ================= [병렬 브랜치 종료 및 합류] ================= -->

### [D010] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식상태는 GCS 8점, 활력징후는 혈압 70/40mmHg, 맥박수 140회/분 (빠르고 약함), 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E009 |


---

### [E009] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | vitalinfo_1_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D011 |


---

### [D011] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 기도 확보를 위해 intubation을 시행하겠습니다. 간호사 B 선생님은 삽관 보조해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D012 |


---

### [D012] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 그동안 간호사 C 선생님은 멸균장갑을 착용하고 거즈로 출혈부위를 지혈해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D013 |


---

### [D013] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 D 선생님은 수액 투여를 위해 양팔에 IV 라인 확보해주세요. 혈관을 보고 18게이지로 잡고, 수액은 생리식염수와 플라즈마 솔루션 달겠습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P004 |


---

### [P004] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P004_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D022 |

#### [P004_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N008 | CC_B_intubation_A_oxy_patient_a | NurseB, NurseA | airway_team, triage_lead | - | All |
| N010 | CC_C_stopbleeding_patient_a | NurseC | bleeding_control | - | All |
| N011 | CC_D_iv_patient_a | NurseD, NurseC | iv_team, access_support | - | All |


---


<!-- ================= [P004 병렬 브랜치 1] 플레이어 B & A (기관내삽관 및 산소 공급) ================= -->

### [N008] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 기관내삽관에 필요한 물품을 준비합니다. 좌측 체크리스트 창을 참고하여 필요한 물품을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q010 |


---

### [Q010] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Intubation_PatientA |
| **NextIdentifier** | 문자열 | E010 |


---

### [E010] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_checklist_intu |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | V014 |


---

### [V014] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E011 |

#### [V014_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_laryngoscope_blade |
| Registry | Contains | RuntimeState | sig.click_laryngoscope_handle |
| Registry | Contains | RuntimeState | sig.click_endotracheal_tube |
| Registry | Contains | RuntimeState | sig.click_stylet |
| Registry | Contains | RuntimeState | sig.click_plaster |
| Registry | Contains | RuntimeState | sig.click_syringe_5cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A004(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A004 → E011 로 재지정함. 산출물 `laryngoscope` 는 레지스트리 등록 완료, 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 입력 식별자 `laryngo_handle`/`laryngo_blade` → 정본 `laryngoscope_handle`/`laryngoscope_blade` 확정(crafting-recipes.md §확정 요청 [x]).


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_laryngo_blade→click_laryngoscope_blade, click_laryngo_handle→click_laryngoscope_handle, click_et_tube→click_endotracheal_tube (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_laryngoscope_blade, sig.click_laryngoscope_handle, sig.click_endotracheal_tube, sig.click_stylet, sig.click_plaster, sig.click_syringe_5cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [E011] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | hide_checklist_intu |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N008_1 |


---

### [N008_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 후두경을 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_1 |


---

### [V014_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N008_2 |

#### [V014_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_laryngoscope |


- [ ] (b) 선행 구현 필요(미배선): sig.pass_laryngoscope. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [N008_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 기관내관을 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_2 |


---

### [V014_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E012 |

#### [V014_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_et_tube_ready |


- [ ] (b) 선행 구현 필요(미배선): sig.pass_et_tube_ready. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [E012] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_et_tube |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N008_3 |


---

### [N008_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 구강에 삽입된 기관내관을 클릭해 스타일렛을 제거하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_3 |


---

### [V014_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E013 |

#### [V014_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.remove_intu_stylet |


- [ ] (b) 선행 구현 필요(미배선): sig.remove_intu_stylet. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [E013] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | remove_stylet |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N008_4 |


---

### [N008_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 5cc 주사기를 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_4 |


---

### [V014_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N008_5 |

#### [V014_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_syringe |


- [ ] (b) 선행 구현 필요(미배선): sig.pass_syringe. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [N008_5] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 플라스터를 클릭해 선택한 뒤, 삽입된 기관내관을 고정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_5 |


---

### [V014_5] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | S001 |

#### [V014_5_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_plaster_on_intu |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_plaster_on_intu [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].


---

### [S001] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | Q010_1 |


---

### [Q010_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q010_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Intubation_PatientA |
| **NextIdentifier** | 문자열 | D014 |


---

### [D014] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 삽입된 깊이 23cm, 기관내관 고정되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D015 |


---

### [D015] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 삽관이 끝났고, 자발호흡이 있으니 간호사 A 선생님이 T-piece 연결하고 산소 10L 주면서 산소포화도 모니터링 해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009 |


---

### [N009] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q011 |


---

### [Q011] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Oxygen_PatientA |
| **NextIdentifier** | 문자열 | V015 |


---

### [V015] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_1 |

#### [V015_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_humidifier_bottle |
| Registry | Contains | RuntimeState | sig.click_sterile_distilled_water |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A006(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A006 → N009_1 로 재지정함. 산출물 `humidifier_sterile_distilled_water_bottle` 레시피는 등록되어 있다(crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 입력 식별자 `sdw` → 정본 `sterile_distilled_water`, `humidifierbottle` → `humidifier_bottle`로 확정(crafting-recipes.md 참조).


- [x] `sig.click_humidifier_bottle`, `sig.click_sterile_distilled_water`는 `MedicalItem.OnGet()`이 아이템 획득 시 자동 발행한다.


---

### [N009_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 유량계를 습득하여 산소 유량계를 완성합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V015_1 |


---

### [V015_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_2 |

#### [V015_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_flowmeter |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A007(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A007 → N009_2 로 재지정함. 산출물 `oxyflowmeter` 레시피는 등록되어 있다(crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 `oxyflowmeter` 단일 식별자로 확정(환자 B·C 공용, `oxyflowmeter_b`/`oxyflowmeter_c` 미분리)(crafting-recipes.md 참조).


- [x] `sig.click_flowmeter`는 `MedicalItem.OnGet()`이 아이템 획득 시 자동 발행한다.


---

### [N009_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V015_2 |


---

### [V015_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_3 |

#### [V015_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_wall_component_2 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_wall_component_2 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [N009_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소줄과 T-piece를 각각 클릭해 획득하고, 산소 유량계와 환자에게 삽입된 기관내관을 각각 클릭해 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V015_3 |


---

### [V015_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E014 |

#### [V015_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_o2_line |
| Registry | Contains | RuntimeState | sig.interact_tpiece |
| Registry | Contains | RuntimeState | sig.connect_tpiece_and_oxyflow |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_o2_line, sig.connect_tpiece_and_oxyflow [아이템 픽업(MedicalItem.OnGet 자동) / 연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].

- [ ] T-piece 오브젝트에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_tpiece`로 설정한다.


---

### [E014] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_tpiece_ready |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N009_4 |


---

### [N009_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V015_4 |


---

### [V015_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | C008 |

#### [V015_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_oxyflow_wall |


- [ ] 벽 유량계 `WallAttachedOxyflowmeter`의 Attach Completion Signal을 `interact_oxyflow_wall`로 설정한다.


---

### [C008] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C008_Options 표 참조]** |

#### [C008_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 3L |  | #88AAFF | N009_retry |
| 5L |  | #88AAFF | N009_retry |
| 10L |  | #88AAFF | D016 |
| 15L |  | #88AAFF | N009_retry |


---

### [N009_retry] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_retry |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 처방은 10L 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | C008 |


---

### [D016] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 산소 투여 시작했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q011_1 |


---

### [Q011_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q011_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Oxygen_PatientA |
| **NextIdentifier** | 문자열 | CC_B_intubation_A_oxy_patient_a |


---


<!-- ================= [P004 병렬 브랜치 2] 플레이어 C (지혈) ================= -->

### [N010] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q012 |


---

### [Q012] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_PatientA |
| **NextIdentifier** | 문자열 | V016 |


---

### [V016] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N010_1 |

#### [V016_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_gloves |
| Registry | Contains | RuntimeState | sig.click_gauze |
| Registry | Contains | RuntimeState | sig.click_plaster |


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_glove→click_gloves (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_gloves, sig.click_gauze, sig.click_plaster [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N010_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 멸균장갑을 마우스 우클릭으로 착용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V016_1 |


---

### [V016_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N010_2 |

#### [V016_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.wear_glove |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.wear_glove [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].


---

### [N010_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V016_2 |


---

### [V016_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E015 |

#### [V016_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_gauze |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_gauze [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].


---

### [E015] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N010_3 |


---

### [N010_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 압박을 가해 지혈하고 있습니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V016_3 |


---

### [V016_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E016 |

#### [V016_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_plaster_on_gauze |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_plaster_on_gauze [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].


---

### [E016] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_with_plaster_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | S002 |


---

### [S002] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D017 |


---

### [D017] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 지혈 중입니다. 거즈 고정했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q012_1 |


---

### [Q012_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q012_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_PatientA |
| **NextIdentifier** | 문자열 | CC_C_stopbleeding_patient_a |


---


<!-- ================= [P004 병렬 브랜치 3] 플레이어 D & C (IV 라인 및 C-line 보조) ================= -->

### [N011] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 좌측과 우측 팔에 IV 라인을 확보해야 합니다. 18게이지 2개, 준비된 생리식염수 1L 수액백, 준비된 플라즈마 솔루션 1L 수액백을 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | Q013 |


---

### [Q013] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_IV_Line_PatientA |
| **NextIdentifier** | 문자열 | E017 |


---

### [E017] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_iv_checklist |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | V017 |


---

### [V017] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E018 |

#### [V017_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_18g |
| Registry | Contains | RuntimeState | sig.click_normal_saline_1000ml |
| Registry | Contains | RuntimeState | sig.click_plasma_solution_1000ml |


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_ns1→click_normal_saline_1000ml, click_ps1→click_plasma_solution_1000ml (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_18g, sig.click_normal_saline_1000ml, sig.click_plasma_solution_1000ml [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].
- [ ] 개수 불일치 확정요청: 구 md는 TargetCount 4(18G 2개 + 수액 2종)였으나 JSON 정본은 시그널 룰 3개(`click_18g`, `click_normal_saline_1000ml`, `click_plasma_solution_1000ml`)로 축약됨. 18G 2개를 각각 계측할지, 준비된 생리식염수/플라즈마 수액세트(정본 `normal_saline_intravenous_ready`/`plasma_solution_intravenous_ready`, 구 `ns1_ready`/`ps1_ready`, 레지스트리 등록 완료 2026-07-09)를 픽업 완료로 간주할지 확정 필요.

---

### [E018] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | hide_iv_checklist |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N011_1 |


---

### [N011_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V017_1 |


---

### [V017_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E019 |

#### [V017_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.insert_iv_patient_a_left |


- [x] (b) 배선 완료(2026-07-20): `sig.insert_iv_patient_a_left` 는 `PatientController.IntravenousLineCannula.PerformIntravenousLineCannulaInsertion` 이 좌측(첫 삽입) 확정 시 발신한다. 우측은 `sig.insert_iv_patient_a_right`(V017_3 게이트) 로 이어진다. 게이지(18G/20G)별 처치 표현(`Syringe{18G|20G}InsertedInto{Left|Right}Arm`)도 함께 켜진다.


---

### [E019] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_18g_left |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N011_2 |


---

### [N011_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 18G 캐뉼라를 클릭해 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V017_2 |


---

### [V017_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E020 |

#### [V017_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_cannula_and_ns1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_cannula_and_ns1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [E020] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ns1_left |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N011_3 |


---

### [N011_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 한쪽 정맥로가 확보되어, 반대쪽 팔에도 자동으로 18G 캐뉼라 및 플라즈마 솔루션 연결이 진행됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | E021 |


---

### [E021] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_18g_right |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | E022 |


---

### [E022] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ps1_right |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D018 |


---

### [D018] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 양측 정맥로가 모두 확보되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q013_1 |


---

### [Q013_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q013_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_IV_Line_PatientA |
| **NextIdentifier** | 문자열 | D019 |


---

### [D019] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 그래도 혈압이 잡히지 않네요. C-line 잡아서 수액을 빠르게 투여하겠습니다. 간호사 C 선생님, C-line set 건네주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N012 |


---

### [N012] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | C-line set을 클릭해 획득하고, 해당 아이템을 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q014 |


---

### [Q014] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cline_Assist |
| **NextIdentifier** | 문자열 | V018 |


---

### [V018] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E023 |

#### [V018_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_central_line_set |


- [ ] (b) 선행 구현 필요(미배선): sig.pass_central_line_set. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [E023] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_central_line_set |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | Q014_1 |


---

### [Q014_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q014_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cline_Assist |
| **NextIdentifier** | 문자열 | D020 |


---

### [D020] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 C 선생님, Level 1 rapid infuser에 플라즈마 솔루션과 혈액백 연결시켜주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N013 |


---

### [N013] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 플라즈마 솔루션 1L 수액백과 혈액백을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | Q015 |


---

### [Q015] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Lv1_Fluids |
| **NextIdentifier** | 문자열 | V019 |


---

### [V019] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N013_1 |

#### [V019_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_plasma_solution_1000ml |
| Registry | Contains | RuntimeState | sig.click_blood_transfusion_set |


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_blood→click_blood_transfusion_set (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_plasma_solution_1000ml, sig.click_blood_transfusion_set [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N013_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N013_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 플라즈마 솔루션 1L 수액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V019_1 |


---

### [V019_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V019_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N014 |

#### [V019_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_ps1_to_lv1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_ps1_to_lv1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [N014] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 혈액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V020 |


---

### [V020] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E024 |

#### [V020_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_blood_to_lv1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_blood_to_lv1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [E024] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | lv1_ready |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D021 |


---

### [D021] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | Level 1에 플라즈마 솔루션과 혈액백 연결 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q015_1 |


---

### [Q015_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q015_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Lv1_Fluids |
| **NextIdentifier** | 문자열 | CC_D_iv_patient_a |


---


<!-- ================= [P004 병렬 종료 및 환자 악화 시점] ================= -->

### [D022] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 그래도 혈압이 잘 안잡히네요... |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E025 |


---

### [E025] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | patient_crash_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D023 |


---

### [D023] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 심전도만 출력되고, 다른 활력징후가 출력되지 않습니다. 간호사 B 선생님, 환자 맥박 확인해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N016 |


---

### [N016] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 경동맥을 촉지해 맥박을 확인합니다. 목 부위를 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q018 |


---

### [Q018] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse |
| **NextIdentifier** | 문자열 | V022 |


---

### [V022] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q018_1 |

#### [V022_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_pulse_patient_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_pulse_patient_a [사정(PatientController Assess 자동), spec §5.1~5.3].


---

### [Q018_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q018_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse |
| **NextIdentifier** | 문자열 | D024 |


---

### [D024] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 맥박 없습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D025 |


---

### [D025] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | PEA입니다. CPR 하겠습니다. 제가 팀 리더를 맡겠습니다. 간호사 A 선생님은 앰부백 짜주시고, 간호사 B 선생님은 가슴압박 해주세요. 간호사 C 선생님은 제세동기 연결해주시고, 간호사 D 선생님은 C-line으로 에피네프린 1mg 투여해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P005 |


---

### [P005] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P005_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D028 |

#### [P005_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N017 | CC_A_ambu | NurseA | airway_team | - | All |
| N018 | CC_B_chestcomp | NurseB | cpr_team | - | All |
| N019 | CC_C_defib | NurseC | defib_team | - | All |
| N020 | CC_D_epi | NurseD | medication_team | - | All |


---


<!-- ================= [P005 병렬 브랜치 1] 플레이어 A (앰부백 산소화) ================= -->

### [N017] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백과 산소 저장낭을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q019 |


---

### [Q019] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_A |
| **NextIdentifier** | 문자열 | V023 |


---

### [V023] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N017_1 |

#### [V023_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_ambubag |
| Registry | Contains | RuntimeState | sig.click_reservoir_bag |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_ambubag, sig.click_reservoir_bag [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N017_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자에게 연결된 T-piece를 클릭해 연결을 해제하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V023_1 |


---

### [V023_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N017_2 |

#### [V023_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.remove_tpiece |


- [ ] (b) 선행 구현 필요(미배선): sig.remove_tpiece. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [N017_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백을 클릭해 선택한 뒤, 환자에게 삽입된 기관내관을 클릭해 연결하세요. 이후, 산소줄과 앰부백을 클릭해 연결합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V023_2 |


---

### [V023_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E026 |

#### [V023_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_ambubag |
| Registry | Contains | RuntimeState | sig.connect_o2_to_ambu |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_ambubag, sig.connect_o2_to_ambu [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [E026] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_ambu_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | C009 |


---

### [C009] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C009_Options 표 참조]** |

#### [C009_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| Full |  | #88AAFF | S003 |


---

### [S003] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | oxygen_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | N017_3 |


---

### [N017_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백을 클릭해 산소 공급을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | V023_4 |


---

### [V023_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E027 |

#### [V023_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.start_ambu |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.start_ambu [아이템 사용(Item Use Signal), spec §5.1~5.3].


---

### [E027] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_ambubagging |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L001 |


---

### [L001] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C010 |


---

### [C010] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 산소 제공량은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C010_Options 표 참조]** |

#### [C010_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 1500ml (다섯 손가락 모두를 이용해 백을 짠다) |  | #88AAFF | N017_retry_a |
| 약 600ml (엄지, 검지, 중지를 이용해 백을 짠다) |  | #88AAFF | L002 |


---

### [N017_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C010 |


---

### [L002] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C011 |


---

### [C011] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 심폐소생술 시 앰부 배깅(ambu-bagging)의 적절한 속도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C011_Options 표 참조]** |

#### [C011_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 10초에 1번 (분당 약 6회) |  | #88AAFF | N017_retry_b |
| 6초에 1번 (분당 약 10회) |  | #88AAFF | Q019_1 |
| 3초에 1번 (분당 약 20회) |  | #88AAFF | N017_retry_b |


---

### [N017_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 6초에 1번씩 눌러야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C011 |


---

### [Q019_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q019_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_A |
| **NextIdentifier** | 문자열 | CC_A_ambu |


---


<!-- ================= [P005 병렬 브랜치 2] 플레이어 B (가슴 압박) ================= -->

### [N018] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 가슴을 클릭해 가슴압박을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q020 |


---

### [Q020] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_B |
| **NextIdentifier** | 문자열 | V024 |


---

### [V024] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E028 |

#### [V024_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_to_start_comp |


- [ ] (b) 선행 구현 필요(미배선): sig.click_to_start_comp. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [E028] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_chest_compression |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L003 |


---

### [L003] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C012 |


---

### [C012] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 가슴 압박 깊이는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C012_Options 표 참조]** |

#### [C012_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 4cm |  | #88AAFF | N018_retry_a |
| 약 5cm |  | #88AAFF | L004 |
| 약 6cm |  | #88AAFF | N018_retry_a |


---

### [N018_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C012 |


---

### [L004] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C013 |


---

### [C013] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 성인의 정확한 가슴 압박 위치는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C013_Options 표 참조]** |

#### [C013_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 양측 유두선상의 중간지점 |  | #88AAFF | N018_retry_b |
| 흉골 하부 1/2 지점 |  | #88AAFF | L005 |


---

### [N018_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C013 |


---

### [L005] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C014 |


---

### [C014] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 정확한 가슴 압박 횟수는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C014_Options 표 참조]** |

#### [C014_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 분당 약 80~100회 |  | #88AAFF | N018_retry_c |
| 분당 약 100~120회 |  | #88AAFF | L006 |
| 분당 약 120~140회 |  | #88AAFF | N018_retry_c |


---

### [N018_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C014 |


---

### [L006] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C015 |


---

### [C015] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 4. 가슴압박 시 주의사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C015_Options 표 참조]** |

#### [C015_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 지쳐도 한 사람이 계속 가슴압박을 수행한다. |  | #88AAFF | N018_retry_d |
| 뼈가 부러진 것 같으면 멈춘다. |  | #88AAFF | N018_retry_d |
| 충분한 이완을 제공한다. |  | #88AAFF | Q020_1 |


---

### [N018_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 혈액 순환이 가능합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C015 |


---

### [Q020_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q020_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_B |
| **NextIdentifier** | 문자열 | CC_B_chestcomp |


---


<!-- ================= [P005 병렬 브랜치 3] 플레이어 C (제세동기) ================= -->

### [N019] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 제세동 카트를 환자 옆으로 가져오세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q021 |


---

### [Q021] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_C |
| **NextIdentifier** | 문자열 | V025 |


---

### [V025] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N019_1 |

#### [V025_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.move_defibcart_to_patient |


- [ ] (b) 선행 구현 필요(미배선): sig.move_defibcart_to_patient. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [N019_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 제세동 패드를 획득하고, 환자 흉부를 클릭해 부착하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V025_1 |


---

### [V025_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V025_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E029 |

#### [V025_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_defibpad |
| Registry | Contains | RuntimeState | sig.interact_patient_chest |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_defibpad [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].

- [ ] 환자 A 흉부 부위 collider에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_patient_chest`로 설정한다.


---

### [E029] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | attach_defibpad |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | E030 |


---

### [E030] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | defib_ui_irregular |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D026 |


---

### [D026] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 제세동기 준비가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L007 |


---

### [L007] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C016 |


---

### [C016] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 제세동기는 Sync 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 다음 중 제세동을 실시해야 하는 심전도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C016_Options 표 참조]** |

#### [C016_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| Asystole(무수축) |  | #88AAFF | N019_retry_a |
| PEA(무맥성 전기활동) |  | #88AAFF | N019_retry_a |
| VT(맥박이 있는 심실빈맥) |  | #88AAFF | N019_retry_a |
| VF(심실세동) |  | #88AAFF | L008 |


---

### [N019_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 VF(심실세동) 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C016 |


---

### [L008] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C017 |


---

### [C017] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C017_Options 표 참조]** |

#### [C017_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 150~200J(줄) |  | #88AAFF | L009 |
| 360J(줄) |  | #88AAFF | N019_retry_b |


---

### [N019_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 150~200J(줄)이 정답입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C016 |


---

### [L009] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C018 |


---

### [C018] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 제세동 등 전기충격 시 주의해야 할 사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C018_Options 표 참조]** |

#### [C018_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 꼬인 수액 줄을 풀어준다. |  | #88AAFF | N019_retry_c |
| 의료진이 손을 대어도 괜찮다. |  | #88AAFF | N019_retry_c |
| 의사의 지시가 있을 때에만 실시한다. |  | #88AAFF | N019_retry_c |
| 전기충격 전 모두 환자에게서 떨어지도록 지시한다. |  | #88AAFF | Q021_1 |


---

### [N019_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 감전되지 않도록 모두가 떨어지도록 지시해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C017 |


---

### [Q021_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q021_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_C |
| **NextIdentifier** | 문자열 | CC_C_defib |


---


<!-- ================= [P005 병렬 브랜치 4] 플레이어 D (에피네프린 투여) ================= -->

### [N020] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q022 |


---

### [Q022] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_D |
| **NextIdentifier** | 문자열 | V026 |


---

### [V026] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N020_1 |

#### [V026_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_epinephrine_ampule |
| Registry | Contains | RuntimeState | sig.click_syringe_5cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A008(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A008 → N020_1 로 재지정함. 산출물 `epinephrine_5cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 구 `epi_ready`/`epinephrine_syringe` → 정본 `epinephrine_5cc_syringe` 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_epi→click_epinephrine_ampule (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_epinephrine_ampule, sig.click_syringe_5cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N020_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | Push용 생리식염수를 준비합니다. 20cc 생리식염수와 20cc 주사기를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V026_1 |


---

### [V026_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N020_2 |

#### [V026_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_normal_saline_20ml |
| Registry | Contains | RuntimeState | sig.click_syringe_20cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A009(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A009 → N020_2 로 재지정함. 산출물 `normal_saline_20cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 구 `ns_20cc(_ready)` 산출물은 명명 규칙 위배로 제거, 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1)로 대체 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_ns_20cc→click_normal_saline_20ml (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_normal_saline_20ml, sig.click_syringe_20cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N020_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V026_2 |


---

### [V026_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D027 |

#### [V026_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_epi |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_epi [아이템 사용(Item Use Signal), spec §5.1~5.3].

> OR 게이트 처리(B, 우선 채택): 에피네프린 주사기는 완제품이 18종(용량·게이지 변형)으로 존재하나,
> 이 게이트는 개별 변형 픽업이 아니라 **사용 시점 대표 시그널 `sig.push_epi`** 로 검사하므로 어떤 변형을
> 조합·투여했든 통과한다(OR 자연 성립). 정책 근거: `interaction-signal-integration-spec.md` §6,
> `crafting-recipes.md` §확정 요청 (*1).


---

### [D027] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 에피네프린 1mg 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N020_3 |


---

### [N020_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V026_3 |


---

### [V026_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D027_1 |

#### [V026_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_ns |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_ns [아이템 사용(Item Use Signal), spec §5.1~5.3].


---

### [D027_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D027_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 생리식염수 20cc 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L010 |


---

### [L010] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C019 |


---

### [C019] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C019_Options 표 참조]** |

#### [C019_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 1~2분에 한 번 |  | #88AAFF | N020_retry_a |
| 약 3~5분에 한 번 |  | #88AAFF | L011 |
| 약 5~10분에 한 번 |  | #88AAFF | N020_retry_a |
| 누군가 시킬 때 마다 |  | #88AAFF | N020_retry_a |


---

### [N020_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 에피네프린은 3~5분에 한 번 투여합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C019 |


---

### [L011] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C020 |


---

### [C020] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C020_Options 표 참조]** |

#### [C020_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약물만 주입 |  | #88AAFF | N020_retry_b |
| 약물 주입 후 생리식염수 주입 |  | #88AAFF | N020_retry_b |
| 약물 주입 후 생리식염수 주입, 이후 팔 들어올리기 |  | #88AAFF | Q022_1 |


---

### [N020_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 심장에 빠르게 도달시키기 위해 생리식염수 주입 후 팔을 들어올려야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C020 |


---

### [Q022_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q022_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_D |
| **NextIdentifier** | 문자열 | CC_D_epi |


---


<!-- ================= [P005 병렬 종료 및 2nd Cycle (P006) 진입] ================= -->

### [D028] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E031 |


---

### [E031] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | stop_ambu_and_comp |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | E032 |


---

### [E032] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | asystole_monitor_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D029 |


---

### [D029] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | Asystole입니다. 가슴압박과 앰부배깅 하시던 간호사 A, B 선생님끼리 교대 후 계속 가슴압박 해주세요. 간호사 C, D 선생님께서도 교대해서 역할을 수행해 주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P006 |


---

### [P006] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P006_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D033 |

#### [P006_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N021 | CC_A_chestcomp | NurseA | cpr_team | - | All |
| N022 | CC_B_ambu | NurseB | airway_team | - | All |
| N023 | CC_C_epi | NurseC | medication_team | - | All |
| N024 | CC_D_defib | NurseD | defib_team | - | All |


---


<!-- ================= [P006 병렬 브랜치 1] 플레이어 A (가슴압박 교대) ================= -->

### [N021] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 가슴을 클릭해 가슴압박을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q023 |


---

### [Q023] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_A |
| **NextIdentifier** | 문자열 | V027 |


---

### [V027] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E033 |

#### [V027_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_chest |


- [ ] 환자 A 흉부 압박 위치 collider에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_chest`로 설정한다.


---

### [E033] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_chest_compression |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L012 |


---

### [L012] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C021 |


---

### [C021] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 가슴 압박 깊이는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C021_Options 표 참조]** |

#### [C021_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 4cm |  | #88AAFF | N021_retry_a |
| 약 5cm |  | #88AAFF | L013 |
| 약 6cm |  | #88AAFF | N021_retry_a |


---

### [N021_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C021 |


---

### [L013] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C022 |


---

### [C022] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 성인의 정확한 가슴 압박 위치는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C022_Options 표 참조]** |

#### [C022_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 양측 유두선상의 중간지점 |  | #88AAFF | N021_retry_b |
| 흉골 하부 1/2 지점 |  | #88AAFF | L014 |


---

### [N021_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C022 |


---

### [L014] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C023 |


---

### [C023] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 정확한 가슴 압박 횟수는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C023_Options 표 참조]** |

#### [C023_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 분당 약 80~100회 |  | #88AAFF | N021_retry_c |
| 분당 약 100~120회 |  | #88AAFF | L015 |
| 분당 약 120~140회 |  | #88AAFF | N021_retry_c |


---

### [N021_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C023 |


---

### [L015] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C024 |


---

### [C024] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 4. 가슴압박 시 주의사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C024_Options 표 참조]** |

#### [C024_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 지쳐도 한 사람이 계속 가슴압박을 수행한다. |  | #88AAFF | N021_retry_d |
| 뼈가 부러진 것 같으면 멈춘다. |  | #88AAFF | N021_retry_d |
| 충분한 이완을 제공한다. |  | #88AAFF | Q023_1 |


---

### [N021_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C024 |


---

### [Q023_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q023_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_A |
| **NextIdentifier** | 문자열 | CC_A_chestcomp |


---


<!-- ================= [P006 병렬 브랜치 2] 플레이어 B (앰부백 산소화 교대) ================= -->

### [N022] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백을 클릭해 산소 공급을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q024 |


---

### [Q024] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_B |
| **NextIdentifier** | 문자열 | V028 |


---

### [V028] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E034 |

#### [V028_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.start_ambu |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.start_ambu [아이템 사용(Item Use Signal), spec §5.1~5.3].


---

### [E034] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_ambubagging |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L016 |


---

### [L016] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C025 |


---

### [C025] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 산소 제공량은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C025_Options 표 참조]** |

#### [C025_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 600ml (엄지, 검지, 중지를 이용해 백을 짠다) |  | #88AAFF | L017 |
| 약 1500ml (다섯 손가락 모두를 이용해 백을 짠다) |  | #88AAFF | N022_retry_a |


---

### [N022_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N022_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C025 |


---

### [L017] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C026 |


---

### [C026] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 심폐소생술 중 적절한 속도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C026_Options 표 참조]** |

#### [C026_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 10초에 1번 (분당 약 6회) |  | #88AAFF | N022_retry_b |
| 6초에 1번 (분당 약 10회) |  | #88AAFF | Q024_1 |
| 3초에 1번 (분당 약 20회) |  | #88AAFF | N022_retry_b |


---

### [N022_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N022_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 6초에 1번씩 눌러야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C026 |


---

### [Q024_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q024_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_B |
| **NextIdentifier** | 문자열 | CC_B_ambu |


---


<!-- ================= [P006 병렬 브랜치 3] 플레이어 C (에피네프린 투여 교대) ================= -->

### [N023] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q025 |


---

### [Q025] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_C |
| **NextIdentifier** | 문자열 | V029 |


---

### [V029] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N023_1 |

#### [V029_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_epinephrine_ampule |
| Registry | Contains | RuntimeState | sig.click_syringe_5cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A010(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A010 → N023_1 로 재지정함. 산출물 `epinephrine_5cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 구 `epi_ready`/`epinephrine_syringe` → 정본 `epinephrine_5cc_syringe` 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_epinephrine_ampule, sig.click_syringe_5cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N023_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | Push용 생리식염수를 준비합니다. 20cc 생리식염수와 20cc 주사기를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V029_1 |


---

### [V029_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D030 |

#### [V029_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_normal_saline_20ml |
| Registry | Contains | RuntimeState | sig.click_syringe_20cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A011(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A011 → D030 로 재지정함. 산출물 `normal_saline_20cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 구 `ns_20cc(_ready)` 산출물 제거 → 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1) 대체 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_normal_saline_20ml, sig.click_syringe_20cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [D030] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 에피네프린 첫 투여 시점부터 4분 지났습니다. 간호사 C 선생님, 바로 에피네프린과 생리식염수 20cc 투여해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N023_2 |


---

### [N023_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V029_2 |


---

### [V029_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D031 |

#### [V029_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_epi |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_epi [아이템 사용(Item Use Signal), spec §5.1~5.3].

> OR 게이트 처리(B, 우선 채택): V026_2 와 동일하게 사용 시점 대표 시그널 `sig.push_epi` 로 검사하여
> 에피네프린 주사기 18종 변형 중 어느 것을 투여해도 통과한다. 정책: `interaction-signal-integration-spec.md` §6.


---

### [D031] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 에피네프린 1mg 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N023_3 |


---

### [N023_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V029_3 |


---

### [V029_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D031_1 |

#### [V029_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_ns |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_ns [아이템 사용(Item Use Signal), spec §5.1~5.3].


---

### [D031_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D031_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 생리식염수 20cc 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L018 |


---

### [L018] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C027 |


---

### [C027] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C027_Options 표 참조]** |

#### [C027_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 1~2분에 한 번 |  | #88AAFF | N023_retry_a |
| 약 3~5분에 한 번 |  | #88AAFF | L019 |
| 약 5~10분에 한 번 |  | #88AAFF | N023_retry_a |
| 누군가 시킬 때 마다 |  | #88AAFF | N023_retry_a |


---

### [N023_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 에피네프린은 3~5분에 한 번 투여합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C027 |


---

### [L019] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C028 |


---

### [C028] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C028_Options 표 참조]** |

#### [C028_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약물만 주입 |  | #88AAFF | N023_retry_b |
| 약물 주입 후 생리식염수 주입 |  | #88AAFF | N023_retry_b |
| 약물 주입 후 생리식염수 주입, 이후 팔 들어올리기 |  | #88AAFF | Q025_1 |


---

### [N023_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 심장에 빠르게 도달시키기 위해 생리식염수 주입 후 팔을 들어올려야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C028 |


---

### [Q025_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q025_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_C |
| **NextIdentifier** | 문자열 | CC_C_epi |


---


<!-- ================= [P006 병렬 브랜치 4] 플레이어 D (제세동기 대기) ================= -->

### [N024] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 제세동기를 클릭해 역할을 부여받으세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q026 |


---

### [Q026] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_D |
| **NextIdentifier** | 문자열 | V030 |


---

### [V030] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | S004 |

#### [V030_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_defib |


- [ ] 제세동기 collider에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_defib`로 설정한다.


---

### [S004] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | defib_on_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D032 |


---

### [D032] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 제세동기 준비가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L020 |


---

### [L020] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C029 |


---

### [C029] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 제세동기는 Sync 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 다음 중 제세동을 실시해야 하는 심전도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C029_Options 표 참조]** |

#### [C029_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| Asystole(무수축) |  | #88AAFF | N024_retry_a |
| PEA(무맥성 전기활동) |  | #88AAFF | N024_retry_a |
| VT(맥박이 있는 심실빈맥) |  | #88AAFF | N024_retry_a |
| VF(심실세동) |  | #88AAFF | L021 |


---

### [N024_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 VF(심실세동) 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C029 |


---

### [L021] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C030 |


---

### [C030] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C030_Options 표 참조]** |

#### [C030_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 150~200J(줄) |  | #88AAFF | L022 |
| 360J(줄) |  | #88AAFF | N024_retry_b |


---

### [N024_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 150~200J(줄)이 정답입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C030 |


---

### [L022] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **DurationSeconds** | 실수(float) | 4(초) |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C031 |


---

### [C031] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 제세동 등 전기충격 시 주의해야 할 사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C031_Options 표 참조]** |

#### [C031_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 꼬인 수액 줄을 풀어준다. |  | #88AAFF | N024_retry_c |
| 의료진이 손을 대어도 괜찮다. |  | #88AAFF | N024_retry_c |
| 의사의 지시가 있을 때에만 실시한다. |  | #88AAFF | N024_retry_c |
| 전기충격 전 모두 환자에게서 떨어지도록 지시한다. |  | #88AAFF | Q026_1 |


---

### [N024_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 감전되지 않도록 모두가 떨어지도록 지시해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C031 |


---

### [Q026_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q026_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_D |
| **NextIdentifier** | 문자열 | CC_D_defib |


---


<!-- ================= [P006 병렬 종료 및 ROSC 확인] ================= -->

### [D033] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E035 |


---

### [E035] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | stop_ambu_and_comp |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | E036 |


---

### [E036] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | rosc_monitor_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D034 |


---

### [D034] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | QRS 보입니다. 간호사 A 선생님, 환자 맥박 있는지 확인해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N025 |


---

### [N025] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 목을 클릭해서 경동맥을 촉지합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q027 |


---

### [Q027] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse_ROSC |
| **NextIdentifier** | 문자열 | V031 |


---

### [V031] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q027_1 |

#### [V031_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_pulse_patient_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_pulse_patient_a [사정(PatientController Assess 자동), spec §5.1~5.3].


---

### [Q027_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q027_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse_ROSC |
| **NextIdentifier** | 문자열 | D035 |


---

### [D035] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 환자 맥박 느껴집니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D036 |


---

### [D036] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 환자 ROSC 되었습니다. 제가 검사랑 협진 의뢰 할테니 간호사 D 선생님이 의식상태 확인해주세요. 간호사 B 선생님, 의복 제거해서 추가 손상 있는지 사정해주세요. 간호사 A 선생님께서는 다시 분류구역으로 이동해서 환자 분류해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P007 |


---

### [P007] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P007_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D037 |

#### [P007_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N026 | CC_A_triagearea | NurseA | triage_lead | - | All |
| N027 | CC_B_cut_patient_a | NurseB | procedure_team | - | All |
| N028 | CC_D_gcs_patient_a_rosc | NurseD | neuro_assessment | - | All |


---


<!-- ================= [P007 병렬 브랜치 1] 플레이어 A (분류 구역 복귀) ================= -->

### [N026] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 중증도 분류 구역으로 이동하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q028 |


---

### [Q028] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Return_Triage |
| **NextIdentifier** | 문자열 | V032 |


---

### [V032] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E037 |

#### [V032_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.arrive_triagearea |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.arrive_triagearea [구역 진입(ScenarioTriggerZone), spec §5.1~5.3].


---

### [E037] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | player_a_move_to_triage |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | Q028_1 |


---

### [Q028_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q028_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Return_Triage |
| **NextIdentifier** | 문자열 | CC_A_triagearea |


---


<!-- ================= [P007 병렬 브랜치 2] 플레이어 B (의복 제거) ================= -->

### [N027] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 가위를 클릭해 획득하고, 환자를 클릭해 의복을 제거하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q029 |


---

### [Q029] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cut_Clothing |
| **NextIdentifier** | 문자열 | V033 |


---

### [V033] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | S005 |

#### [V033_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_scissors |
| Registry | Contains | RuntimeState | sig.remove_patient_clothing |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_scissors [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].

- [ ] (b) 선행 구현 필요(미배선): sig.remove_patient_clothing. 게임플레이 인터랙션/완료 콜백 구현 후 Raise 필요 (spec §5.3). 인간 작업자 확정 요망.


---

### [S005] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | cutting_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | N027_1 |


---

### [N027_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N027_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 추가 외상은 확인되지 않습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q029_1 |


---

### [Q029_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q029_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cut_Clothing |
| **NextIdentifier** | 문자열 | CC_B_cut_patient_a |


---


<!-- ================= [P007 병렬 브랜치 3] 플레이어 D (의식 상태 사정) ================= -->

### [N028] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 클릭해 환자의 의식 상태를 사정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | Q030 |


---

### [Q030] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_ROSC |
| **NextIdentifier** | 문자열 | V034 |


---

### [V034] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N028_1 |

#### [V034_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_gcs_a_rosc |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_gcs_a_rosc [사정(PatientController Assess 자동), spec §5.1~5.3].


---

### [N028_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식 상태를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N028_2 |


---

### [N028_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C032 |


---

### [C032] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C032_Options 표 참조]** |

#### [C032_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) |  | #88AAFF | N028_retry_a |
| V(Verbal response, 음성에 반응 있음) |  | #88AAFF | N028_retry_a |
| P(Pain response, 통증에 반응 있음) |  | #88AAFF | N028_3 |
| U(Unconsciousness, 반응 없음) |  | #88AAFF | N028_retry_a |


---

### [N028_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C032 |


---

### [N028_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C033 |


---

### [C033] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C033_Options 표 참조]** |

#### [C033_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적) |  | #88AAFF | N028_retry_b |
| 3점(명령) |  | #88AAFF | N028_retry_b |
| 2점(통증) |  | #88AAFF | N028_4 |
| 1점(반응 없음) |  | #88AAFF | N028_retry_b |


---

### [N028_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 통증 자극에만 반응했음을 유의하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C033 |


---

### [N028_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. 현재 기관내삽관이 시행되어있는 상태입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | C034 |


---

### [C034] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C034_Options 표 참조]** |

#### [C034_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) |  | #88AAFF | N028_retry_c |
| 4점(혼란) |  | #88AAFF | N028_retry_c |
| 3점(부적절한 답변) |  | #88AAFF | N028_retry_c |
| 2점(신음소리) |  | #88AAFF | N028_retry_c |
| 1점(반응 없음) |  | #88AAFF | N028_retry_c |
| E(기관삽관) |  | #88AAFF | N028_5 |


---

### [N028_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 기관삽관을 하는 경우 E로 처리(표기)합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C034 |


---

### [N028_5] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 움찔거리며 움직이려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C035 |


---

### [C035] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C035_Options 표 참조]** |

#### [C035_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 6점(명령 수행) |  | #88AAFF | N028_retry_d |
| 5점(통증 원인을 치우려고 손을 뻗음) |  | #88AAFF | N028_retry_d |
| 4점(통증에 회피) |  | #88AAFF | N028_6 |
| 3점(이상 굴곡) |  | #88AAFF | N028_retry_d |
| 2점(이상 신전) |  | #88AAFF | N028_retry_d |
| 1점(반응 없음) |  | #88AAFF | N028_retry_d |


---

### [N028_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 통증으로부터 회피하려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C035 |


---

### [N028_6] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_6 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS 측정 완료. E2 / V(E) / M4 = 총 6E점 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q030_1 |


---

### [Q030_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q030_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_ROSC |
| **NextIdentifier** | 문자열 | CC_D_gcs_patient_a_rosc |


---


<!-- ================= [시나리오 A 종료] ================= -->

### [D037] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | (end) |


---


## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D037 |
| 종료 연출/설명 | ROSC 이후 신경학적 확인과 전신 노출을 마친 뒤, 검은 화면으로 fade out 되며 "시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다." 메세지를 표시하며 종료된다. 이후 다시 밝아지며 다음 시나리오로 이어진다. |

- [x] R9: 종료 노드 = D037, NextIdentifier = (end)/null. JSON과 일치.
