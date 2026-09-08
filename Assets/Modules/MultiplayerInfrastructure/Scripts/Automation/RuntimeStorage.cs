using UnityEngine;
namespace MultiplayerInfrastructure.Automation
{
  public static class RuntimeStorage
  {
    public static string PersistentDataPath
    {
      get
      {
#if UNITY_E2E || UNITY_EDITOR
        if (AutomationConfiguration.Valid) return AutomationConfiguration.Profile;
#endif
        return Application.persistentDataPath;
      }
    }
  }
}
