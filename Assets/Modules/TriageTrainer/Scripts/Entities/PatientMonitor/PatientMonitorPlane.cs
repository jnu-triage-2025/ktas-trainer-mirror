using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  public enum PatientMonitorPlaneType
  {
    Graph,
    Metrics
  }

  /// <summary>2-Plane 모드에서 자식 표시 평면의 역할을 식별하는 마커입니다.</summary>
  [RequireComponent(typeof(UIDocument))]
  public sealed class PatientMonitorPlane : MonoBehaviour
  {
    [SerializeField] private PatientMonitorPlaneType _type;
    [SerializeField] private Vector2Int _lowResolution = new Vector2Int(960, 720);

    public PatientMonitorPlaneType Type => _type;
    public Vector2Int LowResolution => new Vector2Int(
      Mathf.Max(320, _lowResolution.x), Mathf.Max(240, _lowResolution.y));
    public UIDocument Document => GetComponent<UIDocument>();
  }
}
