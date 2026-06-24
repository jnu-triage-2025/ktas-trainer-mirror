# API 레퍼런스: PlasmaSolution1000ml (plasma_solution_1000ml)

> 네임스페이스: TriageTrainer.ItemDefinitions
> 파일 위치: Assets/Modules/TriageTrainer/Scripts/Items/Definitions/PlasmaSolution1000ml.cs

## 0. 개요

`PlasmaSolution1000ml` 은 재난 시뮬레이션 시나리오에서 사용하는 혈장 대용 수액(플라즈마 솔루션 1L)
아이템 정의다. `MedicalItem` 을 상속하는 단순 데이터 정의 클래스이며, 다른 의료 수액 아이템
(`NormalSaline1000ml` 등)과 동일한 구조를 따른다.

신설 배경: 환자 A 시나리오의 의사 NPC 오더("수액은 생리식염수와 플라즈마 솔루션 달겠습니다")와
대응하는 인터랙션 게이트 `sig.click_plasma_solution_1000ml` 를 충족할 아이템이 기존 정의에
존재하지 않아 추가되었다(`interaction-signal-integration-spec.md` TODO-CONTENT-1).

## 1. 정의

| 항목 | 값 |
|---|---|
| 클래스 | `TriageTrainer.ItemDefinitions.PlasmaSolution1000ml` |
| 기반 클래스 | `TriageTrainer.ItemDefinitions.MedicalItem` (← `MultiplayerInfrastructure.ItemSystem.Item`) |
| `Identifier` | `plasma_solution_1000ml` |
| `DisplayName` | `플라즈마 솔루션 1L` |
| `Description` | `혈장 대용 수액(플라즈마 솔루션) 1L.` |
| 스택 | `MedicalItem` 기본값(IsStackable=true, MaxStackCount=64) |
| 핸들러 | `MedicalItem` 기본(OnUse/OnAttack=Success no-op) + `OnGet` 에서 인터랙션 신호 발생 |

## 2. 등록

`RegisteringMultiplayerInfrastructureSupport.Item.cs` 의 `RegisterAllItems()` 에 다음이 추가되어 있다.

```csharp
Registry.RegisterItemDefinition<PlasmaSolution1000ml>(PlasmaSolution1000ml.Identifier);
```

## 3. 시나리오 게이팅 연동

`MedicalItem.OnGet` override 에 의해, 이 아이템을 획득(인벤토리 추가)하면 다음 신호가 올라간다.

- `sig.plasma_solution_1000ml`
- `sig.click_plasma_solution_1000ml`

시나리오의 `Validator(condition=RegistryContains, registryType=RuntimeState,
registryIdentifier="sig.click_plasma_solution_1000ml")` 게이트가 이로써 통과된다.
(환자 A: `click_18g_and_click_ns1_and_click_ps1`, `click_ps1_and_click_blood` 등 분해된 개별 룰)

## 4. 남은 리소스 작업

`ValidateItemResources()` 기준 다음 리소스가 있어야 경고가 사라진다(없어도 로직은 동작).

- 아이콘 스프라이트: `Resources/{ItemTexturesPath}/plasma_solution_1000ml`
- 3D 모델 프리팹: `Resources/Models/Items/plasma_solution_1000ml`

리소스 미배치 시 플레이모드 Console 에 누락 경고가 출력되며, 게임플레이/게이팅 자체에는 영향이 없다.
