# <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay"></a> Class ScenarioTimeRelay

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

다중 시간(스톱워치/카운트다운)의 서버 권한(authoritative) 동기화 중계기.

설계 근거:
- 시간 표시는 모든 클라이언트가 동일한 값을 봐야 한다. 그러나 <xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 는
  클라이언트마다 독립 실행되고 동기화된 커서가 없으므로, 서버가 각 연산(생성/시작/표시 등)을
  모든 클라이언트에 push 해야 한다.
- 타이머 연산의 유일한 출처는 서버에서 실행되는 시나리오 로직(<xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref>)이다.
  따라서 클라이언트→서버 보고 경로(ServerRpc)는 두지 않는다. 서버 컨텍스트면
  ObserversRpc 로 전 클라이언트에 미러링하고, 네트워크 비활성/중계기 부재면
  로컬(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState" data-throw-if-not-resolved="false"></xref>)에만 적용한다.
- 흐름 시작(Start/Resume) 연산에는 서버 tick 을 함께 실어, 늦게 접속한 클라이언트가
  "발행 이후 이미 흐른 시간"을 계산해 현재 진행 지점부터 표시하도록 보정한다.

단일 플레이어(호스트 단독)에서는 서버=클라 이므로 로컬 적용과 동일하게 동작한다.

```csharp
public sealed class ScenarioTimeRelay : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[ScenarioTimeRelay](MultiplayerInfrastructure.Scenario.ScenarioTimeRelay.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_ClearAllAuthoritative"></a> ClearAllAuthoritative\(\)

모든 타이머를 삭제하고 표시를 끈다(전 클라이언트). 시나리오 시작/종료 경계 정리용.
노드 연산 어휘(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType" data-throw-if-not-resolved="false"></xref>)에는 노출하지 않는 시스템 연산이다.

```csharp
public static void ClearAllAuthoritative()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_CreateAuthoritative_System_String_MultiplayerInfrastructure_Scenario_ScenarioTimeDirection_System_Double_System_Double_"></a> CreateAuthoritative\(string, ScenarioTimeDirection, double, double\)

```csharp
public static void CreateAuthoritative(string timerId, ScenarioTimeDirection direction, double startSeconds, double targetSeconds)
```

#### Parameters

`timerId` string

`direction` [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

`startSeconds` double

`targetSeconds` double

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_HideAuthoritative"></a> HideAuthoritative\(\)

```csharp
public static void HideAuthoritative()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_PauseAuthoritative_System_String_"></a> PauseAuthoritative\(string\)

```csharp
public static void PauseAuthoritative(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_RemoveAuthoritative_System_String_"></a> RemoveAuthoritative\(string\)

```csharp
public static void RemoveAuthoritative(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_ResumeAuthoritative_System_String_"></a> ResumeAuthoritative\(string\)

```csharp
public static void ResumeAuthoritative(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_SetAuthoritative_System_String_System_Double_System_Boolean_System_Double_"></a> SetAuthoritative\(string, double, bool, double\)

```csharp
public static void SetAuthoritative(string timerId, double displaySeconds, bool hasNewTarget, double newTargetSeconds)
```

#### Parameters

`timerId` string

`displaySeconds` double

`hasNewTarget` bool

카운트다운 목표(총) 시간을 재설정할지 여부.

`newTargetSeconds` double

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_ShowAuthoritative_System_String_"></a> ShowAuthoritative\(string\)

```csharp
public static void ShowAuthoritative(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_StartAuthoritative_System_String_"></a> StartAuthoritative\(string\)

```csharp
public static void StartAuthoritative(string timerId)
```

#### Parameters

`timerId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeRelay_StopAuthoritative_System_String_"></a> StopAuthoritative\(string\)

```csharp
public static void StopAuthoritative(string timerId)
```

#### Parameters

`timerId` string

