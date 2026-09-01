# <a id="MultiplayerInfrastructure_UI_TextureQualityOptionElement"></a> Class TextureQualityOptionElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

그래픽 설정 UI에서 텍스처 품질 단계 하나를 나타내는 VisualElement입니다.

각 옵션(Ultra / High / Medium / Low)마다 하나씩 생성되며,
클릭 시 <xref href="MultiplayerInfrastructure.UI.TextureQualityOptionElement.OnOptionSelected" data-throw-if-not-resolved="false"></xref> 이벤트로 선택된 품질을 상위에 알립니다.
<xref href="MultiplayerInfrastructure.UI.TextureQualityOptionElement.SetActive(System.Boolean)" data-throw-if-not-resolved="false"></xref>로 현재 선택된 항목임을 강조합니다.

```csharp
[UxmlElement]
public class TextureQualityOptionElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[TextureQualityOptionElement](MultiplayerInfrastructure.UI.TextureQualityOptionElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_TextureQualityOptionElement__ctor"></a> TextureQualityOptionElement\(\)

```csharp
public TextureQualityOptionElement()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_TextureQualityOptionElement_BoundQuality"></a> BoundQuality

현재 바인딩된 품질 단계입니다.

```csharp
public TextureQuality BoundQuality { get; }
```

#### Property Value

 [TextureQuality](MultiplayerInfrastructure.UI.Models.TextureQuality.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_TextureQualityOptionElement_Bind_MultiplayerInfrastructure_UI_Models_TextureQuality_"></a> Bind\(TextureQuality\)

표시할 품질 데이터를 바인딩합니다.

```csharp
public void Bind(TextureQuality quality)
```

#### Parameters

`quality` [TextureQuality](MultiplayerInfrastructure.UI.Models.TextureQuality.md)

이 옵션이 나타내는 품질 단계

### <a id="MultiplayerInfrastructure_UI_TextureQualityOptionElement_SetActive_System_Boolean_"></a> SetActive\(bool\)

이 옵션이 현재 선택된 옵션인지 여부를 설정합니다.

```csharp
public void SetActive(bool active)
```

#### Parameters

`active` bool

<code>true</code>이면 활성(선택됨) 상태로 강조합니다.

### <a id="MultiplayerInfrastructure_UI_TextureQualityOptionElement_OnOptionSelected"></a> OnOptionSelected

이 옵션을 클릭했을 때 해당 품질 단계가 전달됩니다.

```csharp
public event Action<TextureQuality> OnOptionSelected
```

#### Event Type

 Action<[TextureQuality](MultiplayerInfrastructure.UI.Models.TextureQuality.md)\>

