using System;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Editor
{
  public static class ScenarioNodeFactory
  {
    public static IScenarioNode Create(ScenarioNodeType type) => type switch
    {
      ScenarioNodeType.Dialogue => new ScenarioDialogueNode(),
      ScenarioNodeType.Choice => new ScenarioChoiceNode
      {
        Options = new System.Collections.Generic.List<ScenarioChoiceOption>()
      },
      ScenarioNodeType.Sound => new ScenarioSoundNode(),
      ScenarioNodeType.PlayerMove => new ScenarioPlayerMoveNode(),
      ScenarioNodeType.NPCMove => new ScenarioNPCMoveNode(),
      ScenarioNodeType.CameraTarget => new ScenarioCameraTargetNode(),
      ScenarioNodeType.Parallel => new ScenarioParallelNode
      {
        Branches = new System.Collections.Generic.List<ScenarioParallelBranch>()
      },
      ScenarioNodeType.InvokeEvent => new ScenarioInvokeEventNode(),
      ScenarioNodeType.Validator => new ScenarioValidatorNode(),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }
}
