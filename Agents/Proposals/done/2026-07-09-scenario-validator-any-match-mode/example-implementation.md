# 예시 구현: Validator OR(Any) 매칭 모드

> 본 문서는 [Feature Proposal](./Feature%20Proposal%20-%20Scenario%20Validator%20Any%20Match%20Mode.md)
> 의 예시 구현이다. `MultiplayerInfrastructure` 실제 반영은 인간 작업자 승인 후 진행한다.
> 모든 변경은 하위호환(필드 미지정 시 기존 `All`/AND 동작 유지)을 지킨다.
> 참조 라인 번호는 2026-07-09 시점 기준이며, 실제 반영 시 최신 파일로 재확인한다.

---

## 1. 모델 — `Scripts/Scenario/Models/ScenarioGraphNodes/ScenarioValidatorNode.cs`

### 1-1. 신규 enum (기존 `ScenarioValidatorRuleCondition`(line 39~42) 아래에 추가)

```csharp
  /// <summary>
  /// RegistryContains root condition 의 여러 ValidationRules 결합 방식.
  /// (ScenarioPlayerTagMatchMode 를 미러링)
  /// </summary>
  public enum ScenarioValidatorMatchMode
  {
    /// <summary>모든 규칙을 충족해야 통과(AND). 기본값 — 기존 동작.</summary>
    All,

    /// <summary>하나 이상의 규칙을 충족하면 통과(OR).</summary>
    Any
  }
```

### 1-2. `ScenarioValidatorRootCondition`(line 53~61) 에 필드 추가

```csharp
  [Serializable]
  public sealed class ScenarioValidatorRootCondition
  {
    public ScenarioValidatorCondition Condition { get; set; }
    public int TargetCount { get; set; }
    public string PlayerTag { get; set; }
    public ScenarioValidatorPlayerScope PlayerScope { get; set; } = ScenarioValidatorPlayerScope.Any;
    public IReadOnlyList<ScenarioValidatorRule> ValidationRules { get; set; } = new List<ScenarioValidatorRule>();

    // 신규(옵션). 기본 All → 기존 AND 동작 유지(하위호환).
    // RegistryContains 조건의 ValidationRules 결합 방식에만 적용된다.
    public ScenarioValidatorMatchMode MatchMode { get; set; } = ScenarioValidatorMatchMode.All;
  }
```

---

## 2. DTO — `Scripts/Scenario/Models/ScenarioGraphNodesDTO/ScenarioValidatorNodeDTO.cs`

`ScenarioValidatorRootConditionDTO`(line 7~23) 에 필드 추가:

```csharp
    // 신규(옵션). null/미지정 시 All(기존). 값이 All 이면 직렬화에서 생략(아래 로더 참조).
    [JsonPropertyName("matchMode")]
    public string MatchMode { get; set; }
```

---

## 3. 로더 — `Scripts/Scenario/SerializeSupport/ScenarioGraphLoader.cs`

### 3-1. DTO→모델: `ParseValidatorRootConditions`(line 1402~1428) 에서 `MatchMode` 세팅

```csharp
      rootCondition.MatchMode = ParseValidatorMatchMode(dto.MatchMode); // 신규
```

### 3-2. 모델→DTO: `ConvertValidatorRootConditionsToDTO`(line 1430~1456)

```csharp
      // All(기본)이면 생략하여 기존 JSON 과 diff 최소화(onWaitTimeout 관례와 동일)
      dto.MatchMode = rootCondition.MatchMode == ScenarioValidatorMatchMode.All
        ? null
        : rootCondition.MatchMode.ToString();
```

### 3-3. 신규 파서(예: `ParsePlayerTagMatchMode`(line 1600~1612) 옆에 추가)

```csharp
    private static ScenarioValidatorMatchMode ParseValidatorMatchMode(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return ScenarioValidatorMatchMode.All; // 미지정 → 기존 동작

      return Enum.TryParse<ScenarioValidatorMatchMode>(raw, ignoreCase: true, out var mode)
        ? mode
        : ScenarioValidatorMatchMode.All;
    }
```

---

## 4. 런타임 — `Scripts/Scenario/ScenarioController.cs`

`EvaluateValidatorRootCondition` 의 `RegistryContains` 케이스(line 3633~3677) 를 아래로 교체.
`All`(기본) 경로는 기존과 완전히 동일하고, `Any` 경로만 신규다.

```csharp
        case ScenarioValidatorCondition.RegistryContains:
        {
          var rules = rootCondition.ValidationRules;
          if (rules == null || rules.Count == 0)
          {
            failureReason = "validationRules is empty for RegistryContains condition.";
            return false;
          }

          bool anyMode = rootCondition.MatchMode == ScenarioValidatorMatchMode.Any;
          bool anyMatched = false;
          var anyModeFailures = anyMode ? new System.Collections.Generic.List<string>() : null;

          for (int i = 0; i < rules.Count; i++)
          {
            var rule = rules[i];
            if (rule == null)
            {
              continue;
            }

            // 오설정 판정(공통). All 모드에서는 즉시 실패, Any 모드에서는 "미충족"으로 취급.
            string misconfig = null;
            if (rule.Type != ScenarioValidatorRuleType.Registry)
              misconfig = $"rule[{i}] has unsupported type '{rule.Type}'.";
            else if (rule.Condition != ScenarioValidatorRuleCondition.Contains)
              misconfig = $"rule[{i}] has unsupported condition '{rule.Condition}'.";

            string ruleIdentifier = rule.RegistryIdentifier?.Trim();
            if (misconfig == null && string.IsNullOrWhiteSpace(ruleIdentifier))
              misconfig = $"rule[{i}] registryIdentifier is null or empty.";

            if (misconfig != null)
            {
              if (anyMode) { anyModeFailures.Add(misconfig); continue; }
              failureReason = misconfig;
              return false;
            }

            bool matched = Registry.Registry.Contains(rule.RegistryType, ruleIdentifier);

            if (anyMode)
            {
              if (matched) { anyMatched = true; break; } // OR: 첫 충족에서 통과
              anyModeFailures.Add(
                $"rule[{i}] identifier '{ruleIdentifier}' is not registered in {rule.RegistryType}.");
            }
            else
            {
              if (!matched) // AND(기존)
              {
                failureReason =
                  $"rule[{i}] identifier '{ruleIdentifier}' is not registered in {rule.RegistryType}.";
                return false;
              }
            }
          }

          if (anyMode && !anyMatched)
          {
            failureReason =
              $"no rule matched (Any mode). details: {string.Join(" | ", anyModeFailures)}";
            return false;
          }

          return true;
        }
```

---

## 5. 인스펙터 — `Editor/Scenario/ScenarioGraphEditor/ScenarioInspectorView.cs`

`DrawValidatorRegistryRules`(line 439~499) 의 규칙 리스트 위에 모드 선택 추가:

```csharp
      // 규칙 리스트를 그리기 전, 결합 방식 선택
      rootCondition.MatchMode = (ScenarioValidatorMatchMode)EditorGUILayout.EnumPopup(
        new GUIContent("Match Mode", "All=모든 규칙 충족(AND), Any=하나 이상 충족(OR)"),
        rootCondition.MatchMode);
```

---

## 6. (선택) 보조 에디터 반영

- `ScenarioNodeView.cs`(line 819 부근 요약 문자열): `RegistryContains` 요약에 `[Any]`/`[All]` 표기.
- `ScenarioGraphDiagnostics.cs`(line 160~173): `Any` 모드에서 규칙 1개만 있으면 "OR 무의미" 정보 진단(선택).
- `ScenarioNodeFactory.cs`(line 25~32): 신규 노드 기본값은 `All`(변경 불필요).

---

## 7. 회귀 방지 체크

- [ ] `matchMode` 미지정 기존 JSON 라운드트립(로드→저장) 시 `matchMode` 필드가 생기지 않음(생략).
- [ ] 기존 리그레션 JSON 통과(동작 변화 0).
- [ ] `matchMode:"Any"` root condition: 규칙 1개 충족→통과 / 전부 미충족→실패.
- [ ] `All` root condition: 기존과 동일(전부 충족해야 통과).
