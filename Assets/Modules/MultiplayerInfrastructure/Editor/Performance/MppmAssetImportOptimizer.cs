using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Performance
{
  /// <summary>
  /// MPPM Editor 클론의 중복 네이티브 메모리를 줄이기 위한 보수적인 Importer 일괄 최적화입니다.
  /// 라벨로 개별 자산을 정책에서 제외할 수 있습니다.
  /// </summary>
  public static class MppmAssetImportOptimizer
  {
    private const int GeneralTextureMaxSize = 2048;
    private const int EnvironmentTextureMaxSize = 1024;
    private const string ReportPath = "Library/MppmAssetImportOptimizerReport.txt";

    private const string KeepReadableLabel = "KeepReadable";
    private const string KeepMeshReadableLabel = "KeepMeshReadable";
    private const string KeepOriginalTextureSizeLabel = "KeepOriginalTextureSize";
    private const string KeepAnimationQualityLabel = "KeepAnimationQuality";

    // 런타임 GetPixel 사용이 확인된 자산. 라벨을 붙이지 않은 기존 프로젝트도 안전하게 보호한다.
    private static readonly HashSet<string> KnownCpuReadableTextures = new(StringComparer.Ordinal)
    {
      "Assets/Modules/UnityJapanOffice/Textures/UI/ColorTemperatureSample.png",
    };

    [MenuItem("Tools/Performance/Analyze MPPM Asset Import Settings")]
    public static void AnalyzeFromMenu() => Run(apply: false);

    [MenuItem("Tools/Performance/Apply MPPM Asset Import Optimization")]
    public static void ApplyFromMenu() => Run(apply: true);

    /// <summary>Unity -executeMethod 진입점입니다.</summary>
    public static void AnalyzeFromCommandLine() => RunAndExit(apply: false);

    /// <summary>Unity -executeMethod 진입점입니다.</summary>
    public static void ApplyFromCommandLine() => RunAndExit(apply: true);

    private static void RunAndExit(bool apply)
    {
      try
      {
        Run(apply);
        if (Application.isBatchMode)
          EditorApplication.Exit(0);
      }
      catch (Exception exception)
      {
        Debug.LogException(exception);
        if (Application.isBatchMode)
          EditorApplication.Exit(1);
        throw;
      }
    }

    private static void Run(bool apply)
    {
      var changedPaths = new List<string>();
      var stats = new OptimizationStats();

      if (apply)
        AssetDatabase.StartAssetEditing();

      try
      {
        OptimizeTextures(apply, changedPaths, stats);
        OptimizeModels(apply, changedPaths, stats);
      }
      finally
      {
        if (apply)
          AssetDatabase.StopAssetEditing();
      }

      if (apply)
      {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
      }

      WriteReport(changedPaths, stats, apply);
      Debug.Log(
        $"[MPPM Asset Optimizer] mode={(apply ? "APPLY" : "ANALYZE")}, " +
        $"textures={stats.TextureCount}, textureCandidates={stats.TextureCandidates}, " +
        $"models={stats.ModelCount}, modelCandidates={stats.ModelCandidates}, changed={changedPaths.Count}. " +
        $"Report: {ReportPath}");
    }

    private static void OptimizeTextures(
      bool apply,
      List<string> changedPaths,
      OptimizationStats stats)
    {
      foreach (string guid in AssetDatabase.FindAssets("t:Texture", new[] { "Assets" }))
      {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
          continue;

        stats.TextureCount++;
        var labels = AssetDatabase.GetLabels(importer);
        bool changed = false;

        if (!labels.Contains(KeepOriginalTextureSizeLabel, StringComparer.Ordinal))
        {
          int maxSize = IsEnvironmentTexture(path, importer)
            ? EnvironmentTextureMaxSize
            : GeneralTextureMaxSize;
          if (importer.maxTextureSize > maxSize)
          {
            changed = true;
            if (apply)
              importer.maxTextureSize = maxSize;
          }
        }

        bool mustStayReadable = KnownCpuReadableTextures.Contains(path) ||
                                labels.Contains(KeepReadableLabel, StringComparer.Ordinal);
        if (importer.isReadable && !mustStayReadable)
        {
          changed = true;
          if (apply)
            importer.isReadable = false;
        }

        bool shouldStream = ShouldStream(importer, path);
        if (importer.streamingMipmaps != shouldStream)
        {
          changed = true;
          if (apply)
            importer.streamingMipmaps = shouldStream;
        }

        if (!changed)
          continue;

        stats.TextureCandidates++;
        if (apply && AssetDatabase.WriteImportSettingsIfDirty(path))
          changedPaths.Add(path);
      }
    }

    private static void OptimizeModels(
      bool apply,
      List<string> changedPaths,
      OptimizationStats stats)
    {
      foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
      {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
          continue;

        stats.ModelCount++;
        var labels = AssetDatabase.GetLabels(importer);
        bool changed = false;

        if (importer.isReadable && !labels.Contains(KeepMeshReadableLabel, StringComparer.Ordinal))
        {
          changed = true;
          if (apply)
            importer.isReadable = false;
        }

        if (!importer.optimizeMeshPolygons)
        {
          changed = true;
          if (apply)
            importer.optimizeMeshPolygons = true;
        }

        if (!importer.optimizeMeshVertices)
        {
          changed = true;
          if (apply)
            importer.optimizeMeshVertices = true;
        }

        if (importer.importAnimation &&
            importer.animationCompression != ModelImporterAnimationCompression.Optimal &&
            !labels.Contains(KeepAnimationQualityLabel, StringComparer.Ordinal))
        {
          changed = true;
          if (apply)
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        }

        if (!changed)
          continue;

        stats.ModelCandidates++;
        if (apply && AssetDatabase.WriteImportSettingsIfDirty(path))
          changedPaths.Add(path);
      }
    }

    private static bool IsEnvironmentTexture(string path, TextureImporter importer)
    {
      string extension = Path.GetExtension(path);
      return importer.textureShape == TextureImporterShape.TextureCube ||
             extension.Equals(".exr", StringComparison.OrdinalIgnoreCase) ||
             extension.Equals(".hdr", StringComparison.OrdinalIgnoreCase) ||
             path.IndexOf("ReflectionProbe", StringComparison.OrdinalIgnoreCase) >= 0 ||
             path.IndexOf("HDRI", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ShouldStream(TextureImporter importer, string path)
    {
      if (!importer.mipmapEnabled || importer.textureShape != TextureImporterShape.Texture2D)
        return false;

      if (importer.textureType != TextureImporterType.Default &&
          importer.textureType != TextureImporterType.NormalMap)
        return false;

      return path.IndexOf("/UI/", StringComparison.OrdinalIgnoreCase) < 0 &&
             path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) < 0 &&
             path.IndexOf("/Gizmos/", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static void WriteReport(
      IReadOnlyCollection<string> changedPaths,
      OptimizationStats stats,
      bool apply)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Library");
      var lines = new List<string>
      {
        $"mode={(apply ? "APPLY" : "ANALYZE")}",
        $"textures={stats.TextureCount}",
        $"textureCandidates={stats.TextureCandidates}",
        $"models={stats.ModelCount}",
        $"modelCandidates={stats.ModelCandidates}",
        $"changed={changedPaths.Count}",
        "---",
      };
      lines.AddRange(changedPaths.OrderBy(path => path, StringComparer.Ordinal));
      File.WriteAllLines(ReportPath, lines);
    }

    private sealed class OptimizationStats
    {
      public int TextureCount;
      public int TextureCandidates;
      public int ModelCount;
      public int ModelCandidates;
    }
  }
}
