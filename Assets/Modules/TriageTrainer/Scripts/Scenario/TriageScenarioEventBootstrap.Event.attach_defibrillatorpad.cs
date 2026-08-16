using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_AttachDefibrillatorPad()
    {
      Register("attach_defibrillatorpad", Event_AttachDefibrillatorPad);
    }

    private IEnumerator Event_AttachDefibrillatorPad()
    {
      SetActiveIfPresent(_patientADefibrillatorPadVisual, true);
      var patient = ResolvePatientAController();
      patient?.SetNamedChildActive("defibrillatorpad_midaxillary_A", true);
      patient?.SetNamedChildActive("defibrillatorpad_subclavicle_A", true);
      EmitSystemMessage("환자 A 제세동 패드 부착 연출을 적용했습니다.");
      yield break;
    }
  }
}
