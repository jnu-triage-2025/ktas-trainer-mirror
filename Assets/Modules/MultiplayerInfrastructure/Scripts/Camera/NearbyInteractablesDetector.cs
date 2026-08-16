using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Camera
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
  public class NearbyInteractablesDetector : NetworkBehaviour
  {
    [Header("Detection Settings")] [SerializeField, Min(.5f)]
    private float detectionRedius = 1.3f;

    [SerializeField] private Vector3 detectionOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private LayerMask interactionLayerMask = ~0;
    [SerializeField, Min(.02f)] private float queryInterval = .05f;

    [SerializeField] private List<IInteractable> _nearby = new List<IInteractable>();
    [SerializeField] private List<IInteractable> _scratch = new List<IInteractable>();
    private readonly Collider[] overlapColliderBuf = new Collider[32];
    private readonly Dictionary<string, IInteract> _nearestOnly = new Dictionary<string, IInteract>();
    private readonly Dictionary<string, IInteract> _nearestOnlyScratch = new Dictionary<string, IInteract>();
    private readonly Dictionary<string, float> _nearestOnlyDistanceScratch = new Dictionary<string, float>();
    private float nextQueryTime;

    public event Action<IReadOnlyList<IInteractable>> NearbyUpdated;

    // must be allocated from outside to set position
    [SerializeField] Transform detectBased;
    
    public IReadOnlyList<IInteractable> Nearby => _nearby;
    public bool InteractableNearbyExists => _nearby.Count > 0;

    void Update()
    {
      QueryNearbyAndUpdate();
    }

    // must be allocated from outside to set position
    public void RegisterDetectBased(Transform _transform)
    {
      detectBased = _transform;
    }

    private void QueryNearbyAndUpdate()
    {
      if (detectBased.IsUnityNull()) return;
      if (Time.time < nextQueryTime) return;
      nextQueryTime = Time.time + queryInterval;

      Vector3 detectionPosition = detectBased.position + detectionOffset;
      int count = Physics.OverlapSphereNonAlloc(
        detectionPosition,
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

      // 감지 집합이 그대로인 채 구역 경계를 이동해도 같은 종류의 최단 후보는 바뀔 수 있다.
      // 매 query마다 전체 UI를 다시 그리면 선택 인덱스가 흔들리므로, 실제 최단 후보가 바뀐 경우에만 알린다.
      bool nearestOnlyChanged = UpdateNearestOnlySelections(_nearby, detectBased);
      if (changed || nearestOnlyChanged)
        NearbyUpdated?.Invoke(_nearby);
    }

    private bool UpdateNearestOnlySelections(IReadOnlyList<IInteractable> interactables, Transform interactor)
    {
      _nearestOnlyScratch.Clear();
      _nearestOnlyDistanceScratch.Clear();

      for (int i = 0; i < interactables.Count; i++)
      {
        var interacts = interactables[i]?.Interacts;
        if (interacts == null)
          continue;

        for (int j = 0; j < interacts.Length; j++)
        {
          var interact = interacts[j];
          if (!(interact is INearestOnlyInteract candidate)
              || (interact is IInteractorConditional conditional && !conditional.CanInteract(interactor)))
            continue;

          string group = candidate.NearestOnlyGroup;
          Transform origin = candidate.NearestOnlyDistanceOrigin;
          if (string.IsNullOrWhiteSpace(group) || origin == null)
            continue;

          float sqrDistance = (origin.position - interactor.position).sqrMagnitude;
          if (!_nearestOnlyDistanceScratch.TryGetValue(group, out float nearestDistance) || sqrDistance < nearestDistance)
          {
            _nearestOnlyDistanceScratch[group] = sqrDistance;
            _nearestOnlyScratch[group] = interact;
          }
        }
      }

      bool changed = !HaveSameSelections(_nearestOnly, _nearestOnlyScratch);
      if (!changed)
        return false;

      _nearestOnly.Clear();
      foreach (var pair in _nearestOnlyScratch)
        _nearestOnly.Add(pair.Key, pair.Value);
      return true;
    }

    private static bool HaveSameSelections(
      IReadOnlyDictionary<string, IInteract> previous,
      IReadOnlyDictionary<string, IInteract> next)
    {
      if (previous.Count != next.Count)
        return false;

      foreach (var pair in next)
      {
        if (!previous.TryGetValue(pair.Key, out var previousInteract)
            || !ReferenceEquals(previousInteract, pair.Value))
          return false;
      }

      return true;
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
      if (detectBased.IsUnityNull()) return;
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(detectBased.position + detectionOffset, detectionRedius);
    }
#endif
  }
}
