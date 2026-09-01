# <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset"></a> Class CommandDefinition\_EntityPreset

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class CommandDefinition_EntityPreset : IChatCommandModel, IChatCommandPipelineCommand, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_EntityPreset](MultiplayerInfrastructure.Command.CommandDefinition\_EntityPreset.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandPipelineCommand](MultiplayerInfrastructure.Command.IChatCommandPipelineCommand.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset__ctor_MultiplayerInfrastructure_Chat_ChatService_"></a> CommandDefinition\_EntityPreset\(ChatService\)

```csharp
public CommandDefinition_EntityPreset(ChatService chat)
```

#### Parameters

`chat` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_EntityPreset_TryExecute_FishNet_Connection_NetworkConnection_System_String___System_Boolean_System_Collections_Generic_IReadOnlyList_System_String___System_String__"></a> TryExecute\(NetworkConnection, string\[\], bool, out IReadOnlyList<string\>, out string\)

```csharp
public bool TryExecute(NetworkConnection sender, string[] args, bool suppressSystemMessages, out IReadOnlyList<string> pipelineValues, out string error)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

`suppressSystemMessages` bool

`pipelineValues` IReadOnlyList<string\>

`error` string

#### Returns

 bool

