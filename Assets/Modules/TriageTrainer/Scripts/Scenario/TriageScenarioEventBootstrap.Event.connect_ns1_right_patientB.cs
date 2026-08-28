using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectNs1RightPatientB()
    {
      Register("connect_ns1_right_patient_b", Event_ConnectNs1RightPatientB);
    }

    private IEnumerator Event_ConnectNs1RightPatientB()
    {
      SetActiveIfPresent(_patientBNs1RightConnectedVisual, true);
      yield break;
    }
  }
}
