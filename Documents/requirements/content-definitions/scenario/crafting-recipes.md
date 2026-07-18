---
title: "시나리오 조합(crafting) 레시피"
doc_type: requirement
domain: content-definitions
progress: "3-implemented"
status: active
updated: 2026-07-18
---

# 시나리오 조합(crafting) 레시피

모든 레시피와 산출물 아이템은 `RegisteringMultiplayerInfrastructureSupport.Item.cs`의 `RegisterAllItems()` 및 `RegisterAllCombineRecipes()`에 등록되어 있다. 조합은 `ItemCombineRecipeRegistry`가 처리하며, 아래의 모든 레시피는 재료를 각각 1개 소비하여 산출물을 1개 만든다.

## 일반 레시피

| 산출물 | 재료 | 사용 시나리오 |
|---|---|---|
| `laryngoscope` | `laryngoscope_blade` + `laryngoscope_handle` | patient_a |
| `endotracheal_tube_ready` | `endotracheal_tube` + `stylet` | patient_a |
| `normal_saline_intravenous_ready` | `normal_saline_1000ml` + `intravenous_set` | disaster_intro, patient_a |
| `plasma_solution_intravenous_ready` | `plasma_solution_1000ml` + `intravenous_set` | disaster_intro, patient_a |
| `yankauer_suction_ready` | `suction_line` + `yankauer` | patient_a |
| `humidifier_sterile_distilled_water_bottle` | `humidifier_bottle` + `sterile_distilled_water` | patient_a, patient_b_c |
| `oxyflowmeter` | `humidifier_sterile_distilled_water_bottle` + `flowmeter` | patient_a, patient_b_c |

## 주사기 레시피

용액은 `epinephrine_ampule`, `norepinephrine_ampule`, `normal_saline_20ml`의 세 종류이며, 주사기 용량은 `5cc`, `20cc`, `50cc`이다. 카테터는 `cannula_16g`, `cannula_18g`, `cannula_20g`, `cannula_22g`, `cannula_24g`를 사용한다.

### 용액 + 주사기

| 산출물 | 재료 |
|---|---|
| `epinephrine_5cc_syringe` | `epinephrine_ampule` + `syringe_5cc` |
| `epinephrine_20cc_syringe` | `epinephrine_ampule` + `syringe_20cc` |
| `epinephrine_50cc_syringe` | `epinephrine_ampule` + `syringe_50cc` |
| `norepinephrine_5cc_syringe` | `norepinephrine_ampule` + `syringe_5cc` |
| `norepinephrine_20cc_syringe` | `norepinephrine_ampule` + `syringe_20cc` |
| `norepinephrine_50cc_syringe` | `norepinephrine_ampule` + `syringe_50cc` |
| `normal_saline_5cc_syringe` | `normal_saline_20ml` + `syringe_5cc` |
| `normal_saline_20cc_syringe` | `normal_saline_20ml` + `syringe_20cc` |
| `normal_saline_50cc_syringe` | `normal_saline_20ml` + `syringe_50cc` |

### 카테터 포함 주사기

카테터 포함 산출물은 `{solution}_{gauge}_{volume}_syringe` 식별자를 사용한다. 각 산출물은 아래 두 경로로 조합할 수 있다. `solution`은 `epinephrine`, `norepinephrine`, `normal_saline`, `gauge`는 `16g`, `18g`, `20g`, `22g`, `24g`, `volume`은 `5cc`, `20cc`, `50cc`의 모든 조합이다(45종).

| 경로 | 재료 | 산출물 |
|---|---|---|
| 직접 조합 | 해당 용액 + 해당 용량 주사기 + 해당 게이지 카테터 | `{solution}_{gauge}_{volume}_syringe` |
| 단계 조합 | `{solution}_{volume}_syringe` + 해당 게이지 카테터 | `{solution}_{gauge}_{volume}_syringe` |

예를 들어 `epinephrine_18g_20cc_syringe`는 `epinephrine_ampule` + `syringe_20cc` + `cannula_18g`로 직접 조합하거나, `epinephrine_20cc_syringe` + `cannula_18g`로 단계 조합할 수 있다.
