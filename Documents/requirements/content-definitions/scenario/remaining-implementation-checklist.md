---
title: "남은 이벤트 구현 체크리스트"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 남은 이벤트 구현 체크리스트

기준 문서: Documents/scenario/event-registry.md
생성일: 2026-03-17

## 분류 기준
- P0: 기존 패턴(오브젝트 토글/이동/UI 표시)로 바로 구현 가능한 항목
- P1: 타이밍/연결선/단계 상태 전환이 필요한 항목
- P2: 애니메이션/복합 장비 상호작용/고도 그래프가 필요한 항목

## P0 (즉시 구현 가능)
- [x] insert_18g_left
- [x] connect_ns1_left
- [x] insert_18g_right
- [x] connect_ps1_right
- [x] insert_20g_right_patientB
- [x] connect_ns1_right_patientB
- [x] insert_20g_left_patientC
- [x] connect_ns1_left_patientC
- [x] playerA_move_to_triage

## P1 (단계 상태 연동 필요)
- [x] insert_central_line_set
- [x] lv1_ready
- [x] Apply_ambu_patientA
- [x] attach_defibpad
- [x] defib_ui_irregular

## P2 (애니메이션/행동 루프 연동)
- [x] start_ambubagging
- [x] start_chest_compression
- [x] stop_ambu_and_comp

## 참고
- event-registry 상단 예시 행 `(예) move_patient_a_to_treatment` 는 실제 구현 대상이 아님.
- 다음 라운드는 P0를 먼저 일괄 구현하고, 플레이모드 로그로 오브젝트 키를 고정한 뒤 P1/P2로 확장 권장.
