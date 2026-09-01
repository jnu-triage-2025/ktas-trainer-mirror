# <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give"></a> Class CommandDefinition\_Give

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class CommandDefinition_Give : IChatCommandModel, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_Give](MultiplayerInfrastructure.Command.CommandDefinition\_Give.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give__ctor_MultiplayerInfrastructure_Chat_ChatService_"></a> CommandDefinition\_Give\(ChatService\)

```csharp
public CommandDefinition_Give(ChatService chat)
```

#### Parameters

`chat` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Give_TryExecuteGive_FishNet_Connection_NetworkConnection_System_String___System_String__"></a> TryExecuteGive\(NetworkConnection, string\[\], out string\)

아이템 지급의 도메인 로직. 채팅 명령과 시나리오 노드가 동일한 검증·인벤토리·초과분 드롭
처리를 사용하도록, UI/권한/채팅 전송과 분리한다.

```csharp
public static bool TryExecuteGive(NetworkConnection sender, string[] args, out string message)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

`message` string

#### Returns

 bool

