# <a id="MultiplayerInfrastructure_UI_TimeDisplayElement"></a> Class TimeDisplayElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

시간 표시(스톱워치/카운트다운) HUD 의 시각 요소.

화면 가로 중앙, 세로 top 정렬로 hh:mm:ss 를 표기한다.
정방향(스톱워치)이면 증가, 역방향(카운트다운)이면 감소하는 값을
<xref href="MultiplayerInfrastructure.UI.TimeDisplayUIController" data-throw-if-not-resolved="false"></xref> 가 매 프레임 주입한다.

비차단 HUD 이므로 pickingMode 는 Ignore 로 두어 하위 UI 클릭을 가로채지 않는다.
(<xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref> 는 모달 전용이므로 사용하지 않는다.)

```csharp
[UxmlElement]
public class TimeDisplayElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[TimeDisplayElement](MultiplayerInfrastructure.UI.TimeDisplayElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_TimeDisplayElement__ctor"></a> TimeDisplayElement\(\)

```csharp
public TimeDisplayElement()
```

## Fields

### <a id="MultiplayerInfrastructure_UI_TimeDisplayElement_RootName"></a> RootName

```csharp
public const string RootName = "time-display-root"
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_UI_TimeDisplayElement_InvalidateRenderCache"></a> InvalidateRenderCache\(\)

렌더 캐시를 무효화하여 다음 <xref href="MultiplayerInfrastructure.UI.TimeDisplayElement.Render(MultiplayerInfrastructure.Scenario.ScenarioTimeDirection%2cSystem.Int64%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref> 에서 강제로 다시 그리게 한다.
표시 대상 타이머가 교체될 때(Show) 호출하여, 값/모드가 우연히 같아도 즉시 갱신되게 한다.

```csharp
public void InvalidateRenderCache()
```

### <a id="MultiplayerInfrastructure_UI_TimeDisplayElement_Render_MultiplayerInfrastructure_Scenario_ScenarioTimeDirection_System_Int64_System_Boolean_"></a> Render\(ScenarioTimeDirection, long, bool\)

현재 시각 스냅샷을 렌더한다. 값/모드/상태가 실제로 바뀐 경우에만 요소를 갱신하여
프레임당 불필요한 텍스트/색상 쓰기(리페인트 유발)를 피한다.

```csharp
public void Render(ScenarioTimeDirection direction, long wholeSeconds, bool countdownFinished)
```

#### Parameters

`direction` [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

`wholeSeconds` long

`countdownFinished` bool

### <a id="MultiplayerInfrastructure_UI_TimeDisplayElement_SetVisibleState_System_Boolean_"></a> SetVisibleState\(bool\)

표시 여부를 토글한다.

```csharp
public void SetVisibleState(bool visible)
```

#### Parameters

`visible` bool

