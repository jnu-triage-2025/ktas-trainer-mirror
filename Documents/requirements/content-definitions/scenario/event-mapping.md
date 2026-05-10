---
title: "시나리오 이벤트-구현 매핑 (재난 시나리오)"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 시나리오 이벤트-구현 매핑 (재난 시나리오)

이 문서는 원본 시나리오 텍스트를 바탕으로, 구현이 필요한 핵심 상호작용을 이벤트 단위로 정리한 목록입니다. 새로운 이벤트는 [Documents/scenario/event-registry.md](event-registry.md)에 기록합니다.

> 참고: 아래 표에는 초기 설계 단계 식별자도 포함됩니다. 런타임에서 실제 사용하는 EventIdentifier는 [Documents/scenario/event-registry.md](event-registry.md)를 기준으로 확정합니다.

## 태그/분기 연계 메모

- 역할/태그 분기 동작 변경이 포함되는 이벤트는 시나리오 본문의 ParallelBranch 표(`RequiredPlayerTags`, `ForbiddenPlayerTags`, `RequiredPlayerTagsMatchMode`)와 함께 수정합니다.
- 이벤트에서 플레이어 태그를 변경하는 흐름은 `TagModification` 노드로 작성하고, 그래프 상위 `tags` 선언과 일치하도록 유지합니다.

## 이벤트 목록 (핵심 상호작용)

| 구간 | EventIdentifier | 내용 요약 | 구현 포인트 |
|---|---|---|---|
| 초기 대응 | select_role_nurse_a/b/c/d | 역할 선택 UI | ChoiceOption과 역할 플래그 연동 |
| 초기 대응 | prep_treatment_supplies | 처치 물품 준비 | 프리팹 활성화, 준비 상태 플래그 |
| 초기 대응 | prep_fluids_and_meds | 수액/약물 준비 | 물품 스폰, 인벤토리 준비 |
| 중증도 분류 | start_triage_two_patients | 환자 A/더미 도착 | 환자 스폰, 베드 배치 |
| 중증도 분류 | triage_assign_red_and_waiting | 환자 A 이동/더미 대기 | 베드 이동, 상태 변경 |
| 환자 A | move_patient_a_to_treatment | 침대 이동 | 베드 이동 + 위치 고정 |
| 환자 A | assess_vitals_and_gcs_a | 활력/GCS 사정 | UI 결과 출력, 상태 플래그 |
| 환자 A | perform_airway_suction_a | 경추 고정/석션 | 석션 애니메이션, 출혈 효과 유지 |
| 환자 A | doctor_order_intubation_iv_press_a | 의사 오더 | 대사 출력, 다음 단계 활성화 |
| 환자 A | assist_intubation_a | 삽관 보조 | 아이템 조립/전달, 절차 완료 체크 |
| 환자 A | apply_direct_pressure_a | 출혈 부위 압박 | 붕대 적용, 출혈 감소 효과 |
| 환자 A | establish_iv_and_fluids_a | IV 확보/수액 연결 | 캐뉼라/수액 연결 상태 |
| 환자 A | connect_c_line_and_rapid_infuser_a | C-line/대량수액 | C-line 전달, 급속수액 연결 |
| 환자 A | check_pulse_and_detect_pea_a | 맥박 확인/PEA | 선택 UI, 경동맥 선택 시 진행 |
| 환자 A | perform_cpr_compressions_a | 가슴압박 | 압박 애니메이션, 퀴즈 UI |
| 환자 A | perform_bvm_oxygenation_a | 앰부백 산소화 | 앰부백 연결/압박 횟수 |
| 환자 A | apply_defib_pads_a | 제세동 패드 부착 | 패드 부착 위치 체크 |
| 환자 A | prepare_and_give_epinephrine_a | 에피네프린 투여 | 주사기 준비/투여 완료 |
| 환자 A | confirm_rosc_a | ROSC 확인 | 리듬 변경, 상태 전환 |
| 환자 A | check_pupil_response_a | 동공 반응 확인 | 펜라이트 반응 연출 |
| 환자 A | perform_exposure_a | 의복 제거 | 가위 아이템 사용/노출 상태 |
| 환자 B/C | move_patient_b_to_treatment | 처치 구역 이동 | 베드 이동 |
| 환자 B/C | assess_vitals_and_gcs_b | 의식/활력 사정 | 질문 UI/결과 출력 |
| 환자 B/C | apply_nasal_cannula_b | 비강 캐뉼라 연결 | 산소 연결 상태 |
| 환자 B/C | doctor_order_neuro_oxygen_hemostasis_iv_b | 의사 오더 | 대사 출력 |
| 환자 B/C | perform_oxygen_hemostasis_iv_b | 산소 3L/지혈/IV | 산소 유량, 붕대, IV 연결 |
| 환자 B/C | check_pupil_and_report_b | 동공 반응/보고 | 펜라이트 + 보고 UI |
| 환자 B/C | order_brain_ct_b | Brain CT 지시 | 다음 이동 단계 활성화 |
| 환자 B/C | move_to_ct_room_b | CT실 이동 | 2인 베드 이동 조건 |

## 누락 가능성이 높은 기능

- 앰부백 압박 애니메이션 및 반복 횟수 카운트
- 베드 이동 시 복수 플레이어 조건
- 장비 조립(후두경, ET-tube+스타일렛, 산소 유량계)
- 패드 부착 위치 검증

필요 시 상호작용 기능은 [Documents/requirements/gameplay/interaction/interaction-feature-spec.md](../../gameplay/interaction/interaction-feature-spec.md)를 참고합니다.
