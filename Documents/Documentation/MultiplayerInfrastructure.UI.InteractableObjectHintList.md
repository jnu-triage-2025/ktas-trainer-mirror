# <a id="MultiplayerInfrastructure_UI_InteractableObjectHintList"></a> Class InteractableObjectHintList

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class InteractableObjectHintList : ScrollView
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
ScrollView ← 
[InteractableObjectHintList](MultiplayerInfrastructure.UI.InteractableObjectHintList.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintList__ctor"></a> InteractableObjectHintList\(\)

```csharp
public InteractableObjectHintList()
```

## Methods

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintList_ApplyStyles"></a> ApplyStyles\(\)

```csharp
public void ApplyStyles()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintList_Rebuild_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_InteractableEntity_IInteract__System_Int32_MultiplayerInfrastructure_UI_InteractableHintUIMode_System_String_UnityEngine_Sprite_System_Action_System_Int32__"></a> Rebuild\(IReadOnlyList<IInteract\>, int, InteractableHintUIMode, string, Sprite, Action<int\>\)

```csharp
public void Rebuild(IReadOnlyList<IInteract> interacts, int selectedIndex, InteractableHintUIMode mode, string keyLabel, Sprite dialogueIcon, Action<int> onClicked = null)
```

#### Parameters

`interacts` IReadOnlyList<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

`selectedIndex` int

`mode` [InteractableHintUIMode](MultiplayerInfrastructure.UI.InteractableHintUIMode.md)

`keyLabel` string

`dialogueIcon` Sprite

`onClicked` Action<int\>

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintList_ScrollToSelected_System_Int32_"></a> ScrollToSelected\(int\)

```csharp
public void ScrollToSelected(int selectedIndex)
```

#### Parameters

`selectedIndex` int

