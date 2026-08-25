using UnityEngine;

#if UNITY_EDITOR
#endif

namespace TriageTrainer.Entity
{
  public class MovingPatientBedPatientAttachPointObject : MonoBehaviour
  {
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      DrawSceneViewPreview();
    }

    private void OnDrawGizmosSelected()
    {
      DrawSceneViewPreview();
    }

    private void DrawSceneViewPreview()
    {
      Gizmos.color = new Color(0.85f, 1f, 0.2f, 0.9f);
      Gizmos.DrawSphere(transform.position, 0.04f);
    }
#endif
  }
}
