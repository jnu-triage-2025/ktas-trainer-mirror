# <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalPlayerContext"></a> Class ScenarioSignalPlayerContext

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

서버 권위 콜백 안에서 실제 행동 플레이어를 신호 발신자로 보존하는 일시적 컨텍스트.
네트워크 요청자 정보를 잃는 하위 콜백은 이 범위 안에서 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref>를 호출한다.

```csharp
public static class ScenarioSignalPlayerContext
```

#### Inheritance

object ← 
[ScenarioSignalPlayerContext](MultiplayerInfrastructure.Scenario.ScenarioSignalPlayerContext.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalPlayerContext_Push_FishNet_Connection_NetworkConnection_"></a> Push\(NetworkConnection\)

```csharp
public static IDisposable Push(NetworkConnection connection)
```

#### Parameters

`connection` NetworkConnection

#### Returns

 IDisposable

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalPlayerContext_Push_System_String_System_String_"></a> Push\(string, string\)

서버 코드가 이미 확인한 플레이어 식별자로 신호 발신자 범위를 연다.

```csharp
public static IDisposable Push(string playerIdentifier, string playerDisplayName)
```

#### Parameters

`playerIdentifier` string

`playerDisplayName` string

#### Returns

 IDisposable

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalPlayerContext_TryGetCurrent_System_String__System_String__"></a> TryGetCurrent\(out string, out string\)

```csharp
public static bool TryGetCurrent(out string playerIdentifier, out string playerDisplayName)
```

#### Parameters

`playerIdentifier` string

`playerDisplayName` string

#### Returns

 bool

