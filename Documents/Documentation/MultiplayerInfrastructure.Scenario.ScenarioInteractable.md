# <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable"></a> Class ScenarioInteractable

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

상호작용 시 시나리오를 시작하는 컴포넌트입니다.
NPC 또는 오브젝트에 부착합니다.

```csharp
public class ScenarioInteractable : NetworkBehaviour, IInteractable, IInteract
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[ScenarioInteractable](MultiplayerInfrastructure.Scenario.ScenarioInteractable.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_SetScenario_UnityEngine_TextAsset_System_String_"></a> SetScenario\(TextAsset, string\)

```csharp
public void SetScenario(TextAsset scenarioJson, string startNodeIdentifier = null)
```

#### Parameters

`scenarioJson` TextAsset

`startNodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_SetScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_String_"></a> SetScenario\(ScenarioGraph, string\)

```csharp
public void SetScenario(ScenarioGraph graph, string startNodeIdentifier = null)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`startNodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractable_OnScenarioRequested"></a> OnScenarioRequested

```csharp
public static event Action<ScenarioGraph, string, int?> OnScenarioRequested
```

#### Event Type

 Action<[ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md), string, int?\>

