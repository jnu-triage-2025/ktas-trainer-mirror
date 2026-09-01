# <a id="MultiplayerInfrastructure_FishNetSupports_PlayerSpawnPointRegistry"></a> Class PlayerSpawnPointRegistry

Namespace: [MultiplayerInfrastructure.FishNetSupports](MultiplayerInfrastructure.FishNetSupports.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class PlayerSpawnPointRegistry
```

#### Inheritance

object ← 
[PlayerSpawnPointRegistry](MultiplayerInfrastructure.FishNetSupports.PlayerSpawnPointRegistry.md)

## Methods

### <a id="MultiplayerInfrastructure_FishNetSupports_PlayerSpawnPointRegistry_Register_MultiplayerInfrastructure_FishNetSupports_IPlayerSpawnPointProvider_"></a> Register\(IPlayerSpawnPointProvider\)

```csharp
public static void Register(IPlayerSpawnPointProvider provider)
```

#### Parameters

`provider` [IPlayerSpawnPointProvider](MultiplayerInfrastructure.FishNetSupports.IPlayerSpawnPointProvider.md)

### <a id="MultiplayerInfrastructure_FishNetSupports_PlayerSpawnPointRegistry_TryGet_System_String_UnityEngine_Transform__"></a> TryGet\(string, out Transform\)

```csharp
public static bool TryGet(string identifier, out Transform spawnTransform)
```

#### Parameters

`identifier` string

`spawnTransform` Transform

#### Returns

 bool

### <a id="MultiplayerInfrastructure_FishNetSupports_PlayerSpawnPointRegistry_Unregister_MultiplayerInfrastructure_FishNetSupports_IPlayerSpawnPointProvider_"></a> Unregister\(IPlayerSpawnPointProvider\)

```csharp
public static void Unregister(IPlayerSpawnPointProvider provider)
```

#### Parameters

`provider` [IPlayerSpawnPointProvider](MultiplayerInfrastructure.FishNetSupports.IPlayerSpawnPointProvider.md)

### <a id="MultiplayerInfrastructure_FishNetSupports_PlayerSpawnPointRegistry_Changed"></a> Changed

```csharp
public static event Action Changed
```

#### Event Type

 Action

