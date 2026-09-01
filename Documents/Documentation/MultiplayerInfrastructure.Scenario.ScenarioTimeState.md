# <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState"></a> Class ScenarioTimeState

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

다중 시간(스톱워치/카운트다운)의 로컬 스냅샷 모델.

설계 개요:
- 식별자(timerId)로 여러 타이머를 동시에 보유한다. 다만 화면에 표시되는 타이머는 항상 최대 1개이며,
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState._shownTimerId" data-throw-if-not-resolved="false"></xref> 가 그 대상을 가리킨다.
- 생성(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Create(System.String%2cMultiplayerInfrastructure.Scenario.ScenarioTimeDirection%2cSystem.Double%2cSystem.Double)" data-throw-if-not-resolved="false"></xref>)은 "정지" 상태로 만들고, 흐름은 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Start(System.String%2cSystem.Double)" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Resume(System.String%2cSystem.Double)" data-throw-if-not-resolved="false"></xref> 로,
  화면 표시는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Show(System.String)" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Hide" data-throw-if-not-resolved="false"></xref> 로 "별도 제어"한다. 카운트다운이 0 에 도달해도
  자동으로 숨기지 않는다(표시 전환은 오직 Show/Hide/Remove 로만 발생).

멀티플레이어:
- <xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 는 클라이언트마다 독립 실행되므로, 서버가 각 연산을 push 하고
  각 클라이언트가 로컬 기준점(TimerInstance._localAnchorRealtime, realtime)에서 tick 한다.
  서버 동기화는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref> 가, 표시는
  <xref href="MultiplayerInfrastructure.UI.TimeDisplayUIController" data-throw-if-not-resolved="false"></xref> HUD 가 담당한다.

```csharp
public static class ScenarioTimeState
```

#### Inheritance

object ← 
[ScenarioTimeState](MultiplayerInfrastructure.Scenario.ScenarioTimeState.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_IsVisible"></a> IsVisible

현재 화면에 표시할 타이머가 있는가.

```csharp
public static bool IsVisible { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_ShownTimerId"></a> ShownTimerId

현재 표시 중인 타이머 id(없으면 null).

```csharp
public static string ShownTimerId { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_ApplyResync_System_String_MultiplayerInfrastructure_Scenario_ScenarioTimeDirection_System_Double_System_Double_System_Boolean_System_Double_"></a> ApplyResync\(string, ScenarioTimeDirection, double, double, bool, double\)

주기적 재동기화 스냅샷을 적용한다. 지정한 타이머의 값(방향/목표/경과)만 스냅샷으로 덮어쓴다(없으면 생성).
흐르는 중이면 발행 이후 이미 흐른 시간(<code class="paramref">alreadyRunningSeconds</code>)을 반영해
늦게 수신한 피어도 정확히 맞춘다.

표시 여부(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState._shownTimerId" data-throw-if-not-resolved="false"></xref>)는 이 메서드에서 절대 변경하지 않는다. 표시 전환은
스펙상 오직 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Show(System.String)" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Hide" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Remove(System.String)" data-throw-if-not-resolved="false"></xref> 연산으로만 발생해야 한다.
(과거 구현은 resync 가 표시를 강제로 켜서, 생성만 한 타이머나 이전 세션의 버퍼된 resync 로 인해
 Show 없이 화면에 표시되는 문제가 있었다.)

```csharp
public static void ApplyResync(string timerId, ScenarioTimeDirection direction, double displaySeconds, double targetSeconds, bool running, double alreadyRunningSeconds)
```

#### Parameters

`timerId` string

`direction` [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

`displaySeconds` double

`targetSeconds` double

`running` bool

`alreadyRunningSeconds` double

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Create_System_String_MultiplayerInfrastructure_Scenario_ScenarioTimeDirection_System_Double_System_Double_"></a> Create\(string, ScenarioTimeDirection, double, double\)

타이머를 생성(또는 재설정)한다. "정지" 상태로 만들며, 화면에 표시하지 않는다.
이미 같은 id 가 있으면 방향/목표/시작값을 새로 덮어쓴다.

```csharp
public static void Create(string timerId, ScenarioTimeDirection direction, double startSeconds, double targetSeconds)
```

#### Parameters

`timerId` string

타이머 식별자.

`direction` [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

정방향(스톱워치) 또는 역방향(카운트다운).

`startSeconds` double

시작 시점의 "표시 값"(초). 스톱워치는 표시할 경과(보통 0), 카운트다운은 표시할 남은값(보통 목표와 동일).

`targetSeconds` double

카운트다운의 목표(총) 시간(초). 스톱워치에서는 무시된다.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_FormatHhMmSs_System_Int64_"></a> FormatHhMmSs\(long\)

정수 초 값을 hh:mm:ss 로 변환한다. 두 자리 룩업으로 박싱/보간 할당을 피한다.

```csharp
public static string FormatHhMmSs(long wholeSeconds)
```

#### Parameters

`wholeSeconds` long

#### Returns

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_FormatHhMmSs_System_Double_"></a> FormatHhMmSs\(double\)

편의 오버로드. 실수 초를 정수 초로 내림하여 포맷한다.

```csharp
public static string FormatHhMmSs(double totalSeconds)
```

#### Parameters

`totalSeconds` double

#### Returns

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Hide"></a> Hide\(\)

화면 표시를 끈다. 타이머 상태/흐름은 유지한다(삭제 아님).

```csharp
public static void Hide()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Pause_System_String_"></a> Pause\(string\)

타이머 흐름을 일시정지한다(표시/값 유지). 현재까지의 경과를 확정한다.

```csharp
public static void Pause(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Remove_System_String_"></a> Remove\(string\)

타이머를 삭제한다. 표시 중이던 타이머면 표시도 꺼진다.

```csharp
public static void Remove(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_RemoveAll"></a> RemoveAll\(\)

모든 타이머를 삭제하고 표시를 끈다. 시나리오 시작/종료 등 경계 정리에 사용.

```csharp
public static void RemoveAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_ResetStatics"></a> ResetStatics\(\)

정적 상태를 초기화한다. 플레이 세션 시작 시(도메인 리로드 비활성 환경 포함)와
시나리오 시작/종료 시 호출하여 이전 세션/시나리오의 잔여 타이머가 새어 나오지 않게 한다.

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
public static void ResetStatics()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Resume_System_String_System_Double_"></a> Resume\(string, double\)

일시정지된 타이머 흐름을 재개한다.

```csharp
public static void Resume(string timerId, double alreadyRunningSeconds = 0)
```

#### Parameters

`timerId` string

`alreadyRunningSeconds` double

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Set_System_String_System_Double_System_Nullable_System_Double__"></a> Set\(string, double, double?\)

타이머의 현재 표시값을 절대값으로 설정한다(스톱워치=경과, 카운트다운=남은값). 흐름 상태는 유지한다.
카운트다운의 목표(총) 시간도 함께 조정할 수 있다.

```csharp
public static void Set(string timerId, double displaySeconds, double? newTargetSeconds)
```

#### Parameters

`timerId` string

`displaySeconds` double

설정할 현재 표시값(초). 음수는 0 으로 취급.

`newTargetSeconds` double?

카운트다운 목표(총) 시간(초)을 재설정한다. null 이면 기존 목표 유지. 스톱워치에서는 무시된다.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Show_System_String_"></a> Show\(string\)

지정한 타이머를 화면에 표시한다(표시는 항상 최대 1개, 기존 표시는 교체). 없으면 무시.

```csharp
public static void Show(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_ShownCountdownFinished"></a> ShownCountdownFinished\(\)

현재 표시 중인 카운트다운이 0 에 도달했는가(표시 중 아님/스톱워치면 false).

```csharp
public static bool ShownCountdownFinished()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_ShownDirection"></a> ShownDirection\(\)

현재 표시 중인 타이머의 흐름 방향. 표시 중이 아니면 스톱워치(기본).

```csharp
public static ScenarioTimeDirection ShownDirection()
```

#### Returns

 [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_ShownWholeSeconds"></a> ShownWholeSeconds\(\)

현재 표시 중인 타이머의 표시값을 정수 초로 반환한다(음수/NaN 은 0). 표시 중이 아니면 0.
매 프레임 변화 감지에 사용하여 값이 바뀐 초에만 재포맷하도록 한다(프레임당 문자열 할당 방지).

```csharp
public static long ShownWholeSeconds()
```

#### Returns

 long

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Start_System_String_System_Double_"></a> Start\(string, double\)

타이머의 흐름을 시작(또는 재시작)한다. 없으면 아무 것도 하지 않는다.
표시 상태는 건드리지 않는다(표시는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Show(System.String)" data-throw-if-not-resolved="false"></xref> 로 별도 제어).

```csharp
public static void Start(string timerId, double alreadyRunningSeconds = 0)
```

#### Parameters

`timerId` string

`alreadyRunningSeconds` double

이 명령이 서버에서 발행된 뒤 이 피어가 수신하기까지 이미 흐른 시간(초). 늦게 수신한 피어의 보정용.
서버/호스트/즉시 수신 피어는 0. 음수는 0 으로 취급한다.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Stop_System_String_"></a> Stop\(string\)

타이머 흐름을 정지하고 값을 시작값(경과 0)으로 되돌린다(표시/생성 상태 유지).
스톱워치는 00:00:00, 카운트다운은 목표 시간(전량)으로 표시된다.

```csharp
public static void Stop(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_TryGetShownAuthoritative_System_String__MultiplayerInfrastructure_Scenario_ScenarioTimeDirection__System_Double__System_Double__System_Boolean__"></a> TryGetShownAuthoritative\(out string, out ScenarioTimeDirection, out double, out double, out bool\)

현재 표시 중인 타이머의 권위 상태를 반환한다(주기적 재동기화용).
표시 중인 타이머가 없으면 false.

```csharp
public static bool TryGetShownAuthoritative(out string timerId, out ScenarioTimeDirection direction, out double displaySeconds, out double targetSeconds, out bool running)
```

#### Parameters

`timerId` string

표시 중인 타이머 id.

`direction` [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

흐름 방향.

`displaySeconds` double

현재 표시값(스톱워치=경과, 카운트다운=남은값). 실수 초.

`targetSeconds` double

카운트다운 목표(총) 시간. 스톱워치면 0.

`running` bool

현재 흐르고 있는가.

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeState_Changed"></a> Changed

표시 대상/값 상태가 바뀔 때(생성/시작/정지/표시/숨김/삭제 등) 발생한다. HUD 즉시 갱신용.

```csharp
public static event Action Changed
```

#### Event Type

 Action

