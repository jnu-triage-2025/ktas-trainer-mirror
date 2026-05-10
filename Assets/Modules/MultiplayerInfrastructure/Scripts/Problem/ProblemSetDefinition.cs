using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Problem
{
  public sealed class ProblemSetDefinition
  {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("problems")]
    public List<ProblemDefinition> Problems { get; set; } = new();
  }

  public sealed class ProblemDefinition
  {
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }

    [JsonPropertyName("figure")]
    public string Figure { get; set; }

    [JsonPropertyName("choice")]
    public ChoiceDefinition Choice { get; set; }

    [JsonPropertyName("shortAnswer")]
    public ShortAnswerDefinition ShortAnswer { get; set; }

    [JsonPropertyName("onCorrect")]
    public OnCorrectDefinition OnCorrect { get; set; }

    [JsonPropertyName("grading")]
    public GradingDefinition Grading { get; set; }

    public sealed class ChoiceDefinition
    {
      [JsonPropertyName("options")]
      public List<string> Options { get; set; } = new();

      [JsonPropertyName("correctIndex")]
      public int CorrectIndex { get; set; } = -1;
    }

    public sealed class ShortAnswerDefinition
    {
      [JsonPropertyName("operator")]
      public string Operator { get; set; } = "and";

      [JsonPropertyName("conditions")]
      public List<ConditionDefinition> Conditions { get; set; } = new();
    }

    public sealed class ConditionDefinition
    {
      [JsonPropertyName("type")]
      public string Type { get; set; }

      [JsonPropertyName("value")]
      public string Value { get; set; }

      [JsonPropertyName("ignoreCase")]
      public bool IgnoreCase { get; set; } = true;
    }

    public sealed class OnCorrectDefinition
    {
      [JsonPropertyName("scoreboard")]
      public List<ScoreboardAdjustmentDefinition> Scoreboard { get; set; } = new();
    }

    public sealed class ScoreboardAdjustmentDefinition
    {
      [JsonPropertyName("objective")]
      public string Objective { get; set; }

      [JsonPropertyName("criteria")]
      public string Criteria { get; set; } = "dummy";

      [JsonPropertyName("operation")]
      public string Operation { get; set; } = "add";

      [JsonPropertyName("value")]
      public int Value { get; set; } = 1;
    }

    public sealed class GradingDefinition
    {
      [JsonPropertyName("retryOnWrong")]
      public bool RetryOnWrong { get; set; } = true;
    }
  }
}
