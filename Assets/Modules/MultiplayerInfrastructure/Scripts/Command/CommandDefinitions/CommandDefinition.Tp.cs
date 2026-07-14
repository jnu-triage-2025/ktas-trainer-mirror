using System;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Permission;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// /tp 커맨드 — 순간이동.
  ///
  /// 지원하는 형태:
  ///   /tp x y z                   — 자기 자신을 (x, y, z) 로 이동
  ///   /tp &lt;player&gt; x y z          — player 를 (x, y, z) 로 이동
  ///   /tp &lt;player&gt;                — 자기 자신을 player 위치로 이동
  ///   /tp &lt;player1&gt; &lt;player2&gt;      — player1 을 player2 위치로 이동
  ///   /tp &lt;waypoint&gt;              — 자기 자신을 waypoint 위치로 이동
  ///   /tp &lt;player&gt; &lt;waypoint&gt;     — player 를 waypoint 위치로 이동
  /// </summary>
  public class CommandDefinition_Tp : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "tp";
    public string Description => "Teleport yourself or another player.";

    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("tp x y z",                "Teleport yourself to (x, y, z)."),
      new UsageLine("tp <player> x y z",       "Teleport player to (x, y, z)."),
      new UsageLine("tp <player>",             "Teleport yourself to player."),
      new UsageLine("tp <player1> <player2>",  "Teleport player1 to player2."),
      new UsageLine("tp <waypoint>",           "Teleport yourself to waypoint."),
      new UsageLine("tp <player> <waypoint>",  "Teleport player to waypoint."),
      new UsageLine("  <player>",              "@s, @a, @p, @r, <clientId>, fish:<id>, id:<user>, name:<name>."),
      new UsageLine("  <waypoint>",            "Registered waypoint identifier."),
    };

    public string PermissionIdentifier => "tp";
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;

    public CommandDefinition_Tp(ChatService chat)
    {
      _chat = chat;
    }

    // ── Execute ──────────────────────────────────────────────────────────────

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length == 0)
      {
        SendUsage(sender);
        return;
      }

      // /tp x y z  (3 floats → teleport self)
      if (args.Length == 3 && TryParseFiniteFloat(args[0], out float x3) && TryParseFiniteFloat(args[1], out float y3) && TryParseFiniteFloat(args[2], out float z3))
      {
        HandleSelfToXyz(sender, new Vector3(x3, y3, z3));
        return;
      }

      // /tp <player> x y z  (4 args, last 3 are floats → teleport player to coords)
      if (args.Length == 4 && TryParseFiniteFloat(args[1], out float x4) && TryParseFiniteFloat(args[2], out float y4) && TryParseFiniteFloat(args[3], out float z4))
      {
        HandlePlayerToXyz(sender, args[0], new Vector3(x4, y4, z4));
        return;
      }

      // /tp <target>  (1 token — player or waypoint)
      if (args.Length == 1)
      {
        HandleSelfToTarget(sender, args[0]);
        return;
      }

      // /tp <a> <b>  (2 tokens — player→player or player→waypoint)
      if (args.Length == 2)
      {
        HandleTwoTokens(sender, args[0], args[1]);
        return;
      }

      SendUsage(sender);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    /// /tp x y z
    private void HandleSelfToXyz(NetworkConnection sender, Vector3 destination)
    {
      if (!TryResolveController(sender, sender, out var controller, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      Teleport(controller, destination);
      _chat.SendSystemMessage(sender, $"Teleported to ({destination.x:0.##}, {destination.y:0.##}, {destination.z:0.##}).");
    }

    /// /tp <player> x y z
    private void HandlePlayerToXyz(NetworkConnection sender, string playerToken, Vector3 destination)
    {
      if (!TryResolveController(playerToken, sender, out var controller, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      if (!IsAdminOrSelf(sender, controller))
      {
        _chat.SendSystemMessage(sender, "Permission denied: you can only teleport yourself.");
        return;
      }

      string name = ResolveDisplayName(controller);
      Teleport(controller, destination);
      _chat.SendSystemMessage(sender, $"Teleported {name} to ({destination.x:0.##}, {destination.y:0.##}, {destination.z:0.##}).");
    }

    /// /tp <target>  — self → player or self → waypoint
    private void HandleSelfToTarget(NetworkConnection sender, string targetToken)
    {
      // Try player first.
      if (TryResolveController(targetToken, sender, out var destController, out _))
      {
        if (!TryResolveController(sender, sender, out var selfController, out string selfError))
        {
          _chat.SendSystemMessage(sender, selfError);
          return;
        }

        string destName = ResolveDisplayName(destController);
        Teleport(selfController, destController.transform.position);
        _chat.SendSystemMessage(sender, $"Teleported to {destName}.");
        return;
      }

      // Try waypoint.
      if (TryResolveWaypointPosition(targetToken, out Vector3 waypointPos))
      {
        if (!TryResolveController(sender, sender, out var selfController, out string selfError))
        {
          _chat.SendSystemMessage(sender, selfError);
          return;
        }

        Teleport(selfController, waypointPos);
        _chat.SendSystemMessage(sender, $"Teleported to waypoint '{targetToken}'.");
        return;
      }

      _chat.SendSystemMessage(sender, $"Target '{targetToken}' was not found as a player or waypoint.");
    }

    /// /tp <a> <b>  — player→player or player→waypoint
    private void HandleTwoTokens(NetworkConnection sender, string aToken, string bToken)
    {
      // Resolve subject (a).
      if (!TryResolveController(aToken, sender, out var subjectController, out string subjectError))
      {
        _chat.SendSystemMessage(sender, subjectError);
        return;
      }

      if (!IsAdminOrSelf(sender, subjectController))
      {
        _chat.SendSystemMessage(sender, "Permission denied: you can only teleport yourself.");
        return;
      }

      string subjectName = ResolveDisplayName(subjectController);

      // Try b as player.
      if (TryResolveController(bToken, sender, out var destController, out _))
      {
        string destName = ResolveDisplayName(destController);
        Teleport(subjectController, destController.transform.position);
        _chat.SendSystemMessage(sender, $"Teleported {subjectName} to {destName}.");
        return;
      }

      // Try b as waypoint.
      if (TryResolveWaypointPosition(bToken, out Vector3 waypointPos))
      {
        Teleport(subjectController, waypointPos);
        _chat.SendSystemMessage(sender, $"Teleported {subjectName} to waypoint '{bToken}'.");
        return;
      }

      _chat.SendSystemMessage(sender, $"Destination '{bToken}' was not found as a player or waypoint.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void Teleport(PlayerController controller, Vector3 position)
    {
      controller.TeleportToServer(position);
    }

    /// float.TryParse 에 NaN/Infinity 거부 가드 추가.
    private static bool TryParseFiniteFloat(string s, out float value)
    {
      if (!float.TryParse(s, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out value))
        return false;

      return float.IsFinite(value);
    }

    /// sender 가 null(시스템 콘솔) 이거나 호스트면 항상 허용.
    /// 그 외에는 PermissionService 로 "tp" 권한을 확인하거나 subject 가 sender 본인인지 확인.
    private static bool IsAdminOrSelf(NetworkConnection sender, PlayerController subject)
    {
      // 서버 콘솔(sender == null) 또는 호스트(IsHost) → 항상 허용
      if (sender == null || sender.IsHost)
        return true;

      // subject 의 소유자가 sender 본인이면 허용 (자기 자신 이동은 언제나 가능)
      if (subject != null && subject.Owner != null && subject.Owner.ClientId == sender.ClientId)
        return true;

      // 그 외 타인 이동은 PermissionService 에서 "tp" 권한 확인
      // (CommandService 레벨에서 이미 top-level "tp" 권한을 확인했으므로,
      //  여기서 실질적으로 체크하는 것은 operator 이상 여부다.)
      if (UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor))
        return PermissionService.HasPermission(descriptor.Identifier, "tp");

      return false;
    }

    private static bool TryResolveWaypointPosition(string identifier, out Vector3 position)
    {
      position = Vector3.zero;

      // WaypointAnchor (scene instance) 우선
      if (WaypointAnchor.TryGet(identifier, out var anchor))
      {
        position = anchor.transform.position;
        return true;
      }

      // Registry fallback (preloaded waypoints)
      if (Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, identifier, out Vector3 regPos))
      {
        position = regPos;
        return true;
      }

      return false;
    }

    /// sender connection → PlayerController (자기 자신 조회)
    private static bool TryResolveController(NetworkConnection sender, NetworkConnection fallback,
                                              out PlayerController controller, out string error)
    {
      controller = null;
      error = string.Empty;

      var conn = sender ?? fallback;
      if (conn == null)
      {
        error = "System execution requires an explicit target.";
        return false;
      }

      if (conn.FirstObject == null || !conn.FirstObject.TryGetComponent(out controller))
      {
        error = "Unable to locate your player object.";
        return false;
      }

      return true;
    }

    /// token → PlayerController (selector, name, id 등 모두 지원)
    private static bool TryResolveController(string token, NetworkConnection sender,
                                              out PlayerController controller, out string error)
    {
      controller = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(token))
      {
        error = "Target token is empty.";
        return false;
      }

      // @-selector
      if (token.StartsWith("@", StringComparison.Ordinal))
      {
        if (!TargetSelectorResolver.TryResolveTargets(sender, token, out var targets, out error))
          return false;

        if (targets.Count != 1)
        {
          error = $"Selector '{token}' matched {targets.Count} targets; expected exactly 1.";
          return false;
        }

        if (targets[0] == null || targets[0].FirstObject == null
            || !targets[0].FirstObject.TryGetComponent(out controller))
        {
          error = "Resolved target has no player object.";
          return false;
        }

        return true;
      }

      // fish:<clientId>
      if (token.StartsWith("fish:", StringComparison.OrdinalIgnoreCase))
      {
        string rawId = token.Substring("fish:".Length);
        if (!int.TryParse(rawId, out int fishId))
        {
          error = "Invalid FishNet client id after 'fish:'.";
          return false;
        }

        var conn = FindConnectionByClientId(fishId);
        if (conn == null)
        {
          error = $"No player found for fish id '{fishId}'.";
          return false;
        }

        if (conn.FirstObject == null || !conn.FirstObject.TryGetComponent(out controller))
        {
          error = "Target has no player object.";
          return false;
        }

        return true;
      }

      // id:<userIdentifier>
      if (token.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
      {
        string uid = token.Substring("id:".Length);
        if (!Registry.Registry.TryGetEntityByOwnerUserIdentifier(uid, out var desc) || desc?.ClientId == null)
        {
          error = $"No player found for identifier '{uid}'.";
          return false;
        }

        var conn = FindConnectionByClientId(desc.ClientId.Value);
        if (conn == null || conn.FirstObject == null || !conn.FirstObject.TryGetComponent(out controller))
        {
          error = "Target has no player object.";
          return false;
        }

        return true;
      }

      // name:<displayName>
      if (token.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
      {
        string displayName = token.Substring("name:".Length);
        return TryResolveControllerByDisplayName(displayName, out controller, out error);
      }

      // numeric → clientId
      if (int.TryParse(token, out int numericId))
      {
        var conn = FindConnectionByClientId(numericId);
        if (conn == null)
        {
          error = $"No player found for client id '{numericId}'.";
          return false;
        }

        if (conn.FirstObject == null || !conn.FirstObject.TryGetComponent(out controller))
        {
          error = "Target has no player object.";
          return false;
        }

        return true;
      }

      // Fallback: user identifier → display name
      if (Registry.Registry.TryGetEntityByOwnerUserIdentifier(token, out var descFallback)
          && descFallback?.ClientId != null)
      {
        var conn = FindConnectionByClientId(descFallback.ClientId.Value);
        if (conn != null && conn.FirstObject != null && conn.FirstObject.TryGetComponent(out controller))
          return true;
      }

      return TryResolveControllerByDisplayName(token, out controller, out error);
    }

    private static bool TryResolveControllerByDisplayName(string displayName,
                                                           out PlayerController controller, out string error)
    {
      controller = null;
      error = string.Empty;

      if (!UserDescriptorService.TryGetByDisplayName(displayName, out var descriptor))
      {
        error = $"No player found with name '{displayName}'.";
        return false;
      }

      if (!UserDescriptorService.TryGetClientId(descriptor.Identifier, out int clientId))
      {
        error = $"Could not resolve client id for player '{displayName}'.";
        return false;
      }

      var conn = FindConnectionByClientId(clientId);
      if (conn == null || conn.FirstObject == null || !conn.FirstObject.TryGetComponent(out controller))
      {
        error = $"Player '{displayName}' has no active player object.";
        return false;
      }

      return true;
    }

    private static NetworkConnection FindConnectionByClientId(int clientId)
    {
      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return null;

      foreach (var kvp in clients)
      {
        if (kvp.Value != null && kvp.Value.ClientId == clientId)
          return kvp.Value;
      }

      return null;
    }

    private static string ResolveDisplayName(PlayerController controller)
    {
      if (controller == null)
        return "Unknown";

      if (controller.Owner != null
          && UserDescriptorService.TryGetByClientId(controller.Owner.ClientId, out var desc))
        return desc.DisplayName;

      return controller.name;
    }

    private void SendUsage(NetworkConnection sender)
    {
      _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
    }
  }
}
