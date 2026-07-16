using MultiplayerInfrastructure.Scenario.Requirements;
using TriageTrainer.Entity;

namespace TriageTrainer.Scenario.Requirements
{
  public sealed class TriageScenarioRuntimeCapabilityProvider : IScenarioRuntimeCapabilityProvider
  {
    public string Identifier => "triage-trainer.patient-capabilities";

    public bool TryGetIdentifier(object target, out string identifier)
    {
      identifier = (target as PatientController)?.Identifier;
      return !string.IsNullOrWhiteSpace(identifier);
    }

    public bool Supports(object target, ScenarioRequirementCapability capability)
    {
      var patient = target as PatientController;
      if (patient == null) return false;
      return capability == ScenarioRequirementCapability.PatientMedicalStateTarget
             || capability == ScenarioRequirementCapability.ScenarioEntityInitTarget
             || capability == ScenarioRequirementCapability.ScenarioTriageAssessTarget;
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
      ScenarioRuntimeCapabilityProviderRegistry.Register(new TriageScenarioRuntimeCapabilityProvider(), out _);
    }
  }
}
