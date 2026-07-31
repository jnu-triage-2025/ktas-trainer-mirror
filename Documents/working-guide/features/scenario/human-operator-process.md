---
title: "재난 시나리오 인게임화 — 인간 작업 프로세스(운영자용)"
doc_type: requirement
domain: content-definitions
progress: "1-designed"
status: active
updated: 2026-06-23
flags: []
---

# 재난 시나리오 인게임화 — 인간 작업 프로세스(운영자용)

이 문서는 환자 A/B/C 재난 시나리오를 **실제로 플레이 가능한 상태**로 만들기 위해
비개발 운영자(Unity 에디터 작업자)가 수행해야 할 일을 **순서대로** 정리한 체크리스트다.
코드/데이터 작업(시나리오 변환, 이벤트 키 정합, 역할 태그, 엔진 확장, 게이트 신호 계측)은
이미 완료되었고, 남은 것은 **씬 구성 + 식별자 지정 + 리소스 배치 + 실행/검증** 이다.

세부 근거 문서(필요 시 참조):
- 실행/씬 셋업: [`patient-a-b-c-play-setup-guide.md`](./patient-a-b-c-play-setup-guide.md)
- 인터랙션 신호 연결: [`interaction-signal-integration-spec.md`](./interaction-signal-integration-spec.md)
- 인스펙터 상세 체크리스트: [`implementation-prep.md`](./implementation-prep.md)
- 변환/설계 결정 기록: [`patient-a-b-c-conversion-notes.md`](./patient-a-b-c-conversion-notes.md)

> 핵심 개념: 시나리오는 "학습자가 인터랙션을 완료했는가"를 **완료 신호 `sig.*`** 로 검사한다.
> 게임플레이 코드는 인터랙션 완료 시 자동으로 신호를 올리지만, **어떤 오브젝트가 어떤 신호를
> 올릴지는 그 오브젝트의 `Identifier` 로 결정** 된다. 따라서 운영자의 핵심 작업은 "씬 오브젝트의
> Identifier 를 시나리오가 기대하는 조건명에 맞추는 것" 이다. (현재 미설정이어도 시나리오는
> 자동 통과(onFailure:Ignore)되어 데모는 가능하다.)

---

## 단계 0. 사전 확인 (1회)

- [ ] Unity 에디터에서 대상 씬(예: `IndevScene`)을 연다.
- [ ] 빌드/컴파일 오류가 없는지 확인한다(엔진 SPEC 및 신호 계측 코드 포함).
- [ ] `/scenario list` 가 동작하는지(서버/호스트 실행 후) 확인한다.

## 단계 1. 부트스트랩 & UI 배치

- [ ] 씬에 `TriageScenarioEventBootstrap` 컴포넌트를 가진 GameObject 가 1개 있는지 확인(없으면 생성).
- [ ] 대화/선택 UI(`DialoguePanelUIController` 등)가 씬/레지스트리에 존재하는지 확인.
- [ ] 부트스트랩 인스펙터에서 `_autoResolveReferencesFromRegistry = true`,
      `_logRegistrySnapshotOnEnable = true` 설정(검증 편의).
- 참조: [`patient-a-b-c-play-setup-guide.md`](./patient-a-b-c-play-setup-guide.md) §1, §3.

## 단계 2. 등장 오브젝트 배치 + 식별자 지정

씬에 아래 오브젝트를 두고, **각 오브젝트의 `Identifier` 를 표의 값으로 지정** 한다(또는 부트스트랩 인스펙터 연결).

### 2-1. 환자 — **프리셋 스폰 방식**(씬에 직접 두지 않음)

> 주의(정정): 환자 A/B/C 는 의학적 내용이 **서로 다르다**. `PatientController._medicalState`
> (의식·혈압·맥박·심정지 여부·ECG·health problem 등)는 **프리팹에 직렬화(SerializeField)** 되어 있어
> 식별자만 바꾼다고 환자 A(심정지)와 환자 B(두부손상, 의식 명료)가 구분되지 않는다.
> 따라서 "`patient` 하나만 등록"으로는 부족하며, 아래 둘 중 하나를 택한다.

**방식 A — 환자별 프리셋(권장, 현재 시나리오 반영됨)**
- `EntityPresetRegistryRequirementsSO` 에 환자별 프리셋을 **3개** 등록한다(각각 그 환자의 의학적 상태/외형을
  프리팹에 구성):
  - `patient_a` (심정지/흉부 관통상 프리팹), `patient_b`, `patient_c`, 그리고 분류용 `patient_dummy_d_b`
  - 각 항목: `fallbackEntityType=Undefined`(그대로 둠), `prefab=해당 환자 프리팹`, `isNetworked=true`.
    (환자는 `PatientController` 가 스스로 `EntityType.Patient` 로 등록하므로 `fallbackEntityType` 는 사용되지 않는다.
     자가 등록 컴포넌트가 없는 단순 프리팹에만 `fallbackEntityType` 를 적절히 지정한다.)
- 시나리오 시작부 `EntityPresetSpawn` 노드가 각 프리셋에서 스폰한다(현재 JSON 값):
  - `patient_a_critical` / `SPAWN_A` → `presetIdentifier=patient_a`, `spawnedEntityIdentifier=patient_a`
  - `patient_b_c_ct` / `SPAWN_B`·`SPAWN_C`·`SPAWN_PATIENT_DUMMY_D_B` → `patient_b`·`patient_c`·`patient_dummy_d_b`
- (환자 A/B/C 프리팹이 외형은 같고 의학 상태만 다르면, 같은 베이스 프리팹을 복제해 인스펙터의
  `_medicalState` 만 다르게 채운 3개 프리팹으로 만든다.)

**방식 B — 단일 프리셋 + 런타임 상태 주입**
- 프리셋을 `patient` 하나만 등록하고, 스폰 노드의 `spawnedEntityIdentifier` 로 `patient_a/_b/_c` 식별자만 부여.
- 단, 이 경우 **각 환자의 의학적 상태를 스폰 후 런타임에 주입** 해야 한다(현재 미구현). 즉 환자별
  `_medicalState` 를 설정하는 이벤트/노드가 추가로 필요하다. 현재는 활력징후 **모니터 표시값** 만
  부트스트랩의 `_patientXInitialMonitorParameters` 로 데이터 주입되고, 환자 자신의 `_medicalState`
  (심정지/의식/health problem 등 처치 로직 구동값)는 주입 경로가 없다. → 방식 B 를 쓰려면 별도 구현 필요.

스폰 위치는 기본 원점(0,0,0)이다. 특정 위치에 두려면 스폰 노드의 `positionSourceEntityIdentifier`
를 위치 기준 엔티티(예: 베드/스폰포인트)로 지정하거나 `positionX/Y/Z` 를 편집한다.

> 동작 원리(공통): 스폰 시 `spawnedEntityIdentifier` 가 `PatientController` 에 주입
> (`ISpawnedEntityIdentifierReceiver`)되어 그 식별자로 레지스트리에 등록된다. 따라서 `patient_a/_b/_c` 가
> 구분되어 `click_patient_a` / `select_patient_b` 게이트와 이벤트 핸들러(`*_patient_a` 등)가 동작한다.
> (네트워크 프리셋은 서버에서 FishNet 으로 복제 스폰.)

### 2-1b. 간호사/침대/모니터 (씬 배치 또는 별도 프리셋)
| 종류 | 식별자 |
|---|---|
| 간호사(NPC) | `NurseA`~`NurseD` |
| 침대(분류/처치) | `patientABed`, `patientDummyDABed`, `patientATreatmentBed` 등 |
| 모니터 | `patientA_monitor`, `patientB_monitor`, `patientC_monitor` |

이들은 현재 씬 배치 + 식별자 지정(또는 부트스트랩 인스펙터 연결)로 다룬다. 필요 시 환자와 동일하게
프리셋+스폰 노드로 전환할 수 있다.

### 2-1bis. 엔티티 종류(EntityType)와 등록 소유권

혼동을 줄이기 위한 사실 정리:
- 등록 종류(`EntityType`): **`Undefined`(기본값)**, `Player`, `Npc`, **`Patient`**, `MovingPatientBed`, `Waypoint`,
  `ScenarioInteractable`, `ScenarioTriggerZone`, `ItemObject`. (환자 전용 `Patient` 및 미지정 기본값 `Undefined` 추가됨.)
- **모든 런타임 엔티티는 식별자(identifier)로 단일 저장소(`RegistryType.Entity`)에 등록** 된다.
  `EntityType` 은 분류/필터용 메타데이터일 뿐, 조회는 `Registry.Get(RegistryType.Entity, "<식별자>")` 로
  종류와 무관하게 식별자로 한다.

> **등록 소유권(중요)**: 레지스트리 등록은 **각 엔티티의 컴포넌트가 스스로 수행** 한다.
> - 환자: `PatientController` 가 `OnStartClient` 에서 `EntityType.Patient` 로 자가 등록.
> - 침대: `MovingPatientBedController` 가 `SetIdentifier` 로 `EntityType.MovingPatientBed` 로 등록.
>
> 프리셋 스폰(`EntityPresetSpawn`)은 **인스턴스화 + (네트워크) 스폰 + 식별자 주입 + 위계 해제** 만 담당하고,
> **엔티티 등록과 EntityType 결정은 하지 않는다**(그 책임은 컴포넌트 소유). 즉 환자/침대처럼 자가 등록하는
> 프리팹에서는 **프리셋 요구사항의 `fallbackEntityType` 값이 사용되지 않는다**(컴포넌트가 자기 타입으로 등록).
> `fallbackEntityType`(기본값 `Undefined`)은 *자가 등록 컴포넌트가 없는 단순 프리팹* 의 **폴백 등록** 에만 쓰인다.
> 자가 등록 프리팹/컨테이너에서는 `Undefined` 로 두면 된다.
>
> 따라서 질문에 답하면: 컨테이너(환자+침대) 프리셋의 `entityType` 은 **의미 없음**(루트는 소비되고 등록되지 않음).
> 환자/침대는 이미 각자의 구현체가 스폰·등록을 관리하며, 프리셋이 그 책임을 흡수하지 않는다(의도대로 분리됨).

### 2-1c. 묶음 프리셋 + 위계 해제(ungroup) — 환자+침대를 함께 배치할 때

> **결합 처리 전략(정본)**: 환자+침대 "결합"은 **루트 컨테이너 프리셋 1개를 등록 → 런타임에 그 루트가
> 해제(detach)되며 환자/침대가 각각 독립 엔티티로 등록되는** 방식이 정본이다. 환자·침대를 별도 프리셋으로
> 따로 등록해 런타임에 합치는 방식이 **아니다**. 정리하면:
> - **등록(에디터)**: 컨테이너 프리셋 **1개**(비-NetworkObject 루트 + 환자/침대 자식 NetworkObject) + 분리 설정.
> - **런타임**: 스폰 시 컨테이너 루트는 소비되고(자식 분리 후 빈 컨테이너는 제거), 환자/침대가 각각
>   독립 루트 NetworkObject 로 분리·복제·등록된다(`patient_a`, `bed_a`). 위계는 사라진다.
> - **결합 상태**: 위계가 없으므로 논리 결합은 `attach_patient_bed_pairs`(아래 (4))로 재설정한다.
> - 즉 "루트가 런타임에 해제될 것을 기대"가 맞고, "따로 등록"은 아니다.

환자와 침대를 "한 묶음"으로 결합 배치하고 싶을 때(예: 환자가 침대에 결합된 상태로 시작):

**(1) 컨테이너 프리팹 구성 (에디터, 비런타임)**
- **비-NetworkObject 루트** GameObject 를 만들고, 그 아래에 환자(`PatientController`)와 침대
  (`MovingPatientBedController`)를 **각각 자식 NetworkObject** 로 둔다. 하나의 프리팹으로 저장.
- 결합 상태(환자가 침대 위)는 프리팹에서 직접 사전 설정 가능하다(직렬화 필드):
  침대의 `_reposedTargetComponent` ← 환자, 환자의 `_currentBed`/`_isMovingPatientBedAttached` ← 침대.
  (스폰 후 분리되면 위계는 사라지지만, 런타임 논리 결합은 아래 (4) 로 재설정.)

**(2) 분리(ungroup) 설정 — 프리셋 SO 에 내장(권장) 또는 시나리오 노드**
- **권장: `EntityPresetRegistryRequirementsSO` 의 해당 항목 `childDetachments`** 에 분리할 자식을 등록한다
  (`childPath`=예 `Bed`, `spawnedEntityIdentifier`=예 `bed_a`). 프리셋 자체가 분리 설정을 가지므로 자기서술적.
- 또는 시나리오 `EntityPresetSpawn` 노드의 `childDetachments` 로 지정(노드 지정이 있으면 노드 우선, 없으면 프리셋 설정 사용).
- 규칙: **분리는 NetworkObject 자식에 대해서만** 동작(비-NetworkObject 무시·잔류). 미지정 자식은 자동 분리 안 함.

**(3) 비런타임(에디터) 검증 — 지원됨**
- SO 인스펙터에서 값 변경 시 자동(`OnValidate`) + 우클릭 메뉴 **"Validate Presets (Editor)"** 로 수동 검증.
- 검사: 각 `childPath` 가 프리팹에서 해석되는가 / 분리 대상이 NetworkObject 인가 / 컨테이너 루트가
  비-NetworkObject 인가(권장) / identifier 중복·prefab 누락. 문제 시 Console 경고. **플레이 없이 확인 가능.**

**(4) 런타임 결합 재설정 — 지원됨(`attach_patient_bed_pairs`)**
- 스폰 시 지정 자식이 루트로 분리되어 각각 독립 엔티티로 스폰·등록된다(네트워크 프리셋은 서버에서 FishNet 복제).
  분리된 침대는 `MovingPatientBedController.ApplySpawnedEntityIdentifier`(=`SetIdentifier`)로 식별자 등록.
- 분리 후 둘은 위계 없는 독립 객체이므로, "결합 상태"로 시작하려면 스폰 직후 시나리오에서
  InvokeEvent **`attach_patient_bed_pairs`** 노드를 호출한다. 이 핸들러는 부트스트랩 인스펙터의
  `_patientBedPairs`(환자 식별자 ↔ 침대 식별자 목록)를 읽어, 레지스트리에서 객체를 찾아
  `bed.TryReposeTarget(patient)` 로 논리 결합을 재설정한다.
  - 운영자 작업: 부트스트랩 인스펙터 `attach_patient_bed_pairs > Patient Bed Pairs` 에
    `{ patientIdentifier: patient_a, bedIdentifier: bed_a }` 등을 등록.
  - 시나리오: 그룹 스폰 노드 다음에 `{ "nodeType": "InvokeEvent", "eventIdentifier": "attach_patient_bed_pairs", "moveNextBehavior": "WaitUntilDone", "nextIdentifier": "..." }` 를 둔다.

### 2-2. 수액/산소/모니터 연결 지점(`IntravenousLineConnectionPoint`)
연결 완료 시 끝점 `Identifier` 로 신호가 올라가므로, **연결 지점의 `Identifier` 를 아래 조건명으로 지정** 한다.
(연결 = 한쪽 시작점 + 다른 쪽 끝점. 보통 게이트는 끝점 Identifier 로 맞추면 된다.)

`connect_cannula_and_ns1`, `connect_cannula_and_ns1_patient_b`, `connect_cannula_and_ns1_patient_c`,
`connect_ambubag`, `connect_o2_to_ambu`, `connect_blood_to_lv1`, `connect_ps1_to_lv1`,
`connect_patient_and_monitor_b`, `connect_patient_and_monitor_patient_c`,
`connect_wall_component_1`, `connect_wall_component_2`, `connect_wall_component_and_yankauer`,
`connect_nasal_and_o2`, `connect_tpiece_and_oxyflow`.

- 참조: [`interaction-signal-integration-spec.md`](./interaction-signal-integration-spec.md) "connect_*" 절.

### 2-3. 아이템 (자동 — 식별자 변경 불요)
의료 아이템은 획득 시 자동으로 `sig.click_<식별자>` 를 올리고, 시나리오 조건은 이미 아이템 식별자에
맞춰 정합되어 있다. **운영자는 아이템 식별자를 바꾸지 않는다.** 월드에 픽업 가능한 아이템 인스턴스만 배치.

## 단계 3. 리소스(아이콘/모델) 배치

- [ ] 신규 아이템 `plasma_solution_1000ml` 의 아이콘/모델 리소스 배치
      (`Resources/{ItemTexturesPath}/plasma_solution_1000ml`, `Resources/Models/Items/plasma_solution_1000ml`).
      미배치 시 Console 경고만 발생, 로직은 동작. 참조: [`../../api-references/items/plasma_solution_1000ml.md`](../../../api-references/items/plasma_solution_1000ml.md).
- [ ] 플레이모드 진입 후 `ValidateItemResources` 요약에서 누락 경고가 허용 범위인지 확인.

## 단계 4. 실행 & 진행 순서

서버(호스트) 채팅/콘솔에서:

1. `/scenario execute @s disaster_intro` — 인트로부터 시작.
   - "역할을 선택하세요" 선택지에서 각 플레이어가 A/B/C/D 중 하나를 선택 → 역할 태그 자동 부여.
2. `/scenario execute @s patient_a_critical`
3. 환자 B/C가 필요할 때 관리자가 `/scenario execute @s patient_b_c_ct`를 별도로 실행

> 환자 시나리오만 단독 테스트 시: `/tag add @self <역할태그>` 로 수동 부여
> (역할 태그 집합은 [`patient-a-b-c-conversion-notes.md`](./patient-a-b-c-conversion-notes.md) "초기 역할 태그 부여" 표).

## 단계 5. 검증

- [ ] 인트로 → 환자 A가 정상 종료되고, 관리자가 별도 실행한 환자 B/C도 정상 진행된다.
- [ ] Console 의 "Registry snapshot" 에서 환자/모니터/간호사 식별자가 등록되어 보인다.
- [ ] `No handler registered for event '...'` 경고가 `todo.*` 외에는 없다.
- [ ] (게이팅 활성화 시) 해당 인터랙션을 수행해야 다음 단계로 진행된다
      (예: IV 연결 전에는 다음으로 넘어가지 않음 — 단, 게이트는 `onFailure:Ignore` 라 미설정 식별자는 자동 통과).

---

## 운영자가 다루지 않는 것(개발 작업 = 별도)

아래는 운영자 작업이 아니라 개발 백로그다(이 문서 범위 밖, 참고용).

- 미구현 인터랙션 게임플레이(거즈 적용/캐뉼라 삽입/약물 주입/조립/NPC 전달 = `apply/insert/push/pass`)와
  신체부위 사정 UI(`click_*_face`, `click_chest`, `check_*`)의 게임플레이 구현 및 신호 계측.
- 서브그래프 재사용(엔진 SPEC-4, 보류).
- 상세 상태: [`interaction-signal-integration-spec.md`](./interaction-signal-integration-spec.md) "[없음]/[부분] 잔여" 항목.

## 요약 — 운영자 최소 작업

1. 부트스트랩 + UI 가 씬에 있는지 확인.
2. 환자/더미/간호사/침대/모니터 오브젝트 배치 + 식별자 지정(특히 환자 = `patient_a/_b/_c`).
3. IV/산소/모니터 연결 지점 `Identifier` 를 connect_* 조건명으로 지정.
4. `plasma_solution_1000ml` 리소스 배치(선택).
5. `/scenario execute @s disaster_intro` → 역할 선택 → 환자 A 실행·검증. 환자 B/C는 관리자가 별도 실행·검증.
