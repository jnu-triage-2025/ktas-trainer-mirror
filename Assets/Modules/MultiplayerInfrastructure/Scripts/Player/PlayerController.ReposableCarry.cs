using MultiplayerInfrastructure.Entity;
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
      target.transform.SetParent(anchor, false);
      if (TryResolveTargetCarryAttachPoint(target.transform, out Transform targetAttachPoint))
      {
        target.transform.localPosition = -targetAttachPoint.localPosition;
        target.transform.localRotation = Quaternion.Inverse(targetAttachPoint.localRotation);
      }
      else
      {
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;
      }

      reposable.OnPlayerAttachedEnter();
      return true;
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

      _carriedReposableComponent.transform.SetParent(null, true);
      ResolveDropTransform(_carriedReposableComponent.transform);
      dropped.OnPlayerAttachedExit();
      _carriedReposableComponent = null;
      return true;
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
