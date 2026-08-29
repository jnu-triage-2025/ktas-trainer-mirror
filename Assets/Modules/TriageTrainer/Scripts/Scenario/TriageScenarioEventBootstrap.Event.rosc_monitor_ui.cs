using System.Collections;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Quest;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_RoscMonitorUi()
    {
      Register("rosc_monitor_ui", Event_RoscMonitorUi);
    }

    private IEnumerator Event_RoscMonitorUi()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
      {
        patient.MedicalStateIsCardiacArrest = false;
        patient.SetMonitorMedicalState(
          _patientARoscMonitorParameters,
          new ARTParameters { bpm = 110f, systolic = 90f, diastolic = 55f, noise = 0f },
          new CVPParameters { bpm = 110f, mean = 8f, noise = 0f },
          new PlethParameters { bpm = 110f, spo2 = 94f, noise = 0f },
          new NumericsParameters { bpm = 110f, pvcs = 0f, pulseRate = 110f, perfusionIndex = 1.2f, spo2 = 94f },
          new NIBPParameters { systolic = 90f, diastolic = 55f },
          new TemperatureParameters { t1 = 35.9f, t2 = 35.9f },
          new STLeadValues());
        GameLogService.WriteScenario("Patient A medical state changed: ROSC.", "patient_a_critical");
      }

      // 첫 심정지 맥박 확인 메뉴를 제거하고 ROSC 확인용 메뉴만 노출한다.
      // 두 메뉴가 동시에 나타나는 것과 r1 신호가 V031을 통과하지 못하는 것을 함께 방지한다.
      // 메인 흐름 이벤트이므로 전원의 플래그를 갱신한다.
      PlayerQuestStateFlagService.SetForAll(
        PatientACriticalQuestStateFlags.ArrestPulseAssess, value: false);
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.RoscPulseAssess);
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.RoscGcsAssess);

      yield return ApplyPatientAMonitorProfile(_patientARoscMonitorParameters, "환자 A ROSC 모니터 프로필을 적용했습니다.");
    }
  }
}
