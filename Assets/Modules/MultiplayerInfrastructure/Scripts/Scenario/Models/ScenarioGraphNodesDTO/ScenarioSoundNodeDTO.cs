using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioSoundNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("soundResourceIdentifier")]
    public string SoundResourceIdentifier { get; set; }

    [JsonPropertyName("waitUntilFinished")]
    public bool? WaitUntilFinished { get; set; }
  }
}
