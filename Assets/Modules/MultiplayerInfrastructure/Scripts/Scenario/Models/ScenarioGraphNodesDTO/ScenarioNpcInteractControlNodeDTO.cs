using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioNpcInteractControlNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("npcIdentifier")]
    public string NpcIdentifier { get; set; }

    [JsonPropertyName("interactableIdentifier")]
    public string InteractableIdentifier { get; set; }

    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("showOverheadName")]
    public bool? ShowOverheadName { get; set; }
  }
}
