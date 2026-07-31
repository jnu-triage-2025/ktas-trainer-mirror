---
title: "환자 B/C(PatientTypeB Male/Female) EntityPreset 구성 가이드 (운영자용)"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-24
flags: []
---

# 환자 B/C(PatientTypeB Male/Female) EntityPreset 구성 가이드 (운영자용)

`patient_b_c_ct` 시나리오의 `patient_b` / `patient_c` 를 **환자 A와 동일하게 침대에 누운 결합 상태로 스폰**하기 위한 절차.

- 매핑(확정): `patient_b` → **PatientTypeBMale**, `patient_c` → **PatientTypeBFemale**.
- `patient_dummy_d_b` 는 별도 콘텐츠지만 `patient_b_c_ct`의 `SPAWN_PATIENT_DUMMY_D_B`가 직접 요구하므로
  닫힌 플레이 승인 범위에 포함한다(§5 참고 및 [닫힌 플레이 감사](patient-b-c-closed-scenario-audit.md)).
- 결합 방식은 환자 A와 동일: 환자 프리셋이 침대 프리셋을 `unwrapOnSpawn + linkChildToParent` 로 함께 스폰.
  자세한 원리는 [`patient-bed-combined-preset-guide.md`](./patient-bed-combined-preset-guide.md).

> **현재 상태(2026-07-29 감사)**: SO의 `patient_b`/`bed_b`/`patient_c`/`bed_c`와
> 두 B/C 프리팹의 NetworkObject, PatientController, CapsuleCollider, FishNet spawnable
> 등록은 확인되었다. 아래 절차는 신규 변경 후 회귀 검증용으로 유지한다.

---

## 1단계 — 환자 B/C 프리팹 회귀 검증 (에디터, 필수)

`PatientTypeA.prefab` 를 기준 템플릿으로 삼아, Male/Female 프리팹에 동일한 핵심 컴포넌트를 추가한다.

대상 프리팹:
- `Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab`
- `Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab`

각 프리팹의 **루트 GameObject** 에 다음 구성이 존재하는지 확인한다(=`PatientTypeA` 와 동일 구조).

1. **`NetworkObject`** (FishNet) 컴포넌트와 `Is Spawnable` 확인.
2. **`PatientController`** (`TriageTrainer.Entity.PatientController`) 컴포넌트와 참조 확인.
   - `PatientTypeA` 의 PatientController 인스펙터 값을 참고해 동일 항목을 채운다.
   - Identity `_identifier`: 기본값은 무관(스폰 시 프리셋이 `patient_b`/`patient_c` 로 주입·복제함). 헷갈리지 않게 `patient_b`/`patient_c` 로 적어둬도 된다.
   - Display(lift/carry/monitor_select 텍스트·아이콘), Patient `_weight`, Collider(standing/lying 캡슐), Animation `_runtimeAnimatorController`(`PatientCharacterModel.controller`), Visual 매핑 등.
3. **상태 컴포넌트**: 루트에 이미 있는 `PatientTypeBMaleState` / `PatientTypeBFemaleState`(둘 다 `PatientStateABC` 파생)를 유지한다.
   - 누운 자세/콜라이더 오프셋(`PositionOnLayingOnPatientMovingBed`, `Collider...OnLayingOnPatientMovingBed`)이 올바른지 확인한다(환자가 침대 위에 자연스럽게 눕도록). `PatientController` 가 이 값을 읽어 결합 시 자세/콜라이더를 적용한다.
4. **모델/애니메이터**: 자식 모델에 `HumanoidAnimationController` + `Animator` 가 있고 `lying` Bool 파라미터와 `laying-idle` 상태가 있는지 확인(환자 A와 동일 컨트롤러를 쓰면 충족).
5. **콜라이더**: 루트에 `CapsuleCollider`(PatientController 의 `[RequireComponent]`). standing/lying 값은 A 참고.

> 팁: 가장 안전한 방법은 `PatientTypeA.prefab` 를 열어 루트의 `NetworkObject`/`PatientController`/`CapsuleCollider`
> 컴포넌트를 "Copy Component" → B 프리팹 루트에 "Paste Component As New" 한 뒤, 모델/상태 참조만 B 에 맞게 교체하는 것이다.

## 2단계 — FishNet Spawnable Prefabs 회귀 검증 (필수)

두 프리팹의 GUID가 FishNet 프리팹 컬렉션에 등록되어 있어야 한다.

1. 플레이모드 종료.
2. Unity 메뉴 **Fish-Networking > Utility > Reserialize NetworkObjects**(또는 **Refresh Default Prefabs**) 실행.
3. `Assets/DefaultPrefabObjects.asset` 에 두 B 프리팹 guid 가 포함되었는지 확인
   (`PatientTypeBMale` = `0e03ef43...`, `PatientTypeBFemale` = `860f27b5...`).
   - 포함되지 않으면: 프리팹이 **Variant** 가 아닌 일반 프리팹인지 확인(Variant 는 컬렉션에 들어가지 않음).

> 미등록 상태로 스폰하면 `NetworkObject ... ObjectId [65535] ... is expected to be initialized but was not` 오류가 난다.
> 상세: [`entity-preset-debug-guide.md`](./entity-preset-debug-guide.md) §3-2 (B).

## 3단계 — 프리셋 등록 확인 (SO)

`New EntityPreset Registry Requirements SO.asset` 에 다음 6개 프리셋이 있어야 한다(이미 추가됨).

| identifier | prefab | isNetworked | childReferences |
|---|---|---|---|
| `patient_a` | PatientTypeA | true | `bed_a` (unwrap+link) |
| `bed_a` | PatientMovingBed | true | — |
| `patient_b` | PatientTypeBMale | true | `bed_b` (unwrap+link) |
| `bed_b` | PatientMovingBed | true | — |
| `patient_c` | PatientTypeBFemale | true | `bed_c` (unwrap+link) |
| `bed_c` | PatientMovingBed | true | — |

> `bed_b`/`bed_c` 는 `bed_a` 와 동일하게 원본 `PatientMovingBed.prefab`(컬렉션 등록된 일반 프리팹)을 사용한다.
> 침대 식별자만 다르게(`bed_b`/`bed_c`) 부여해 환자별로 구분 등록한다.

## 4단계 — 검증 + 테스트

1. SO 인스펙터 우클릭 **"Validate Presets (Editor)"** 실행 → 경고 없어야 한다.
   - B 프리팹 완성 전이라면 "프리팹이 Spawnable Prefabs 에 등록되어 있지 않습니다(PrefabId 미할당)" 경고가 뜬다 → §1~§2 를 먼저 끝낸다.
2. IndevScene `EntityPresetDebugger` 에서 `patient_b` / `patient_c` 를 각각 스폰.
3. **Report Registered Entities** 로 `patient_b`(Patient)/`bed_b`(MovingPatientBed), `patient_c`/`bed_c` 가 등록되는지,
   환자가 침대에 누운 상태(누운 애니메이션/자세)로 나오는지 확인.
4. 호스트뿐 아니라 **원격 클라이언트**에서도 동일하게 결합 상태로 보이는지 확인(식별자 SyncVar 복제 동작 검증).

## 5단계 — patient_dummy_d_b (시나리오 필수)

`SPAWN_PATIENT_DUMMY_D_B` 노드는 `patient_dummy_d_b` 프리셋을 스폰한다. patient_dummy_d_b 는 Male/Female 환자와 다른 별도 오브젝트이므로,
용도에 맞는 프리팹을 정해 별도 EntityPreset(`patient_dummy_d_b`)으로 등록해야 한다. 현재 저장소
감사에서는 해당 preset과 전용 프리팹을 확인하지 못했으므로 닫힌 플레이의 미완료 항목이다.
침대 결합이 필요하면 `patient_b`/`patient_c` 와 동일한 패턴(`unwrapOnSpawn + linkChildToParent`)을 적용한다.

## 체크리스트

- [x] PatientTypeBMale/Female 루트에 `NetworkObject`(IsSpawnable) + `PatientController` + `CapsuleCollider` 존재.
- [ ] 상태 컴포넌트(`PatientTypeBMaleState`/`FemaleState`)의 누운 자세/콜라이더 오프셋 플레이 검증.
- [x] `DefaultPrefabObjects.asset` 에 두 B/C 프리팹 GUID 포함.
- [x] SO 에 `patient_b`/`bed_b`/`patient_c`/`bed_c` 등록 + 각 환자에 unwrap+link 침대 참조.
- [ ] "Validate Presets (Editor)" 경고 없음.
- [ ] 디버거로 결합 스폰 확인(호스트 + 원격 클라).
- [ ] `patient_dummy_d_b` 프리팹/EntityPreset 등록 및 분류 클릭 producer 검증.
