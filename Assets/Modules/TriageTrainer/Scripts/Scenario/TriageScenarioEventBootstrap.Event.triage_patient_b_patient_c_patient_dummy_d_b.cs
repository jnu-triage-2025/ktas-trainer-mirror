using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_TriagePatientBPatientCPatientDummyDB()
    {
      Register("triage_patient_b_patient_c_patient_dummy_d_b", Event_TriagePatientBPatientCPatientDummyDB);
    }

    private IEnumerator Event_TriagePatientBPatientCPatientDummyDB()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientBObject, true);
      SetActiveIfPresent(_patientCObject, true);
      SetActiveIfPresent(_patientDummyDBObject, true);

      if (_patientBcdSpawnMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(_patientBObject, _patientBSpawnPoint, _patientBcdSpawnMoveDurationSeconds);
        yield return MoveToIfPresent(_patientCObject, _patientCSpawnPoint, _patientBcdSpawnMoveDurationSeconds);
        yield return MoveToIfPresent(_patientDummyDBObject, _patientDummyDBSpawnPoint, _patientBcdSpawnMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(_patientBObject, _patientBSpawnPoint);
        SnapToIfPresent(_patientCObject, _patientCSpawnPoint);
        SnapToIfPresent(_patientDummyDBObject, _patientDummyDBSpawnPoint);
      }

#if UNITY_EDITOR
      Debug.Log("[EmitSystemMessage] 환자 B/C 및 patient_dummy_d_b가 트리아지 구역에 도착했습니다.");
#endif
      yield break;
    }
  }
}
