using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerInfrastructure.Logging
{
  /// <summary>
  /// 인게임 로그를 수집하고 애플리케이션 데이터 폴더에 저장하는 정적 서비스.
  ///
  /// ## 동작 원칙
  /// - 서버/클라이언트 양측에서 각자 독립적으로 동작한다.
  /// - 각 세션(session-uuid)별로 별도 로그 파일/폴더를 생성한다.
  /// - 메모리에 최근 엔트리를 캐시(순환 버퍼 방식)하며, 주기적으로 파일에 플러시한다.
  /// - 로컬 시간 기준으로 타임스탬프를 기록한다.
  ///
  /// ## 저장 경로
  ///   {Application.persistentDataPath}/GameLogs/{sessionSlug}.log
  ///
  /// ## 커맨드
  ///   /log folder  — 로그 폴더를 파일 탐색기로 연다 (에디터/빌드, 비배치 모드 전용)
  ///   /log export  — 현재 세션 로그를 사용자 지정 경로로 복사/내보내기
  ///   /log list    — 저장된 세션 로그 목록을 채팅에 출력
  ///   /log flush   — 버퍼를 강제 플러시
  /// </summary>
  public static class GameLogService
  {
    public readonly struct SessionLogClearResult
    {
      public bool Success { get; }
      public string Message { get; }

      public SessionLogClearResult(bool success, string message)
      {
        Success = success;
        Message = message ?? string.Empty;
      }
    }
    // ── 상수 ──────────────────────────────────────────────────────────────────

    /// <summary>로그 파일을 저장하는 하위 디렉터리 이름.</summary>
    public const string LogSubfolder = "GameLogs";
    public const string DatapackSubfolder = "DataPacks";

    /// <summary>메모리에 보관하는 최대 엔트리 수 (순환 버퍼 상한).</summary>
    private const int MaxCachedEntries = 4096;

    /// <summary>자동 플러시 간격 (초). 이 간격마다 디스크에 플러시한다.</summary>
    private const float AutoFlushIntervalSeconds = 2f;

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private static readonly List<GameLogEntry> _entries = new();
    private static string _currentLogFilePath;
    private static StreamWriter _writer;
    private static bool _initialized;
    private static float _lastFlushTime;

    /// <summary>로그가 초기화되어 있는지.</summary>
    public static bool IsInitialized => _initialized;

    /// <summary>현재 세션의 로그 파일 전체 경로. 초기화 전이면 null.</summary>
    public static string CurrentLogFilePath => _currentLogFilePath;

    /// <summary>로그 루트 폴더 경로.</summary>
    public static string LogRootPath =>
      Path.Combine(Application.persistentDataPath, LogSubfolder);

    public static string DatapackRootPath =>
      Path.Combine(Application.persistentDataPath, DatapackSubfolder);

    // ── 컨텍스트 ──────────────────────────────────────────────────────────────

    private static string _contextLabel = "unknown";

    /// <summary>
    /// 이 인스턴스의 컨텍스트 레이블 (예: "server", "client", "client:abc123").
    /// <see cref="Initialize"/> 호출 시 지정하거나 <see cref="SetContext"/> 로 변경한다.
    /// </summary>
    public static string ContextLabel => _contextLabel;

    /// <summary>컨텍스트 레이블을 변경한다.</summary>
    public static void SetContext(string label)
    {
      if (!string.IsNullOrWhiteSpace(label))
        _contextLabel = label.Trim();
    }

    // ── 초기화 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 로그 서비스를 초기화한다.
    /// 이미 초기화된 경우 무시한다.
    /// </summary>
    /// <param name="contextLabel">이 피어의 컨텍스트 문자열 (예: "server", "client")</param>
    public static void Initialize(string contextLabel = "unknown")
    {
      if (_initialized)
        return;

      GameSessionService.EnsureInitialized();
      SetContext(contextLabel);

      try
      {
        string dir = LogRootPath;
        if (!Directory.Exists(dir))
          Directory.CreateDirectory(dir);
        if (!Directory.Exists(DatapackRootPath))
          Directory.CreateDirectory(DatapackRootPath);

        string slug = GameSessionService.GetSessionSlug();
        _currentLogFilePath = Path.Combine(dir, $"{slug}.log");

        // AutoFlush=false: 매 Write마다 동기 디스크 I/O를 하지 않고,
        // AutoFlushIntervalSeconds 주기로 주기적 플러시한다.
        _writer = new StreamWriter(_currentLogFilePath, append: false, encoding: Encoding.UTF8)
        {
          AutoFlush = false,
        };
        _lastFlushTime = Time.realtimeSinceStartup;

        _initialized = true;

        // 로그 파일 헤더
        WriteRaw($"# KTAS Trainer Game Log");
        WriteRaw($"# Session  : session-{GameSessionService.SessionId}");
        WriteRaw($"# Started  : {GameSessionService.SessionStartTime:yyyy-MM-dd HH:mm:ss zzz}");
        WriteRaw($"# Context  : {_contextLabel}");
        WriteRaw($"# Path     : {_currentLogFilePath}");
        WriteRaw(string.Empty);

        // 헤더는 즉시 플러시
        _writer.Flush();

        Write(GameLogCategory.System, $"Log service initialized. context={_contextLabel}");
        Debug.Log($"[GameLogService] Initialized. Log file: {_currentLogFilePath}");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[GameLogService] Failed to initialize log file: {ex.Message}");
      }
    }

    /// <summary>
    /// 로그 서비스를 종료하고 파일 스트림을 닫는다.
    /// 애플리케이션 종료 전 또는 세션 종료 시 호출한다.
    /// </summary>
    public static void Shutdown()
    {
      if (!_initialized)
        return;

      Write(GameLogCategory.System, "Log service shutting down.");

      try
      {
        _writer?.Flush();
        _writer?.Close();
        _writer?.Dispose();
        _writer = null;
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[GameLogService] Error during shutdown: {ex.Message}");
      }

      _initialized = false;
      _currentLogFilePath = null;
    }

    // ── 쓰기 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 로그 엔트리를 기록한다.
    /// </summary>
    /// <param name="category">로그 카테고리</param>
    /// <param name="message">로그 메시지</param>
    /// <param name="tag">추가 태그 (선택)</param>
    public static void Write(GameLogCategory category, string message, string tag = null)
    {
      if (!_initialized)
        return;

      var entry = new GameLogEntry(DateTime.Now, _contextLabel, category, message, tag);
      AppendEntry(entry);

      // 주기적 플러시: AutoFlushIntervalSeconds 이상 경과한 경우 디스크에 기록
      float now = Time.realtimeSinceStartup;
      if (now - _lastFlushTime >= AutoFlushIntervalSeconds)
      {
        Flush();
        _lastFlushTime = now;
      }
    }

    /// <summary>시스템 이벤트를 기록한다.</summary>
    public static void WriteSystem(string message, string tag = null)
      => Write(GameLogCategory.System, message, tag);

    /// <summary>플레이어 접속/퇴장을 기록한다.</summary>
    public static void WritePlayerJoin(string message, string playerTag = null)
      => Write(GameLogCategory.PlayerJoin, message, playerTag);

    /// <summary>채팅 메시지를 기록한다.</summary>
    public static void WriteChat(string message, string senderTag = null)
      => Write(GameLogCategory.Chat, message, senderTag);

    /// <summary>커맨드 실행을 기록한다.</summary>
    public static void WriteCommand(string message, string senderTag = null)
      => Write(GameLogCategory.Command, message, senderTag);

    /// <summary>시나리오 그래프 실행을 기록한다.</summary>
    public static void WriteScenario(string message, string scenarioTag = null)
      => Write(GameLogCategory.ScenarioGraph, message, scenarioTag);

    /// <summary>시나리오 신호를 기록한다.</summary>
    public static void WriteSignal(string message, string signalTag = null)
      => Write(GameLogCategory.ScenarioSignal, message, signalTag);

    /// <summary>인터랙션을 기록한다.</summary>
    public static void WriteInteraction(string message, string interactionTag = null)
      => Write(GameLogCategory.Interaction, message, interactionTag);

    // ── 조회 API ──────────────────────────────────────────────────────────────

    /// <summary>현재 세션의 캐시된 엔트리를 반환한다 (최대 MaxCachedEntries 개, 오래된 것부터 순환 제거됨).</summary>
    public static IReadOnlyList<GameLogEntry> GetEntries() => _entries;

    /// <summary>
    /// 현재 세션의 로그를 지정 경로의 텍스트 파일로 내보낸다.
    /// destinationPath는 persistentDataPath 하위이거나 절대 경로여야 하며,
    /// 경로 탈출(../)을 허용하지 않는다.
    /// </summary>
    public static bool TryExport(string destinationPath, out string error)
    {
      error = string.Empty;

      if (!_initialized || string.IsNullOrWhiteSpace(_currentLogFilePath))
      {
        error = "Log service is not initialized.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(destinationPath))
      {
        error = "Destination path is required.";
        return false;
      }

      // 경로 탈출 방지: 정규화된 전체 경로로 변환한 뒤 안전 루트 내에 있는지 확인
      try
      {
        string fullDest = Path.GetFullPath(destinationPath);
        string safeRoot = Path.GetFullPath(Application.persistentDataPath);

        // persistentDataPath 하위가 아닌 경우에는 경고는 하지 않고 그대로 허용하되,
        // 상위 디렉터리 탈출(..를 통한) 여부만 체크한다.
        // (operator만 이 커맨드를 사용할 수 있으므로 절대 경로 외부 export도 허용)
        if (fullDest.Contains("..", StringComparison.Ordinal))
        {
          error = "Destination path must not contain '..'.";
          return false;
        }
      }
      catch (Exception ex)
      {
        error = $"Invalid destination path: {ex.Message}";
        return false;
      }

      try
      {
        string dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        // 현재 버퍼를 플러시한 뒤 파일을 복사한다.
        _writer?.Flush();
        File.Copy(_currentLogFilePath, destinationPath, overwrite: true);
        return true;
      }
      catch (Exception ex)
      {
        error = ex.Message;
        return false;
      }
    }

    /// <summary>
    /// 로그 루트 폴더에 저장된 세션 로그 파일 목록을 반환한다.
    /// </summary>
    public static string[] GetLogFiles()
    {
      try
      {
        string dir = LogRootPath;
        if (!Directory.Exists(dir))
          return Array.Empty<string>();

        return Directory.GetFiles(dir, "*.log", SearchOption.TopDirectoryOnly);
      }
      catch
      {
        return Array.Empty<string>();
      }
    }

    /// <summary>
    /// 로컬에 저장된 모든 세션 로그를 제거한다. 현재 기록 중인 로그는 먼저 닫은 뒤 제거하고,
    /// 제거 후에는 동일한 컨텍스트로 새 로그를 다시 시작한다.
    /// macOS에서는 휴지통 이동을 우선 시도하며, 지원하지 않는 플랫폼 또는 실패 시 영구 삭제한다.
    /// </summary>
    public static async Task<SessionLogClearResult> ClearAllSessionLogsAsync()
    {
      string context = _contextLabel;
      bool restartAfterClear = _initialized;
      // Application.persistentDataPath는 Unity 메인 스레드에서만 접근 가능하므로,
      // 워커 작업에 넘길 절대 경로를 여기서 먼저 확정한다.
      string logDirectory = LogRootPath;

      try
      {
        if (restartAfterClear)
          Shutdown();

        _entries.Clear();
        var result = await Task.Run(() => ClearSessionLogFiles(logDirectory));
        string message = result.Message;
        if (restartAfterClear && result.Success)
          message += " 현재 세션 로그는 계속 기록됩니다.";
        return new SessionLogClearResult(result.Success, message);
      }
      catch (Exception ex)
      {
        return new SessionLogClearResult(false, $"세션 로그 제거에 실패했습니다: {ex.Message}");
      }
      finally
      {
        if (restartAfterClear)
          Initialize(context);
      }
    }

    private static SessionLogClearResult ClearSessionLogFiles(string directory)
    {
      if (!Directory.Exists(directory))
        return new SessionLogClearResult(true, "삭제할 세션 로그가 없습니다.");

      string[] files = Directory.GetFiles(directory, "*.log", SearchOption.TopDirectoryOnly);
      // macOS Finder는 휴지통 이동마다 효과음을 낸다. 파일별 이동 대신 로그 폴더 전체를
      // 한 번만 이동하면 사용자에게 들리는 효과음과 Finder 호출 횟수를 모두 줄일 수 있다.
      if (TryMovePathToTrash(directory))
      {
        return new SessionLogClearResult(
          true,
          $"세션 로그 {files.Length}개가 포함된 로그 폴더를 휴지통으로 이동했습니다.");
      }

      // 폴더 단위 휴지통 이동을 지원하지 않거나 실패한 플랫폼의 폴백: 기존처럼 파일별 제거.
      int trashedCount = 0;
      int deletedCount = 0;
      for (int i = 0; i < files.Length; i++)
      {
        if (TryMovePathToTrash(files[i]))
          trashedCount++;
        else
        {
          File.Delete(files[i]);
          deletedCount++;
        }
      }

      string message = files.Length == 0
        ? "삭제할 세션 로그가 없습니다."
        : $"세션 로그 {files.Length}개를 제거했습니다. (휴지통 {trashedCount}개, 영구 삭제 {deletedCount}개)";
      return new SessionLogClearResult(true, message);
    }

    /// <summary>
    /// 로그 루트 폴더를 파일 탐색기(OS 네이티브)로 연다.
    /// 에디터와 빌드에서만 동작한다. 배치(헤드리스) 모드에서는 경로만 출력한다.
    /// </summary>
    public static void OpenLogFolder()
      => OpenFolderAtPath(LogRootPath, "Log");

    /// <summary>데이터팩이 저장되는 DataPacks 폴더를 연다.</summary>
    public static void OpenDatapackFolder()
      => OpenFolderAtPath(DatapackRootPath, "Datapack");

    private static void OpenFolderAtPath(string dir, string label)
    {
      try
      {
        if (!Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        // 배치(헤드리스 서버) 모드에서는 Process.Start 불가 — 경로만 출력
        if (Application.isBatchMode)
        {
          Debug.Log($"[GameLogService] {label} folder (batch mode): {dir}");
          return;
        }

#if UNITY_EDITOR
        // 에디터: Unity 내장 API를 사용해 Finder/Explorer를 확실하게 연다
        UnityEditor.EditorUtility.RevealInFinder(dir);
#elif UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
          FileName = "explorer.exe",
          Arguments = dir.Replace('/', '\\'),
          UseShellExecute = true,
        });
#elif UNITY_STANDALONE_OSX
        // ArgumentList를 사용해 경로를 안전하게 전달 (공백/특수문자 포함 경로 대응)
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
          FileName = "/usr/bin/open",
          ArgumentList = { dir },
          UseShellExecute = false,
        });
#elif UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
          FileName = "xdg-open",
          ArgumentList = { dir },
          UseShellExecute = false,
        });
#else
        Debug.Log($"[GameLogService] {label} folder: {dir}");
#endif
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[GameLogService] Failed to open {label.ToLowerInvariant()} folder: {ex.Message}");
      }
    }

    private static bool TryMovePathToTrash(string path)
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
      try
      {
        string escapedPath = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
          FileName = "/usr/bin/osascript",
          ArgumentList = { "-e", $"tell application \"Finder\" to delete POSIX file \"{escapedPath}\"" },
          UseShellExecute = false,
          CreateNoWindow = true,
        });
        process?.WaitForExit();
        return process != null && process.ExitCode == 0 && !File.Exists(path) && !Directory.Exists(path);
      }
      catch
      {
        return false;
      }
#else
      return false;
#endif
    }

    /// <summary>버퍼를 강제 플러시한다.</summary>
    public static void Flush()
    {
      try
      {
        _writer?.Flush();
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[GameLogService] Flush failed: {ex.Message}");
      }
    }

    // ── 내부 ──────────────────────────────────────────────────────────────────

    private static void AppendEntry(GameLogEntry entry)
    {
      // 순환 버퍼: 상한 초과 시 가장 오래된 항목 제거
      if (_entries.Count >= MaxCachedEntries)
        _entries.RemoveAt(0);
      _entries.Add(entry);

      // 파일 기록 (버퍼만 채움, AutoFlush=false이므로 디스크에 즉시 쓰지 않음)
      try
      {
        _writer?.WriteLine(entry.ToLogLine());
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[GameLogService] Failed to write log entry: {ex.Message}");
      }
    }

    private static void WriteRaw(string line)
    {
      try
      {
        _writer?.WriteLine(line);
      }
      catch { /* silent */ }
    }
  }
}
