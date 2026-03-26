using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectNs1LeftPatientC()
    {
      Register("connect_ns1_left_patientC", Event_ConnectNs1LeftPatientC);
    }

    private IEnumerator Event_ConnectNs1LeftPatientC()
    {
      SetActiveIfPresent(_patientCNs1LeftConnectedVisual, true);
      EmitSystemMessage("환자 C 좌측 NS 연결 연출을 적용했습니다.");
      yield break;
    }
  }
}
