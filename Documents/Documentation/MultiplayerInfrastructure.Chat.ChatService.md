# <a id="MultiplayerInfrastructure_Chat_ChatService"></a> Class ChatService

Namespace: [MultiplayerInfrastructure.Chat](MultiplayerInfrastructure.Chat.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class ChatService : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Chat_ChatService_CommandService"></a> CommandService

```csharp
public ChatCommandService CommandService { get; }
```

#### Property Value

 [ChatCommandService](MultiplayerInfrastructure.Command.ChatCommandService.md)

## Methods

### <a id="MultiplayerInfrastructure_Chat_ChatService_BroadcastSystemMessage_System_String_"></a> BroadcastSystemMessage\(string\)

서버에서 모든 접속자에게 시스템 메시지를 채팅으로 전파합니다.

```csharp
public void BroadcastSystemMessage(string message)
```

#### Parameters

`message` string

### <a id="MultiplayerInfrastructure_Chat_ChatService_GetDisplayName_FishNet_Connection_NetworkConnection_"></a> GetDisplayName\(NetworkConnection\)

```csharp
public string GetDisplayName(NetworkConnection conn)
```

#### Parameters

`conn` NetworkConnection

#### Returns

 string

### <a id="MultiplayerInfrastructure_Chat_ChatService_GetLastProblemSheetGradeCode_FishNet_Connection_NetworkConnection_"></a> GetLastProblemSheetGradeCode\(NetworkConnection\)

```csharp
public int GetLastProblemSheetGradeCode(NetworkConnection sender)
```

#### Parameters

`sender` NetworkConnection

#### Returns

 int

### <a id="MultiplayerInfrastructure_Chat_ChatService_ReportProblemAnswer_System_String_System_Int32_System_Int32_System_String_"></a> ReportProblemAnswer\(string, int, int, string\)

```csharp
public void ReportProblemAnswer(string problemSetIdentifier, int problemIndex, int selectedChoiceIndex, string shortAnswer)
```

#### Parameters

`problemSetIdentifier` string

`problemIndex` int

`selectedChoiceIndex` int

`shortAnswer` string

### <a id="MultiplayerInfrastructure_Chat_ChatService_SendSystemMessage_FishNet_Connection_NetworkConnection_System_String_"></a> SendSystemMessage\(NetworkConnection, string\)

```csharp
public void SendSystemMessage(NetworkConnection conn, string message)
```

#### Parameters

`conn` NetworkConnection

`message` string

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchActionbar_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_String_System_String__"></a> TryDispatchActionbar\(IEnumerable<NetworkConnection\>, string, out string\)

```csharp
public bool TryDispatchActionbar(IEnumerable<NetworkConnection> targets, string actionbar, out string error)
```

#### Parameters

`targets` IEnumerable<NetworkConnection\>

`actionbar` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchProblemSheet_System_String_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_Int32_System_Boolean_System_String__"></a> TryDispatchProblemSheet\(string, IEnumerable<NetworkConnection\>, int, bool, out string\)

```csharp
public bool TryDispatchProblemSheet(string problemSetIdentifier, IEnumerable<NetworkConnection> targets, int startIndex, bool singleProblemMode, out string error)
```

#### Parameters

`problemSetIdentifier` string

`targets` IEnumerable<NetworkConnection\>

`startIndex` int

`singleProblemMode` bool

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchScenario_System_String_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_String__"></a> TryDispatchScenario\(string, IEnumerable<NetworkConnection\>, out string\)

```csharp
public bool TryDispatchScenario(string scenarioIdentifier, IEnumerable<NetworkConnection> targets, out string error)
```

#### Parameters

`scenarioIdentifier` string

`targets` IEnumerable<NetworkConnection\>

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchSubtitle_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_String_System_String__"></a> TryDispatchSubtitle\(IEnumerable<NetworkConnection\>, string, out string\)

```csharp
public bool TryDispatchSubtitle(IEnumerable<NetworkConnection> targets, string subtitle, out string error)
```

#### Parameters

`targets` IEnumerable<NetworkConnection\>

`subtitle` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchTitle_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_String_System_String_System_String__"></a> TryDispatchTitle\(IEnumerable<NetworkConnection\>, string, string, out string\)

```csharp
public bool TryDispatchTitle(IEnumerable<NetworkConnection> targets, string title, string subtitle, out string error)
```

#### Parameters

`targets` IEnumerable<NetworkConnection\>

`title` string

`subtitle` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchTitleClear_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_String__"></a> TryDispatchTitleClear\(IEnumerable<NetworkConnection\>, out string\)

```csharp
public bool TryDispatchTitleClear(IEnumerable<NetworkConnection> targets, out string error)
```

#### Parameters

`targets` IEnumerable<NetworkConnection\>

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchTitleReset_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_String__"></a> TryDispatchTitleReset\(IEnumerable<NetworkConnection\>, out string\)

```csharp
public bool TryDispatchTitleReset(IEnumerable<NetworkConnection> targets, out string error)
```

#### Parameters

`targets` IEnumerable<NetworkConnection\>

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryDispatchTitleTimes_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__System_Int32_System_Int32_System_Int32_System_String__"></a> TryDispatchTitleTimes\(IEnumerable<NetworkConnection\>, int, int, int, out string\)

```csharp
public bool TryDispatchTitleTimes(IEnumerable<NetworkConnection> targets, int fadeInTicks, int stayTicks, int fadeOutTicks, out string error)
```

#### Parameters

`targets` IEnumerable<NetworkConnection\>

`fadeInTicks` int

`stayTicks` int

`fadeOutTicks` int

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryExecuteSystemCommand_System_String_System_String__"></a> TryExecuteSystemCommand\(string, out string\)

```csharp
public bool TryExecuteSystemCommand(string commandLine, out string result)
```

#### Parameters

`commandLine` string

`result` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TryExecuteSystemCommand_System_String_FishNet_Connection_NetworkConnection_System_String__"></a> TryExecuteSystemCommand\(string, NetworkConnection, out string\)

서버 권한으로 커맨드를 실행한다. <code class="paramref">executionContext</code>는 대상 셀렉터(@s 등)
해결에만 사용되며, 권한 검사는 수행되지 않는다(시스템 권한 실행). 실행 중 커맨드 정의가
발생시킨 시스템 메시지는 플레이어 채팅창이 아닌 서버 로그로 기록된다.
시나리오 ExecuteCommand 노드처럼 "서버가 특정 플레이어를 대신해" 실행하는 경우
해당 플레이어의 연결을 컨텍스트로 전달한다.

```csharp
public bool TryExecuteSystemCommand(string commandLine, NetworkConnection executionContext, out string result)
```

#### Parameters

`commandLine` string

`executionContext` NetworkConnection

`result` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TrySetDebugIntCprPlayingEscapeKeyServer_System_Boolean_"></a> TrySetDebugIntCprPlayingEscapeKeyServer\(bool\)

CPR 디버그 Escape 규칙을 서버와 모든 관찰 클라이언트에 동일하게 적용한다.
게임룰 명령은 서버에서만 실행되지만 Escape 입력은 각 소유 클라이언트가 판정하므로,
static 값만 변경해서는 원격 디버그 플레이어에게 규칙이 전달되지 않는다.

```csharp
public bool TrySetDebugIntCprPlayingEscapeKeyServer(bool enabled)
```

#### Parameters

`enabled` bool

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Chat_ChatService_TrySetValidatorBlockLogTargets_MultiplayerInfrastructure_Scenario_ScenarioValidatorBlockLogTarget_System_String__"></a> TrySetValidatorBlockLogTargets\(ScenarioValidatorBlockLogTarget, out string\)

```csharp
public bool TrySetValidatorBlockLogTargets(ScenarioValidatorBlockLogTarget targets, out string error)
```

#### Parameters

`targets` [ScenarioValidatorBlockLogTarget](MultiplayerInfrastructure.Scenario.ScenarioValidatorBlockLogTarget.md)

`error` string

#### Returns

 bool

