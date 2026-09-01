# <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable"></a> Class OxyLinePairInteractable

Namespace: [TriageTrainer.Entity.OxyLine](TriageTrainer.Entity.OxyLine.md)  
Assembly: Assembly\-CSharp.dll  

Exposes one oxygen-line connection action from either endpoint of a configured pair.
Both instances must reference each other. The action is unavailable until the required
patient display object is active and the associated wall flowmeter is attached.

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class OxyLinePairInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional, IQuestPresentationTarget
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[OxyLinePairInteractable](TriageTrainer.Entity.OxyLine.OxyLinePairInteractable.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md)

## Properties

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_InteractionIdentifier"></a> InteractionIdentifier

```csharp
public string InteractionIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_OxyLine_OxyLinePairInteractable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

