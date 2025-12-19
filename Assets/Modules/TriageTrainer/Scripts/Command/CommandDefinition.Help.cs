using System.Linq;
using FishNet.Connection;
using TriageTrainer.Chat;

namespace TriageTrainer.Command
{
  public class CommandDefinition_Help : IChatCommandModel
  {
    public string CommandEntry => "help";
    public string Description => "Show available commands or details for one.";
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
        var list = _service.GetCommands()
          .OrderBy(c => c.CommandEntry)
          .Select(c => $"/{c.CommandEntry} - {c.Description}");
        string joined = string.Join('\n', list);
        _manager.SendSystemMessage(sender, joined);
        return;
      }

      string target = args[0]?.ToLowerInvariant();
      var match = _service.GetCommands().FirstOrDefault(c => c.CommandEntry.ToLowerInvariant() == target);
      if (match == null)
      {
        _manager.SendSystemMessage(sender, $"Unknown command: /{target}");
        return;
      }

      _manager.SendSystemMessage(sender, $"/{match.CommandEntry} - {match.Description}");
    }
  }
}
