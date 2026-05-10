---
title: "재난 시나리오 미구현 기능 구현 준비서 (안전 우선)"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 재난 시나리오 미구현 기능 구현 준비서 (안전 우선)

이 문서는 현재 시스템 이해가 충분하지 않은 상태에서도, 기존 구현을 해치지 않고 미구현 기능을 순차 구현하기 위한 준비 문서다.

## 1) 적용 원칙

- 기반 시스템(Assets/Modules/MultiplayerInfrastructure)은 수정하지 않는다.
- 구현은 Assets/Modules/TriageTrainer 아래에서만 진행한다.
- 시나리오 흐름 연결은 InvokeEvent 중심으로 구성한다.
- 모든 신규 이벤트는 `Documents/requirements/content-definitions/scenario/event-registry.md`에 먼저 기록하고 상태를 관리한다.
- 기능은 작은 단위(기능 카드)로 쪼개서 1개씩 완료한다.

근거 문서:
- [Documents/requirements/gameplay/interaction/interaction-feature-spec.md](../../gameplay/interaction/interaction-feature-spec.md)
- [Documents/api-references/architecture/multiplayer-infrastructure-overview.md](../../../api-references/architecture/multiplayer-infrastructure-overview.md)
- [Documents/requirements/content-definitions/scenario/scenario-authoring-guide.md](scenario-authoring-guide.md)
- [Documents/requirements/content-definitions/scenario/scenario-graph-spec.md](scenario-graph-spec.md)

## 2) 현재 부족한 내용 (구현 관점)

### A. 런타임 시나리오 그래프 부족

현재 Resources/Scenario에는 샘플/검증 그래프만 있고, 실제 3개 본 시나리오(disaster_intro, patient_a_critical, patient_b_c_ct) 런타임 JSON이 없다.

대상 경로:
- [Assets/Modules/TriageTrainer/Resources/Scenario](../../../../Assets/Modules/TriageTrainer/Resources/Scenario)

### B. 이벤트 핸들러 연결 지점 부족

시나리오 문서의 EventIdentifier는 많지만, TriageTrainer 모듈에서 ScenarioEventIdentifierRegistry.Register를 수행하는 전용 부트스트랩이 없다.

현재 확인된 등록 방식:
- Datapack 경유 동적 등록 중심
- TriageTrainer의 고정 이벤트 등록 스크립트 부재

### C. 문서-엔진 표현 차이

아래 항목은 그대로 JSON화하면 실패 가능성이 크다.

- Parallel.WaitMode: 문서 WaitAll, 엔진 All/Any/None
- Parallel.AllocationType: 문서 ByRole, 엔진 SelfAll/RandomOneAll/SpreadRandom/SpreadOrdinary
- Choice: 문서 일부가 ChoiceOptionNode 별도 노드 형태, 엔진은 Choice.options 배열 형태
- Validator.Condition: 문서는 Click_xxx/Enter_xxx 등 도메인 문자열, 엔진은 PlayerCount 계열 enum

## 3) 저위험 구현 전략

### 단계 1. 그래프 추가보다 이벤트부터 구현

먼저 이벤트 구현 기반을 만든 뒤 그래프를 붙인다.

1. TriageTrainer 전용 이벤트 부트스트랩 작성
2. 핵심 이벤트 3~5개를 먼저 연결
3. 이벤트 단위로 플레이모드 검증
4. 그 다음 시나리오 JSON을 추가

이유:
- 그래프부터 작성하면 이벤트 미연결로 실행이 막힌다.
- 이벤트부터 준비하면 실패 지점을 빠르게 고립시킬 수 있다.

### 단계 2. MVP 범위를 disaster_intro로 고정

초기 범위를 아래 이벤트로 한정한다.

- triage_patientA_dummyA
- show_patientA_ui
- show_dummyA_ui
- B_C_D_to_triage

이유:
- 문서 상 가장 짧은 진입 시나리오라 검증 사이클이 짧다.
- 환자 이동/UI 출력/역할 분기의 핵심 패턴을 모두 포함한다.

### 단계 3. 환자 A, B/C 시나리오는 패턴 복제로 확장

intro에서 검증된 패턴(환자 스폰/이동, UI 패널 표시, 장비 시각 적용, 모니터 출력)을 patient_a_critical, patient_b_c_ct에 재사용한다.

## 4) 구현 전에 먼저 만들 준비물

### 4-1. 이벤트 부트스트랩 스크립트

권장 위치:
- Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs

역할:
- OnEnable: 이벤트 등록
- OnDisable/OnDestroy: 이벤트 해제
- 코루틴 반환 핸들러 표준화

### 4-2. 기능 카드 파일

권장 위치:
- Documents/scenario/cards/

카드 1개당 필수 항목:
- 기능명
- EventIdentifier
- 실행 주체
- 대상
- 선행 조건
- 입력
- 출력
- 완료 조건
- 실패 처리

### 4-3. 시나리오 JSON 변환 규칙표

권장 위치:
- Documents/scenario/json-conversion-rules.md

필수 규칙:
- WaitAll -> All
- ByRole -> SelfAll + requiredRoleIdentifiers 활용
- 태그 분기 -> requiredPlayerTags + requiredPlayerTagsMatchMode 활용
- 제외 태그 -> forbiddenPlayerTags 활용
- 그래프 상위 tags 선언 권장 (미선언 태그 사용 시 로더 경고)
- PlayerTag 노드는 TagModification 표기를 권장 (PlayerTag 하위 호환)
- ChoiceOptionNode 분해 -> Choice.options 병합
- Validator의 도메인 조건은 Interaction/InvokeEvent로 치환

현재 작성됨:
- Documents/scenario/json-conversion-rules.md

## 5) 1차 구현 대상 백로그 (우선순위)

### P0 (즉시)

- TriageScenarioEventBootstrap 추가
- disaster_intro의 4개 이벤트 구현
- 이벤트 레지스트리 상태를 implemented로 갱신

### P1 (다음)

- move_patientA_to_treatmentroom
- activate_vital_monitor_ui_patientA
- show_suction_checklist_ui / hide_suction_checklist_ui

### P2 (확장)

- patient_b_c_ct의 UI 표시/환자 이동 이벤트
- 모니터/동공반사/CT 이동 이벤트

## 6) 위험요소와 회피

- 위험: Validator를 도메인 조건 검사기로 오해하여 엔진 수정 시도
- 회피: Validator는 플레이어 수 조건에서만 사용, 도메인 진행은 Interaction + InvokeEvent 중심

- 위험: 문서 표기값(WaitAll, ByRole, Immediate)을 그대로 JSON에 넣음
- 회피: 엔진 enum 기준으로 변환 후 저장

- 위험: 이벤트 이름 오탈자
- 회피: event-registry와 시나리오 문서를 교차검증하고 상수화

## 7) 구현 시작 체크리스트

- [ ] TriageTrainer 이벤트 부트스트랩 스크립트 생성
- [ ] disaster_intro 이벤트 4개 기능 카드 작성
- [ ] 이벤트용 대상 오브젝트 식별자 확정(환자/침대/UI 패널)
- [ ] Resources/Scenario에 intro 전용 런타임 그래프 JSON 생성
- [ ] 플레이모드에서 이벤트 단위 수동 호출 테스트
- [ ] event-registry 상태 갱신

## 8) 이번 준비 문서의 결론

현 시점의 최적 경로는 그래프 대규모 작성이 아니라, 이벤트 부트스트랩과 intro 핵심 이벤트부터 작게 완성하는 방식이다. 이 경로는 기존 시스템 변경 없이도 진행 가능하며, 이해 부족으로 인한 회귀 위험을 가장 낮춘다.

## 9) 생성 완료 산출물

- 이벤트 부트스트랩 스크립트(placeholder):
	- Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs
- intro MVP 런타임 그래프(JSON):
	- Assets/Modules/TriageTrainer/Resources/Scenario/disaster_intro_mvp.json
- intro 기능 카드 4종:
	- Documents/scenario/cards/disaster_intro-triage_patientA_dummyA.md
	- Documents/scenario/cards/disaster_intro-show_patientA_ui.md
	- Documents/scenario/cards/disaster_intro-show_dummyA_ui.md
	- Documents/scenario/cards/disaster_intro-B_C_D_to_triage.md
- 남은 이벤트 구현 체크리스트:
	- Documents/scenario/remaining-implementation-checklist.md

## 10) 바로 실행할 다음 단계

- 씬에 TriageScenarioEventBootstrap 컴포넌트 1개 배치
- RegistryPreloader 또는 Registry 등록 경로에 disaster_intro_mvp JSON TextAsset 등록
- 서버 커맨드로 scenarioIdentifier=disaster_intro_mvp 실행
- Console에서 이벤트 호출 로그 4개 확인
	- triage_patientA_dummyA
	- show_patientA_ui
	- show_dummyA_ui
	- B_C_D_to_triage

## 11) 인스펙터 연결 체크리스트 (MVP)

대상 스크립트:
- Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs

필수 연결:
- _disasterIntroMvpGraph: Assets/Modules/TriageTrainer/Resources/Scenario/disaster_intro_mvp.json TextAsset
- _disasterIntroMvpGraphIdentifier: disaster_intro_mvp

권장 연결(연출 확인용):
- _patientAObject, _dummyAObject
- _patientABedObject, _dummyABedObject
- _patientASpawnPoint, _dummyASpawnPoint
- _patientABedSpawnPoint, _dummyABedSpawnPoint
- _patientAUiPanel, _dummyAUiPanel
- _nurseBTransform, _nurseCTransform, _nurseDTransform
- _triageArrivalPoint
- _patientATreatmentBedObject, _patientATreatmentRoomPoint
- _patientAVitalMonitorObject, _patientAVitalPanel
- _suctionChecklistUiPanel
- _intuChecklistUiPanel
- _ivChecklistUiPanel
- _patientAEtTubePreparedVisual, _patientAEtTubeInsertedVisual, _patientAEtTubeWithoutStyletVisual
- _patientATPieceConnectedVisual
- _patientAGauzeVisual, _patientAGauzeWithPlasterVisual
- _patientA18gLeftVisual, _patientANs1LeftConnectedVisual
- _patientA18gRightVisual, _patientAPs1RightConnectedVisual
- _patientACentralLineVisual, _level1ReadyVisual
- _patientAAmbuConnectedVisual, _patientADefibPadVisual
- _defibIrregularUiPanel
- _ambuBaggingAnimators, _chestCompressionAnimators
- _patientBObject, _patientCObject, _dummyBObject
- _patientBSpawnPoint, _patientCSpawnPoint, _dummyBSpawnPoint
- _patientBUiPanel, _patientCUiPanel, _dummyBUiPanel
- _patientBTreatmentBedObject, _patientCTreatmentBedObject
- _patientBTreatmentRoomPoint, _patientCTreatmentRoomPoint
- _patientBVitalMonitorObject, _patientCVitalMonitorObject
- _patientBVitalPanel, _patientCVitalPanel
- _patientBPupilReflexUiPanel, _patientCPupilReflexUiPanel
- _patientBCtRoomPoint, _patientCCtRoomPoint
- _patientBPupilLeftReactiveIndicator, _patientBPupilRightFixedIndicator
- _patientCPupilLeftFixedIndicator, _patientCPupilRightReactiveIndicator
- _ctTransferFadePanel
- _patientBGauzeVisual, _patientBGauzeWithPlasterVisual
- _patientCGauzeVisual, _patientCGauzeWithPlasterVisual
- _patientB20gRightVisual, _patientBNs1RightConnectedVisual
- _patientC20gLeftVisual, _patientCNs1LeftConnectedVisual

식별자 기반 자동 조회(수동 참조 누락 대비):
- _autoResolveReferencesFromRegistry = true
- _patientAEntityIdentifier = patientA
- _dummyAEntityIdentifier = dummyA
- _patientABedEntityIdentifier = patientABed
- _dummyABedEntityIdentifier = dummyABed
- _nurseBEntityIdentifier = NurseB
- _nurseAEntityIdentifier = NurseA
- _nurseCEntityIdentifier = NurseC
- _nurseDEntityIdentifier = NurseD
- _patientATreatmentBedEntityIdentifier = patientATreatmentBed
- _patientAVitalMonitorEntityIdentifier = patientA_monitor
- _patientBEntityIdentifier = patientB
- _patientCEntityIdentifier = patientC
- _dummyBEntityIdentifier = dummyB
- _patientBTreatmentBedEntityIdentifier = patientBTreatmentBed
- _patientCTreatmentBedEntityIdentifier = patientCTreatmentBed
- _patientBVitalMonitorEntityIdentifier = patientB_monitor
- _patientCVitalMonitorEntityIdentifier = patientC_monitor

별칭 기반 휴리스틱 조회(키 불확실 환경 대비):
- _patientAAliases / _dummyAAliases
- _patientABedAliases / _dummyABedAliases
- _nurseBAliases / _nurseCAliases / _nurseDAliases
- _nurseAAliases / _nurseBAliases / _nurseCAliases / _nurseDAliases
- _patientATreatmentBedAliases / _patientAVitalMonitorAliases
- _patientBAliases / _patientCAliases / _dummyBAliases
- _patientBTreatmentBedAliases / _patientCTreatmentBedAliases
- _patientBVitalMonitorAliases / _patientCVitalMonitorAliases
- 조회 순서:
	1) Registry(Entity/Npc) exact key
	2) Npc 컴포넌트의 Identifier / GameObject.name 매칭
	3) GameObject.Find(alias)

triage_patientA_dummyA 이벤트 동작(현재 MVP+):
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
- triage_patientB_patientC_dummyB:
	- patientB/patientC/dummyB 오브젝트 활성화 후 지정 스폰 포인트로 이동
	- _patientBcdSpawnMoveDurationSeconds=0이면 즉시 배치
- show_patientB_ui / show_dummyB_ui / show_patient_c_ui:
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
- 따라서 코드 정적 분석만으로 patientA/dummyA/NurseB/C/D 키를 확정할 수 없다.

키 확정 절차:
- TriageScenarioEventBootstrap의 _logRegistrySnapshotOnEnable = true 유지
- 플레이모드 진입 후 Console의 "Registry snapshot" 로그에서 실제 key를 확인
- 확인된 key를 _patientAEntityIdentifier / _dummyAEntityIdentifier / _nurseBEntityIdentifier ... 에 반영
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
