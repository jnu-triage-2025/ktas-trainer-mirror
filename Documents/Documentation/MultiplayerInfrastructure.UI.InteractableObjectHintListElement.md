# <a id="MultiplayerInfrastructure_UI_InteractableObjectHintListElement"></a> Class InteractableObjectHintListElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class InteractableObjectHintListElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[InteractableObjectHintListElement](MultiplayerInfrastructure.UI.InteractableObjectHintListElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintListElement__ctor"></a> InteractableObjectHintListElement\(\)

```csharp
public InteractableObjectHintListElement()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintListElement_Interact"></a> Interact

```csharp
public IInteract Interact { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintListElement_Bind_MultiplayerInfrastructure_InteractableEntity_IInteract_System_String_MultiplayerInfrastructure_UI_InteractableHintUIMode_UnityEngine_Sprite_System_Boolean_"></a> Bind\(IInteract, string, InteractableHintUIMode, Sprite, bool\)

```csharp
public void Bind(IInteract interact, string keyLabel, InteractableHintUIMode mode, Sprite dialogueIcon, bool isSelected)
```

#### Parameters

`interact` [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

`keyLabel` string

`mode` [InteractableHintUIMode](MultiplayerInfrastructure.UI.InteractableHintUIMode.md)

`dialogueIcon` Sprite

`isSelected` bool

