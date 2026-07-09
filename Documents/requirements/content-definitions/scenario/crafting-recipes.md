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
- 등록 지점: `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/RegisteringMultiplayerInfrastructureSupport.Item.cs` `RegisterAllCombineRecipes()`
- 산출물 아이템 정의: `Assets/Modules/TriageTrainer/Scripts/Items/Definitions/` 하위 클래스(예: `NormalSalineIntravenousReady`, `YankauerSuctionReady`), `RegisterAllItems()`에 함께 등록.
- 조합 매칭: `TryGetMatchingRecipe`는 등록 순서 기준 첫 매칭 레시피를 반환한다(레시피별 재료 AND 매칭). 산출물별로 개별 레시피가 등록되므로 서로 다른 산출물은 각 레시피로 독립 판정된다.

## 레시피 목록 (시나리오 요구 산출물)

> 상태 표기: `등록됨` = C# 레지스트리에 이미 존재, `등록필요` = 시나리오는 요구하나 레지스트리 미등록, `등록필요(재료 미존재로 보류)` = 재료 아이템이 아직 없어 등록 불가.
>
> 등록 지점: `RegisterAllCombineRecipes()` (`RegisteringMultiplayerInfrastructureSupport.Item.cs`). 산출물 아이템 정의는 `Assets/Modules/TriageTrainer/Scripts/Items/Definitions/` 하위 클래스로 신설·등록.

| 산출물(output) | 입력(inputs) | 사용 시나리오 | C# 레지스트리 등록 | 명명 주의 |
|---|---|---|---|---|
| `normal_saline_intravenous_ready` (구 `ns1_ready`) | `normal_saline_1000ml` + `intravenous_set` | disaster_intro / patient_a | **등록됨** (2026-07-09) | 산출물 식별자 확정: `normal_saline_intravenous_ready`. Display "식염수 수액 세트". 리소스(아이콘·모델) `intravenous_set` 복사 배치 완료. |
| `plasma_solution_intravenous_ready` (구 `ps1_ready`) | `plasma_solution_1000ml` + `intravenous_set` | disaster_intro / patient_a | **등록됨** (2026-07-09) | 산출물 식별자 확정: `plasma_solution_intravenous_ready`. Display "혈장 수액 세트". 리소스(아이콘·모델) `intravenous_set` 복사 배치 완료. |
| `yankauer_suction_ready` (구 `yankauer_ready`) | `suction_line` + `yankauer` | patient_a | **등록됨** (2026-07-09) | 산출물 식별자 확정: `yankauer_suction_ready`. Display "준비된 양커 석션". 아이콘 `yankauer` 복사 배치 완료(원본 `yankauer` 는 모델 프리팹 미보유 → 아이콘 전용). |
| `laryngoscope` | `laryngoscope_handle` + `laryngoscope_blade` | patient_a | **등록됨** | 입력 식별자는 `laryngo_handle`/`laryngo_blade`가 아니라 `laryngoscope_handle`/`laryngoscope_blade` |
| `endotracheal_tube_ready` | `endotracheal_tube` + `stylet` | patient_a | **등록됨** | 시나리오 구식 `et_tube_ready`/`et_tube`/`stylet` → `endotracheal_tube_ready`/`endotracheal_tube`로 정합 |
| `humidifierbottle_ready` | `humidifierbottle` + `sterile_distilled_water`(멸균증류수) | patient_a / patient_b_c | 등록필요(재료 미존재로 보류) | 재료 아이템 `humidifierbottle`, `sterile_distilled_water` 미존재 |
| `oxyflowmeter` | `humidifierbottle_ready` + `flowmeter` | patient_a / patient_b_c (B·C 공용) | 등록필요(재료 미존재로 보류) | `humidifierbottle_ready`·`flowmeter` 미존재. 아이템 base model 자산은 존재(`oxyflowmeter.asset`). `oxyflowmeter_b`/`oxyflowmeter_c`는 단일 `oxyflowmeter`로 통합(레시피·입력 동일). |
| `epinephrine_5cc_syringe` (구 `epinephrine_syringe`) | `epinephrine_ampule` + `syringe_5cc` | patient_a | **등록됨** (2026-07-09) | 명명 규칙 정합 위해 구 `epinephrine_syringe` 아이템을 `epinephrine_5cc_syringe`로 대체. 아래 §"주사기 조합 완제품 & 조합 관계" 참조. |

> 참고: 구 `ns_20cc_ready`(별도 산출물)는 명명 규칙에 어긋나 **제거**하고, 아래 §"주사기 조합 완제품 & 조합 관계"의 규칙적 산출물(`normal_saline_20cc_syringe` 등)로 대체함.

## 주사기 조합 완제품 & 조합 관계 (d-4, 2026-07-09)

주사기 계열 완제품은 명명 규칙 `{용액}_{게이지?}_{용량}_syringe` 를 따른다. 게이지(카테터)가 없는 단순 완제품과, 게이지가 결합된 완제품을 모두 지원한다.

### 조합 관계(3종)

| 관계 | 조합식 | 산출물 |
|---|---|---|
| 관계 1 | 용액 + `*cc 주사기` | `{용액}_{*cc}_syringe` (카테터 없음) |
| 관계 2 | `{용액}_{*cc}_syringe` + `*게이지` | `{용액}_{*게이지}_{*cc}_syringe` |
| 관계 3 | 용액 + `*cc 주사기` + `*게이지` | `{용액}_{*게이지}_{*cc}_syringe` |

- 관계 2·3의 최종 산출물은 동일하다. 즉 게이지 포함 완제품은 "미리 용액을 채운 주사기에 카테터를 결합(관계 1→2)"하거나 "한 번에 조합(관계 3)"하는 두 경로 모두로 만들 수 있다.
- 용액 식별자: 에피네프린 `epinephrine_ampule`, 노르에피네프린 `norepinephrine_ampule`, 생리식염수 `normal_saline_20ml`(용량 구분은 주사기 종류로만 표현).

### 카테터 없는 주사기 완제품 (관계 1, 9종) — **등록됨** (2026-07-09)

`{용액} × {5cc, 20cc, 50cc}` = 9종. 아이템 정의 신설·등록, 리소스(아이콘·모델)는 `syringe_{용량}` 복사 배치.

| 산출물 | 입력(용액 + 주사기) | 클래스 |
|---|---|---|
| `epinephrine_5cc_syringe` | `epinephrine_ampule` + `syringe_5cc` | `Epinephrine5ccSyringe` |
| `epinephrine_20cc_syringe` | `epinephrine_ampule` + `syringe_20cc` | `Epinephrine20ccSyringe` |
| `epinephrine_50cc_syringe` | `epinephrine_ampule` + `syringe_50cc` | `Epinephrine50ccSyringe` |
| `norepinephrine_5cc_syringe` | `norepinephrine_ampule` + `syringe_5cc` | `Norepinephrine5ccSyringe` |
| `norepinephrine_20cc_syringe` | `norepinephrine_ampule` + `syringe_20cc` | `Norepinephrine20ccSyringe` |
| `norepinephrine_50cc_syringe` | `norepinephrine_ampule` + `syringe_50cc` | `Norepinephrine50ccSyringe` |
| `normal_saline_5cc_syringe` | `normal_saline_20ml` + `syringe_5cc` | `NormalSaline5ccSyringe` |
| `normal_saline_20cc_syringe` | `normal_saline_20ml` + `syringe_20cc` | `NormalSaline20ccSyringe` |
| `normal_saline_50cc_syringe` | `normal_saline_20ml` + `syringe_50cc` | `NormalSaline50ccSyringe` |

### 게이지 포함 주사기 완제품 (관계 2·3, 45종) — **등록됨**

`{용액} × {16g,18g,20g,22g,24g} × {5cc,20cc,50cc}` = 45종(`{용액}_{게이지}_{용량}_syringe`). 아이템·리소스는 기존에 존재. 이번에 **관계 2 레시피 45종**을 `RegisterFilledSyringeWithGaugeRecipes()`(표 순회 등록)로 추가하여, 관계 3(기존 45종)과 함께 두 경로 모두 지원한다.

## 확정 요청·후속 작업 (인간 작업자)

- [x] 명명충돌 확정: 시나리오 구식 산출물 `et_tube_ready` ↔ C# `endotracheal_tube_ready`, `epi_ready` ↔ (구)`epinephrine_syringe`. C# 정본에 맞춰 시나리오 산출물명을 치환하는 것으로 확정. 구 `epinephrine_syringe` 는 명명 규칙에 맞춰 **`epinephrine_5cc_syringe` 로 대체**함(2026-07-09). (`endotracheal_tube_ready`, `epinephrine_5cc_syringe`)
  - [ ] 시나리오 문서/JSON 내 구 산출물명 `epinephrine_syringe` → `epinephrine_5cc_syringe` 치환 필요 — 인간 작업자/후속 작업(교차 문서 정합). 현재 코드/레지스트리에는 `epinephrine_syringe` 아이템이 더 이상 존재하지 않는다.
    - 코멘트: 모든 종류의 `epinephrine_*_syringe` 를 에피네프린 주사기로서 판별한다. 때문에 or 판단 조건을 로직에 추가하라고 하였다.
  - [x] (*1) 판단 로직 OR 조건 — 인간 작업자 결정에 따라 **(A)·(B) 모두 채택**. 문제 해결은 (B)로 수행하고, (A)는 시스템 확장으로 제안(Feature Proposal)함 (2026-07-09). 에피네프린 주사기는 카테터 없는 완제품 3종 + 게이지 포함 완제품 15종 = 총 18종이 존재하므로, "에피네프린 주사기를 사용" 게이트는 이들 중 **어느 하나라도** 충족하면 통과해야 한다.
    - 배경(계측 구조): 조합·판정은 노드가 아니라 시스템(`ItemCombineRecipeRegistry`)과 개별 시그널(`MedicalItem.OnGet`의 `sig.click_<id>`, `PatientController`의 아이템 사용 시그널 `push_epi`/`push_ns` 등)로 처리된다. 시그널은 **아이템 식별자 단위**라 변형별 아이템이 서로 다른 시그널을 올린다. 현재 `RegistryContains` Validator 는 규칙을 **AND** 로만 결합하므로 "여러 변형 중 하나" OR 게이트를 표현할 수 없다.
    - [x] **(B) 콘텐츠 레벨 대응 — 구현·문서화 완료 (우선 대응).** 사용 시점 대표 시그널(`sig.push_epi`)로 게이팅하면 어떤 변형을 조합·투여했든 통과하므로 OR 조건이 자연히 성립(시스템 변경 불필요). `patient_a_critical.md` 의 에피네프린 투여 게이트(V026_2·V029_2)는 이미 `sig.push_epi` 로 검사하며, 해당 지점에 (B) 처리 주석을 추가함.
      - [x] 공통 지침을 `interaction-signal-integration-spec.md` §6("OR 게이트 처리 정책")에 신설하여, 다른 시나리오 문서에서도 이 부류 문제는 **(B) 우선** 대응하도록 명문화함.
    - [x] **(A) 시스템 레벨 대응 — Feature Proposal 작성 완료(미반영).** Validator `RegistryContains` 에 root condition 별 `matchMode: All|Any` 를 도입하는 확장을 제안. `MultiplayerInfrastructure` 변경이라 직접 수정 대신 제안+예시 구현으로 분리(AGENTS.md 규칙). 위치: `Agents/Proposals/scheduled/2026-07-09-scenario-validator-any-match-mode/`.
      - [ ] (A) 제안의 실제 모듈 반영 — 인간 작업자 검토·승인 후 진행(하위호환 `All` 기본값 보장). 반영 전에도 (B)로 문제는 해결된 상태.
- [x] 등록필요 레시피 중 재료가 모두 존재하는 3종(`ns1_ready`, `ps1_ready`, `yankauer_ready`)을 `RegisterAllCombineRecipes()`에 추가 완료 (2026-07-09).
  - [x] `ns1_ready` → 산출물 아이템 `normal_saline_intravenous_ready`(Display "식염수 수액 세트") 신설·등록, 레시피 `normal_saline_1000ml` + `intravenous_set` 등록.
  - [x] `ps1_ready` → 산출물 아이템 `plasma_solution_intravenous_ready`(Display "혈장 수액 세트") 신설·등록, 레시피 `plasma_solution_1000ml` + `intravenous_set` 등록.
  - [x] `yankauer_ready` → 산출물 아이템 `yankauer_suction_ready`(Display "준비된 양커 석션") 신설·등록, 레시피 `suction_line` + `yankauer` 등록.
  - [x] 위 3종 산출물 아이템의 아이콘 스프라이트/3D 모델 리소스 추가 (2026-07-09). 재료 리소스를 복사해 신규 식별자 자산으로 배치하고, `.meta` 는 고유 GUID로 신규 발급함(중복 없음 확인). 대상 경로: `Resources/Textures/Items/{id}.png`(아이콘), `Resources/Models/Items/{id}.prefab`(모델).
    - `normal_saline_intravenous_ready` / `plasma_solution_intravenous_ready`: 아이콘·모델 모두 `intravenous_set` 복사.
    - `yankauer_suction_ready`: 아이콘 `yankauer` 복사. 모델은 원본 `yankauer` 자체가 3D 모델 프리팹을 갖지 않으므로(아이콘 전용) 동일하게 아이콘만 배치함 → `ValidateItemResources` 의 모델 누락 경고는 원본 `yankauer` 와 동일 상태(별도 모델 준비 시 함께 처리).
- [x] 구 `ns_20cc_ready` 산출물은 명명 규칙 위배로 **제거** (아이템·리소스·레지스트리·레시피 삭제). 카테터 없는 주사기 완제품은 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1)로 대체함. → §"주사기 조합 완제품 & 조합 관계" 참조.
- [x] 주사기 조합 완제품 3관계 구현 (2026-07-09). 관계 1(카테터 없는 완제품 9종) 아이템·리소스·레시피 신설·등록. 관계 2(게이지 결합 45종) 레시피를 `RegisterFilledSyringeWithGaugeRecipes()`로 추가. 관계 3(기존 45종)은 유지. → §"주사기 조합 완제품 & 조합 관계" 참조.
- [ ] 등록필요 레시피 중 재료 미존재로 **보류**된 2종(`humidifierbottle_ready`, `oxyflowmeter`) — 재료 아이템 선행 추가 필요.
  - [ ] `humidifierbottle`(가습병) 아이템 신설 — 미존재. 인간 작업자/후속 작업.
  - [ ] `sterile_distilled_water`(멸균증류수) 아이템 신설 — 미존재. 인간 작업자/후속 작업.
  - [ ] `flowmeter`(유량계) 아이템 신설 — 미존재. 인간 작업자/후속 작업.
  - [ ] 위 3종 재료가 준비되면 `humidifierbottle_ready`(= `humidifierbottle` + `sterile_distilled_water`), `oxyflowmeter`(= `humidifierbottle_ready` + `flowmeter`) 레시피를 `RegisterAllCombineRecipes()`에 추가.
- [x] `oxyflowmeter_b`/`oxyflowmeter_c`가 별도 산출물이어야 하는지 검토 → 레시피·입력이 동일하므로 단일 `oxyflowmeter`로 통합하기로 확정. 본문 표를 `oxyflowmeter` 단일 행으로 통합(환자 B·C 공용)하고 `oxyflowmeter_b`/`oxyflowmeter_c` 행을 제거함.
- [x] `sdw`(멸균증류수)는 `sterile_distilled_water`로 식별자 확정하기
  - [x] 본 문서 내 `sdw` → `sterile_distilled_water` 치환 완료.
  - [ ] 다른 문서(예: `patient_a_critical.md`, `patient_b_c*.md`, `disaster_intro*.md` 등) 전반의 `sdw` → `sterile_distilled_water` 치환 — 인간 작업자/후속 작업(교차 문서 정합).
- [ ] 입력 식별자 정합: `laryngo_handle`/`laryngo_blade` → `laryngoscope_handle`/`laryngoscope_blade`, `et_tube`/`epi`/`ns1`/`ps1` 등 구식 식별자를 아이템 정본 식별자로 치환(→ `interaction-signal-integration-spec.md` §2 아이템 식별자↔조건명 정합 표 참조). — 아이템 픽업 조건명(`click_*`)은 spec §2에서 이미 정합 완료; 조합 산출물명 치환은 위 항목들로 반영됨. `ns_20cc(_ready)` 는 산출물 제거로 폐기(→ `normal_saline_20cc_syringe`).
