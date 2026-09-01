# <a id="MultiplayerInfrastructure_Session_LanDiscoveryService"></a> Class LanDiscoveryService

Namespace: [MultiplayerInfrastructure.Session](MultiplayerInfrastructure.Session.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class LanDiscoveryService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LanDiscoveryService](MultiplayerInfrastructure.Session.LanDiscoveryService.md)

## Properties

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_Instance"></a> Instance

```csharp
public static LanDiscoveryService Instance { get; }
```

#### Property Value

 [LanDiscoveryService](MultiplayerInfrastructure.Session.LanDiscoveryService.md)

## Methods

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_ClearDiscovered"></a> ClearDiscovered\(\)

```csharp
public void ClearDiscovered()
```

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_GetDiscoveredSessions"></a> GetDiscoveredSessions\(\)

```csharp
public List<SessionInformationModel> GetDiscoveredSessions()
```

#### Returns

 List<[SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)\>

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_HasPendingUpdate"></a> HasPendingUpdate\(\)

```csharp
public bool HasPendingUpdate()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_StartBroadcast_System_String_System_Int32_"></a> StartBroadcast\(string, int\)

```csharp
public void StartBroadcast(string sessionName, int gamePort)
```

#### Parameters

`sessionName` string

`gamePort` int

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_StartDiscovery"></a> StartDiscovery\(\)

```csharp
public void StartDiscovery()
```

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_StopBroadcast"></a> StopBroadcast\(\)

```csharp
public void StopBroadcast()
```

### <a id="MultiplayerInfrastructure_Session_LanDiscoveryService_StopDiscovery"></a> StopDiscovery\(\)

```csharp
public void StopDiscovery()
```

