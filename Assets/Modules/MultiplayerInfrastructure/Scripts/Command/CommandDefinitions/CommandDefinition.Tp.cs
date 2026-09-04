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
  ///   /tp ~x ~y ~z                — 자기 자신의 현재 위치를 기준으로 이동
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
      new UsageLine("tp ~x ~y ~z",             "Teleport yourself relative to the current position; each axis may be absolute or relative."),
      new UsageLine("tp <player> x y z",       "Teleport player to (x, y, z)."),
      new UsageLine("tp <player>",             "Teleport yourself to player."),
      new UsageLine("tp <player1> <player2>",  "Teleport player1 to player2."),
      new UsageLine("tp <waypoint>",           "Teleport yourself to waypoint."),
      new UsageLine("tp <player> <waypoint>",  "Teleport player to waypoint."),
      new UsageLine("  <player>",              PlayerTargetResolver.ShortSyntaxHint + "."),
      new UsageLine("  <waypoint>",            "Registered waypoint identifier."),
    };

    public string PermissionIdentifier => "tp";

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

      // /tp x y z  (좌표 3개: 자기 자신으로 순간이동. 좌표는 절대 또는 상대 가능)
      if (args.Length == 3 && AreCoordinateTokens(args))
      {
        HandleSelfToCoordinates(sender, args);
        return;
      }

      // /tp <player> x y z  (뒤의 좌표 3개는 절대 또는 상대 가능)
      if (args.Length == 4 && AreCoordinateTokens(args, 1))
      {
        HandlePlayerToCoordinates(sender, args[0], args, 1);
        return;
      }

      // /tp <target>  (토큰 1개: 플레이어 또는 웨이포인트)
      if (args.Length == 1)
      {
        HandleSelfToTarget(sender, args[0]);
        return;
      }

      // /tp <a> <b>  (토큰 2개: 플레이어→플레이어 또는 플레이어→웨이포인트)
      if (args.Length == 2)
      {
        HandleTwoTokens(sender, args[0], args[1]);
        return;
      }

      SendUsage(sender);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    /// /tp x y z
    private void HandleSelfToCoordinates(NetworkConnection sender, string[] args)
    {
      if (!TryResolveController(sender, sender, out var controller, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      if (!TryParseDestination(args, 0, controller.transform.position, out Vector3 destination))
      {
        SendUsage(sender);
        return;
      }

      Teleport(controller, destination);
      _chat.SendSystemMessage(sender, $"Teleported to ({destination.x:0.##}, {destination.y:0.##}, {destination.z:0.##}).");
    }

    /// /tp <player> x y z
    private void HandlePlayerToCoordinates(NetworkConnection sender, string playerToken, string[] args, int coordinateStart)
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

      if (!TryParseDestination(args, coordinateStart, controller.transform.position, out Vector3 destination))
      {
        SendUsage(sender);
        return;
      }

      string name = ResolveDisplayName(controller);
      Teleport(controller, destination);
      _chat.SendSystemMessage(sender, $"Teleported {name} to ({destination.x:0.##}, {destination.y:0.##}, {destination.z:0.##}).");
    }

    /// /tp <target>  — 자기 자신에서 플레이어 또는 웨이포인트로 이동
    private void HandleSelfToTarget(NetworkConnection sender, string targetToken)
    {
      // 플레이어를 먼저 시도한다.
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

      // 웨이포인트를 시도한다.
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

    /// /tp <a> <b>  — 플레이어에서 플레이어로, 또는 플레이어에서 웨이포인트로 이동
    private void HandleTwoTokens(NetworkConnection sender, string aToken, string bToken)
    {
      // 주체(a)를 해석한다.
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

      // b 를 플레이어로 시도한다.
      if (TryResolveController(bToken, sender, out var destController, out _))
      {
        string destName = ResolveDisplayName(destController);
        Teleport(subjectController, destController.transform.position);
        _chat.SendSystemMessage(sender, $"Teleported {subjectName} to {destName}.");
        return;
      }

      // b 를 웨이포인트로 시도한다.
      if (TryResolveWaypointPosition(bToken, out Vector3 waypointPos))
      {
        Teleport(subjectController, waypointPos);
        _chat.SendSystemMessage(sender, $"Teleported {subjectName} to waypoint '{bToken}'.");
        return;
      }

      _chat.SendSystemMessage(sender, $"Destination '{bToken}' was not found as a player or waypoint.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool AreCoordinateTokens(string[] args, int startIndex = 0)
    {
      return args != null
          && args.Length >= startIndex + 3
          && TryParseCoordinate(args[startIndex], 0f, out _)
          && TryParseCoordinate(args[startIndex + 1], 0f, out _)
          && TryParseCoordinate(args[startIndex + 2], 0f, out _);
    }

    private static bool TryParseDestination(string[] args, int startIndex, Vector3 origin, out Vector3 destination)
    {
      destination = Vector3.zero;
      if (!AreCoordinateTokens(args, startIndex))
        return false;

      if (!TryParseCoordinate(args[startIndex], origin.x, out float x)
          || !TryParseCoordinate(args[startIndex + 1], origin.y, out float y)
          || !TryParseCoordinate(args[startIndex + 2], origin.z, out float z))
        return false;

      destination = new Vector3(x, y, z);
      return true;
    }

    /// 절대좌표(예: 3), 상대좌표(예: ~, ~3, ~-1)를 파싱합니다.
    private static bool TryParseCoordinate(string token, float origin, out float value)
    {
      value = 0f;
      if (string.IsNullOrEmpty(token))
        return false;

      if (token[0] == '~')
      {
        string offsetToken = token.Substring(1);
        if (offsetToken.Length == 0)
          value = origin;
        else if (!TryParseFiniteFloat(offsetToken, out float offset))
          return false;
        else
          value = origin + offset;

        return float.IsFinite(value);
      }

      return TryParseFiniteFloat(token, out value);
    }

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

      // Registry 폴백(프리로드된 웨이포인트)
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

      if (!PlayerTargetResolver.TryGetController(conn, out controller))
      {
        error = "Unable to locate your player object.";
        return false;
      }

      return true;
    }

    /// token → PlayerController (selector, name, id 등 모두 지원)
    private static bool TryResolveController(string token, NetworkConnection sender,
                                              out PlayerController controller, out string error)
      => PlayerTargetResolver.TryResolveSingleController(sender, token, out controller, out error);

    private static string ResolveDisplayName(PlayerController controller)
      => PlayerTargetResolver.DescribeController(controller);

    private void SendUsage(NetworkConnection sender)
    {
      _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
    }
  }
}
