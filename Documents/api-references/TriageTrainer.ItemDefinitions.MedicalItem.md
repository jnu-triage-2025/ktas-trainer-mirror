# API 레퍼런스: TriageTrainer.ItemDefinitions.MedicalItem

> **네임스페이스:** `TriageTrainer.ItemDefinitions`  
> **기반 클래스:** `MultiplayerInfrastructure.ItemSystem.Item`  
> **파일 위치:** `Assets/Modules/TriageTrainer/Scripts/Items/MedicalItem.cs`

---

## 0. 개요

`MedicalItem`은 TriageTrainer 내 모든 의료 아이템 정의의 공통 추상 기반 클래스입니다.  
파생 클래스는 `Identifier`, `DisplayName`, `Description` 상수만 선언하면 되며, 아이템 획득 시 자동으로 시나리오 게이팅 신호를 발생시킵니다.

---

## 1. 기본 상수 (MedicalItem 공통 기본값)

| 상수 | 값 | 설명 |
|---|---|---|
| `IsStackable` | `true` | 스택 가능 여부 |
| `MaxStackCount` | `64` | 최대 스택 수 |
| `HasDurability` | `false` | 내구도 없음 |
| `MinReach` | `1.0f` | 최소 사용 거리(m) |
| `MaxReach` | `2.5f` | 최대 사용 거리(m) |
| `ItemDamage` | `0` | 공격 데미지 없음 |
| `EnabledCooldown` | `false` | 쿨다운 없음 |
| `Color` | `"white"` | UI 표시 색상 |

---

## 2. 파생 클래스 구현 패턴

모든 의료 아이템은 다음 구조를 따릅니다:

```csharp
namespace TriageTrainer.ItemDefinitions
{
  public class MyItem : MedicalItem
  {
    public const string Identifier   = "my_item";
    public const string DisplayName  = "내 아이템";
    public const string Description  = "아이템 설명입니다.";
  }
}
```

특별한 동작이 필요한 경우 `OnGet`, `OnUse`, `OnAttack`을 override합니다.

---

## 3. 시나리오 게이팅 신호 (OnGet)

아이템을 획득(인벤토리 추가)하면 `MedicalItem.OnGet`이 자동으로 다음 두 가지 신호를 발생시킵니다:

- `sig.<identifier>` — 아이템 획득 신호
- `sig.click_<identifier>` — 클릭/획득 이벤트 신호

시나리오의 `Validator` 노드에서 `RegistryContains(RuntimeState, "sig.click_<identifier>")` 조건으로 게이팅할 수 있습니다.

**주의:** 일부 시나리오 조건명이 아이템 식별자와 표기가 다른 경우가 있습니다 (예: `click_glove` ↔ `gloves`). 상세 목록은 `requirements/content-definitions/scenario/` 내 `interaction-signal-integration-spec.md`를 참조하세요.

---

## 4. 등록

모든 아이템은 `RegisteringMultiplayerInfrastructureSupport.Item.cs`의 `RegisterAllItems()`에서 등록됩니다:

```csharp
Registry.RegisterItemDefinition<MyItem>(MyItem.Identifier);
```

---

## 5. 아이템 정의 전체 목록

TriageTrainer에 정의된 모든 의료 아이템 목록입니다.

### 5.1 정맥로 재료 (IV Access)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Cannula16g` | `cannula_16g` | 16게이지 캐뉼라 |
| `Cannula18g` | `cannula_18g` | 18게이지 캐뉼라 |
| `Cannula20g` | `cannula_20g` | 20게이지 캐뉼라 |
| `Cannula22g` | `cannula_22g` | 22게이지 캐뉼라 |
| `Cannula24g` | `cannula_24g` | 24게이지 캐뉼라 |
| `IntravenousSet` | `intravenous_set` | 수액세트 |
| `BloodTransfusionSet` | `blood_transfusion_set` | 수혈세트 |
| `CentralLineSet` | `central_line_set` | C-line 세트 |

### 5.2 수액류 (IV Fluids)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `NormalSaline1000ml` | `normal_saline_1000ml` | N/S 1L (크린조) |
| `NormalSaline20ml` | `normal_saline_20ml` | N/S 20ml |
| `NormalSalineIntravenousReady` | `normal_saline_intravenous_ready` | 식염수 수액 세트 |
| `PlasmaSolution1000ml` | `plasma_solution_1000ml` | 플라즈마 솔루션 1L |
| `PlasmaSolutionIntravenousReady` | `plasma_solution_intravenous_ready` | 혈장 수액 세트 |

### 5.3 기도 관리 (Airway)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Ambubag` | `ambubag` | 앰부백 |
| `ReservoirBag` | `reservoir_bag` | 보유주머니(reservoir bag) |
| `FacialMask` | `facial_mask` | 안면 마스크 |
| `LaryngoscopeHandle` | `laryngoscope_handle` | 후두경 손잡이 |
| `LaryngoScopeBlade` | `laryngoscope_blade` | 후두경 블레이드 |
| `Laryngoscope` | `laryngoscope` | 후두경 (조합 완료) |
| `EndotrachealTube` | `endotracheal_tube` | 기관내관 |
| `Stylet` | `stylet` | 스타일렛 |
| `EndotrachealTubeReady` | `endotracheal_tube_ready` | 기관내관 (준비 완료) |

### 5.4 산소 투여 (Oxygen)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `O2Line` | `o2_line` | 산소 연결줄 |
| `Flowmeter` | `flowmeter` | 유량계 |
| `SterileDistilledWater` | `sterile_distilled_water` | 멸균증류수 |
| `Humidifier` | `humidifier_bottle` | 습윤병 |
| `HumidifierSterileDistilledWaterBottle` | `humidifier_sterile_distilled_water_bottle` | 멸균증류수가 담긴 습윤병 |
| `Oxyflowmeter` | `oxyflowmeter` | 멸균증류수가 담긴 습윤병이 연결된 산소 유량계 |

### 5.5 흡인 (Suction)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `WallSuction` | `wall_suction` | 석션 유닛 |
| `SuctionLine` | `suction_line` | 석션 라인 |
| `SuctionCatheter` | `suction_catheter` | 석션 카테터 |
| `Yankauer` | `yankauer` | 양카우어 석션 팁 |
| `YankauerSuctionReady` | `yankauer_suction_ready` | 조립된 양카우어 팁 |

### 5.6 심전도/제세동 (Cardiac Monitoring)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Electrode` | `electrode` | 전극 |
| `ElectrodeCable` | `electrode_cable` | 전극 케이블 |
| `DefibPad` | `defibpad` | 전극 패드 |
| `VitalSet` | `vital_set` | 활력징후 측정도구 |

### 5.7 약물 (Medications)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `EpinephrineAmpule` | `epinephrine_ampule` | 에피네프린 |
| `NorepinephrineAmpule` | `norepinephrine_ampule` | 노르에피네프린 |

### 5.8 주사기 (Syringes)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Syringe5cc` | `syringe_5cc` | 5cc 주사기 |
| `Syringe20cc` | `syringe_20cc` | 20cc 주사기 |
| `Syringe50cc` | `syringe_50cc` | 50cc 주사기 |

### 5.9 주사기 조합 완제품 (Prepared Syringes)

아래 항목들은 약물 앰풀 + 주사기 + (선택적) 캐뉼라의 조합 결과물입니다.  
명명 규칙: `<약물>_<캐뉼라게이지>_<용량>cc_syringe` 또는 `<약물>_<용량>cc_syringe` (캐뉼라 미포함)

**에피네프린 조합 주사기:**

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Epinephrine5ccSyringe` | `epinephrine_5cc_syringe` | 에피네프린이 든 5cc 주사기 |
| `Epinephrine20ccSyringe` | `epinephrine_20cc_syringe` | 에피네프린이 든 20cc 주사기 |
| `Epinephrine50ccSyringe` | `epinephrine_50cc_syringe` | 에피네프린이 든 50cc 주사기 |
| `Epinephrine16g5ccSyringe` | `epinephrine_16g_5cc_syringe` | 에피네프린이 든 16g 5cc 주사기 |
| `Epinephrine16g20ccSyringe` | `epinephrine_16g_20cc_syringe` | 에피네프린이 든 16g 20cc 주사기 |
| `Epinephrine16g50ccSyringe` | `epinephrine_16g_50cc_syringe` | 에피네프린이 든 16g 50cc 주사기 |
| `Epinephrine18g5ccSyringe` | `epinephrine_18g_5cc_syringe` | 에피네프린이 든 18g 5cc 주사기 |
| `Epinephrine18g20ccSyringe` | `epinephrine_18g_20cc_syringe` | 에피네프린이 든 18g 20cc 주사기 |
| `Epinephrine18g50ccSyringe` | `epinephrine_18g_50cc_syringe` | 에피네프린이 든 18g 50cc 주사기 |
| `Epinephrine20g5ccSyringe` | `epinephrine_20g_5cc_syringe` | 에피네프린이 든 20g 5cc 주사기 |
| `Epinephrine20g20ccSyringe` | `epinephrine_20g_20cc_syringe` | 에피네프린이 든 20g 20cc 주사기 |
| `Epinephrine20g50ccSyringe` | `epinephrine_20g_50cc_syringe` | 에피네프린이 든 20g 50cc 주사기 |
| `Epinephrine22g5ccSyringe` | `epinephrine_22g_5cc_syringe` | 에피네프린이 든 22g 5cc 주사기 |
| `Epinephrine22g20ccSyringe` | `epinephrine_22g_20cc_syringe` | 에피네프린이 든 22g 20cc 주사기 |
| `Epinephrine22g50ccSyringe` | `epinephrine_22g_50cc_syringe` | 에피네프린이 든 22g 50cc 주사기 |
| `Epinephrine24g5ccSyringe` | `epinephrine_24g_5cc_syringe` | 에피네프린이 든 24g 5cc 주사기 |
| `Epinephrine24g20ccSyringe` | `epinephrine_24g_20cc_syringe` | 에피네프린이 든 24g 20cc 주사기 |
| `Epinephrine24g50ccSyringe` | `epinephrine_24g_50cc_syringe` | 에피네프린이 든 24g 50cc 주사기 |

**노르에피네프린 조합 주사기:** (에피네프린과 동일 구조, `norepinephrine_` 접두사)

| 클래스 | Identifier |
|---|---|
| `Norepinephrine5ccSyringe` | `norepinephrine_5cc_syringe` |
| `Norepinephrine20ccSyringe` | `norepinephrine_20cc_syringe` |
| `Norepinephrine50ccSyringe` | `norepinephrine_50cc_syringe` |
| `Norepinephrine{16/18/20/22/24}g{5/20/50}ccSyringe` | `norepinephrine_{gauge}g_{volume}cc_syringe` |

**생리식염수 조합 주사기:** (에피네프린과 동일 구조, `normal_saline_` 접두사)

| 클래스 | Identifier |
|---|---|
| `NormalSaline5ccSyringe` | `normal_saline_5cc_syringe` |
| `NormalSaline20ccSyringe` | `normal_saline_20cc_syringe` |
| `NormalSaline50ccSyringe` | `normal_saline_50cc_syringe` |
| `NormalSaline{16/18/20/22/24}g{5/20/50}ccSyringe` | `normal_saline_{gauge}g_{volume}cc_syringe` |

### 5.10 상처 처치 (Wound Care)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Gauze` | `gauze` | 면균거즈 |
| `ElasticBand` | `elasticband` | 압박붕대 |
| `Plaster` | `plaster` | 플라스터 |

### 5.11 일반 처치 도구 (General Tools)

| 클래스 | Identifier | DisplayName |
|---|---|---|
| `Penlight` | `penlight` | 펜라이트 |
| `Scissors` | `scissors` | 가위 |
| `Swab` | `swab` | 소독솜 |
| `Gloves` | `gloves` | 면균 장갑 |
| `Paper` | `paper` | 종이 |
| `ChecklistPaper` | `checklist_paper` | 종이 |

---

## 6. 관련 문서

- [MultiplayerInfrastructure.Item.Item.md](./MultiplayerInfrastructure.Item.Item.md) — Item 기반 클래스 API
- [MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md](./MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md) — 아이템 조합 레시피
- [working-guide/item-authoring.md](../working-guide/item-authoring.md) — 아이템 제작 가이드
