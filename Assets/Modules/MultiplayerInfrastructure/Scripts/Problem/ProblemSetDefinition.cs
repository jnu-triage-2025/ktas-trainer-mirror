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
    public string FigureIdentifier { get; set; }

    [JsonPropertyName("choice")]
    public ProblemChoiceDefinition Choice { get; set; }

    [JsonPropertyName("shortAnswer")]
    public ProblemShortAnswerDefinition ShortAnswer { get; set; }
  }

  public sealed class ProblemChoiceDefinition
  {
    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();

    [JsonPropertyName("correctIndex")]
    public int CorrectIndex { get; set; } = -1;
  }

  public sealed class ProblemShortAnswerDefinition
  {
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "and";

    [JsonPropertyName("conditions")]
    public List<ShortAnswerConditionDefinition> Conditions { get; set; } = new();
  }

  public sealed class ShortAnswerConditionDefinition
  {
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("ignoreCase")]
    public bool IgnoreCase { get; set; } = true;
  }
}
