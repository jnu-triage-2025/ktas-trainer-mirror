using FishNet.Connection;

namespace Modules.TriageTrainer.Scripts.Command
{
  public interface IChatCommandModel
  {
    string CommandEntry { get; }
    string Description { get; }
    bool RequiresAdmin { get; }

    void Execute(NetworkConnection sender, string[] args);
  }
}
