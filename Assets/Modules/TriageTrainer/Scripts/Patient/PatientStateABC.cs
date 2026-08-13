using UnityEngine;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Entity;
using TriageTrainer.Entity.OxyLine;

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

    protected virtual void Awake()
    {
      InitializeRuntimeReferences(GetComponent<PatientController>());
    }

    public void InitializeRuntimeReferences(PatientController controller)
    {
      if (controller != null)
        ConfigureRuntimeReferences(controller);
    }

    protected virtual void ConfigureRuntimeReferences(PatientController controller) { }

    /// <summary>
    /// B/C의 비강 캐뉼라는 이미 <see cref="OxyLineConnectionPoint"/>로 식별된다.
    /// 별도 마커나 프리팹 fileID에 의존하지 않고 현재 환자 오브젝트에서 찾는다.
    /// 다만 둘 이상이면 임의의 첫 포트를 사용하지 않는다.
    /// </summary>
    protected OxyLineConnectionPoint FindSingleOxygenInterface()
    {
      var points = GetComponentsInChildren<OxyLineConnectionPoint>(true);
      if (points.Length == 1)
        return points[0];

      Debug.LogError(
        $"[{GetType().Name}] 환자 산소 포트는 OxyLineConnectionPoint 하나여야 하지만 {points.Length}개입니다.",
        this);
      return null;
    }
  }
}
