#if UNITY_E2E || UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
namespace MultiplayerInfrastructure.Automation
{
  internal static class AutomationEvents
  {
    internal static event Action<string, string, JObject> Published;
    internal static void Publish(string type, string side, JObject payload)
    {
      // A probe failure must never change scenario execution.
      try { Published?.Invoke(type, side, payload); } catch { }
    }
  }
}
#endif
