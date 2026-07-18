# Feature Proposal: Scenario Validator OR(Any) 매칭 모드

- 작성일: 2026-07-09
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`
- 관련 명세: `Documents/requirements/content-definitions/scenario/crafting-recipes.md` §확정 요청 (*1)
- 관련 명세: `Documents/requirements/content-definitions/scenario/interaction-signal-integration-spec.md`
- 선행/유사 패턴: `ScenarioPlayerTagMatchMode`(Parallel 브랜치의 `RequiredPlayerTagsMatchMode`)

> 상태: **완료(done)**. `matchMode: "Any"`를 모델·JSON 스키마·로더·런타임·에디터에 반영했으며,
> `All` 기본값의 하위호환 직렬화와 `Any`/`All` 런타임 판정을 Unity 에디터 테스트로 검증했다.
> 콘텐츠 레벨 대응(B, `push_*` 사용 시점 시그널 게이팅)은 계속 우선 권장한다.

### 개요

`Validator OR(Any) 매칭 모드` 기능은 시나리오 `Validator` 노드의 `RegistryContains` 조건에서
여러 검증 규칙(`ValidationRules`)을 **AND(모두 충족)뿐 아니라 OR(하나 이상 충족)** 로도 결합할 수
있게 하는 확장이다. 이 기능은 각 root condition 에 선택적 `MatchMode`(`All`/`Any`) 를 부여하여,
"여러 후보 중 어느 하나라도 만족하면 통과" 라는 게이트를 시나리오 데이터만으로 표현할 수 있게 한다.

- 요약: `ScenarioValidatorRootCondition` 에 `MatchMode { All, Any }`(기본 `All`) 를 추가하고,
  `RegistryContains` 런타임 평가 루프를 `Any` 일 때 첫 충족 규칙에서 통과하도록 확장한다.
- 의도/목표: 동일 의미의 여러 산출물(예: 에피네프린 주사기 완제품 18종) 중 **하나라도 준비/사용**
  되면 통과해야 하는 게이트를, Validator 시스템 레벨에서 1급으로 지원한다.
- 주요 맥락: 조합·판정은 노드가 아니라 시스템(`ItemCombineRecipeRegistry`)과 개별 시그널
  (`sig.click_<id>`, 사용 시점 `push_*` 등)로 처리된다. 시그널은 **아이템 식별자 단위**라 변형별
  아이템이 서로 다른 시그널을 올린다. 현재 `RegistryContains` 는 규칙을 전부 AND 로만 결합하므로
  "여러 변형 중 하나" 게이트를 표현할 수 없다.
- 기술적 제약: 하위호환 필수. 필드 미지정(JSON 에 `matchMode` 없음) 시 기존 `All`(AND) 동작을
  100% 유지해야 한다. 기존 변환 시나리오(104개 Validator)에 동작 변화 0 이어야 한다.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자/교육 설계자**로서, "에피네프린 주사기를 사용" 같은 게이트가
카테터 없는 완제품(`epinephrine_5cc_syringe`/`_20cc_syringe`/`_50cc_syringe` 3종)과 게이지 포함
완제품(`epinephrine_16g_5cc_syringe` … `epinephrine_24g_50cc_syringe` 15종) = 총 18종 중
**어느 하나라도** 충족되면 통과하기를 원한다. 왜냐하면 학습자가 어떤 게이지/용량 조합을 준비했든
"에피네프린 주사기 준비"라는 술기 목표는 동일하게 달성한 것이기 때문이다.

그러나 현재 `Validator.RegistryContains` 는 `ValidationRules` 를 전부 AND 로만 결합한다
(근거: `ScenarioController.cs` `EvaluateValidatorRootCondition` 의 `RegistryContains` 케이스,
루프가 규칙 하나라도 미충족이면 즉시 `return false`, 전부 충족해야 `return true`). 따라서
18종을 한 규칙 집합에 나열하면 "18종을 모두 준비" 라는 불가능한 게이트가 되어버린다.

### 사용자 경험 목표

- 운영자: root condition 마다 `MatchMode = Any` 를 선택하면, 나열한 규칙 중 하나만 충족돼도 통과.
  기본값 `All` 은 기존과 동일.
- 학습자: 어떤 유효 변형을 준비/사용하든 동일하게 진행한다.
- 감독/평가자: OR 게이트 의도가 데이터에 명시되어 시나리오 리뷰·진단에서 드러난다.

### 제안

#### 변경 요약 (5개 접점, `ScenarioPlayerTagMatchMode` 패턴을 그대로 미러링)

| 접점 | 파일 | 변경 |
|---|---|---|
| 신규 enum | `Scripts/Scenario/Models/ScenarioGraphNodes/ScenarioValidatorNode.cs` | `ScenarioValidatorMatchMode { All, Any }` 추가 |
| 모델 필드 | 〃 (`ScenarioValidatorRootCondition`) | `MatchMode` 프로퍼티 추가(기본 `All`) |
| DTO 필드 | `Scripts/Scenario/Models/ScenarioGraphNodesDTO/ScenarioValidatorNodeDTO.cs` | `matchMode`(nullable string) 추가 |
| 로더 매핑 | `Scripts/Scenario/SerializeSupport/ScenarioGraphLoader.cs` | 파싱/역직렬화에 `MatchMode` 반영, `All` 이면 생략 |
| 런타임 평가 | `Scripts/Scenario/ScenarioController.cs` | `RegistryContains` 루프를 `Any` 분기로 확장 |
| (인스펙터) | `Editor/Scenario/ScenarioGraphEditor/ScenarioInspectorView.cs` | `Match Mode` EnumPopup 추가 |

#### 예시 구현 (참조)

본 제안 채택 시 적용할 예시 코드는 같은 폴더의 [`example-implementation.md`](./example-implementation.md)
에 파일별 diff 형태로 정리했다. 핵심은 다음 런타임 확장이다(의사 코드):

```csharp
// ScenarioController.cs — RegistryContains 케이스
bool anyMode = rootCondition.MatchMode == ScenarioValidatorMatchMode.Any;
bool anyMatched = false;

for (int i = 0; i < rules.Count; i++)
{
  // (기존 유효성 검사: Type/Condition/Identifier 는 여전히 오설정이면 실패)
  bool matched = Registry.Registry.Contains(rule.RegistryType, ruleIdentifier);

  if (anyMode)
  {
    if (matched) { anyMatched = true; break; }   // Any: 첫 충족에서 통과
  }
  else
  {
    if (!matched) { failureReason = ...; return false; }  // All(기존): 미충족이면 실패
  }
}

return anyMode ? anyMatched : true;  // Any: 하나라도 충족해야 통과 / All: 전부 통과
```

> 유의: `Type != Registry`, `Condition != Contains`, 빈 `RegistryIdentifier` 같은 **오설정**은
> `Any` 모드에서도 그 규칙을 "충족 안 됨"으로 취급하되(스킵), 규칙이 하나도 충족되지 않으면
> 실패 사유에 오설정 목록을 포함해 진단성을 유지한다.

### 자세한 달성 목표

1. `matchMode` 미지정 JSON(기존 게이트)은 **동작 변화 0**(`All` = 현재 AND 동작).
2. `matchMode: "Any"` 지정 시, 나열한 `sig.*` 규칙 중 하나라도 RuntimeState 에 있으면 통과.
3. 예: 한 root condition 에 18종 `sig.push_epinephrine_*` 를 나열하고 `Any` 로 두면,
   어떤 변형을 사용해도 통과.
4. 진단(`ScenarioGraphDiagnostics`)·검색(`ScenarioNodeSearcher`)·노드 요약(`ScenarioNodeView`)에
   모드가 드러나도록 보조 표기 추가(선택).

### 문서화

- `interaction-signal-integration-spec.md` 에 Validator `matchMode` 사용법·OR 게이트 예시 추가.
- 변환 규칙 문서(있다면 `json-conversion-rules.md`)의 Validator 섹션에 신규 필드 표기.
- 채택 시 `Documents/requirements` 색인/링크 검증(`Tools/validate-documentation-links.sh`) 수행.

### 가용성과 테스트

- 위험: `MultiplayerInfrastructure` 변경. 하위호환을 깨면 기존 시나리오/리그레션 JSON 에 영향.
  → 기본값 `All` 고정, `matchMode` 생략 시 기존 경로와 동일하게 위험 최소화.
- 테스트:
  - 기존 리그레션 JSON(`validating_full.json` 등)이 동작 변화 없이 통과.
  - 신규: `matchMode:"Any"` root condition 에서 (a) 규칙 중 1개만 충족→통과, (b) 전부 미충족→실패,
    (c) 오설정 규칙 혼재 시 진단 사유 노출 검증.
  - `/scenario signal <cond>` 커맨드로 OR 게이트 통과/대기 수동 검증.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 동일 의미의 다중 산출물 게이트를 시나리오 데이터만으로 OR 표현 가능.
- 수용 기준:
  1. `matchMode` 미지정 게이트의 런타임 동작이 변경 전과 동일(회귀 0).
  2. `matchMode:"Any"` 게이트가 나열 규칙 중 하나만 충족돼도 통과.
  3. `matchMode:"Any"` 게이트가 나열 규칙 전부 미충족일 때만 실패/대기.

### 링크, 참고사항

- AND 결합 근거: `ScenarioController.cs` `EvaluateValidatorRootCondition` 의 `RegistryContains` 케이스.
- 미러링 대상 패턴: `ScenarioPlayerTagMatchMode`(`ScenarioParallelBranch.RequiredPlayerTagsMatchMode`),
  런타임 판정 `ScenarioController` 의 `== ScenarioPlayerTagMatchMode.Any`, 로더 `ParsePlayerTagMatchMode`.
- 콘텐츠 레벨 우선 대응(B): `crafting-recipes.md` §확정 요청 (*1), `interaction-signal-integration-spec.md`.
