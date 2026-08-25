using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    [Header("Collider")]
    [SerializeField] private CapsuleCollider _capsuleCollider;
    [SerializeField] private Vector3 _standingCapsuleCenter = new Vector3(0f, 0.9f, 0f);
    [SerializeField, Min(0.01f)] private float _standingCapsuleHeight = 1.8f;
    [SerializeField, Min(0.01f)] private float _standingCapsuleRadius = 0.3f;
    [SerializeField, Range(0, 2)] private int _standingCapsuleDirection = 1;

    [SerializeField] private Vector3 _lyingCapsuleCenter = new Vector3(0f, 0.45f, 0f);
    [SerializeField, Min(0.01f)] private float _lyingCapsuleHeight = 1.8f;
    [SerializeField, Min(0.01f)] private float _lyingCapsuleRadius = 0.3f;
    [SerializeField, Range(0, 2)] private int _lyingCapsuleDirection = 2;

    private bool _hasAppliedLyingCollider;

    private void InitializeCollider()
    {
      if (_capsuleCollider == null)
        _capsuleCollider = GetComponent<CapsuleCollider>();

      _hasAppliedLyingCollider = false;
      ApplyColliderSettings(isLying: false);
    }

    private void ApplyColliderSettings(bool isLying)
    {
      if (_capsuleCollider == null)
        return;

      if (isLying)
      {
        if (TryGetLayingOnMovingBedCollider(out var center, out float height, out float radius, out int direction))
        {
          _capsuleCollider.center = center;
          _capsuleCollider.height = Mathf.Max(0.01f, height);
          _capsuleCollider.radius = Mathf.Max(0.01f, radius);
          _capsuleCollider.direction = Mathf.Clamp(direction, 0, 2);
        }
        else
        {
          _capsuleCollider.center = _lyingCapsuleCenter;
          _capsuleCollider.height = Mathf.Max(0.01f, _lyingCapsuleHeight);
          _capsuleCollider.radius = Mathf.Max(0.01f, _lyingCapsuleRadius);
          _capsuleCollider.direction = Mathf.Clamp(_lyingCapsuleDirection, 0, 2);
        }
      }
      else
      {
        _capsuleCollider.center = _standingCapsuleCenter;
        _capsuleCollider.height = Mathf.Max(0.01f, _standingCapsuleHeight);
        _capsuleCollider.radius = Mathf.Max(0.01f, _standingCapsuleRadius);
        _capsuleCollider.direction = Mathf.Clamp(_standingCapsuleDirection, 0, 2);
      }
    }

    private void ApplyColliderForAnimationState(bool isLying)
    {
      if (_capsuleCollider == null)
        _capsuleCollider = GetComponent<CapsuleCollider>();

      if (_capsuleCollider == null)
        return;

      if (_hasAppliedLyingCollider == isLying)
        return;

      ApplyColliderSettings(isLying);
      _hasAppliedLyingCollider = isLying;
    }

    private void OnMovingPatientBedAttachedStateChanged(bool isAttached)
    {
      ApplyColliderForAnimationState(isAttached);
    }
  }
}
