using System;
using System.Linq;

namespace MultiplayerInfrastructure.Problem
{
  public static class ProblemAnswerEvaluator
  {
    public static bool EvaluateChoice(ProblemDefinition.ChoiceDefinition definition, int selectedIndex)
    {
      if (definition == null)
        return false;

      return selectedIndex >= 0 && selectedIndex == definition.CorrectIndex;
    }

    public static bool EvaluateShortAnswer(ProblemDefinition.ShortAnswerDefinition definition, string input)
    {
      if (definition == null)
        return false;

      input ??= string.Empty;
      definition.Conditions ??= new System.Collections.Generic.List<ProblemDefinition.ConditionDefinition>();

      if (definition.Conditions.Count == 0)
        return false;

      var conditionResults = definition.Conditions.Select(each => EvaluateCondition(each, input));
      return string.Equals(definition.Operator, "or", StringComparison.OrdinalIgnoreCase)
        ? conditionResults.Any(each => each)
        : conditionResults.All(each => each);
    }

    private static bool EvaluateCondition(ProblemDefinition.ConditionDefinition condition, string input)
    {
      if (condition == null || string.IsNullOrWhiteSpace(condition.Type))
        return false;

      var value = condition.Value ?? string.Empty;
      var comparison = condition.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

      return condition.Type.Trim().ToLowerInvariant() switch
      {
        "exact" => string.Equals(input, value, comparison),
        "contains" => input.IndexOf(value, comparison) >= 0,
        "not_contains" => input.IndexOf(value, comparison) < 0,
        _ => false
      };
    }
  }
}
