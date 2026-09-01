using System.Collections;
using MultiplayerInfrastructure.Logging;
using TriageTrainer.Entity.Patient;

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

        // 무수축은 ECG 파형만 평탄해지는 상태가 아니다. 이전 단계(patient_crash_ui)가 기록해 둔
        // 숫자 채널을 그대로 되돌려 넣으면 파형은 평탄한데 큰 숫자 심박수는 직전 값(80)으로
        // 남아, 훈련생이 무수축 환자를 "HR 80"으로 판독하게 된다. 따라서 리듬 전이와 함께
        // 숫자·NIBP·pleth·ART·CVP 채널을 모두 측정 불가로 확정한다.
        float unavailable = PatientMedicalState.MonitorValueUnavailable;
        patient.MedicalStateIsCardiacArrest = true;
        patient.SetMonitorMedicalState(
          _patientAAsystoleMonitorParameters,
          new ARTParameters { bpm = unavailable, systolic = unavailable, diastolic = unavailable, noise = 0f },
          new CVPParameters { bpm = unavailable, mean = unavailable, noise = 0f },
          new PlethParameters { bpm = unavailable, spo2 = unavailable, noise = 0f },
          new NumericsParameters
          {
            // 무수축의 심박수는 0 이며, 0 은 측정 불가가 아닌 유효한 임상 값이다.
            bpm = _patientAAsystoleMonitorParameters.bpm,
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
          $"Patient A monitor state changed: Asystole, ECG HR={_patientAAsystoleMonitorParameters.bpm:0}, non-ECG channels=unavailable.",
          "patient_a_critical");
      }

      yield return ApplyPatientAMonitorProfile(_patientAAsystoleMonitorParameters, "환자 A 무수축(Asystole) 모니터 프로필을 적용했습니다.");
    }
  }
}
