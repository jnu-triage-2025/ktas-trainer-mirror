# <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission"></a> Class CommandDefinition\_Permission

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

/permission 커맨드 — 권한(role/user) 관리.

서브커맨드 목록:
  role list
  role info &lt;role&gt;
  role add &lt;role&gt;
  role remove &lt;role&gt;
  role default [role]
  role set &lt;role&gt; perm add &lt;permission&gt;
  role set &lt;role&gt; perm remove &lt;permission&gt;
  role set &lt;role&gt; contains add &lt;role2&gt;
  role set &lt;role&gt; contains remove &lt;role2&gt;
  user get &lt;player&gt;
  user set &lt;player&gt; &lt;role&gt;
  reset

```csharp
public class CommandDefinition_Permission : IChatCommandModel, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_Permission](MultiplayerInfrastructure.Command.CommandDefinition\_Permission.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission__ctor_MultiplayerInfrastructure_Chat_ChatService_"></a> CommandDefinition\_Permission\(ChatService\)

```csharp
public CommandDefinition_Permission(ChatService chat)
```

#### Parameters

`chat` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Permission_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

