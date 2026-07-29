using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  /// <summary>Dual PatientMonitor의 Graph 출력 자식 오브젝트를 식별하는 컴포넌트입니다.</summary>
  [RequireComponent(typeof(UIDocument))]
  [DisallowMultipleComponent]
  [AddComponentMenu("Triage Trainer/Patient Monitor/Dual Graph Part Controller")]
  public sealed class DualPatientMonitorGraphPartController : PatientMonitorPlane
  {
    public override PatientMonitorPlaneType Type => PatientMonitorPlaneType.Graph;
  }
}
