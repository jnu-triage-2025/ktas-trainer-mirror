# <a id="MultiplayerInfrastructure_UI_UIOverlayStack"></a> Class UIOverlayStack

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class UIOverlayStack
```

#### Inheritance

object ← 
[UIOverlayStack](MultiplayerInfrastructure.UI.UIOverlayStack.md)

## Properties

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_Top"></a> Top

```csharp
public static IUIOverlay Top { get; }
```

#### Property Value

 [IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_Clear"></a> Clear\(\)

```csharp
public static void Clear()
```

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_IsEmpty"></a> IsEmpty\(\)

```csharp
public static bool IsEmpty()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_IsTop_MultiplayerInfrastructure_UI_IUIOverlay_"></a> IsTop\(IUIOverlay\)

```csharp
public static bool IsTop(IUIOverlay overlay)
```

#### Parameters

`overlay` [IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_Pop"></a> Pop\(\)

```csharp
public static IUIOverlay Pop()
```

#### Returns

 [IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_Push_MultiplayerInfrastructure_UI_IUIOverlay_"></a> Push\(IUIOverlay\)

```csharp
public static void Push(IUIOverlay overlay)
```

#### Parameters

`overlay` [IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

### <a id="MultiplayerInfrastructure_UI_UIOverlayStack_StackChanged"></a> StackChanged

```csharp
public static event Action StackChanged
```

#### Event Type

 Action

