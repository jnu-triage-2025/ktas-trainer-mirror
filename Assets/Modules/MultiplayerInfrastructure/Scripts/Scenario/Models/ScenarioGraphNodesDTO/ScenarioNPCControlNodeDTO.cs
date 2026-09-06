using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioNPCControlNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("mode")] public string Mode { get; set; }
    [JsonPropertyName("npcIdentifier")] public string NPCIdentifier { get; set; }

    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("showOverheadName")] public bool? ShowOverheadName { get; set; }

    [JsonPropertyName("destinationType")] public string DestinationType { get; set; }
    [JsonPropertyName("destinationIdentifier")] public string DestinationIdentifier { get; set; }
    [JsonPropertyName("destinationX")] public float? DestinationX { get; set; }
    [JsonPropertyName("destinationY")] public float? DestinationY { get; set; }
    [JsonPropertyName("destinationZ")] public float? DestinationZ { get; set; }
    [JsonPropertyName("ignoreGroundCheck")] public bool? IgnoreGroundCheck { get; set; }
    [JsonPropertyName("moveMode")] public string MoveMode { get; set; }
    [JsonPropertyName("moveSpeed")] public float? MoveSpeed { get; set; }
    [JsonPropertyName("moveDuration")] public float? MoveDuration { get; set; }

    [JsonPropertyName("facingYawDegrees")] public float? FacingYawDegrees { get; set; }
  }
}
