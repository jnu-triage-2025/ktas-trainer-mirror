using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public abstract partial class PatientMonitorController
  {
    [SerializeField, HideInInspector] private bool _interactionColliderAutoManaged;

#if UNITY_EDITOR
    [System.NonSerialized] private bool _interactionColliderCreationScheduled;
#endif

    /// <summary>
    /// NearbyInteractablesDetector는 콜라이더가 붙은 오브젝트에서 IInteractable을
    /// 조회하므로, 모니터의 상호작용 표면은 항상 컨트롤러와 같은 오브젝트에 둡니다.
    /// 화면 자식의 콜라이더만으로는 부모 컨트롤러의 상호작용을 찾을 수 없습니다.
    /// </summary>
    /// <param name="allowCreate">
    /// AddComponent는 내부적으로 SendMessage를 사용하므로 Awake/OnValidate 실행 중에는
    /// 호출할 수 없습니다(Unity가 경고를 냅니다). 해당 구간에서는 false를 넘겨 생성을
    /// 미루고, 이미 존재하는 콜라이더의 크기 갱신만 수행합니다.
    /// </param>
    private void EnsureInteractionCollider(bool allowCreate = true)
    {
      var collider = GetComponent<BoxCollider>();
      if (collider == null)
      {
        if (!allowCreate)
        {
#if UNITY_EDITOR
          // 에디트 모드에서는 OnEnable이 호출되지 않으므로, OnValidate 스택 밖의
          // 안전한 시점으로 생성을 미뤄 둔다.
          ScheduleInteractionColliderCreation();
#endif
          // 플레이 중이라면 Awake 직후의 OnEnable이 같은 작업을 이어서 수행한다.
          return;
        }

        collider = gameObject.AddComponent<BoxCollider>();
        _interactionColliderAutoManaged = true;
      }

      // 오브젝트에 명시적으로 작성된 콜라이더는 보존한다. 이 헬퍼가 만든
      // 콜라이더만 자식 표시 오브젝트 변화에 맞춰 다시 계산된다.
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

#if UNITY_EDITOR
    private void ScheduleInteractionColliderCreation()
    {
      if (_interactionColliderCreationScheduled || Application.isPlaying)
        return;

      _interactionColliderCreationScheduled = true;
      UnityEditor.EditorApplication.delayCall += () =>
      {
        // 프리팹 일괄 순회 중이었다면 지연 호출 시점에 대상이 이미 사라졌을 수 있다.
        if (this == null)
          return;

        _interactionColliderCreationScheduled = false;
        if (Application.isPlaying || GetComponent<BoxCollider>() != null)
          return;

        EnsureInteractionCollider();
        UnityEditor.EditorUtility.SetDirty(gameObject);
      };
    }
#endif

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
