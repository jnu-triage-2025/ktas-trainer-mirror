# <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp"></a> Class CommandDefinition\_Tp

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

/tp 커맨드 — 순간이동.

지원하는 형태:
  /tp x y z                   — 자기 자신을 (x, y, z) 로 이동
  /tp ~x ~y ~z                — 자기 자신의 현재 위치를 기준으로 이동
  /tp &lt;player&gt; x y z          — player 를 (x, y, z) 로 이동
  /tp &lt;player&gt;                — 자기 자신을 player 위치로 이동
  /tp &lt;player1&gt; &lt;player2&gt;      — player1 을 player2 위치로 이동
  /tp &lt;waypoint&gt;              — 자기 자신을 waypoint 위치로 이동
  /tp &lt;player&gt; &lt;waypoint&gt;     — player 를 waypoint 위치로 이동

```csharp
public class CommandDefinition_Tp : IChatCommandModel, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_Tp](MultiplayerInfrastructure.Command.CommandDefinition\_Tp.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp__ctor_MultiplayerInfrastructure_Chat_ChatService_"></a> CommandDefinition\_Tp\(ChatService\)

```csharp
public CommandDefinition_Tp(ChatService chat)
```

#### Parameters

`chat` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Tp_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

