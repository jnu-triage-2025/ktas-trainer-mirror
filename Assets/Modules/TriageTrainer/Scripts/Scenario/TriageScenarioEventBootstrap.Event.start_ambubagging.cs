using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_StartAmbuBagging()
    {
      Register("start_ambubagging", Event_StartAmbuBagging);
    }

    private IEnumerator Event_StartAmbuBagging()
    {
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, true);
      EmitSystemMessage("앰부배깅 애니메이션을 시작했습니다.");
      yield break;
    }
  }
}
