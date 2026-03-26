using FishNet.Connection;
using MultiplayerInfrastructure.Chat;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Kick : IChatCommandModel
  {
    public string CommandEntry => "kick";
    public string Description => "Kick a player by name or ID.";
    public bool RequiresAdmin => true;

    private readonly ChatService _manager;

    public CommandDefinition_Kick(ChatService manager)
    {
      _manager = manager;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _manager.SendSystemMessage(sender, "Usage: /kick <playerNameOrId>");
        return;
      }

      string target = string.Join(' ', args);
      // _manager.KickPlayer(target, sender);
      _manager.SendSystemMessage(sender, $"Kick request sent for '{target}'.");
    }
  }
}