using System;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

namespace MultiplayerInfrastructure.Variable
{
  public static class SessionVariableService
  {
    public sealed class ObjectiveDefinition
    {
      public string Name { get; }
      public string Criteria { get; }

      public ObjectiveDefinition(string name, string criteria)
      {
        Name = name;
        Criteria = criteria;
      }
    }

    private static readonly Dictionary<string, ObjectiveDefinition> _objectives =
      new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Dictionary<string, int>> _scoresByObjective =
      new(StringComparer.OrdinalIgnoreCase);

    private static bool IsServerMutationAllowed()
    {
      if (InstanceFinder.IsServerStarted)
        return true;

      Debug.LogWarning("[SessionVariableService] Variable mutation attempted outside server context. Ignored.");
      return false;
    }

    public static bool AddObjective(string objective, string criteria, out string error)
    {
      error = string.Empty;
      if (!IsServerMutationAllowed())
      {
        error = "Variables can only be modified on the server.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(objective))
      {
        error = "Objective name is required.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(criteria))
      {
        error = "Criteria is required.";
        return false;
      }

      if (_objectives.ContainsKey(objective))
      {
        error = $"Objective '{objective}' already exists.";
        return false;
      }

      _objectives[objective] = new ObjectiveDefinition(objective, criteria);
      _scoresByObjective[objective] = new Dictionary<string, int>(StringComparer.Ordinal);
      return true;
    }

    public static bool RemoveObjective(string objective, out string error)
    {
      error = string.Empty;
      if (!IsServerMutationAllowed())
      {
        error = "Variables can only be modified on the server.";
        return false;
      }

      if (!_objectives.Remove(objective))
      {
        error = $"Objective '{objective}' does not exist.";
        return false;
      }

      _scoresByObjective.Remove(objective);
      return true;
    }

    public static IReadOnlyCollection<ObjectiveDefinition> GetObjectives()
      => _objectives.Values;

    public static bool ContainsObjective(string objective)
      => !string.IsNullOrWhiteSpace(objective) && _objectives.ContainsKey(objective);

    public static bool TryGetScore(string userIdentifier, string objective, out int value)
    {
      value = 0;
      if (!TryGetObjectiveScoreMap(objective, out var scores))
        return false;

      return scores.TryGetValue(userIdentifier, out value);
    }

    public static bool SetScore(string userIdentifier, string objective, int value, out string error)
    {
      error = string.Empty;
      if (!IsServerMutationAllowed())
      {
        error = "Variables can only be modified on the server.";
        return false;
      }

      if (!TryGetObjectiveScoreMap(objective, out var scores))
      {
        error = $"Objective '{objective}' does not exist.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(userIdentifier))
      {
        error = "Target player identifier is invalid.";
        return false;
      }

      scores[userIdentifier] = value;
      return true;
    }

    public static bool AddScore(string userIdentifier, string objective, int delta, out string error)
    {
      if (!TryGetScore(userIdentifier, objective, out var current))
        current = 0;

      return SetScore(userIdentifier, objective, SaturatingAdd(current, delta), out error);
    }

    public static bool RemoveScore(string userIdentifier, string objective, int delta, out string error)
    {
      if (!TryGetScore(userIdentifier, objective, out var current))
        current = 0;

      return SetScore(userIdentifier, objective, SaturatingAdd(current, -delta), out error);
    }

    public static bool ResetScore(string userIdentifier, string objective, out string error)
    {
      error = string.Empty;
      if (!IsServerMutationAllowed())
      {
        error = "Variables can only be modified on the server.";
        return false;
      }

      if (!TryGetObjectiveScoreMap(objective, out var scores))
      {
        error = $"Objective '{objective}' does not exist.";
        return false;
      }

      scores.Remove(userIdentifier);
      return true;
    }

    public static bool ResetAllScores(string userIdentifier, out string error)
    {
      error = string.Empty;
      if (!IsServerMutationAllowed())
      {
        error = "Variables can only be modified on the server.";
        return false;
      }

      foreach (var kv in _scoresByObjective)
        kv.Value.Remove(userIdentifier);

      return true;
    }

    public static IReadOnlyDictionary<string, int> GetScoresForUser(string userIdentifier)
    {
      var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(userIdentifier))
        return result;

      foreach (var pair in _scoresByObjective)
      {
        if (pair.Value.TryGetValue(userIdentifier, out var value))
          result[pair.Key] = value;
      }

      return result;
    }

    public static bool ApplyOperation(
      string targetIdentifier,
      string targetObjective,
      string operation,
      string sourceIdentifier,
      string sourceObjective,
      out string error)
    {
      error = string.Empty;
      if (!IsServerMutationAllowed())
      {
        error = "Variables can only be modified on the server.";
        return false;
      }

      if (!TryGetObjectiveScoreMap(targetObjective, out var targetMap)
          || !TryGetObjectiveScoreMap(sourceObjective, out var sourceMap))
      {
        error = "Target or source objective does not exist.";
        return false;
      }

      int target = targetMap.TryGetValue(targetIdentifier, out var t) ? t : 0;
      int source = sourceMap.TryGetValue(sourceIdentifier, out var s) ? s : 0;

      switch (operation)
      {
        case "+=":
          targetMap[targetIdentifier] = SaturatingAdd(target, source);
          return true;
        case "-=":
          targetMap[targetIdentifier] = SaturatingAdd(target, -source);
          return true;
        case "*=":
          targetMap[targetIdentifier] = SaturatingMultiply(target, source);
          return true;
        case "/=":
          if (source == 0)
          {
            error = "Division by zero is not allowed.";
            return false;
          }

          targetMap[targetIdentifier] = target / source;
          return true;
        case "%=":
          if (source == 0)
          {
            error = "Modulo by zero is not allowed.";
            return false;
          }

          targetMap[targetIdentifier] = target % source;
          return true;
        case "=":
          targetMap[targetIdentifier] = source;
          return true;
        case "<":
          targetMap[targetIdentifier] = Math.Min(target, source);
          return true;
        case ">":
          targetMap[targetIdentifier] = Math.Max(target, source);
          return true;
        case "><":
          targetMap[targetIdentifier] = source;
          sourceMap[sourceIdentifier] = target;
          return true;
        default:
          error = $"Unsupported operation '{operation}'.";
          return false;
      }
    }

    private static bool TryGetObjectiveScoreMap(string objective, out Dictionary<string, int> scores)
    {
      scores = null;
      if (string.IsNullOrWhiteSpace(objective))
        return false;

      return _scoresByObjective.TryGetValue(objective, out scores);
    }

    private static int SaturatingAdd(int a, int b)
    {
      long sum = (long)a + b;
      if (sum > int.MaxValue)
        return int.MaxValue;
      if (sum < int.MinValue)
        return int.MinValue;
      return (int)sum;
    }

    private static int SaturatingMultiply(int a, int b)
    {
      long value = (long)a * b;
      if (value > int.MaxValue)
        return int.MaxValue;
      if (value < int.MinValue)
        return int.MinValue;
      return (int)value;
    }
  }
}
