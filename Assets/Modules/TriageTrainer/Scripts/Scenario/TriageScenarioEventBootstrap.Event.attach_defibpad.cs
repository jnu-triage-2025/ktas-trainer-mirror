using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_AttachDefibPad()
    {
      Register("attach_defibpad", Event_AttachDefibPad);
    }

    private IEnumerator Event_AttachDefibPad()
    {
      SetActiveIfPresent(_patientADefibPadVisual, true);
      EmitSystemMessage("환자 A 제세동 패드 부착 연출을 적용했습니다.");
      yield break;
    }
  }
}
