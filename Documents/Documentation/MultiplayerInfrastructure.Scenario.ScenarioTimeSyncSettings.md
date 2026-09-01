# <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings"></a> Class ScenarioTimeSyncSettings

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시간 표시(스톱워치/카운트다운)의 주기적 재동기화 밀도를 담는 서버 측 설정.

설계:
- 타이머 값은 연산 시점(Create/Start/Show 등)에만 전파되고 그 후엔 각 클라이언트가 로컬로 tick 하므로
  시간이 지날수록 드리프트가 누적될 수 있다. 서버는 이 설정에 따라 "현재 표시 중인 타이머"의
  권위 값을 주기적으로 재전파하여 모든 클라이언트를 다시 맞춘다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref>).
- 밀도는 tick / milliseconds / seconds 세 단위로 지정할 수 있으며, 기본값은 1초에 1회이다.
- 이 설정은 서버 권위이며, 조정은 오직 명령어(<code>timesync</code>)로만 수행한다(런타임 UI/인스펙터 노출 없음).

내부적으로는 원본 값+단위를 보존(명령 에코/조회용)하되, 실제 재전파 간격 판정은
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeSyncSettings.GetIntervalTicks" data-throw-if-not-resolved="false"></xref> 가 반환하는 tick 수로 수행한다.

```csharp
public static class ScenarioTimeSyncSettings
```

#### Inheritance

object ← 
[ScenarioTimeSyncSettings](MultiplayerInfrastructure.Scenario.ScenarioTimeSyncSettings.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_Unit"></a> Unit

현재 설정된 단위.

```csharp
public static ScenarioTimeUnit Unit { get; }
```

#### Property Value

 [ScenarioTimeUnit](MultiplayerInfrastructure.Scenario.ScenarioTimeUnit.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_Value"></a> Value

현재 설정된 원본 값(단위는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeSyncSettings.Unit" data-throw-if-not-resolved="false"></xref>).

```csharp
public static double Value { get; }
```

#### Property Value

 double

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_Configure_System_Double_MultiplayerInfrastructure_Scenario_ScenarioTimeUnit_"></a> Configure\(double, ScenarioTimeUnit\)

재동기화 밀도를 설정한다. 값은 양수여야 한다.

```csharp
public static bool Configure(double value, ScenarioTimeUnit unit)
```

#### Parameters

`value` double

`unit` [ScenarioTimeUnit](MultiplayerInfrastructure.Scenario.ScenarioTimeUnit.md)

#### Returns

 bool

유효하면 true, 아니면 false(설정 미변경).

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_Describe"></a> Describe\(\)

현재 설정을 사람이 읽을 수 있는 문자열로 반환한다(명령 에코용).

```csharp
public static string Describe()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_GetIntervalTicks"></a> GetIntervalTicks\(\)

현재 설정을 tick 간격으로 환산한다(최소 1). 재전파 판정에 사용.
TimeManager 가 없으면 근사 tickRate 로 폴백한다.

```csharp
public static uint GetIntervalTicks()
```

#### Returns

 uint

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_ResetToDefault"></a> ResetToDefault\(\)

도메인 리로드 비활성 환경에서도 기본값(1초)으로 초기화되도록 한다.

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
public static void ResetToDefault()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeSyncSettings_Changed"></a> Changed

설정이 바뀔 때 발생한다(재전파 카운터 재설정 등에 사용).

```csharp
public static event Action Changed
```

#### Event Type

 Action

