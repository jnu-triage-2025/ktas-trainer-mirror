using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>
  /// Scenario Graph Editor의 "최근 연 파일(Open Recent)" 목록 저장소.
  /// 목록은 프로젝트 로컬 캐시(Library/ScenarioGraphEditor/recent_files.json)에 저장된다.
  /// 이 경로는 버전 관리에 포함되지 않지만 Unity 재시작 후에도 유지된다.
  /// </summary>
  public static class ScenarioGraphEditorRecentStore
  {
    public const int MaxEntries = 12;

    private const string CacheDirectoryName = "ScenarioGraphEditor";
    private const string CacheFileName = "recent_files.json";

    private sealed class RecentFilesData
    {
      public List<string> Paths { get; set; } = new List<string>();
    }

    private static string CacheFilePath
    {
      get
      {
        var root = GetProjectRoot();
        return string.IsNullOrEmpty(root) ? null : Path.Combine(root, "Library", CacheDirectoryName, CacheFileName);
      }
    }

    /// <summary>
    /// 최근 목록을 읽어 아직 파일이 존재하는 항목만 반환한다.
    /// 삭제된 항목이 제외되었다면 캐시 파일에도 결과를 바로 반영한다.
    /// </summary>
    public static IReadOnlyList<string> LoadExisting()
    {
      var paths = LoadRaw();
      var existing = paths.Where(File.Exists).ToList();
      if (existing.Count != paths.Count)
        WritePaths(existing);
      return existing;
    }

    /// <summary>
    /// 경로를 최근 목록 맨 앞에 기록한다. 중복은 제거되고 MaxEntries 개수로 제한된다.
    /// </summary>
    public static void Record(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        return;

      string normalized;
      try
      {
        normalized = Path.GetFullPath(path);
      }
      catch
      {
        return;
      }

      var paths = LoadRaw();
      paths.RemoveAll(each => string.Equals(each, normalized, StringComparison.OrdinalIgnoreCase));
      paths.Insert(0, normalized);
      if (paths.Count > MaxEntries)
        paths.RemoveRange(MaxEntries, paths.Count - MaxEntries);

      WritePaths(paths);
    }

    /// <summary>
    /// 최근 목록을 비운다.
    /// </summary>
    public static void Clear()
    {
      WritePaths(new List<string>());
    }

    private static List<string> LoadRaw()
    {
      var file = CacheFilePath;
      if (string.IsNullOrEmpty(file))
        return new List<string>();

      if (!File.Exists(file))
        return new List<string>();

      return ReadPaths(file);
    }

    private static List<string> ReadPaths(string file)
    {
      try
      {
        var json = File.ReadAllText(file);
        var data = JsonSerializer.Deserialize<RecentFilesData>(json);
        return data?.Paths?.Where(each => !string.IsNullOrWhiteSpace(each)).ToList() ?? new List<string>();
      }
      catch
      {
        // 캐시 파일이 손상되었으면 빈 목록으로 시작한다.
        return new List<string>();
      }
    }

    private static void WritePaths(List<string> paths)
    {
      var file = CacheFilePath;
      if (string.IsNullOrEmpty(file))
        return;

      try
      {
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        var json = JsonSerializer.Serialize(
          new RecentFilesData { Paths = paths },
          new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[ScenarioGraphEditor] 최근 연 파일 목록 저장 실패: {ex.Message}");
      }
    }

    private static string GetProjectRoot()
    {
      return Directory.GetParent(Application.dataPath)?.FullName;
    }
  }
}
