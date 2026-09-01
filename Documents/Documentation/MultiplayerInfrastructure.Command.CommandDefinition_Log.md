# <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log"></a> Class CommandDefinition\_Log

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

/log 커맨드 - 게임 로그 파일 관리.

서브커맨드:
  /log folder          — 로그 폴더를 OS 파일 탐색기로 연다
  /log list            — 저장된 로그 파일 목록을 채팅에 출력
  /log export [path]   — 현재 세션 로그를 텍스트 파일로 내보내기
                         (path 생략 시 로그 폴더에 -export.log 접미사로 저장)
  /log flush           — 현재 로그 버퍼를 강제 플러시
  /log info            — 현재 세션/로그 파일 정보 출력

```csharp
public class CommandDefinition_Log : IChatCommandModel, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_Log](MultiplayerInfrastructure.Command.CommandDefinition\_Log.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log__ctor_MultiplayerInfrastructure_Chat_ChatService_"></a> CommandDefinition\_Log\(ChatService\)

```csharp
public CommandDefinition_Log(ChatService chat)
```

#### Parameters

`chat` [ChatService](MultiplayerInfrastructure.Chat.ChatService.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_Log_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

