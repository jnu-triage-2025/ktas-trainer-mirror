using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_StopAmbuAndComp()
    {
      Register("stop_ambu_and_comp", Event_StopAmbuAndComp);
    }

    private IEnumerator Event_StopAmbuAndComp()
    {
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
      CompletePatientACprCycle();
      EmitSystemMessage("앰부배깅/가슴압박 애니메이션을 중지했습니다.");
      yield break;
    }
  }
}
