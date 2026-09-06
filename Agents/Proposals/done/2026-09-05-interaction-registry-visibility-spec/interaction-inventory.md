# 인터렉션 데이터 전수조사 (2026-09-05)

이 문서는 인터렉션 등록·가시성 체계를 재설계하기 전에, 현재 저장소에서 인터렉션을 정의하거나
그 노출 여부를 바꾸는 모든 데이터와 코드를 조사한 결과입니다. 조사 범위는 `MultiplayerInfrastructure`,
`TriageTrainer` 두 모듈의 스크립트, 프리팹, 씬, ScriptableObject, 시나리오 JSON, 퀘스트 JSON입니다.
같은 폴더의 [`PROPOSAL.md`](./PROPOSAL.md)가 이 조사를 근거로 명세를 제안합니다.

조사 방법은 다음과 같습니다.

- `IInteract`, `IInteractable`, `Interactable` 을 구현하거나 상속하는 클래스를 모두 나열했습니다.
- 위 컴포넌트의 스크립트 GUID로 프리팹, 씬, 에셋의 YAML 문서를 찾아 직렬화된 인터렉션 필드를 추출했습니다.
- 시나리오 JSON 15개와 퀘스트 JSON 6개에서 인터렉션에 관여하는 노드와 정의를 추출했습니다.
- 런타임에 인터렉션을 추가·활성·비활성하는 호출 지점을 모두 찾았습니다.

## A. 프리팹·씬·에셋에 직렬화된 인터렉션 데이터

이 절의 항목이 요구사항 1번("컴포넌트에 사전 작성된 데이터에 의존하지 않는다")의 직접 대상입니다.

### A-1. `PatientTypeA.prefab` (`Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/`)

`ScenarioActionInteractable` 12개가 환자 하위 오브젝트에 부착되어 있습니다. `_interactionIdentifier`가
비어 있으면 완료 신호를 인터렉션 식별자로 대신 씁니다.

| 표시 문구 | 완료 신호 | 소유 엔티티 | 역할 태그 | 초기 활성 | 비고 |
|---|---|---|---|---|---|
| 앰부배깅 시작 | `start_ambu_r1` | `patient_a` | 없음 | 꺼짐 | 플래그 `scen_a.cpr1_actions` |
| 앰부배깅 시작 | `start_ambu_r2` | `patient_a` | 없음 | 꺼짐 | 플래그 `scen_a.cpr2_actions` |
| 가슴압박 수행 | `click_to_start_comp` | `patient_a` | `nurse_b` | 꺼짐 | 플래그 `scen_a.cpr1_actions` |
| 가슴압박 수행 | `interact_chest` | `patient_a` | `nurse_a` | 꺼짐 | 플래그 `scen_a.cpr2_actions` |
| 제세동 패드 부착 | `interact_patient_chest` | `patient_a` | 없음 | 꺼짐 | 플래그와 함께 퀘스트 바인딩 필수 |
| T-piece 연결 해제 | `remove_tpiece` | `patient_a` | 없음 | 꺼짐 | 플래그 `scen_a.cpr1_actions` |
| 기관내관에 앰부백 연결 | `connect_ambubag` | `patient_a` | 없음 | 꺼짐 | 플래그 `scen_a.cpr1_actions` |
| 앰부백에 산소 저장낭 연결 | `connect_o2_to_ambu` | `patient_a` | 없음 | 꺼짐 | 플래그 `scen_a.cpr1_actions` |
| T-Piece 연결 | `interact_tpiece` | `patient_a` | 없음 | 꺼짐 | 요구 아이템 `tpiece_set`, 식별자 명시 |
| 스타일렛 제거 | `remove_intu_stylet` | `patient_a` | 없음 | 꺼짐 | 식별자 명시 |
| 의복 제거 | `remove_patient_clothing` | `patient_a` | 없음 | 꺼짐 | 플래그 `scen_a.clothing_removal` |
| 환자 활력징후 및 외양 평가 시행하기 | `assess_patient_a_triage` | (비어 있음) | 없음 | 켜짐 | 소유 엔티티가 없어 퀘스트 마크 대상이 될 수 없음 |

`PatientController` 직렬화 데이터:

| 필드 | 값 |
|---|---|
| `_identifier` | `patient_a` |
| `_assessActions` | `assess_vital`(요구 아이템 `vital_set`), `assess_pulse_r1`, `assess_pulse_r2`, `assess_gcs_rosc`, `assess_avpu_gcs`, `assess_pulse`, `assess_gcs`. 7개 모두 초기 비활성 |
| `_interactConfigs` | `lift_from_bed` 꺼짐, `carry_patient` 켜짐, `monitor_select` 켜짐 |
| `_triageConfig` | `_assessable`, 표시 문구, 완료 후 재노출 정책 |
| `_intravenousLineCannulaConfig` | 지원 여부, 표시 문구, 좌·우 팔 지원 |

### A-2. `PatientTypeBFemale.prefab`, `PatientTypeBMale.prefab`, `PatientTypeDDummyB.prefab`

| 필드 | 값 |
|---|---|
| `_identifier` | `patient`(B 두 종), `patient_dummy_d_b` |
| `_assessActions` | `assess_avpu_gcs`, `assess_pulse`, `assess_gcs`, `assess_vital`. 4개 모두 초기 비활성 |
| `_interactConfigs` | `lift_from_bed` 꺼짐, `carry_patient` 켜짐, `monitor_select` 켜짐 |
| `_triageConfig._displayText` | "트리아지 분류"(B 두 종) |

B 환자의 프리팹 식별자는 `patient`이고 런타임에는 EntityPresetSpawn이 `patient_b`, `patient_c`를 주입합니다.

### A-3. `PatientTypeDDummyA.prefab`

- `ScenarioActionInteractable` 1개: "환자 활력징후 및 외양 평가 시행하기", 완료 신호
  `assess_patient_dummy_d_a_triage`, 소유 엔티티 비어 있음, 초기 활성.
- `PatientController`: `_identifier` `patient_dummy_d_a`, 사정 4개 비활성, `lift_from_bed` 켜짐.

### A-4. NPC 프리팹과 NPC 기본 모델 에셋

| 파일 | 내용 |
|---|---|
| `Prefabs/Entities/NPC/DoctorNPCHat.prefab` | `Npc._identifier` `npc-doctor-1`, `_displayText` "말 걸기", 시나리오·제출·커스텀 목록 모두 비어 있음 |
| `Prefabs/Entities/NPC/TutorialNPCHat.prefab` | `Npc._identifier` `npc-tutorial-guide-hat`, `_displayText` "말 걸기", 목록 비어 있음 |
| `MultiplayerInfrastructure/ScriptableObjects/NPCBaseModels/DemoScenarioGivingNPC.asset` | `scenarioInteracts` 1개(`disaster_intro`, 시작 노드 `D001`, 표시 문구 "[NPC] 데모 시나리오 시작") |

NPC 프리팹의 `_displayText` "말 걸기"는 `Interactable` 기반 필드이지만, `Npc.Interacts`가 목록을 다시
만들기 때문에 실제 힌트에는 쓰이지 않습니다.

### A-5. 씬에 직접 배치된 인터렉션

| 씬 | 컴포넌트 | 내용 |
|---|---|---|
| `Assets/Scenes/IndevScene.unity` | `Npc` `demo_scenario_giving_npc` | 시나리오 인터렉트 1개(`disaster_intro`), 제출 인터렉트 1개(요구 `laryngoscope`, 완료 신호 없음, `consumeOnce` 꺼짐) |
| `Assets/Scenes/TutorialScene.unity` | `TutorialDecoyInteractable` 3개 | "택배: 8909", "우편: 김강산님", "배달: 밤샘 청년". 표시 문구와 안내 독백이 인스펙터 값 |
| `Assets/Scenes/test/Test-IntravenousLineConnection.unity` 외 2개 테스트 씬 | `Npc` `demo_scenario_giving_npc` | IndevScene과 같은 구성 |
| `Assets/Scenes/OverworldScene.unity` | 프리팹 인스턴스 오버라이드 | 환자·NPC·`ScenarioActionInteractable` 컴포넌트의 별도 인스턴스는 검출되지 않았습니다. 장비·설치물 프리팹 인스턴스의 표시 문구 오버라이드가 존재할 수 있으며, 이번 조사에서는 항목별로 나열하지 않았습니다 |

튜토리얼 미끼 인터렉션 세 개는 엔티티 식별자가 없어, 현재 구조로는 식별자 기반 등록 대상이 될 수 없습니다.

### A-6. `Interactable` 기반 클래스의 공통 직렬화 필드

`Interactable` 추상 클래스는 `_displayText`, `_displayIcon`, `_displayIcons`, `_displayColor`,
`_presentationEntityIdentifier`, `_interactionIdentifier`를 직렬화합니다. 이를 상속하는 클래스는
`Npc`, `ItemSubmissionInteractable`, `StaticObjectDisplayment`(벽 산소유량계, 벽 흡인기), `StaticPlacedItem`입니다.
따라서 다음 프리팹이 이 필드를 갖습니다.

| 프리팹 | 관련 필드 |
|---|---|
| `Prefabs/StaticAttachmentDisplayments/WallAttachedOxyflowmeter/oxyflowmeter.prefab` | 표시 문구, `_attachedInteractSignal`(조작 신호), 설치 상태별 문구 |
| `Prefabs/StaticAttachmentDisplayments/WallAttachedWallSuction/WallSuction.prefab` | 표시 문구, `_attachCompletionSignal`(값 `connect_wall_component_1`이면 환자 A 설치 대상으로 판정) |
| `Prefabs/Entities/MinecraftBoatLikes/PatientMovingBed.prefab` | `_reposeDisplayText`, `_enableMovementInteraction`, 이동 상호작용 표시 문구 |
| `Prefabs/Entities/MinecraftBoatLikes/Defibrillator.prefab` | `_displayText` |
| `Prefabs/Entities/MinecraftBoatLikes/level1_rapid_infuser.prefab` | `_displayText` |
| `MultiplayerInfrastructure/Prefabs/Player.prefab` | `InteractableEntityResolver.handlerSources`(비어 있음, 레거시) |

그 밖에 `PatientMonitorController._interactEntries`(`select_patient_mode`, `detail_overlay`, `disconnect_patient`의
활성 여부), `IntravenousLineConnectionPoint`의 인터렉트별 `_enabled`, `OxyLinePairInteractable._displayText`,
`StretcherController._displayText`가 프리팹에 직렬화됩니다.

### A-7. 레거시 컴포넌트

- `ScenarioInteractable`(MultiplayerInfrastructure, NetworkBehaviour): 두 모듈의 프리팹과 씬에서 인스턴스가
  검출되지 않았습니다. `Npc`의 시나리오 인터렉트가 같은 역할을 대신합니다.
- `InteractableEntityResolver`: `Player.prefab`에 빈 목록으로만 남아 있고, 플레이어 인터렉션 경로는
  `NearbyInteractablesDetector`와 `PlayerController.Interactables`가 담당합니다.
- `LootableItemInteractHandler`: 월드 아이템 획득용 레거시 핸들러입니다.
- `ScenarioDevStub`: 디버그 전용입니다.

## B. 시나리오 JSON과 퀘스트 JSON

### B-1. 시나리오 최상위 `actingNpcs[].interactions`

| 시나리오 | NPC | 인터렉션 |
|---|---|---|
| `patient_a_critical` | `npc-doctor-patient-a-critical` | ItemSubmission 4개: `patient-a-doctor-submit-laryngoscope`(`sig.pass_laryngoscope`), `-et-tube`(`sig.pass_et_tube_ready`), `-5cc-syringe`(`sig.pass_syringe`), `-central-line-set`(`sig.pass_central_line_set`). 모두 `enabled: false` |
| `tutorial` | `npc-tutorial-guide-hat` | Signal 1개 `npc-tutorial-guide-hat__interaction-talk-start`(`tutorial_hat_talk_start`, 활성), ItemSubmission 1개 `tutorial-guide-hat-package-submission`(`tutorial.delivery.package.submitted`, 비활성) |
| `patient_b_c_ct` | `npc-doctor-patient-b-c-ct` | 없음 |

이 정의는 시나리오 시작 시 서버가 NPC를 스폰하고 `ObserversConfigureScenarioActingNpc`로 전 피어에
복제하며, 늦게 접속한 피어에게도 다시 적용됩니다. 현재 구조에서 유일하게 "시나리오 데이터에서
일괄 등록되고 복제되는" 인터렉션 정의입니다.

### B-2. 인터렉션 활성 상태를 바꾸는 노드

| 시나리오 | 노드 | 대상 | 효과 |
|---|---|---|---|
| `patient_a_critical` | `ItemSubmissionConfig` 4개(`ISC_PASS_*`) | 의사 NPC 제출 인터렉션 4개 | 요구 아이템·완료 신호 덮어쓰기. `enabled`를 생략했으므로 기본값 true가 적용되어 해당 시점에 활성화됨 |
| `tutorial` | `ItemSubmissionConfig` 3개 | `tutorial-guide-hat-package-submission` | 시계 제출 설정, 택배 제출 설정, 마지막에 `enabled: false` |
| `tutorial` | `NPCControl`(Update) 1개 | `npc-tutorial-guide-hat` | 표시명만 갱신(`interactOperation: None`) |
| 모든 시나리오 | `NpcInteractControl` | 없음 | 사용 사례 0건. `NPCControl` Update 모드의 `interactOperation`과 기능이 겹침 |
| `disaster_intro`, 예제 2개 | `Interaction` 노드 | 트리거 존 식별자 | 완료 조건 핸들러가 있으면 실행할 뿐, 인터렉션 등록이나 가시성에는 관여하지 않음 |
| `patient_a_critical` | `QuestMark` 2개 | 웨이포인트 | 인터렉션 대상 아님 |
| `patient_b_c_ct` | `QuestMark` 2개 | NPC 머리 위 마크 | 인터렉션 대상 아님 |

### B-3. `InvokeEvent`로 호출되어 인터렉션을 여는 코드 이벤트

| 시나리오 | 이벤트 식별자 | 실제 동작 | 상태 저장 위치 |
|---|---|---|---|
| `patient_a_critical` | `activate_patient_a_vital_assess`, `_avpu_gcs_assess`, `_arrest_actions`, `_stylet_removal`, `_tpiece_attach`, `_cpr2_actions`, `_clothing_removal` | 플레이어별 퀘스트 상태 플래그 설정(태그별 또는 전원) | `PlayerQuestStateFlag` 레지스트리(서버 권위, 복제됨) |
| `patient_a_critical` | `rosc_monitor_ui` | `scen_a.rosc_pulse_assess`, `scen_a.rosc_gcs_assess` 플래그 설정 | 위와 같음 |
| `patient_a_critical` | `prepare_patient_a_manual_*` 6개 | 수동 진입 준비. `ScenarioActionInteractable` 완료 표시 초기화, `scen_a.cpr1_actions` 재설정 | 컴포넌트 로컬 + 플래그 풀 |
| `patient_a_critical` | `activate_vital_monitor_ui_patient_a` | 모니터 오브젝트 활성화, 닫기 무장 | GameObject 활성 상태(로컬) |
| `patient_b_c_ct` | `activate_patient_{b,c}_recognition_{1..4}`, `_strength_check`, `_pupil_check` (12개) | `PatientController.ActivateRecognitionCheck` | `SyncList<PatientRecognitionCheckEntry>`(복제됨) |
| `patient_b_c_ct` | `activate_patient_{b,c}_nurse_{c,d}_treatment` (4개) | 환자 B/C 처치 단계 전이 | `SyncVar` 단계 값(복제됨) |
| `patient_b_c_ct` | `activate_vital_monitor_ui_patient_{b,c}` | 모니터 활성화 | GameObject 활성 상태(로컬) |
| `patient_b_c_ct` | `reset_patient_*_triage_attempt` | 트리아지 재노출 | `SyncVar _assessable`(복제됨) |
| `tutorial` | `show_tutorial_interaction_hint` | 안내 UI | 인터렉션 등록과 무관 |

### B-4. 퀘스트 정의의 `presentationBindings`(targetType `Interaction`)

퀘스트 바인딩은 원래 아이콘 표시용이지만, 아래 표의 일부 인터렉션은 `HasActiveInteractionBinding`으로
노출 자체를 판정합니다. 즉 현재 구조에서는 퀘스트 바인딩이 암묵적인 가시성 조건입니다.

| 파일 | Interaction 바인딩 수 | 노출 판정에 쓰이는 주소 |
|---|---|---|
| `TriageTrainer/Resources/Quest/patient_a_critical.quests.quest.json` | 47 | `patient_a/item_apply`, `patient_a/wall_suction_use`, `patient_a/patient_a_use_*` 6개, `patient_a/interact_patient_chest` |
| `TriageTrainer/Resources/Quest/patient_b_c_ct.quests.quest.json` | 37 | 없음(아이콘 표시 전용) |
| `TriageTrainer/Resources/Quest/disaster_intro.quests.quest.json`, `tutorial.quests.quest.json` | 0 | 없음 |
| `MultiplayerInfrastructure/Resources/Quest/tutorial_delivery.quest.json`, `test_ordinal_waypoint.quest.json` | 0 | 없음 |

바인딩이 참조하는 엔티티 식별자 어휘: `patient_a`, `patient_b`, `patient_c`, `patient_dummy_d_b`, `bed_a`,
`bed_b`, `bed_c`, `patient_monitor`(공통), `patient_a_wall_suction`, `patient_a_oxyflowmeter`,
`zone_{0..3}:oxyflowmeter`, `level1_rapid_infuser_a`, `npc-doctor-patient-a-critical`.

## C. 코드 리터럴로 정의된 인터렉션

표의 "노출 입력"은 지금 `CanInteract`가 참조하는 상태입니다. "복제"는 그 상태가 서버 권위로 전 피어에
동기화되는지 여부입니다.

### C-1. `PatientController` (TriageTrainer/Scripts/Patient)

| 인터렉션 식별자 | 정의 위치 | 표시 문구 | 노출 입력 | 복제 |
|---|---|---|---|---|
| `lift_from_bed` | `Interactions.cs` | "{환자}를 들어올리기" | `_interactConfigs` 활성, 침대 존재 | 아니오(직렬화 로컬) |
| `carry_patient` | `Interactions.cs` | "{환자}를 들어올리기" | `_interactConfigs` 활성, 침대 없음, 운반 중 아님 | 아니오 |
| `monitor_select` | `Interactions.cs` | 인스펙터 문구 | 플레이어 선택 모드(항상 활성으로 고정) | 로컬 모드 |
| `item_apply` | `Interactions.cs` | "환자에게 들고 있는 처치 물품 적용" | 환자 A 아이템 사용 무장 시 숨김, `patient_a_critical`이면 퀘스트 바인딩 필요, 적용 가능 아이템 보유 | 퀘스트는 소유자 피어에만 복제 |
| `patient_bc_nasal_cannula` | `Interactions.cs` | "비강 캐뉼라 적용" | B/C 환자, `_patientBCNurseDStage`, 태그 `nurse_d`, 거리 | SyncVar, 태그 복제 |
| `wall_suction_use` | `WallSuctionUse.cs` | "환자에게 흡인기 사용" | `patient_a_critical`이면 퀘스트 바인딩 필요 | 퀘스트 |
| `triage_assess` | `Triage.cs` | 인스펙터 문구 또는 "트리아지 분류" | `SyncVar _assessable`, 사정 가능 상태 | 예 |
| `assess_*`(인스펙터 목록 + `DefaultAssessActions` 코드 기본값) | `Assess.cs` | 인스펙터 문구 | `patient_a_critical`이면 플래그 풀, 아니면 인스펙터 `Enabled` | 플래그 예 / `Enabled` 아니오 |
| `recognition_check`(항목별 `_completionSignal`) | `RecognitionCheck.cs` | 활성화 정의의 문구 | `SyncList` 항목의 `InteractionEnabled`, 역할 태그, 펜라이트 | 예 |
| 마이크 다시 사용하기 | `RecognitionCheck.cs` | 고정 문구 | 마이크 항목 존재, 장치 상태 | 로컬 |
| `intravenous_line_cannula` | `IntravenousLineCannula.cs` | 인스펙터 문구 | `_supported`, `_intravenousLineCannulaInteractable`, B/C 단계 | 부분 |
| `normal_saline_connect` | `Interactions.cs` | "생리식염수 연결" | B/C 연결 가능 상태 | 예 |
| `patient_a_normal_saline_connect`, `patient_a_plasma_solution_connect` | `PatientAFluidConnection.cs` | "생리식염수 연결", "플라즈마 솔루션 연결" | 환자 A, 캐뉼라 삽입 표시, 연결 지점 존재 | 처치 표시는 복제 |
| `patient_a_use_cervical_collar`, `_gauze`, `_plaster_on_gauze`, `_plaster_on_intubation`, `_epinephrine_5cc_syringe`, `_normal_saline_20cc_syringe` | `PatientAItemUse.cs`(`PatientAItemUseSpecs`) | "환자에게 {아이템명} 사용" | 환자 A + `patient_a_critical` 무장 + 퀘스트 바인딩 | 퀘스트 |

### C-2. 시나리오 단계 인터렉션 게이트 표

`TriageTrainer/Scripts/Scenario/PatientACriticalQuestStateFlags.cs`의 `FlagByInteraction`은 `patient_a`의
인터렉션 주소 16개를 플래그 10개에 대응시키는 코드 리터럴입니다. `GetInteractionDisplayPriority`는
`start_ambu_r1`(1000), `remove_patient_clothing`(900)의 표시 우선순위를 코드로 고정합니다.
`RequiresActiveQuestBinding`은 `patient_a/interact_patient_chest` 한 건을 특례로 둡니다. 이 표는
`patient_a_critical`이 실행 중일 때만 켜집니다.

### C-3. `Npc`와 NPC 파생 인터렉션 (MultiplayerInfrastructure/Scripts/Entity)

| 인터렉션 | 생성 근거 | 인터렉션 식별자 | 노출 입력 |
|---|---|---|---|
| `ScenarioNpcInteract` | `_scenarioInteracts`(인스펙터 또는 `NPCBaseModelSO`) | 시나리오 식별자 | 항상 노출 |
| `ItemSubmissionInteractable`(자동 생성) | `_submissionInteracts` 또는 `actingNpcs[].interactions` | 정의의 식별자 | `_enabled`(로컬), `_completed` |
| `ScenarioActingNpcStartInteract` | `actingNpcs[].interactions`(StartScenario) | 정의의 식별자 | 항상 노출 |
| `ScenarioActingNpcSignalInteract` | `actingNpcs[].interactions`(Signal) | 정의의 식별자 | 항상 노출(`enabled` 값을 읽지 않음) |
| 커스텀 소스 | `_customInteractSources`, `NPCControl` Create/Delete | 소스 컴포넌트에 따름 | 소스에 따름 |

### C-4. 장비·설치물 (TriageTrainer/Scripts/Entities)

| 클래스 | 인터렉션 | 노출 입력 | 시나리오 결합 |
|---|---|---|---|
| `PatientMonitorController` | `select_patient_mode`, `detail_overlay`, `disconnect_patient` | `_interactEntries` 활성(로컬), 최근접 그룹 | `detail_overlay` 실행부가 `"patient_a_critical"` 문자열을 직접 비교 |
| `PatientMonitorMountPoint` | 설치, 회수 | 설치 상태 | 없음 |
| `IntravenousLineConnectionPoint` | 연결 시작, 여기에 연결, 분리 | 항목별 `_enabled`(로컬), 연결 수용 가능 | 없음 |
| `Level1RapidInfuserController` | 조종 토글, 수액 추가(`level1_add_plasma_solution`, `level1_add_blood_bag`), `level1_connect_cline` | 보유 아이템, 장비 상태, 서버에서 `CLineOperatorRoleTag` 검사 | 역할 태그 상수 |
| `MovingPatientBedController` | `move_bed`(조종), 눕히기 | `_enableMovementInteraction`(로컬), 운반 상태, 참가자 | 없음 |
| `DefibrillatorCartController` | 조종 토글 | 운반 상태 | 없음 |
| `StretcherController` | 잡기 | 플레이어 존재 | 없음 |
| `OxyLinePairInteractable` | `connect_oxygen_line` | 양 끝점 해석 가능 | 없음 |
| `WallAttachedWallSuction` | 설치·회수(`wall_suction_install`), `connect_yankauer` | 설치 상태, 환자 A 설치 대상 여부 | `_attachCompletionSignal == "connect_wall_component_1"`로 환자 A 대상을 판정 |
| `WallAttachedOxyflowmeter` | 설치·회수·조작(`oxyflowmeter`) | `ScenarioInteractionSignals.IsRaised`(조작 신호, 환자별 산소 연결 신호) | 신호 이름 규칙에 결합 |
| `PatientCareDescriptionZone` | 인터렉션 없음(신호 발신만) | 해당 없음 | 없음 |
| `StaticPlacedItem`, `LootableItemInteractHandler` | 획득 | 사라짐 상태 | 없음 |

### C-5. 런타임에 인터렉션을 추가·전환하는 호출 지점

| 호출 | 호출자 | 상태 저장 | 복제 |
|---|---|---|---|
| `PatientController.AddInteract / RemoveInteract / SetInteractEnabled` | 정의만 존재, 현재 호출자 없음 | `_interactConfigs` | 아니오 |
| `ScenarioActionInteractable.SetEnabled` | `IInteractToggleable` 경유(NPCControl Update, NpcInteractControl) | `_enabled` | 아니오 |
| `ItemSubmissionInteractable.SetEnabled / SetRequiredItems / SetCompletionSignal` | `ItemSubmissionConfig` 노드 | `_enabled`, `_runtimeDefinition` | 아니오(각 피어가 노드를 실행해 수렴, 늦은 접속자는 제외) |
| `Npc.AddCustomInteractSource / RemoveCustomInteractSource` | `NPCControl` Update Create/Delete | `_customInteractSources` | `PublishNPCControlUpdate`로 복제 |
| `PlayerQuestStateFlagService.SetForTag / SetForAll / SetForAnyTag / Unset` | 환자 A 개방 이벤트, 사정 완료, ROSC | 플래그 풀 | 예(서버 권위, 옵저버 스냅샷) |
| `PatientController.ActivateRecognitionCheck` | 환자 B/C 인지 확인 이벤트 | `SyncList` | 예 |
| `PatientController.ActivatePatientBCNurse{C,D}Stage` | 환자 B/C 처치 이벤트 | `SyncVar` | 예 |
| `ScenarioActionInteractable.ResetAllCompletionsForNewScenarioRun`, `ResetCompletionForScenario` | 시나리오 시작 훅, 수동 진입 준비 | 정적 인스턴스 목록 | 아니오 |
| `PatientACriticalQuestStateFlags.ArmFor / Disarm` | `TriageScenarioEventBootstrap`의 시나리오 시작·종료 훅 | 정적 플래그 | 각 피어가 자체 판정 |

## D. 가시성 제어 경로 매트릭스

현재 인터렉션 노출을 좌우하는 경로는 일곱 가지가 공존합니다.

| 경로 | 예 | 판정 단위 | 서버 권위 | 늦은 접속 복원 | 시나리오 데이터 표현 |
|---|---|---|---|---|---|
| 1. 컴포넌트 `_enabled` + 노드 토글 | `ScenarioActionInteractable`, `ItemSubmissionInteractable`, `PatientMonitor` 항목 | 전역(엔티티 공유) | 아니오 | 아니오 | `ItemSubmissionConfig`, `NPCControl` |
| 2. 플레이어별 퀘스트 상태 플래그 풀 | 환자 A 단계 인터렉션 | 플레이어별 | 예 | 예 | 없음(코드 이벤트 + 코드 표) |
| 3. 퀘스트 표시 바인딩 존재 | `item_apply`, `patient_a_use_*` | 플레이어별(플레이어 범위 퀘스트) | 예 | 예 | 퀘스트 JSON(암묵적) |
| 4. 올라간 신호 | `_requiredRaisedSignals`, 산소유량계 | 전역 | 예 | 예(스냅샷 요청) | 없음 |
| 5. 플레이어 역할 태그 | `_requiredPlayerTag`, `nurse_d` 검사 | 플레이어별 | 예 | 예 | `PlayerTag` 노드(태그 부여만) |
| 6. 엔티티 네트워크 상태 | `SyncVar _assessable`, B/C 단계, 인지 확인 `SyncList` | 전역 | 예 | 예 | 없음(코드 이벤트) |
| 7. 소비 완료 표시 | `consumeOnce` + `_completed` | 전역(컴포넌트) | 아니오 | 아니오 | 없음 |

경로 1과 7이 요구사항의 멀티플레이어 조건("플레이가 막히면 안 된다")을 만족하지 못하는 근본 원인입니다.
예를 들어 `patient_a_critical`의 `ISC_PASS_*` 노드가 의사 NPC 제출 인터렉션을 켠 뒤에 접속한 피어는
`actingNpcs` 정의의 `enabled: false`만 복제받아 제출 인터렉션을 볼 수 없습니다.

`EntityTag` 노드가 부여하는 엔티티 태그는 `PlayerTagService.AddTagToIdentifier`로 서버에만 기록되고,
플레이어 소유자가 아닌 식별자에 대해서는 복제 경로가 없습니다. 엔티티 태그를 조건으로 삼으려면
복제가 선행되어야 합니다.

## E. 마이그레이션 분류 요약

| 분류 | 대상 | 이전 방향 |
|---|---|---|
| E-1. 프리팹 직렬화 인터렉션 정의 | A-1 ~ A-3의 `ScenarioActionInteractable` 13개, `_assessActions` 23개(5 프리팹), `_interactConfigs` 15개, `_triageConfig`, `_intravenousLineCannulaConfig` | 코드 리터럴(환자 공통 기본 정의) + 시나리오 데이터(시나리오별 문구·조건) |
| E-2. 씬 직접 배치 정의 | IndevScene·테스트 씬의 데모 NPC, TutorialScene 미끼 3개 | 시나리오 데이터. 미끼 오브젝트는 엔티티 식별자 부여가 선행 |
| E-3. ScriptableObject 정의 | `DemoScenarioGivingNPC.asset` | 시나리오 데이터 또는 삭제 |
| E-4. 시나리오 데이터 정의 | `actingNpcs[].interactions` | 유지하되 새 공통 정의 형식으로 통합 |
| E-5. 노드 기반 토글 | `ItemSubmissionConfig.enabled`, `NPCControl` Update, `NpcInteractControl` | 트리거 방식 노드(`InteractionVisibility`)로 대체 |
| E-6. 코드 이벤트 기반 개방 | B-3의 `activate_*` 이벤트 26개, 플래그 표, `PatientBCRecognitionActivations` | 조건 기반 가시성 데이터로 이전. 코드 이벤트는 엔티티 상태 변경만 담당 |
| E-7. 암묵적 퀘스트 결합 | `HasActiveInteractionBinding` 판정 9건, `RequiresActiveQuestBinding` | 명시적 조건 절(`PlayerHasQuest`)로 전환 |
| E-8. 하드코딩된 시나리오 결합 | 모니터 `"patient_a_critical"` 비교, 벽 흡인기 신호 이름 비교, `IsPatientAItemUseArmed` | 조건 절 또는 시나리오 데이터의 표시 정의로 이전 |
| E-9. 유지(내재 능력 조건) | 운반 중 여부, 침대 존재, 보유 아이템, 거리, 선택 모드, 연결 수용 가능, 참가자 상태 | 코드 유지. 가시성 조건과 분리해 "수행 가능 조건"으로 명세 |
| E-10. 레거시 | `ScenarioInteractable`, `InteractableEntityResolver.handlerSources`, `Interaction` 노드, `NpcInteractControl` 노드 | 폐기 또는 호환 유지 여부 결정 필요 |
