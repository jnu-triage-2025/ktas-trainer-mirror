# <a id="MultiplayerInfrastructure_Command_UsageLine"></a> Struct UsageLine

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

A single usage row: a syntax fragment (subcommand + arguments) on the
left and its explanation on the right. Rendered as an aligned two-column
row under the command name.

```csharp
public readonly struct UsageLine
```

## Constructors

### <a id="MultiplayerInfrastructure_Command_UsageLine__ctor_System_String_System_String_"></a> UsageLine\(string, string\)

```csharp
public UsageLine(string syntax, string description)
```

#### Parameters

`syntax` string

`description` string

### <a id="MultiplayerInfrastructure_Command_UsageLine__ctor_System_String_"></a> UsageLine\(string\)

```csharp
public UsageLine(string syntax)
```

#### Parameters

`syntax` string

## Fields

### <a id="MultiplayerInfrastructure_Command_UsageLine_Description"></a> Description

Right column explanation. May be empty.

```csharp
public readonly string Description
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Command_UsageLine_Syntax"></a> Syntax

Left column, e.g. "add &lt;target&gt; &lt;tag&gt;".

```csharp
public readonly string Syntax
```

#### Field Value

 string

