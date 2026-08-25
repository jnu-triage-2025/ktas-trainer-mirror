using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectNs1Left()
    {
      Register("connect_ns1_left", Event_ConnectNs1Left);
    }

    private IEnumerator Event_ConnectNs1Left()
    {
      SetActiveIfPresent(_patientANs1LeftConnectedVisual, true);
      EmitSystemMessage("환자 A 좌측 NS 연결 연출을 적용했습니다.");
      yield break;
    }
  }
}
