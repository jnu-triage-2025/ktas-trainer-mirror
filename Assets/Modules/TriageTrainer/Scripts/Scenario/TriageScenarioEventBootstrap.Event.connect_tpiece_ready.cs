using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectTPieceReady()
    {
      Register("connect_tpiece_ready", Event_ConnectTPieceReady);
    }

    private IEnumerator Event_ConnectTPieceReady()
    {
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      EmitSystemMessage("환자 A T-piece 연결 연출을 적용했습니다.");
      yield break;
    }
  }
}
