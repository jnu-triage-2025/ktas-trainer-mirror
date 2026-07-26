using System;
using System.IO;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Logging;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// /log 커맨드 - 게임 로그 파일 관리.
  ///
  /// 서브커맨드:
  ///   /log folder          — 로그 폴더를 OS 파일 탐색기로 연다
  ///   /log list            — 저장된 로그 파일 목록을 채팅에 출력
  ///   /log export [path]   — 현재 세션 로그를 텍스트 파일로 내보내기
  ///                          (path 생략 시 로그 폴더에 -export.log 접미사로 저장)
  ///   /log flush           — 현재 로그 버퍼를 강제 플러시
  ///   /log info            — 현재 세션/로그 파일 정보 출력
  /// </summary>
  public class CommandDefinition_Log : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "log";
    public string Description => "Manage game session logs.";

    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("log folder",             "Open the log folder in the file explorer."),
      new UsageLine("log list",               "List saved log files."),
      new UsageLine("log export [path]",      "Export current session log to a text file."),
      new UsageLine("log flush",              "Flush the current log buffer to disk."),
      new UsageLine("log info",               "Show current session and log file info."),
    };

    public string PermissionIdentifier => "log";

    private readonly ChatService _chat;

    public CommandDefinition_Log(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      string sub = args != null && args.Length > 0 ? args[0].ToLowerInvariant().Trim() : string.Empty;

      switch (sub)
      {
        case "folder":
          ExecuteFolder(sender);
          break;

        case "list":
          ExecuteList(sender);
          break;

        case "export":
          string exportPath = args != null && args.Length > 1
            ? string.Join(' ', args[1..]).Trim()
            : string.Empty;
          ExecuteExport(sender, exportPath);
          break;

        case "flush":
          ExecuteFlush(sender);
          break;

        case "info":
          ExecuteInfo(sender);
          break;

        default:
          _chat.SendSystemMessage(sender,
            "Usage: /log folder | /log list | /log export [path] | /log flush | /log info");
          break;
      }
    }

    // ── 서브커맨드 구현 ────────────────────────────────────────────────────────

    private void ExecuteFolder(NetworkConnection sender)
    {
      GameLogService.OpenLogFolder();
      _chat.SendSystemMessage(sender,
        $"Opened log folder: {GameLogService.LogRootPath}");
    }

    private void ExecuteList(NetworkConnection sender)
    {
      var files = GameLogService.GetLogFiles();
      if (files == null || files.Length == 0)
      {
        _chat.SendSystemMessage(sender, "No log files found.");
        return;
      }

      // 최신순 정렬 후 파일 이름만 출력
      var sorted = files
        .OrderByDescending(f => File.GetLastWriteTime(f))
        .Select(f => Path.GetFileName(f))
        .ToArray();

      string list = string.Join('\n', sorted.Take(20)); // 최대 20개 표시
      int total = sorted.Length;
      string header = total > 20
        ? $"Log files ({total} total, showing latest 20):"
        : $"Log files ({total} total):";

      _chat.SendSystemMessage(sender, $"{header}\n{list}");
    }

    private void ExecuteExport(NetworkConnection sender, string destinationPath)
    {
      if (!GameLogService.IsInitialized)
      {
        _chat.SendSystemMessage(sender, "Log service is not initialized.");
        return;
      }

      // 경로가 비어 있으면 로그 폴더 안에 -export.log 접미사로 저장
      if (string.IsNullOrWhiteSpace(destinationPath))
      {
        string slug = GameSessionService.GetSessionSlug();
        destinationPath = Path.Combine(GameLogService.LogRootPath, $"{slug}-export.log");
      }

      if (!GameLogService.TryExport(destinationPath, out string error))
      {
        _chat.SendSystemMessage(sender, $"Export failed: {error}");
        return;
      }

      _chat.SendSystemMessage(sender, $"Log exported to: {destinationPath}");
    }

    private void ExecuteFlush(NetworkConnection sender)
    {
      GameLogService.Flush();
      _chat.SendSystemMessage(sender, "Log buffer flushed.");
    }

    private void ExecuteInfo(NetworkConnection sender)
    {
      if (!GameLogService.IsInitialized)
      {
        _chat.SendSystemMessage(sender,
          "Log service is not initialized.\n" +
          $"Log folder: {GameLogService.LogRootPath}");
        return;
      }

      string sessionId = GameSessionService.SessionId;
      string startTime = GameSessionService.SessionStartTime.ToString("yyyy-MM-dd HH:mm:ss");
      string filePath = GameLogService.CurrentLogFilePath ?? "(none)";
      string context = GameLogService.ContextLabel;
      int entryCount = GameLogService.GetEntries().Count;

      _chat.SendSystemMessage(sender,
        $"Session : session-{sessionId}\n" +
        $"Started : {startTime}\n" +
        $"Context : {context}\n" +
        $"Entries : {entryCount}\n" +
        $"File    : {filePath}");
    }
  }
}
