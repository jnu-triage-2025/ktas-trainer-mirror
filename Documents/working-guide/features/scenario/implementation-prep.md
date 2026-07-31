---
title: "재난 시나리오 미구현 기능 구현 준비서 (안전 우선)"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 재난 시나리오 미구현 기능 구현 준비서 (보관용)

이 문서는 과거 구현 준비 초안의 보관본이다. 현재 작업의 기준 문서는 아니다.

대체 참조:
- [human-operator-process.md](./human-operator-process.md)
- [patient-a-b-c-play-setup-guide.md](./patient-a-b-c-play-setup-guide.md)
- [entity-preset-debug-guide.md](./entity-preset-debug-guide.md)
- [scenario-preflight-and-dev-stub-setup-guide.md](./scenario-preflight-and-dev-stub-setup-guide.md)
- _patientBTreatmentBedEntityIdentifier = patientBTreatmentBed
- _patientCTreatmentBedEntityIdentifier = patientCTreatmentBed
- _patientBVitalMonitorEntityIdentifier = patientB_monitor
- _patientCVitalMonitorEntityIdentifier = patientC_monitor

별칭 기반 휴리스틱 조회(키 불확실 환경 대비):
- _patientAAliases / _patientDummyDAAliases
- _patientABedAliases / _patientDummyDABedAliases
- _nurseBAliases / _nurseCAliases / _nurseDAliases
- _nurseAAliases / _nurseBAliases / _nurseCAliases / _nurseDAliases
- _patientATreatmentBedAliases / _patientAVitalMonitorAliases
- _patientBAliases / _patientCAliases / _patientDummyDBAliases
- _patientBTreatmentBedAliases / _patientCTreatmentBedAliases
- _patientBVitalMonitorAliases / _patientCVitalMonitorAliases
- 조회 순서:
	1) Registry(Entity/Npc) exact key
	2) Npc 컴포넌트의 Identifier / GameObject.name 매칭
	3) GameObject.Find(alias)

triage_patientA_patientDummyDA 이벤트 동작(현재 MVP+):
- _autoAttachPatientsToBeds = true 이면 환자(GameObject의 PatientController)를 침대(GameObject의 MovingPatientBedController)에 TryReposeTarget으로 연결 시도
- _preferMovingBedsForSpawn = true 이고 침대 오브젝트가 있으면 환자 대신 침대를 스폰 포인트로 이동
- 침대 참조가 없거나 연결 실패하면 기존 환자/더미 직접 이동 경로로 자동 fallback

patient_a_critical P1 이벤트 동작(현재 MVP):
- move_patientA_to_treatmentroom:
	- _patientATreatmentBedObject 우선 이동, 없으면 patientABed, 없으면 patientA 이동 fallback
	- _autoAttachPatientAToTreatmentBed=true 이면 treatment bed로 환자 A 재연결 시도
- activate_vital_monitor_ui_patientA:
	- _patientAVitalMonitorObject / _patientAVitalPanel 활성화
	- _patientAVitalMonitorController가 연결되어 있으면 enabled=true
	- _applyPatientAInitialMonitorProfile=true 이면 _patientAInitialMonitorParameters를 적용한 뒤 모니터 시작
- show_suction_checklist_ui / hide_suction_checklist_ui:
	- _suctionChecklistUiPanel 활성/비활성 토글
- show_checklist_intu / hide_checklist_intu:
	- _intuChecklistUiPanel 활성/비활성 토글
- show_iv_checklist / hide_iv_checklist:
	- _ivChecklistUiPanel 활성/비활성 토글
- vitalinfo_1_patientA:
	- _patientAVitalInfoMessage를 시스템 메시지로 출력
	- _vitalInfoAlsoActivateMonitor=true면 환자 A 모니터/패널을 함께 활성화
- insert_et_tube / remove_stylet / connect_tpiece_ready:
	- 기관내관 준비/삽입/스타일렛 제거/T-piece 연결 시각 오브젝트를 단계별 토글
- apply_gauze_patientA / apply_gauze_with_plaster_patientA:
	- 환자 A 거즈 -> 거즈+플라스터 오브젝트 단계 전환
- insert_18g_left / connect_ns1_left / insert_18g_right / connect_ps1_right:
	- 환자 A 카테터/수액 시각 오브젝트 단계 토글
- playerA_move_to_triage:
	- nurseA를 _playerATriagePoint로 이동(보간/즉시 이동 옵션)
- insert_central_line_set / lv1_ready / Apply_ambu_patientA / attach_defibpad:
	- 중심정맥관/Level1/앰부/제세동 패드 시각 오브젝트 활성화
- defib_ui_irregular:
	- _defibIrregularUiPanel 표시 + 불규칙 ECG 파라미터 적용
- start_ambubagging / start_chest_compression / stop_ambu_and_comp:
	- Animator bool 기반으로 흉부압박/앰부배깅 루프 시작/종료
- patient_crash_ui / asystole_monitor_ui / ROSC_monitor_ui:
	- 환자 A 모니터에 단계별 ECG 파라미터 프리셋 적용
	- 모니터/패널이 비활성이면 자동 활성화 후 적용

patient_b_c_ct intro 이벤트 동작(현재 MVP):
- triage_patientB_patientC_patientDummyDB:
	- patientB/patientC/patientDummyDB 오브젝트 활성화 후 지정 스폰 포인트로 이동
	- _patientBcdSpawnMoveDurationSeconds=0이면 즉시 배치
- show_patientB_ui / show_patientDummyDB_ui / show_patient_c_ui:
	- 각 대상 UI 패널을 _uiPanelAutoHideSeconds 규칙으로 표시
- move_patientB / move_patientC:
	- treatment bed 우선 이동, 없으면 patient 오브젝트 직접 이동 fallback
	- _autoAttachPatientBToTreatmentBed / _autoAttachPatientCToTreatmentBed 옵션으로 침대 탑승 시도
- activate_vital_monitor_ui_patientB / activate_vital_monitor_ui_patientC:
	- 각 환자 모니터/패널 활성화 후 초기 ECG 파라미터 적용
	- _applyPatientBInitialMonitorProfile / _applyPatientCInitialMonitorProfile 로 적용 여부 제어
- pupil_reflex_patientB / pupil_reflex_patientC:
	- 각 환자의 동공반사 전용 UI 패널을 표시(자동 숨김 옵션 공유)
	- patientB: 좌측 반응/우측 고정, patientC: 우측 반응/좌측 고정 인디케이터 활성화
- move_patients_to_CT:
	- patientB/patientC를 CT 포인트로 이동 (treatment bed가 있으면 bed 우선 이동)
	- _patientsToCtMoveDurationSeconds=0이면 즉시 배치
	- _ctTransferFadePanel이 있으면 이동 후 페이드 패널 표시
- apply_gauze_patientB / apply_gauze_with_plaster_patientB:
	- 환자 B 거즈 -> 거즈+플라스터 오브젝트 단계 전환
- apply_gauze_patientC / apply_gauze_with_plaster_patientC:
	- 환자 C 거즈 -> 거즈+플라스터 오브젝트 단계 전환
- insert_20g_right_patientB / connect_ns1_right_patientB:
	- 환자 B 카테터/수액 시각 오브젝트 단계 토글
- insert_20g_left_patientC / connect_ns1_left_patientC:
	- 환자 C 카테터/수액 시각 오브젝트 단계 토글

주의:
- 식별자는 RegistryType.Entity 또는 RegistryType.Npc에 등록된 값과 정확히 일치해야 한다.
- 등록 전 단계에서는 마지막 fallback으로 GameObject.Find(식별자) 경로를 사용한다.

실측 결과(현재 리포지토리):
- Primary Registry Preload NPC SO / Entity SO / Interactable Entity SO는 모두 비어 있다.
- NPCRegistry.asset Entries도 비어 있다.
- 따라서 코드 정적 분석만으로 patientA/patientDummyDA/NurseB/C/D 키를 확정할 수 없다.

키 확정 절차:
- TriageScenarioEventBootstrap의 _logRegistrySnapshotOnEnable = true 유지
- 플레이모드 진입 후 Console의 "Registry snapshot" 로그에서 실제 key를 확인
- 확인된 key를 _patientAEntityIdentifier / _patientDummyDAEntityIdentifier / _nurseBEntityIdentifier ... 에 반영
- 미해결 대상은 자동으로 "Unresolved targets after auto-resolve" 경고 로그가 출력된다.
- 컴포넌트 우클릭 ContextMenu의 "Validate Event Wiring" 실행 시, 핵심 참조 누락 필드를 즉시 보고한다.
- 컴포넌트 우클릭 ContextMenu의 "Run Core Smoke Test" 실행 시 핵심 이벤트를 순차 호출해 기본 흐름을 빠르게 검증한다.

안전 기본값 권장:
- _spawnMoveDurationSeconds = 0 (즉시 배치)
- _autoAttachPatientsToBeds = true
- _preferMovingBedsForSpawn = true
- _instantMoveBcd = true
- _bcdMoveDurationSeconds = 1.0
- _patientATreatmentMoveDurationSeconds = 1.5
- _applyPatientAInitialMonitorProfile = true
- _patientAInitialMonitorParameters.bpm = 140
- _vitalInfoAlsoActivateMonitor = true
- _patientACrashMonitorParameters.bpm = 80
- _patientAAsystoleMonitorParameters.bpm = 0
- _patientARoscMonitorParameters.bpm = 110
- _patientBcdSpawnMoveDurationSeconds = 0
- _patientBTreatmentMoveDurationSeconds = 1.5
- _patientCTreatmentMoveDurationSeconds = 1.5
- _patientBInitialMonitorParameters.bpm = 120
- _patientCInitialMonitorParameters.bpm = 120
- _patientsToCtMoveDurationSeconds = 2.0
- _playerAMoveToTriageDurationSeconds = 1.0
- _ctTransferFadeHoldSeconds = 1.0
- _ctTransferFadeAutoHideSeconds = 0
- _uiPanelAutoHideSeconds = 0 (자동 숨김 없음)

검증 기준:
- 참조가 없는 항목은 예외 없이 건너뛰고 로그/시스템 메시지만 출력되어야 한다.
- 시나리오 흐름은 멈추지 않고 다음 노드로 진행되어야 한다.
