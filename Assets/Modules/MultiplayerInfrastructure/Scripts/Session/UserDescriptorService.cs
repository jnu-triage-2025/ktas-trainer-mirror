using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Logging;
using UnityEngine;

namespace MultiplayerInfrastructure.Session
{
  /// <summary>
  /// 접속 중인 모든 플레이어의 UserDescriptor를 관리하는 정적 서비스.
  ///
  /// 서버와 모든 클라이언트에서 각자 로컬 사전을 유지합니다.
  /// - 서버  : PlayerController.OnStartServer / OnStopServer 에서 자동 등록·해제됩니다.
  /// - 클라이언트: PlayerController 스폰·디스폰 시 (IsOwner 여부 무관) 자동 등록·해제됩니다.
  /// - SyncVar 변경 시 UpdateDisplayName()이 자동 호출되어 DisplayName이 최신으로 유지됩니다.
  ///
  /// 쿼리 방법
  /// - 코드 내 엔티티 기준 : TryGetByIdentifier(uuid)
  /// - FishNet 연결 기준   : TryGetByClientId(clientId)
  /// - 플레이어 이름 기준  : TryGetByDisplayName(name)  ← 채팅 명령어 등 사람 입력용
  /// - 전체 열거           : GetAll()
  /// </summary>
  public static class UserDescriptorService
  {
    public const int MaxDisplayNameLength = 32;
    // Identifier(UUID) → UserDescriptor  (주 저장소)
    private static readonly Dictionary<string, UserDescriptor> _byIdentifier
      = new(System.StringComparer.Ordinal);

    // ClientId → Identifier
    private static readonly Dictionary<int, string> _identifierByClientId = new();

    // Identifier → ClientId
    private static readonly Dictionary<string, int> _clientIdByIdentifier
      = new(System.StringComparer.Ordinal);

    // ── 등록 / 해제 ──────────────────────────────────────────────────────────

    /// <summary>PlayerController 스폰 시 호출됩니다.</summary>
    public static void Register(int clientId, UserDescriptor descriptor)
    {
      if (descriptor == null)
        return;

      _byIdentifier[descriptor.Identifier] = descriptor;
      _identifierByClientId[clientId] = descriptor.Identifier;
      _clientIdByIdentifier[descriptor.Identifier] = clientId;

      Debug.Log($"[UserDescriptorService] Registered: clientId={clientId} → {descriptor}");
      GameLogService.WritePlayerJoin(
        $"Player joined: displayName={descriptor.DisplayName}, clientId={clientId}, uuid={descriptor.Identifier}",
        descriptor.Identifier);
    }

    /// <summary>PlayerController 디스폰 시 호출됩니다.</summary>
    public static void Unregister(string identifier)
    {
      if (string.IsNullOrEmpty(identifier) || !_byIdentifier.TryGetValue(identifier, out var descriptor))
        return;

      if (_clientIdByIdentifier.TryGetValue(identifier, out var clientId))
      {
        _identifierByClientId.Remove(clientId);
        _clientIdByIdentifier.Remove(identifier);
      }

      _byIdentifier.Remove(identifier);
      Debug.Log($"[UserDescriptorService] Unregistered: {descriptor}");
      GameLogService.WritePlayerJoin(
        $"Player left: displayName={descriptor.DisplayName}, uuid={descriptor.Identifier}",
        descriptor.Identifier);
    }

    /// <summary>SyncVar 변경 시 DisplayName을 최신으로 유지합니다.</summary>
    public static void UpdateDisplayName(string identifier, string newDisplayName)
    {
      if (_byIdentifier.TryGetValue(identifier, out var descriptor))
        descriptor.DisplayName = newDisplayName;
    }

    public static bool TryNormalizeDisplayName(string displayName, out string normalized, out string error)
    {
      normalized = displayName?.Trim() ?? string.Empty;
      error = string.Empty;
      if (normalized.Length == 0)
      {
        error = "Display name is required.";
        return false;
      }
      if (normalized.Length > MaxDisplayNameLength)
      {
        error = $"Display name cannot exceed {MaxDisplayNameLength} characters.";
        return false;
      }
      if (normalized.Any(char.IsControl))
      {
        error = "Display name cannot contain control characters.";
        return false;
      }
      return true;
    }

    public static bool IsDisplayNameInUse(string displayName, string exceptIdentifier = null)
    {
      if (!TryNormalizeDisplayName(displayName, out string normalized, out _))
        return false;
      return _byIdentifier.Values.Any(value => value != null
        && !string.Equals(value.Identifier, exceptIdentifier, System.StringComparison.Ordinal)
        && string.Equals(value.DisplayName, normalized, System.StringComparison.OrdinalIgnoreCase));
    }

    // ── 조회 API ─────────────────────────────────────────────────────────────

    /// <summary>Identifier(UUID)로 조회합니다. 코드 내 엔티티 기준 쿼리.</summary>
    public static bool TryGetByIdentifier(string identifier, out UserDescriptor descriptor)
      => _byIdentifier.TryGetValue(identifier, out descriptor);

    /// <summary>FishNet ClientId로 조회합니다. FishNet 레벨 처리에 사용.</summary>
    public static bool TryGetByClientId(int clientId, out UserDescriptor descriptor)
    {
      descriptor = null;
      return _identifierByClientId.TryGetValue(clientId, out var id)
             && _byIdentifier.TryGetValue(id, out descriptor);
    }

    /// <summary>
    /// DisplayName으로 조회합니다 (대소문자 무시). 채팅 명령어 등 사람 입력용.
    /// 동명 플레이어가 있을 경우 첫 번째를 반환한다. 명확한 식별이 필요하면 Identifier를 사용하세요.
    /// </summary>
    public static bool TryGetByDisplayName(string displayName, out UserDescriptor descriptor)
    {
      descriptor = null;
      if (string.IsNullOrWhiteSpace(displayName))
        return false;

      descriptor = _byIdentifier.Values
        .Where(d => d != null && string.Equals(d.DisplayName, displayName.Trim(), System.StringComparison.OrdinalIgnoreCase))
        .OrderBy(d => d.Identifier, System.StringComparer.Ordinal)
        .FirstOrDefault();
      return descriptor != null;
    }

    /// <summary>Identifier → ClientId 역방향 조회.</summary>
    public static bool TryGetClientId(string identifier, out int clientId)
      => _clientIdByIdentifier.TryGetValue(identifier, out clientId);

    /// <summary>등록된 모든 설명자를 반환합니다 (Identifier 키).</summary>
    public static IReadOnlyDictionary<string, UserDescriptor> GetAll()
      => _byIdentifier;
  }
}
