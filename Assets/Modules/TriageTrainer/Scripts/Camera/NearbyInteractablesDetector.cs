using System;
using System.Collections.Generic;
using TriageTrainer.InteractableEntity;
using TriageTrainer.Registry;
using Unity.VisualScripting;
using UnityEngine;

namespace TriageTrainer.Camera
{
  /// <summary>
  /// PlayerInteractiveDetector는 플레이어가 상호작용할 수 있는 물체를 감지해 사용자의 로컬 UI에 표시하도록 지시합니다.
  ///
  /// 상호작용 가능 물체에 대한 툴팁은 플레이어 위치가 아닌 카메라의 위치를 기준으로 표시되므로,
  /// 이 컴포넌트는 카메라에 추가되도록 의도되었습니다.
  ///
  /// 카메라의 위치를 기준으로 하는 이유는 관전자 모드에서도 상호작용 툴팁을 표시하기 위함입니다.
  /// (카메라 홀더를 기준으로 하면, 관전자 모드 상황에서는 카메라만 다른 카메라 홀더에 붙으므로 적절히 표시되지 않음)
  /// (+ 이와 관련한 개선 구현 방안이 있으나 후순위로 변경: TODO.md 참고)
  /// </summary>
  public class NearbyInteractablesDetector : MonoBehaviour
  {
    [Header("Detection Settings")] [SerializeField, Min(.5f)]
    private float detectionRedius = 6f;

    [SerializeField] private LayerMask interactionLayerMask = ~0;
    [SerializeField, Min(.02f)] private float queryInterval = .05f;

    private readonly List<IInteractable> _nearby = new();
    private readonly List<IInteractable> _scratch = new();
    private readonly Collider[] overlapColliderBuf = new Collider[32];
    private float nextQueryTime;

    public event Action<IReadOnlyList<IInteractable>> NearbyUpdated;
    
    public IReadOnlyList<IInteractable> Nearby => _nearby;

    void Awake()
    {
      CurrentSessionPlayInfoRegistry.Register(this);
    }

    void Update()
    {
      QueryNearbyAndUpdate();
    }

    private void QueryNearbyAndUpdate()
    {
      if (Time.time < nextQueryTime) return;
      nextQueryTime = Time.time + queryInterval;
      
      int count = Physics.OverlapSphereNonAlloc(
        transform.position, 
        detectionRedius,
        overlapColliderBuf,
        interactionLayerMask,
        QueryTriggerInteraction.Collide
      );
      _scratch.Clear();
      for (int i = 0; i < count; i++)
      {
        var collider = overlapColliderBuf[i];
        if (collider.IsUnityNull()) continue;
        if (collider.TryGetComponent(out IInteractable interactable) && !_scratch.Contains(interactable))
          _scratch.Add(interactable);
      }

      bool changed = HasListChanged(_nearby, _scratch);

      _nearby.Clear();
      _nearby.AddRange(_scratch);

      if (changed)
        NearbyUpdated?.Invoke(_nearby);
    }

    private static bool HasListChanged(List<IInteractable> previous, List<IInteractable> next)
    {
      if (previous.Count != next.Count) return true;
      for (int i = 0; i < next.Count; i++)
      {
        if (!previous.Contains(next[i])) return true;
      }

      return false;
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, detectionRedius);
    }
#endif
  }
}
