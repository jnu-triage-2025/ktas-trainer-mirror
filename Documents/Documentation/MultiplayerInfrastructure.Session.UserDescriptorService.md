# <a id="MultiplayerInfrastructure_Session_UserDescriptorService"></a> Class UserDescriptorService

Namespace: [MultiplayerInfrastructure.Session](MultiplayerInfrastructure.Session.md)  
Assembly: Assembly\-CSharp.dll  

접속 중인 모든 플레이어의 UserDescriptor를 관리하는 정적 서비스.

서버와 모든 클라이언트에서 각자 로컬 사전을 유지합니다.
- 서버  : PlayerController.OnStartServer / OnStopServer 에서 자동 등록·해제됩니다.
- 클라이언트: PlayerController 스폰·디스폰 시 (IsOwner 여부 무관) 자동 등록·해제됩니다.
- SyncVar 변경 시 UpdateDisplayName()이 자동 호출되어 DisplayName이 최신으로 유지됩니다.

쿼리 방법
- 코드 내 엔티티 기준 : TryGetByIdentifier(uuid)
- FishNet 연결 기준   : TryGetByClientId(clientId)
- 플레이어 이름 기준  : TryGetByDisplayName(name)  ← 채팅 명령어 등 사람 입력용
- 전체 열거           : GetAll()

```csharp
public static class UserDescriptorService
```

#### Inheritance

object ← 
[UserDescriptorService](MultiplayerInfrastructure.Session.UserDescriptorService.md)

## Fields

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_MaxDisplayNameLength"></a> MaxDisplayNameLength

```csharp
public const int MaxDisplayNameLength = 32
```

#### Field Value

 int

## Methods

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_GetAll"></a> GetAll\(\)

등록된 모든 설명자를 반환합니다 (Identifier 키).

```csharp
public static IReadOnlyDictionary<string, UserDescriptor> GetAll()
```

#### Returns

 IReadOnlyDictionary<string, [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)\>

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_IsDisplayNameInUse_System_String_System_String_"></a> IsDisplayNameInUse\(string, string\)

```csharp
public static bool IsDisplayNameInUse(string displayName, string exceptIdentifier = null)
```

#### Parameters

`displayName` string

`exceptIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_Register_System_Int32_MultiplayerInfrastructure_Session_UserDescriptor_"></a> Register\(int, UserDescriptor\)

PlayerController 스폰 시 호출됩니다.

```csharp
public static void Register(int clientId, UserDescriptor descriptor)
```

#### Parameters

`clientId` int

`descriptor` [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_TryGetByClientId_System_Int32_MultiplayerInfrastructure_Session_UserDescriptor__"></a> TryGetByClientId\(int, out UserDescriptor\)

FishNet ClientId로 조회합니다. FishNet 레벨 처리에 사용.

```csharp
public static bool TryGetByClientId(int clientId, out UserDescriptor descriptor)
```

#### Parameters

`clientId` int

`descriptor` [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_TryGetByDisplayName_System_String_MultiplayerInfrastructure_Session_UserDescriptor__"></a> TryGetByDisplayName\(string, out UserDescriptor\)

DisplayName으로 조회합니다 (대소문자 무시). 채팅 명령어 등 사람 입력용.
동명 플레이어가 있을 경우 첫 번째를 반환한다. 명확한 식별이 필요하면 Identifier를 사용하세요.

```csharp
public static bool TryGetByDisplayName(string displayName, out UserDescriptor descriptor)
```

#### Parameters

`displayName` string

`descriptor` [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_TryGetByIdentifier_System_String_MultiplayerInfrastructure_Session_UserDescriptor__"></a> TryGetByIdentifier\(string, out UserDescriptor\)

Identifier(UUID)로 조회합니다. 코드 내 엔티티 기준 쿼리.

```csharp
public static bool TryGetByIdentifier(string identifier, out UserDescriptor descriptor)
```

#### Parameters

`identifier` string

`descriptor` [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_TryGetClientId_System_String_System_Int32__"></a> TryGetClientId\(string, out int\)

Identifier → ClientId 역방향 조회.

```csharp
public static bool TryGetClientId(string identifier, out int clientId)
```

#### Parameters

`identifier` string

`clientId` int

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_TryNormalizeDisplayName_System_String_System_String__System_String__"></a> TryNormalizeDisplayName\(string, out string, out string\)

```csharp
public static bool TryNormalizeDisplayName(string displayName, out string normalized, out string error)
```

#### Parameters

`displayName` string

`normalized` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_Unregister_System_String_"></a> Unregister\(string\)

PlayerController 디스폰 시 호출됩니다.

```csharp
public static void Unregister(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Session_UserDescriptorService_UpdateDisplayName_System_String_System_String_"></a> UpdateDisplayName\(string, string\)

SyncVar 변경 시 DisplayName을 최신으로 유지합니다.

```csharp
public static void UpdateDisplayName(string identifier, string newDisplayName)
```

#### Parameters

`identifier` string

`newDisplayName` string

