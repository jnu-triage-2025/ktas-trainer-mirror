using UnityEngine;
#if UNITY_E2E || UNITY_EDITOR
using System.IO;
using Newtonsoft.Json.Linq;
#endif

namespace MultiplayerInfrastructure.Automation
{
  public static class ProfilePlayerPrefs
  {
#if UNITY_E2E || UNITY_EDITOR
    private static JObject _data;
    private static bool Isolated => AutomationConfiguration.Valid;
    private static string StorePath => Path.Combine(AutomationConfiguration.Profile, "preferences.json");
    private static JObject Data => _data ??= File.Exists(StorePath) ? JObject.Parse(File.ReadAllText(StorePath)) : new JObject();
    internal static void ResetCache() => _data = null;
#endif
    public static int GetInt(string key, int fallback = 0)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) return Data.TryGetValue(key, out var value) ? value.Value<int>() : fallback;
#endif
      return PlayerPrefs.GetInt(key, fallback);
    }
    public static void SetInt(string key, int value)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) { Data[key] = value; return; }
#endif
      PlayerPrefs.SetInt(key, value);
    }
    public static float GetFloat(string key, float fallback = 0f)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) return Data.TryGetValue(key, out var value) ? value.Value<float>() : fallback;
#endif
      return PlayerPrefs.GetFloat(key, fallback);
    }
    public static void SetFloat(string key, float value)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) { Data[key] = value; return; }
#endif
      PlayerPrefs.SetFloat(key, value);
    }
    public static string GetString(string key, string fallback = "")
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) return Data.TryGetValue(key, out var value) ? value.Value<string>() : fallback;
#endif
      return PlayerPrefs.GetString(key, fallback);
    }
    public static void SetString(string key, string value)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) { Data[key] = value; return; }
#endif
      PlayerPrefs.SetString(key, value);
    }
    public static bool HasKey(string key)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) return Data.ContainsKey(key);
#endif
      return PlayerPrefs.HasKey(key);
    }
    public static void DeleteKey(string key)
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated) { Data.Remove(key); return; }
#endif
      PlayerPrefs.DeleteKey(key);
    }
    public static void Save()
    {
#if UNITY_E2E || UNITY_EDITOR
      if (Isolated)
      {
        Directory.CreateDirectory(AutomationConfiguration.Profile);
        var temporary = StorePath + ".tmp";
        File.WriteAllText(temporary, Data.ToString());
        if (File.Exists(StorePath)) File.Replace(temporary, StorePath, null);
        else File.Move(temporary, StorePath);
        return;
      }
#endif
      PlayerPrefs.Save();
    }
  }
}
