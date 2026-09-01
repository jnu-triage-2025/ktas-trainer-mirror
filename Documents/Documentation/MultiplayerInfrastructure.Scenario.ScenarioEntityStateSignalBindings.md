# <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindings"></a> Class ScenarioEntityStateSignalBindings

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

<code>EntityStateSignalBinding</code> 노드가 등록한 "엔티티 상태 이벤트 → 시나리오 신호" 바인딩을 추적·정리한다.

<p>
실제 리스너 등록은 대상 엔티티의 <xref href="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource" data-throw-if-not-resolved="false"></xref> 에 이루어지며,
이 레지스트리는 각 바인딩의 콜백까지 보관하여 시나리오 종료 시 또는 동일 식별자 재등록 시
정확히 재구성한다. 소스는 이벤트명 단위 해제만 제공하므로, 특정 바인딩을 제거할 때는
해당 (소스, 이벤트) 리스너를 모두 지운 뒤 남은 바인딩을 다시 등록한다.
</p>

<p>
콜백에서 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref> 를 호출한다. 신호 발신 자체가 서버 권위
라우팅이므로, 이벤트가 서버(호스트) 컨텍스트에서 발생하는 한 모든 피어에 일관되게 전파된다.
</p>

```csharp
public static class ScenarioEntityStateSignalBindings
```

#### Inheritance

object ← 
[ScenarioEntityStateSignalBindings](MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindings.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindings_ClearAll"></a> ClearAll\(\)

시나리오 시작/종료 시 모든 바인딩을 정리한다.

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindings_Register_System_String_MultiplayerInfrastructure_Entity_IScenarioEntityStateEventSource_System_String_System_String_System_String_System_Boolean_"></a> Register\(string, IScenarioEntityStateEventSource, string, string, string, bool\)

바인딩을 등록한다. 동일 <code class="paramref">bindingIdentifier</code> 가 이미 있으면 먼저 해제 후 교체한다.

```csharp
public static bool Register(string bindingIdentifier, IScenarioEntityStateEventSource source, string eventName, string eventKey, string outputSignalIdentifier, bool consumeOnce)
```

#### Parameters

`bindingIdentifier` string

`source` [IScenarioEntityStateEventSource](MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource.md)

`eventName` string

`eventKey` string

`outputSignalIdentifier` string

`consumeOnce` bool

#### Returns

 bool

대상 소스가 이벤트를 인식하여 등록에 성공하면 true.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindings_Unregister_System_String_"></a> Unregister\(string\)

바인딩을 해제한다. 같은 (소스, 이벤트)의 남은 바인딩은 재등록하여 유지한다.

```csharp
public static bool Unregister(string bindingIdentifier)
```

#### Parameters

`bindingIdentifier` string

#### Returns

 bool

