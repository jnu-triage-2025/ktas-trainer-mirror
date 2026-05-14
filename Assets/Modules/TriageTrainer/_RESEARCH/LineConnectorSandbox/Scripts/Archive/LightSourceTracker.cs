using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// 마우스 커서를 광원으로 간주해 동공 영역 진입 여부를 판정합니다.
  /// </summary>
  [DisallowMultipleComponent]
  public class LightSourceTracker : MonoBehaviour
  {
    [Header("Eye References")]
    [SerializeField] private PupilReflex leftPupil;
    [SerializeField] private PupilReflex rightPupil;

    [Header("Legacy Radius Fallback")]
    [Tooltip("RectTransform을 찾지 못했을 때만 사용하는 fallback 반경(픽셀)")]
    [SerializeField, Min(1f)] private float lightRadius = 40f;

    private void Update()
    {
      Vector2 mousePos = Input.mousePosition;

      UpdatePupil(leftPupil, mousePos);
      UpdatePupil(rightPupil, mousePos);
    }

    private void UpdatePupil(PupilReflex pupil, Vector2 mouseScreenPos)
    {
      if (pupil == null)
      {
        return;
      }

      RectTransform pupilRect = pupil.RectTransform;
      bool isLightOnPupil = IsPointInsidePupil(pupilRect, mouseScreenPos);
      pupil.TriggerLightReflex(isLightOnPupil);
    }

    private bool IsPointInsidePupil(RectTransform pupilRect, Vector2 screenPoint)
    {
      if (pupilRect == null)
      {
        return false;
      }

      Canvas rootCanvas = pupilRect.GetComponentInParent<Canvas>();
      Camera uiCamera = ResolveUiCamera(rootCanvas);

      if (!RectTransformUtility.RectangleContainsScreenPoint(pupilRect, screenPoint, uiCamera))
      {
        return false;
      }

      if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        pupilRect,
        screenPoint,
        uiCamera,
        out Vector2 localPoint))
      {
        float radiusX = Mathf.Max(0.0001f, pupilRect.rect.width * 0.5f);
        float radiusY = Mathf.Max(0.0001f, pupilRect.rect.height * 0.5f);

        float normalizedX = localPoint.x / radiusX;
        float normalizedY = localPoint.y / radiusY;

        return (normalizedX * normalizedX) + (normalizedY * normalizedY) <= 1f;
      }

      float distance = Vector2.Distance(screenPoint, RectTransformUtility.WorldToScreenPoint(uiCamera, pupilRect.position));
      return distance <= lightRadius;
    }

    private static Camera ResolveUiCamera(Canvas rootCanvas)
    {
      if (rootCanvas == null)
      {
        return Camera.main;
      }

      if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
      {
        return null;
      }

      if (rootCanvas.worldCamera != null)
      {
        return rootCanvas.worldCamera;
      }

      return Camera.main;
    }
  }
}