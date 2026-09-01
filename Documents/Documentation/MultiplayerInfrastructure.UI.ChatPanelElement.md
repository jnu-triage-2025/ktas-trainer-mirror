# <a id="MultiplayerInfrastructure_UI_ChatPanelElement"></a> Class ChatPanelElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class ChatPanelElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[ChatPanelElement](MultiplayerInfrastructure.UI.ChatPanelElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement__ctor"></a> ChatPanelElement\(\)

```csharp
public ChatPanelElement()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_CursorPosition"></a> CursorPosition

현재 입력창의 커서 위치.

```csharp
public int CursorPosition { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_InputText"></a> InputText

```csharp
public string InputText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_IsInputFocused"></a> IsInputFocused

```csharp
public bool IsInputFocused { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_IsOpen"></a> IsOpen

```csharp
public bool IsOpen { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_AppendMessage_System_String_System_Boolean_"></a> AppendMessage\(string, bool\)

```csharp
public void AppendMessage(string message, bool showToastWhenHidden = true)
```

#### Parameters

`message` string

`showToastWhenHidden` bool

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_ApplyInput_System_String_System_Int32_"></a> ApplyInput\(string, int\)

```csharp
public void ApplyInput(string text, int cursorPos)
```

#### Parameters

`text` string

`cursorPos` int

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_ClearInput"></a> ClearInput\(\)

```csharp
public void ClearInput()
```

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_ClearLog"></a> ClearLog\(\)

```csharp
public void ClearLog()
```

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_ClearToasts"></a> ClearToasts\(\)

```csharp
public void ClearToasts()
```

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_ConsumeInput"></a> ConsumeInput\(\)

```csharp
public string ConsumeInput()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_FocusInput"></a> FocusInput\(\)

```csharp
public void FocusInput()
```

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_PushInput_System_String_"></a> PushInput\(string\)

```csharp
public void PushInput(string text)
```

#### Parameters

`text` string

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_RecallNextInput"></a> RecallNextInput\(\)

```csharp
public void RecallNextInput()
```

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_RecallPreviousInput"></a> RecallPreviousInput\(\)

```csharp
public void RecallPreviousInput()
```

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_SetMaxLogEntries_System_Int32_"></a> SetMaxLogEntries\(int\)

```csharp
public void SetMaxLogEntries(int maxEntries)
```

#### Parameters

`maxEntries` int

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_SetOpen_System_Boolean_"></a> SetOpen\(bool\)

```csharp
public void SetOpen(bool open)
```

#### Parameters

`open` bool

### <a id="MultiplayerInfrastructure_UI_ChatPanelElement_InputKeyPressed"></a> InputKeyPressed

Raised for keyboard input in the chat field. The controller uses this
to invalidate a Tab-cycling session when the user edits the text or
navigates with another key.

```csharp
public event Action<KeyCode> InputKeyPressed
```

#### Event Type

 Action<KeyCode\>

