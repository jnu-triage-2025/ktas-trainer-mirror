# <a id="MultiplayerInfrastructure_Scenario_ScenarioConcurrencyConflictPolicyExtensions"></a> Class ScenarioConcurrencyConflictPolicyExtensions

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class ScenarioConcurrencyConflictPolicyExtensions
```

#### Inheritance

object ← 
[ScenarioConcurrencyConflictPolicyExtensions](MultiplayerInfrastructure.Scenario.ScenarioConcurrencyConflictPolicyExtensions.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioConcurrencyConflictPolicyExtensions_TryParse_System_String_MultiplayerInfrastructure_Scenario_ScenarioConcurrencyConflictPolicy__System_String__"></a> TryParse\(string, out ScenarioConcurrencyConflictPolicy, out string\)

커맨드/직렬화 문자열을 정책 값으로 파싱한다(숫자 별칭 0/1/2 허용).
실패 시 false 를 반환하고 <code class="paramref">error</code> 에 사유를 담는다.

```csharp
public static bool TryParse(string raw, out ScenarioConcurrencyConflictPolicy value, out string error)
```

#### Parameters

`raw` string

`value` [ScenarioConcurrencyConflictPolicy](MultiplayerInfrastructure.Scenario.ScenarioConcurrencyConflictPolicy.md)

`error` string

#### Returns

 bool

