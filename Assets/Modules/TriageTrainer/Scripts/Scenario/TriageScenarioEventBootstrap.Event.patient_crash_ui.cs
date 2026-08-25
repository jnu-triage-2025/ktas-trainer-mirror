using System.Collections;
using MultiplayerInfrastructure.Logging;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_PatientCrashUi()
    {
      Register("patient_crash_ui", Event_PatientCrashUi);
    }

    private IEnumerator Event_PatientCrashUi()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
      {
        float unavailable = PatientMedicalState.MonitorValueUnavailable;
        patient.MedicalStateIsCardiacArrest = true;
        patient.SetMonitorMedicalState(
          _patientACrashMonitorParameters,
          new ARTParameters { bpm = unavailable, systolic = unavailable, diastolic = unavailable, noise = 0f },
          new CVPParameters { bpm = unavailable, mean = unavailable, noise = 0f },
          new PlethParameters { bpm = unavailable, spo2 = unavailable, noise = 0f },
          new NumericsParameters
          {
            bpm = _patientACrashMonitorParameters.bpm,
            pvcs = unavailable,
            pulseRate = unavailable,
            perfusionIndex = unavailable,
            spo2 = unavailable
          },
          new NIBPParameters { systolic = unavailable, diastolic = unavailable },
          new TemperatureParameters { t1 = unavailable, t2 = unavailable },
          new STLeadValues
          {
            i = unavailable,
            ii = unavailable,
            iii = unavailable,
            avr = unavailable,
            avl = unavailable,
            avf = unavailable,
            v1 = unavailable,
            v2 = unavailable,
            v3 = unavailable,
            v4 = unavailable,
            v5 = unavailable,
            v6 = unavailable
          });
        GameLogService.WriteScenario(
          $"Patient A monitor state changed: PEA, ECG HR={_patientACrashMonitorParameters.bpm:0}, non-ECG channels=unavailable.",
          "patient_a_critical");
      }

      yield return ApplyPatientAMonitorProfile(_patientACrashMonitorParameters, "환자 A 상태 악화 모니터 프로필을 적용했습니다.");
    }
  }
}
