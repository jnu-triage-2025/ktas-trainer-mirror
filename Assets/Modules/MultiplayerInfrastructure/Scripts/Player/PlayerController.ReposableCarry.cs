using MultiplayerInfrastructure.Entity;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Reposable Carry")]
    [SerializeField] private Transform _reposableCarryAnchor;
    [SerializeField] private MonoBehaviour _carriedReposableComponent;

    public bool IsCarryingReposable => CarriedReposable != null;
    public IReposable CarriedReposable => _carriedReposableComponent as IReposable;

    public bool TryPickUpReposable(IReposable reposable)
    {
      if (reposable == null)
        return false;

      if (IsCarryingReposable)
        return false;

      if (reposable is not MonoBehaviour target)
        return false;

      var anchor = _reposableCarryAnchor != null ? _reposableCarryAnchor : transform;

      _carriedReposableComponent = target;
      target.transform.SetParent(anchor, false);
      target.transform.localPosition = Vector3.zero;
      target.transform.localRotation = Quaternion.identity;
      return true;
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
      _carriedReposableComponent = null;
      return true;
    }
  }
}
