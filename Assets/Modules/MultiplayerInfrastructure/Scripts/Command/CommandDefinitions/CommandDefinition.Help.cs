using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Help : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "help";
    public string Description => "List commands. Use /<command> -h for details.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("help", "List all commands with a short summary."),
      new UsageLine("help <command>", "Show detailed usage for a command."),
      new UsageLine("<command> -h", "Show detailed usage (also --help, /?)."),
    };
    public string PermissionIdentifier => "help";
    public bool RequiresAdmin => false;

    private readonly ChatService _manager;
    private readonly ChatCommandService _service;

    public CommandDefinition_Help(ChatService manager, ChatCommandService service)
    {
      _manager = manager;
      _service = service;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_manager == null || _service == null)
        return;

      if (args == null || args.Length == 0)
      {
        var lines = _service.GetCommands()
          .OrderBy(c => c.CommandEntry)
          .Select(c => $"/{c.CommandEntry} - {ChatCommandHelp.GetSummary(c)}");
        string joined = "Commands (use /<command> -h for details):\n" + string.Join('\n', lines);
        _manager.SendSystemMessage(sender, joined);
        return;
      }

      string target = args[0]?.TrimStart('/').ToLowerInvariant();
      var match = _service.GetCommands().FirstOrDefault(c => c.CommandEntry.ToLowerInvariant() == target);
      if (match == null)
      {
        _manager.SendSystemMessage(sender, $"Unknown command: /{target}");
        return;
      }

      _manager.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(match));
    }
  }
}
