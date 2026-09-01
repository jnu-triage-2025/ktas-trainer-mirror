# <a id="TriageTrainer_Scenario_TriageScenarioEventBootstrap"></a> Class TriageScenarioEventBootstrap

Namespace: [TriageTrainer.Scenario](TriageTrainer.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

Registers TriageTrainer scenario event handlers without modifying base infrastructure.

NOTE:
- Current handlers are safe placeholders for MVP wiring.
- Replace each coroutine body with real presentation/interaction logic incrementally.

```csharp
public class TriageScenarioEventBootstrap : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[TriageScenarioEventBootstrap](TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)

## Fields

### <a id="TriageTrainer_Scenario_TriageScenarioEventBootstrap_PreinstalledOxygenWarning"></a> PreinstalledOxygenWarning

```csharp
public const string PreinstalledOxygenWarning = "이미 산소장치가 설치되어있다. 이것을 해제하고 새로 설치하자."
```

#### Field Value

 string

