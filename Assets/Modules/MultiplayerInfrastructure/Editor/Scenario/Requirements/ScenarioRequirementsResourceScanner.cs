#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  internal sealed class ScenarioRequirementAssetPair
  {
    public string ScenarioPath { get; }
    public string SidecarPath { get; }
    public byte[] SourceBytes { get; }
    public ScenarioRequirementAssetPair(string scenarioPath, string sidecarPath, byte[] sourceBytes) { ScenarioPath = scenarioPath; SidecarPath = sidecarPath; SourceBytes = sourceBytes; }
  }

  internal static class ScenarioRequirementsResourceScanner
  {
    public static IReadOnlyList<ScenarioRequirementAssetPair> Discover()
    {
      return AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(path => path.EndsWith(".scenario.json", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => new ScenarioRequirementAssetPair(path, DeriveSidecarPath(path), File.ReadAllBytes(path)))
        .ToArray();
    }

    private static string DeriveSidecarPath(string scenarioPath)
      => scenarioPath.Substring(0, scenarioPath.Length - ".scenario.json".Length) + ".scenario.requirements.json";
  }
}
#endif
