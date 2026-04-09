using UnityEngine;

namespace TriageTrainer.Patient
{
  public class PatientTypeBFemaleState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeBFemaleTreatmentDisplayState treatmentDisplayState = new PatientTypeBFemaleTreatmentDisplayState();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;
  }
}
