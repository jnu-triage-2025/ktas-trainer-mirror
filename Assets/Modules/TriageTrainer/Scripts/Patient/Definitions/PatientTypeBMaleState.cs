using UnityEngine;

namespace TriageTrainer.Patient
{
  public class PatientTypeBMaleState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeBMaleTreatmentDisplayState treatmentDisplayState = new PatientTypeBMaleTreatmentDisplayState();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;
  }
}
