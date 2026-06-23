# 설계 명세 — TODO-SPEC-2: Validator 도메인 인터랙션 검증

## 문제
현재 `ScenarioValidatorCondition`(`ScenarioValidatorNode.cs:7`)은 다음만 지원한다.
`PlayerCountEqual/NotEqual/LessThan/LessThanOrEqual/GreaterThan/GreaterThanOrEqual`, `RegistryContains`, `PlayerAssignedTag`.
`ScenarioValidatorRuleType`은 `Registry`, `RuleCondition`은 `Contains`만 있다.
따라서 "학습자가 거즈를 적용했다", "활력징후 측정도구를 클릭했다", "경동맥을 촉지했다" 같은
**도메인 인터랙션 완료** 여부를 시나리오 데이터로 검증할 수 없다.
변환에서는 이를 `todo.validate.*` InvokeEvent 스텁(약 84개)으로 우회했고, 핸들러 미등록 시 무동작 스킵된다.

## 제안: 인터랙션 완료 신호(Signal) + Validator 룰

### A. 런타임 신호 저장소
- `RegistryType.RuntimeState`(이미 존재)에 "완료 신호"를 key 단위로 누적/조회.
- 게임플레이 코드(TriageTrainer)가 인터랙션 완료 시 `RaiseSignal(signalId)` 호출.
- 신호는 시나리오 실행 컨텍스트 단위로 스코프(시작 시 초기화) — 사이클 반복 시 재사용 가능.

### B. Validator 확장(전부 optional, 하위호환)
```
enum ScenarioValidatorCondition { ... , SignalRaised }      // 신규 멤버 추가
enum ScenarioValidatorRuleType  { Registry, InteractionSignal }  // 신규 멤버 추가
```
- `RootCondition.Condition = SignalRaised`, `targetCount = N`(요구 신호 개수),
  `validationRules[].type = InteractionSignal`, `registryIdentifier = <signalId>`.
- 복수 조건(예: 후두경 블레이드+손잡이)은 `validationRules`에 여러 룰 또는 `targetCount=2`로 표현.

### C. 대안(권장 1차): 기존 Interaction 노드 게이트 정식화
- 이미 존재하는 `ScenarioInteractionNode`(`actorScope/targetIdentifier/requiredItemIdentifier/interactionType/completionConditionIdentifier`)
  실행기를 게임플레이 인터랙션 완료와 연결.
- 변환 산출물의 `todo.validate.<x>` InvokeEvent를 다음으로 점진 치환:
  ```json
  { "nodeType": "Interaction", "actorScope": "Player",
    "targetIdentifier": "vital_set", "interactionType": "Use",
    "completionConditionIdentifier": "ic.vital_set_used", "nextIdentifier": "..." }
  ```

## 예시 변환 (환자 A V011 = click vital set)
- 현재(JSON): `InvokeEvent eventIdentifier="todo.validate.click_vital_set"`
- 변경안:
  ```json
  { "nodeType": "Validator", "identifier": "V011",
    "rootConditions": [ { "condition": "SignalRaised", "targetCount": 1,
      "validationRules": [ { "type": "InteractionSignal", "condition": "Contains",
        "registryType": "RuntimeState", "registryIdentifier": "sig.click_vital_set" } ] } ],
    "onFailure": "Ignore", "nextIdentifier": "N005_1" }
  ```

## 직렬화/검증
- DTO: `ScenarioValidatorNodeDTO`/`ScenarioValidatorRuleDTO`에 신규 enum 문자열 허용(STJ case-insensitive).
- `scenario.schema.json`: `ScenarioValidatorRootCondition.condition` enum에 `SignalRaised`,
  `ScenarioValidatorRule.type` enum에 `InteractionSignal` 추가(기존 값 유지 → 하위호환).

## 테스트
- 신호 미발생 시 Validator 대기/실패 분기, 발생 시 통과.
- `targetCount` 부분 충족 → 미통과.
- 기존 `PlayerCount*`/`RegistryContains` 그래프 회귀 없음.

## 영향
- 변환 산출물의 `todo.validate.*` 84개를 데이터로 표현 가능.
- 게임플레이 측은 "완료 시 RaiseSignal 1줄"만 추가하면 되어 핸들러 보일러플레이트 감소.
