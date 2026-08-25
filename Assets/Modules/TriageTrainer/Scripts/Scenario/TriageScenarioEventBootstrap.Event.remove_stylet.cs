using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_RemoveStylet()
    {
      Register("remove_stylet", Event_RemoveStylet);
    }

    private IEnumerator Event_RemoveStylet()
    {
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, false);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, true);
      var patient = ResolvePatientAController();
      patient?.SetTreatmentApplied(
        "endotracheal_tube_stylet_inserted",
        false,
        TriageTrainer.Entity.PatientController.TreatmentDisplay.EndotrachealTubeStyletInserted);
      patient?.SetTreatmentApplied(
        "endotracheal_tube_insert_done",
        true,
        TriageTrainer.Entity.PatientController.TreatmentDisplay.EndotrachealTubeInsertDone);
      EmitSystemMessage("환자 A 기관내관 스타일렛 제거 연출을 적용했습니다.");
      yield break;
    }
  }
}
