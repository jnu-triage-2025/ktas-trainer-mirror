using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerInfrastructure.Session
{
  [Serializable]
  public sealed class SessionConfiguration
  {
    public string address = "localhost";
    public ushort port = 37891;
    public string sessionName = "MyFishSession";
    public string[] datapacks = Array.Empty<string>();
  }

  /// <summary>세션을 열 때 사용하는 개발/런타임 기본값을 불러온다.</summary>
  public static class SessionConfigurationService
  {
    private const string RelativePath = "Session/session.config.json";
    private static SessionConfiguration _current;

    public static SessionConfiguration Current
    {
      get
      {
        EnsureLoaded();
        return _current;
      }
    }

    public static IReadOnlyList<string> DatapackIds => Current.datapacks ?? Array.Empty<string>();

    public static void EnsureLoaded()
    {
      if (_current != null)
        return;

      _current = new SessionConfiguration();
      string path = Path.Combine(Application.streamingAssetsPath, RelativePath);
      try
      {
        if (!File.Exists(path))
        {
          Debug.LogWarning($"[SessionConfigurationService] Configuration not found at '{path}'. Using built-in defaults.");
          return;
        }

        var loaded = JsonUtility.FromJson<SessionConfiguration>(File.ReadAllText(path));
        if (loaded == null)
          throw new InvalidDataException("Configuration JSON produced no object.");
        if (string.IsNullOrWhiteSpace(loaded.address))
          throw new InvalidDataException("address is required.");
        if (loaded.port == 0)
          throw new InvalidDataException("port must be between 1 and 65535.");
        if (string.IsNullOrWhiteSpace(loaded.sessionName))
          loaded.sessionName = _current.sessionName;
        if (loaded.datapacks == null)
          loaded.datapacks = Array.Empty<string>();
        _current = loaded;
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[SessionConfigurationService] Failed to load '{path}': {ex.Message}. Using built-in defaults.");
      }
    }
  }
}
