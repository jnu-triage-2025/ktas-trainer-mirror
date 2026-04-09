using UnityEngine;

namespace TriageTrainer.Patient
{
  public abstract class PatientStateABC : MonoBehaviour
  {
    public abstract PatientTreatmentDisplayStateABC TreatmentDisplayState { get; }
  }
}
