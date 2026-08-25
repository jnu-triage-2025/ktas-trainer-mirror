using System;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Variable;
using UnityEngine;

namespace MultiplayerInfrastructure.Problem
{
  public static class ProblemRewardService
  {
    public static void ApplyOnCorrect(ProblemDefinition problem, int ownerClientId)
    {
      if (problem?.OnCorrect?.Scoreboard == null || problem.OnCorrect.Scoreboard.Count == 0)
        return;

      if (!UserDescriptorService.TryGetByClientId(ownerClientId, out var user) || user == null)
      {
        Debug.LogWarning($"[ProblemRewardService] Cannot resolve user for clientId '{ownerClientId}'.");
        return;
      }

      string userIdentifier = user.Identifier;
      if (string.IsNullOrWhiteSpace(userIdentifier))
      {
        Debug.LogWarning($"[ProblemRewardService] User identifier is empty for clientId '{ownerClientId}'.");
        return;
      }

      foreach (var action in problem.OnCorrect.Scoreboard)
      {
        if (action == null || string.IsNullOrWhiteSpace(action.Objective))
          continue;

        string objective = action.Objective.Trim();
        string criteria = NormalizeCriteria(action.Criteria);
        if (!SessionVariableService.ContainsObjective(objective)
            && !SessionVariableService.AddObjective(objective, criteria, out string addError))
        {
          Debug.LogWarning($"[ProblemRewardService] Failed to create objective '{objective}': {addError}");
          continue;
        }

        int value = action.Value;
        string op = (action.Operation ?? "add").Trim().ToLowerInvariant();
        bool ok = op switch
        {
          "set" => SessionVariableService.SetScore(userIdentifier, objective, value, out _),
          "remove" => SessionVariableService.RemoveScore(userIdentifier, objective, Math.Abs(value), out _),
          _ => SessionVariableService.AddScore(userIdentifier, objective, value, out _)
        };

        if (!ok)
        {
          Debug.LogWarning(
            $"[ProblemRewardService] Failed to apply scoreboard action: objective='{objective}', op='{op}', value='{value}', user='{userIdentifier}'.");
        }
      }
    }

    private static string NormalizeCriteria(string criteria)
    {
      if (string.IsNullOrWhiteSpace(criteria))
        return "dummy";

      string lowered = criteria.Trim().ToLowerInvariant();
      return lowered == "trigger" ? "trigger" : "dummy";
    }
  }
}
