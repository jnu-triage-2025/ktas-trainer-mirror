using UnityEngine;

namespace TriageTrainer.Patient
{
  public class PatientTypeAState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeATreatmentDisplayState treatmentDisplayState = new PatientTypeATreatmentDisplayState();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;
  }
}
