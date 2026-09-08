using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MultiplayerInfrastructure.Automation.Editor
{
  // Inspect compiled player metadata, never the Editor assembly (which intentionally includes the bridge).
  public sealed class AutomationBuildGuard : IPostBuildPlayerScriptDLLs
  {
    private static readonly string[] RequiredTypes = {
      "AutomationBridge", "AutomationConfiguration", "AutomationInputSource", "AutomationUI",
      "AutomationEvents", "AutomationContent", "AutomationFixtures", "AutomationGeometry"
    };
    private static readonly HashSet<string> OrdinaryTypes = new HashSet<string> {
      "IPlayerInputSource", "LegacyPlayerInputSource", "PlayerInput", "ProfilePlayerPrefs", "RuntimeStorage"
    };
    internal static string[] Inspect(AssemblyDefinition assembly, bool instrumented)
    {
      const string prefix = "MultiplayerInfrastructure.Automation";
      var problems = new List<string>();
      if (instrumented)
      {
        foreach (string name in RequiredTypes)
          if (assembly.MainModule.GetType(prefix + "." + name) == null)
            problems.Add("Missing runtime type: " + name);
      }
      else
      {
        foreach (var type in assembly.MainModule.GetTypes())
        {
          if (type.Namespace == prefix && !OrdinaryTypes.Contains(type.Name))
            problems.Add("Automation runtime type remains: " + type.FullName);
          if (type.FullName == prefix + ".PlayerInput" && type.Fields.Any(field => field.Name == "Override"))
            problems.Add("Automation input override remains");
          foreach (var method in type.Methods)
          {
            if (type.Namespace.StartsWith("MultiplayerInfrastructure", StringComparison.Ordinal)
                && method.Name.StartsWith("Automation", StringComparison.Ordinal))
              problems.Add("Automation probe remains: " + method.FullName);
            if (method.HasBody && method.Body.Instructions.Any(instruction =>
                instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldstr && instruction.Operand is string text
                && text.StartsWith("UNITY_E2E_", StringComparison.Ordinal)))
              problems.Add("Automation environment setting remains: " + method.FullName);
          }
        }
      }
      return problems.ToArray();
    }
    public int callbackOrder => 10000;
    public void OnPostBuildPlayerScriptDLLs(BuildReport report)
    {
      var files = report.GetFiles().Where(file => Path.GetFileName(file.path) == "Assembly-CSharp.dll" && File.Exists(file.path)).ToArray();
      if (files.Length == 0) throw new BuildFailedException("E2E build guard could not locate the compiled player assembly.");
      foreach (var file in files)
      {
        using var assembly = AssemblyDefinition.ReadAssembly(file.path);
        var problems = Inspect(assembly, AutomationBuild.InProgress);
        if (problems.Length > 0) throw new BuildFailedException(string.Join("\n", problems));
      }
    }
  }
}
