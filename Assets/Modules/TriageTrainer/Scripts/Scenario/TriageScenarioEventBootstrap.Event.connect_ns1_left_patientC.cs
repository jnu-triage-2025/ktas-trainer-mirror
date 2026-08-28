using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectNs1LeftPatientC()
    {
      Register("connect_ns1_left_patient_c", Event_ConnectNs1LeftPatientC);
    }

    private IEnumerator Event_ConnectNs1LeftPatientC()
    {
      SetActiveIfPresent(_patientCNs1LeftConnectedVisual, true);
      yield break;
    }
  }
}
