# <a id="TriageTrainer_Scenario_TutorialDecoyInteractable"></a> Class TutorialDecoyInteractable

Namespace: [TriageTrainer.Scenario](TriageTrainer.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

튜토리얼의 오답 배송 물품에 사용하는 로컬 안내 상호작용.
실행 중인 메인 시나리오를 중단하지 않고 비점유 다이얼로그만 표시한다.

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class TutorialDecoyInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[TutorialDecoyInteractable](TriageTrainer.Scenario.TutorialDecoyInteractable.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md)

## Properties

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

## Methods

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Scenario_TutorialDecoyInteractable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

