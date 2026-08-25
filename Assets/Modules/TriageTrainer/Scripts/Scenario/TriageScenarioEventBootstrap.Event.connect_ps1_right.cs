using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectPs1Right()
    {
      Register("connect_ps1_right", Event_ConnectPs1Right);
    }

    private IEnumerator Event_ConnectPs1Right()
    {
      SetActiveIfPresent(_patientAPs1RightConnectedVisual, true);
      EmitSystemMessage("환자 A 우측 플라즈마 연결 연출을 적용했습니다.");
      yield break;
    }
  }
}
