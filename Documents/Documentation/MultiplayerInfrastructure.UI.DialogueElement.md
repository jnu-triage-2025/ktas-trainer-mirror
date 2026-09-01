# <a id="MultiplayerInfrastructure_UI_DialogueElement"></a> Class DialogueElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class DialogueElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[DialogueElement](MultiplayerInfrastructure.UI.DialogueElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_DialogueElement__ctor"></a> DialogueElement\(\)

```csharp
public DialogueElement()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_DialogueElement_DialogueText"></a> DialogueText

```csharp
[UxmlAttribute]
public string DialogueText { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_UI_DialogueElement_EnableRichText"></a> EnableRichText

```csharp
[UxmlAttribute]
public bool EnableRichText { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_HighlightKeywords"></a> HighlightKeywords

```csharp
[UxmlAttribute]
public bool HighlightKeywords { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_IsTyping"></a> IsTyping

```csharp
public bool IsTyping { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_IsVisible"></a> IsVisible

```csharp
public bool IsVisible { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_IsWaitingForInput"></a> IsWaitingForInput

```csharp
public bool IsWaitingForInput { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_SpeakerName"></a> SpeakerName

```csharp
[UxmlAttribute]
public string SpeakerName { get; set; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_UI_DialogueElement_CompleteTyping"></a> CompleteTyping\(\)

```csharp
public void CompleteTyping()
```

### <a id="MultiplayerInfrastructure_UI_DialogueElement_GetCurrentChar"></a> GetCurrentChar\(\)

```csharp
public char GetCurrentChar()
```

#### Returns

 char

### <a id="MultiplayerInfrastructure_UI_DialogueElement_Hide"></a> Hide\(\)

```csharp
public void Hide()
```

### <a id="MultiplayerInfrastructure_UI_DialogueElement_Reset"></a> Reset\(\)

```csharp
public void Reset()
```

### <a id="MultiplayerInfrastructure_UI_DialogueElement_SetDialogueText_System_String_"></a> SetDialogueText\(string\)

```csharp
public void SetDialogueText(string text)
```

#### Parameters

`text` string

### <a id="MultiplayerInfrastructure_UI_DialogueElement_SetWaitingForInput_System_Boolean_"></a> SetWaitingForInput\(bool\)

```csharp
public void SetWaitingForInput(bool waiting)
```

#### Parameters

`waiting` bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_Show"></a> Show\(\)

```csharp
public void Show()
```

### <a id="MultiplayerInfrastructure_UI_DialogueElement_StartTyping_System_String_"></a> StartTyping\(string\)

```csharp
public void StartTyping(string text)
```

#### Parameters

`text` string

### <a id="MultiplayerInfrastructure_UI_DialogueElement_TypeNextCharacter"></a> TypeNextCharacter\(\)

```csharp
public bool TypeNextCharacter()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_DialogueElement_OnDialogueClicked"></a> OnDialogueClicked

```csharp
public event Action OnDialogueClicked
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_DialogueElement_OnTypingComplete"></a> OnTypingComplete

```csharp
public event Action OnTypingComplete
```

#### Event Type

 Action

