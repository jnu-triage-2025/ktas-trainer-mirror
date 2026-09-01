# <a id="MultiplayerInfrastructure_Scenario_ScenarioConcurrencyConflictPolicy"></a> Enum ScenarioConcurrencyConflictPolicy

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

두 개 이상의 시나리오 흐름이 동시에 대화창 계열 UI(Dialogue/Choice/Quiz)를
점유하려 할 때의 처리 정책.

```csharp
public enum ScenarioConcurrencyConflictPolicy
```

## Fields

`Cancel = 1` 

나중에 대화창을 점유하려 한 흐름을 취소한다(먼저 점유한 흐름 보존).



`Panic = 2` 

진행 중인 모든 시나리오를 중단한다(EndScenario).



`Warn = 0` 

경고만 남기고 그대로 진행한다(undefined behavior 허용). 기본값이자 기존 동작.



