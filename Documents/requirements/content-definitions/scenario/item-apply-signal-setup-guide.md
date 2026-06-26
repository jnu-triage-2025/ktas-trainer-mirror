---
title: "아이템 처치(apply) 사용 신호 설정 가이드"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-25
---

# 아이템 처치(apply) 사용 신호 설정 가이드

이 가이드는 운영자가 "학습자가 아이템(거즈/고정기/플라스터/장갑 등)을 환자에게 **사용(적용)** 하면
시나리오 게이트가 통과되도록" 설정하는 방법을 설명합니다.

> 중요(2026-06-25): 표준 처치/사정 신호는 **코드에 기본값으로 하드코딩**되어 있습니다. 따라서 아래 매핑을
> 인스펙터에 입력하지 않아도(또는 컴포넌트 Reset 으로 직렬화 필드가 비워져도) 표준 케이스는 자동 동작합니다.
> 인스펙터 입력은 **기본값을 덮어쓰거나(override) 비표준 신호를 추가**할 때만 필요합니다.
> 하드코딩 기본값 정의: `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.SignalDefaults.cs`,
> `PatientController.Assess.cs`(`DefaultAssessActions`).

## 1. 동작 원리 (2026-06-25 구현)

- 플레이어가 아이템을 들고 **조준한 상태에서 사용** 하면, `PlayerController` 가 조준 대상(크로스헤어
  레이캐스트 히트)에서 `IItemUseTarget` 을 찾아 `OnItemUsed(사용자, 아이템식별자)` 를 호출합니다.
- 대상이 **환자(`PatientController`)** 또는 **환자침대(`MovingPatientBedController`)** 이면, 설정된
  부착 비주얼을 켜고(기존 동작), **그 아이템에 매핑된 시나리오 신호(`sig.*`)를 올립니다.**
- 신호가 올라가면 대응 Validator 게이트(예: `V016_2 sig.apply_gauze`)가 통과됩니다.

## 2. 설정 방법 (Unity 에디터)

환자/환자침대 프리팹의 `PatientController`(또는 `MovingPatientBedController`) 인스펙터에서
**Attachable Item Visuals** 목록의 각 항목에 다음을 채웁니다.

| 필드 | 설명 |
|---|---|
| `Item Identifier` | 사용할 아이템 식별자(예: `gauze`, `neckstabilizer`, `gloves`) |
| `Visual Object` | 적용 시 켜질 부착 비주얼 GameObject |
| `Apply Signal` | **(신규)** 적용 완료 시 올릴 시나리오 신호 조건명. 예: `apply_gauze`, `apply_stabilizer_patient_a`, `wear_glove` |

- `Apply Signal` 을 **비워 두면** 신호를 올리지 않습니다(기존 동작, 시각 부착만).
- 신호명은 `sig.` 접두사 없이 조건명만 입력합니다(자동으로 `sig.` 부착).

### 시나리오 게이트와의 매핑 예 (disaster 시나리오)

| 아이템(Item Identifier) | Apply Signal | 게이트 노드 |
|---|---|---|
| `gauze` | `apply_gauze` | `V016_2`(환자 A), `V058`(환자 B) |
| `neckstabilizer` | `apply_stabilizer_patient_a` | `V013_1`(환자 A) |
| `plaster` | `apply_plaster_on_intu` / `apply_plaster_on_gauze` | `V014_5` 등 |
| `gloves` | `wear_glove` | `V016_1` 등 |
| `electrode` | `apply_electrode` | `V043`(환자 B) |

## 2-1. Item Use Signals (시각 부착 없는 사용 신호)

흡인/앤부/약물 투여처럼 **시각 부착이 필요 없는** 사용 동작은 `PatientController`(또는
`MovingPatientBedController`)의 **Item Use Signals** 목록에 매핑합니다.

| 필드 | 설명 |
|---|---|
| `Item Identifier` | 사용할 아이템 식별자(예: `yankauer`, `ambubag`, `epinephrine_ampule`) |
| `Use Signal` | 사용 시 올릴 시나리오 신호 조건명(예: `suction_patient_a`, `start_ambu`, `push_epi`) |

매핑 예:

| 아이템 | Use Signal | 게이트 |
|---|---|---|
| `yankauer` | `suction_patient_a` | `V013_4`(환자 A) |
| `ambubag` | `start_ambu` | 환자 A CPR 구간 |
| `epinephrine_ampule` | `push_epi` | `V026_2`(환자 A) |
| `normal_saline_20ml` | `push_ns` | 환자 A CPR 구간 |

> 부착(Apply)과 사용(Use) 신호는 독립적으로 동작합니다. 시각 부착이 있으면 Attachable Item Visuals에,
> 없으면 Item Use Signals에 매핑하면 됩니다. 둘 다 설정해도 됩니다.

## 2-2. Assess Actions (환자 사정 신호)

의식상태(AVPU/GCS)·활력징후·맥박 확인처럼 "환자를 클릭해 사정"하는 동작은 `PatientController` 의
**Assess Actions** 목록에 등록합니다. 등록된 사정은 환자 상호작용 힌트로 노출되고, 수행 시 신호를 올립니다.

| 필드 | 설명 |
|---|---|
| `Identifier` | 사정 동작 식별자(중복 불가). 예: `assess_avpu_gcs`, `assess_pulse` |
| `Display Text` | 상호작용 힌트 문구. 예: `의식상태 사정`, `맥박 확인` |
| `Assess Signal` | 수행 시 올릴 신호 조건명. 예: `check_avpu_gcs_patient_a`, `check_pulse_patient_a` |
| `Enabled` | 노출 여부. 시나리오 진행 중 `SetAssessActionEnabled(id, bool)` 로 제어 가능 |

매핑 예:

| Assess Signal | 게이트 |
|---|---|
| `check_avpu_gcs_patient_a` | `V012`(환자 A) |
| `check_pulse_patient_a` | `V022`, `V031`(환자 A) |
| `check_gcs_patient_b` | `V041`(환자 B) |
| `check_vital_patient_b` | `V045`(환자 B) |
| `check_gcs_patient_c` | `V060`(환자 C) |

> `show_vital_patient_a`, `close_vital_ui_b/c` 는 바이탈 모니터 UI 열기/닫기 콜백이 필요해 별도(후속)입니다.

## 3. 테스트

1. 거즈 아이템을 획득해 들고, 환자를 조준한 상태에서 사용 입력.
2. 거즈 부착 비주얼이 켜지고 `sig.apply_gauze` 가 올라가는지 확인(`/scenario` 디버그 또는 게이트 진행).
3. 대응 Validator 게이트(`V016_2`)가 통과되어 다음 단계로 진행되는지 확인.
4. 평가 기록(`RubricRecorder`)에 해당 항목이 "수행"으로 기록되는지 확인.

## 3-1. 코드 하드코딩 기본값 (Reset 무관)

아래는 `PatientController` 에 코드 상수로 내장되어 인스펙터/Reset 과 무관하게 항상 적용됩니다
(`{id}` 는 환자 Identifier 로 치환). 인스펙터에 같은 항목을 넣으면 그쪽이 우선합니다.

- 아이템 사용(부착 없음): `yankauer`/`yankauer_ready`→`suction_{id}`, `ambubag`→`start_ambu`,
  `epinephrine_ampule`→`push_epi`, `normal_saline_20ml`→`push_ns`.
- 아이템 부착(시각 적용 시): `gauze`→`apply_gauze`, `gloves`→`wear_glove`, `electrode`→`apply_electrode`,
  `neckstabilizer`→`apply_stabilizer_{id}`, `plaster`→`apply_plaster_on_gauze`+`apply_plaster_on_intu`(둘 다).
  - 단, 부착 신호는 해당 아이템의 **부착 비주얼(Visual Object)** 이 설정되어 있어야 발화한다(비주얼은 콘텐츠).
- 사정(환자 클릭): 표준 사정 동작 `assess_avpu_gcs`/`assess_pulse`/`assess_gcs`/`assess_vital` 가 자동 노출되며
  각각 `check_avpu_gcs_{id}`/`check_pulse_{id}`/`check_gcs_{id}`/`check_vital_{id}` 를 올린다.

비표준(인스펙터 override 필요) 예: `check_gcs_a_rosc`(ROSC 후 GCS, `{id}` 규칙과 불일치) → 해당 환자
Assess Actions 에 `AssessSignal=check_gcs_a_rosc` 로 명시.

> 비고: 위 기본값은 **PatientController** 에만 내장된다. `MovingPatientBedController` 는 인스펙터 매핑만 사용한다
> (침대는 어떤 환자인지 모호하므로 환자별 신호 기본값을 두지 않음).

## 4. 한계 / 후속

- 본 구현은 **사용(Use) 입력 → 조준 대상의 `IItemUseTarget`** 경로다. 조준이 빗나가면(히트 없음)
  아무 일도 일어나지 않는다(기존 동작 유지, 안전).
- 약물 주입/흡인/제거/NPC 전달 등은 별도 메커닉이 필요하다(`interaction-signal-integration-spec.md` §2 참고).
- 부착 비주얼/식별자 정합은 운영자가 프리팹에서 맞춰야 한다.

## 관련 문서

- [인터랙션 완료 신호 연결 명세](./interaction-signal-integration-spec.md) §2 apply_* 절
- [api-references/MultiplayerInfrastructure.Entity.IItemUseTarget.md](../../../api-references/MultiplayerInfrastructure.Entity.IItemUseTarget.md)
- 제안서: `Agents/Proposals/done/2026-06-25-item-use-target-signal/`
