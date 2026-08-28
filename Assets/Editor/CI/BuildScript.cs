using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GitLabBuild
{
  /// <summary>
  /// Builds the player described by the BUILD_* environment variables.
  /// BUILD_SUBTARGET selects between the normal player ("Player") and the
  /// dedicated headless server ("Server").
  /// </summary>
  public static void Build()
  {
    string subtargetName = GetEnvironmentVariable("BUILD_SUBTARGET", StandaloneBuildSubtarget.Player.ToString());
    if (!Enum.TryParse(subtargetName, true, out StandaloneBuildSubtarget subtarget)
        || !Enum.IsDefined(typeof(StandaloneBuildSubtarget), subtarget))
    {
      throw new InvalidOperationException(
          $"Unsupported BUILD_SUBTARGET: {subtargetName}. Use 'Player' or 'Server'.");
    }

    BuildWithSubtarget(subtarget);
  }

  /// <summary>
  /// Convenience entry point for dedicated server builds. Equivalent to
  /// running <see cref="Build"/> with BUILD_SUBTARGET=Server.
  /// </summary>
  public static void BuildDedicatedServer()
  {
    BuildWithSubtarget(StandaloneBuildSubtarget.Server);
  }

  private static void BuildWithSubtarget(StandaloneBuildSubtarget subtarget)
  {
    string targetName = GetEnvironmentVariable("BUILD_TARGET", EditorUserBuildSettings.activeBuildTarget.ToString());
    string buildName = GetEnvironmentVariable("BUILD_NAME", "ktas-trainer");
    string buildDirectory = GetEnvironmentVariable("BUILD_PATH", "build");

    if (!Enum.TryParse(targetName, true, out BuildTarget target))
    {
      throw new InvalidOperationException($"Unsupported BUILD_TARGET: {targetName}");
    }

    if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
    {
      throw new InvalidOperationException(
          $"The installed Unity Editor does not support {targetName}. " +
          "Install the matching Unity Build Support module and run the build again.");
    }

    DirectoryInfo projectDirectory = Directory.GetParent(Application.dataPath);
    if (projectDirectory == null)
    {
      throw new InvalidOperationException("Could not resolve the Unity project directory.");
    }

    bool isServer = subtarget == StandaloneBuildSubtarget.Server;
    string projectPath = projectDirectory.FullName;
    string outputDirectory = Path.Combine(
        projectPath,
        buildDirectory,
        isServer ? $"{target}-Server" : target.ToString());
    Directory.CreateDirectory(outputDirectory);

    string locationPath = GetLocationPath(target, outputDirectory, buildName);
    string[] scenes = EditorBuildSettings.scenes
        .Where(scene => scene.enabled)
        .Select(scene => scene.path)
        .ToArray();

    if (scenes.Length == 0)
    {
      throw new InvalidOperationException("No enabled scenes are configured in EditorBuildSettings.");
    }

    // The subtarget has to be applied to the editor state as well, otherwise the
    // player is compiled without the UNITY_SERVER scripting define.
    StandaloneBuildSubtarget previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
    EditorUserBuildSettings.standaloneBuildSubtarget = subtarget;

    try
    {
      Debug.Log($"Building {targetName} ({subtarget}) to {locationPath}");
      BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
      {
        scenes = scenes,
        locationPathName = locationPath,
        target = target,
        subtarget = (int)subtarget,
        options = BuildOptions.StrictMode
      });

      Debug.Log($"Build result: {report.summary.result}, size: {report.summary.totalSize} bytes");
      if (report.summary.result != BuildResult.Succeeded)
      {
        string hint = isServer
            ? " Dedicated server builds also require the 'Dedicated Server Build Support' module for the selected platform."
            : string.Empty;
        throw new InvalidOperationException($"Unity build failed with result: {report.summary.result}.{hint}");
      }
    }
    finally
    {
      EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
    }
  }

  private static string GetEnvironmentVariable(string name, string fallback)
  {
    string value = Environment.GetEnvironmentVariable(name);
    return string.IsNullOrWhiteSpace(value) ? fallback : value;
  }

  private static string GetLocationPath(BuildTarget target, string outputDirectory, string buildName)
  {
    return target switch
    {
      BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => Path.Combine(outputDirectory, $"{buildName}.exe"),
      BuildTarget.StandaloneOSX => Path.Combine(outputDirectory, $"{buildName}.app"),
      BuildTarget.WebGL => outputDirectory,
      _ => Path.Combine(outputDirectory, buildName)
    };
  }
}
