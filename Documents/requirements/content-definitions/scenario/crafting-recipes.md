---
title: "시나리오 조합(crafting) 레시피 모음"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-07-09
flags: ["refactor-required"]
---

# 시나리오 조합(crafting) 레시피 모음

## 배경 (d-2)

기존 시나리오 그래프는 아이템 조합을 `CombineItem` **노드**로 표현했다. 그러나 조합은 이제 **노드가 아니라 시스템**(`MultiplayerInfrastructure.ItemSystem.ItemCombineRecipeRegistry`)으로 처리한다. 조합은 인벤토리 UI의 조합 패널을 통해 **수동 수행**되며, `CombineItem` 노드의 `AutoCombine`은 실질 조합을 수행하지 않는다(엔진 `ScenarioController.ExecuteCombineItemNode`는 `AutoCombine=true`일 때 로그 후 진행만 함).

따라서:

- 조합은 시나리오 노드에서 분리하여 **이 문서에 레시피로 모아 표기**한다.
- 시나리오 그래프는 "조합 완료된(prepared) 아이템"을 전제로, 해당 산출물 아이템을 획득/사용하는 흐름만 참조한다. 원본에서 `CombineItem` 노드가 있던 자리에는 "이 레시피의 산출물이 준비되어 있어야 한다"는 참조 주석을 단다.
- 레시피는 C# 코드(`RegisteringMultiplayerInfrastructureSupport.Item.cs`의 `RegisterAllCombineRecipes()`)로 등록한다. **ScriptableObject/JSON 레시피 자산은 없다.**

## 레시피 모델 (참조)

- 클래스: `MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe` (`Ingredients`: `RecipeIngredient{ Identifier, RequiredCount }`, `OutputItemIdentifier`, `OutputItemCount`)
- 레지스트리: `ItemCombineRecipeRegistry` (`Register`/`GetAll`/`TryGetMatchingRecipe`)
- 등록 지점: `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/RegisteringMultiplayerInfrastructureSupport.Item.cs:148` `RegisterAllCombineRecipes()`

## 레시피 목록 (시나리오 요구 산출물)

> 상태 표기: `등록됨` = C# 레지스트리에 이미 존재, `등록필요` = 시나리오는 요구하나 레지스트리 미등록.

| 산출물(output) | 입력(inputs) | 사용 시나리오 | C# 레지스트리 등록 | 명명 주의 |
|---|---|---|---|---|
| `ns1_ready` | `normal_saline_1000ml` + `intravenous_set` | disaster_intro / patient_a | 등록필요 | — |
| `ps1_ready` | `plasma_solution_1000ml` + `intravenous_set` | disaster_intro / patient_a | 등록필요 | — |
| `yankauer_ready` | `suction_line` + `yankauer` | patient_a | 등록필요 | — |
| `laryngoscope` | `laryngoscope_handle` + `laryngoscope_blade` | patient_a | **등록됨** | 입력 식별자는 `laryngo_handle`/`laryngo_blade`가 아니라 `laryngoscope_handle`/`laryngoscope_blade` |
| `endotracheal_tube_ready` | `endotracheal_tube` + `stylet` | patient_a | **등록됨** | 시나리오 구식 `et_tube_ready`/`et_tube`/`stylet` → `endotracheal_tube_ready`/`endotracheal_tube`로 정합 |
| `humidifierbottle_ready` | `humidifierbottle` + `sdw`(멸균증류수) | patient_a / patient_b_c | 등록필요 | `sdw` 아이템 식별자 확정 필요 |
| `oxyflowmeter` | `humidifierbottle_ready` + `flowmeter` | patient_a | 등록필요 | 아이템 base model 자산은 존재(`oxyflowmeter.asset`) |
| `oxyflowmeter_b` | `humidifierbottle_ready` + `flowmeter` | patient_b_c (환자 B) | 등록필요 | 산출물만 다르고 레시피 동일 → 단일 `oxyflowmeter`로 통합 검토 |
| `oxyflowmeter_c` | `humidifierbottle_ready` + `flowmeter` | patient_b_c (환자 C) | 등록필요 | 〃 |
| `epinephrine_syringe` | `epinephrine_ampule` + `syringe_5cc` | patient_a | **등록됨** | 시나리오 구식 `epi_ready`/`epi` → `epinephrine_syringe`/`epinephrine_ampule`로 정합 |
| `ns_20cc_ready` | `normal_saline_20ml` + `syringe_20cc` | patient_a | 등록필요 | 시나리오 구식 `ns_20cc` → `normal_saline_20ml`로 정합 |

## 확정 요청·후속 작업 (인간 작업자)

- [ ] 명명충돌 확정: 시나리오 구식 산출물 `et_tube_ready` ↔ C# `endotracheal_tube_ready`, `epi_ready` ↔ `epinephrine_syringe`. 시나리오/JSON을 C# 정본으로 맞출지, 반대로 C# 레시피 산출물명을 바꿀지 확정 필요. (권장: C# 정본에 맞춰 시나리오 산출물명 치환)
- [ ] 등록필요 레시피 8종(`ns1_ready`, `ps1_ready`, `yankauer_ready`, `humidifierbottle_ready`, `oxyflowmeter`, `oxyflowmeter_b`, `oxyflowmeter_c`, `ns_20cc_ready`)을 `RegisterAllCombineRecipes()`에 추가.
- [ ] `oxyflowmeter_b`/`oxyflowmeter_c`가 실제로 별도 산출물이어야 하는지 검토. 레시피·입력이 동일하므로 단일 `oxyflowmeter`로 통합 가능한지 확정.
- [ ] 산출물 아이템 자산(base model) 신설 필요분 확인: `ns1_ready`, `ps1_ready`, `yankauer_ready`, `humidifierbottle_ready`, `epinephrine_syringe`(=구 `epi_ready`), `ns_20cc_ready`, `oxyflowmeter_b/c` 는 아이템 자산이 없을 수 있음.
- [ ] `sdw`(멸균증류수) 아이템 식별자 최종 확정.
- [ ] 입력 식별자 정합: `laryngo_handle`/`laryngo_blade` → `laryngoscope_handle`/`laryngoscope_blade`, `et_tube`/`epi`/`ns1`/`ps1`/`ns_20cc` 등 구식 식별자를 아이템 정본 식별자로 치환(→ `interaction-signal-integration-spec.md` §2 아이템 식별자↔조건명 정합 표 참조).
