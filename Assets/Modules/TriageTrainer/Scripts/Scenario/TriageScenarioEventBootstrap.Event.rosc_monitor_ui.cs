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

      // 맥박·의식 재사정 상호작용의 노출은 시나리오 데이터의 퀘스트 조건(Quest_Check_Pulse_ROSC, Quest_Check_GCS_ROSC)이 정한다.

      yield return ApplyPatientAMonitorProfile(_patientARoscMonitorParameters, "환자 A ROSC 모니터 프로필을 적용했습니다.");
    }
  }
}
