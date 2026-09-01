# <a id="MultiplayerInfrastructure_Command_IChatCommandUsage"></a> Interface IChatCommandUsage

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

Optional interface for commands that expose structured, multi-line usage.
Rendered as:
  /command
      syntax1     description1
      syntax2     description2
When not implemented, help falls back to <xref href="MultiplayerInfrastructure.Command.IChatCommandModel.Description" data-throw-if-not-resolved="false"></xref>.

```csharp
public interface IChatCommandUsage
```

## Properties

### <a id="MultiplayerInfrastructure_Command_IChatCommandUsage_UsageLines"></a> UsageLines

The rows describing each subcommand/argument form of the command.

```csharp
IReadOnlyList<UsageLine> UsageLines { get; }
```

#### Property Value

 IReadOnlyList<[UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)\>

