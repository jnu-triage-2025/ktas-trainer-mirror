using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_VitalInfoPatientA()
    {
      Register("vitalinfo_1_patientA", Event_VitalInfoPatientA);
    }

    private IEnumerator Event_VitalInfoPatientA()
    {
      ResolveRuntimeReferencesIfNeeded();

      if (_vitalInfoAlsoActivateMonitor)
      {
        SetActiveIfPresent(_patientAVitalMonitorObject, true);
        SetActiveIfPresent(_patientAVitalPanel, true);
        if (_patientAVitalMonitorController != null)
        {
          _patientAVitalMonitorController.enabled = true;
        }
      }

      EmitSystemMessage(_patientAVitalInfoMessage);
      yield break;
    }
  }
}
