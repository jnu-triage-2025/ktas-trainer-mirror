using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// /tag 명령어.
  /// 
  /// /tag add [@self|playername] {tag}        — 태그 추가
  /// /tag remove {playername} {tag}            — 태그 제거
  /// /tag change {playername} {from} {to}      — 태그 변경
  /// /tag change {playername} {from} {to} --force — 태그가 없어도 강제 추가
  /// /tag show {playername}                    — 태그 목록 출력
  /// </summary>
  public class CommandDefinition_Tag : IChatCommandModel
  {
    public string CommandEntry => "tag";
    public string Description =>
      "Manage player tags.\n" +
      "  /tag add <@self|name> <tag>                       — add tag\n" +
      "  /tag remove <name> <tag>                          — remove tag\n" +
      "  /tag change <name> <fromTag> <toTag> [--force]    — change tag\n" +
      "  /tag show <name>                                  — show tags";
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
      // /tag add [@self|playername] {tag}
      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag add <@self|name> <tag>");
        return;
      }

      string nameSelector = args[0];
      string tag = string.Join(' ', args[1..]);

      if (!TryResolveSession(sender, nameSelector, out var session, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      PlayerTagService.AddTag(session.Identifier, tag);
      _chat.SendSystemMessage(sender, $"[태그] '{session.DisplayName}'에게 태그 '{tag}'를 추가했습니다.");
    }

    // ── /tag remove ──────────────────────────────────────────────────────

    private void HandleRemove(NetworkConnection sender, string[] args)
    {
      // /tag remove {playername} {tag}
      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag remove <name> <tag>");
        return;
      }

      string playerName = args[0];
      string tag = string.Join(' ', args[1..]);

      if (!TryResolveSession(sender, playerName, out var session, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      bool removed = PlayerTagService.RemoveTag(session.Identifier, tag);
      if (!removed)
        _chat.SendSystemMessage(sender, $"[태그] '{session.DisplayName}'에게 태그 '{tag}'가 없습니다.");
      else
        _chat.SendSystemMessage(sender, $"[태그] '{session.DisplayName}'에서 태그 '{tag}'를 제거했습니다.");
    }

    // ── /tag change ──────────────────────────────────────────────────────

    private void HandleChange(NetworkConnection sender, string[] args)
    {
      // /tag change {playername} {fromTag} {toTag} [--force]
      if (args.Length < 3)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag change <name> <fromTag> <toTag> [--force]");
        return;
      }

      string playerName = args[0];
      string fromTag = args[1];
      string toTag = args[2];
      bool force = args.Length >= 4 && args[3].Equals("--force", System.StringComparison.OrdinalIgnoreCase);

      if (!TryResolveSession(sender, playerName, out var session, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      bool changed = PlayerTagService.ChangeTag(session.Identifier, fromTag, toTag);

      if (!changed)
      {
        if (force)
        {
          // --force: 없어도 toTag를 추가
          PlayerTagService.AddTag(session.Identifier, toTag);
          _chat.SendSystemMessage(sender,
            $"[태그] '{session.DisplayName}'에게 태그 '{fromTag}'이(가) 없어 '{toTag}'를 강제로 추가했습니다.");
        }
        else
        {
          _chat.SendSystemMessage(sender,
            $"[태그] '{session.DisplayName}'에게 태그 '{fromTag}'이(가) 할당되어 있지 않습니다.");
          _chat.SendSystemMessage(sender,
            $"[태그] 변경을 중단합니다. 강제로 추가하려면 --force 옵션을 사용하세요.");
        }
      }
      else
      {
        _chat.SendSystemMessage(sender,
          $"[태그] '{session.DisplayName}'의 태그가 '{fromTag}' → '{toTag}'(으)로 변경되었습니다.");
      }
    }

    // ── /tag show ────────────────────────────────────────────────────────

    private void HandleShow(NetworkConnection sender, string[] args)
    {
      // /tag show {playername}
      if (args.Length < 1)
      {
        _chat.SendSystemMessage(sender, "Usage: /tag show <name>");
        return;
      }

      string playerName = args[0];
      if (!TryResolveSession(sender, playerName, out var session, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      var tags = PlayerTagService.GetTags(session.Identifier);
      if (tags.Count == 0)
        _chat.SendSystemMessage(sender, $"[태그] '{session.DisplayName}'에게 할당된 태그가 없습니다.");
      else
        _chat.SendSystemMessage(sender,
          $"[태그] '{session.DisplayName}': [{string.Join(", ", tags)}]");
    }

    // ── 세션 조회 헬퍼 ────────────────────────────────────────────────────

    /// <summary>
    /// "@self"이면 sender 자신의 세션을, 그 외에는 DisplayName으로 세션을 찾습니다.
    /// </summary>
    private bool TryResolveSession(
      NetworkConnection sender,
      string selector,
      out UserDescriptor session,
      out string error)
    {
      session = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(selector))
      {
        error = "플레이어 이름이 필요합니다.";
        return false;
      }

      if (selector.Equals("@self", System.StringComparison.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = "명령 실행자를 확인할 수 없습니다.";
          return false;
        }

        if (!UserDescriptorService.TryGetByClientId(sender.ClientId, out session))
        {
          error = "본인의 세션을 찾을 수 없습니다.";
          return false;
        }

        return true;
      }

      if (!UserDescriptorService.TryGetByDisplayName(selector, out session))
      {
        error = $"플레이어 '{selector}'를 찾을 수 없습니다.";
        return false;
      }

      return true;
    }
  }
}
