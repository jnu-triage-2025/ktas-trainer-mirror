---
title: "환자 + 환자침대 결합 프리셋 구성 가이드 (운영자용)"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-24
flags: []
---

# 환자 + 환자침대 결합 프리셋 구성 가이드 (운영자용)

환자(`PatientController`)와 환자침대(`MovingPatientBedController`)를 **사전 설정 상태로 결합 스폰**하고,
런타임에 두 개의 독립 엔티티로 배치하는 절차를 단계별로 설명한다.

핵심 개념 한 줄: **환자/침대를 각각 EntityPreset 으로 등록 → 환자 프리셋이 침대 프리셋을 `unwrapOnSpawn` 으로 함께 참조 →
스폰 시 둘이 각각 독립 루트로 생성·복제·등록 → 논리 결합(환자가 침대 위)을 이벤트로 재설정.**

> 변경 안내(2026-06-24): 더 이상 "컨테이너 프리팹 + childPath" 방식을 쓰지 않는다. 하위는 **다른 EntityPreset 의 식별자**로
> 참조한다. 컨테이너 프리팹/ nested NetworkObject / FishNet Reserialize 의존이 사라졌다.

관련: [`human-operator-process.md`](./human-operator-process.md) §2-1c.

---

## 왜 이렇게 하나 (배경)

- 런타임에 환자와 침대는 **Transform 위계(부모-자식)가 없다.** 침대는 매 프레임 환자를 앵커 위치로 스냅하고,
  논리적으로 `_currentBed`/`_reposedTargetComponent` 참조로만 연결한다.
- 환자와 침대는 **각각 독립 NetworkObject** 다. FishNet 에서 중첩 NetworkObject 는 단일 단위로 취급되어 부적합하다.
- 따라서 결합을 한 프리팹에 nested 로 저장하지 않고, **각각 독립 프리셋**으로 두고 스폰 시 함께 생성한다(unwrap).

---

## 단계별 절차

### 1단계 — 환자/침대 프리팹 준비 (에디터)

기존 프리팹을 그대로 쓴다. **별도의 컨테이너 프리팹을 만들지 않는다.**

- 환자 오브젝트 프리팹: `Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA.prefab` (`PatientController`, NetworkObject)
- 침대 오브젝트 프리팹: `Assets/Modules/TriageTrainer/Prefabs/Entities/PatientMovingBed.prefab` (`MovingPatientBedController`, NetworkObject)

> 환자 자체의 의학 상태(`_medicalState`)는 환자 프리팹에 직접 구성한다. 환자 A/B/C 가 다르면 각각 별도 환자 프리팹을 만든다.

### 2단계 — 프리셋 등록 (SO, 에디터)

`EntityPresetRegistryRequirementsSO` 에 프리셋 **2개**를 등록한다.

| identifier | prefab | isNetworked | fallbackEntityType | childReferences |
|---|---|---|---|---|
| `bed_a` | 침대 프리팹 | true | Undefined(자가 등록) | (없음) |
| `patient_a` | 환자 프리팹 | true | Undefined(자가 등록) | `bed_a` 1개 (아래) |

`patient_a` 의 `childReferences` 1개:
- `childPresetIdentifier`: `bed_a` (함께 스폰할 하위 프리셋 식별자)
- `spawnedEntityIdentifier`: `bed_a` (스폰된 침대 인스턴스가 가질 식별자)
- `unwrapOnSpawn`: **true** (침대를 환자 자식이 아니라 동일 계층의 독립 루트로 둔다)

> `childReferences` 는 원본 프리팹의 자식 Transform 을 가리키지 않는다. **이미 등록된 다른 EntityPreset 의 식별자**를 가리킨다.
> 환자는 `PatientController` 가, 침대는 `MovingPatientBedController` 가 각자 식별자를 받아 **스스로** 레지스트리에 등록한다
> (각자의 EntityType: `Patient` / `MovingPatientBed`). `fallbackEntityType` 은 자가 등록 컴포넌트가 없는 단순 프리팹에만 쓰인다.

### 3단계 — 에디터 검증 (플레이 없이)

- SO 인스펙터에서 값이 바뀌면 자동 검증되고, 우클릭 메뉴 **"Validate Presets (Editor)"** 로 수동 검증한다.
- 검사 항목: identifier 중복·prefab 누락 / `childReferences` 의 `childPresetIdentifier` 비어있음·자기 참조(순환) /
  하위 프리셋 식별자가 SO 안에 정의되어 있는지(없으면 오타 경고).

### 4단계 — 시나리오에서 스폰 + 결합 재설정

시나리오 JSON 에서 다음 순서로 노드를 둔다.

1. **스폰**: `EntityPresetSpawn` 노드로 환자 프리셋을 스폰한다(침대는 자동으로 함께 unwrap 스폰됨).
   ```json
   { "nodeType": "EntityPresetSpawn", "identifier": "SPAWN_A",
     "presetIdentifier": "patient_a", "spawnedEntityIdentifier": "patient_a",
     "nextIdentifier": "RELINK_A" }
   ```
   - 하위 구성(침대 unwrap)은 프리셋 정의에 있으므로 노드에는 적지 않는다(노드는 하위 구성을 다루지 않음).
   - 스폰 결과: `patient_a`(EntityType.Patient), `bed_a`(EntityType.MovingPatientBed) 가 각각 독립 루트로 등록.

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
| 인스턴스화 + 네트워크 스폰 + 식별자 주입 + 하위 프리셋 재귀 스폰(unwrap 포함) | 프리셋 스폰(`Registry.TrySpawnEntityPreset`) |
| 엔티티 등록 + EntityType 결정 | **각 컴포넌트**(`PatientController`=Patient, `MovingPatientBedController`=MovingPatientBed) |
| 논리 결합(환자↔침대) | `attach_patient_bed_pairs` 핸들러(`bed.TryReposeTarget`) |

즉 프리셋은 "스폰 메커니즘"만 담당하고, 엔티티의 정체성/등록은 원래 소유자(컴포넌트)에 있다.

## 체크리스트

- [ ] 환자/침대 프리팹은 각각 NetworkObject 를 가진 독립 프리팹이다(컨테이너 프리팹 불필요).
- [ ] SO 에 `bed_a`, `patient_a` 2개 프리셋이 등록됨.
- [ ] `patient_a.childReferences` 에 `bed_a` 가 `unwrapOnSpawn=true` 로 등록됨.
- [ ] 두 프리셋 모두 `isNetworked=true`, `fallbackEntityType=Undefined`.
- [ ] "Validate Presets (Editor)" 경고 없음.
- [ ] 시나리오: `EntityPresetSpawn`(`patient_a`) → `attach_patient_bed_pairs` 순서.
- [ ] 부트스트랩 `_patientBedPairs` 에 (patient_a, bed_a) 등록.
