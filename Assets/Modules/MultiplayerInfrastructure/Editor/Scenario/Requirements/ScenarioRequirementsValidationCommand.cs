#if UNITY_EDITOR
using System;
using MultiplayerInfrastructure.Scenario.Editor;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  /// <summary>
  /// Single authoring/CI entry point for the complete Scenario Requirements
  /// validation suite. Individual validators remain callable from batch code,
  /// but are intentionally not exposed as competing editor menu commands.
  /// </summary>
  public static class ScenarioRequirementsValidationCommand
  {
    [MenuItem("Tools/Multiplayer Infrastructure/Validate Scenario Requirements")]
    public static void RunAll()
    {
      try
      {
        ScenarioRequirementsPhaseValidation.Run();
        ScenarioRequirementsRuntimeRegistrationValidation.Run();
        var buildReport = ScenarioRequirementsBuildValidator.ValidateFromEditor();
        if (buildReport.HasBlockingErrors)
          throw new InvalidOperationException(buildReport.ToStableSummary());
        Debug.Log("[ScenarioRequirements] Complete validation passed.");
      }
      catch (Exception ex)
      {
        Debug.LogError("[ScenarioRequirements] Complete validation failed: " + ex.Message);
        throw;
      }
    }
  }
}
#endif
