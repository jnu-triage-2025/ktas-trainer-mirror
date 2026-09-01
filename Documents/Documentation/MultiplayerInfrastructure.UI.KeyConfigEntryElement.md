# <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement"></a> Class KeyConfigEntryElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

키 설정 목록의 항목 하나를 나타내는 VisualElement입니다.
좌측에 기능 이름, 우측에 할당된 키 이름을 표시하며,
강조(Focused) 상태, 선택(Selected) 상태, 리바인딩 대기(Rebinding) 상태를 지원합니다.

```csharp
[UxmlElement]
public class KeyConfigEntryElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[KeyConfigEntryElement](MultiplayerInfrastructure.UI.KeyConfigEntryElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement__ctor"></a> KeyConfigEntryElement\(\)

```csharp
public KeyConfigEntryElement()
```

## Methods

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_Bind_MultiplayerInfrastructure_UI_KeyBindingEntry_"></a> Bind\(KeyBindingEntry\)

항목 데이터를 바인딩하고 UI를 갱신합니다.

```csharp
public void Bind(KeyBindingEntry entry)
```

#### Parameters

`entry` [KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_RefreshKeyLabel"></a> RefreshKeyLabel\(\)

바인딩 데이터가 외부에서 변경된 후 키 레이블만 새로 고칩니다.

```csharp
public void RefreshKeyLabel()
```

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_SetFocused_System_Boolean_"></a> SetFocused\(bool\)

포커스(강조) 상태를 설정합니다. 스크롤 이동은 호출자가 담당합니다.

```csharp
public void SetFocused(bool focused)
```

#### Parameters

`focused` bool

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_SetRebinding_System_Boolean_"></a> SetRebinding\(bool\)

리바인딩 대기 상태를 설정합니다.
true이면 키 레이블에 "키를 누르세요…" 텍스트와 rebinding 스타일 클래스를 적용합니다.
false이면 원래 키 이름으로 복원합니다.

```csharp
public void SetRebinding(bool rebinding)
```

#### Parameters

`rebinding` bool

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_SetSelected_System_Boolean_"></a> SetSelected\(bool\)

선택 상태를 설정합니다.

```csharp
public void SetSelected(bool selected)
```

#### Parameters

`selected` bool

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_OnEntryClicked"></a> OnEntryClicked

항목을 클릭했을 때 해당 항목의 actionId가 전달됩니다.

```csharp
public event Action<string> OnEntryClicked
```

#### Event Type

 Action<string\>

### <a id="MultiplayerInfrastructure_UI_KeyConfigEntryElement_OnRebindRequested"></a> OnRebindRequested

키 레이블을 클릭해 리바인딩을 요청할 때 해당 항목의 actionId가 전달됩니다.

```csharp
public event Action<string> OnRebindRequested
```

#### Event Type

 Action<string\>

