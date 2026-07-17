using System;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioRuntimeBootstrapGate
  {
    public static void MarkSceneReady(UnityEngine.SceneManagement.Scene scene)
      => ScenarioRuntimeSceneReadiness.MarkReady(scene);

    public static bool TryValidate(
      ScenarioGraph graph,
      ScenarioRuntimeValidationContext context,
      out ScenarioRuntimeValidationResult result,
      ScenarioRequirementSceneComposition composition = null)
    {
      result = ScenarioRuntimeRequirementsValidator.Validate(
        graph,
        ScenarioRuntimeValidationMode.AbortSessionBootstrap,
        context,
        composition);
      return !result.ShouldAbort;
    }
  }
}
