using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Problem;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    [Serializable]
    private sealed class ProblemPackManifest
    {
      public string format;
      public string problemJson;
      public string figuresBasePath;
    }

    public static bool PreloadProblemSet(string identifier)
    {
      EnsureBuiltInRegistryInitialized();

      string normalizedIdentifier = NormalizeProblemIdentifier(identifier);
      if (string.IsNullOrWhiteSpace(normalizedIdentifier))
        return false;

      var registry = ResolveRegistry(RegistryType.ProblemSet);
      if (!registry.TryGetValue(normalizedIdentifier, out var definition))
      {
        var textAsset = TryLoadProblemSetTextAsset(normalizedIdentifier);
        if (textAsset == null)
          return false;

        registry[normalizedIdentifier] = textAsset;
        definition = textAsset;
      }

      return TryResolveProblemSet(RegistryType.ProblemSet, normalizedIdentifier, registry, ref definition);
    }

    public static bool TryGetProblemSet(string identifier, out ProblemSetDefinition problemSet, out string error)
    {
      problemSet = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(identifier))
      {
        error = "Problem set identifier is required.";
        return false;
      }

      string normalizedIdentifier = NormalizeProblemIdentifier(identifier);
      if (!PreloadProblemSet(normalizedIdentifier))
      {
        error = $"Problem set '{identifier}' is not registered or failed to parse.";
        return false;
      }

      problemSet = Get<ProblemSetDefinition>(RegistryType.ProblemSet, normalizedIdentifier);
      if (problemSet == null)
      {
        error = $"Problem set '{identifier}' could not be resolved.";
        return false;
      }

      return true;
    }

    public static bool TryGetProblemIdentifierFromManifest(string manifestResourceName, out string problemIdentifier)
    {
      problemIdentifier = string.Empty;

      if (string.IsNullOrWhiteSpace(manifestResourceName))
        return false;

      var textAsset = LoadManifestTextAsset(manifestResourceName);
      if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
        return false;

      ProblemPackManifest manifest;
      try
      {
        manifest = JsonUtility.FromJson<ProblemPackManifest>(textAsset.text);
      }
      catch
      {
        return false;
      }

      if (manifest == null)
        return false;

      if (!string.IsNullOrWhiteSpace(manifest.problemJson))
      {
        problemIdentifier = NormalizeProblemIdentifier(manifest.problemJson);
        if (!string.IsNullOrWhiteSpace(problemIdentifier))
          return true;
      }

      if (!string.IsNullOrWhiteSpace(manifestResourceName))
      {
        problemIdentifier = NormalizeProblemIdentifier(manifestResourceName);
        if (!string.IsNullOrWhiteSpace(problemIdentifier))
          return true;
      }

      return false;
    }

    private static string NormalizeProblemIdentifier(string jsonReference)
    {
      if (string.IsNullOrWhiteSpace(jsonReference))
        return string.Empty;

      var normalized = jsonReference.Trim().Replace('\\', '/');
      const string problemsPrefix = "Problems/";
      if (normalized.StartsWith(problemsPrefix, StringComparison.OrdinalIgnoreCase))
        normalized = normalized.Substring(problemsPrefix.Length);

      int slashIndex = normalized.LastIndexOf('/');
      if (slashIndex >= 0 && slashIndex < normalized.Length - 1)
        normalized = normalized.Substring(slashIndex + 1);

      if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        normalized = normalized.Substring(0, normalized.Length - 5);

      return normalized.Trim();
    }

    private static TextAsset TryLoadProblemSetTextAsset(string normalizedIdentifier)
    {
      if (string.IsNullOrWhiteSpace(normalizedIdentifier))
        return null;

      var candidates = new List<string>
      {
        normalizedIdentifier,
        $"{normalizedIdentifier}/{normalizedIdentifier}"
      };

      foreach (var candidate in candidates)
      {
        if (string.IsNullOrWhiteSpace(candidate))
          continue;

        var textAsset = Resources.Load<TextAsset>($"Problems/{candidate}");
        if (textAsset != null)
          return textAsset;
      }

      return null;
    }

    private static TextAsset LoadManifestTextAsset(string manifestResourceName)
    {
      if (string.IsNullOrWhiteSpace(manifestResourceName))
        return null;

      string normalizedPath = manifestResourceName.Trim().Replace('\\', '/');
      const string problemsPrefix = "Problems/";
      if (normalizedPath.StartsWith(problemsPrefix, StringComparison.OrdinalIgnoreCase))
        normalizedPath = normalizedPath.Substring(problemsPrefix.Length);

      if (normalizedPath.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
        normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - ".manifest".Length);

      var textAsset = Resources.Load<TextAsset>($"Problems/{normalizedPath}");
      if (textAsset != null)
        return textAsset;

      var manifests = Resources.LoadAll<TextAsset>("Problems");
      foreach (var candidate in manifests)
      {
        if (candidate == null)
          continue;

        if (string.Equals(candidate.name, "problem-pack.manifest", StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.name, "problem-pack", StringComparison.OrdinalIgnoreCase))
          return candidate;
      }

      return null;
    }

    public static bool PreloadProblemSetFromManifest(string manifestResourceName = "problem-pack.manifest")
    {
      if (!TryGetProblemIdentifierFromManifest(manifestResourceName, out var problemIdentifier))
        return false;

      return PreloadProblemSet(problemIdentifier);
    }

    public static void RegisterProblemFigure(string identifier, Texture2D texture)
    {
      if (string.IsNullOrWhiteSpace(identifier) || texture == null)
        return;

      Register(RegistryType.ProblemFigure, identifier, texture);
    }

    public static bool TryGetProblemFigure(string identifier, out Texture2D texture)
      => TryGet(RegistryType.ProblemFigure, identifier, out texture);

    public static bool TryResolveProblemFigureReference(string figureReference, out Texture2D texture)
    {
      texture = null;

      if (string.IsNullOrWhiteSpace(figureReference))
        return false;

      string id = ParseProblemFigureIdentifier(figureReference);
      if (string.IsNullOrWhiteSpace(id))
        return false;

      if (TryGet<Texture2D>(RegistryType.ProblemFigure, id, out texture) && texture != null)
        return true;

      texture = Resources.Load<Texture2D>($"ProblemFigures/{id}");
      if (texture == null)
        texture = Resources.Load<Texture2D>($"Problems/figures/{id}");

      if (texture == null)
        texture = Resources.Load<Texture2D>($"Problems/{id}");

      if (texture == null)
      {
        var allFigures = Resources.LoadAll<Texture2D>("Problems");
        for (int i = 0; i < allFigures.Length; i++)
        {
          var candidate = allFigures[i];
          if (candidate == null)
            continue;

          if (string.Equals(candidate.name, id, StringComparison.OrdinalIgnoreCase))
          {
            texture = candidate;
            break;
          }
        }
      }

      if (texture == null)
        return false;

      Register(RegistryType.ProblemFigure, id, texture);
      return true;
    }

    public static string ParseProblemFigureIdentifier(string figureReference)
    {
      if (string.IsNullOrWhiteSpace(figureReference))
        return string.Empty;

      const string prefix = "probfig:";
      if (!figureReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return string.Empty;

      string raw = figureReference.Substring(prefix.Length).Trim();
      if (raw.StartsWith("(", StringComparison.Ordinal) && raw.EndsWith(")", StringComparison.Ordinal) && raw.Length > 2)
        raw = raw.Substring(1, raw.Length - 2).Trim();

      return raw;
    }

    private static bool TryResolveProblemSet(
      RegistryType registryType,
      string identifier,
      Dictionary<string, object> registry,
      ref object definition)
    {
      if (registryType != RegistryType.ProblemSet)
        return true;

      if (definition is ProblemSetDefinition)
        return true;

      if (definition is not TextAsset textAsset)
        return false;

      if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
        return false;

      try
      {
        var parsed = ProblemSetLoader.LoadFromJson(textAsset.text);
        if (string.IsNullOrWhiteSpace(parsed.Identifier))
          parsed.Identifier = identifier;

        registry[identifier] = parsed;
        definition = parsed;
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Registry] Failed to load ProblemSet '{identifier}': {ex.Message}");
        return false;
      }
    }
  }
}
