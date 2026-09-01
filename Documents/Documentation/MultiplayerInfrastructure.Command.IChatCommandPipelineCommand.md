# <a id="MultiplayerInfrastructure_Command_IChatCommandPipelineCommand"></a> Interface IChatCommandPipelineCommand

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public interface IChatCommandPipelineCommand
```

## Methods

### <a id="MultiplayerInfrastructure_Command_IChatCommandPipelineCommand_TryExecute_FishNet_Connection_NetworkConnection_System_String___System_Boolean_System_Collections_Generic_IReadOnlyList_System_String___System_String__"></a> TryExecute\(NetworkConnection, string\[\], bool, out IReadOnlyList<string\>, out string\)

```csharp
bool TryExecute(NetworkConnection sender, string[] args, bool suppressSystemMessages, out IReadOnlyList<string> pipelineValues, out string error)
```

#### Parameters

`sender` NetworkConnection

`args` string\[\]

`suppressSystemMessages` bool

`pipelineValues` IReadOnlyList<string\>

`error` string

#### Returns

 bool

