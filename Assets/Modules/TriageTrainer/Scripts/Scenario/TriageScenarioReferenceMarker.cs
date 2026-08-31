using UnityEngine;

namespace TriageTrainer.Scenario
{
  public enum TriageScenarioReferenceRole
  {
    TriageArrivalPoint,
    PlayerATriagePoint,
    PatientATreatmentRoomPoint,
    PatientBTreatmentRoomPoint,
    PatientCTreatmentRoomPoint,
    PatientBCtRoomPoint,
    PatientCCtRoomPoint,
    PatientAUiPanel,
    PatientDummyDAUiPanel,
    PatientBUiPanel,
    PatientCUiPanel,
    PatientDummyDBUiPanel,
    SuctionChecklistUiPanel,
    IntubationChecklistUiPanel,
    IntravenousChecklistUiPanel,
    CtTransferFadePanel,
  }

  public sealed class TriageScenarioReferenceMarker : MonoBehaviour
  {
    [SerializeField] private TriageScenarioReferenceRole _role;
    public TriageScenarioReferenceRole Role => _role;
  }
}
