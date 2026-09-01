# <a id="MultiplayerInfrastructure_Command_ChatCommandHelp"></a> Class ChatCommandHelp

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class ChatCommandHelp
```

#### Inheritance

object ← 
[ChatCommandHelp](MultiplayerInfrastructure.Command.ChatCommandHelp.md)

## Methods

### <a id="MultiplayerInfrastructure_Command_ChatCommandHelp_GetHelpPage_MultiplayerInfrastructure_Command_IChatCommandModel_"></a> GetHelpPage\(IChatCommandModel\)

Builds the full help page for a command:
  /command - summary
      syntax1     description1
      syntax2     description2
The left (syntax) column is auto-aligned so descriptions line up.
Falls back to the one-line Description when the command has no
structured usage.

```csharp
public static string GetHelpPage(IChatCommandModel command)
```

#### Parameters

`command` [IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Command_ChatCommandHelp_GetSummary_MultiplayerInfrastructure_Command_IChatCommandModel_"></a> GetSummary\(IChatCommandModel\)

Returns the short, single-line summary shown in /help listings.
Always collapses to the first non-empty line so listings stay clean
even if a command's Description accidentally contains line breaks.

```csharp
public static string GetSummary(IChatCommandModel command)
```

#### Parameters

`command` [IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Command_ChatCommandHelp_IsHelpFlag_System_String___"></a> IsHelpFlag\(string\[\]\)

True when the first argument is a help flag (-h, --help, /?, ?, help).

```csharp
public static bool IsHelpFlag(string[] args)
```

#### Parameters

`args` string\[\]

#### Returns

 bool

