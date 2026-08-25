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

      string[] commands = _target.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      _executionDepth++;
      try
      {
        for (int i = 0; i < commands.Length; i++)
        {
          string[] targetParts = commands[i].Trim().TrimStart('/').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          if (targetParts.Length == 0)
            continue;

          // Alias arguments are appended to the final command only, keeping chained setup
          // commands deterministic while preserving single-command alias behavior.
          string[] forwarded = targetParts.Skip(1)
            .Concat(i == commands.Length - 1 ? args ?? Array.Empty<string>() : Array.Empty<string>())
            .ToArray();
          _service.TryExecute(targetParts[0], forwarded, sender);
        }
      }
      finally { _executionDepth--; }
    }
  }
}
