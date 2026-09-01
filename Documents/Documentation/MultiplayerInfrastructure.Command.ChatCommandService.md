# <a id="MultiplayerInfrastructure_Command_ChatCommandService"></a> Class ChatCommandService

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class ChatCommandService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ChatCommandService](MultiplayerInfrastructure.Command.ChatCommandService.md)

## Methods

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_GetCommands"></a> GetCommands\(\)

```csharp
public IEnumerable<IChatCommandModel> GetCommands()
```

#### Returns

 IEnumerable<[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md)\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_Initialize_MultiplayerInfrastructure_Chat_ChatService_"></a> Initialize\(ChatService\)

```csharp
public void Initialize(ChatService manager)
```

#### Parameters

`manager` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_RegisterAlias_System_String_System_String_System_String_"></a> RegisterAlias\(string, string, string\)

```csharp
public void RegisterAlias(string alias, string target, string ownerId = null)
```

#### Parameters

`alias` string

`target` string

`ownerId` string

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_TryExecute_System_String_System_String___FishNet_Connection_NetworkConnection_"></a> TryExecute\(string, string\[\], NetworkConnection\)

```csharp
public bool TryExecute(string commandName, string[] args, NetworkConnection sender)
```

#### Parameters

`commandName` string

`args` string\[\]

`sender` NetworkConnection

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_TryExecute_System_String_System_String___FishNet_Connection_NetworkConnection_System_Boolean_System_Collections_Generic_IReadOnlyList_System_String___System_String__"></a> TryExecute\(string, string\[\], NetworkConnection, bool, out IReadOnlyList<string\>, out string\)

```csharp
public bool TryExecute(string commandName, string[] args, NetworkConnection sender, bool suppressSystemMessages, out IReadOnlyList<string> pipelineValues, out string error)
```

#### Parameters

`commandName` string

`args` string\[\]

`sender` NetworkConnection

`suppressSystemMessages` bool

`pipelineValues` IReadOnlyList<string\>

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_TryExecute_System_String_System_String___FishNet_Connection_NetworkConnection_System_Boolean_System_Boolean_System_Collections_Generic_IReadOnlyList_System_String___System_String__"></a> TryExecute\(string, string\[\], NetworkConnection, bool, bool, out IReadOnlyList<string\>, out string\)

커맨드를 실행한다. <code class="paramref">bypassPermissionCheck</code>가 true면 sender 컨텍스트는
대상 셀렉터(@s 등) 해결과 메시지 라우팅에만 사용되고 권한 검사는 건너뛴다
(서버/시나리오 등 시스템 권한 실행).

```csharp
public bool TryExecute(string commandName, string[] args, NetworkConnection sender, bool suppressSystemMessages, bool bypassPermissionCheck, out IReadOnlyList<string> pipelineValues, out string error)
```

#### Parameters

`commandName` string

`args` string\[\]

`sender` NetworkConnection

`suppressSystemMessages` bool

`bypassPermissionCheck` bool

`pipelineValues` IReadOnlyList<string\>

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_TryGetCommand_System_String_MultiplayerInfrastructure_Command_IChatCommandModel__"></a> TryGetCommand\(string, out IChatCommandModel\)

이름으로 등록된 명령어를 조회합니다.
Tab 자동완성 서비스(<xref href="MultiplayerInfrastructure.Command.ChatCommandCompletionService" data-throw-if-not-resolved="false"></xref>)에서 사용합니다.

```csharp
public bool TryGetCommand(string name, out IChatCommandModel command)
```

#### Parameters

`name` string

`command` [IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Command_ChatCommandService_UnregisterAlias_System_String_System_String_"></a> UnregisterAlias\(string, string\)

```csharp
public void UnregisterAlias(string alias, string ownerId)
```

#### Parameters

`alias` string

`ownerId` string

