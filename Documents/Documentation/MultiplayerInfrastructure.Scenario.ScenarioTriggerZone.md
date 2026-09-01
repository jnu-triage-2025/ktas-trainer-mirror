# <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone"></a> Class ScenarioTriggerZone

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

트리거 존 진입 시 자동으로 시나리오를 시작합니다.
신호 계열 존(그래프 미지정)은 재생 중인 시나리오가 있을 때만 감지/발신합니다
(<xref href="MultiplayerInfrastructure.Scenario.ScenarioController.HasActiveScenario" data-throw-if-not-resolved="false"></xref> 기준).

```csharp
public class ScenarioTriggerZone : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ScenarioTriggerZone](MultiplayerInfrastructure.Scenario.ScenarioTriggerZone.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_HasTriggered"></a> HasTriggered

```csharp
public bool HasTriggered { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_ForceTrigger"></a> ForceTrigger\(\)

```csharp
[ContextMenu("Scenario Trigger/Force Trigger")]
public void ForceTrigger()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_ResetAllForNewScenarioRun"></a> ResetAllForNewScenarioRun\(\)

새 시나리오 실행을 위해 모든 활성 존의 중복 방지 상태를 되돌린다.

<p>
존의 발신 억제 상태는 한 번의 시나리오 실행 안에서만 의미가 있다. 시나리오가 다시
시작되면 신호 레지스트리는 비워지지만 존의 상태는 그대로 남아, 같은 엔티티나 플레이어가
다시 진입해도 신호를 올리지 않는다. 그 신호를 기다리는 게이트와 카운터는 영구히 막힌다.
</p>

```csharp
public static void ResetAllForNewScenarioRun()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_ResetTrigger"></a> ResetTrigger\(\)

```csharp
[ContextMenu("Scenario Trigger/Reset Trigger")]
public void ResetTrigger()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_SetScenario_UnityEngine_TextAsset_System_String_"></a> SetScenario\(TextAsset, string\)

```csharp
public void SetScenario(TextAsset scenarioJson, string startNodeIdentifier = null)
```

#### Parameters

`scenarioJson` TextAsset

`startNodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_SetScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_String_"></a> SetScenario\(ScenarioGraph, string\)

```csharp
public void SetScenario(ScenarioGraph graph, string startNodeIdentifier = null)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`startNodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriggerZone_OnScenarioRequested"></a> OnScenarioRequested

```csharp
public static event Action<ScenarioGraph, string, int?> OnScenarioRequested
```

#### Event Type

 Action<[ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md), string, int?\>

