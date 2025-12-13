using FishNet.Connection;
using TriageTrainer.Chat;

namespace TriageTrainer.Command
{
  public class CammandDefinition_Gamemode : IChatCommandModel
  {
    public string CommandEntry => "gamemode";
    public string Description => (
        "Change Gamemode of a player."
      + " Usage: /gamemode <mode>"
      + " Modes: player (0), spectator (1)"
      + " example: /gamemode 0"
      + "          /gamemode spectator"
    );

    public bool RequiresAdmin => false;

    private readonly ChatManager _manager;

    public CammandDefinition_Gamemode(ChatManager manager)
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

      string mode = args[0].ToLower();
      switch (mode)
      {
        case "0":
        case "player":
          // _manager.SetPlayerGamemode(target, Gamemode.Player);
          _manager.SendSystemMessage(sender, $"Set gamemode to 'Player'.");
          break;
        case "1":
        case "spectator":
          // _manager.SetPlayerGamemode(target, Gamemode.Spectator);
          _manager.SendSystemMessage(sender, $"Set gamemode to 'Spectator'.");
          break;
        default:
          _manager.SendSystemMessage(sender, $"Unknown gamemode '{mode}'. Valid modes are: player (0), spectator (1).");
          break;
      }
    }
  }
}