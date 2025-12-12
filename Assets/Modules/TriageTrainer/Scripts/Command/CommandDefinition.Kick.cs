using FishNet.Connection;
using TriageTrainer.Scripts.Chat;

namespace TriageTrainer.Scripts.Command
{
  public class CommandDefinition_Kick : IChatCommandModel
  {
    public string CommandEntry => "kick";
    public string Description => "Kick a player by name or ID.";
    public bool RequiresAdmin => true;

    private readonly ChatManager _manager;

    public CommandDefinition_Kick(ChatManager manager)
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