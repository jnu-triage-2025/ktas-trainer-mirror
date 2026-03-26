using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectNs1RightPatientB()
    {
      Register("connect_ns1_right_patientB", Event_ConnectNs1RightPatientB);
    }

    private IEnumerator Event_ConnectNs1RightPatientB()
    {
      SetActiveIfPresent(_patientBNs1RightConnectedVisual, true);
      EmitSystemMessage("환자 B 우측 NS 연결 연출을 적용했습니다.");
      yield break;
    }
  }
}
