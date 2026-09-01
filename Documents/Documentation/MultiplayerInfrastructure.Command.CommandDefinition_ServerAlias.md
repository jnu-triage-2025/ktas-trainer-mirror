# <a id="MultiplayerInfrastructure_Command_CommandDefinition_ServerAlias"></a> Class CommandDefinition\_ServerAlias

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class CommandDefinition_ServerAlias : IChatCommandModel
```

#### Inheritance

object ← 
[CommandDefinition\_ServerAlias](MultiplayerInfrastructure.Command.CommandDefinition\_ServerAlias.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ServerAlias__ctor_MultiplayerInfrastructure_Command_CommandDefinition_Server_System_String_System_String_"></a> CommandDefinition\_ServerAlias\(CommandDefinition\_Server, string, string\)

```csharp
public CommandDefinition_ServerAlias(CommandDefinition_Server server, string alias, string subcommand)
```

#### Parameters

`server` [CommandDefinition\_Server](MultiplayerInfrastructure.Command.CommandDefinition\_Server.md)

`alias` string

`subcommand` string

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ServerAlias_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ServerAlias_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ServerAlias_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ServerAlias_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

