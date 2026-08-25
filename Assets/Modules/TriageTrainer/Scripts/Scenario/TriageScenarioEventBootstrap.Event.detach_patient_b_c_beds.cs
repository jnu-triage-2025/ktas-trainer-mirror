using System.Collections;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const float DetachBedResolutionTimeoutSeconds = 5f;

    private void RegisterEvent_DetachPatientBCBeds()
    {
      Register("detach_patient_b_c_beds", Event_DetachPatientBCBeds);
    }

    private IEnumerator Event_DetachPatientBCBeds()
    {
      float startedAt = Time.realtimeSinceStartup;

      while (Time.realtimeSinceStartup - startedAt < DetachBedResolutionTimeoutSeconds)
      {
        ResolveRuntimeReferencesIfNeeded();
        bool patientBDetached = DetachBedParticipants(_patientBTreatmentBedObject);
        bool patientCDetached = DetachBedParticipants(_patientCTreatmentBedObject);
        if (patientBDetached && patientCDetached)
          yield break;

        yield return null;
      }

      Debug.LogError(
        "[TriageScenarioEventBootstrap] detach_patient_b_c_beds timed out because one or more treatment beds could not be resolved. " +
        "The scenario will continue to avoid a permanent graph stall.",
        this);
    }

    private static bool DetachBedParticipants(GameObject bedObject)
    {
      var bed = bedObject != null
        ? bedObject.GetComponentInChildren<MovingPatientBedController>(true)
        : null;
      if (bed == null)
        return false;

      bed.DetachAllParticipants();
      return true;
    }
  }
}
