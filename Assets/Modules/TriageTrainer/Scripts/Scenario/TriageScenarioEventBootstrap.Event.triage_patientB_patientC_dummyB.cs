using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_TriagePatientBPatientCDummyB()
    {
      Register("triage_patient_b_patient_c_dummy_b", Event_TriagePatientBPatientCDummyB);
    }

    private IEnumerator Event_TriagePatientBPatientCDummyB()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientBObject, true);
      SetActiveIfPresent(_patientCObject, true);
      SetActiveIfPresent(_dummyBObject, true);

      if (_patientBcdSpawnMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(_patientBObject, _patientBSpawnPoint, _patientBcdSpawnMoveDurationSeconds);
        yield return MoveToIfPresent(_patientCObject, _patientCSpawnPoint, _patientBcdSpawnMoveDurationSeconds);
        yield return MoveToIfPresent(_dummyBObject, _dummyBSpawnPoint, _patientBcdSpawnMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(_patientBObject, _patientBSpawnPoint);
        SnapToIfPresent(_patientCObject, _patientCSpawnPoint);
        SnapToIfPresent(_dummyBObject, _dummyBSpawnPoint);
      }

      EmitSystemMessage("환자 B/C 및 더미 B가 트리아지 구역에 도착했습니다.");
      yield break;
    }
  }
}
