using System;

namespace MultiplayerInfrastructure.Logging
{
  /// <summary>
  /// 로그 엔트리의 카테고리.
  /// </summary>
  public enum GameLogCategory
  {
    /// <summary>시스템 수준 이벤트 (세션 시작/종료, 서버/클라이언트 연결 등)</summary>
    System,

    /// <summary>플레이어 접속/퇴장</summary>
    PlayerJoin,

    /// <summary>채팅 메시지</summary>
    Chat,

    /// <summary>커맨드 실행 (서버 또는 플레이어)</summary>
    Command,

    /// <summary>시나리오 그래프 실행 (시작/종료/노드 진행)</summary>
    ScenarioGraph,

    /// <summary>시나리오 신호 발생 (interaction signal raise/clear)</summary>
    ScenarioSignal,

    /// <summary>인터랙션 발생</summary>
    Interaction,

    /// <summary>기타</summary>
    Misc,
  }

  /// <summary>
  /// 단일 로그 엔트리.
  /// 로컬 타임 기준 타임스탬프, 컨텍스트(서버/클라이언트), 카테고리, 메시지를 포함한다.
  /// </summary>
  public sealed class GameLogEntry
  {
    /// <summary>엔트리 생성 시각 (로컬 시간).</summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 이 엔트리가 기록된 컨텍스트.
    /// "server", "client", "client:{uuid}" 등.
    /// </summary>
    public string Context { get; }

    /// <summary>로그 카테고리.</summary>
    public GameLogCategory Category { get; }

    /// <summary>로그 메시지.</summary>
    public string Message { get; }

    /// <summary>추가 태그/식별자 (시나리오 ID, 플레이어 UUID 등). 없으면 null.</summary>
    public string Tag { get; }

    public GameLogEntry(
      DateTime timestamp,
      string context,
      GameLogCategory category,
      string message,
      string tag = null)
    {
      Timestamp = timestamp;
      Context = context ?? string.Empty;
      Category = category;
      Message = message ?? string.Empty;
      Tag = tag;
    }

    /// <summary>
    /// 텍스트 파일 한 줄로 직렬화한다.
    /// 형식: [yyyy-MM-dd HH:mm:ss.fff] [context] [category] [tag?] message
    /// </summary>
    public string ToLogLine()
    {
      string tagPart = string.IsNullOrWhiteSpace(Tag) ? string.Empty : $" [{Tag}]";
      return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Context}] [{Category}]{tagPart} {Message}";
    }

    public override string ToString() => ToLogLine();
  }
}
