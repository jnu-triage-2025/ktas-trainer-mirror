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

2. **결합 설정 — 권장: 프리셋이 자동 결합**

   결합(환자가 침대 위)은 **침대의 SyncVar 권위값(`_reposedTargetIdentifier`)** 으로 네트워크 복제된다.
   서버가 그 값을 설정하면 모든 피어가 식별자로 환자를 찾아 자동 결합한다(누운 애니메이션/콜라이더/위치 스냅까지).

   **(권장) 프리셋 하위 참조에 `linkChildToParent` 지정** — 별도 노드/프리팹 수작업 불필요:
   - SO 의 `patient_a.childReferences > bed_a` 항목에 `linkChildToParent = true` 를 켠다(이미 SO 에 설정됨).
   - 스폰 시 엔진이 침대(하위)에게 부모(환자) 런타임 식별자를 전달 → 침대가 그 환자를 결합 권위값으로 설정 →
     스폰 즉시 결합되고 전 피어로 복제된다. 환자 등록이 늦어도 침대가 매 프레임 재시도하여 결국 결합된다.
   - 침대 프리팹은 공유 자산이어도 되며, 환자별로 식별자를 손으로 적어둘 필요가 없다(부모 식별자가 런타임에 주입됨).

   **(대체) 시나리오에서 결합** — 런타임에 동적으로 묶을 때:
   ```json
   { "nodeType": "InvokeEvent", "identifier": "RELINK_A",
     "eventIdentifier": "attach_patient_bed_pairs",
     "moveNextBehavior": "WaitUntilDone", "nextIdentifier": "..." }
   ```
   - 부트스트랩 `attach_patient_bed_pairs > Patient Bed Pairs` 에 `{ patientIdentifier: patient_a, bedIdentifier: bed_a }` 등록.
   - 핸들러가 `bed.TryReposeTarget(patient)` 를 호출 → 내부적으로 서버 권위값(SyncVar)을 설정한다(과거처럼 로컬만
     바꾸지 않는다). 따라서 결합이 모든 피어에 정상 복제·지속된다.

> 중요(네트워크 식별자): 환자/침대의 런타임 식별자는 이제 **SyncVar** 로 전 피어에 복제된다. 과거에는 식별자가 서버에서만
> 주입되어 원격 클라에서는 기본값(`patient`)으로 남았고, 그 때문에 식별자 기반 결합이 원격에서 해석되지 않아 "스폰은 되나
> 결합되지 않는" 문제가 있었다. 이제 모든 피어가 같은 식별자로 등록·조회하므로 결합이 정상 복제된다.

---

## 책임 분리 요약 (혼동 방지)

| 동작 | 담당 |
|---|---|
| 인스턴스화 + 네트워크 스폰 + 식별자 주입 + 하위 프리셋 재귀 스폰(unwrap) + 부모 식별자 전달(`linkChildToParent`) | 프리셋 스폰(`Registry.TrySpawnEntityPreset`) |
| 엔티티 등록 + EntityType 결정 + **런타임 식별자 복제(SyncVar)** | **각 컴포넌트**(`PatientController`/`MovingPatientBedController`) |
| 논리 결합(환자↔침대) **권위/복제** | 침대의 SyncVar `_reposedTargetIdentifier`(서버 설정 → 전 피어 OnChange 적용) |
| 결합 트리거 | 프리셋 `linkChildToParent`(권장) 또는 `attach_patient_bed_pairs`/`bed.TryReposeTarget` |

즉 프리셋은 "스폰 + 결합 트리거"를 담당하고, 결합 상태는 침대의 식별자 SyncVar 가 네트워크 권위적으로 보유한다.

## 체크리스트

- [ ] 환자/침대 프리팹은 각각 NetworkObject 를 가진 독립 프리팹이다(컨테이너 프리팹 불필요).
- [ ] SO 에 `bed_a`, `patient_a` 2개 프리셋이 등록됨.
- [ ] `patient_a.childReferences` 에 `bed_a` 가 `unwrapOnSpawn=true`, `linkChildToParent=true` 로 등록됨.
- [ ] 두 프리셋 모두 `isNetworked=true`, `fallbackEntityType=Undefined`.
- [ ] "Validate Presets (Editor)" 경고 없음.
- [ ] 시나리오: `EntityPresetSpawn`(`patient_a`) 만으로 결합 스폰(권장). 동적 결합이 필요하면 뒤에 `attach_patient_bed_pairs` 추가.
