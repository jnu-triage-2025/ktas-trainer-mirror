# <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys"></a> Class RegistryGlobalKeys

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class RegistryGlobalKeys
```

#### Inheritance

object ← 
[RegistryGlobalKeys](MultiplayerInfrastructure.Registry.RegistryGlobalKeys.md)

## Fields

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_DefaultCommonSpawnPoint"></a> DefaultCommonSpawnPoint

```csharp
public const string DefaultCommonSpawnPoint = "DefaultCommonSpawnPoint"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_IsDedicatedServer"></a> IsDedicatedServer

데디케이티드 서버(헤드리스) 모드로 실행 중인지 여부.
DedicatedServerRuntime이 등록하며, 부트스트랩 흐름에서 클라이언트 전용 처리를 건너뛰는 데 사용됩니다.

```csharp
public const string IsDedicatedServer = "IsDedicatedServer"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_IsOpeningServer"></a> IsOpeningServer

```csharp
public const string IsOpeningServer = "IsOpeningServer"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_LoadedFromIntroScene"></a> LoadedFromIntroScene

```csharp
public const string LoadedFromIntroScene = "LoadedFromIntroScene"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_SelectedDatapackIds"></a> SelectedDatapackIds

```csharp
public const string SelectedDatapackIds = "SelectedDatapackIds"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_SessionInformation"></a> SessionInformation

```csharp
public const string SessionInformation = "SessionInformation"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_UseLanDiscovery"></a> UseLanDiscovery

```csharp
public const string UseLanDiscovery = "UseLanDiscovery"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Registry_RegistryGlobalKeys_UserDisplayName"></a> UserDisplayName

IntroScene에서 플레이어가 입력한 DisplayName.
PlayerController 스폰 시 서버에 전달(CmdSetDisplayName)하는 데 사용됩니다.
개발용 씬에서 직접 실행 시 이 키는 존재하지 않으며, UUID 앞 8자리가 대신 사용됩니다.

```csharp
public const string UserDisplayName = "UserDisplayName"
```

#### Field Value

 string

