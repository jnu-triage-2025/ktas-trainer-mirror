using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_AsystoleMonitorUi()
    {
      Register("asystole_monitor_ui", Event_AsystoleMonitorUi);
    }

    private IEnumerator Event_AsystoleMonitorUi()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
      {
        patient.SetResuscitationMedicationRound(2);
        var state = patient.MedicalState;
        patient.SetMonitorMedicalState(
          _patientAAsystoleMonitorParameters,
          state.art,
          state.cvp,
          state.pleth,
          state.numerics,
          state.nibp,
          state.temperature,
          state.stLeads);
      }

      yield return ApplyPatientAMonitorProfile(_patientAAsystoleMonitorParameters, "환자 A 무수축(Asystole) 모니터 프로필을 적용했습니다.");
    }
  }
}
