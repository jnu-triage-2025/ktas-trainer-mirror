using System.Collections.Generic;
using System.Text;
using FishNet.Connection;

namespace MultiplayerInfrastructure.Command
{
  public interface IChatCommandModel
  {
    public string CommandEntry { get; }

    /// <summary>
    /// /help 목록에 사용되는 한 줄짜리 사람이 읽을 수 있는 설명이다.
    /// 짧게 유지한다. 한 문장으로, 줄 바꿈 없이 작성한다.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 이 커맨드 최상위에 필요한 permission identifier.
    /// 예: "scenario" → "scenario" 권한이 있어야 실행 가능.
    /// PermissionService 에서 하위 경로 확장을 통해 "scenario.execute" 등도 처리된다.
    /// null 또는 empty 이면 모든 유저가 실행 가능.
    /// </summary>
    public string PermissionIdentifier { get; }

    public void Execute(NetworkConnection sender, string[] args);
  }

  /// <summary>
  /// 사용법 한 행이다. 왼쪽에 구문 조각(하위 커맨드 + 인자), 오른쪽에 그 설명을 담는다.
  /// 커맨드 이름 아래에 정렬된 두 열 행으로 표시된다.
  /// </summary>
  public readonly struct UsageLine
  {
    /// <summary>왼쪽 열. 예: "add &lt;target&gt; &lt;tag&gt;".</summary>
    public readonly string Syntax;

    /// <summary>오른쪽 열 설명. 비어 있을 수 있다.</summary>
    public readonly string Description;

    public UsageLine(string syntax, string description)
    {
      Syntax = syntax ?? string.Empty;
      Description = description ?? string.Empty;
    }

    public UsageLine(string syntax) : this(syntax, string.Empty) { }
  }

  /// <summary>
  /// 구조화된 여러 줄 사용법을 노출하는 커맨드를 위한 선택 인터페이스이다.
  /// 다음과 같이 표시된다:
  ///   /command
  ///       syntax1     description1
  ///       syntax2     description2
  /// 구현하지 않으면 도움말은 <see cref="IChatCommandModel.Description"/> 로 대체된다.
  /// </summary>
  public interface IChatCommandUsage
  {
    /// <summary>
    /// 커맨드의 각 하위 커맨드/인자 형태를 설명하는 행들이다.
    /// </summary>
    public IReadOnlyList<UsageLine> UsageLines { get; }
  }

  public static class ChatCommandHelp
  {
    // 정렬된 사용법 행을 표시할 때 사용하는 열 간격과 들여쓰기.
    private const string LineIndent = "    ";
    private const int ColumnGap = 5;

    /// <summary>
    /// /help 목록에 표시되는 짧은 한 줄 요약을 반환한다.
    /// 커맨드의 Description 에 실수로 줄 바꿈이 포함되더라도 항상 첫 번째 비어 있지
    /// 않은 줄로 축약하여 목록이 깔끔하게 유지되도록 한다.
    /// </summary>
    public static string GetSummary(IChatCommandModel command)
    {
      if (command == null)
        return string.Empty;

      string description = command.Description;
      if (string.IsNullOrWhiteSpace(description))
        return string.Empty;

      int newlineIndex = description.IndexOf('\n');
      string firstLine = newlineIndex >= 0 ? description.Substring(0, newlineIndex) : description;
      return firstLine.Trim();
    }

    /// <summary>
    /// 커맨드의 전체 도움말 페이지를 만든다:
    ///   /command - summary
    ///       syntax1     description1
    ///       syntax2     description2
    /// 왼쪽(구문) 열은 자동 정렬되어 설명이 줄을 맞춘다.
    /// 커맨드에 구조화된 사용법이 없으면 한 줄 Description 으로 대체한다.
    /// </summary>
    public static string GetHelpPage(IChatCommandModel command)
    {
      if (command == null)
        return string.Empty;

      string summary = GetSummary(command);
      string header = string.IsNullOrEmpty(summary)
        ? $"/{command.CommandEntry}"
        : $"/{command.CommandEntry} - {summary}";

      if (command is IChatCommandUsage usageProvider)
      {
        IReadOnlyList<UsageLine> lines = usageProvider.UsageLines;
        if (lines != null && lines.Count > 0)
          return header + "\n" + RenderUsageLines(lines);
      }

      return header;
    }

    /// <summary>
    /// 사용법 행들을 정렬된 두 열의 들여쓰기 블록으로 표시한다.
    /// </summary>
    private static string RenderUsageLines(IReadOnlyList<UsageLine> lines)
    {
      // 설명이 정렬되도록 가장 넓은 구문 조각을 찾는다.
      int widest = 0;
      for (int i = 0; i < lines.Count; i++)
      {
        string syntax = lines[i].Syntax ?? string.Empty;
        if (syntax.Length > widest)
          widest = syntax.Length;
      }

      var builder = new StringBuilder();
      for (int i = 0; i < lines.Count; i++)
      {
        if (i > 0)
          builder.Append('\n');

        string syntax = lines[i].Syntax ?? string.Empty;
        string description = lines[i].Description ?? string.Empty;

        builder.Append(LineIndent);
        builder.Append(syntax);

        if (!string.IsNullOrEmpty(description))
        {
          int pad = (widest - syntax.Length) + ColumnGap;
          if (pad < 1)
            pad = 1;
          builder.Append(' ', pad);
          builder.Append(description);
        }
      }

      return builder.ToString();
    }

    /// <summary>
    /// 첫 번째 인자가 도움말 플래그(-h, --help, /?, ?, help)이면 true 를 반환한다.
    /// </summary>
    public static bool IsHelpFlag(string[] args)
    {
      if (args == null || args.Length == 0)
        return false;

      string first = args[0];
      if (string.IsNullOrWhiteSpace(first))
        return false;

      switch (first.Trim().ToLowerInvariant())
      {
        case "-h":
        case "--help":
        case "/?":
        case "?":
          return true;
        default:
          return false;
      }
    }
  }
}
