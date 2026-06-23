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
환자/더미는 **하나의 환자 프리셋을 등록해두고 시나리오가 스폰** 한다(수동 배치 불요).
- `EntityPresetRegistryRequirementsSO` 에 환자 프리셋을 식별자 **`patient`** 로 등록한다
  (`entityType=Npc`, `prefab=환자 프리팹`, `isNetworked=true`). 부트스트랩 `Awake_EntityPreset` 가 자동 등록.
- 시나리오 시작부의 `EntityPresetSpawn` 노드가 이 프리셋에서 인스턴스를 스폰하며,
  **인스턴스별 식별자를 자동 부여** 한다:
  - `patient_a_critical` 시작 노드 `SPAWN_A` → `patient_a`
  - `patient_b_c_ct` 시작 노드 `SPAWN_B`/`SPAWN_C`/`SPAWN_DUMMY_B` → `patient_b` / `patient_c` / `dummy_b`
- 스폰 위치는 기본 원점(0,0,0)이다. 특정 위치에 두려면 스폰 노드의 `positionSourceEntityIdentifier`
  를 위치 기준 엔티티(예: 베드/스폰포인트)로 지정하거나 `positionX/Y/Z` 를 편집한다.

> 동작 원리: 스폰 시 `ScenarioEntityPresetSpawnNode.spawnedEntityIdentifier` 값이
> `PatientController` 에 주입(`ISpawnedEntityIdentifierReceiver`)되어 그 식별자로 레지스트리에 등록된다.
> 따라서 한 프리셋에서 `patient_a/_b/_c` 가 구분되며, `click_patient_a` / `select_patient_b` 게이트와
> 이벤트 핸들러(`*_patient_a` 등)가 그대로 동작한다. (네트워크 프리셋은 서버에서 FishNet 으로 복제 스폰.)

### 2-1b. 간호사/침대/모니터 (씬 배치 또는 별도 프리셋)
| 종류 | 식별자 |
|---|---|
| 간호사(NPC) | `NurseA`~`NurseD` |
| 침대(분류/처치) | `patientABed`, `dummyABed`, `patientATreatmentBed` 등 |
| 모니터 | `patientA_monitor`, `patientB_monitor`, `patientC_monitor` |

이들은 현재 씬 배치 + 식별자 지정(또는 부트스트랩 인스펙터 연결)로 다룬다. 필요 시 환자와 동일하게
프리셋+스폰 노드로 전환할 수 있다.

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
3. `/scenario execute @s patient_b_c_ct`

> 환자 시나리오만 단독 테스트 시: `/tag add @self <역할태그>` 로 수동 부여
> (역할 태그 집합은 [`patient-a-b-c-conversion-notes.md`](./patient-a-b-c-conversion-notes.md) "초기 역할 태그 부여" 표).

## 단계 5. 검증

- [ ] 인트로 → 환자 A → 환자 B/C 전 구간이 끊김 없이 진행된다.
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
5. `/scenario execute @s disaster_intro` → 역할 선택 → 환자 A → 환자 B/C 순서로 실행·검증.
