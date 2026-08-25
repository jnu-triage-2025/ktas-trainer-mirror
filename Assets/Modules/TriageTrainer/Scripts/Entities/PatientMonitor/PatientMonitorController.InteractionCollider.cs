using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public abstract partial class PatientMonitorController
  {
    [SerializeField, HideInInspector] private bool _interactionColliderAutoManaged;

    /// <summary>
    /// NearbyInteractablesDetector는 콜라이더가 붙은 오브젝트에서 IInteractable을
    /// 조회하므로, 모니터의 상호작용 표면은 항상 컨트롤러와 같은 오브젝트에 둡니다.
    /// 화면 자식의 콜라이더만으로는 부모 컨트롤러의 상호작용을 찾을 수 없습니다.
    /// </summary>
    private void EnsureInteractionCollider()
    {
      var collider = GetComponent<BoxCollider>();
      if (collider == null)
      {
        collider = gameObject.AddComponent<BoxCollider>();
        _interactionColliderAutoManaged = true;
      }

      // Preserve a collider explicitly authored on the object. Only colliders
      // created by this helper are recalculated as child display objects change.
      if (!_interactionColliderAutoManaged)
        return;

      var renderers = GetComponentsInChildren<Renderer>(true);
      if (renderers.Length == 0)
      {
        collider.center = new Vector3(0f, 0f, 0.25f);
        collider.size = new Vector3(2f, 1.5f, 0.5f);
        return;
      }

      var firstPoint = transform.InverseTransformPoint(GetBoundsCorner(renderers[0].bounds, 0));
      var bounds = new Bounds(firstPoint, Vector3.zero);
      for (int i = 0; i < renderers.Length; i++)
      {
        var rendererBounds = renderers[i].bounds;
        for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
          bounds.Encapsulate(transform.InverseTransformPoint(GetBoundsCorner(rendererBounds, cornerIndex)));
      }

      bounds.Expand(new Vector3(0.15f, 0.15f, 0.2f));
      collider.center = bounds.center;
      collider.size = bounds.size;
    }

    private static Vector3 GetBoundsCorner(Bounds bounds, int index)
    {
      var min = bounds.min;
      var max = bounds.max;
      return new Vector3(
        (index & 1) == 0 ? min.x : max.x,
        (index & 2) == 0 ? min.y : max.y,
        (index & 4) == 0 ? min.z : max.z);
    }
  }
}
