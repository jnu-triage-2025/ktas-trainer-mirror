using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Gamemode : IChatCommandModel
  {
    public string CommandEntry => "gamemode";
    public string Description => (
      "Change your gamemode."
      + " Usage: /gamemode <mode>"
      + " Modes: player (0), spectator (1)"
      + " example: /gamemode 0"
      + "          /gamemode spectator"
    );

    public bool RequiresAdmin => false;

    private readonly ChatService _manager;

    public CommandDefinition_Gamemode(ChatService manager)
    {
      _manager = manager;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _manager.SendSystemMessage(sender, "Usage: /gamemode <mode>");
        return;
      }

      if (sender == null || sender.FirstObject == null || !sender.FirstObject.TryGetComponent(out PlayerController controller))
      {
        _manager.SendSystemMessage(sender, "Unable to locate your target.");
        return;
      }

      if (!TryParseGamemode(args[0], out PlayerGamemode targetMode, out string parseError))
      {
        _manager.SendSystemMessage(sender, parseError);
        return;
      }

      if (!PlayerGamemodeService.TrySetGamemode(sender, controller, targetMode, out string error))
      {
        _manager.SendSystemMessage(sender, error);
        return;
      }

      _manager.SendSystemMessage(sender, $"Set gamemode to '{targetMode}'.");
    }

    private bool TryParseGamemode(string raw, out PlayerGamemode mode, out string error)
    {
      error = string.Empty;
      mode = PlayerGamemode.Player;

      if (string.IsNullOrWhiteSpace(raw))
      {
        error = "Usage: /gamemode <player|spectator>";
        return false;
      }

      string lowered = raw.ToLower();
      switch (lowered)
      {
        case "0":
        case "player":
          mode = PlayerGamemode.Player;
          return true;
        case "1":
        case "spectator":
          mode = PlayerGamemode.Spectator;
          return true;
        default:
          error = "Unknown gamemode. Use 'player' (0) or 'spectator' (1).";
          return false;
      }
    }
  }
}