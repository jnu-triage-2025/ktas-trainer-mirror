using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 이동식 환자 침대가 도착했을 때 정렬될 월드상의 고정 위치와 방향을 정의한다.
  /// </summary>
  public sealed class MovingPatientBedPositioningPoint : MonoBehaviour
  {
    [Header("Snap")]
    [SerializeField, Min(0.01f)] private float _snapDistance = 1f;

    [Header("Occupied Area")]
    [SerializeField] private Vector2 _occupiedSize = new(2.2f, 1f);
    [SerializeField, Min(0f)] private float _displayHeight = 0.03f;

    public float SnapDistance => _snapDistance;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    public bool IsWithinSnapDistance(Vector3 worldPosition)
    {
      Vector3 offset = worldPosition - transform.position;
      offset.y = 0f;
      return offset.sqrMagnitude <= _snapDistance * _snapDistance;
    }

    private void OnValidate()
    {
      _snapDistance = Mathf.Max(0.01f, _snapDistance);
      _occupiedSize.x = Mathf.Max(0.01f, _occupiedSize.x);
      _occupiedSize.y = Mathf.Max(0.01f, _occupiedSize.y);
      _displayHeight = Mathf.Max(0f, _displayHeight);
    }

    private void OnDrawGizmos()
    {
      DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
      DrawGizmo();
    }

    private void DrawGizmo()
    {
      Vector3 center = transform.position + transform.up * _displayHeight;
      Vector3 size = new(_occupiedSize.x, 0.01f, _occupiedSize.y);

      Matrix4x4 previousMatrix = Gizmos.matrix;
      Color previousColor = Gizmos.color;
      Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
      Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.2f);
      Gizmos.DrawCube(Vector3.zero, size);
      Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.95f);
      Gizmos.DrawWireCube(Vector3.zero, size);
      Gizmos.matrix = previousMatrix;

      // 표시용 화살표만 반시계 90도 회전한다. 스냅 방향(transform.rotation)은 유지한다.
      Vector3 arrowForward = Quaternion.AngleAxis(-90f, transform.up) * transform.forward;
      Vector3 arrowRight = Quaternion.AngleAxis(-90f, transform.up) * transform.right;
      Vector3 arrowStart = center - arrowForward * (_occupiedSize.y * 0.25f);
      Vector3 arrowEnd = center + arrowForward * (_occupiedSize.y * 0.45f);
      Gizmos.DrawLine(arrowStart, arrowEnd);
      Gizmos.DrawLine(arrowEnd, arrowEnd - arrowForward * 0.22f + arrowRight * 0.12f);
      Gizmos.DrawLine(arrowEnd, arrowEnd - arrowForward * 0.22f - arrowRight * 0.12f);
      Gizmos.DrawWireSphere(center, 0.06f);
      Gizmos.color = previousColor;

#if UNITY_EDITOR
      Handles.color = new Color(0.05f, 0.45f, 0.65f, 1f);
      Handles.Label(center + Vector3.up * 0.15f, "Patient Bed Positioning Point");
#endif
    }
  }
}
