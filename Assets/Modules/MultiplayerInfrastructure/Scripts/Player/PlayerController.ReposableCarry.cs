using System.Collections.Generic;
using MultiplayerInfrastructure.Entity;
using TriageTrainer.Entity;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const string DefaultCarryAnchorName = "ReposableCarryAttachPoint";
    private const string DefaultTargetCarryAttachPointName = "PatientCarryAttachPoint";
    private const float DefaultDropForwardDistance = 0.9f;
    private const float DefaultDropProbeHeight = 1.5f;
    private const float DefaultDropProbeDistance = 3f;

    [Header("Reposable Carry")]
    [SerializeField] private Transform _reposableCarryAnchor;
    [SerializeField] private MonoBehaviour _carriedReposableComponent;
    [SerializeField] private bool _followCarriedReposableWithoutParent;
    [SerializeField] private Vector3 _carriedAttachPointLocalPosition;
    [SerializeField] private Quaternion _carriedAttachPointLocalRotation = Quaternion.identity;

    private readonly List<Collider> _ignoredCarryColliders = new();

    public bool IsCarryingReposable => CarriedReposable != null;
    public IReposable CarriedReposable => _carriedReposableComponent as IReposable;

    private void Awake_ReposableCarry()
    {
      EnsureCarryAttachPoint();
    }

    private void OnValidate_ReposableCarry()
    {
      EnsureCarryAttachPoint();
    }

    private void EnsureCarryAttachPoint()
    {
      if (_reposableCarryAnchor != null && !_reposableCarryAnchor.IsChildOf(transform))
      {
        Debug.LogWarning("[PlayerController] ReposableCarryAnchor must be under the player hierarchy. Rebinding anchor.", this);
        _reposableCarryAnchor = null;
      }

      if (_reposableCarryAnchor != null)
        return;

      var typed = GetComponentInChildren<ReposableAttachPointObject>(true);
      if (typed != null)
      {
        _reposableCarryAnchor = typed.transform;
        return;
      }

      var existing = transform.Find(DefaultCarryAnchorName);
      if (existing != null)
      {
        _reposableCarryAnchor = existing;
        return;
      }

      var go = new GameObject(DefaultCarryAnchorName);
      go.AddComponent<ReposableAttachPointObject>();
      _reposableCarryAnchor = go.transform;
      _reposableCarryAnchor.SetParent(transform, false);
      _reposableCarryAnchor.localPosition = new Vector3(0f, 1.2f, 0.7f);
      _reposableCarryAnchor.localRotation = Quaternion.identity;
    }

    private void RefreshCarryAttachPointFromHierarchy()
    {
      var typed = GetComponentInChildren<ReposableAttachPointObject>(true);
      if (typed != null)
      {
        _reposableCarryAnchor = typed.transform;
        return;
      }

      EnsureCarryAttachPoint();
    }

    public bool TryPickUpReposable(IReposable reposable)
    {
      if (reposable == null)
        return false;

      if (IsCarryingReposable)
        return false;

      if (reposable is not MonoBehaviour target)
        return false;

      RefreshCarryAttachPointFromHierarchy();

      var anchor = _reposableCarryAnchor != null ? _reposableCarryAnchor : transform;

      _carriedReposableComponent = target;
      SetCarryCollisionIgnore(target.transform, true);

      bool shouldFollowByWorldAnchorOnly = target.TryGetComponent(out PatientController _);
      _followCarriedReposableWithoutParent = shouldFollowByWorldAnchorOnly;

      if (!shouldFollowByWorldAnchorOnly)
        target.transform.SetParent(anchor, false);

      if (TryResolveTargetCarryAttachPoint(target.transform, out Transform targetAttachPoint))
      {
        _carriedAttachPointLocalPosition = target.transform.InverseTransformPoint(targetAttachPoint.position);
        _carriedAttachPointLocalRotation = Quaternion.Inverse(target.transform.rotation) * targetAttachPoint.rotation;
      }
      else
      {
        _carriedAttachPointLocalPosition = Vector3.zero;
        _carriedAttachPointLocalRotation = Quaternion.identity;
      }

      SnapCarriedReposableToAnchor(target.transform, anchor);

      reposable.OnPlayerAttachedEnter();
      return true;
    }

    private void Update_ReposableCarry()
    {
      if (!_followCarriedReposableWithoutParent)
        return;

      if (_carriedReposableComponent == null)
      {
        SetCarryCollisionIgnore(null, false);
        _followCarriedReposableWithoutParent = false;
        return;
      }

      RefreshCarryAttachPointFromHierarchy();
      var anchor = _reposableCarryAnchor != null ? _reposableCarryAnchor : transform;
      SnapCarriedReposableToAnchor(_carriedReposableComponent.transform, anchor);
    }

    private void SnapCarriedReposableToAnchor(Transform targetTransform, Transform anchor)
    {
      if (targetTransform == null || anchor == null)
        return;

      Quaternion rootRotation = anchor.rotation * Quaternion.Inverse(_carriedAttachPointLocalRotation);
      Vector3 rootPosition = anchor.position - (rootRotation * _carriedAttachPointLocalPosition);

      targetTransform.SetPositionAndRotation(rootPosition, rootRotation);
    }

    private static bool TryResolveTargetCarryAttachPoint(Transform targetRoot, out Transform attachPoint)
    {
      attachPoint = null;
      if (targetRoot == null)
        return false;

      if (targetRoot.TryGetComponent(out TriageTrainer.Entity.PatientController patient) && patient.CarryAttachPoint != null)
      {
        attachPoint = patient.CarryAttachPoint;
        return true;
      }

      attachPoint = targetRoot.Find(DefaultTargetCarryAttachPointName);
      return attachPoint != null;
    }

    public bool TryDropCarriedReposable(out IReposable dropped)
    {
      dropped = CarriedReposable;
      if (dropped == null || _carriedReposableComponent == null)
      {
        dropped = null;
        return false;
      }

      if (!_followCarriedReposableWithoutParent)
        _carriedReposableComponent.transform.SetParent(null, true);

      ResolveDropTransform(_carriedReposableComponent.transform);
      dropped.OnPlayerAttachedExit();
      _carriedReposableComponent = null;
      SetCarryCollisionIgnore(null, false);
      _followCarriedReposableWithoutParent = false;
      _carriedAttachPointLocalPosition = Vector3.zero;
      _carriedAttachPointLocalRotation = Quaternion.identity;
      return true;
    }

    private void SetCarryCollisionIgnore(Transform carriedRoot, bool ignore)
    {
      var playerCollider = ResolveCarryPlayerCollider();
      if (playerCollider == null)
        return;

      if (!ignore)
      {
        for (int i = 0; i < _ignoredCarryColliders.Count; i++)
        {
          var col = _ignoredCarryColliders[i];
          if (col == null)
            continue;

          Physics.IgnoreCollision(playerCollider, col, false);
        }

        _ignoredCarryColliders.Clear();
        return;
      }

      _ignoredCarryColliders.Clear();
      if (carriedRoot == null)
        return;

      var carriedColliders = carriedRoot.GetComponentsInChildren<Collider>(true);
      for (int i = 0; i < carriedColliders.Length; i++)
      {
        var col = carriedColliders[i];
        if (col == null || ReferenceEquals(col, playerCollider))
          continue;

        Physics.IgnoreCollision(playerCollider, col, true);
        _ignoredCarryColliders.Add(col);
      }
    }

    private Collider ResolveCarryPlayerCollider()
    {
      if (_characterController == null)
        _characterController = GetComponent<CharacterController>();

      return _characterController;
    }

    private void ResolveDropTransform(Transform droppedTransform)
    {
      if (droppedTransform == null)
        return;

      Vector3 origin = transform.position + transform.forward * DefaultDropForwardDistance;
      Vector3 probeStart = origin + Vector3.up * DefaultDropProbeHeight;
      if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, DefaultDropProbeDistance, ~0, QueryTriggerInteraction.Ignore))
      {
        origin = hit.point;
      }

      droppedTransform.position = origin;
    }
  }
}
