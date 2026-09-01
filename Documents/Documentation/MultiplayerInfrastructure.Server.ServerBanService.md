# <a id="MultiplayerInfrastructure_Server_ServerBanService"></a> Class ServerBanService

Namespace: [MultiplayerInfrastructure.Server](MultiplayerInfrastructure.Server.md)  
Assembly: Assembly\-CSharp.dll  

서버 재시작 후에도 유지되는 표시 이름 기반 차단 목록입니다.

```csharp
public static class ServerBanService
```

#### Inheritance

object ← 
[ServerBanService](MultiplayerInfrastructure.Server.ServerBanService.md)

## Methods

### <a id="MultiplayerInfrastructure_Server_ServerBanService_Ban_System_String_"></a> Ban\(string\)

```csharp
public static bool Ban(string displayName)
```

#### Parameters

`displayName` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Server_ServerBanService_EnsureLoaded"></a> EnsureLoaded\(\)

```csharp
public static void EnsureLoaded()
```

### <a id="MultiplayerInfrastructure_Server_ServerBanService_GetBanList"></a> GetBanList\(\)

```csharp
public static IReadOnlyList<string> GetBanList()
```

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Server_ServerBanService_IsBanned_System_String_"></a> IsBanned\(string\)

```csharp
public static bool IsBanned(string displayName)
```

#### Parameters

`displayName` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Server_ServerBanService_Unban_System_String_"></a> Unban\(string\)

```csharp
public static bool Unban(string displayName)
```

#### Parameters

`displayName` string

#### Returns

 bool

