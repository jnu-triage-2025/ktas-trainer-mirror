using System.Collections;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_TriagePatientAAndPatientDummyDA()
    {
      Register("triage_patient_a_patient_dummy_d_a", Event_TriagePatientAAndPatientDummyDA);
    }

    private IEnumerator Event_TriagePatientAAndPatientDummyDA()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientAObject, true);
      SetActiveIfPresent(_patientDummyDAObject, true);
      SetActiveIfPresent(_patientABedObject, true);
      SetActiveIfPresent(_patientDummyDABedObject, true);

      if (_autoAttachPatientsToBeds)
      {
        TryAttachPatientToBed(_patientAObject, _patientABedObject, "patientA");
        TryAttachPatientToBed(_patientDummyDAObject, _patientDummyDABedObject, "patientDummyDA");
      }

      bool shouldMoveBeds = _preferMovingBedsForSpawn && (_patientABedObject != null || _patientDummyDABedObject != null);

      if (_spawnMoveDurationSeconds > 0f)
      {
        if (shouldMoveBeds)
        {
          yield return MoveToIfPresent(_patientABedObject, GetPreferredDestination(_patientABedSpawnPoint, _patientASpawnPoint), _spawnMoveDurationSeconds);
          yield return MoveToIfPresent(_patientDummyDABedObject, GetPreferredDestination(_patientDummyDABedSpawnPoint, _patientDummyDASpawnPoint), _spawnMoveDurationSeconds);
        }
        else
        {
          yield return MoveToIfPresent(_patientAObject, _patientASpawnPoint, _spawnMoveDurationSeconds);
          yield return MoveToIfPresent(_patientDummyDAObject, _patientDummyDASpawnPoint, _spawnMoveDurationSeconds);
        }
      }
      else
      {
        if (shouldMoveBeds)
        {
          SnapToIfPresent(_patientABedObject, GetPreferredDestination(_patientABedSpawnPoint, _patientASpawnPoint));
          SnapToIfPresent(_patientDummyDABedObject, GetPreferredDestination(_patientDummyDABedSpawnPoint, _patientDummyDASpawnPoint));
        }
        else
        {
          SnapToIfPresent(_patientAObject, _patientASpawnPoint);
          SnapToIfPresent(_patientDummyDAObject, _patientDummyDASpawnPoint);
        }
      }

#if UNITY_EDITOR
      Debug.Log("[EmitSystemMessage] 환자 A/patient_dummy_d_a 및 이동식 침대가 트리아지 구역에 배치되었습니다.");
#endif
      yield break;
    }

    private IEnumerator Event_ResetDisasterIntroTriageAttempt(string patientIdentifier)
    {
      ResolveRuntimeReferencesIfNeeded();
      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Clear(
        $"disaster_intro_triage_submitted_{patientIdentifier}");
      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Clear(
        $"disaster_intro_triage_correct_{patientIdentifier}");

      GameObject target = patientIdentifier == "patient_a"
        ? _patientAObject
        : _patientDummyDAObject;
      target?.GetComponentInChildren<TriageTrainer.Entity.PatientController>(true)
        ?.ResetTriageAssessmentForRetry();
      yield break;
    }
  }
}
