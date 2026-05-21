using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class MovingPatientBedController
  {
    [Header("Attachment Display")]
    [SerializeField] private bool _intravenousStandAttached;
    [SerializeField] private GameObject _intravenousStandReference;
    [SerializeField] private bool _intravenousHangerAttached;
    [SerializeField] private GameObject _intravenousHangerReference;
    [SerializeField] private bool _intravenousFluidAttached;
    [SerializeField] private GameObject _intravenousFluidReference;

    private void SyncPatientAttachmentVisuals(Transform patientAnchor)
    {
      SyncPatientAttachmentVisual(_intravenousStandReference, _intravenousStandAttached, patientAnchor);
      SyncPatientAttachmentVisual(_intravenousHangerReference, _intravenousHangerAttached, patientAnchor);
      SyncPatientAttachmentVisual(_intravenousFluidReference, _intravenousFluidAttached, patientAnchor);
    }

    private static void SyncPatientAttachmentVisual(GameObject target, bool isAttached, Transform patientAnchor)
    {
      if (target == null)
        return;

      target.SetActive(isAttached);
      if (!isAttached || patientAnchor == null)
        return;

      target.transform.SetPositionAndRotation(patientAnchor.position, patientAnchor.rotation);
    }
  }
}
