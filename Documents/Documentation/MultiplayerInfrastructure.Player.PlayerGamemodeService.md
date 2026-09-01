# <a id="MultiplayerInfrastructure_Player_PlayerGamemodeService"></a> Class PlayerGamemodeService

Namespace: [MultiplayerInfrastructure.Player](MultiplayerInfrastructure.Player.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class PlayerGamemodeService
```

#### Inheritance

object ← 
[PlayerGamemodeService](MultiplayerInfrastructure.Player.PlayerGamemodeService.md)

## Methods

### <a id="MultiplayerInfrastructure_Player_PlayerGamemodeService_GetGamemode_FishNet_Connection_NetworkConnection_"></a> GetGamemode\(NetworkConnection\)

```csharp
public static PlayerGamemode GetGamemode(NetworkConnection conn)
```

#### Parameters

`conn` NetworkConnection

#### Returns

 [PlayerGamemode](MultiplayerInfrastructure.Player.PlayerGamemode.md)

### <a id="MultiplayerInfrastructure_Player_PlayerGamemodeService_RegisterPlayer_MultiplayerInfrastructure_Player_PlayerController_"></a> RegisterPlayer\(PlayerController\)

```csharp
public static void RegisterPlayer(PlayerController controller)
```

#### Parameters

`controller` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_Player_PlayerGamemodeService_TrySetGamemode_FishNet_Connection_NetworkConnection_MultiplayerInfrastructure_Player_PlayerController_MultiplayerInfrastructure_Player_PlayerGamemode_System_String__"></a> TrySetGamemode\(NetworkConnection, PlayerController, PlayerGamemode, out string\)

```csharp
public static bool TrySetGamemode(NetworkConnection _, PlayerController target, PlayerGamemode mode, out string error)
```

#### Parameters

`_` NetworkConnection

`target` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`mode` [PlayerGamemode](MultiplayerInfrastructure.Player.PlayerGamemode.md)

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerGamemodeService_UnregisterPlayer_MultiplayerInfrastructure_Player_PlayerController_"></a> UnregisterPlayer\(PlayerController\)

```csharp
public static void UnregisterPlayer(PlayerController controller)
```

#### Parameters

`controller` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

