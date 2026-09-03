using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// /tag 명령어.
  /// 
  /// /tag add [@self|target] {tag}        — 태그 추가
  /// /tag remove {target} {tag}            — 태그 제거
  /// /tag change {target} {from} {to}      — 태그 변경
  /// /tag change {target} {from} {to} --force — 태그가 없어도 강제 추가
  /// /tag show {target}                    — 태그 목록 출력
  /// </summary>
  public class CommandDefinition_Tag : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "tag";
    public string Description => "Manage target tags.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("tag add <target> <tag>", "Add a tag to the target."),
      new UsageLine("tag remove <target> <tag>", "Remove a tag from the target."),
      new UsageLine("tag change <target> <from> <to> [--force]", "Rename a tag (--force adds if missing)."),
      new UsageLine("tag show <target>", "List the target's tags."),
      new UsageLine("  <target>", "@selector (@a, @p, @r, @s), id:<uuid>, name:<displayName>, display name, or entity id."),
    };
    public string PermissionIdentifier => "tag";

    private readonly ChatService _chat;

    public CommandDefinition_Tag(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
        return;
      }

      string sub = args[0].ToLowerInvariant();

      switch (sub)
      {
        case "add":
          HandleAdd(sender, args[1..]);
          break;
        case "remove":
          HandleRemove(sender, args[1..]);
          break;
        case "change":
          HandleChange(sender, args[1..]);
          break;
        case "show":
          HandleShow(sender, args[1..]);
          break;
        default:
          _chat.SendSystemMessage(sender, $"알 수 없는 서브커맨드 '{args[0]}'. /tag 를 입력해 도움말을 확인하세요.");
          break;
      }
    }

    // ── /tag add ─────────────────────────────────────────────────────────

    private void HandleAdd(NetworkConnection sender, string[] args)
    {
      // /tag add [@self|target] {tag}
      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag add <@self|target> <tag>");
        return;
      }

      string nameSelector = args[0];
      string tag = string.Join(' ', args[1..]);

      if (!TryResolveTargets(sender, nameSelector, out var targets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      foreach (TagTarget target in targets)
      {
        PlayerTagService.AddTagToIdentifier(target.Identifier, tag);
        _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에게 태그 '{tag}'를 추가했습니다.");
      }
    }

    // ── /tag remove ──────────────────────────────────────────────────────

    private void HandleRemove(NetworkConnection sender, string[] args)
    {
      // /tag remove {target} {tag}
      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag remove <target> <tag>");
        return;
      }

      string targetName = args[0];
      string tag = string.Join(' ', args[1..]);

      if (!TryResolveTargets(sender, targetName, out var targets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      foreach (TagTarget target in targets)
      {
        bool removed = PlayerTagService.RemoveTagFromIdentifier(target.Identifier, tag);
        if (!removed)
          _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에게 태그 '{tag}'가 없습니다.");
        else
          _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에서 태그 '{tag}'를 제거했습니다.");
      }
    }

    // ── /tag change ──────────────────────────────────────────────────────

    private void HandleChange(NetworkConnection sender, string[] args)
    {
      // /tag change {target} {fromTag} {toTag} [--force]
      if (args.Length < 3)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag change <target> <fromTag> <toTag> [--force]");
        return;
      }

      string targetName = args[0];
      string fromTag = args[1];
      string toTag = args[2];
      bool force = args.Length >= 4 && args[3].Equals("--force", System.StringComparison.OrdinalIgnoreCase);

      if (!TryResolveTargets(sender, targetName, out var targets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      foreach (TagTarget target in targets)
      {
        bool changed = PlayerTagService.ChangeTagForIdentifier(target.Identifier, fromTag, toTag);

        if (!changed)
        {
          if (force)
          {
            // --force: 없어도 toTag를 추가
            PlayerTagService.AddTagToIdentifier(target.Identifier, toTag);
            _chat.SendSystemMessage(sender,
              $"[태그] '{target.DisplayName}'에게 태그 '{fromTag}'이(가) 없어 '{toTag}'를 강제로 추가했습니다.");
          }
          else
          {
            _chat.SendSystemMessage(sender,
              $"[태그] '{target.DisplayName}'에게 태그 '{fromTag}'이(가) 할당되어 있지 않습니다.");
            _chat.SendSystemMessage(sender,
              $"[태그] 변경을 중단합니다. 강제로 추가하려면 --force 옵션을 사용하세요.");
          }
        }
        else
        {
          _chat.SendSystemMessage(sender,
            $"[태그] '{target.DisplayName}'의 태그가 '{fromTag}' → '{toTag}'(으)로 변경되었습니다.");
        }
      }
    }

    // ── /tag show ────────────────────────────────────────────────────────

    private void HandleShow(NetworkConnection sender, string[] args)
    {
      // /tag show {target}
      if (args.Length < 1)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag show <target>");
        return;
      }

      string targetName = args[0];
      if (!TryResolveTargets(sender, targetName, out var targets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      foreach (TagTarget target in targets)
      {
        var tags = PlayerTagService.GetTagsByIdentifier(target.Identifier);
        if (tags.Count == 0)
          _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에게 할당된 태그가 없습니다.");
        else
          _chat.SendSystemMessage(sender,
            $"[태그] '{target.DisplayName}': [{string.Join(", ", tags)}]");
      }
    }

    // ── 세션 조회 헬퍼 ────────────────────────────────────────────────────

    /// <summary>
    /// 대상 토큰을 하나 이상의 태그 대상으로 해석합니다.
    /// 선택자(@a, @p, @r, @s 등), id:/name: 접두사, 표시 이름, 엔티티 식별자를 허용합니다.
    /// </summary>
    private bool TryResolveTargets(
      NetworkConnection sender,
      string selector,
      out System.Collections.Generic.List<TagTarget> targets,
      out string error)
    {
      targets = new System.Collections.Generic.List<TagTarget>();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(selector))
      {
        error = "Target name is required.";
        return false;
      }

      // 엔티티 식별자는 플레이어 해석보다 우선합니다.
      if (!selector.StartsWith('@')
          && Registry.Registry.TryGetEntity(selector, out var entity)
          && entity?.GameObject != null)
      {
        targets.Add(new TagTarget(entity.Identifier, entity.DisplayName ?? entity.Identifier));
        return true;
      }

      if (PlayerTargetResolver.TryResolve(sender, selector, out var descriptors, out string playerError))
      {
        foreach (var descriptor in descriptors)
          targets.Add(new TagTarget(descriptor.Identifier, descriptor.DisplayName));

        return true;
      }

      // 선택자 형태로도 등록된 엔티티를 지정할 수 있으므로 마지막으로 레지스트리를 확인합니다.
      if (Registry.Registry.TryGetEntity(selector, out var selectorEntity) && selectorEntity?.GameObject != null)
      {
        targets.Add(new TagTarget(selectorEntity.Identifier, selectorEntity.DisplayName ?? selectorEntity.Identifier));
        return true;
      }

      error = playerError;
      return false;
    }

    private readonly struct TagTarget
    {
      public readonly string Identifier;
      public readonly string DisplayName;

      public TagTarget(string identifier, string displayName)
      {
        Identifier = identifier;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? identifier : displayName;
      }
    }
  }
}
