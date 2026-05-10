---
title: "Event Handler File Index"
doc_type: requirement
status: active
updated: 2026-04-14
---

# Event Handler File Index

TriageTrainer 시나리오 이벤트 구현 파일 인덱스입니다.

운영 규칙:
- 이벤트 구현을 추가/변경하면 [Documents/scenario/event-registry.md](event-registry.md)와 함께 갱신합니다.
- 이벤트가 분기 조건 또는 플레이어 태그 변경에 영향을 주는 경우, 시나리오 문서의 `RequiredPlayerTags`/`ForbiddenPlayerTags`/`RequiredPlayerTagsMatchMode` 및 `TagModification` 노드 정의를 함께 확인합니다.

## Core bootstrap

- Bootstrap: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs
- Intro/PatientA registration router: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.IntroAndPatientAEvents.cs
- PatientB/C registration router: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientBCEvents.cs

## Intro

- triage_patientA_dummyA: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.triage_patientA_dummyA.cs
- show_patientA_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_patientA_ui.cs
- show_dummyA_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_dummyA_ui.cs
- B_C_D_to_triage: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.B_C_D_to_triage.cs

## Patient A

- move_patientA_to_treatmentroom: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.move_patientA_to_treatmentroom.cs
- activate_vital_monitor_ui_patientA: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.activate_vital_monitor_ui_patientA.cs
- show_suction_checklist_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_suction_checklist_ui.cs
- hide_suction_checklist_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.hide_suction_checklist_ui.cs
- vitalinfo_1_patientA: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.vitalinfo_1_patientA.cs
- show_checklist_intu: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_checklist_intu.cs
- hide_checklist_intu: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.hide_checklist_intu.cs
- show_iv_checklist: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_iv_checklist.cs
- hide_iv_checklist: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.hide_iv_checklist.cs
- insert_et_tube: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.insert_et_tube.cs
- remove_stylet: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.remove_stylet.cs
- connect_tpiece_ready: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.connect_tpiece_ready.cs
- insert_18g_left: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.insert_18g_left.cs
- connect_ns1_left: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.connect_ns1_left.cs
- insert_18g_right: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.insert_18g_right.cs
- connect_ps1_right: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.connect_ps1_right.cs
- apply_gauze_patientA: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_gauze_patientA.cs
- apply_gauze_with_plaster_patientA: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_gauze_with_plaster_patientA.cs
- playerA_move_to_triage: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.playerA_move_to_triage.cs
- insert_central_line_set: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.insert_central_line_set.cs
- lv1_ready: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.lv1_ready.cs
- Apply_ambu_patientA: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_ambu_patientA.cs
- attach_defibpad: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.attach_defibpad.cs
- defib_ui_irregular: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.defib_ui_irregular.cs
- start_ambubagging: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.start_ambubagging.cs
- start_chest_compression: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.start_chest_compression.cs
- stop_ambu_and_comp: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.stop_ambu_and_comp.cs
- patient_crash_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.patient_crash_ui.cs
- asystole_monitor_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.asystole_monitor_ui.cs
- ROSC_monitor_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.rosc_monitor_ui.cs

## Patient B/C

- triage_patientB_patientC_dummyB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.triage_patientB_patientC_dummyB.cs
- show_patientB_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_patientB_ui.cs
- show_dummyB_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_dummyB_ui.cs
- show_patient_c_ui: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.show_patient_c_ui.cs
- move_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.move_patientB.cs
- move_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.move_patientC.cs
- activate_vital_monitor_ui_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.activate_vital_monitor_ui_patientB.cs
- activate_vital_monitor_ui_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.activate_vital_monitor_ui_patientC.cs
- pupil_reflex_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.pupil_reflex_patientB.cs
- pupil_reflex_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.pupil_reflex_patientC.cs
- move_patients_to_CT: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.move_patients_to_CT.cs
- apply_gauze_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_gauze_patientB.cs
- apply_gauze_with_plaster_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_gauze_with_plaster_patientB.cs
- apply_gauze_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_gauze_patientC.cs
- apply_gauze_with_plaster_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.apply_gauze_with_plaster_patientC.cs
- insert_20g_right_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.insert_20g_right_patientB.cs
- connect_ns1_right_patientB: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.connect_ns1_right_patientB.cs
- insert_20g_left_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.insert_20g_left_patientC.cs
- connect_ns1_left_patientC: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.connect_ns1_left_patientC.cs
