# <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync"></a> Class CommandDefinition\_TimeSync

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

시간 표시(스톱워치/카운트다운)의 주기적 재동기화 밀도를 조정하는 명령어.

밀도는 tick / ms / seconds 단위로 지정하며, 기본값은 1초에 1회이다.
이 설정은 서버 권위이므로 서버(호스트/콘솔)에서만 조정할 수 있다.
(모든 명령 실행은 서버에서 수행되며, PermissionService 권한으로 접근을 제한한다.)

```csharp
public class CommandDefinition_TimeSync : IChatCommandModel, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_TimeSync](MultiplayerInfrastructure.Command.CommandDefinition\_TimeSync.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync__ctor_MultiplayerInfrastructure_Chat_ChatService_"></a> CommandDefinition\_TimeSync\(ChatService\)

```csharp
public CommandDefinition_TimeSync(ChatService chat)
```

#### Parameters

`chat` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_TimeSync_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

