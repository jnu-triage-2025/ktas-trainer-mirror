#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Entity.IntravenousLine
{
  public class IntravenousLineConnectionPoint: MonoBehaviour
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
      Gizmos.color = new Color(0.35f, 0.7f, 0.5f, 0.9f);
      Gizmos.DrawSphere(transform.position, 0.04f);
    }
#endif
  }
}
