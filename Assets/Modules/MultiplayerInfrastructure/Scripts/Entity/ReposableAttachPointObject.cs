using UnityEngine;

#if UNITY_EDITOR
#endif

namespace MultiplayerInfrastructure.Entity
{
  public class ReposableAttachPointObject : MonoBehaviour
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
      Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.95f);
      Gizmos.DrawSphere(transform.position, 0.05f);
      Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.18f);
    }
#endif
  }
}
