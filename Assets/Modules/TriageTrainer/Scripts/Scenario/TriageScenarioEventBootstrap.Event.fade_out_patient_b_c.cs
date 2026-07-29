using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private GameObject _patientBCFinalFadeOverlay;
    private CanvasGroup _patientBCFinalFadeCanvasGroup;

    private void RegisterEvent_FadeOutPatientBC()
    {
      Register("fade_out_patient_b_c", Event_FadeOutPatientBC);
    }

    private IEnumerator Event_FadeOutPatientBC()
    {
      EnsurePatientBCFinalFadeOverlay();
      if (_patientBCFinalFadeCanvasGroup == null)
        yield break;

      _patientBCFinalFadeOverlay.SetActive(true);
      _patientBCFinalFadeCanvasGroup.alpha = 0f;

      const float durationSeconds = 1f;
      float elapsed = 0f;
      while (elapsed < durationSeconds)
      {
        elapsed += Time.unscaledDeltaTime;
        _patientBCFinalFadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / durationSeconds);
        yield return null;
      }

      _patientBCFinalFadeCanvasGroup.alpha = 1f;
    }

    private void EnsurePatientBCFinalFadeOverlay()
    {
      if (_patientBCFinalFadeOverlay != null)
        return;

      _patientBCFinalFadeOverlay = new GameObject("PatientBCFinalFadeOverlay");
      _patientBCFinalFadeOverlay.transform.SetParent(transform, false);

      var canvas = _patientBCFinalFadeOverlay.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = short.MaxValue;

      _patientBCFinalFadeCanvasGroup = _patientBCFinalFadeOverlay.AddComponent<CanvasGroup>();
      var image = _patientBCFinalFadeOverlay.AddComponent<Image>();
      image.color = Color.black;
      image.raycastTarget = true;

      var rect = image.rectTransform;
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;

      _patientBCFinalFadeOverlay.SetActive(false);
    }

    private void DisposePatientBCFinalFadeOverlay()
    {
      if (_patientBCFinalFadeOverlay != null)
        Destroy(_patientBCFinalFadeOverlay);

      _patientBCFinalFadeOverlay = null;
      _patientBCFinalFadeCanvasGroup = null;
    }
  }
}
