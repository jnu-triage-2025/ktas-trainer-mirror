using System.Collections.Generic;
using FishNet.Connection;

namespace MultiplayerInfrastructure.Player
{
  public static class PlayerGamemodeService
  {
    private static readonly Dictionary<int, PlayerGamemode> _gamemodeByClient = new();

    public static PlayerGamemode GetGamemode(NetworkConnection conn)
    {
      if (conn != null && _gamemodeByClient.TryGetValue(conn.ClientId, out PlayerGamemode mode))
        return mode;

      return PlayerGamemode.Player;
    }

    public static void RegisterPlayer(PlayerController controller)
    {
      if (controller == null || controller.Owner == null) return;

      int clientId = controller.Owner.ClientId;
      _gamemodeByClient[clientId] = PlayerGamemode.Player;
      controller.ApplyGamemodeServer(PlayerGamemode.Player);
    }

    public static void UnregisterPlayer(PlayerController controller)
    {
      if (controller == null || controller.Owner == null) return;

      _gamemodeByClient.Remove(controller.Owner.ClientId);
    }

    public static bool TrySetGamemode(NetworkConnection issuer, PlayerController target, PlayerGamemode mode, out string error)
    {
      error = string.Empty;
      _ = issuer;

      if (target == null || target.Owner == null)
      {
        error = "Target player is unavailable.";
        return false;
      }

      int targetId = target.Owner.ClientId;

      if (_gamemodeByClient.TryGetValue(targetId, out PlayerGamemode current) && current == mode)
      {
        error = $"Player is already '{mode}'.";
        return false;
      }

      _gamemodeByClient[targetId] = mode;
      target.ApplyGamemodeServer(mode);
      return true;
    }
  }
}
