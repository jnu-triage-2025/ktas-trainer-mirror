using UnityEngine;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Patient
{
  public abstract class PatientStateABC : MonoBehaviour
  {
    [SerializeField]
    private PatientDescriptor patientDescriptor = new PatientDescriptor();

    [Header("Moving Bed Laying Offsets")]
    [SerializeField] private Vector3 positionOnLayingOnPatientMovingBed = new Vector3(0f, -0.95f, -0.2f);
    [SerializeField] private Vector3 eulerAnglesOnLayingOnPatientMovingBed = Vector3.zero;

    [Header("Moving Bed Laying Collider")]
    [SerializeField] private Vector3 colliderCenterOnLayingOnPatientMovingBed = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(0.01f)] private float colliderHeightOnLayingOnPatientMovingBed = 1.8f;
    [SerializeField, Min(0.01f)] private float colliderRadiusOnLayingOnPatientMovingBed = 0.3f;
    [SerializeField, Range(0, 2)] private int colliderDirectionOnLayingOnPatientMovingBed = 2;

    public PatientDescriptor Descriptor => patientDescriptor;
    public virtual Vector3 PositionOnLayingOnPatientMovingBed => positionOnLayingOnPatientMovingBed;
    public virtual Vector3 EulerAnglesOnLayingOnPatientMovingBed => eulerAnglesOnLayingOnPatientMovingBed;
    public virtual Vector3 ColliderCenterOnLayingOnPatientMovingBed => colliderCenterOnLayingOnPatientMovingBed;
    public virtual float ColliderHeightOnLayingOnPatientMovingBed => Mathf.Max(0.01f, colliderHeightOnLayingOnPatientMovingBed);
    public virtual float ColliderRadiusOnLayingOnPatientMovingBed => Mathf.Max(0.01f, colliderRadiusOnLayingOnPatientMovingBed);
    public virtual int ColliderDirectionOnLayingOnPatientMovingBed => Mathf.Clamp(colliderDirectionOnLayingOnPatientMovingBed, 0, 2);

    public abstract PatientTreatmentDisplayStateABC TreatmentDisplayState { get; }
  }
}
