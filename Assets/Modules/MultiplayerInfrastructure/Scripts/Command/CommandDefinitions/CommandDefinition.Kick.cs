using FishNet.Connection;
using MultiplayerInfrastructure.Chat;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Kick : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "kick";
    public string Description => "Kick a target by name or ID.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("kick <target>", "Kick a connected target. Requires admin."),
      new UsageLine("  <target>", "Target display name or client ID."),
    };
    public string PermissionIdentifier => "kick";
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
        _manager.SendSystemMessage(sender, "Usage: /kick <targetNameOrId>");
        return;
      }

      string target = string.Join(' ', args);
      // _manager.KickPlayer(target, sender);
      _manager.SendSystemMessage(sender, $"Kick request sent for '{target}'.");
    }
  }
}