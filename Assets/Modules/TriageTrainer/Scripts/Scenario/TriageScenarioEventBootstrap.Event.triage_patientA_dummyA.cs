using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_TriagePatientAAndDummyA()
    {
      Register("triage_patientA_dummyA", Event_TriagePatientAAndDummyA);
    }

    private IEnumerator Event_TriagePatientAAndDummyA()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientAObject, true);
      SetActiveIfPresent(_dummyAObject, true);
      SetActiveIfPresent(_patientABedObject, true);
      SetActiveIfPresent(_dummyABedObject, true);

      if (_autoAttachPatientsToBeds)
      {
        TryAttachPatientToBed(_patientAObject, _patientABedObject, "patientA");
        TryAttachPatientToBed(_dummyAObject, _dummyABedObject, "dummyA");
      }

      bool shouldMoveBeds = _preferMovingBedsForSpawn && (_patientABedObject != null || _dummyABedObject != null);

      if (_spawnMoveDurationSeconds > 0f)
      {
        if (shouldMoveBeds)
        {
          yield return MoveToIfPresent(_patientABedObject, GetPreferredDestination(_patientABedSpawnPoint, _patientASpawnPoint), _spawnMoveDurationSeconds);
          yield return MoveToIfPresent(_dummyABedObject, GetPreferredDestination(_dummyABedSpawnPoint, _dummyASpawnPoint), _spawnMoveDurationSeconds);
        }
        else
        {
          yield return MoveToIfPresent(_patientAObject, _patientASpawnPoint, _spawnMoveDurationSeconds);
          yield return MoveToIfPresent(_dummyAObject, _dummyASpawnPoint, _spawnMoveDurationSeconds);
        }
      }
      else
      {
        if (shouldMoveBeds)
        {
          SnapToIfPresent(_patientABedObject, GetPreferredDestination(_patientABedSpawnPoint, _patientASpawnPoint));
          SnapToIfPresent(_dummyABedObject, GetPreferredDestination(_dummyABedSpawnPoint, _dummyASpawnPoint));
        }
        else
        {
          SnapToIfPresent(_patientAObject, _patientASpawnPoint);
          SnapToIfPresent(_dummyAObject, _dummyASpawnPoint);
        }
      }

      EmitSystemMessage("환자 A/더미 A 및 이동식 침대가 트리아지 구역에 배치되었습니다.");
      yield break;
    }
  }
}
