# <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable"></a> Class ScenarioSelectionInteractable

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 선택지를 IInteract로 래핑하는 클래스.
InteractableObjectHintUIController에서 표시할 수 있도록 합니다.

```csharp
public class ScenarioSelectionInteractable : IInteract
```

#### Inheritance

object ← 
[ScenarioSelectionInteractable](MultiplayerInfrastructure.UI.ScenarioSelectionInteractable.md)

#### Implements

[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable__ctor_MultiplayerInfrastructure_Scenario_ScenarioChoiceOption_System_Int32_System_Action_MultiplayerInfrastructure_Scenario_ScenarioChoiceOption_System_Int32__"></a> ScenarioSelectionInteractable\(ScenarioChoiceOption, int, Action<ScenarioChoiceOption, int\>\)

```csharp
public ScenarioSelectionInteractable(ScenarioChoiceOption option, int index, Action<ScenarioChoiceOption, int> onInteract)
```

#### Parameters

`option` [ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md)

`index` int

`onInteract` Action<[ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md), int\>

## Properties

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_Index"></a> Index

```csharp
public int Index { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_Option"></a> Option

```csharp
public ScenarioChoiceOption Option { get; }
```

#### Property Value

 [ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_ScenarioSelectionInteractable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

