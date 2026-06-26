---
title: "아이템 처치(apply) 사용 신호 · 처치 시각 표현 설정 가이드"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-26
---

# 아이템 처치(apply) 사용 신호 · 처치 시각 표현 설정 가이드

이 가이드는 "학습자가 아이템(거즈/고정기/장갑/앰부 등)을 환자에게 **사용**하면 처치 시각 표현이 켜지고
시나리오 게이트가 통과되도록" 하는 동작의 구성 방법을 설명합니다.

## 1. 설계 원칙 (데이터 / 컨트롤러 분리)

- **데이터**: `PatientDisplayState`(MonoBehaviour) 가 처치 표현 플래그(`DisplayState`)와 각 표현에 대응하는
  자식 GameObject 참조(`ChildGameObjects`)를 **데이터로만** 보유한다. (show/hide 로직 없음)
- **적용(컨트롤러)**: `PatientController` 가 아이템 사용/신호를 받아 해당 플래그를 켜고
  `ChildGameObjects` 의 대응 GameObject 를 `SetActive` 한다. **부위 구분(흉부/팔/눈썹)은 환자 프리팹의
  hierarchy/오브젝트 배치가 이미 반영**하므로, 컨트롤러는 플래그(=오브젝트)만 켜고 끈다.
- **매핑은 코드 하드코딩**: "어떤 아이템 → 어떤 처치 표현 + 어떤 신호" 는 `PatientController` 코드 상수
  (`ItemUseEffects`, `PatientController.TreatmentDisplay.cs`)로 내장되어 **인스펙터/Reset 과 무관**하게 동작한다.

## 2. 동작 흐름

1. 플레이어가 아이템을 들고 환자를 **조준한 상태에서 사용** → `PlayerController.UseItem()` 이
   크로스헤어 레이캐스트 히트에서 `IItemUseTarget`(=`PatientController`)을 찾아 `OnItemUsed(user, itemId)` 호출.
2. `PatientController.ApplyItemUse(itemId)` 가 `ItemUseEffects` 매핑을 조회하여
   - 매핑된 처치 표현(`TreatmentDisplay`)을 켠다(`DisplayState` 플래그 true + 자식 GameObject `SetActive(true)`),
   - 매핑된 시나리오 신호(`sig.*`)를 올린다(`{id}` 는 환자 Identifier 로 치환, 서버 권한 라우팅).
3. 신호가 올라가면 대응 Validator 게이트(예: `V016_2 sig.apply_gauze`)가 통과된다.

## 3. 운영자가 해야 하는 Unity 작업

코드 매핑은 이미 내장되어 있으므로, **데이터(프리팹) 쪽만** 맞추면 됩니다.

1. 환자 프리팹에 `PatientDisplayState` 컴포넌트가 있고, `ChildGameObjects` 의 각 항목에 **환자 몸의
   해당 처치 표현 오브젝트**(예: `GauzePatchedOnThorax`, `CervicalCollarOnNeck`, `AmbuBagAttachedToEndotrachealTube`)가
   연결되어 있는지 확인. (오브젝트는 평소 꺼져 있다가 처치 시 켜짐)
2. 환자 `PatientController.Identifier` 를 `patient_a` / `patient_b` / `patient_c` 로 지정
   (환자별 신호 `suction_{id}`, `apply_stabilizer_{id}` 등에 사용).
3. 사용할 아이템(거즈/고정기/장갑/앰부/에피네프린 등) 프리팹의 `Identifier` 가 매핑 키와 일치하는지 확인
   (`gauze`, `neckstabilizer`, `gloves`, `ambubag`, `epinephrine_ampule`, `normal_saline_20ml`, `yankauer` 등).

> 신호명을 인스펙터에 입력할 필요는 없습니다(코드 하드코딩). 비표준 매핑이 필요하면 `ItemUseEffects`
> (코드) 또는 Assess Actions 인스펙터를 통해 확장/override 합니다.

## 4. 코드 하드코딩 매핑 (현행)

`PatientController.TreatmentDisplay.cs` 의 `ItemUseEffects`:

| 아이템 | 처치 표현(TreatmentDisplay) | 신호 |
|---|---|---|
| `gauze` | `GauzePatchedOnThorax` | `apply_gauze` |
| `plaster` | `GauzeDressingDoneOnThorax` | `apply_plaster_on_gauze`, `apply_plaster_on_intu` |
| `gloves` | (없음) | `wear_glove` |
| `neckstabilizer` | `CervicalCollarOnNeck` | `apply_stabilizer_{id}` |
| `electrode` | (없음) | `apply_electrode` |
| `nasal` | `NasalCannulaApplied` | `apply_nasal_cannula` |
| `yankauer`/`yankauer_ready` | (없음) | `suction_{id}` |
| `ambubag` | `AmbuBagAttachedToEndotrachealTube` | `start_ambu` |
| `epinephrine_ampule` | (없음) | `push_epi` |
| `normal_saline_20ml` | (없음) | `push_ns` |

> 부위 구분이 다른 환자(예: 거즈를 좌측 상완에 적용하는 환자 B)는 해당 환자 프리팹의 `ChildGameObjects`
> 에 알맞은 부위 오브젝트를 연결하면 된다. 표현 플래그명이 부위까지 포함하지 않는 경우(예: `apply_gauze`)는
> 환자별 프리팹 배치로 부위가 결정되므로 코드 변경이 필요 없다. (흉부 외 부위를 별도 플래그로 켜야 하면
> `ItemUseEffects` 에 환자별 분기를 추가)

## 5. Assess Actions (환자 사정 신호)

의식/활력/맥박 사정은 `PatientController` 의 Assess 인터랙션으로 처리되며, 표준 사정 동작
(`assess_avpu_gcs`/`assess_pulse`/`assess_gcs`/`assess_vital`)은 **코드 기본값으로 자동 노출**되어
각각 `check_avpu_gcs_{id}`/`check_pulse_{id}`/`check_gcs_{id}`/`check_vital_{id}` 를 올린다.
비표준 신호(예: `check_gcs_a_rosc`)는 환자 Assess Actions 인스펙터에 `AssessSignal` 로 명시한다.
`SetAssessActionEnabled(id, bool)` 로 시나리오 진행 중 노출을 제어할 수 있다.

## 6. 테스트

1. 거즈 아이템을 들고 환자를 조준해 사용 → 흉부 거즈 오브젝트가 켜지고 `sig.apply_gauze` 발생, `V016_2` 통과.
2. 환자 사정 클릭 → `check_*` 게이트 통과, `RubricRecorder` "수행" 기록.
3. 흡인/앤부/약물 사용 → `suction_{id}`/`start_ambu`/`push_epi` 발생.

## 7. 한계 / 후속

- 조준이 빗나가면(레이캐스트 히트 없음) 아무 일도 일어나지 않는다(안전).
- 정맥 삽입(`insert_iv_*`), 제거(`remove_*`), NPC 전달(`pass_*`), 모니터 UI 토글(`show_vital`/`close_vital_ui_*`),
  신체부위/장비 클릭은 전용 메커닉이 필요하다(미구현).
- 처치 표현이 `apply_gauze` 같은 부위 무관 신호와 부위별 플래그 사이에서 환자별 분기가 필요하면
  `ItemUseEffects` 에 환자 분기를 추가한다(현재는 흉부 기준 기본값).

## 관련 문서

- [인터랙션 완료 신호 연결 명세](./interaction-signal-integration-spec.md) §2
- [api-references/MultiplayerInfrastructure.Entity.IItemUseTarget.md](../../../api-references/MultiplayerInfrastructure.Entity.IItemUseTarget.md)
