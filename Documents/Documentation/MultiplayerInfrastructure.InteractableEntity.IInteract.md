# <a id="MultiplayerInfrastructure_InteractableEntity_IInteract"></a> Interface IInteract

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

IInteractable 모든 상호작용 가능 객체의 컨트롤러에서 구현해야합니다. 이후에 InteractableEntityResolver에 의해 활용됩니다.

```csharp
public interface IInteract
```

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteract_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteract_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteract_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteract_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
string DisplayText { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteract_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

