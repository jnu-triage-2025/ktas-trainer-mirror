# <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounters"></a> Class ScenarioSignalCounters

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

<code>SignalCounter</code> 노드가 등록한 카운터를 관리한다. 접두사로 시작하는 서로 다른(distinct)
시나리오 신호의 개수를 세어, 임계치에 도달하면 출력 신호를 발신한다.

<p>
시나리오 신호는 sticky 이며 <code>OnSignalRegistered</code> 는 각 신호의 최초 등록 시 1회만 발생한다.
따라서 "같은 신호 N번" 이 아니라 "접두사 매칭 신호의 distinct 개수" 를 센다. 등록 시점에 이미
올라와 있던 매칭 신호도 초기 카운트에 포함한다.
</p>

<p>
임계치 도달 시 출력 신호를 <code>Raise</code> 하고 해당 카운터를 자동 제거(1회성)한다.
재진입/중복 발신 방지를 위해 <xref href="MultiplayerInfrastructure.Scenario.ScenarioConditionalSignalListeners" data-throw-if-not-resolved="false"></xref> 와 동일한 큐 기반
디스패치 방어를 사용한다.
</p>

```csharp
public static class ScenarioSignalCounters
```

#### Inheritance

object ← 
[ScenarioSignalCounters](MultiplayerInfrastructure.Scenario.ScenarioSignalCounters.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounters_ClearAll"></a> ClearAll\(\)

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounters_RefreshDynamicThresholds"></a> RefreshDynamicThresholds\(\)

Re-evaluates counters whose expected signal set can change as players disconnect.

```csharp
public static void RefreshDynamicThresholds()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounters_Register_System_String_System_String_System_Int32_System_String_"></a> Register\(string, string, int, string\)

카운터를 등록한다. 동일 식별자 재등록은 교체한다. 등록 즉시 임계치를 충족하면 바로 발신한다.

```csharp
public static bool Register(string identifier, string sourcePrefix, int threshold, string output)
```

#### Parameters

`identifier` string

`sourcePrefix` string

`threshold` int

`output` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounters_Register_System_String_System_String_System_Int32_System_String_System_Func_System_Collections_Generic_IReadOnlyCollection_System_String___"></a> Register\(string, string, int, string, Func<IReadOnlyCollection<string\>\>\)

```csharp
public static bool Register(string identifier, string sourcePrefix, int threshold, string output, Func<IReadOnlyCollection<string>> expectedSignals)
```

#### Parameters

`identifier` string

`sourcePrefix` string

`threshold` int

`output` string

`expectedSignals` Func<IReadOnlyCollection<string\>\>

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalCounters_Unregister_System_String_"></a> Unregister\(string\)

```csharp
public static bool Unregister(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

