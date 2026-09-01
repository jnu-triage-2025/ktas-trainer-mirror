# <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent"></a> Struct EntityOverheadLabelUIController.LabelContent

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

단일 라벨의 표시 내용.

```csharp
public readonly struct EntityOverheadLabelUIController.LabelContent
```

## Constructors

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent__ctor_UnityEngine_Color_System_String_UnityEngine_Color_"></a> LabelContent\(Color, string, Color\)

```csharp
public LabelContent(Color swatchColor, string text, Color textColor)
```

#### Parameters

`swatchColor` Color

`text` string

`textColor` Color

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent__ctor_System_String_UnityEngine_Color_"></a> LabelContent\(string, Color\)

색상 사각형 없이 텍스트만 표시하는 라벨(예: NPC 이름표).

```csharp
public LabelContent(string text, Color textColor)
```

#### Parameters

`text` string

`textColor` Color

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent__ctor_UnityEngine_Sprite_"></a> LabelContent\(Sprite\)

```csharp
public LabelContent(Sprite icon)
```

#### Parameters

`icon` Sprite

## Fields

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_Icon"></a> Icon

```csharp
public readonly Sprite Icon
```

#### Field Value

 Sprite

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_ShowSwatch"></a> ShowSwatch

색상 사각형 표시 여부. false 면 텍스트만 표시한다(예: NPC 이름표).

```csharp
public readonly bool ShowSwatch
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_SwatchColor"></a> SwatchColor

```csharp
public readonly Color SwatchColor
```

#### Field Value

 Color

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_Text"></a> Text

```csharp
public readonly string Text
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_TextColor"></a> TextColor

```csharp
public readonly Color TextColor
```

#### Field Value

 Color

