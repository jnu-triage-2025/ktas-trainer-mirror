using FishNet.Connection;

namespace MultiplayerInfrastructure.Command
{
  public interface IChatCommandModel
  {
    string CommandEntry { get; }
    string Description { get; }
    bool RequiresAdmin { get; }

    void Execute(NetworkConnection sender, string[] args);
  }
}
