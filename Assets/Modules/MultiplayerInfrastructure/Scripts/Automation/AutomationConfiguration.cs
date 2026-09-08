#if UNITY_E2E || UNITY_EDITOR
using System;
using System.IO;
using System.Linq;

namespace MultiplayerInfrastructure.Automation
{
  internal static class AutomationConfiguration
  {
    internal static bool Enabled => Environment.GetCommandLineArgs().Contains("--e2e")
      || (UnityEngine.Application.isEditor && Environment.GetEnvironmentVariable("UNITY_E2E_EDITOR") == "1");
    internal static string Token => Environment.GetEnvironmentVariable("UNITY_E2E_TOKEN");
    internal static string InstanceId => Environment.GetEnvironmentVariable("UNITY_E2E_INSTANCE") ?? "editor";
    internal static string RunId => Environment.GetEnvironmentVariable("UNITY_E2E_RUN") ?? "exploration";
    internal static string Profile => Environment.GetEnvironmentVariable("UNITY_E2E_PROFILE");
    internal static bool AllowChatCommands => !Valid || Environment.GetEnvironmentVariable("UNITY_E2E_ALLOW_CHAT_COMMANDS") == "1";
    internal static bool AllowProtocolTests => Valid && Environment.GetEnvironmentVariable("UNITY_E2E_ALLOW_PROTOCOL_TESTS") == "1";
    internal static bool AllowScenarioFixtures => Valid && Environment.GetEnvironmentVariable("UNITY_E2E_ALLOW_SCENARIO_FIXTURES") == "1";
    internal static int ControlLeaseMs
    {
      get
      {
        string raw = Environment.GetEnvironmentVariable("UNITY_E2E_CONTROL_LEASE_MS");
        if (string.IsNullOrEmpty(raw)) return 2000;
        if (!int.TryParse(raw, out int value) || value < 2000 || value > 10000)
          throw new ArgumentException("INVALID_CONTROL_LEASE");
        return value;
      }
    }
    internal static int Port => int.TryParse(Environment.GetEnvironmentVariable("UNITY_E2E_PORT"), out int value) ? value : 17891;
    internal static bool Valid => Enabled && Token?.Length >= 32 && Path.IsPathRooted(Profile ?? "") && Port > 1024 && Port <= 65535;
  }
}
#endif
