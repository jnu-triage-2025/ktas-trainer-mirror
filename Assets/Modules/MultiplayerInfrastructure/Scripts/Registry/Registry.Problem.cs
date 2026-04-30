using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Problem;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    public static bool PreloadProblemSet(string identifier)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      var registry = ResolveRegistry(RegistryType.ProblemSet);
      if (!registry.TryGetValue(identifier, out var definition))
      {
        var textAsset = Resources.Load<TextAsset>($"Problems/{identifier}");
        if (textAsset == null)
          return false;

        registry[identifier] = textAsset;
        definition = textAsset;
      }

      return TryResolveProblemSet(RegistryType.ProblemSet, identifier, registry, ref definition);
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

      if (!PreloadProblemSet(identifier))
      {
        error = $"Problem set '{identifier}' is not registered or failed to parse.";
        return false;
      }

      problemSet = Get<ProblemSetDefinition>(RegistryType.ProblemSet, identifier);
      if (problemSet == null)
      {
        error = $"Problem set '{identifier}' could not be resolved.";
        return false;
      }

      return true;
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
