using System;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Editor
{
  public static class ScenarioNodeFactory
  {
    public static IScenarioNode Create(ScenarioNodeType type) => type switch
    {
      ScenarioNodeType.Dialogue => new ScenarioDialogueNode(),
      ScenarioNodeType.Choice => new ScenarioChoiceNode(),
      ScenarioNodeType.Sound => new ScenarioSoundNode(),
      ScenarioNodeType.PlayerMove => new ScenarioPlayerMoveNode(),
      ScenarioNodeType.CameraTarget => new ScenarioCameraTargetNode(),
      ScenarioNodeType.Parallel => new ScenarioParallelNode(),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }
}
