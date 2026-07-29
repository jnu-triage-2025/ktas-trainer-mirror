using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  /// <summary>
  /// 기존 PatientMonitor 구현의 단일 출력 컨트롤러입니다.
  /// 환자 상태, 네트워크 동기화, 파형 계산과 단일 UIDocument 그래픽을 보존합니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  [AddComponentMenu("Triage Trainer/Patient Monitor/Single Patient Monitor Controller")]
  public class SinglePatientMonitorController : PatientMonitorController
  {
  }
}
