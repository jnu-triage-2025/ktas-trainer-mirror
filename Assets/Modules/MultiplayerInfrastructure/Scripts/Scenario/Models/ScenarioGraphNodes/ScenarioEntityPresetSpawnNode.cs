namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioEntityPresetSpawnNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.EntityPresetSpawn;
    public string NextIdentifier { get; set; }

    public string PresetIdentifier { get; set; }

    public string PositionSourceEntityIdentifier { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    public string ResultStateKey { get; set; }
  }
}
