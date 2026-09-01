# <a id="MultiplayerInfrastructure_UI_ReusableVerticalScrollbar"></a> Class ReusableVerticalScrollbar

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

ScrollView와 함께 사용할 수 있는 경량 세로 스크롤바입니다.
호출자가 현재 오프셋/콘텐츠 크기를 공급하고, 사용자의 이동 요청을 받아 실제 ScrollView에 반영합니다.

```csharp
public sealed class ReusableVerticalScrollbar : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[ReusableVerticalScrollbar](MultiplayerInfrastructure.UI.ReusableVerticalScrollbar.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_ReusableVerticalScrollbar__ctor"></a> ReusableVerticalScrollbar\(\)

```csharp
public ReusableVerticalScrollbar()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_ReusableVerticalScrollbar_Thumb"></a> Thumb

```csharp
public VisualElement Thumb { get; }
```

#### Property Value

 VisualElement

## Methods

### <a id="MultiplayerInfrastructure_UI_ReusableVerticalScrollbar_SetMetrics_System_Single_System_Single_System_Single_"></a> SetMetrics\(float, float, float\)

```csharp
public void SetMetrics(float viewportHeight, float contentHeight, float offset)
```

#### Parameters

`viewportHeight` float

`contentHeight` float

`offset` float

### <a id="MultiplayerInfrastructure_UI_ReusableVerticalScrollbar_ScrollNormalizedRequested"></a> ScrollNormalizedRequested

```csharp
public event Action<float> ScrollNormalizedRequested
```

#### Event Type

 Action<float\>

