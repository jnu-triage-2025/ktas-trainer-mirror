---
title: "환자 + 환자침대 결합 프리셋 구성 가이드 (운영자용)"
doc_type: requirement
domain: content-definitions
progress: "1-designed"
status: active
updated: 2026-06-23
flags: []
---

# 환자 + 환자침대 결합 프리셋 구성 가이드 (운영자용)

환자(`PatientController`)와 환자침대(`MovingPatientBedController`)를 **하나로 결합해 배치**하고,
런타임에 두 개의 독립 엔티티로 풀어(ungroup) 스폰하는 절차를 단계별로 설명한다.

핵심 개념 한 줄: **컨테이너 프리팹 1개를 등록 → 스폰 시 환자/침대가 각각 독립 객체로 분리·복제·등록 →
논리 결합(환자가 침대 위)을 이벤트로 재설정.** (환자·침대를 따로 등록하지 않는다. 컨테이너 루트는 런타임에 소비된다.)

관련: [`human-operator-process.md`](./human-operator-process.md) §2-1c,
엔진 동작 근거 [`../../api-references/...`](../../../api-references/).

---

## 왜 이렇게 하나 (배경)

- 런타임에 환자와 침대는 **Transform 위계(부모-자식)가 없다.** 침대는 매 프레임 환자를 앵커 위치로 스냅하고,
  논리적으로 `_currentBed`/`_reposedTargetComponent` 참조로만 연결한다.
- 환자와 침대는 **각각 독립 NetworkObject** 다. FishNet 에서 중첩 NetworkObject 는 단일 단위로 취급되어
  독립 개체로는 부적합하다.
- 따라서 "결합"을 프리팹 위계로 저장하더라도, 런타임에는 **위계를 풀어 두 독립 루트** 로 만들어야 한다.

---

## 단계별 절차

### 1단계 — 컨테이너 프리팹 만들기 (에디터)

1. 빈 GameObject 를 만든다(예: `PatientABedGroup`). **이 루트에는 NetworkObject 를 붙이지 않는다**(순수 컨테이너).
2. 이 루트의 자식으로 다음을 둔다(각각 NetworkObject):
   - 환자 오브젝트(`PatientController`) — 이름 예 `Patient`
   - 침대 오브젝트(`MovingPatientBedController`) — 이름 예 `Bed`
3. (선택) **결합 상태를 프리팹에 사전 설정**: 환자를 침대 위에 올라간 상태로 두려면 직렬화 필드를 연결한다.
   - 침대의 `_reposedTargetComponent` ← 환자 컴포넌트
   - 환자의 `_currentBed` ← 침대, `_isMovingPatientBedAttached` = true
   - (이는 시각/초기 상태용. 런타임 논리 결합은 4단계에서 확정한다.)
4. 이 루트를 **프리팹으로 저장**한다(예: `Assets/Modules/TriageTrainer/Prefabs/EntityPresets/patient_a_bed_group/`).

> 환자 자체의 의학 상태(`_medicalState`: 의식/혈압/심정지 등)는 환자 오브젝트(프리팹)에 직접 구성한다.
> 환자 A/B/C 가 다르면 각각 별도 컨테이너 프리팹을 만든다.

### 2단계 — 프리셋 등록 + 분리 설정 (SO, 에디터)

`EntityPresetRegistryRequirementsSO` 에 컨테이너 프리셋 **1개** 를 추가한다.

| 필드 | 값 |
|---|---|
| `identifier` | `patient_a_bed_group` (컨테이너 식별자) |
| `prefab` | 1단계에서 만든 컨테이너 프리팹 |
| `isNetworked` | `true` |
| `fallbackEntityType` | `Undefined` (컨테이너 루트는 등록되지 않으므로 미사용) |
| `childDetachments` | 아래 2개 |

`childDetachments` (분리할 자식 = NetworkObject 만):
- `{ childPath: "Patient", spawnedEntityIdentifier: "patient_a" }`
- `{ childPath: "Bed",     spawnedEntityIdentifier: "bed_a" }`

> `childPath` 는 컨테이너 루트 기준 자식 이름/경로. `spawnedEntityIdentifier` 는 분리 후 그 객체가 가질 식별자.
> 환자는 `PatientController` 가, 침대는 `MovingPatientBedController` 가 이 식별자를 받아 **스스로** 레지스트리에 등록한다
> (각자의 EntityType: `Patient` / `MovingPatientBed`).

### 3단계 — 에디터 검증 (플레이 없이)

- SO 인스펙터에서 값이 바뀌면 자동 검증되고, 우클릭 메뉴 **"Validate Presets (Editor)"** 로 수동 검증한다.
- 검사 항목: 각 `childPath` 가 프리팹에서 찾아지는가 / 분리 대상이 NetworkObject 인가 /
  컨테이너 루트가 비-NetworkObject 인가(권장) / identifier 중복·prefab 누락.
- Console 에 경고가 없으면 구조가 올바른 것이다.

### 4단계 — 시나리오에서 스폰 + 결합 재설정

시나리오 JSON 에서 다음 순서로 노드를 둔다.

1. **스폰**: `EntityPresetSpawn` 노드로 컨테이너를 스폰한다.
   ```json
   { "nodeType": "EntityPresetSpawn", "identifier": "SPAWN_A_GROUP",
     "presetIdentifier": "patient_a_bed_group", "nextIdentifier": "RELINK_A" }
   ```
   - 분리 설정을 SO 에 넣었으므로 노드에는 `childDetachments` 를 생략해도 된다(프리셋 설정 사용).
   - 스폰 결과: `patient_a`(EntityType.Patient), `bed_a`(EntityType.MovingPatientBed) 가 각각 독립 등록.
     컨테이너 루트는 소비되어 사라진다.

2. **결합 재설정**: `attach_patient_bed_pairs` 이벤트를 호출한다.
   ```json
   { "nodeType": "InvokeEvent", "identifier": "RELINK_A",
     "eventIdentifier": "attach_patient_bed_pairs",
     "moveNextBehavior": "WaitUntilDone", "nextIdentifier": "..." }
   ```
   - 부트스트랩 인스펙터 `attach_patient_bed_pairs > Patient Bed Pairs` 에
     `{ patientIdentifier: patient_a, bedIdentifier: bed_a }` 를 등록해 둔다.
   - 핸들러가 레지스트리에서 두 객체를 찾아 `bed.TryReposeTarget(patient)` 로 논리 결합(환자가 침대 위)을 재설정한다.

---

## 책임 분리 요약 (혼동 방지)

| 동작 | 담당 |
|---|---|
| 인스턴스화 + 네트워크 스폰 + 식별자 주입 + 위계 해제(분리) | 프리셋 스폰(`Registry.TrySpawnEntityPreset`) |
| 엔티티 등록 + EntityType 결정 | **각 컴포넌트**(`PatientController`=Patient, `MovingPatientBedController`=MovingPatientBed) |
| 논리 결합(환자↔침대) | `attach_patient_bed_pairs` 핸들러(`bed.TryReposeTarget`) |
| 컨테이너 루트 | 분리 후 소비/제거(등록되지 않음) |

즉 프리셋은 "스폰 메커니즘"만 담당하고, 엔티티의 정체성/등록은 원래 소유자(컴포넌트)에 있다.

## 체크리스트

- [ ] 컨테이너 루트는 NetworkObject 가 **아니다**(순수 컨테이너).
- [ ] 환자/침대 자식은 각각 NetworkObject 다.
- [ ] SO `childDetachments` 에 `Patient`/`Bed` 가 각 식별자(`patient_a`/`bed_a`)로 등록됨.
- [ ] `fallbackEntityType` = `Undefined`.
- [ ] "Validate Presets (Editor)" 경고 없음.
- [ ] 시나리오: `EntityPresetSpawn` → `attach_patient_bed_pairs` 순서.
- [ ] 부트스트랩 `_patientBedPairs` 에 (patient_a, bed_a) 등록.
