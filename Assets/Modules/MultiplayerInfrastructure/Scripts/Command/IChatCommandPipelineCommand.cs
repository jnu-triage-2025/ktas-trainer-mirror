using System.Collections.Generic;
using FishNet.Connection;

namespace MultiplayerInfrastructure.Command
{
  public interface IChatCommandPipelineCommand
  {
    bool TryExecute(
      NetworkConnection sender,
      string[] args,
      bool suppressSystemMessages,
      out IReadOnlyList<string> pipelineValues,
      out string error);
  }
}
