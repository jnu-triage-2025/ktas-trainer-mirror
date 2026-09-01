# <a id="TriageTrainer_Scenario_ScenarioActionInteractable"></a> Class ScenarioActionInteractable

Namespace: [TriageTrainer.Scenario](TriageTrainer.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오에서 요구하는 물체/부위 상호작용을 위한 경량 Interactable 입니다.
상호작용이 확정되면 완료 신호를 올리고, 필요하면 연결된 시각 오브젝트를 표시 또는 숨깁니다.

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class ScenarioActionInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional, IInteractToggleable, IInteractDisplayIcons, IInteractDisplayPriority, IQuestPresentationTarget
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ScenarioActionInteractable](TriageTrainer.Scenario.ScenarioActionInteractable.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md), 
[IInteractToggleable](MultiplayerInfrastructure.InteractableEntity.IInteractToggleable.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IInteractDisplayPriority](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayPriority.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md)

## Properties

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_CompletionSignal"></a> CompletionSignal

```csharp
public string CompletionSignal { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_DisplayIcons"></a> DisplayIcons

```csharp
public IReadOnlyList<Sprite> DisplayIcons { get; }
```

#### Property Value

 IReadOnlyList<Sprite\>

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_DisplayPriority"></a> DisplayPriority

```csharp
public int DisplayPriority { get; }
```

#### Property Value

 int

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_InteractionIdentifier"></a> InteractionIdentifier

```csharp
public string InteractionIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_RequiredPlayerTag"></a> RequiredPlayerTag

```csharp
public string RequiredPlayerTag { get; }
```

#### Property Value

 string

## Methods

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_ResetCompletionForScenario"></a> ResetCompletionForScenario\(\)

시나리오 수동 진입 준비 체인이 단계를 되돌릴 때 "이미 수행함" 표시만 지운다.
<xref href="TriageTrainer.Scenario.ScenarioActionInteractable._consumeOnce" data-throw-if-not-resolved="false"></xref> 상호작용은 한 번 수행하면 다시 노출되지 않으므로, 같은 세션에서
이전 단계를 다시 재생하면 그 단계의 상호작용을 수행할 수 없게 된다.
<xref href="TriageTrainer.Scenario.ScenarioActionInteractable.SetEnabled(System.Boolean)" data-throw-if-not-resolved="false"></xref> 와 달리 노출 기준값(<xref href="TriageTrainer.Scenario.ScenarioActionInteractable._enabled" data-throw-if-not-resolved="false"></xref>)은 건드리지 않는다.
노출 판정을 플래그 풀에 맡긴 시나리오에서 이 값을 함께 켜면 단계 밖 상호작용이 열린다.

```csharp
public void ResetCompletionForScenario()
```

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_SetEnabled_System_Boolean_"></a> SetEnabled\(bool\)

```csharp
public void SetEnabled(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="TriageTrainer_Scenario_ScenarioActionInteractable_OnInteractionCompleted"></a> OnInteractionCompleted

```csharp
public static event Action<ScenarioActionInteractable, PlayerController> OnInteractionCompleted
```

#### Event Type

 Action<[ScenarioActionInteractable](TriageTrainer.Scenario.ScenarioActionInteractable.md), [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)\>

