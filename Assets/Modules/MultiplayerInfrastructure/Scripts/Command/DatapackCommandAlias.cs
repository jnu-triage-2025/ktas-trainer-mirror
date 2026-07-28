using System;
using System.Linq;
using FishNet.Connection;

namespace MultiplayerInfrastructure.Command
{
  internal sealed class DatapackCommandAlias : IChatCommandModel
  {
    private readonly ChatCommandService _service;
    private readonly string _alias;
    private readonly string _target;
    private static int _executionDepth;
    public string OwnerId { get; }

    public DatapackCommandAlias(ChatCommandService service, string alias, string target, string ownerId)
    { _service = service; _alias = alias; _target = target.Trim().TrimStart('/'); OwnerId = ownerId ?? string.Empty; }

    public string CommandEntry => _alias;
    public string Description => $"Datapack alias for /{_target}.";
    public string PermissionIdentifier => string.Empty;

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_executionDepth >= 32)
        return;
      string[] targetParts = _target.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (targetParts.Length == 0)
        return;
      string[] forwarded = targetParts.Skip(1).Concat(args ?? Array.Empty<string>()).ToArray();
      _executionDepth++;
      try { _service.TryExecute(targetParts[0], forwarded, sender); }
      finally { _executionDepth--; }
    }
  }
}
