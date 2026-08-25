using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Server
{
  /// <summary>서버 재시작 후에도 유지되는 표시 이름 기반 차단 목록입니다.</summary>
  public static class ServerBanService
  {
    [Serializable]
    private sealed class BanFile
    {
      public List<string> names = new();
    }

    private static readonly HashSet<string> _bannedNames = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    private static string FilePath => Path.Combine(Application.dataPath, "..", "server-bans.json");

    public static void EnsureLoaded()
    {
      if (_loaded)
        return;

      _loaded = true;
      if (!File.Exists(FilePath))
        return;

      try
      {
        var file = JsonUtility.FromJson<BanFile>(File.ReadAllText(FilePath));
        if (file?.names == null)
          return;
        foreach (string name in file.names.Where(name => !string.IsNullOrWhiteSpace(name)))
          _bannedNames.Add(name.Trim());
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ServerBanService] Failed to load '{FilePath}': {ex.Message}");
      }
    }

    public static bool IsBanned(string displayName)
    {
      EnsureLoaded();
      return !string.IsNullOrWhiteSpace(displayName) && _bannedNames.Contains(displayName.Trim());
    }

    public static bool Ban(string displayName)
    {
      EnsureLoaded();
      if (string.IsNullOrWhiteSpace(displayName) || !_bannedNames.Add(displayName.Trim()))
        return false;
      Save();
      return true;
    }

    public static bool Unban(string displayName)
    {
      EnsureLoaded();
      if (string.IsNullOrWhiteSpace(displayName) || !_bannedNames.Remove(displayName.Trim()))
        return false;
      Save();
      return true;
    }

    public static IReadOnlyList<string> GetBanList()
    {
      EnsureLoaded();
      return _bannedNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void Save()
    {
      try
      {
        File.WriteAllText(FilePath, JsonUtility.ToJson(new BanFile
        {
          names = _bannedNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList(),
        }, true));
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ServerBanService] Failed to save '{FilePath}': {ex.Message}");
      }
    }
  }
}
