using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 커맨드의 플레이어 인자를 해석하는 단일 진입점.
  ///
  /// 허용 토큰:
  ///   @a / @p / @r / @s / @e / @n  — 대상 선택자. [tag=..], [distance=..] 등 인자를 붙일 수 있다.
  ///   @self, self, me              — 실행자 본인.
  ///   id:&lt;uuid&gt;             — 사용자 식별자. 앞부분만 적어도 하나로 좁혀지면 통과한다.
  ///   name:&lt;displayName&gt;      — 표시 이름. 공백이 있으면 "따옴표"로 감싼다.
  ///   fish:&lt;clientId&gt;         — FishNet 연결 번호. client:, conn:도 같은 뜻이다.
  ///   entity:&lt;identifier&gt;     — 레지스트리 엔티티 식별자.
  ///   그 외                         — 식별자 → 표시 이름 → 연결 번호 → 엔티티 → 부분 일치 순으로 찾는다.
  ///
  /// 선택자는 스폰된 플레이어만 매칭하므로, 아직 스폰되지 않은 사용자는 식별자나 표시 이름으로 지정해야 한다.
  /// 이름 자체의 일치 규칙은 <see cref="PlayerNameQuery"/>가 담당한다.
  /// </summary>
  /// <summary>시스템 메시지가 플레이어를 지칭하는 방식. 명령이 대상을 지정한 토큰 형식에서 정해진다.</summary>
  public enum PlayerReferenceStyle
  {
    /// <summary>표시 이름만 쓴다. 선택자, 이름, 대상 생략(자기 자신)에 해당한다.</summary>
    DisplayName,
    /// <summary><c>이름(clientId)</c>. fish:/client: 접두사나 숫자로 FishNet 연결 번호를 지정한 경우.</summary>
    ClientId,
    /// <summary><c>이름(uuid)</c>. id:/uuid: 접두사나 식별자 자체로 지정한 경우.</summary>
    Identifier,
  }

  public static class PlayerTargetResolver
  {
    /// <summary>도움말·오류 메시지에 사용하는 토큰 문법 안내.</summary>
    public const string TokenSyntaxHint =
      "@selector (@a, @p, @r, @s), a display name, id:<uuid>, name:<display name>, fish:<clientId>, or entity:<identifier>";

    /// <summary>커맨드 사용법 줄에 넣는 짧은 안내.</summary>
    public const string ShortSyntaxHint = "@s, @a, @p, @r, <name>, id:<uuid>, name:<name>, fish:<clientId>, <clientId>";

    private static readonly string[] IdentifierPrefixes = { "id:", "uuid:", "user:" };
    private static readonly string[] DisplayNamePrefixes = { "name:", "player:", "displayname:" };
    private static readonly string[] ClientIdPrefixes = { "fish:", "client:", "clientid:", "conn:" };
    private static readonly string[] EntityPrefixes = { "entity:" };
    private static readonly string[] SelfAliases = { "self", "me" };

    /// <summary>자동 완성 후보로 제시하는 대상 선택자와, 후보 목록에 함께 표시할 뜻.</summary>
    private static readonly (string Selector, string Meaning)[] SelectorSuggestions =
    {
      ("@s", "yourself"),
      ("@a", "all players"),
      ("@p", "nearest player"),
      ("@r", "random player"),
      ("@n", "nearest player"),
      ("@e", "entities; use @e[type=player]"),
    };

    /// <summary>해석된 대상 하나. 설명자와 연결 중 한쪽만 존재할 수 있다.</summary>
    private readonly struct ResolvedTarget
    {
      public readonly UserDescriptor Descriptor;
      public readonly NetworkConnection Connection;

      public ResolvedTarget(UserDescriptor descriptor, NetworkConnection connection)
      {
        Descriptor = descriptor;
        Connection = connection;
      }

      public string Key => Descriptor?.Identifier
        ?? (Connection != null ? $"client:{Connection.ClientId}" : null);
    }

    // ── 토큰 판별 ────────────────────────────────────────────────────────────

    /// <summary>토큰이 대상 선택자 형식인지 여부.</summary>
    public static bool IsSelector(string token)
      => !string.IsNullOrWhiteSpace(token) && PlayerNameQuery.Normalize(token).StartsWith('@');

    // ── 설명자 단위 해석 ─────────────────────────────────────────────────────

    /// <summary>토큰이 가리키는 모든 사용자를 해석한다.</summary>
    public static bool TryResolve(
      NetworkConnection sender,
      string token,
      out List<UserDescriptor> descriptors,
      out string error)
    {
      descriptors = new List<UserDescriptor>();

      if (!TryResolveTargets(sender, token, out var targets, out error))
        return false;

      foreach (ResolvedTarget target in targets)
      {
        if (target.Descriptor == null)
          continue;

        if (!descriptors.Exists(existing =>
              string.Equals(existing.Identifier, target.Descriptor.Identifier, StringComparison.Ordinal)))
        {
          descriptors.Add(target.Descriptor);
        }
      }

      if (descriptors.Count == 0)
      {
        error = $"'{PlayerNameQuery.Normalize(token)}' matched a connection with no registered user descriptor.";
        return false;
      }

      return true;
    }

    /// <summary>대상이 정확히 한 명이어야 하는 커맨드용 해석.</summary>
    public static bool TryResolveSingle(
      NetworkConnection sender,
      string token,
      out UserDescriptor descriptor,
      out string error)
    {
      descriptor = null;

      if (!TryResolve(sender, token, out var descriptors, out error))
        return false;

      if (descriptors.Count > 1)
      {
        error = BuildMultipleMatchError(token, descriptors.Count);
        return false;
      }

      descriptor = descriptors[0];
      return true;
    }

    // ── 연결 단위 해석 ───────────────────────────────────────────────────────

    /// <summary>토큰이 가리키는 모든 FishNet 연결을 해석한다.</summary>
    public static bool TryResolveConnections(
      NetworkConnection sender,
      string token,
      out List<NetworkConnection> connections,
      out string error)
    {
      connections = new List<NetworkConnection>();

      if (!TryResolveTargets(sender, token, out var targets, out error))
        return false;

      foreach (ResolvedTarget target in targets)
      {
        if (target.Connection == null)
          continue;

        if (!connections.Exists(existing => existing.ClientId == target.Connection.ClientId))
          connections.Add(target.Connection);
      }

      if (connections.Count == 0)
      {
        string described = PlayerNameQuery.DescribeCandidates(
          targets.Select(target => target.Descriptor).Where(descriptor => descriptor != null));

        error = string.IsNullOrEmpty(described)
          ? $"No connected player matched '{PlayerNameQuery.Normalize(token)}'."
          : $"{described} is registered but has no active connection.";
        return false;
      }

      return true;
    }

    /// <summary>대상이 정확히 하나여야 하는 커맨드용 연결 해석.</summary>
    public static bool TryResolveSingleConnection(
      NetworkConnection sender,
      string token,
      out NetworkConnection connection,
      out string error)
    {
      connection = null;

      if (!TryResolveConnections(sender, token, out var connections, out error))
        return false;

      if (connections.Count > 1)
      {
        error = BuildMultipleMatchError(token, connections.Count);
        return false;
      }

      connection = connections[0];
      return true;
    }

    // ── 플레이어 오브젝트 단위 해석 ──────────────────────────────────────────

    /// <summary>토큰이 가리키는 모든 플레이어 오브젝트를 해석한다.</summary>
    public static bool TryResolveControllers(
      NetworkConnection sender,
      string token,
      out List<PlayerController> controllers,
      out string error)
    {
      controllers = new List<PlayerController>();

      if (!TryResolveConnections(sender, token, out var connections, out error))
        return false;

      foreach (NetworkConnection connection in connections)
      {
        if (TryGetController(connection, out var controller))
          controllers.Add(controller);
      }

      if (controllers.Count == 0)
      {
        error = $"'{PlayerNameQuery.Normalize(token)}' has no active player object.";
        return false;
      }

      return true;
    }

    /// <summary>대상이 정확히 하나여야 하는 커맨드용 플레이어 오브젝트 해석.</summary>
    public static bool TryResolveSingleController(
      NetworkConnection sender,
      string token,
      out PlayerController controller,
      out string error)
    {
      controller = null;

      if (!TryResolveControllers(sender, token, out var controllers, out error))
        return false;

      if (controllers.Count > 1)
      {
        error = BuildMultipleMatchError(token, controllers.Count);
        return false;
      }

      controller = controllers[0];
      return true;
    }

    // ── 보조 조회 ────────────────────────────────────────────────────────────

    /// <summary>설명자에 대응하는 FishNet 연결을 찾는다.</summary>
    public static bool TryGetConnection(UserDescriptor descriptor, out NetworkConnection connection)
    {
      connection = null;
      if (descriptor == null)
        return false;

      if (UserDescriptorService.TryGetClientId(descriptor.Identifier, out int clientId)
          && TryGetConnectionByClientId(clientId, out connection))
      {
        return true;
      }

      if (Registry.Registry.TryGetEntityByOwnerUserIdentifier(descriptor.Identifier, out var entity)
          && entity?.ClientId != null
          && TryGetConnectionByClientId(entity.ClientId.Value, out connection))
      {
        return true;
      }

      return false;
    }

    /// <summary>FishNet 연결 번호로 연결을 찾는다. 서버 목록이 비어 있으면 씬 검색으로 폴백한다.</summary>
    public static bool TryGetConnectionByClientId(int clientId, out NetworkConnection connection)
    {
      connection = null;

      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients != null && clients.Count > 0)
      {
        foreach (var pair in clients)
        {
          if (pair.Value != null && pair.Value.ClientId == clientId)
          {
            connection = pair.Value;
            return true;
          }
        }

        return false;
      }

      // 오프라인 실행이나 연결 초기화 전에는 서버 연결 목록이 없으므로 씬 검색으로 폴백한다.
      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      foreach (PlayerController player in players)
      {
        if (player?.Owner != null && player.Owner.ClientId == clientId)
        {
          connection = player.Owner;
          return true;
        }
      }

      return false;
    }

    /// <summary>연결이 소유한 플레이어 오브젝트를 찾는다.</summary>
    public static bool TryGetController(NetworkConnection connection, out PlayerController controller)
    {
      controller = null;
      if (connection == null)
        return false;

      if (connection.FirstObject != null
          && connection.FirstObject.TryGetComponent(out controller)
          && controller != null)
      {
        return true;
      }

      controller = null;
      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      foreach (PlayerController player in players)
      {
        if (player?.Owner != null && player.Owner.ClientId == connection.ClientId)
        {
          controller = player;
          return true;
        }
      }

      return false;
    }

    /// <summary>메시지에 넣을 연결의 표시 이름.</summary>
    public static string DescribeConnection(NetworkConnection connection)
    {
      if (connection == null)
        return "Unknown";

      if (UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor)
          && !string.IsNullOrWhiteSpace(descriptor?.DisplayName))
      {
        return descriptor.DisplayName;
      }

      return $"client {connection.ClientId}";
    }

    /// <summary>메시지에 넣을 플레이어 오브젝트의 표시 이름.</summary>
    public static string DescribeController(PlayerController controller)
    {
      if (controller == null)
        return "Unknown";

      return controller.Owner != null ? DescribeConnection(controller.Owner) : controller.name;
    }

    // ── 시스템 메시지의 대상 표기 ─────────────────────────────────────────────

    /// <summary>
    /// 명령이 대상을 지정한 방식을 판정한다. 시스템 메시지는 이 결과에 따라 플레이어를
    /// 표시 이름만으로, 또는 <c>표시 이름(clientId)</c>·<c>표시 이름(uuid)</c> 로 지칭한다.
    /// 판정 순서는 <see cref="TryResolveTargets"/>의 해석 순서를 그대로 따른다.
    /// </summary>
    public static PlayerReferenceStyle GetReferenceStyle(
      string token,
      UserDescriptor descriptor,
      NetworkConnection connection)
    {
      string trimmed = PlayerNameQuery.Normalize(token);
      if (trimmed.Length == 0 || trimmed.StartsWith('@'))
        return PlayerReferenceStyle.DisplayName;

      if (TryStripPrefix(trimmed, ClientIdPrefixes, out _))
        return PlayerReferenceStyle.ClientId;

      if (TryStripPrefix(trimmed, IdentifierPrefixes, out _))
        return PlayerReferenceStyle.Identifier;

      if (TryStripPrefix(trimmed, DisplayNamePrefixes, out _)
          || TryStripPrefix(trimmed, EntityPrefixes, out _)
          || SelfAliases.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
      {
        return PlayerReferenceStyle.DisplayName;
      }

      // 접두사가 없는 토큰은 표시 이름 완전 일치 → 식별자 완전 일치 → 연결 번호 → 식별자 앞부분 순으로 해석된다.
      if (descriptor != null && string.Equals(descriptor.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase))
        return PlayerReferenceStyle.DisplayName;

      if (descriptor?.Identifier != null && string.Equals(descriptor.Identifier, trimmed, StringComparison.OrdinalIgnoreCase))
        return PlayerReferenceStyle.Identifier;

      if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int clientId)
          && connection != null
          && connection.ClientId == clientId)
      {
        return PlayerReferenceStyle.ClientId;
      }

      if (descriptor?.Identifier != null
          && trimmed.Length >= PlayerNameQuery.MinimumIdentifierPrefixLength
          && descriptor.Identifier.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
      {
        return PlayerReferenceStyle.Identifier;
      }

      return PlayerReferenceStyle.DisplayName;
    }

    /// <summary>
    /// 시스템 메시지에서 대상 플레이어를 지칭하는 문자열. 표시 이름을 기본으로 하되,
    /// 명령이 FishNet 연결 번호로 지정했으면 <c>이름(clientId)</c>, 사용자 식별자로 지정했으면
    /// <c>이름(uuid)</c> 형식으로 어떤 대상을 가리켰는지 함께 보여 준다.
    /// </summary>
    /// <param name="token">명령에 입력된 대상 토큰. 대상을 생략한 자기 자신 등은 null 을 넘긴다.</param>
    public static string DescribeTarget(string token, UserDescriptor descriptor, NetworkConnection connection)
    {
      string name = DescribeTargetName(descriptor, connection);
      switch (GetReferenceStyle(token, descriptor, connection))
      {
        case PlayerReferenceStyle.ClientId:
          return connection != null
            ? $"{name}({connection.ClientId.ToString(CultureInfo.InvariantCulture)})"
            : name;
        case PlayerReferenceStyle.Identifier:
          return !string.IsNullOrWhiteSpace(descriptor?.Identifier)
            ? $"{name}({descriptor.Identifier})"
            : name;
        default:
          return name;
      }
    }

    /// <summary>연결로 지정된 대상의 시스템 메시지 표기. 설명자는 연결 번호로 찾는다.</summary>
    public static string DescribeTarget(string token, NetworkConnection connection)
    {
      UserDescriptor descriptor = null;
      if (connection != null)
        UserDescriptorService.TryGetByClientId(connection.ClientId, out descriptor);

      return DescribeTarget(token, descriptor, connection);
    }

    /// <summary>설명자로 지정된 대상의 시스템 메시지 표기. 연결은 설명자로 찾는다.</summary>
    public static string DescribeTarget(string token, UserDescriptor descriptor)
    {
      TryGetConnection(descriptor, out NetworkConnection connection);
      return DescribeTarget(token, descriptor, connection);
    }

    /// <summary>플레이어 오브젝트로 지정된 대상의 시스템 메시지 표기.</summary>
    public static string DescribeTarget(string token, PlayerController controller)
    {
      if (controller == null)
        return "Unknown";

      return controller.Owner != null ? DescribeTarget(token, controller.Owner) : controller.name;
    }

    private static string DescribeTargetName(UserDescriptor descriptor, NetworkConnection connection)
    {
      if (!string.IsNullOrWhiteSpace(descriptor?.DisplayName))
        return descriptor.DisplayName;

      if (connection != null)
        return DescribeConnection(connection);

      return descriptor != null ? PlayerNameQuery.Describe(descriptor) : "Unknown";
    }

    /// <summary>
    /// 자동 완성 후보를 모은다. 접속 중인 플레이어마다 표시 이름과 id: 토큰, fish: 토큰을
    /// 함께 제시하고, 마지막에 대상 선택자를 덧붙인다. 각 후보의 뜻은
    /// <see cref="DescribeToken"/>이 돌려준다.
    /// </summary>
    public static IReadOnlyList<string> CollectTokenSuggestions()
    {
      var result = new List<string>();

      try
      {
        foreach (UserDescriptor descriptor in PlayerNameQuery.Snapshot())
        {
          if (!string.IsNullOrWhiteSpace(descriptor.DisplayName))
            result.Add(PlayerNameQuery.Quote(descriptor.DisplayName));

          if (!string.IsNullOrWhiteSpace(descriptor.Identifier))
            result.Add(IdentifierPrefixes[0] + descriptor.Identifier);

          if (UserDescriptorService.TryGetClientId(descriptor.Identifier, out int clientId))
            result.Add(ClientIdPrefixes[0] + clientId.ToString(CultureInfo.InvariantCulture));
        }
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[PlayerTargetResolver] Failed to collect player tokens: {exception.Message}");
      }

      foreach ((string selector, string _) in SelectorSuggestions)
        result.Add(selector);

      return result
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>대상 선택자 토큰 목록. 자동 완성에서 선택자만 제안할 때 사용한다.</summary>
    public static IReadOnlyList<string> CollectSelectorSuggestions()
      => SelectorSuggestions.Select(entry => entry.Selector).ToList();

    /// <summary>
    /// 자동 완성 후보 한 줄에 함께 보여 줄 설명을 돌려준다. 식별자와 연결 번호 토큰은
    /// 그 토큰이 가리키는 플레이어의 표시 이름을, 대상 선택자는 그 뜻을 돌려준다.
    /// 표시 이름 후보처럼 설명이 필요 없는 토큰에는 빈 문자열을 돌려준다.
    /// </summary>
    public static string DescribeToken(string token)
    {
      string trimmed = PlayerNameQuery.Normalize(token);
      if (trimmed.Length == 0)
        return string.Empty;

      if (trimmed.StartsWith('@'))
      {
        string normalized = NormalizeSelfAlias(trimmed);
        foreach ((string selector, string meaning) in SelectorSuggestions)
        {
          if (string.Equals(selector, normalized, StringComparison.OrdinalIgnoreCase))
            return meaning;
        }

        return string.Empty;
      }

      if (TryStripPrefix(trimmed, IdentifierPrefixes, out string identifier))
      {
        return PlayerNameQuery.TryResolveByIdentifier(identifier, out var byIdentifier, out _)
          ? DescribeDisplayName(byIdentifier)
          : string.Empty;
      }

      if (TryStripPrefix(trimmed, ClientIdPrefixes, out string clientIdText))
      {
        return int.TryParse(PlayerNameQuery.Normalize(clientIdText), out int clientId)
               && UserDescriptorService.TryGetByClientId(clientId, out var byClientId)
          ? DescribeDisplayName(byClientId)
          : string.Empty;
      }

      return string.Empty;
    }

    private static string DescribeDisplayName(UserDescriptor descriptor)
      => string.IsNullOrWhiteSpace(descriptor?.DisplayName) ? string.Empty : descriptor.DisplayName;

    // ── 내부: 토큰 문법 ──────────────────────────────────────────────────────

    private static bool TryResolveTargets(
      NetworkConnection sender,
      string token,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      string trimmed = PlayerNameQuery.Normalize(token);
      if (trimmed.Length == 0)
      {
        error = "Player token is required.";
        return false;
      }

      if (trimmed.StartsWith('@'))
        return TryResolveSelector(sender, trimmed, out targets, out error);

      if (TryStripPrefix(trimmed, IdentifierPrefixes, out string identifier))
        return TryResolveIdentifierToken(identifier, out targets, out error);

      if (TryStripPrefix(trimmed, DisplayNamePrefixes, out string displayName))
        return TryResolveDisplayNameToken(displayName, out targets, out error);

      if (TryStripPrefix(trimmed, ClientIdPrefixes, out string clientIdText))
        return TryResolveClientIdToken(clientIdText, out targets, out error);

      if (TryStripPrefix(trimmed, EntityPrefixes, out string entityIdentifier))
        return TryResolveEntityToken(entityIdentifier, out targets, out error);

      return TryResolveFreeformToken(sender, trimmed, out targets, out error);
    }

    private static bool TryResolveIdentifierToken(
      string identifier,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      string normalized = PlayerNameQuery.Normalize(identifier);
      if (normalized.Length == 0)
      {
        error = "An identifier is required after 'id:'.";
        return false;
      }

      if (PlayerNameQuery.TryResolveByIdentifier(normalized, out var descriptor, out var ambiguous))
      {
        targets.Add(CreateTarget(descriptor));
        return true;
      }

      if (ambiguous.Count > 1)
      {
        error = $"Identifier '{normalized}' is ambiguous: {PlayerNameQuery.DescribeCandidates(ambiguous)}.";
        return false;
      }

      // 설명자가 등록되기 전이라도 레지스트리에 플레이어 엔티티가 있으면 연결을 찾을 수 있다.
      if (TryResolveEntityTarget(normalized, out ResolvedTarget entityTarget))
      {
        targets.Add(entityTarget);
        return true;
      }

      error = $"No player found with identifier '{normalized}'.";
      return false;
    }

    private static bool TryResolveDisplayNameToken(
      string displayName,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      string normalized = PlayerNameQuery.Normalize(displayName);
      if (normalized.Length == 0)
      {
        error = "A display name is required after 'name:'.";
        return false;
      }

      if (PlayerNameQuery.TryResolveByDisplayName(normalized, out var descriptor, out var ambiguous))
      {
        targets.Add(CreateTarget(descriptor));
        return true;
      }

      error = ambiguous.Count > 1
        ? $"Player name '{normalized}' is ambiguous: {PlayerNameQuery.DescribeCandidates(ambiguous)}."
        : $"No player found with name '{normalized}'.";
      return false;
    }

    private static bool TryResolveClientIdToken(
      string clientIdText,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      string normalized = PlayerNameQuery.Normalize(clientIdText);
      if (!int.TryParse(normalized, out int clientId))
      {
        error = $"Invalid client id '{normalized}'.";
        return false;
      }

      if (!TryResolveClientIdTarget(clientId, out ResolvedTarget target))
      {
        error = $"No player found for client id '{clientId}'.";
        return false;
      }

      targets.Add(target);
      return true;
    }

    private static bool TryResolveEntityToken(
      string entityIdentifier,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      string normalized = PlayerNameQuery.Normalize(entityIdentifier);
      if (normalized.Length == 0)
      {
        error = "An entity identifier is required after 'entity:'.";
        return false;
      }

      if (!TryResolveEntityTarget(normalized, out ResolvedTarget target))
      {
        error = $"No player entity found with identifier '{normalized}'.";
        return false;
      }

      targets.Add(target);
      return true;
    }

    private static bool TryResolveFreeformToken(
      NetworkConnection sender,
      string token,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      // 1) 식별자·표시 이름 완전 일치를 가장 먼저 본다. 별칭이나 숫자 이름을 가로채지 않기 위함이다.
      var exact = PlayerNameQuery.FindAll(token, out PlayerNameMatchKind exactKind);
      if (exact.Count > 0
          && (exactKind == PlayerNameMatchKind.Identifier || exactKind == PlayerNameMatchKind.DisplayName))
      {
        targets.Add(CreateTarget(exact[0]));
        return true;
      }

      // 2) self / me 별칭.
      if (SelfAliases.Contains(token, StringComparer.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = $"'{token}' cannot be used from system execution.";
          return false;
        }

        if (!TryResolveClientIdTarget(sender.ClientId, out ResolvedTarget selfTarget))
        {
          error = "Unable to find your user descriptor.";
          return false;
        }

        targets.Add(selfTarget);
        return true;
      }

      // 3) 숫자는 FishNet 연결 번호로 본다.
      if (int.TryParse(token, out int clientId)
          && TryResolveClientIdTarget(clientId, out ResolvedTarget clientTarget))
      {
        targets.Add(clientTarget);
        return true;
      }

      // 4) 레지스트리 엔티티(소유자 식별자 또는 엔티티 식별자).
      if (TryResolveEntityTarget(token, out ResolvedTarget entityTarget))
      {
        targets.Add(entityTarget);
        return true;
      }

      // 5) 앞부분·부분 문자열 등 나머지 단계.
      if (PlayerNameQuery.TryResolve(token, out var descriptor, out var ambiguous, out _))
      {
        targets.Add(CreateTarget(descriptor));
        return true;
      }

      error = ambiguous.Count > 1
        ? $"Player '{token}' is ambiguous: {PlayerNameQuery.DescribeCandidates(ambiguous)}. Use name:<exact name> or id:<uuid>."
        : $"Player '{token}' not found. Use {TokenSyntaxHint}.";
      return false;
    }

    private static bool TryResolveSelector(
      NetworkConnection sender,
      string token,
      out List<ResolvedTarget> targets,
      out string error)
    {
      targets = new List<ResolvedTarget>();
      error = string.Empty;

      string normalized = NormalizeSelfAlias(token);

      // 인자가 없는 @s는 세션만으로 해석해, 아직 플레이어가 스폰되지 않은 실행자도 자신을 지정할 수 있게 한다.
      if (string.Equals(normalized, "@s", StringComparison.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = "@s cannot be used from system execution.";
          return false;
        }

        if (!TryResolveClientIdTarget(sender.ClientId, out ResolvedTarget selfTarget))
        {
          error = "Unable to find your user descriptor.";
          return false;
        }

        targets.Add(selfTarget);
        return true;
      }

      if (!TargetSelectorResolver.TryResolveTargets(sender, normalized, out var connections, out error))
        return false;

      foreach (NetworkConnection connection in connections)
      {
        if (connection == null)
          continue;

        UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor);
        var candidate = new ResolvedTarget(descriptor, connection);

        if (!targets.Exists(existing => string.Equals(existing.Key, candidate.Key, StringComparison.Ordinal)))
          targets.Add(candidate);
      }

      if (targets.Count == 0)
      {
        error = $"No player session matched the selector '{token}'.";
        return false;
      }

      return true;
    }

    // ── 내부: 대상 생성 ──────────────────────────────────────────────────────

    private static ResolvedTarget CreateTarget(UserDescriptor descriptor)
    {
      TryGetConnection(descriptor, out NetworkConnection connection);
      return new ResolvedTarget(descriptor, connection);
    }

    private static bool TryResolveClientIdTarget(int clientId, out ResolvedTarget target)
    {
      target = default;

      bool hasConnection = TryGetConnectionByClientId(clientId, out NetworkConnection connection);
      bool hasDescriptor = UserDescriptorService.TryGetByClientId(clientId, out var descriptor) && descriptor != null;

      if (!hasConnection && !hasDescriptor)
        return false;

      target = new ResolvedTarget(hasDescriptor ? descriptor : null, connection);
      return true;
    }

    private static bool TryResolveEntityTarget(string identifier, out ResolvedTarget target)
    {
      target = default;
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      if (Registry.Registry.TryGetEntityByOwnerUserIdentifier(identifier, out var byOwner)
          && byOwner?.ClientId != null
          && TryResolveClientIdTarget(byOwner.ClientId.Value, out target))
      {
        return true;
      }

      if (Registry.Registry.TryGetEntity(identifier, out var entity)
          && entity != null
          && entity.EntityType == Registry.EntityType.Player)
      {
        if (entity.ClientId != null && TryResolveClientIdTarget(entity.ClientId.Value, out target))
          return true;

        if (!string.IsNullOrWhiteSpace(entity.OwnerUserIdentifier)
            && UserDescriptorService.TryGetByIdentifier(entity.OwnerUserIdentifier, out var descriptor)
            && descriptor != null)
        {
          target = CreateTarget(descriptor);
          return true;
        }
      }

      return false;
    }

    // ── 내부: 문자열 유틸 ────────────────────────────────────────────────────

    private static bool TryStripPrefix(string token, IReadOnlyList<string> prefixes, out string remainder)
    {
      remainder = string.Empty;

      foreach (string prefix in prefixes)
      {
        if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
          remainder = token.Substring(prefix.Length);
          return true;
        }
      }

      return false;
    }

    /// <summary>@self[...] 형태를 @s[...]로 바꾼다.</summary>
    private static string NormalizeSelfAlias(string token)
    {
      const string selfPrefix = "@self";
      if (!token.StartsWith(selfPrefix, StringComparison.OrdinalIgnoreCase))
        return token;

      return "@s" + token.Substring(selfPrefix.Length);
    }

    private static string BuildMultipleMatchError(string token, int count)
      => $"Target '{PlayerNameQuery.Normalize(token)}' matched {count} players; expected 1.";
  }
}
