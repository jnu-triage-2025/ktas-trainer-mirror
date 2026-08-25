using System.Collections.Generic;
using System.Text;
using FishNet.Connection;

namespace MultiplayerInfrastructure.Command
{
  public interface IChatCommandModel
  {
    public string CommandEntry { get; }

    /// <summary>
    /// One-line human readable description used by /help listings.
    /// Keep this short: a single sentence, no line breaks.
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
  /// A single usage row: a syntax fragment (subcommand + arguments) on the
  /// left and its explanation on the right. Rendered as an aligned two-column
  /// row under the command name.
  /// </summary>
  public readonly struct UsageLine
  {
    /// <summary>Left column, e.g. "add &lt;target&gt; &lt;tag&gt;".</summary>
    public readonly string Syntax;

    /// <summary>Right column explanation. May be empty.</summary>
    public readonly string Description;

    public UsageLine(string syntax, string description)
    {
      Syntax = syntax ?? string.Empty;
      Description = description ?? string.Empty;
    }

    public UsageLine(string syntax) : this(syntax, string.Empty) { }
  }

  /// <summary>
  /// Optional interface for commands that expose structured, multi-line usage.
  /// Rendered as:
  ///   /command
  ///       syntax1     description1
  ///       syntax2     description2
  /// When not implemented, help falls back to <see cref="IChatCommandModel.Description"/>.
  /// </summary>
  public interface IChatCommandUsage
  {
    /// <summary>
    /// The rows describing each subcommand/argument form of the command.
    /// </summary>
    public IReadOnlyList<UsageLine> UsageLines { get; }
  }

  public static class ChatCommandHelp
  {
    // Column gap and indentation used when rendering aligned usage rows.
    private const string LineIndent = "    ";
    private const int ColumnGap = 5;

    /// <summary>
    /// Returns the short, single-line summary shown in /help listings.
    /// Always collapses to the first non-empty line so listings stay clean
    /// even if a command's Description accidentally contains line breaks.
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
    /// Builds the full help page for a command:
    ///   /command - summary
    ///       syntax1     description1
    ///       syntax2     description2
    /// The left (syntax) column is auto-aligned so descriptions line up.
    /// Falls back to the one-line Description when the command has no
    /// structured usage.
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
    /// Renders usage rows into an aligned two-column, indented block.
    /// </summary>
    private static string RenderUsageLines(IReadOnlyList<UsageLine> lines)
    {
      // Determine the widest syntax fragment so descriptions align.
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
    /// True when the first argument is a help flag (-h, --help, /?, ?, help).
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
