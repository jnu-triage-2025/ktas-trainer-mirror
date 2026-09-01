# <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement"></a> Class KeyboardLayoutElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

키보드 레이아웃을 시각화하는 VisualElement입니다.
각 키를 흰색 배경 + 회색 테두리 VisualElement로 직접 그립니다.

레이어 구성:
  Layer 0 - 키 오버레이 (할당된 기능 레이블, 클릭 영역)
  Layer 1 - 키 이름 레이블 (Q, W, E … 등 작은 텍스트)

```csharp
[UxmlElement]
public class KeyboardLayoutElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[KeyboardLayoutElement](MultiplayerInfrastructure.UI.KeyboardLayoutElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement__ctor"></a> KeyboardLayoutElement\(\)

```csharp
public KeyboardLayoutElement()
```

## Methods

### <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement_ClearAllHighlights"></a> ClearAllHighlights\(\)

모든 강조 표시를 제거합니다.

```csharp
public void ClearAllHighlights()
```

### <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement_HighlightKey_UnityEngine_KeyCode_"></a> HighlightKey\(KeyCode\)

특정 키를 강조(하이라이트) 표시합니다.

```csharp
public void HighlightKey(KeyCode key)
```

#### Parameters

`key` KeyCode

### <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement_SetBindings_System_Collections_Generic_IEnumerable_MultiplayerInfrastructure_UI_KeyBindingEntry__"></a> SetBindings\(IEnumerable<KeyBindingEntry\>\)

키 바인딩 목록으로 오버레이를 갱신합니다.
이미 화면에 올라온 후 호출해도 됩니다.

```csharp
public void SetBindings(IEnumerable<KeyBindingEntry> entries)
```

#### Parameters

`entries` IEnumerable<[KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)\>

### <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement_OnAssignedKeyClicked"></a> OnAssignedKeyClicked

할당된 키를 클릭했을 때 해당 기능의 actionId를 인자로 전달합니다.

```csharp
public event Action<string> OnAssignedKeyClicked
```

#### Event Type

 Action<string\>

### <a id="MultiplayerInfrastructure_UI_KeyboardLayoutElement_OnKeyClicked"></a> OnKeyClicked

할당 여부에 관계없이 키를 클릭할 때 KeyCode를 전달합니다.

```csharp
public event Action<KeyCode> OnKeyClicked
```

#### Event Type

 Action<KeyCode\>

