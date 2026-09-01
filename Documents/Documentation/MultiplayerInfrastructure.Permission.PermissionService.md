# <a id="MultiplayerInfrastructure_Permission_PermissionService"></a> Class PermissionService

Namespace: [MultiplayerInfrastructure.Permission](MultiplayerInfrastructure.Permission.md)  
Assembly: Assembly\-CSharp.dll  

커맨드 권한 시스템.

permissions.json 파일을 서버 측에서 읽고 쓴다.
파일이 없으면 기본값으로 자동 생성한다.

## 구조
  roles        : 정의된 모든 role 이름 목록
  default      : 새 유저에게 자동 부여되는 role 이름
  permissions  : role 별 설정
    [roleName].contains    : 이 role이 상속할 다른 role 목록
    [roleName].permissions : 이 role에 직접 부여된 permission identifier 목록
  userRoles    : userIdentifier → roleName 매핑 (런타임 영속)

## Permission identifier 형식
  "command"          — 특정 최상위 커맨드
  "command.sub"      — 특정 서브커맨드
  "*"                — 모든 커맨드
  "command.*"        — command 하위 모든 identifier

## 기본 role
  user     : 대부분의 커맨드 사용 가능 (permission 관리 제외)
  operator : user 상속 + permission 관리 커맨드 사용 가능

```csharp
public static class PermissionService
```

#### Inheritance

object ← 
[PermissionService](MultiplayerInfrastructure.Permission.PermissionService.md)

## Methods

### <a id="MultiplayerInfrastructure_Permission_PermissionService_AddPermissionToRole_System_String_System_String_System_String__"></a> AddPermissionToRole\(string, string, out string\)

role에 단일 permission identifier를 추가한다.

```csharp
public static bool AddPermissionToRole(string roleName, string permissionId, out string error)
```

#### Parameters

`roleName` string

`permissionId` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_AddRole_System_String_System_String__"></a> AddRole\(string, out string\)

새 role을 추가한다.

```csharp
public static bool AddRole(string roleName, out string error)
```

#### Parameters

`roleName` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_EnsureLoaded"></a> EnsureLoaded\(\)

파일에서 설정을 로드한다. 파일이 없으면 기본값으로 생성.
서버 시작 시 한 번 호출하면 된다.

```csharp
public static void EnsureLoaded()
```

### <a id="MultiplayerInfrastructure_Permission_PermissionService_GetDefaultRole"></a> GetDefaultRole\(\)

현재 default role 이름을 반환한다.

```csharp
public static string GetDefaultRole()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_Permission_PermissionService_GetRoles"></a> GetRoles\(\)

등록된 모든 role 이름 목록을 반환한다.

```csharp
public static IReadOnlyList<string> GetRoles()
```

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Permission_PermissionService_GetUserRole_System_String_"></a> GetUserRole\(string\)

유저의 현재 role을 반환한다. 설정이 없으면 default role 반환.

```csharp
public static string GetUserRole(string userIdentifier)
```

#### Parameters

`userIdentifier` string

#### Returns

 string

### <a id="MultiplayerInfrastructure_Permission_PermissionService_HasPermission_System_String_System_String_"></a> HasPermission\(string, string\)

userIdentifier 에게 permissionId 가 허용되는지 확인한다.
sender == null(서버 콘솔) 또는 host는 항상 허용.

```csharp
public static bool HasPermission(string userIdentifier, string permissionId)
```

#### Parameters

`userIdentifier` string

`permissionId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_RemovePermissionFromRole_System_String_System_String_System_String__"></a> RemovePermissionFromRole\(string, string, out string\)

role에서 단일 permission identifier를 제거한다.

```csharp
public static bool RemovePermissionFromRole(string roleName, string permissionId, out string error)
```

#### Parameters

`roleName` string

`permissionId` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_RemoveRole_System_String_System_String__"></a> RemoveRole\(string, out string\)

role을 삭제한다. default role은 삭제할 수 없다.

```csharp
public static bool RemoveRole(string roleName, out string error)
```

#### Parameters

`roleName` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_ResetToDefaults"></a> ResetToDefaults\(\)

기본값으로 초기화하고 저장한다.

```csharp
public static void ResetToDefaults()
```

### <a id="MultiplayerInfrastructure_Permission_PermissionService_Save"></a> Save\(\)

현재 상태를 파일에 저장한다.

```csharp
public static void Save()
```

### <a id="MultiplayerInfrastructure_Permission_PermissionService_SetDefaultRole_System_String_System_String__"></a> SetDefaultRole\(string, out string\)

role의 default 설정을 변경한다.

```csharp
public static bool SetDefaultRole(string roleName, out string error)
```

#### Parameters

`roleName` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_SetRolePermissions_System_String_System_Collections_Generic_IReadOnlyList_System_String__System_Collections_Generic_IReadOnlyList_System_String__System_String__"></a> SetRolePermissions\(string, IReadOnlyList<string\>, IReadOnlyList<string\>, out string\)

role의 permission 목록을 교체한다.

```csharp
public static bool SetRolePermissions(string roleName, IReadOnlyList<string> permissions, IReadOnlyList<string> contains, out string error)
```

#### Parameters

`roleName` string

`permissions` IReadOnlyList<string\>

`contains` IReadOnlyList<string\>

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_SetUserRole_System_String_System_String_System_String__"></a> SetUserRole\(string, string, out string\)

유저의 role을 설정하고 저장한다.

```csharp
public static bool SetUserRole(string userIdentifier, string roleName, out string error)
```

#### Parameters

`userIdentifier` string

`roleName` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Permission_PermissionService_TryGetRoleDefinition_System_String_System_Collections_Generic_IReadOnlyList_System_String___System_Collections_Generic_IReadOnlyList_System_String___System_String__"></a> TryGetRoleDefinition\(string, out IReadOnlyList<string\>, out IReadOnlyList<string\>, out string\)

role의 권한 목록을 반환한다.

```csharp
public static bool TryGetRoleDefinition(string roleName, out IReadOnlyList<string> permissions, out IReadOnlyList<string> contains, out string error)
```

#### Parameters

`roleName` string

`permissions` IReadOnlyList<string\>

`contains` IReadOnlyList<string\>

`error` string

#### Returns

 bool

