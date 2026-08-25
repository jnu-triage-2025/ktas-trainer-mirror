using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GitLabBuild
{
  public static void Build()
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

    string projectPath = projectDirectory.FullName;
    string outputDirectory = Path.Combine(projectPath, buildDirectory, target.ToString());
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

    Debug.Log($"Building {targetName} to {locationPath}");
    BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
    {
      scenes = scenes,
      locationPathName = locationPath,
      target = target,
      options = BuildOptions.StrictMode
    });

    Debug.Log($"Build result: {report.summary.result}, size: {report.summary.totalSize} bytes");
    if (report.summary.result != BuildResult.Succeeded)
    {
      throw new InvalidOperationException($"Unity build failed with result: {report.summary.result}");
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
