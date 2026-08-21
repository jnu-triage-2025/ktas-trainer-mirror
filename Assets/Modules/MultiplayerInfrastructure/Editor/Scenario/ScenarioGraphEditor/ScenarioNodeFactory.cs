using System;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Editor
{
  public static class ScenarioNodeFactory
  {
    public static IScenarioNode Create(ScenarioNodeType type) => type switch
    {
      ScenarioNodeType.Dialogue => new ScenarioDialogueNode(),
      ScenarioNodeType.DisinteractableDialogue => new ScenarioDisinteractableDialogueNode(),
      ScenarioNodeType.Choice => new ScenarioChoiceNode
      {
        Options = new System.Collections.Generic.List<ScenarioChoiceOption>()
      },
      ScenarioNodeType.Sound => new ScenarioSoundNode(),
      ScenarioNodeType.PlayerMove => new ScenarioPlayerMoveNode(),
      ScenarioNodeType.NPCMove => new ScenarioNPCMoveNode(),
      ScenarioNodeType.NPCControl => new ScenarioNPCControlNode(),
      ScenarioNodeType.CameraTarget => new ScenarioCameraTargetNode(),
      ScenarioNodeType.Parallel => new ScenarioParallelNode
      {
        Branches = new System.Collections.Generic.List<ScenarioParallelBranch>()
      },
      ScenarioNodeType.InvokeEvent => new ScenarioInvokeEventNode(),
      ScenarioNodeType.ServerInternalSignal => new ScenarioServerInternalSignalNode(),
      ScenarioNodeType.Validator => new ScenarioValidatorNode
      {
        RootConditions = new System.Collections.Generic.List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.RegistryContains,
            ValidationRules = new System.Collections.Generic.List<ScenarioValidatorRule>
            {
              new ScenarioValidatorRule
              {
                Type = ScenarioValidatorRuleType.Registry,
                Condition = ScenarioValidatorRuleCondition.Contains
              }
            }
          }
        }
      },
      ScenarioNodeType.QuestControl => new ScenarioQuestControlNode(),
      ScenarioNodeType.QuestWaypointHighlight => new ScenarioQuestWaypointHighlightNode(),
      ScenarioNodeType.Delay => new ScenarioDelayNode(),
      ScenarioNodeType.Interaction => new ScenarioInteractionNode(),
      ScenarioNodeType.CombineItem => new ScenarioCombineItemNode
      {
        InputItemIdentifiers = new System.Collections.Generic.List<string>()
      },
      ScenarioNodeType.Quiz => new ScenarioQuizNode
      {
        Options = new System.Collections.Generic.List<string>
        {
          "Option 1",
          "Option 2"
        }
      },
      ScenarioNodeType.StateUpdate => new ScenarioStateUpdateNode(),
      ScenarioNodeType.PlayerTag => new ScenarioPlayerTagNode(),
      ScenarioNodeType.PlayTTS => new ScenarioPlayTTSNode(),
      ScenarioNodeType.TriageAssessControl => new ScenarioTriageAssessControlNode(),
      ScenarioNodeType.EntityPresetSpawn => new ScenarioEntityPresetSpawnNode(),
      ScenarioNodeType.EntityTag => new ScenarioEntityTagNode(),
      ScenarioNodeType.EntityInit => new ScenarioEntityInitNode
      {
        StateOperations = new System.Collections.Generic.List<ScenarioEntityStateOperation>()
      },
      ScenarioNodeType.PatientMedicalStatePreset => new ScenarioPatientMedicalStatePresetNode(),
      ScenarioNodeType.ItemSubmissionConfig => new ScenarioItemSubmissionConfigNode
      {
        RequiredItems = new System.Collections.Generic.List<ScenarioItemRequirement>()
      },
      ScenarioNodeType.NpcInteractControl => new ScenarioNpcInteractControlNode(),
      ScenarioNodeType.TimeControl => new ScenarioTimeControlNode(),
      ScenarioNodeType.EntityStateSignalBinding => new ScenarioEntityStateSignalBindingNode(),
      ScenarioNodeType.SignalCounter => new ScenarioSignalCounterNode(),
      ScenarioNodeType.ManualEntrypoint => new ScenarioManualEntrypointNode(),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }
}
