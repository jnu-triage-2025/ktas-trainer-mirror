# <a id="MultiplayerInfrastructure_UI_UIScaleOptionElement"></a> Class UIScaleOptionElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

그래픽 설정 UI에서 UI 배율 단계 하나를 나타내는 VisualElement입니다.

각 단계(1~4)마다 하나씩 생성되며,
클릭 시 <xref href="MultiplayerInfrastructure.UI.UIScaleOptionElement.OnOptionSelected" data-throw-if-not-resolved="false"></xref> 이벤트로 선택된 배율을 상위에 알립니다.
<xref href="MultiplayerInfrastructure.UI.UIScaleOptionElement.SetActive(System.Boolean)" data-throw-if-not-resolved="false"></xref>로 현재 선택된 항목임을 강조합니다.

```csharp
[UxmlElement]
public class UIScaleOptionElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[UIScaleOptionElement](MultiplayerInfrastructure.UI.UIScaleOptionElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_UIScaleOptionElement__ctor"></a> UIScaleOptionElement\(\)

```csharp
public UIScaleOptionElement()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_UIScaleOptionElement_BoundScale"></a> BoundScale

현재 바인딩된 배율 단계입니다.

```csharp
public UIScale BoundScale { get; }
```

#### Property Value

 [UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_UIScaleOptionElement_Bind_MultiplayerInfrastructure_UI_Models_UIScale_"></a> Bind\(UIScale\)

표시할 배율 데이터를 바인딩합니다.

```csharp
public void Bind(UIScale scale)
```

#### Parameters

`scale` [UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)

이 옵션이 나타내는 배율 단계

### <a id="MultiplayerInfrastructure_UI_UIScaleOptionElement_SetActive_System_Boolean_"></a> SetActive\(bool\)

이 옵션이 현재 선택된 옵션인지 여부를 설정합니다.

```csharp
public void SetActive(bool active)
```

#### Parameters

`active` bool

<code>true</code>이면 활성(선택됨) 상태로 강조합니다.

### <a id="MultiplayerInfrastructure_UI_UIScaleOptionElement_OnOptionSelected"></a> OnOptionSelected

이 옵션을 클릭했을 때 해당 배율 단계가 전달됩니다.

```csharp
public event Action<UIScale> OnOptionSelected
```

#### Event Type

 Action<[UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)\>

