# API 레퍼런스: MultiplayerInfrastructure.Permission.PermissionService

> **네임스페이스:** `MultiplayerInfrastructure.Permission`  
> **유형:** 정적 클래스  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Permission/PermissionService.cs`

---

## 0. 개요

`PermissionService`는 인게임 커맨드 실행 권한을 관리하는 정적 서비스입니다.  
`permissions.json` 파일로 role 기반 권한(RBAC)을 구성하며, 서버 시작 시 파일을 로드하고 런타임에 변경 사항을 파일에 저장합니다.

---

## 1. 설계 원칙

- **서버 전용:** 권한 변경은 서버에서만 수행됩니다.
- **Role 기반:** 사용자는 하나의 role을 가지며, role은 다른 role을 상속(`contains`)할 수 있습니다.
- **파일 영속:** `permissions.json`에 변경 사항이 즉시 저장됩니다.
- **자동 초기화:** 파일 미존재 시 기본값(`user`/`operator` role)으로 자동 생성됩니다.

---

## 2. permissions.json 구조

```json
{
  "roles": ["user", "operator"],
  "defaultRole": "user",
  "roleDefinitions": [
    {
      "name": "user",
      "contains": [],
      "permissions": ["help", "gamemode", "give", "clean", "tag", "scoreboard", "scenario", ...]
    },
    {
      "name": "operator",
      "contains": ["user"],
      "permissions": ["permission", "log"]
    }
  ],
  "userRoles": [
    { "userIdentifier": "uuid-abc123", "role": "operator" }
  ]
}
```

**파일 경로:**
- 에디터: `{프로젝트 루트}/permissions.json`
- 빌드: `{실행 파일 디렉터리}/permissions.json`

---

## 3. 기본 Role 구성

| Role | 상속 | 포함 권한 |
|---|---|---|
| `user` | 없음 | help, gamemode, give, clean, tag, scoreboard, scenario, problemsheet, character, title, entitypreset, timesync, tp, kick |
| `operator` | `user` | permission, log (+ user의 모든 권한) |

---

## 4. Permission Identifier 형식

| 형식 | 설명 |
|---|---|
| `"command"` | 특정 최상위 커맨드 |
| `"command.sub"` | 특정 서브커맨드 |
| `"*"` | 모든 커맨드 (슈퍼유저) |
| `"command.*"` | command 하위 모든 identifier |

---

## 5. 공개 API

### 초기화

```csharp
// 파일 로드 (서버 시작 시 한 번 호출). 이미 로드되어 있으면 무시
PermissionService.EnsureLoaded()

// 현재 상태를 파일에 저장
PermissionService.Save()

// 기본값으로 초기화하고 저장
PermissionService.ResetToDefaults()
```

### 권한 확인

```csharp
// userIdentifier에게 permissionId 허용 여부 확인
// userIdentifier가 null(서버 콘솔)이면 항상 허용
bool HasPermission(string userIdentifier, string permissionId)
```

### 유저 Role 관리

```csharp
// 유저의 현재 role 조회 (미설정 시 default role 반환)
string GetUserRole(string userIdentifier)

// 유저의 role 변경 및 저장
bool SetUserRole(string userIdentifier, string roleName, out string error)
```

### Role CRUD

```csharp
// 모든 role 이름 목록 조회
IReadOnlyList<string> GetRoles()

// role의 권한 목록 조회
bool TryGetRoleDefinition(string roleName, out IReadOnlyList<string> permissions, out IReadOnlyList<string> contains, out string error)

// 새 role 추가
bool AddRole(string roleName, out string error)

// role 삭제 (default role 삭제 불가)
bool RemoveRole(string roleName, out string error)

// role의 권한/상속 목록 교체
bool SetRolePermissions(string roleName, IReadOnlyList<string> permissions, IReadOnlyList<string> contains, out string error)

// role에 단일 권한 추가
bool AddPermissionToRole(string roleName, string permissionId, out string error)

// role에서 단일 권한 제거
bool RemovePermissionFromRole(string roleName, string permissionId, out string error)

// default role 변경
bool SetDefaultRole(string roleName, out string error)

// 현재 default role 이름 반환
string GetDefaultRole()
```

### 사용 예시

```csharp
// 커맨드 핸들러에서 권한 확인
public void Execute(string userIdentifier, string[] args)
{
    if (!PermissionService.HasPermission(userIdentifier, "scenario"))
    {
        ChatService.Instance.SendError(userIdentifier, "권한이 없습니다.");
        return;
    }
    // 커맨드 실행
}

// 운영자 권한 부여
PermissionService.SetUserRole(userIdentifier, "operator", out var error);
```

---

## 6. 관련 문서

- [Commands.md](../guide/Commands.md) — 전체 커맨드 목록 및 사용법
- [MultiplayerInfrastructure.Chat.ChatService.md](./MultiplayerInfrastructure.Chat.ChatService.md) — 채팅 서비스 API
