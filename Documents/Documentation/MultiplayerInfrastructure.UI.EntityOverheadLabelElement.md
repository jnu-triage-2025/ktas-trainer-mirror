# <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelElement"></a> Class EntityOverheadLabelElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

엔티티 위에 띄우는 일반화된 오버헤드 라벨(뱃지) 비주얼 엘리먼트.

<p>
플레이어 이름표를 머리 위에 띄우듯, 임의의 월드 엔티티 위에 "색상 사각형 + 텍스트" 형태의 라벨을 표시한다.
색상 사각형과 텍스트 색상은 독립적으로 지정할 수 있어, 트리아지 등급(색상 + 명칭) 같은 도메인 표기에
재사용된다. 색상 사각형은 숨길 수 있어(NPC 이름표처럼) 텍스트만 표시할 수도 있다.
위치(화면 좌표) 갱신은 <xref href="MultiplayerInfrastructure.UI.EntityOverheadLabelUIController" data-throw-if-not-resolved="false"></xref> 가 담당한다.
</p>

```csharp
public sealed class EntityOverheadLabelElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[EntityOverheadLabelElement](MultiplayerInfrastructure.UI.EntityOverheadLabelElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelElement__ctor"></a> EntityOverheadLabelElement\(\)

```csharp
public EntityOverheadLabelElement()
```

## Methods

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelElement_SetContent_UnityEngine_Color_System_String_UnityEngine_Color_System_Boolean_"></a> SetContent\(Color, string, Color, bool\)

라벨 내용을 갱신한다.

```csharp
public void SetContent(Color swatchColor, string text, Color textColor, bool showSwatch = true)
```

#### Parameters

`swatchColor` Color

색상 사각형의 색.

`text` string

표기할 텍스트(명칭).

`textColor` Color

텍스트 색.

`showSwatch` bool

색상 사각형 표시 여부. false 면 텍스트만 표시한다(예: NPC 이름표).

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelElement_SetContent_UnityEngine_Color_System_String_UnityEngine_Color_System_Boolean_UnityEngine_Sprite_"></a> SetContent\(Color, string, Color, bool, Sprite\)

```csharp
public void SetContent(Color swatchColor, string text, Color textColor, bool showSwatch, Sprite icon)
```

#### Parameters

`swatchColor` Color

`text` string

`textColor` Color

`showSwatch` bool

`icon` Sprite

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelElement_SetScreenPosition_UnityEngine_Vector2_"></a> SetScreenPosition\(Vector2\)

화면 좌표에 라벨 중심을 배치한다(자기 크기 중심 정렬).

```csharp
public void SetScreenPosition(Vector2 panelPosition)
```

#### Parameters

`panelPosition` Vector2

