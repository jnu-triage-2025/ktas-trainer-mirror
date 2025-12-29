using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Quest;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioQuestControlNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("failureStrategy")]
    public string FailureStrategy { get; set; }

    [JsonPropertyName("quest")]
    public QuestData Quest { get; set; }
  }
}
