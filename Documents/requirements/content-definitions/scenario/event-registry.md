---
title: "시나리오 이벤트 레지스트리"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-07-14
flags: ["refactor-required"]
---

# 시나리오 이벤트 레지스트리

이 문서는 시나리오 그래프의 `InvokeEvent` 노드가 호출하는 이벤트 식별자(EventIdentifier)를 추적하는 목록이다. 새 `InvokeEvent`를 추가하면 반드시 여기에 기록한다.

## 표기 규칙 (재작성 기준, 2026-07-09)

- **식별자는 snake_case로 통일**한다. 판단 기준은 실제 JSON 산출물과 C# 코드이며(a-2), 문서(md)/레지스트리/JSON 3자가 어긋나면 **JSON·C# 쪽을 정본**으로 본다.
- **콘텐츠(대사·안내문)에 노출되는 화자명은 `시스템`(한글)**, **코드/식별자/기술 표기는 `System`(영문)**으로 구분한다(a-3).
- 주석은 실제 주석 대신 체크리스트 문법 `- [ ]`(미완/확인 필요) / `- [x]`(완료)로, 항목 뒤에 이후 요구 작업을 표기한다.
- 상태 값은 `planned`, `implemented`, `deprecated` 중 하나로 기록한다.

## 태그 연계 규칙

- EventIdentifier를 추가/수정할 때, 해당 이벤트가 사용되는 시나리오 그래프 상위 `tags` 선언을 함께 확인한다.
- 병렬 분기 조건 변경이 필요한 이벤트는 문서에서 `RequiredPlayerTags`, `ForbiddenPlayerTags`, `RequiredPlayerTagsMatchMode`를 함께 갱신한다.
- 플레이어 태그 변경 흐름은 시나리오 문서/JSON에서 `TagModification` 노드 표기를 우선 사용한다. (엔진 enum 멤버는 `PlayerTag`이며, `TagModification`은 구버전 호환 표기이다. → `scenario-graph-spec.md` §TagModificationNode)

---

# 이벤트 본문

## 1. 재난 초기 대응 및 중증도 분류 (disaster_intro)

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| `triage_patient_a_patient_dummy_d_a` | 환자 A와 patient_dummy_d_a가 중증도 분류 구역으로 이송되는 연출(각 캐릭터 Stretcher에 누운 상태로 진입) | 시나리오 시작 후 간호사 A 준비 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| `show_patient_a_ui` | 환자 A 외견/상태 정보 UI 패널 활성화 | 간호사 A가 환자 A 클릭 시 | 〃 | implemented |
| `show_patient_dummy_d_a_ui` | patient_dummy_d_a 외견/상태 정보 UI 패널 활성화 | 간호사 A가 patient_dummy_d_a 클릭 시 | 〃 | implemented |
| `b_c_d_to_triage` | 간호사 B, C, D가 이송을 돕기 위해 트리아지 구역으로 이동하는 연출 | 간호사 A 분류 종료·이송 요청 대사 후 | 〃 | implemented |

- [ ] 확인: disaster_intro JSON은 현재 `triage_patientA_patientDummyDA` 등 camelCase 잔재가 있는지 재점검하고 snake_case로 정합 필요.

---

## 2. 환자 A 중증 처치 (patient_a_critical)

> JSON 정본 기준. md 본문·구(舊)레지스트리의 camelCase(`move_patientA_to_treatmentroom`, `apply_gauze_patientA`, `ROSC_monitor_ui` 등)는 폐기하고 아래 snake_case로 통일한다.

| EventIdentifier | 설명 | 호출 시점(노드) | 상태 |
|---|---|---|---|
| `move_patient_a_to_treatmentroom` | 환자 A 베드를 4인이 처치실로 이동시키는 연출 | E005 | implemented |
| `activate_vital_monitor_ui_patient_a` | 환자 A 활력징후 UI 출력 + 모니터 오브젝트 출력 시작 | E006 | implemented |
| `show_suction_checklist_ui` | 흡인 준비물 4종 체크리스트 UI 표시 | E007 | implemented |
| `hide_suction_checklist_ui` | 흡인 준비물 체크리스트 UI 숨김 | E008 | implemented |
| `vitalinfo_1_patient_a` | 환자 A 첫 활력징후 수치 모니터 강제 출력 | E009 | implemented |
| `show_checklist_intu` | 기관내삽관 준비물 6종 체크리스트 UI 표시 | E010 | implemented |
| `hide_checklist_intu` | 기관내삽관 준비물 체크리스트 UI 숨김 | E011 | implemented |
| `insert_et_tube` | 환자 구강 내로 기관내관 삽입 연출 | E012 | implemented |
| `remove_stylet` | 삽입된 기관내관에서 스타일렛 제거 연출 | E013 | implemented |
| `connect_tpiece_ready` | 기관내관에 T-piece 연결 연출 | E014 | implemented |
| `apply_gauze_patient_a` | 환자 A 흉부 출혈부위 거즈 적용 연출 | E015 | implemented |
| `apply_gauze_with_plaster_patient_a` | 거즈를 플라스터 부착 상태로 변경 | E016 | implemented |
| `show_iv_checklist` | IV 준비물 체크리스트 UI 표시 | E017 | implemented |
| `hide_iv_checklist` | IV 준비물 체크리스트 UI 숨김 | E018 | implemented |
| `insert_18g_left` | 좌측 팔 18G 카테터 삽입 연출 | E019 | implemented |
| `connect_ns1_left` | 좌측 18G에 생리식염수 1L 연결 연출 | E020 | implemented |
| `insert_18g_right` | 우측 팔 18G 카테터 자동 삽입 연출 | E021 | implemented |
| `connect_ps1_right` | 우측 18G에 플라즈마 솔루션 자동 연결 연출 | E022 | implemented |
| `insert_central_line_set` | 의사 NPC가 C-line 삽입하는 연출 | E023 | implemented |
| `lv1_ready` | Level 1 rapid infuser에 수액/혈액 장착 상태 변경 | E024 | implemented |
| `patient_crash_ui` | 환자 상태 악화(모니터 알람+HR/RR/BP -?- 로 출력, SpO2 급감) 연출 | E025 | implemented |
| `apply_ambu_patient_a` | 기관내관 상단에 앰부백 연결 연출 | E026 | implemented |
| `start_ambubagging` | 앰부백 짜는 애니메이션 시작(무한 반복) | E027 · E034 | implemented |
| `start_chest_compression` | 가슴압박 애니메이션 시작(무한 반복) | E028 · E033 | implemented |
| `attach_defibpad` | 환자 맨가슴에 제세동 패드 부착 연출 | E029 | implemented |
| `defib_ui_irregular` | 제세동기 모니터에 비정상 심전도 파형 출력 | E030 | implemented |
| `stop_ambu_and_comp` | 가슴압박·앰부배깅 중지 및 대기 연출 | E031 · E035 | implemented |
| `asystole_monitor_ui` | 모니터·제세동기에 Asystole(무수축) 파형 출력 | E032 | implemented |
| `rosc_monitor_ui` | 모니터에 QRS·정상 활력징후 출력(ROSC) | E036 | implemented |
| `player_a_move_to_triage` | 간호사 A가 트리아지 구역으로 이동 | E037 | implemented |

- [ ] 확인: `start_ambubagging`/`start_chest_compression`/`stop_ambu_and_comp`가 1·2차 CPR 사이클에서 재사용된다(E027/E034 등). 사이클 반복 시 신호 리셋(`ScenarioInteractionSignals.Clear`)이 필요한 구간과 짝을 맞추는지 확인.

---

## 3. 환자 B, C 지연 처치 (patient_b_c_ct)

> JSON 정본(snake_case) 기준. 구(舊)레지스트리의 camelCase(`triage_patientB_patientC_patientDummyDB`, `show_patientB_ui`, `move_patientB`, `move_patients_to_CT`, `B_C_D_to_triage` 등)는 폐기한다.

| EventIdentifier | 설명 | 호출 시점(노드) | 상태 |
|---|---|---|---|
| `triage_patient_b_patient_c_patient_dummy_d_b` | 환자 B, patient_dummy_d_b, 환자 C가 트리아지 구역으로 이송되는 연출 | E038 | implemented |
| `show_patient_b_ui` | 환자 B 외견/상태 정보 UI 패널 활성화 | E039 | implemented |
| `show_patient_dummy_d_b_ui` | patient_dummy_d_b 외견/상태 정보 UI 패널 활성화 | E040 | implemented |
| `show_patient_c_ui` | 환자 C 외견/상태 정보 UI 패널 활성화 | E041 | implemented |
| `b_c_d_to_triage` | 간호사 B, C, D가 트리아지 구역으로 이동하는 연출 | E042 | implemented |
| `move_patient_b` | 환자 B 스트레쳐를 입원실 구역으로 이동 | E043 | implemented |
| `activate_vital_monitor_ui_patient_b` | 환자 B 활력징후 UI 출력 + 모니터 표시 | E044 | implemented |
| `pupil_reflex_patient_b` | 환자 B 좌측 동공 고정(대광반사 이상) 연출 | E045 | implemented |
| `insert_20g_right_patient_b` | 환자 B 우측 팔 20G 카테터 삽입 연출 | E046 | implemented |
| `connect_ns1_right_patient_b` | 환자 B 우측 20G에 생리식염수 연결 연출 | E047 | implemented |
| `apply_gauze_patient_b` | 환자 B 좌측 상완 및 얼굴 출혈부위 거즈 적용 연출 | E048 | implemented |
| `apply_gauze_with_plaster_patient_b` | 환자 B 거즈 모두를 플라스터 부착 상태로 변경 | E049 | implemented |
| `move_patient_c` | 환자 C 스트레쳐를 처치 구역으로 이동 | E050 | implemented |
| `activate_vital_monitor_ui_patient_c` | 환자 C 활력징후 UI 출력 + 모니터 표시 | E051 | implemented |
| `pupil_reflex_patient_c` | 환자 C 우측 동공 고정(대광반사 이상) 연출 | E052 | implemented |
| `insert_20g_left_patient_c` | 환자 C 좌측 팔 20G 카테터 삽입 연출 | E053 | implemented |
| `connect_ns1_left_patient_c` | 환자 C 좌측 20G에 생리식염수 연결 연출 | E054 | implemented |
| `apply_gauze_patient_c` | 환자 C 우측 상완 및 얼굴 출혈부위 거즈 적용 연출 | E055 | implemented |
| `apply_gauze_with_plaster_patient_c` | 환자 C 거즈 모두를 플라스터 부착 상태로 변경 | E056 | implemented |
| `move_patients_to_ct` | 환자 B·C가 CT실로 이동하는 최종 연출 | E057 | implemented |

- [x] 확인: 환자 C 대광반사 좌/우 이상 방향(`pupil_reflex_patient_c`)이 환자 B와 반대인지 원본 임상 의도와 대조. 2026-07-14 확인 완료.

---

## 4. 폐기된 식별자 매핑 (구 → 신)

아래는 구(舊)레지스트리/구 md 본문에서 사용되던 camelCase 식별자와 현행 snake_case의 대응이다. 코드/문서가 구 식별자를 참조하면 실패하므로, 발견 시 신 식별자로 치환한다.

| 구 식별자 (deprecated) | 신 식별자 (정본) |
|---|---|
| `triage_patientA_patientDummyDA` | `triage_patient_a_patient_dummy_d_a` |
| `show_patientA_ui` / `show_patientDummyDA_ui` | `show_patient_a_ui` / `show_patient_dummy_d_a_ui` |
| `move_patientA_to_treatmentroom` | `move_patient_a_to_treatmentroom` |
| `activate_vital_monitor_ui_patientA` | `activate_vital_monitor_ui_patient_a` |
| `vitalinfo_1_patientA` | `vitalinfo_1_patient_a` |
| `apply_gauze_patientA` / `apply_gauze_with_plaster_patientA` | `apply_gauze_patient_a` / `apply_gauze_with_plaster_patient_a` |
| `Apply_ambu_patientA` | `apply_ambu_patient_a` |
| `ROSC_monitor_ui` | `rosc_monitor_ui` |
| `playerA_move_to_triage` | `player_a_move_to_triage` |
| `triage_patientB_patientC_patientDummyDB` | `triage_patient_b_patient_c_patient_dummy_d_b` |
| `show_patientB_ui` / `show_patientDummyDB_ui` | `show_patient_b_ui` / `show_patient_dummy_d_b_ui` |
| `B_C_D_to_triage` | `b_c_d_to_triage` |
| `move_patientB` / `move_patientC` | `move_patient_b` / `move_patient_c` |
| `activate_vital_monitor_ui_patientB/C` | `activate_vital_monitor_ui_patient_b/c` |
| `pupil_reflex_patientB/C` | `pupil_reflex_patient_b/c` |
| `insert_20g_right_patientB` / `insert_20g_left_patientC` | `insert_20g_right_patient_b` / `insert_20g_left_patient_c` |
| `connect_ns1_right_patientB` / `connect_ns1_left_patientC` | `connect_ns1_right_patient_b` / `connect_ns1_left_patient_c` |
| `apply_gauze_patientB/C` 등 | `apply_gauze_patient_b/c` 등 |
| `move_patients_to_CT` | `move_patients_to_ct` |

- [x] a-1 반영: 식별자 snake_case 통일.
- [x] a-2 반영: JSON/C# 정본 기준.
- [ ] 확인(인간 작업자): 위 매핑을 최종 확정한 뒤, 구 식별자를 참조하는 문서/코드가 남아있지 않은지 리포지토리 전역 검색으로 검증.
