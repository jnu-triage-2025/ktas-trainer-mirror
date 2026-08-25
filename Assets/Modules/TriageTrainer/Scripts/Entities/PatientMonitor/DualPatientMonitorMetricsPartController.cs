using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  /// <summary>Dual PatientMonitor의 Metrics 출력 자식 오브젝트를 식별하는 컴포넌트입니다.</summary>
  [RequireComponent(typeof(UIDocument))]
  [DisallowMultipleComponent]
  [AddComponentMenu("Triage Trainer/Patient Monitor/Dual Metrics Part Controller")]
  public sealed class DualPatientMonitorMetricsPartController : PatientMonitorPlane
  {
    public override PatientMonitorPlaneType Type => PatientMonitorPlaneType.Metrics;
  }
}
