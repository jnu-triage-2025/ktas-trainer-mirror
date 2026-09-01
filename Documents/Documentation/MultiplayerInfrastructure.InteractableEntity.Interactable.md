# <a id="MultiplayerInfrastructure_InteractableEntity_Interactable"></a> Class Interactable

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public abstract class Interactable : MonoBehaviour, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md)

#### Derived

[ItemSubmissionInteractable](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.md), 
[Npc](MultiplayerInfrastructure.Entity.Npc.md), 
[StaticObjectDisplayment](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md), 
[StaticPlacedItem](MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md)

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public virtual bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public virtual Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public virtual Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_DisplayIcons"></a> DisplayIcons

```csharp
public virtual IReadOnlyList<Sprite> DisplayIcons { get; }
```

#### Property Value

 IReadOnlyList<Sprite\>

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public virtual string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_InteractionIdentifier"></a> InteractionIdentifier

```csharp
public virtual string InteractionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_Interacts"></a> Interacts

```csharp
public virtual IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public virtual string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_Interactable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public abstract void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

