using UnityEngine;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Patient
{
  public abstract class PatientStateABC : MonoBehaviour
  {
    [SerializeField]
    private PatientDescriptor patientDescriptor = new PatientDescriptor();

    public PatientDescriptor Descriptor => patientDescriptor;

    public abstract PatientTreatmentDisplayStateABC TreatmentDisplayState { get; }
  }
}
