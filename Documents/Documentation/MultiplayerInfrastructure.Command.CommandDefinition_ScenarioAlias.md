# <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias"></a> Class CommandDefinition\_ScenarioAlias

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class CommandDefinition_ScenarioAlias : IChatCommandModel, IChatCommandUsage
```

#### Inheritance

object ← 
[CommandDefinition\_ScenarioAlias](MultiplayerInfrastructure.Command.CommandDefinition\_ScenarioAlias.md)

#### Implements

[IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md), 
[IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias__ctor_MultiplayerInfrastructure_Command_CommandDefinition_Scenario_"></a> CommandDefinition\_ScenarioAlias\(CommandDefinition\_Scenario\)

```csharp
public CommandDefinition_ScenarioAlias(CommandDefinition_Scenario scenario)
```

#### Parameters

`scenario` [CommandDefinition\_Scenario](MultiplayerInfrastructure.Command.CommandDefinition\_Scenario.md)

## Properties

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias_CommandEntry"></a> CommandEntry

```csharp
public string CommandEntry { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias_Description"></a> Description

One-line human readable description used by /help listings.
Keep this short: a single sentence, no line breaks.

```csharp
public string Description { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias_PermissionIdentifier"></a> PermissionIdentifier

이 커맨드 최상위에 필요한 permission identifier.
예: "scenario" → "scenario" 권한이 있어야 실행 가능.
PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
null 또는 empty 이면 모든 유저가 실행 가능.

```csharp
public string PermissionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
public IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Command_CommandDefinition_ScenarioAlias_Execute_FishNet_Connection_NetworkConnection_System_String___"></a> Execute\(NetworkConnection, string\[\]\)

```csharp
public void Execute(NetworkConnection sender, string[] args)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

