namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioInteractionNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Interaction;
    public string NextIdentifier { get; set; }

    public ScenarioInteractionActorScope ActorScope { get; set; } = ScenarioInteractionActorScope.Player;
    public string TargetIdentifier { get; set; }
    public string RequiredItemIdentifier { get; set; }
    public ScenarioInteractionType InteractionType { get; set; } = ScenarioInteractionType.Use;
    public string CompletionConditionIdentifier { get; set; }
  }
}
