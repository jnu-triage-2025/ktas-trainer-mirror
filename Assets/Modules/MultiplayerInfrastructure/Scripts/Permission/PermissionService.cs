using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerInfrastructure.Permission
{
  /// <summary>
  /// 커맨드 권한 시스템.
  ///
  /// permissions.json 파일을 서버 측에서 읽고 쓴다.
  /// 파일이 없으면 기본값으로 자동 생성한다.
  ///
  /// ## 구조
  ///   roles        : 정의된 모든 role 이름 목록
  ///   default      : 새 유저에게 자동 부여되는 role 이름
  ///   permissions  : role 별 설정
  ///     [roleName].contains    : 이 role이 상속할 다른 role 목록
  ///     [roleName].permissions : 이 role에 직접 부여된 permission identifier 목록
  ///   userRoles    : userIdentifier → roleName 매핑 (런타임 영속)
  ///
  /// ## Permission identifier 형식
  ///   "command"          — 특정 최상위 커맨드
  ///   "command.sub"      — 특정 서브커맨드
  ///   "*"                — 모든 커맨드
  ///   "command.*"        — command 하위 모든 identifier
  ///
  /// ## 기본 role
  ///   user     : 대부분의 커맨드 사용 가능 (permission 관리 제외)
  ///   operator : user 상속 + permission 관리 커맨드 사용 가능
  /// </summary>
  public static class PermissionService
  {
    // ── 내부 모델 ─────────────────────────────────────────────────────────────

    [Serializable]
    private class RoleDefinition
    {
      public List<string> contains = new();
      public List<string> permissions = new();
    }

    [Serializable]
    private class PermissionsFile
    {
      public List<string> roles = new();
      public string @default = "user";
      public Dictionary<string, RoleDefinition> permissions = new(StringComparer.OrdinalIgnoreCase);
      public Dictionary<string, string> userRoles = new(StringComparer.OrdinalIgnoreCase);
    }

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private static PermissionsFile _file = new();
    private static bool _loaded = false;

    // ── 파일 경로 ─────────────────────────────────────────────────────────────

    private static string FilePath
    {
      get
      {
#if UNITY_EDITOR
        // 에디터: 프로젝트 루트/permissions.json
        return Path.Combine(Application.dataPath, "..", "permissions.json");
#else
        // 빌드: 실행 파일 옆 permissions.json (서버 사이드)
        return Path.Combine(Application.dataPath, "..", "permissions.json");
#endif
      }
    }

    // ── 기본값 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 코드에 하드코딩된 기본 permissions 설정을 반환한다.
    /// permissions.json이 없을 때 이 값으로 파일을 생성한다.
    /// </summary>
    private static PermissionsFile BuildDefaultFile()
    {
      var file = new PermissionsFile
      {
        roles = new List<string> { "user", "operator" },
        @default = "user",
        permissions = new Dictionary<string, RoleDefinition>(StringComparer.OrdinalIgnoreCase),
        userRoles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
      };

      // user role: permission 관리 제외한 모든 커맨드
      file.permissions["user"] = new RoleDefinition
      {
        contains = new List<string>(),
        permissions = new List<string>
        {
          "help",
          "gamemode",
          "speed",
          "give",
          "clean",
          "tag",
          "scoreboard",
          "scenario",
          "problemsheet",
          "character",
          "title",
          "entitypreset",
          "timesync",
          "tp",
          "kick",
        },
      };

      // operator role: user 상속 + permission 관리 + log 관리
      file.permissions["operator"] = new RoleDefinition
      {
        contains = new List<string> { "user" },
        permissions = new List<string>
        {
          "permission",
          "gamerule",
          "log",
          "server",
        },
      };

      return file;
    }

    // ── 초기화 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 파일에서 설정을 로드한다. 파일이 없으면 기본값으로 생성.
    /// 서버 시작 시 한 번 호출하면 된다.
    /// </summary>
    public static void EnsureLoaded()
    {
      if (_loaded)
        return;

      Load();
    }

    private static void Load()
    {
      string path = FilePath;
      if (!File.Exists(path))
      {
        Debug.Log($"[PermissionService] permissions.json not found at '{path}'. Creating default.");
        _file = BuildDefaultFile();
        Save();
        _loaded = true;
        return;
      }

      try
      {
        string json = File.ReadAllText(path);
        var loaded = JsonUtility.FromJson<PermissionsFileJson>(json);
        _file = ConvertFromJson(loaded);
        _loaded = true;
        Debug.Log($"[PermissionService] Loaded from '{path}'.");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[PermissionService] Failed to load '{path}': {ex.Message}. Using defaults.");
        _file = BuildDefaultFile();
        _loaded = true;
      }
    }

    /// <summary>현재 상태를 파일에 저장한다.</summary>
    public static void Save()
    {
      try
      {
        string path = FilePath;
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        string json = SerializeToJson(_file);
        File.WriteAllText(path, json);
        Debug.Log($"[PermissionService] Saved to '{path}'.");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[PermissionService] Failed to save: {ex.Message}");
      }
    }

    /// <summary>기본값으로 초기화하고 저장한다.</summary>
    public static void ResetToDefaults()
    {
      _file = BuildDefaultFile();
      Save();
      Debug.Log("[PermissionService] Reset to defaults.");
    }

    // ── Permission 체크 API ───────────────────────────────────────────────────

    /// <summary>
    /// userIdentifier 에게 permissionId 가 허용되는지 확인한다.
    /// sender == null(서버 콘솔) 또는 host는 항상 허용.
    /// </summary>
    public static bool HasPermission(string userIdentifier, string permissionId)
    {
      EnsureLoaded();

      if (string.IsNullOrWhiteSpace(permissionId))
        return true;

      string role = GetUserRole(userIdentifier);
      return RoleHasPermission(role, permissionId, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static bool RoleHasPermission(string role, string permissionId, HashSet<string> visited)
    {
      if (string.IsNullOrWhiteSpace(role))
        return false;

      if (visited.Contains(role))
        return false;

      visited.Add(role);

      if (!_file.permissions.TryGetValue(role, out var def))
        return false;

      // 직접 권한 목록 검사
      foreach (var p in def.permissions)
      {
        if (MatchesPermission(p, permissionId))
          return true;
      }

      // 상속된 role 검사
      foreach (var inherited in def.contains)
      {
        if (RoleHasPermission(inherited, permissionId, visited))
          return true;
      }

      return false;
    }

    /// <summary>
    /// pattern이 permissionId와 매칭되는지 확인한다.
    /// - "*"         : 모두 허용
    /// - "foo.*"     : "foo" 또는 "foo.xxx" 형식 허용
    /// - "foo"       : 정확히 "foo" 또는 "foo."로 시작하는 모든 하위 경로 허용
    /// </summary>
    private static bool MatchesPermission(string pattern, string permissionId)
    {
      if (string.IsNullOrWhiteSpace(pattern))
        return false;

      // 전체 와일드카드
      if (pattern == "*")
        return true;

      // prefix.* 형식
      if (pattern.EndsWith(".*", StringComparison.Ordinal))
      {
        string prefix = pattern.Substring(0, pattern.Length - 2);
        return string.Equals(permissionId, prefix, StringComparison.OrdinalIgnoreCase)
            || permissionId.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
      }

      // 정확히 일치하거나, pattern이 permissionId의 상위 경로인 경우
      // 예: pattern="foo"가 permissionId="foo.bar"를 허용
      if (string.Equals(permissionId, pattern, StringComparison.OrdinalIgnoreCase))
        return true;

      if (permissionId.StartsWith(pattern + ".", StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    }

    // ── 유저 Role API ──────────────────────────────────────────────────────────

    /// <summary>유저의 현재 role을 반환한다. 설정이 없으면 default role 반환.</summary>
    public static string GetUserRole(string userIdentifier)
    {
      EnsureLoaded();

      if (!string.IsNullOrWhiteSpace(userIdentifier)
          && _file.userRoles.TryGetValue(userIdentifier, out var role)
          && !string.IsNullOrWhiteSpace(role))
        return role;

      return _file.@default ?? "user";
    }

    /// <summary>유저의 role을 설정하고 저장한다.</summary>
    public static bool SetUserRole(string userIdentifier, string roleName, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(userIdentifier))
      {
        error = "User identifier is required.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(roleName))
      {
        error = "Role name is required.";
        return false;
      }

      if (!_file.permissions.ContainsKey(roleName))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      _file.userRoles[userIdentifier] = roleName;
      Save();
      return true;
    }

    // ── Role CRUD API ─────────────────────────────────────────────────────────

    /// <summary>등록된 모든 role 이름 목록을 반환한다.</summary>
    public static IReadOnlyList<string> GetRoles()
    {
      EnsureLoaded();
      return _file.roles;
    }

    /// <summary>role의 권한 목록을 반환한다.</summary>
    public static bool TryGetRoleDefinition(string roleName, out IReadOnlyList<string> permissions,
                                             out IReadOnlyList<string> contains, out string error)
    {
      EnsureLoaded();
      permissions = Array.Empty<string>();
      contains = Array.Empty<string>();
      error = string.Empty;

      if (!_file.permissions.TryGetValue(roleName, out var def))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      permissions = def.permissions;
      contains = def.contains;
      return true;
    }

    /// <summary>새 role을 추가한다.</summary>
    public static bool AddRole(string roleName, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(roleName))
      {
        error = "Role name is required.";
        return false;
      }

      if (_file.permissions.ContainsKey(roleName))
      {
        error = $"Role '{roleName}' already exists.";
        return false;
      }

      _file.permissions[roleName] = new RoleDefinition();
      if (!_file.roles.Contains(roleName))
        _file.roles.Add(roleName);

      Save();
      return true;
    }

    /// <summary>role을 삭제한다. default role은 삭제할 수 없다.</summary>
    public static bool RemoveRole(string roleName, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(roleName))
      {
        error = "Role name is required.";
        return false;
      }

      if (string.Equals(roleName, _file.@default, StringComparison.OrdinalIgnoreCase))
      {
        error = $"Cannot remove the default role '{roleName}'.";
        return false;
      }

      if (!_file.permissions.ContainsKey(roleName))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      _file.permissions.Remove(roleName);
      _file.roles.Remove(roleName);
      Save();
      return true;
    }

    /// <summary>role의 permission 목록을 교체한다.</summary>
    public static bool SetRolePermissions(string roleName, IReadOnlyList<string> permissions,
                                           IReadOnlyList<string> contains, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (!_file.permissions.TryGetValue(roleName, out var def))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      def.permissions = new List<string>(permissions ?? Array.Empty<string>());
      def.contains = new List<string>(contains ?? Array.Empty<string>());
      Save();
      return true;
    }

    /// <summary>role에 단일 permission identifier를 추가한다.</summary>
    public static bool AddPermissionToRole(string roleName, string permissionId, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (!_file.permissions.TryGetValue(roleName, out var def))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      if (def.permissions.Contains(permissionId))
      {
        error = $"Role '{roleName}' already has permission '{permissionId}'.";
        return false;
      }

      def.permissions.Add(permissionId);
      Save();
      return true;
    }

    /// <summary>role에서 단일 permission identifier를 제거한다.</summary>
    public static bool RemovePermissionFromRole(string roleName, string permissionId, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (!_file.permissions.TryGetValue(roleName, out var def))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      if (!def.permissions.Remove(permissionId))
      {
        error = $"Role '{roleName}' does not have permission '{permissionId}'.";
        return false;
      }

      Save();
      return true;
    }

    /// <summary>role의 default 설정을 변경한다.</summary>
    public static bool SetDefaultRole(string roleName, out string error)
    {
      EnsureLoaded();
      error = string.Empty;

      if (!_file.permissions.ContainsKey(roleName))
      {
        error = $"Role '{roleName}' does not exist.";
        return false;
      }

      _file.@default = roleName;
      Save();
      return true;
    }

    /// <summary>현재 default role 이름을 반환한다.</summary>
    public static string GetDefaultRole()
    {
      EnsureLoaded();
      return _file.@default ?? "user";
    }

    // ── JSON 직렬화 (JsonUtility 우회) ───────────────────────────────────────
    // JsonUtility 는 Dictionary 를 직접 지원하지 않으므로 수동으로 직렬화한다.

    [Serializable]
    private class RoleEntry
    {
      public string name;
      public List<string> contains = new();
      public List<string> permissions = new();
    }

    [Serializable]
    private class UserRoleEntry
    {
      public string userIdentifier;
      public string role;
    }

    [Serializable]
    private class PermissionsFileJson
    {
      public List<string> roles = new();
      public string defaultRole = "user";
      public List<RoleEntry> roleDefinitions = new();
      public List<UserRoleEntry> userRoles = new();
    }

    private static PermissionsFile ConvertFromJson(PermissionsFileJson src)
    {
      if (src == null)
        return BuildDefaultFile();

      var result = new PermissionsFile
      {
        roles = src.roles ?? new List<string>(),
        @default = src.defaultRole ?? "user",
        permissions = new Dictionary<string, RoleDefinition>(StringComparer.OrdinalIgnoreCase),
        userRoles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
      };

      if (src.roleDefinitions != null)
      {
        foreach (var entry in src.roleDefinitions)
        {
          if (string.IsNullOrWhiteSpace(entry?.name))
            continue;

          result.permissions[entry.name] = new RoleDefinition
          {
            contains = entry.contains ?? new List<string>(),
            permissions = entry.permissions ?? new List<string>(),
          };
        }
      }

      if (src.userRoles != null)
      {
        foreach (var entry in src.userRoles)
        {
          if (!string.IsNullOrWhiteSpace(entry?.userIdentifier) && !string.IsNullOrWhiteSpace(entry.role))
            result.userRoles[entry.userIdentifier] = entry.role;
        }
      }

      return result;
    }

    private static string SerializeToJson(PermissionsFile src)
    {
      var json = new PermissionsFileJson
      {
        roles = new List<string>(src.roles),
        defaultRole = src.@default,
        roleDefinitions = new List<RoleEntry>(),
        userRoles = new List<UserRoleEntry>(),
      };

      foreach (var kv in src.permissions)
      {
        json.roleDefinitions.Add(new RoleEntry
        {
          name = kv.Key,
          contains = new List<string>(kv.Value.contains),
          permissions = new List<string>(kv.Value.permissions),
        });
      }

      foreach (var kv in src.userRoles)
      {
        json.userRoles.Add(new UserRoleEntry
        {
          userIdentifier = kv.Key,
          role = kv.Value,
        });
      }

      return JsonUtility.ToJson(json, prettyPrint: true);
    }
  }
}
