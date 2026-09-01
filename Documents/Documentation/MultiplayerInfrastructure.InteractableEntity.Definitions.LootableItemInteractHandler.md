# <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler"></a> Class LootableItemInteractHandler

Namespace: [MultiplayerInfrastructure.InteractableEntity.Definitions](MultiplayerInfrastructure.InteractableEntity.Definitions.md)  
Assembly: Assembly\-CSharp.dll  

월드에 드롭된 아이템(<xref href="MultiplayerInfrastructure.ItemSystem.ItemObject" data-throw-if-not-resolved="false"></xref>)을 플레이어가 획득할 수 있도록 합니다.

필요할 때 별도로 부착할 수 있는 레거시 상호작용 핸들러입니다.
NearbyInteractablesDetector 가 같은 GameObject의 Collider를 통해 감지하며,
플레이어가 상호작용(E 키)하면 Interact()가 호출됩니다.

```csharp
public class LootableItemInteractHandler : MonoBehaviour, IInteractable, IInteract
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LootableItemInteractHandler](MultiplayerInfrastructure.InteractableEntity.Definitions.LootableItemInteractHandler.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_Definitions_LootableItemInteractHandler_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때 호출됩니다.
아이템을 플레이어 인벤토리에 추가하고 ItemObject를 제거합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

