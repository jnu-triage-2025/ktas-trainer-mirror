using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    [Header("Collider")]
    [SerializeField] private CapsuleCollider _capsuleCollider;
    [SerializeField] private Vector3 _capsuleCenter = new Vector3(0f, 0.9f, 0f);
    [SerializeField, Min(0.01f)] private float _capsuleHeight = 1.8f;
    [SerializeField, Min(0.01f)] private float _capsuleRadius = 0.3f;

    private void InitializeCollider()
    {
      if (_capsuleCollider == null)
        _capsuleCollider = GetComponent<CapsuleCollider>();

      ApplyColliderSettings();
    }

    private void ApplyColliderSettings()
    {
      if (_capsuleCollider == null)
        return;

      _capsuleCollider.center = _capsuleCenter;
      _capsuleCollider.height = Mathf.Max(0.01f, _capsuleHeight);
      _capsuleCollider.radius = Mathf.Max(0.01f, _capsuleRadius);
    }
  }
}
