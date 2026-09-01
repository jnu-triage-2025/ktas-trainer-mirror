using System;
using System.Collections.Generic;
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

    /// <summary>
    /// 별칭 자체에는 고유 권한이 없다. 대신 <see cref="RequiredTargetPermissionIdentifiers"/> 로
    /// 대상 커맨드들의 권한을 모두 요구한다.
    ///
    /// <para>
    /// 이 값을 빈 문자열로 두고 권한 검사를 통과시키면, 데이터팩 별칭이 권한 경계를 우회하는
    /// 통로가 된다. <c>PermissionService.HasPermission</c> 은 빈 식별자를 거부하므로, 별칭은
    /// 반드시 <see cref="ChatCommandService"/> 의 별칭 전용 검사 경로를 거쳐야 한다.
    /// </para>
    /// </summary>
    public string PermissionIdentifier => AliasPermissionIdentifier;

    /// <summary>별칭임을 나타내는 예약 권한 식별자. 어떤 role 에도 부여하지 않는다.</summary>
    internal const string AliasPermissionIdentifier = "__datapack_alias__";

    /// <summary>이 별칭이 실행하는 대상 커맨드 이름들을 순서대로 반환한다.</summary>
    internal IReadOnlyList<string> TargetCommandNames
    {
      get
      {
        var names = new List<string>();
        foreach (string command in _target.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
          string[] parts = command.Trim().TrimStart('/')
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          if (parts.Length > 0)
            names.Add(parts[0]);
        }

        return names;
      }
    }

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

          // 별칭 인자는 마지막 커맨드에만 붙인다. 연결된 설정 커맨드를 결정적으로
          // 유지하면서 단일 커맨드 별칭 동작도 보존한다.
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
