using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Registry;
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
  public class CommandDefinition_Tag : IChatCommandModel
  {
    public string CommandEntry => "tag";
    public string Description =>
      "Manage target tags.\n" +
      "  /tag add <@self|target> <tag>                       — add tag\n" +
      "  /tag remove <target> <tag>                          — remove tag\n" +
      "  /tag change <target> <fromTag> <toTag> [--force]    — change tag\n" +
      "  /tag show <target>                                  — show tags";
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;

    public CommandDefinition_Tag(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, Description);
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

      if (!TryResolveTarget(sender, nameSelector, out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      PlayerTagService.AddTagToIdentifier(target.Identifier, tag);
      _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에게 태그 '{tag}'를 추가했습니다.");
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

      if (!TryResolveTarget(sender, targetName, out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      bool removed = PlayerTagService.RemoveTagFromIdentifier(target.Identifier, tag);
      if (!removed)
        _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에게 태그 '{tag}'가 없습니다.");
      else
        _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에서 태그 '{tag}'를 제거했습니다.");
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

      if (!TryResolveTarget(sender, targetName, out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

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
      if (!TryResolveTarget(sender, targetName, out var target, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      var tags = PlayerTagService.GetTagsByIdentifier(target.Identifier);
      if (tags.Count == 0)
        _chat.SendSystemMessage(sender, $"[태그] '{target.DisplayName}'에게 할당된 태그가 없습니다.");
      else
        _chat.SendSystemMessage(sender,
          $"[태그] '{target.DisplayName}': [{string.Join(", ", tags)}]");
    }

    // ── 세션 조회 헬퍼 ────────────────────────────────────────────────────

    /// <summary>
    /// "@self"이면 sender 자신의 세션을, 그 외에는 DisplayName으로 세션을 찾습니다.
    /// </summary>
    private bool TryResolveTarget(
      NetworkConnection sender,
      string selector,
      out TagTarget target,
      out string error)
    {
      target = default;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(selector))
      {
        error = "Target name is required.";
        return false;
      }

      if (selector.Equals("@self", System.StringComparison.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = "명령 실행자를 확인할 수 없습니다.";
          return false;
        }

        if (!UserDescriptorService.TryGetByClientId(sender.ClientId, out var selfSession))
        {
          error = "본인의 세션을 찾을 수 없습니다.";
          return false;
        }

        target = new TagTarget(selfSession.Identifier, selfSession.DisplayName);

        return true;
      }

      if (selector.StartsWith("@", System.StringComparison.Ordinal))
      {
        if (!TargetSelectorResolver.TryResolveTargets(sender, selector, out var targets, out error))
          return false;

        if (targets.Count != 1)
        {
          error = $"Target selector matched {targets.Count} targets; expected 1.";
          return false;
        }

        if (!UserDescriptorService.TryGetByClientId(targets[0].ClientId, out var selectedSession))
        {
          if (Registry.Registry.TryGetEntity(selector, out var selectedEntity) && selectedEntity?.GameObject != null)
          {
            target = new TagTarget(selectedEntity.Identifier, selectedEntity.DisplayName ?? selectedEntity.Identifier);
            return true;
          }

          error = "Target was not found.";
          return false;
        }

        target = new TagTarget(selectedSession.Identifier, selectedSession.DisplayName);

        return true;
      }

      if (Registry.Registry.TryGetEntity(selector, out var entity) && entity?.GameObject != null)
      {
        target = new TagTarget(entity.Identifier, entity.DisplayName ?? entity.Identifier);
        return true;
      }

      if (!UserDescriptorService.TryGetByDisplayName(selector, out var namedSession))
      {
        error = $"Target '{selector}'를 찾을 수 없습니다.";
        return false;
      }

      target = new TagTarget(namedSession.Identifier, namedSession.DisplayName);

      return true;
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
