# <a id="MultiplayerInfrastructure_Session_SessionInformationModel"></a> Class SessionInformationModel

Namespace: [MultiplayerInfrastructure.Session](MultiplayerInfrastructure.Session.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public class SessionInformationModel
```

#### Inheritance

object ← 
[SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

## Constructors

### <a id="MultiplayerInfrastructure_Session_SessionInformationModel__ctor_System_String_System_UInt16_System_String_System_Nullable_System_DateTime__"></a> SessionInformationModel\(string, ushort, string?, DateTime?\)

```csharp
public SessionInformationModel(string address, ushort port, string? sessionName = null, DateTime? lastSeenUtc = null)
```

#### Parameters

`address` string

`port` ushort

`sessionName` string?

`lastSeenUtc` DateTime?

## Properties

### <a id="MultiplayerInfrastructure_Session_SessionInformationModel_Address"></a> Address

```csharp
public string Address { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Session_SessionInformationModel_LastSeenUtc"></a> LastSeenUtc

```csharp
public DateTime LastSeenUtc { get; set; }
```

#### Property Value

 DateTime

### <a id="MultiplayerInfrastructure_Session_SessionInformationModel_Name"></a> Name

```csharp
public string Name { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Session_SessionInformationModel_Port"></a> Port

```csharp
public ushort Port { get; set; }
```

#### Property Value

 ushort

## Methods

### <a id="MultiplayerInfrastructure_Session_SessionInformationModel_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

