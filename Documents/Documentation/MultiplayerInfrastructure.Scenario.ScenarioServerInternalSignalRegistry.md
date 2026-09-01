# <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry"></a> Class ScenarioServerInternalSignalRegistry

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

서버 권위 내부 신호를 관리하는 FIFO 레지스트리.

register/resolve 어느 쪽이 먼저 오더라도 나중에 들어온 반대쪽과 매칭된다.
targetId 는 `@s`(self) 또는 `@m`(server) 같은 서버 내부 대상 식별자와
일반 플레이어 식별자 모두를 허용한다.

```csharp
public static class ScenarioServerInternalSignalRegistry
```

#### Inheritance

object ← 
[ScenarioServerInternalSignalRegistry](MultiplayerInfrastructure.Scenario.ScenarioServerInternalSignalRegistry.md)

## Fields

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_SelfTarget"></a> SelfTarget

```csharp
public const string SelfTarget = "@s"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_ServerTarget"></a> ServerTarget

```csharp
public const string ServerTarget = "@m"
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_Clear_System_String_System_String_"></a> Clear\(string, string\)

특정 대상/신호 쌍을 큐에서 제거한다.

```csharp
public static void Clear(string targetId, string signalId)
```

#### Parameters

`targetId` string

`signalId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_ClearAll"></a> ClearAll\(\)

모든 내부 신호 상태를 초기화한다.

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_NormalizeTarget_System_String_"></a> NormalizeTarget\(string\)

대상과 신호 식별자를 정규화한다. target 이 비어 있으면 서버 대상(@m)으로 본다.

```csharp
public static string NormalizeTarget(string targetId)
```

#### Parameters

`targetId` string

#### Returns

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_Register_System_String_System_String_System_Action_"></a> Register\(string, string, Action\)

수신 측이 신호 대기를 등록한다. 이미 같은 신호가 resolve 된 상태라면 즉시 콜백을 실행한다.

```csharp
public static bool Register(string targetId, string signalId, Action onResolved)
```

#### Parameters

`targetId` string

`signalId` string

`onResolved` Action

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioServerInternalSignalRegistry_Resolve_System_String_System_String_"></a> Resolve\(string, string\)

발신 측이 신호를 resolve 한다. 이미 같은 신호를 기다리는 수신자가 있으면 즉시 콜백을 실행한다.

```csharp
public static bool Resolve(string targetId, string signalId)
```

#### Parameters

`targetId` string

`signalId` string

#### Returns

 bool

