using UnityEngine;
using UnityEngine.Events;

namespace TriageTrainer.Tests.PupilReflexSandbox
{
  /// <summary>
  /// 양쪽 눈의 직접 대사광 검사 완료 상태를 추적하고, 이후 시나리오 로직에 연결할 수 있는
  /// UnityEvent 를 노출한다.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilReflexExamController : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private PupilReflexEye leftEye;
    [SerializeField] private PupilReflexEye rightEye;

    [Header("Status Overlay")]
    [SerializeField] private bool showStatusOverlay = true;
    [SerializeField] private Vector2 overlayPosition = new Vector2(12f, 12f);

    [Header("Events")]
    [SerializeField] private UnityEvent onLeftEyeChecked;
    [SerializeField] private UnityEvent onRightEyeChecked;
    [SerializeField] private UnityEvent onBothEyesChecked;

    private bool leftChecked;
    private bool rightChecked;
    private bool completionEventRaised;

    public bool LeftChecked => leftChecked;
    public bool RightChecked => rightChecked;
    public bool IsCompleted => leftChecked && rightChecked;
    public bool HasAssignedEyes => leftEye != null || rightEye != null;

    private void Update()
    {
      if (!leftChecked && leftEye != null && leftEye.HasReceivedDirectLight)
      {
        leftChecked = true;
        onLeftEyeChecked?.Invoke();
      }

      if (!rightChecked && rightEye != null && rightEye.HasReceivedDirectLight)
      {
        rightChecked = true;
        onRightEyeChecked?.Invoke();
      }

      if (!completionEventRaised && leftChecked && rightChecked)
      {
        completionEventRaised = true;
        onBothEyesChecked?.Invoke();
      }
    }

    private void OnGUI()
    {
      if (!showStatusOverlay)
      {
        return;
      }

      float x = overlayPosition.x;
      float y = overlayPosition.y;

      string leftStatus = leftChecked ? "Checked" : "Pending";
      string rightStatus = rightChecked ? "Checked" : "Pending";
      string completed = IsCompleted ? "Yes" : "No";

      float leftPupil = leftEye != null ? leftEye.CurrentPupilDiameterMillimeters : 0f;
      float rightPupil = rightEye != null ? rightEye.CurrentPupilDiameterMillimeters : 0f;

      GUI.Label(
        new Rect(x, y, 560f, 80f),
        "Pupil Reflex Exam\n" +
        "- Left: " + leftStatus + "  |  Right: " + rightStatus + "  |  Completed: " + completed + "\n" +
        "- Pupil(mm) L/R: " + leftPupil.ToString("F2") + " / " + rightPupil.ToString("F2"));
    }

    public void ConfigureEyes(PupilReflexEye left, PupilReflexEye right)
    {
      leftEye = left;
      rightEye = right;
    }

    public void ResetProgress()
    {
      leftChecked = false;
      rightChecked = false;
      completionEventRaised = false;

      if (leftEye != null)
      {
        leftEye.ResetExamState();
      }

      if (rightEye != null)
      {
        rightEye.ResetExamState();
      }
    }

  }
}
