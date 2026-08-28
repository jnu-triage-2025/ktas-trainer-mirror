using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_Insert20gRightPatientB()
    {
      Register("insert_20g_right_patient_b", Event_Insert20gRightPatientB);
    }

    private IEnumerator Event_Insert20gRightPatientB()
    {
      SetActiveIfPresent(_patientB20gRightVisual, true);
      yield break;
    }
  }
}
